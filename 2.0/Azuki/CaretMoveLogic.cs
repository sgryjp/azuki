// file: CaretMoveLogic.cs
// brief: Implementation of caret movement.
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	static class CaretMoveLogic
	{
		#region Public interface
		public delegate int CalcMethod( IViewInternal view );

		/// <summary>
		/// Moves caret to the index where the specified method calculates.
		/// </summary>
		public static void MoveCaret( CalcMethod calculator, IUserInterface ui )
		{
			var doc = ui.Document;
			var view = (IViewInternal)ui.View;

			int nextIndex = calculator( view );
			if( nextIndex == doc.CaretIndex )
			{
				// notify that the caret not moved
				Plat.Inst.MessageBeep();
			}
			else
			{
				// set new selection and scroll to caret
				doc.SetSelection( nextIndex, nextIndex );
				ui.SelectionMode = TextDataType.Normal;
			}
			view.ScrollToCaret();
		}

		/// <summary>
		/// Expand selection to the index where the specified method calculates
		/// (selection anchor will not be changed).
		/// </summary>
		public static void SelectTo( CalcMethod calculator, IUserInterface ui )
		{
			var doc = ui.Document;
			var view = (IViewInternal)ui.View;
			int nextIndex;

			// calculate where to expand selection
			nextIndex = calculator( view );
			if( nextIndex == doc.CaretIndex )
			{
				// notify that the caret not moved
				Plat.Inst.MessageBeep();
			}

			// set new selection
			doc.SetSelection( doc.AnchorIndex, nextIndex, view );
			view.ScrollToCaret();
		}
		#endregion

		#region Index Calculation
		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "right" key.
		/// </summary>
		public static int Calc_Right( IView view )
		{
			var doc = view.Document;
			if( doc.Length < doc.CaretIndex+1 )
			{
				return doc.Length;
			}

			// Avoid placing caret at middle of an undividable character sequences.
			int newCaretIndex = doc.CaretIndex + 1;
			while( doc.IsNotDividableIndex(newCaretIndex) )
			{
				newCaretIndex++;
			}

			return newCaretIndex;
		}

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "left" key.
		/// </summary>
		public static int Calc_Left( IView view )
		{
			var doc = view.Document;
			if( doc.CaretIndex-1 < 0 )
			{
				return 0;
			}

			// Avoid placing caret at middle of an undividable character sequences.
			int newCaretIndex = doc.CaretIndex - 1;
			while( doc.IsNotDividableIndex(newCaretIndex) )
			{
				newCaretIndex--;
			}

			return newCaretIndex;
		}

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "down" key.
		/// </summary>
		public static int Calc_Down( IViewInternal view )
		{
			var doc = view.Document;

			// Get screen location of the caret
			var pt = view.GetVirtualPos( doc.CaretIndex );

			// Calculate next location
			pt.X = view.GetDesiredColumn();
			pt.Y += view.LineSpacing;
			/* because View.GetCharIndex handles this case.
			if( view.VisibleSize.Height - view.LineSpacing < pt.Y )
				return doc.CaretIndex; // No lines' below. Don't move.
			*/
			var newIndex = view.GetCharIndex( pt );

			// In line selection mode, moving caret across the line containing the anchor position
			// should select the line and a line below. To select a line below, calculate index of
			// the char at one more line below.
			if( doc.SelectionMode == TextDataType.Line
				&& view.IsLineHead(newIndex) )
			{
				var pt2 = new Point( pt.X, pt.Y+view.LineSpacing );
				int skippedNewIndex = view.GetCharIndex( pt2 );
				if( skippedNewIndex == doc.AnchorIndex )
					newIndex = skippedNewIndex;
			}

			return newIndex;
		}

		/// <summary>
		/// Calculate index of the location
		/// where the caret should move to after pressing "up" key.
		/// </summary>
		public static int Calc_Up( IViewInternal view )
		{
			Point pt;
			int newIndex;
			var doc = view.Document;

			// Get screen location of the caret
			pt = view.GetVirtualPos( doc.CaretIndex );

			// Calculate next location
			pt.X = view.GetDesiredColumn();
			pt.Y -= view.LineSpacing;
			newIndex = view.GetCharIndex( pt );
			if( newIndex < 0 )
			{
				return doc.CaretIndex; // Don't move
			}

			// In line selection mode, moving caret across the line containing the anchor position
			// should select the line and a line above. To select a line above, calculate index of
			// the char at one more line above.
			if( doc.SelectionMode == TextDataType.Line
				&& newIndex == doc.AnchorIndex
				&& view.IsLineHead(newIndex) )
			{
				pt.Y -= view.LineSpacing;
				if( 0 <= pt.Y )
				{
					newIndex = view.GetCharIndex( pt );
				}
			}

			return newIndex;
		}

		/// <summary>
		/// Calculate index of the next word.
		/// </summary>
		public static int Calc_NextWord( IView view )
		{
			int index;
			var doc = view.Document;

			// Stop just after an EOL code
			if( Utl.IsEol(doc, doc.CaretIndex) )
				return Utl.SkipOneEol( doc, doc.CaretIndex );

			// Stay in valid range
			index = doc.CaretIndex + 1;
			if( doc.Length <= index )
				return doc.Length;

			// Seek to next word starting position
			index = doc.WordProc.NextWordStart( doc, index );

			// Skip trailling whitespace
			if( Utl.IsWhiteSpace(doc, index) )
				index = doc.WordProc.NextWordStart( doc, index+1 );

			return index;
		}

		/// <summary>
		/// Calculate index of the previous word.
		/// </summary>
		public static int Calc_PrevWord( IView view )
		{
			int index;
			int startIndex;
			var doc = view.Document;

			// Stay in valid range
			index = doc.CaretIndex - 1;
			if( index <= 0 )
				return 0;

			// Skip whitespaces
			startIndex = index;
			if( Utl.IsWhiteSpace(doc, index) )
			{
				index = doc.WordProc.PrevWordStart( doc, index ) - 1;
				if( index < 0 )
					return 0;
			}
			DebugUtl.Assert( 0 <= index && index <= doc.Length );

			// Stop just before an EOL code
			if( Utl.IsEol(doc, index) )
			{
				if( startIndex != index )
				{
					// Do not skip this EOL code if this was detected after skipping whitespaces
					return index + 1;
				}
				else if( doc[index] == '\r' )
				{
					return index;
				}
				else
				{
					DebugUtl.Assert( doc[index] == '\n' );
					if( 0 <= index-1 && doc[index-1] == '\r' )
						return index-1;
					else
						return index;
				}
			}

			// Seek to previous word starting position
			index = doc.WordProc.PrevWordStart( doc, index );

			return index;
		}

		/// <summary>
		/// Calculate index of the first char of the line where caret is at.
		/// </summary>
		public static int Calc_LineHead( IView view )
		{
			return view.Lines.AtOffset( view.Document.CaretIndex ).Begin;
		}

		/// <summary>
		/// Calculate index of the first non-whitespace char of the line where caret is at.
		/// </summary>
		public static int Calc_LineHeadSmart( IView view )
		{
			var doc = view.Document;

			int lineHeadIndex = view.Lines.AtOffset( doc.CaretIndex ).Begin;
			int firstNonSpaceIndex = lineHeadIndex;
			while( firstNonSpaceIndex < doc.Length
				&& Utl.IsWhiteSpace(doc, firstNonSpaceIndex) )
			{
				firstNonSpaceIndex++;
			}

			return (firstNonSpaceIndex == doc.CaretIndex) ? lineHeadIndex
														  : firstNonSpaceIndex;
		}

		/// <summary>
		/// Calculate index of the end location of the line where caret is at.
		/// </summary>
		public static int Calc_LineEnd( IView view )
		{
			var doc = view.Document;
			int offset = -1;

			var pos = view.GetTextPosition( doc.CaretIndex );
			if( view.Lines.Count <= pos.Line+1 )
				return doc.Length;

			int nextIndex = view.GetCharIndex( new TextPoint(pos.Line+1, 0) );
			if( 0 <= nextIndex-1 && doc[nextIndex-1] == '\n'
				&& 0 <= nextIndex-2 && doc[nextIndex-2] == '\r' )
			{
				offset = -2;
			}

			return nextIndex + offset;
		}

		/// <summary>
		/// Calculate first index of the file.
		/// </summary>
		public static int Calc_FileHead( IView view )
		{
			return 0;
		}

		/// <summary>
		/// Calculate end index of the file.
		/// </summary>
		public static int Calc_FileEnd( IView view )
		{
			return view.Document.Length;
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static bool IsWhiteSpace( Document doc, int index )
			{
				if( doc.Length <= index )
					return false;

				return (doc[index] == ' '
						|| doc[index] == '\t'
						|| doc[index] == '\x3000');
			}

			public static bool IsEol( Document doc, int index )
			{
				if( doc.Length <= index )
					return false;

				return (doc[index] == '\r' || doc[index] == '\n');
			}

			public static int SkipOneEol( Document doc, int startIndex )
			{
				int index = startIndex;
				char ch;
				
				ch = doc[index];
				if( ch == 0x0d ) // CR?
				{
					index++;
					if( doc.Length <= index )
						return doc.Length;
					
					ch = doc[index];
					if( ch == 0x0a ) // CR+LF?
					{
						index++;
						if( doc.Length <= index )
							return doc.Length;
					}
				}
				else if( ch == 0x0a ) // LF?
				{
					index++;
					if( doc.Length <= index )
						return doc.Length;
				}

				return index;
			}
		}
		#endregion
	}
}
