// file: GapBuffer.cs
// brief: Data structure holding a 'gap' in it for efficient insert/delete operation.
//=========================================================
using System;
using System.Diagnostics;
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
		{}
		#endregion

		#region Edit
		public void Insert( int insertIndex, string str )
		{
			// [case 1: Insert(1, "foobar", 0, 4)]
			// ABCDE___FGHI     (gappos:5, gaplen:3)
			// ABCDEFGHI___     (gappos:9, gaplen:3)
			// ABCDEFGHI_______ (gappos:9, gaplen:7)
			// A_______BCDEFGHI (gappos:1, gaplen:7)
			// Afoob___BCDEFGHI (gappos:5, gaplen:3)
			DebugUtl.Assert( 0 <= insertIndex, "Invalid index was given (insertIndex:"+insertIndex+")." );
			DebugUtl.Assert( str != null, "Null was given to 'str'." );

			var insertLen = str.Length;

			// Ensure there'll be sufficient size of gap at insertion point
			EnsureSpaceForInsertion( insertLen );
			MoveGapTo( insertIndex );

			// Insert data
			for( int i=0; i<str.Length; i++ )
			{
				_Data[ insertIndex + i ] = str[i];
			}

			// Update states
			_Count += insertLen;
			_GapPos += insertLen;
			_GapLen -= insertLen;
		}
		#endregion

		#region Text Search
		public void FindNext( string value,
							  int begin, int end, bool matchCase,
							  out int foundBegin, out int foundEnd )
		{
			Debug.Assert( value != null );
			Debug.Assert( 0 <= begin );
			Debug.Assert( begin <= end );
			Debug.Assert( end <= _Count );

			// If the gap exists after the search starting position,
			// it must be moved to before the starting position.
			int start, length;
			int foundIndex;
			StringComparison compType;

			// convert begin/end indexes to start/length indexes
			start = begin;
			length = end - begin;
			if( length <= 0 )
			{
				foundBegin = foundEnd = -1;
				return;
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
				foundBegin = foundEnd = -1;
				return;
			}

			// calculate found index not in gapped buffer but in content
			if( _GapPos < end )
			{
				foundIndex -= _GapLen;
			}

			// return found index
			foundBegin = foundIndex;
			foundEnd = foundIndex + value.Length;
		}

		public void FindPrev( string value,
							  int begin, int end, bool matchCase,
							  out int foundBegin, out int foundEnd )
		{
			Debug.Assert( value != null );
			Debug.Assert( 0 <= begin );
			Debug.Assert( begin <= end );
			Debug.Assert( end <= _Count );

			// If the gap exists before the search starting position,
			// it must be moved to after the starting position.
			int start, length;
			int foundIndex;
			StringComparison compType;

			// if empty string is the value to search, just return search start index
			if( value.Length == 0 )
			{
				foundBegin = foundEnd = end;
				return;
			}

			// convert begin/end indexes to start/length indexes
			start = end - 1;
			length = end - begin;
			if( start < 0 || length <= 0 )
			{
				foundBegin = foundEnd = -1;
				return;
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
				foundBegin = foundEnd = -1;
				return;
			}

			// calculate found index not in gapped buffer but in content
			if( _GapPos < end )
			{
				foundIndex -= _GapLen;
			}

			// return found index
			foundBegin = foundIndex;
			foundEnd = foundIndex + value.Length;
		}

		public void FindNext( Regex regex,
							  int begin, int end,
							  out int foundBegin, out int foundEnd )
		{
			Debug.Assert( regex != null );
			Debug.Assert( 0 <= begin );
			Debug.Assert( begin <= end );
			Debug.Assert( end <= _Count );

			int start, length;
			Match match;

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

			// do search
			match = regex.Match( new String(_Data), start, length );
			if( match.Success == false )
			{
				foundBegin = foundEnd = -1;
				return;
			}

			// return found index
			if( start == begin )
			{
				foundBegin = match.Index;
				foundEnd = match.Index + match.Length;
			}
			else
			{
				foundBegin = match.Index - _GapLen;
				foundEnd = match.Index - _GapLen + match.Length;
			}
		}

		public void FindPrev( Regex regex,
							  int begin, int end,
							  out int foundBegin, out int foundEnd )
		{
			Debug.Assert( regex != null );
			Debug.Assert( 0 <= begin );
			Debug.Assert( begin <= end );
			Debug.Assert( end <= _Count );
			Debug.Assert( (regex.Options & RegexOptions.RightToLeft) != 0 );

			int start, length;
			Match match;

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

			// do search
			match = regex.Match( new String(_Data), start, length );
			if( match.Success == false )
			{
				foundBegin = foundEnd = -1;
				return;
			}

			// return found index
			if( start == begin )
			{
				foundBegin = match.Index;
				foundEnd = match.Index + match.Length;
			}
			else
			{
				foundBegin  = match.Index - _GapLen;
				foundEnd = match.Index - _GapLen + match.Length;
			}
		}
		#endregion
	}
}
