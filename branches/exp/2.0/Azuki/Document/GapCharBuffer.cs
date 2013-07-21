// file: GapBuffer.cs
// brief: Data structure holding a 'gap' in it for efficient insert/delete operation.
//=========================================================
using System;
using System.Text.RegularExpressions;

namespace Sgry.Azuki
{
	/// <summary>
	/// A gap-buffer specialized for char with efficient search logic.
	/// </summary>
	class GapCharBuffer : GapBuffer<char>
	{
		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public GapCharBuffer( int initBufferSize )
			: this( initBufferSize, 0 )
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public GapCharBuffer( int initBufferSize, int growSize )
			: base( initBufferSize, growSize )
		{
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
			// If the gap exists after the search starting position,
			// it must be moved to before the starting position.
			int start, length;
			int foundIndex;
			StringComparison compType;

			DebugUtl.Assert( value != null );
			DebugUtl.Assert( 0 <= begin );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );

			// convert begin/end indexes to start/length indexes
			start = begin;
			length = end - begin;
			if( length <= 0 )
			{
				return null;
			}

			// move the gap if necessary
			if( _GapPos <= begin )
			{
				// the gap exists before search range so the gap is not needed to be moved
				//DO_NOT//MoveGapTo( somewhere );
				start += _GapLen;
			}
			else if( _GapPos < end )
			{
				// the gap exists IN the search range so the gap must be moved
				MoveGapTo( begin );
				start += _GapLen;
			}
			//NO_NEED//else if( end <= _GapPos ) {} // nothing to do in this case

			// find
			compType = (matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
			foundIndex = new String(_Data).IndexOf( value, start, length, compType );
			if( foundIndex == -1 )
			{
				return null;
			}

			// calculate found index not in gapped buffer but in content
			if( _GapPos < end )
			{
				foundIndex -= _GapLen;
			}

			// return found index
			return new SearchResult( foundIndex, foundIndex + value.Length );
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
			// If the gap exists before the search starting position,
			// it must be moved to after the starting position.
			int start, length;
			int foundIndex;
			StringComparison compType;

			DebugUtl.Assert( value != null );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );

			// if empty string is the value to search, just return search start index
			if( value.Length == 0 )
			{
				return new SearchResult( end, end );
			}

			// convert begin/end indexes to start/length indexes
			start = end - 1;
			length = end - begin;
			if( start < 0 || length <= 0 )
			{
				return null;
			}

			// calculate start index in the gapped buffer
			if( _GapPos < begin )
			{
				// the gap exists before search range so the gap is not needed to be moved
				start += _GapLen;
			}
			else if( _GapPos < end )
			{
				// the gap exists in the search range so the gap must be moved
				MoveGapTo( end );
			}
			//NO_NEED//else if( end <= _GapPos ) {} // nothing to do in this case

			// find
			compType = (matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase);
			foundIndex = new String(_Data).LastIndexOf( value, start, length, compType );
			if( foundIndex == -1 )
			{
				return null;
			}

			// calculate found index not in gapped buffer but in content
			if( _GapPos < end )
			{
				foundIndex -= _GapLen;
			}

			// return found index
			return new SearchResult( foundIndex, foundIndex + value.Length );
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
			int start, length;
			Match match;

			DebugUtl.Assert( regex != null );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );

			// in any cases, search length is "end - begin".
			length = end - begin;

			// determine where the gap should be moved to
			if( end <= _GapPos )
			{
				// search must stop before reaching the gap so there is no need to move gap
				start = begin;
			}
			else
			{
				// search may not stop before reaching to the gap
				// so move gap to ensure there is no gap in the search range
				start = begin + _GapLen;
				MoveGapTo( begin );
			}

			// find
			match = regex.Match( new String(_Data), start, length );
			if( match.Success == false )
			{
				return null;
			}

			// return found index
			if( start == begin )
				return new SearchResult( match.Index, match.Index + match.Length );
			else
				return new SearchResult( match.Index - _GapLen, match.Index - _GapLen + match.Length );
		}

		public SearchResult FindPrev( Regex regex, int begin, int end )
		{
			int start, length;
			Match match;

			DebugUtl.Assert( regex != null );
			DebugUtl.Assert( begin <= end );
			DebugUtl.Assert( end <= _Count );
			DebugUtl.Assert( (regex.Options & RegexOptions.RightToLeft) != 0 );

			// convert begin/end indexes to start/length
			length = end - begin;
			if( end <= _GapPos )
			{
				// search must stop before reaching the gap so there is no need to move gap
				start = begin;
			}
			else
			{
				// search may not stop before reaching to the gap
				// so move gap to ensure there is no gap in the search range
				start = begin + _GapLen;
				MoveGapTo( begin );
			}

			// find
			match = regex.Match( new String(_Data), start, length );
			if( match.Success == false )
			{
				return null;
			}

			// return found index
			if( start == begin )
				return new SearchResult( match.Index, match.Index + match.Length );
			else
				return new SearchResult( match.Index - _GapLen, match.Index - _GapLen + match.Length );
		}
		#endregion
	}
}
