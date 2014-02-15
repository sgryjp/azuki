// file: TextBuffer.cs
// brief: A buffer object maintaining characters, lines, and other meta data.
//=========================================================
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// A buffer object maintaining characters, lines, and other meta data.
	/// </summary>
	internal class TextBuffer : IList<char>
	{
		#region Fields
		readonly Document _Document;
		readonly GapCharBuffer _Chars;
		readonly GapBuffer<CharClass> _Classes;
		internal readonly GapBuffer<int> _LHI; // line head indexes
		readonly GapBuffer<DirtyState> _LDS; // line dirty states
		readonly RleArray<uint> _MarkingBitMasks;
		readonly IList<WeakReference> _AutoUpdateTargets = new GapBuffer<WeakReference>( 32 );
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance. This must be called by Document.ctor.
		/// </summary>
		internal TextBuffer( Document doc, int initGapSize, int growSize )
		{
			_Document = doc;
			_Chars = new GapCharBuffer( initGapSize, growSize );
			_Classes = new GapBuffer<CharClass>( initGapSize, growSize );
			_LHI = new GapBuffer<int>( 64 ) {
				0
			};
			_LDS = new GapBuffer<DirtyState>( 64 ) {
				DirtyState.Clean
			};
			_MarkingBitMasks = new RleArray<uint>();
			LastModifiedTime = DateTime.Now;
		}
		#endregion

		internal GapBuffer<DirtyState> LDS
		{
			get{ return _LDS; }
		}

		#region Character Classes
		/// <summary>
		/// Clears class information from all characters.
		/// </summary>
		public void ClearCharClasses()
		{
			for( int i=0; i<_Classes.Count; i++ )
			{
				_Classes[i] = CharClass.Normal;
			}
		}

		/// <summary>
		/// Gets class of the character at specified index.
		/// </summary>
		public CharClass GetCharClassAt( int index )
		{
			return _Classes[ index ];
		}

		/// <summary>
		/// Sets class of the character at specified index.
		/// </summary>
		public void SetCharClassAt( int index, CharClass klass )
		{
			_Classes[ index ] = klass;
		}
		#endregion

		#region Marking
		public IList<uint> Marks
		{
			get{ return _MarkingBitMasks; }
		}
		#endregion

		#region Line / Column
		/// <exception cref="ArgumentOutOfRangeException"/>
		public LineColumnPos GetLineColumnPos( int charIndex )
		{
			if( charIndex < 0 || Count < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", charIndex, "Invalid index was"
													   + " given. (charIndex:" + charIndex
													   + ", Count:" + Count + ")." );

			return TextUtil.GetLineColumnPos( _Chars, _LHI, charIndex );
		}

		public int GetCharIndex( LineColumnPos pos )
		{
			Debug.Assert( 0 <= pos.Line );
			Debug.Assert( pos.Line < _LHI.Count );
			Debug.Assert( 0 <= pos.Column );

			return TextUtil.GetCharIndex( _Chars, _LHI, pos );
		}

		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public IRange GetTextRange( int beginLineIndex, int beginColumnIndex,
									int endLineIndex, int endColumnIndex )
		{
			return GetTextRange( new LineColumnPos(beginLineIndex, beginColumnIndex),
								 new LineColumnPos(endLineIndex, endColumnIndex) );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public IRange GetTextRange( LineColumnPos beginPos, LineColumnPos endPos )
		{
			if( endPos.Line < 0 || _LHI.Count <= endPos.Line )
				throw new ArgumentOutOfRangeException( "endPos", endPos, "Specified line index is"
													   + " out of valid range. (endPos:" + endPos
													   + ", line count:" + _LHI.Count + ")" );
			if( beginPos.Line < 0 || endPos.Line < beginPos.Line )
				throw new ArgumentOutOfRangeException( "beginPos", beginPos, "Specified line index"
													   + " is out of valid range. (beginPos:"
													   + beginPos + ", endPos:" + endPos + ")" );
			if( beginPos.Column < 0 )
				throw new ArgumentOutOfRangeException( "beginPos", beginPos, "Specified column"
													   + " index is out of valid range. (beginPos:"
													   + beginPos + ")" );
			if( endPos.Column < 0 )
				throw new ArgumentOutOfRangeException( "endPos", endPos,"Specified column index is"
													   + " out of valid range. (endPos:" + endPos
													   + ")" );
			if( beginPos.Line == endPos.Line && endPos.Column < beginPos.Column )
				throw new ArgumentOutOfRangeException( "endPos", "No valid range can be made with"
													   + " given 'beginPos' and 'endPos'."
													   + " (beginPos:" + beginPos + ", endPos:"
													   + endPos + ")" );

			// Calculate beginning position
			int begin = _LHI[beginPos.Line] + beginPos.Column;
			if( beginPos.Line + 1 < _LHI.Count && _LHI[beginPos.Line+1] < begin )
			{
				throw new ArgumentOutOfRangeException( "beginPos", beginPos, "'beginPos' is out of"
													   + " valid range. (beginPos:" + beginPos
													   + ", Lines[" + beginPos.Line + "].Length:"
													   + GetLineRange(beginPos.Line).Length+")." );
			}
			if( Count < begin )
			{
				throw new ArgumentOutOfRangeException( "beginPos", beginPos, "'beginPos' is out of"
													   + " valid range. (calculated begin:" + begin
													   + ", Count:" + Count
													   + ", Lines[" + beginPos.Line + "].Length:"
													   + GetLineRange(beginPos.Line).Length+")." );
			}

			// Calculate ending position
			int end = _LHI[endPos.Line] + endPos.Column;
			if( endPos.Line + 1 < _LHI.Count && _LHI[endPos.Line+1] < end )
			{
				throw new ArgumentOutOfRangeException( "endPos", endPos, "'endPos' is out of valid"
													   + " range. (endPos:" + endPos + ", Lines["
													   + endPos.Line + "].Length:"
													   + GetLineRange(endPos.Line).Length+")." );
			}
			if( Count < end )
			{
				throw new ArgumentOutOfRangeException( "endPos", endPos, "'endPos' is out of valid"
													   + " range. (calculated end:" + end
													   + ", Count:" + Count
													   + ", Lines[" + endPos.Line + "].Length:"
													   + GetLineRange(endPos.Line).Length+")." );
			}
			Debug.Assert( begin <= end );

			// Return the range
			return new Range( begin, end );
		}

		/// <summary>
		/// Maintain line head indexes for text insertion. MUST BE CALLED BEFORE ACTUAL INSERTION.
		/// </summary>
		void LHI_Insert( string insertText, int insertIndex )
		{
			DebugUtl.Assert( insertText != null, "insertText must not be null." );
			DebugUtl.Assert( 0 <= insertIndex && insertIndex <= _Chars.Count,
							 "insertIndex is out of range (" + insertIndex + ")." );
			LineColumnPos insPos;
			int lineIndex; // work variable
			int lineHeadIndex;
			int lineEndIndex;
			int insLineCount;

			// At first, find the line which contains the insertion point
			insPos = GetLineColumnPos( insertIndex );
			lineIndex = insPos.Line;

			// If the inserting divides a CR+LF, insert an entry for the CR separated
			if( 0 < insertIndex && _Chars[insertIndex-1] == '\r'
				&& insertIndex < _Chars.Count && _Chars[insertIndex] == '\n' )
			{
				_LHI.Insert( lineIndex+1, insertIndex );
				_LDS.Insert( lineIndex+1, DirtyState.Dirty );
				lineIndex++;
			}

			// If inserted text begins with LF and is inserted just after a CR, remove this CR's
			// entry
			if( 0 < insertIndex && _Chars[insertIndex-1] == '\r'
				&& 0 < insertText.Length && insertText[0] == '\n' )
			{
				_LHI.RemoveAt( lineIndex );
				_LDS.RemoveAt( lineIndex );
				lineIndex--;
			}

			// Insert line index entries
			insLineCount = 1;
			lineHeadIndex = 0;
			do
			{
				// Get end index of this line
				lineEndIndex = TextUtil.NextLineHead( insertText, lineHeadIndex ) - 1;
				if( lineEndIndex == -2 ) // == "if NextLineHead returns -1"
				{
					// No more lines follows so no more entries are needed
					break;
				}
				_LHI.Insert( lineIndex+insLineCount,insertIndex+lineEndIndex+1);
				_LDS.Insert( lineIndex+insLineCount, DirtyState.Dirty );
				insLineCount++;

				// Find next line head
				lineHeadIndex = TextUtil.NextLineHead( insertText, lineHeadIndex );
			}
			while( lineHeadIndex != -1 );

			// If finaly character of the inserted string is CR and if it is inserted just before
			// an LF, remove this CR's entry since it will be a part of a CR+LF
			if( 0 < insertText.Length
				&& insertText[insertText.Length - 1] == '\r'
				&& insertIndex < _Chars.Count
				&& _Chars[insertIndex] == '\n' )
			{
				int lastInsertedLine = lineIndex + insLineCount - 1;
				_LHI.RemoveAt( lastInsertedLine );
				_LDS.RemoveAt( lastInsertedLine );
				lineIndex--;
			}

			// shift all the followings
			for( int i=lineIndex+insLineCount; i<_LHI.Count; i++ )
			{
				_LHI[i] += insertText.Length;
			}

			// mark the insertion target line as 'dirty'
			if( 0 < insertText.Length && insertText[0] == '\n'
				&& 0 < insertIndex && _Chars[insertIndex-1] == '\r'
				&& insertIndex < _Chars.Count && _Chars[insertIndex] != '\n' )
			{
				// Inserted text has an LF at beginning and there is a CR (not part of a CR+LF) at
				// insertion point so a new CR+LF is made. Since newly made CR+LF is regarded as
				// part of the line which originally ended with a CR, the line should be marked as
				// modified.
				DebugUtl.Assert( 0 < insPos.Line );
				_LDS[insPos.Line-1] = DirtyState.Dirty;
			}
			else
			{
				_LDS[insPos.Line] = DirtyState.Dirty;
			}
		}
		
		/// <summary>
		/// Maintain line head indexes for text deletion. MUST BE CALLED BEFORE ACTUAL DELETION.
		/// </summary>
		void LHI_Delete( int delBegin, int delEnd )
		{
			DebugUtl.Assert( 0 <= delBegin && delBegin < _Chars.Count,
							 "delBegin is out of range." );
			DebugUtl.Assert( delBegin <= delEnd && delEnd <= _Chars.Count,
							 "delEnd is out of range." );
			int delFirstLine;
			int delLen = delEnd - delBegin;

			// calculate line indexes of both ends of the range
			var delFromPos = GetLineColumnPos( delBegin );
			var delToPos = GetLineColumnPos( delEnd );
			delFirstLine = delFromPos.Line;

			if( 0 < delBegin && _Chars[delBegin-1] == '\r' )
			{
				if( delEnd < _Chars.Count && _Chars[delEnd] == '\n' )
				{
					// Delete an entry of a line terminated with a CR in case of that the CR will
					// be merged into an CR+LF.
					_LHI.RemoveAt( delToPos.Line );
					_LDS.RemoveAt( delToPos.Line );
					delToPos.Line--;
				}
				else if( _Chars[delBegin] == '\n' )
				{
					// Insert an entry of a line terminated with a CR in case of that an LF was
					// removed from an CR+LF.
					_LHI.Insert( delToPos.Line, delBegin );
					_LDS.Insert( delToPos.Line, DirtyState.Dirty );
					delFromPos.Line++;
					delToPos.Line++;
				}
			}

			// subtract line head indexes for lines after deletion point
			for( int i=delToPos.Line+1; i<_LHI.Count; i++ )
			{
				_LHI[i] -= delLen;
			}

			// if deletion decreases line count, delete entries
			if( delFromPos.Line < delToPos.Line )
			{
				_LHI.RemoveRange( delFromPos.Line+1, delToPos.Line+1 );
				_LDS.RemoveRange( delFromPos.Line+1, delToPos.Line+1 );
			}

			// mark the deletion target line as 'dirty'
			if( 0 < delBegin && _Chars[delBegin-1] == '\r'
				&& delEnd < _Chars.Count && _Chars[delEnd] == '\n'
				&& 0 < delFirstLine )
			{
				// This deletion combines a CR and an LF. Since newly made CR+LF is regarded as
				// part of the line which originally ended with a CR, the line should be marked as
				// modified.
				_LDS[delFirstLine-1] = DirtyState.Dirty;
			}
			else
			{
				_LDS[delFirstLine] = DirtyState.Dirty;
			}
		}
		#endregion

		#region Content Access
		public int GetLineCount()
		{
			Debug.Assert( _LHI.Count == _LDS.Count );
			return _LHI.Count;
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public ILineRange GetLineRange( int lineIndex )
		{
			return GetLineRange( lineIndex, false );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public ILineRange GetLineRange( int lineIndex, bool includesEolCode )
		{
			if( lineIndex < 0 || _LHI.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", lineIndex, "Invalid line index"
													   + " was given (lineIndex:" + lineIndex
													   + ", line count:" + _LHI.Count + ")." );

			var range = TextUtil.GetLineRange( _Chars, _LHI, lineIndex, includesEolCode );
			return new LineRange( _Document, range.Begin, range.End, lineIndex );
		}

		public ILineRange GetLineRangeFromCharIndex( int charIndex, bool includesEolCode )
		{
			Debug.Assert( 0 <= charIndex );
			Debug.Assert( charIndex <= _Chars.Count );

			var lineIndex = TextUtil.GetLineIndexFromCharIndex( _LHI, charIndex );
			var range = TextUtil.GetLineRange( _Chars, _LHI, lineIndex, includesEolCode );
			return new LineRange( _Document, range.Begin, range.End, lineIndex );;
		}

		public string GetText()
		{
			return GetText( new Range(0, Count) );
		}

		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public string GetText( IRange range )
		{
			if( range == null )
				throw new ArgumentNullException( "range" );
			if( range.Begin < 0 || range.End < 0 || Count < range.End )
				throw new ArgumentOutOfRangeException( "range", range, "Invalid index was given"
													   + " (range:" + range + ", Count:"
													   + Count + ")." );
			if( range.End < range.Begin )
				throw new ArgumentException( "range", "Invalid range was given: " + range );

			if( range.IsEmpty )
				return String.Empty;

			// constrain indexes to avoid dividing a grapheme cluster
			TextUtil.ConstrainIndex( _Chars, ref range );

			// retrieve a part of the content
			var buf = new char[range.Length];
			_Chars.CopyTo( range.Begin, range.End, buf );
			return new String( buf );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public string GetText( int beginLineIndex, int beginColumnIndex, int endLineIndex, int endColumnIndex )
		{
			return GetText( GetTextRange(beginLineIndex, beginColumnIndex,
										 endLineIndex, endColumnIndex) );
		}

		/// <summary>
		/// Gets or sets the size of the internal buffer.
		/// </summary>
		/// <exception cref="System.OutOfMemoryException">There is no enough memory to expand buffer.</exception>
		public int Capacity
		{
			get{ return _Chars.Capacity; }
			set
			{
				_Chars.Capacity = value;
				_Classes.Capacity = value;
				//NO_NEED//_MarkingBitMasks.Xxx = value;
			}
		}
		#endregion

		#region Edit
		public void Add( char ch )
		{
			Add( new String(ch, 1) );
		}

		public void Add( char[] chars )
		{
			Insert( Count, chars );
		}

		public void Add( string str )
		{
			Add( str.ToCharArray() );
		}

		public void Insert( int index, char ch )
		{
			Insert( index, new String(ch, 1) );
		}

		public void Insert( int index, char[] chars )
		{
			Insert( index, new String(chars) );
		}

		public void Insert( int index, string str )
		{
			LHI_Insert( str, index );
			_Chars.Insert( index, str );
			_Classes.Insert( index, new CharClass[str.Length] );
			_MarkingBitMasks.Insert( index, 0, str.Length );
			LastModifiedTime = DateTime.Now;

			Debug.Assert( _Chars.Count == _Classes.Count );
			Debug.Assert( _Chars.Count == _MarkingBitMasks.Count );
			Debug.Assert( _LHI.Count == _LDS.Count );

			InvokeContentChanged( index, String.Empty, str );
		}

		public void RemoveAt( int index )
		{
			Remove( index, index+1 );
		}

		public bool Remove( char item )
		{
			int index = IndexOf( item );
			if( 0 <= index )
			{
				RemoveAt( index );
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Removes elements at specified range.
		/// </summary>
		public void Remove( IRange range )
		{
			Remove( range.Begin, range.End );
		}

		/// <summary>
		/// Removes elements at specified range [begin, end).
		/// </summary>
		public void Remove( int begin, int end )
		{
			var oldText = GetText( new Range(begin, end) );

			LHI_Delete( begin, end );
			_Chars.RemoveRange( begin, end );
			_Classes.RemoveRange( begin, end );
			for( int i=begin; i<end; i++ )
			{
				_MarkingBitMasks.RemoveAt( begin );
			}
			LastModifiedTime = DateTime.Now;

			Debug.Assert( _Chars.Count == _Classes.Count );
			Debug.Assert( _Chars.Count == _MarkingBitMasks.Count );
			Debug.Assert( _LHI.Count == _LDS.Count );

			InvokeContentChanged( begin, oldText, String.Empty );
		}

		/// <summary>
		/// Deletes all elements.
		/// </summary>
		public void Clear()
		{
			_Chars.Clear();
			_Classes.Clear();
			_MarkingBitMasks.Clear();
			LastModifiedTime = DateTime.Now;
		}
		#endregion

		#region Text Search
		/// <summary>
		/// Finds a text pattern.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The index of the position to start searching.</param>
		/// <param name="end">The index of the position to stop searching.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Range of the firstly found pattern or null if not found.</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public IRange FindNext( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", Count:"+Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			int foundBegin, foundEnd;
			_Chars.FindNext( value, begin, end, matchCase, out foundBegin, out foundEnd );
			return (foundBegin < 0) ? null
									: new Range( _Document, foundBegin, foundEnd );
		}

		/// <summary>
		/// Finds previous occurrence of a text pattern.
		/// </summary>
		/// <param name="value">The string to find.</param>
		/// <param name="begin">The index of the position to start searching.</param>
		/// <param name="end">The index of the position to stop searching.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Range of the firstly found pattern or null if not found.</returns>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public IRange FindPrev( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", Count:"+Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			int foundBegin, foundEnd;
			_Chars.FindPrev( value, begin, end, matchCase, out foundBegin, out foundEnd );
			return (foundBegin < 0) ? null
									: new Range( _Document, foundBegin, foundEnd );
		}

		/// <summary>
		/// Find a text pattern.
		/// </summary>
		/// <param name="regex">Regular expression of the text pattern to find.</param>
		/// <param name="begin">The index of the position to start searching.</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <returns>Range of the firstly found pattern or null if not found.</returns>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public IRange FindNext( Regex regex, int begin, int end )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", Count:"+Count+")" );
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( (regex.Options & RegexOptions.RightToLeft) != 0 )
				throw new ArgumentException( "RegexOptions.RightToLeft option must not be set to the object 'regex'.", "regex" );

			int foundBegin, foundEnd;
			_Chars.FindNext( regex, begin, end, out foundBegin, out foundEnd );
			return (foundBegin < 0) ? null
									: new Range( _Document, foundBegin, foundEnd );
		}

		/// <summary>
		/// Find previous occurence of a text pattern.
		/// </summary>
		/// <param name="regex">Regular expression of the text pattern to find.</param>
		/// <param name="begin">The index of the position to start searching.</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <returns>Range of the firstly found pattern or null if not found.</returns>
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public IRange FindPrev( Regex regex, int begin, int end )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", Count:"+Count+")" );
			if( regex == null )
				throw new ArgumentNullException( "regex" );
			if( (regex.Options & RegexOptions.RightToLeft) == 0 )
				throw new ArgumentException( "RegexOptions.RightToLeft option must be set to the object 'regex'.", "regex" );

			int foundBegin, foundEnd;
			_Chars.FindPrev( regex, begin, end, out foundBegin, out foundEnd );
			return (foundBegin < 0) ? null
									: new Range( _Document, foundBegin, foundEnd );
		}
		#endregion

		#region Event
		public event ContentChangedEventHandler ContentChanged;
		void InvokeContentChanged( int index, string oldText, string newText )
		{
			Debug.Assert( 0 <= index );
			Debug.Assert( index <= Count );
			Debug.Assert( oldText != null );
			Debug.Assert( newText != null );

			DoAutoUpdate( index, oldText, newText );

			// Fire events for every normal listeners
			if( ContentChanged != null )
				ContentChanged( this, new ContentChangedEventArgs(index, oldText, newText) );
		}
		#endregion

		#region AutoUpdate
		internal void RemoveAutoUpdateTarget( IRange range )
		{
			for( int i=0; i<_AutoUpdateTargets.Count; i++ )
			{
				var wr = _AutoUpdateTargets[i];
				if( ReferenceEquals(range, wr.Target) )
				{
					_AutoUpdateTargets.RemoveAt( i );
					return;
				}
			}
		}

		internal void AddAutoUpdateTarget( IRange range )
		{
			Debug.Assert( range != null );

			foreach( var wr in _AutoUpdateTargets )
				if( wr.IsAlive && ReferenceEquals(range, wr.Target) )
					return;
			_AutoUpdateTargets.Add( new WeakReference(range) );
		}

		void DoAutoUpdate( int index, string oldText, string newText )
		{
			Debug.Assert( 0 <= index );
			Debug.Assert( index <= Count );
			Debug.Assert( oldText != null );
			Debug.Assert( newText != null );

			var deadRanges = new Stack<int>();
			for( int i=0; i<_AutoUpdateTargets.Count; i++ )
			{
				var range = _AutoUpdateTargets[i];
				if( range.IsAlive )
					DoAutoUpdate( (IRange)range.Target, index, oldText, newText );
				else
					deadRanges.Push( i );
			}
			while( 0 < deadRanges.Count )
			{
				_AutoUpdateTargets.RemoveAt( deadRanges.Pop() );
			}
		}

		void DoAutoUpdate( IRange range, int index, string oldText, string newText )
		{
			Debug.Assert( range.Begin <= range.End );

			// Removal
			if( 0 < oldText.Length )
			{
				if( index < range.Begin )
				{
					if( index + oldText.Length <= range.Begin )
						range.Begin -= oldText.Length;
					else
						range.Begin -= range.Begin - index;
				}
				if( index <= range.End )
				{
					if( index + oldText.Length <= range.End )
						range.End -= oldText.Length;
					else
						range.End -= range.End - index;
				}
			}

			// Insertion
			var diff = newText.Length;
			if( 0 < diff )
			{
				if( range.IsEmpty )
				{
					if( index <= range.Begin )
					{
						range.Begin += diff;
						range.End += diff;
					}
				}
				else if( index < range.Begin )
				{
					range.Begin += diff;
					range.End += diff;
				}
				else if( index == range.Begin )
				{
					if( index == range.Begin
						&& ((int)range.AutoUpdateMode & 0x10) != 0 )
						range.Begin += newText.Length;
					range.End += diff;
				}
				else if( index < range.End )
				{
					range.End += newText.Length;
				}
				else if( range.End == index )
				{
					if( ((int)range.AutoUpdateMode & 0x20) != 0 )
						range.End += newText.Length;
				}
			}
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets when this buffer was edited lastly. Note that changing meta data such as marking
		/// is not regarded as an 'edit' here.
		/// </summary>
		public DateTime LastModifiedTime
		{
			get;
			internal set;
		}

		/// <summary>
		/// Gets a character.
		/// </summary>
		public char this[int index]
		{
			get{ return _Chars[index]; }
			set{ _Chars[index] = value; }
		}

		/// <summary>
		/// Gets a substring from a specified range.
		/// </summary>
		public string this[Range range]
		{
			get{ return GetText(range); }
		}

#		if DEBUG
		/// <summary>
		/// ToString for Debug.
		/// </summary>
		public override string ToString()
		{
			var buf = new System.Text.StringBuilder( this.Count );
			for( int i=0; i<Count; i++ )
			{
				buf.Append( this[i] );
			}
			return buf.ToString();
		}
#		endif
		#endregion

		#region IList<char> Members
		public int IndexOf( char item )
		{
			for( int i=0; i<Count; i++ )
				if( this[i] == item )
					return i;
			return -1;
		}

		public bool Contains( char item )
		{
			return (0 <= IndexOf(item));
		}

		public void CopyTo( char[] array, int arrayIndex )
		{
			_Chars.CopyTo( array, arrayIndex );
		}

		public int Count
		{
			get{ return _Chars.Count; }
		}

		public bool IsReadOnly
		{
			get{ return false; }
		}

		public IEnumerator<char> GetEnumerator()
		{
			return _Chars.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _Chars.GetEnumerator();
		}
		#endregion
	}
}
