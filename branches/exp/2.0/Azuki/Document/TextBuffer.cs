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
	class TextBuffer : IList<char>
	{
		#region Fields
		readonly GapCharBuffer _Chars;
		readonly GapBuffer<CharClass> _Classes;
		readonly GapBuffer<int> _LHI; // line head indexes
		readonly GapBuffer<LineDirtyState> _LDS; // line dirty states
		readonly RleArray<uint> _MarkingBitMasks;
		readonly IList<WeakReference> _TrackingRanges = new GapBuffer<WeakReference>( 32 );
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TextBuffer( int initGapSize, int growSize )
		{
			_Chars = new GapCharBuffer( initGapSize, growSize );
			_Classes = new GapBuffer<CharClass>( initGapSize, growSize );
			_LHI = new GapBuffer<int>( 64 ) {
				0
			};
			_LDS = new GapBuffer<LineDirtyState>( 64 ) {
				LineDirtyState.Clean
			};
			_MarkingBitMasks = new RleArray<uint>();
		}
		#endregion

		#region そのうち削除
		public LineDirtyState GetLineDirtyState( int lineIndex )
		{
			if( lineIndex < 0 )
				throw new ArgumentOutOfRangeException( "lineIndex", "Specified line index is out"
													   + " of valid range. (lineIndex:" + lineIndex
													   + ", LineCount:" + LineCount + ")" );

			return (lineIndex < _LDS.Count) ? _LDS[ lineIndex ]
											: LineDirtyState.Clean;
		}
		internal void SetLineDirtyState( int lineIndex, LineDirtyState lds )
		{
			Debug.Assert( 0 <= lineIndex );
			Debug.Assert( lineIndex <= _LDS.Count );

			if( lineIndex < _LDS.Count )
				_LDS[lineIndex] = lds;
		}
		internal void SetLineDirtyStatesToSavedState()
		{
			for( int i=0; i<_LDS.Count; i++ )
				if( _LDS[i] == LineDirtyState.Modified )
					_LDS[i] = LineDirtyState.Saved;
		}
		internal void ClearLineDirtyStates()
		{
			for( int i=0; i<_LDS.Count; i++ )
				_LDS[i] = LineDirtyState.Clean;
		}
		#endregion

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
		public RleArray<uint> Marks
		{
			get{ return _MarkingBitMasks; }
		}
		#endregion

		#region Line / Column
		public TextPoint GetTextPosition( int charIndex )
		{
			Debug.Assert( 0 <= charIndex );
			Debug.Assert( charIndex <= Count );

			return TextUtil.GetTextPosition( _Chars, _LHI, charIndex );
		}

		public int GetCharIndex( TextPoint position )
		{
			Debug.Assert( 0 <= position.Line );
			Debug.Assert( position.Line < LineCount );
			Debug.Assert( 0 <= position.Column );

			return TextUtil.GetCharIndex( _Chars, _LHI, position );
		}

		/// <summary>
		/// Maintain line head indexes for text insertion. MUST BE CALLED BEFORE ACTUAL INSERTION.
		/// </summary>
		void LHI_Insert( string insertText, int insertIndex )
		{
			DebugUtl.Assert( insertText != null, "insertText must not be null." );
			DebugUtl.Assert( 0 <= insertIndex && insertIndex <= _Chars.Count,
							 "insertIndex is out of range (" + insertIndex + ")." );
			TextPoint insPos;
			int lineIndex; // work variable
			int lineHeadIndex;
			int lineEndIndex;
			int insLineCount;

			// At first, find the line which contains the insertion point
			insPos = GetTextPosition( insertIndex );
			lineIndex = insPos.Line;

			// If the inserting divides a CR+LF, insert an entry for the CR separated
			if( 0 < insertIndex && _Chars[insertIndex-1] == '\r'
				&& insertIndex < _Chars.Count && _Chars[insertIndex] == '\n' )
			{
				_LHI.Insert( lineIndex+1, insertIndex );
				_LDS.Insert( lineIndex+1, LineDirtyState.Modified );
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
				_LDS.Insert( lineIndex+insLineCount, LineDirtyState.Modified );
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
				_LDS[insPos.Line-1] = LineDirtyState.Modified;
			}
			else
			{
				_LDS[insPos.Line] = LineDirtyState.Modified;
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
			var delFromPos = GetTextPosition( delBegin );
			var delToPos = GetTextPosition( delEnd );
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
					_LDS.Insert( delToPos.Line, LineDirtyState.Modified );
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
				_LDS[delFirstLine-1] = LineDirtyState.Modified;
			}
			else
			{
				_LDS[delFirstLine] = LineDirtyState.Modified;
			}
		}
		#endregion

		#region Content Access
		public int LineCount
		{
			get{ return _LHI.Count; }
		}

		public Range GetLineRange( int lineIndex, bool includesEolCode )
		{
			Debug.Assert( 0 <= lineIndex );
			Debug.Assert( lineIndex < LineCount );

			return TextUtil.GetLineRange( _Chars, _LHI, lineIndex, includesEolCode );
		}

		public Range GetLineRangeFromCharIndex( int charIndex, bool includesEolCode )
		{
			Debug.Assert( 0 <= charIndex );
			Debug.Assert( charIndex <= _Chars.Count );

			var lineIndex = TextUtil.GetLineIndexFromCharIndex( _LHI, charIndex );
			return TextUtil.GetLineRange( _Chars, _LHI, lineIndex, includesEolCode );
		}

		public string GetText( IRange range )
		{
			Debug.Assert( 0 <= range.Begin );
			Debug.Assert( range.Begin <= range.End );
			Debug.Assert( range.End <= _Chars.Count );

			if( range.IsEmpty )
				return String.Empty;

			// constrain indexes to avoid dividing a grapheme cluster
			TextUtil.ConstrainIndex( _Chars, ref range );

			// retrieve a part of the content
			var buf = new char[range.Length];
			_Chars.CopyTo( range.Begin, range.End, buf );
			return new String( buf );
		}

		public string GetText( int beginLineIndex, int beginColumnIndex, int endLineIndex, int endColumnIndex )
		{
			Debug.Assert( 0 <= endLineIndex );
			Debug.Assert( endLineIndex < LineCount );
			Debug.Assert( 0 <= beginLineIndex );
			Debug.Assert( beginLineIndex <= endLineIndex );
			Debug.Assert( 0 <= endColumnIndex );
			Debug.Assert( 0 <= beginColumnIndex );

			// Return an empty string for an empty range
			if( beginLineIndex == endLineIndex && beginColumnIndex == endColumnIndex )
			{
				return String.Empty;
			}

			// Prepare buffer
			int begin = _LHI[beginLineIndex] + beginColumnIndex;
			int end = _LHI[endLineIndex] + endColumnIndex;
			if( _Chars.Count < end )
			{
				throw new ArgumentOutOfRangeException( "?", "Invalid index was given (calculated end:"+end+", Count:"+Count+")." );
			}
			if( end <= begin )
			{
				throw new ArgumentOutOfRangeException( "?", String.Format("Invalid index was given (calculated range:[{4}, {5}) / beginLineIndex:{0}, beginColumnIndex:{1}, endLineIndex:{2}, endColumnIndex:{3}", beginLineIndex, beginColumnIndex, endLineIndex, endColumnIndex, begin, end) );
			}

			// Copy the substring
			return GetText( new Range(begin, end) );
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
		/// <param name="value">The String to find.</param>
		/// <param name="begin">Begin index of the search range.</param>
		/// <param name="end">End index of the search range.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		public SearchResult FindNext( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", Count:"+Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			return _Chars.FindNext( value, begin, end, matchCase );
		}

		/// <summary>
		/// Finds previous occurrence of a text pattern.
		/// </summary>
		/// <param name="value">The String to find.</param>
		/// <param name="begin">The begin index of the search range.</param>
		/// <param name="end">The end index of the search range.</param>
		/// <param name="matchCase">Whether the search should be case-sensitive or not.</param>
		/// <returns>Search result object if found, otherwise null if not found.</returns>
		public SearchResult FindPrev( string value, int begin, int end, bool matchCase )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin", "parameter begin must be a positive integer. (begin:"+begin+")" );
			if( end < begin )
				throw new ArgumentOutOfRangeException( "end", "parameter end must be greater than parameter begin. (begin:"+begin+", end:"+end+")" );
			if( Count < end )
				throw new ArgumentOutOfRangeException( "end", "end must not be greater than character count. (end:"+end+", Count:"+Count+")" );
			if( value == null )
				throw new ArgumentNullException( "value" );

			return _Chars.FindPrev( value, begin, end, matchCase );
		}

		/// <summary>
		/// Find a text pattern by regular expression.
		/// </summary>
		/// <param name="regex">A Regex object expressing the text pattern.</param>
		/// <param name="begin">The search starting position.</param>
		/// <param name="end">Index of where the search must be terminated</param>
		/// <returns></returns>
		/// <remarks>
		/// This method find a text pattern
		/// expressed by a regular expression in the current content.
		/// The text matching process continues for the index
		/// specified with the <paramref name="end"/> parameter
		/// and does not stop at line ends nor null-characters.
		/// </remarks>
		public SearchResult FindNext( Regex regex, int begin, int end )
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

			return _Chars.FindNext( regex, begin, end );
		}

		public SearchResult FindPrev( Regex regex, int begin, int end )
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

			return _Chars.FindPrev( regex, begin, end );
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

			// Fire events for each living tracking ranges
			var zombies =  new List<int>( _TrackingRanges.Count );
			for( int i=0; i<_TrackingRanges.Count; i++ )
			{
				WeakReference ptr = _TrackingRanges[i];
				if( ptr.IsAlive )
				{
					var range = (TrackingRange)ptr.Target;
					range.OnContentChanged( this,
											new ContentChangedEventArgs(index,
																		oldText,
																		newText) );
				}
				else
				{
					zombies.Add( i );
				}
			}

			// Remove zombie references
			for( int i=zombies.Count-1; 0<=i; i-- )
				_TrackingRanges.RemoveAt( zombies[i] );

			// Fire events for every normal listeners
			if( ContentChanged != null )
				ContentChanged( this, new ContentChangedEventArgs(index, oldText, newText) );
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets when this buffer was edited lastly.
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

		public TrackingRange CreateTrackingRange( int begin, int end, BoundaryTrackingMode mode )
		{
			var range = new TrackingRange( this, begin, end, mode );
			_TrackingRanges.Add( new WeakReference(range) );
			return range;
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
