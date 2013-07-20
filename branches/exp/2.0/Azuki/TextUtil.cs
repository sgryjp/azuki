// file: TextUtil.cs
// brief: Utility functions to manipulate strings.
//=========================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Utility functions to manipulate strings.
	/// </summary>
	static class TextUtil
	{
		public static readonly char[] EolChars = new char[]{ '\r', '\n' };

		#region Line/Column
		public static int GetCharIndex( IList<char> text,
										IList<int> lhi,
										TextPoint position )
		{
			DebugUtl.Assert( text != null && lhi != null && 0 <= position.Line
							 && 0 <= position.Column, "invalid arguments were given" );
			DebugUtl.Assert( position.Line < lhi.Count, String.Format(
							 "too large line index was given (given:{0} actual"
							 + " line count:{1})", position.Line, lhi.Count) );

			int lineHeadIndex = lhi[position.Line];

#			if DEBUG
			int lineLength = GetLineLengthByCharIndex( text, lineHeadIndex );
			if( lineLength < position.Column )
			{
				if( position.Line == lhi.Count-1
					&& lineLength+1 == position.Column )
				{
					// indicates EOF. this case is valid.
				}
				else
				{
					DebugUtl.Fail( "specified column index was too large"
								   + " (given:"+position.Column+" actual line"
								   + " length:"+lineLength+")" );
				}
			}
#			endif

			return lineHeadIndex + position.Column;
		}

		public static int GetLineIndexFromCharIndex( IList<int> lhi,
													 int charIndex )
		{
			DebugUtl.Assert( 0<=charIndex,"invalid args; given charIndex was "
							 + charIndex );

			int index = BinarySearch( lhi, charIndex );
			return (0 <= index) ? index : ~(index) - 1;
		}

		public static void
			GetLineColumnIndexFromCharIndex( IList<char> text,
											 IList<int> lhi,
											 int charIndex,
											 out int lineIndex,
											 out int columnIndex )
		{
			DebugUtl.Assert( text != null && lhi != null );
			DebugUtl.Assert( 0<=charIndex,"invalid args; given charIndex was "
							 + charIndex );
			DebugUtl.Assert( charIndex <= text.Count, String.Format(
							 "given charIndex was too large (given:{0} "
							 + "actual text count:{1})",charIndex,text.Count));

			int index = BinarySearch( lhi, charIndex );
			lineIndex = ( 0 <= index ) ? index : ~(index) - 1;
			if( lhi.Count <= lineIndex )
				lineIndex = lhi.Count - 1;
			columnIndex = charIndex - lhi[lineIndex];
		}

		public static int GetLineHeadIndexFromCharIndex( IList<char> text,
														 IList<int> lhi,
														 int charIndex )
		{
			DebugUtl.Assert( text != null && lhi != null );
			DebugUtl.Assert( 0 <= charIndex, "invalid arguments were given ("
							 + charIndex + ")" );
			DebugUtl.Assert( charIndex <= text.Count, String.Format(
							 "too large char-index was given (given:{0} actual"
							 + " text count:{1})", charIndex, text.Count) );

			int index = BinarySearch( lhi, charIndex );
			int lineIndex = ( 0 <= index ) ? index : ~(index) - 1;
			if( lhi.Count <= lineIndex )
				lineIndex = lhi.Count - 1;
			return lhi[lineIndex];
		}

		public static Range GetLineRange( IList<char> text,
										 IList<int> lhi,
										 int lineIndex,
										 bool includesEolCode )
		{
			DebugUtl.Assert( text != null );
			DebugUtl.Assert( lhi != null );
			DebugUtl.Assert( 0 <= lineIndex && lineIndex < lhi.Count,
							 "argument out of range; given lineIndex is "
							 + lineIndex + " but lhi.Count is " + lhi.Count );

			Range range = new Range();

			// Get range of the specified line
			range.Begin = lhi[lineIndex];
			if( lineIndex+1 < lhi.Count )
			{
				range.End = lhi[lineIndex + 1];
			}
			else
			{
				range.End = text.Count;
			}
			Debug.Assert( 0 <= range.Begin && range.Begin <= range.End );
			Debug.Assert( range.End <= text.Count );

			// Subtract length of the trailing EOL code
			if( includesEolCode == false )
			{
				int length = range.End - range.Begin;
				if( 1 <= length && text[range.End-1] == '\n' )
				{
					if( 2 <= length && text[range.End-2] == '\r' )
						range.End -= 2;
					else
						range.End--;
				}
				else if( 1 <= length && text[range.End-1] == '\r' )
				{
					range.End--;
				}
			}

			Debug.Assert( range.Begin <= range.End );
			return range;
		}

		public static int GetLineLengthByCharIndex( IList<char> text,
													int charIndex )
		{
			int prevLH = PrevLineHead( text, charIndex );
			int nextLH = NextLineHead( text, charIndex );
			if( nextLH == -1 )
			{
				nextLH = text.Count - 1;
			}

			return (nextLH - prevLH);
		}

		public static int GetLineLengthByCharIndex( string text, int charIndex )
		{
			int prevLH = PrevLineHead( text, charIndex );
			int nextLH = NextLineHead( text, charIndex );
			if( nextLH == -1 )
			{
				nextLH = text.Length - 1;
			}

			return (nextLH - prevLH);
		}

		public static bool IsMultiLine( string text )
		{
			int lineHead = 0;

			lineHead = NextLineHead( text, lineHead );
			if( lineHead != -1 )
			{
				return true;
			}

			return false;
		}

		public static int CountLines( string text )
		{
			int count = 0;
			int lineHead = 0;

			lineHead = NextLineHead( text, lineHead );
			while( lineHead != -1 )
			{
				count++;
				lineHead = NextLineHead( text, lineHead );
			}

			return count + 1;
		}

		public static bool IsEolChar( char ch )
		{
			return (ch == '\r' || ch == '\n');
		}

		public static bool IsEolChar( string str, int index )
		{
			if( 0 <= index && index < str.Length )
			{
				char ch = str[index];
				return (ch == '\r' || ch == '\n');
			}
			else
			{
				return false;
			}
		}

		public static int NextLineHead( IList<char> str, int startIndex )
		{
			DebugUtl.Assert( str != null );
			for( int i=startIndex; i<str.Count; i++ )
			{
				// found EOL code?
				if( str[i] == '\r' )
				{
					if( i+1 < str.Count
						&& str[i+1] == '\n' )
					{
						return i+2;
					}

					return i+1;
				}
				else if( str[i] == '\n' )
				{
					return i+1;
				}
			}

			return -1; // not found
		}

		public static int NextLineHead( string str, int startIndex )
		{
			DebugUtl.Assert( str != null );
			for( int i=startIndex; i<str.Length; i++ )
			{
				// found EOL code?
				if( str[i] == '\r' )
				{
					if( i+1 < str.Length
						&& str[i+1] == '\n' )
					{
						return i+2;
					}

					return i+1;
				}
				else if( str[i] == '\n' )
				{
					return i+1;
				}
			}

			return -1; // not found
		}

		public static int PrevLineHead( IList<char> str, int startIndex )
		{
			DebugUtl.Assert( startIndex <= str.Count,
							 "invalid argument; startIndex is ("
							 +startIndex+" but str.Count is "+str.Count+")" );

			if( str.Count <= startIndex )
			{
				startIndex = str.Count - 1;
			}

			for( int i=startIndex-1; 0<=i; i-- )
			{
				// found EOL code?
				if( str[i] == '\n' )
				{
					return i+1;
				}
				else if( str[i] == '\r' )
				{
					if( i+1 < str.Count
						&& str[i+1] == '\n' )
					{
						continue;
					}
					return i+1;
				}
			}

			return 0;
		}

		public static int PrevLineHead( string str, int startIndex )
		{
			DebugUtl.Assert( startIndex <= str.Length,
							 "invalid argument; startIndex is ("
							 +startIndex+" but str.Length is "+str.Length+")" );

			if( str.Length <= startIndex )
			{
				startIndex = str.Length - 1;
			}

			for( int i=startIndex-1; 0<=i; i-- )
			{
				// found EOL code?
				if( str[i] == '\n' )
				{
					return i+1;
				}
				else if( str[i] == '\r' )
				{
					if( i+1 < str.Length
						&& str[i+1] == '\n' )
					{
						continue;
					}
					return i+1;
				}
			}

			return 0;
		}

		/// <summary>
		/// Find non-EOL char from specified index.
		/// Note that the char at specified index always be skipped.
		/// </summary>
		public static int PrevNonEolChar( IList<char> str, int startIndex )
		{
			for( int i=startIndex-1; 0<=i; i-- )
			{
				if( IsEolChar(str[i]) != true )
				{
					// found non-EOL code
					return i;
				}
			}

			return -1; // not found
		}
		#endregion

		#region LHI_Insert and LHI_Delete
		/// <summary>
		/// Maintain line head indexes for text insertion.
		/// THIS MUST BE CALLED BEFORE ACTUAL INSERTION.
		/// </summary>
		public static void LHI_Insert(
				SplitArray<int> lhi,
				SplitArray<LineDirtyState> lds,
				IList<char> text,
				string insertText, int insertIndex
			)
		{
			DebugUtl.Assert( lhi != null && 0 < lhi.Count && lhi[0] == 0,
							 "lhi must have 0 as a first member." );
			DebugUtl.Assert( lds != null && 0 < lds.Count,
							 "lds must have at one or more items." );
			DebugUtl.Assert( lhi.Count == lds.Count, "lhi.Count(" + lhi.Count
							 + ") is not lds.Count(" + lds.Count + ")" );
			DebugUtl.Assert( String.IsNullOrEmpty(insertText) == false,
							 "insertText must not be null nor empty." );
			DebugUtl.Assert( 0 <= insertIndex && insertIndex <= text.Count,
							 "insertIndex is out of range (" + insertIndex
							 + ")." );
			int insTargetLine, dummy;
			int lineIndex; // work variable
			int lineHeadIndex;
			int lineEndIndex;
			int insLineCount;

			// at first, find the line which contains the insertion point
			GetLineColumnIndexFromCharIndex( text, lhi, insertIndex,
											 out insTargetLine, out dummy );
			lineIndex = insTargetLine;

			// if the inserting divides a CR+LF, insert an entry for the CR
			// separated
			if( 0 < insertIndex && text[insertIndex-1] == '\r'
				&& insertIndex < text.Count && text[insertIndex] == '\n' )
			{
				lhi.Insert( lineIndex+1, insertIndex );
				lds.Insert( lineIndex+1, LineDirtyState.Dirty );
				lineIndex++;
			}

			// if inserted text begins with LF and is inserted just after a CR,
			// remove this CR's entry
			if( 0 < insertIndex && text[insertIndex-1] == '\r'
				&& 0 < insertText.Length && insertText[0] == '\n' )
			{
				lhi.RemoveAt( lineIndex );
				lds.RemoveAt( lineIndex );
				lineIndex--;
			}

			// insert line index entries to LHI
			insLineCount = 1;
			lineHeadIndex = 0;
			do
			{
				// get end index of this line
				lineEndIndex = NextLineHead( insertText, lineHeadIndex ) - 1;
				if( lineEndIndex == -2 ) // == "if NextLineHead returns -1"
				{
					// no more lines following to this line.
					// this is the final line. no need to insert new entry
					break;
				}
				lhi.Insert( lineIndex+insLineCount,insertIndex+lineEndIndex+1);
				lds.Insert( lineIndex+insLineCount, LineDirtyState.Dirty );
				insLineCount++;

				// find next line head
				lineHeadIndex = NextLineHead( insertText, lineHeadIndex );
			}
			while( lineHeadIndex != -1 );

			// If finaly character of the inserted string is CR and if it is
			// inserted just before an LF, remove this CR's entry since it will
			// be a part of a CR+LF
			if( 0 < insertText.Length
				&& insertText[insertText.Length - 1] == '\r'
				&& insertIndex < text.Count
				&& text[insertIndex] == '\n' )
			{
				int lastInsertedLine = lineIndex + insLineCount - 1;
				lhi.RemoveAt( lastInsertedLine );
				lds.RemoveAt( lastInsertedLine );
				lineIndex--;
			}

			// shift all the followings
			for( int i=lineIndex+insLineCount; i<lhi.Count; i++ )
			{
				lhi[i] += insertText.Length;
			}

			// mark the insertion target line as 'dirty'
			if( insertText[0] == '\n'
				&& 0 < insertIndex && text[insertIndex-1] == '\r'
				&& insertIndex < text.Count && text[insertIndex] != '\n' )
			{
				// Inserted text has an LF at beginning and there is a CR (not
				// part of a CR+LF) at insertion point so a new CR+LF is made.
				// Since newly made CR+LF is regarded as part of the line
				// which originally ended with a CR, the line should be marked
				// as modified.
				DebugUtl.Assert( 0 < insTargetLine );
				lds[insTargetLine-1] = LineDirtyState.Dirty;
			}
			else
			{
				lds[insTargetLine] = LineDirtyState.Dirty;
			}
		}
		
		/// <summary>
		/// Maintain line head indexes for text deletion.
		/// THIS MUST BE CALLED BEFORE ACTUAL DELETION.
		/// </summary>
		public static void LHI_Delete(
				SplitArray<int> lhi,
				SplitArray<LineDirtyState> lds,
				IList<char> text,
				int delBegin, int delEnd
			)
		{
			DebugUtl.Assert( lhi != null && 0 < lhi.Count && lhi[0] == 0,
							 "lhi must have 0 as a first member." );
			DebugUtl.Assert( lds != null && 0 < lds.Count, "lds must have one"
							 + " or more items." );
			DebugUtl.Assert( lhi.Count == lds.Count, "lhi.Count(" + lhi.Count
							 + ") is not lds.Count(" + lds.Count + ")" );
			DebugUtl.Assert( 0 <= delBegin && delBegin < text.Count,
							 "delBegin is out of range." );
			DebugUtl.Assert( delBegin <= delEnd && delEnd <= text.Count,
							 "delEnd is out of range." );
			int delFirstLine;
			int delFromL, delToL;
			int dummy;
			int delLen = delEnd - delBegin;

			// calculate line indexes of both ends of the range
			GetLineColumnIndexFromCharIndex( text, lhi, delBegin,
											 out delFromL, out dummy );
			GetLineColumnIndexFromCharIndex( text, lhi, delEnd,
											 out delToL, out dummy );
			delFirstLine = delFromL;

			if( 0 < delBegin && text[delBegin-1] == '\r' )
			{
				if( delEnd < text.Count && text[delEnd] == '\n' )
				{
					// Delete an entry of a line terminated with a CR in case
					// of that the CR will be merged into an CR+LF.
					lhi.RemoveAt( delToL );
					lds.RemoveAt( delToL );
					delToL--;
				}
				else if( text[delBegin] == '\n' )
				{
					// Insert an entry of a line terminated with a CR in case
					// of that an LF was removed from an CR+LF.
					lhi.Insert( delToL, delBegin );
					lds.Insert( delToL, LineDirtyState.Dirty );
					delFromL++;
					delToL++;
				}
			}

			// subtract line head indexes for lines after deletion point
			for( int i=delToL+1; i<lhi.Count; i++ )
			{
				lhi[i] -= delLen;
			}

			// if deletion decreases line count, delete entries
			if( delFromL < delToL )
			{
				lhi.RemoveRange( delFromL+1, delToL+1 );
				lds.RemoveRange( delFromL+1, delToL+1 );
			}

			// mark the deletion target line as 'dirty'
			if( 0 < delBegin && text[delBegin-1] == '\r'
				&& delEnd < text.Count && text[delEnd] == '\n'
				&& 0 < delFirstLine )
			{
				// This deletion combines a CR and an LF.
				// Since newly made CR+LF is regarded as part of the line
				// which originally ended with a CR, the line should be marked
				// as modified.
				lds[delFirstLine-1] = LineDirtyState.Dirty;
			}
			else
			{
				lds[delFirstLine] = LineDirtyState.Dirty;
			}
		}
		#endregion

		#region Unicode character sequence
		/// <summary>
		/// Determines whether given character is a combining character or not.
		/// </summary>
		public static bool IsCombiningCharacter( char ch )
		{
			UnicodeCategory category = Char.GetUnicodeCategory( ch );
			return ( category == UnicodeCategory.NonSpacingMark
					|| category == UnicodeCategory.SpacingCombiningMark
					|| category == UnicodeCategory.EnclosingMark
				);
		}

		/// <summary>
		/// Determines whether given character is a combining character or not.
		/// </summary>
		public static bool IsCombiningCharacter( string text, int index )
		{
			if( index < 0 || text.Length <= index )
				return false;

			return IsCombiningCharacter( text[index] );
		}

		/// <summary>
		/// Determines whether text can not be divided at given index or not.
		/// </summary>
		public static bool IsNotDividableIndex( char prevCh, char ch )
		{
			if( prevCh == '\r' && ch == '\n' )
			{
				return true;
			}
			if( Char.IsHighSurrogate(prevCh) && Char.IsLowSurrogate(ch) )
			{
				return true;
			}
			if( IsCombiningCharacter(ch) && IsEolChar(prevCh) == false )
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Determines whether text can not be divided at given index or not.
		/// </summary>
		/// <param name="text">The text to be examined.</param>
		/// <param name="index">The index to determine whether it points to middle of an undividable character sequence or not.</param>
		/// <remarks>
		///   <para>
		///   This method determines whether a string can not be divided at given index or not.
		///   This is only an utility method.
		///   Please refer to the document of
		///   <see cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex instance method</see>
		///   for detail.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex method</seealso>
		public static bool IsNotDividableIndex( string text, int index )
		{
			if( text == null || index <= 0 || text.Length <= index )
				return false;

			return IsNotDividableIndex( text[index-1], text[index] );
		}

		/// <summary>
		/// Determines whether text can not be divided at given index or not.
		/// </summary>
		/// <param name="text">The text to be examined.</param>
		/// <param name="index">The index to determine whether it points to middle of an undividable character sequence or not.</param>
		/// <remarks>
		///   <para>
		///   This method determines whether a string can not be divided at given index or not.
		///   This is only an utility method.
		///   Please refer to the document of
		///   <see cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex instance method</see>
		///   for detail.
		///   </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.IsNotDividableIndex(int)">Document.IsNotDividableIndex method</seealso>
		public static bool IsNotDividableIndex( IList<char> text, int index )
		{
			if( text == null || index <= 0 || text.Count <= index )
				return false;

			return IsNotDividableIndex( text[index-1], text[index] );
		}

		public static void ConstrainIndex( IList<char> text, Range range )
		{
			if( range.IsEmpty == false )
			{
				Debug.Assert( range.Begin < range.End );
				while( IsNotDividableIndex(text, range.Begin) )
					range.Begin--;
				while( IsNotDividableIndex(text, range.End) )
					range.End++;
			}
			else
			{
				while( IsNotDividableIndex(text, range.Begin) )
				{
					range.Begin--;
					range.End--;
				}
			}
		}

		public static void ConstrainIndex( IList<char> text, ref int anchor, ref int caret )
		{
			if( anchor < caret )
			{
				while( IsNotDividableIndex(text, anchor) )
					anchor--;
				while( IsNotDividableIndex(text, caret) )
					caret++;
			}
			else if( caret < anchor )
			{
				while( IsNotDividableIndex(text, caret) )
					caret--;
				while( IsNotDividableIndex(text, anchor) )
					anchor++;
			}
			else// if( anchor == caret )
			{
				while( IsNotDividableIndex(text, caret) )
				{
					anchor--;
					caret--;
				}
			}
		}

		public static int NextGraphemeClusterIndex( IList<char> text, int index )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 <= index );
			Debug.Assert( index < text.Count );

			do
			{
				index++;
			}
			while( index < text.Count && IsNotDividableIndex(text, index) );

			return index;
		}

		public static int PrevGraphemeClusterIndex( IList<char> text, int index )
		{
			Debug.Assert( text != null );
			Debug.Assert( 0 <= index );
			Debug.Assert( index <= text.Count );

			do
			{
				index--;
			}
			while( 0 < index && IsNotDividableIndex(text, index) );

			return index;
		}
		#endregion

		#region Others
		public static int BinarySearch<T>( IList<T> list, T item )
		{
			return BinarySearch( list, item, Comparer<T>.Default.Compare );
		}

		public static int BinarySearch<T>( IList<T> list, T item, Comparison<T> compare )
		{
			Debug.Assert( compare != null );

			if( list.Count == 0 )
				return ~(0);

			int left = 0;
			int right = list.Count;
			int middle;
			for(;;)
			{
				middle = left + ( (right - left) >> 1 );
				int result = compare( list[middle], item );
				if( 0 < result )
				{
					if( right == middle )
						return ~(middle);
					right = middle;
				}
				else if( result < 0 )
				{
					if( left == middle )
						return ~(middle + 1);
					left = middle;
				}
				else
				{
					return middle;
				}
			}
		}
		#endregion
	}
}
