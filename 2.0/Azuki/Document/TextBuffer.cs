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
		readonly GapBuffer<int> _LHI = new GapBuffer<int>( 64 ); // line head indexes
		readonly RleArray<uint> _MarkingBitMasks;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TextBuffer( int initGapSize, int growSize )
		{
			_Chars = new GapCharBuffer( initGapSize, growSize );
			_Classes = new GapBuffer<CharClass>( initGapSize, growSize );
			_LHI.Add( 0 );
			_MarkingBitMasks = new RleArray<uint>();
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

		public string GetText( Range range )
		{
			Debug.Assert( range != null );
			Debug.Assert( 0 <= range.Begin );
			Debug.Assert( range.Begin <= range.End );
			Debug.Assert( range.End <= _Chars.Count );

			if( range.IsEmpty )
				return String.Empty;

			// constrain indexes to avoid dividing a grapheme cluster
			TextUtil.ConstrainIndex( _Chars, range );

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
			Add( new[]{ch} );
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
			Insert( index, new[]{ch} );
		}

		public void Insert( int index, char[] chars )
		{
			Insert( index, chars, null );
		}

		public void Insert( int index, char[] chars, GapBuffer<LineDirtyState> lds )
		{
			if( lds != null )
				TextUtil.LHI_Insert( _LHI, lds, _Chars, chars, index );

			_Chars.Insert( index, chars );
			_Classes.Insert( index, new CharClass[chars.Length] );
			_MarkingBitMasks.Insert( index, 0, chars.Length );
		}

		public void Insert( int index, string str )
		{
			Insert( index, str.ToCharArray() );
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
		/// Removes elements at specified range [begin, end).
		/// </summary>
		public void Remove( int begin, int end )
		{
			Remove( begin, end, null );
		}

		/// <summary>
		/// Removes elements at specified range [begin, end).
		/// </summary>
		public void Remove( int begin, int end, GapBuffer<LineDirtyState> lds )
		{
			if( lds != null )
				TextUtil.LHI_Delete( _LHI, lds, _Chars, begin, end );

			_Chars.RemoveRange( begin, end );
			_Classes.RemoveRange( begin, end );
			for( int i=begin; i<end; i++ )
			{
				_MarkingBitMasks.RemoveAt( begin );
			}
			Debug.Assert( _Chars.Count == _Classes.Count );
			Debug.Assert( _Chars.Count == _MarkingBitMasks.Count );
		}

		/// <summary>
		/// Deletes all elements.
		/// </summary>
		public void Clear()
		{
			_Chars.Clear();
			_Classes.Clear();
			_MarkingBitMasks.Clear();
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
			return _Chars.FindNext( regex, begin, end );
		}

		public SearchResult FindPrev( Regex regex, int begin, int end )
		{
			return _Chars.FindPrev( regex, begin, end );
		}
		#endregion

		#region Utilities
#		if DEBUG
		/// <summary>
		/// ToString for Debug.
		/// </summary>
		public override string ToString()
		{
			System.Text.StringBuilder buf = new System.Text.StringBuilder( this.Count );
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

		public char this[int index]
		{
			get{ return _Chars[index]; }
			set{ _Chars[index] = value; }
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
