// file: CaretMoveLogic.cs
// brief: Implementation of caret movement.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-09-09
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	internal static class CaretMoveLogic
	{
		#region Public interface
		public delegate int CalcMethod( View view );

		/// <summary>
		/// Moves caret to the index where the specified method calculates.
		/// </summary>
		public static void MoveCaret( CalcMethod calculater, View view )
		{
			Document doc = view.Document;
			int nextIndex = calculater( view );
			if( nextIndex == doc.CaretIndex )
			{
				// notify that the caret not moved
				Plat.Inst.MessageBeep();
			}
			else
			{
				// set new selection and scroll to caret
				doc.SetSelection( nextIndex, nextIndex );
				view.ScrollToCaret();
			}
		}

		/// <summary>
		/// Expand selection to the index where the specified method calculates
		/// (selection anchor will not changed).
		/// </summary>
		public static void SelectTo( CalcMethod calculater, View view )
		{
			Document doc = view.Document;
			int nextIndex = calculater( view );
			if( nextIndex == doc.CaretIndex )
			{
				// notify that the caret not moved
				Plat.Inst.MessageBeep();
			}
			else
			{
				// set new selection and scroll to caret
				doc.SetSelection( doc.AnchorIndex, nextIndex );
				view.ScrollToCaret();
			}
		}
		#endregion

		#region Index Calculation
		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "right" key.
		/// </summary>
		public static CalcMethod Calc_Right
			= delegate( View view )
		{
			Document doc = view.Document;
			if( doc.Length < doc.CaretIndex+1 )
			{
				return doc.Length;
			}
			
			int offset = 1;
			int caret = doc.CaretIndex;

			// if a CR-LF or a surrogate pair is the next, move 2 chars forward
			if( caret+2 <= doc.Length )
			{
				string nextTwoChars = doc.GetTextInRange( caret, caret+2 );
				if( nextTwoChars == "\r\n"
					|| Document.IsHighSurrogate(nextTwoChars[0]) )
				{
					offset = 2;
				}
			}

			return doc.CaretIndex + offset;
		};

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "left" key.
		/// </summary>
		public static CalcMethod Calc_Left
			= delegate( View view )
		{
			Document doc = view.Document;
			if( doc.CaretIndex-1 < 0 )
			{
				return 0;
			}
			
			int offset = 1;
			int caret = doc.CaretIndex;

			// only when the CRLF is at previous pos, move 2 chars backward
			if( 0 <= caret-2
				&& doc.GetTextInRange(caret-2, caret) == "\r\n" )
			{
				offset = 2;
			}

			return doc.CaretIndex - offset;
		};

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "down" key.
		/// </summary>
		public static CalcMethod Calc_Down
			= delegate( View view )
		{
			Point pt;
			int newIndex;
			Document doc = view.Document;

			// get screen location of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );

			// calculate next location
			pt.X = view.GetDesiredColumn();
			pt.Y += view.LineSpacing;
			/* NOT NEEDED because View.GetIndexFromVirPos handles this case.
			if( view.Height - view.LineSpacing < pt.Y )
			{
				return doc.CaretIndex; // no lines below. don't move.
			}*/
			newIndex = view.GetIndexFromVirPos( pt );

			return newIndex;
		};

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "up" key.
		/// </summary>
		public static CalcMethod Calc_Up
			= delegate( View view )
		{
			Point pt;
			int newIndex;
			Document doc = view.Document;

			// get screen location of the caret
			pt = view.GetVirPosFromIndex( doc.CaretIndex );

			// calculate next location
			pt.X = view.GetDesiredColumn();
			pt.Y -= view.LineSpacing;
			newIndex = view.GetIndexFromVirPos( pt );
			if( newIndex < 0 )
			{
				return doc.CaretIndex; // don't move
			}

			return newIndex;
		};

		/// <summary>
		/// Calculate index of the next word.
		/// </summary>
		public static CalcMethod Calc_NextWord
			= delegate( View view )
		{
			Document doc = view.Document;
			if( doc.Length < doc.CaretIndex+1 )
			{
				return doc.Length;
			}

			return WordLogic.NextWordStartForMove( doc.InternalBuffer, doc.CaretIndex );
		};

		/// <summary>
		/// Calculate index of the previous word.
		/// </summary>
		public static CalcMethod Calc_PrevWord
			= delegate( View view )
		{
			Document doc = view.Document;
			if( doc.CaretIndex <= 1 )
			{
				return 0;
			}

			return WordLogic.PrevWordStartForMove( doc, doc.CaretIndex );
		};

		/// <summary>
		/// Calculate index of the first char of the line where caret is at.
		/// </summary>
		public static CalcMethod Calc_LineHead
			= delegate( View view )
		{
			return view.GetLineHeadIndexFromCharIndex(
					view.Document.CaretIndex
				);
		};

		/// <summary>
		/// Calculate index of the first non-whitespace char of the line where caret is at.
		/// </summary>
		public static CalcMethod Calc_LineHeadSmart
			= delegate( View view )
		{
			int lineHeadIndex, firstNonSpaceIndex;
			Document doc = view.Document;

			lineHeadIndex = view.GetLineHeadIndexFromCharIndex( doc.CaretIndex );

			firstNonSpaceIndex = lineHeadIndex;
			while( firstNonSpaceIndex < doc.Length
				&& Char.IsWhiteSpace(doc[firstNonSpaceIndex]) )
			{
				firstNonSpaceIndex++;
			}

			return (firstNonSpaceIndex == doc.CaretIndex) ? lineHeadIndex : firstNonSpaceIndex;
		};

		/// <summary>
		/// Calculate index of the end location of the line where caret is at.
		/// </summary>
		public static CalcMethod Calc_LineEnd
			= delegate( View view )
		{
			Document doc = view.Document;
			int line, column;
			int offset = -1;

			view.GetLineColumnIndexFromCharIndex( doc.CaretIndex, out line, out column );
			if( view.LineCount <= line+1 )
			{
				return doc.Length;
			}

			int nextIndex = view.GetCharIndexFromLineColumnIndex( line+1, 0 );
			if( 0 <= nextIndex-1 && doc.GetCharAt(nextIndex-1) == '\n'
				&& 0 <= nextIndex-2 && doc.GetCharAt(nextIndex-2) == '\r' )
			{
				offset = -2;
			}

			return nextIndex + offset;
		};

		/// <summary>
		/// Calculate first index of the file.
		/// </summary>
		public static CalcMethod Calc_FileHead
			= delegate( View view )
		{
			return 0;
		};

		/// <summary>
		/// Calculate end index of the file.
		/// </summary>
		public static CalcMethod Calc_FileEnd
			= delegate( View view )
		{
			return view.Document.Length;
		};
		#endregion
	}
}
