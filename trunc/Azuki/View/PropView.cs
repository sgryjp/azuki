﻿// file: PropWrapView.cs
// brief: Platform independent view (propotional).
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-08-03
//=========================================================
//#define DRAW_SLOWLY
using System;
using System.Drawing;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view implementation to display text with propotional font.
	/// </summary>
	class PropView : View
	{
		int _PrevCaretLine, _PrevCaretColumn, _PrevAnchorLine, _PrevAnchorColumn;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal PropView( IUserInterface ui )
			: base( ui )
		{
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		internal PropView( View other )
			: base( other )
		{
			// release selection
			// (because changing view while keeping selection makes
			// pretty difficult problem around invalidation,
			// force to release selection here)
			if( Document != null )
			{
				Document.SetSelection( Document.CaretIndex, Document.CaretIndex );
			}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets number of the physical lines.
		/// </summary>
		public override int LineCount
		{
			get{ return base.Document.LineCount; }
		}
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		public override Point GetVirPosFromIndex( int index )
		{
			int line, column;
			Document.GetLineColumnIndexFromCharIndex( index, out line, out column );
			return GetVirPosFromIndex( line, column );
		}

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		public override Point GetVirPosFromIndex( int lineIndex, int columnIndex )
		{
			if( lineIndex < 0 || columnIndex < 0 )
				throw new ArgumentException( "invalid index was given (minus value)", String.Format("lineIndex:{0} columnIndex:{1}", lineIndex, columnIndex) );

			Point pos = new Point( 0, LineSpacing*lineIndex ); // init value is for when the columnIndex is 0

			// if the location is not the head of the line, calculate x-coord.
			if( 0 < columnIndex )
			{
				// get partial content of the line which exists before the caret
				string leftPart = Document.GetTextInRange( lineIndex, 0, lineIndex, columnIndex );

				// measure the characters
				pos.X = MeasureTokenEndX( leftPart, pos.X );
			}

			return pos;
		}

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>the index of the char or -1 if invalid point was specified.</returns>
		public override int GetIndexFromVirPos( Point pt )
		{
			int lineIndex, columnIndex;
			int drawableTextLen;

			// calc line index
			lineIndex = (pt.Y / LineSpacing);
			if( lineIndex < 0 )
			{
				return -1;
			}
			else if( Document.LineCount <= lineIndex
				&& Document.LineCount != 0 )
			{
				// the point indicates beyond the final line.
				// treat as if the final line was specified
				lineIndex = Document.LineCount - 1;
			}

			// calc column index
			columnIndex = 0;
			if( 0 < pt.X )
			{
				// get content of the line
				string line = Document.GetLineContent( lineIndex );

				// calc maximum length of chars in line
				int rightLimitX = pt.X;
				int leftPartWidth = MeasureTokenEndX( line, 0, rightLimitX, out drawableTextLen );
				columnIndex = drawableTextLen;

				// if the location is nearer to the NEXT of that char,
				// we should return the index of next one.
				if( drawableTextLen < line.Length )
				{
					string nextChar = line[drawableTextLen].ToString();
					int nextCharWidth = MeasureTokenEndX( nextChar, leftPartWidth ) - leftPartWidth;
					if( leftPartWidth + nextCharWidth/2 < pt.X ) // == "x of middle of next char" < "x of click in virtual text area"
					{
						columnIndex = drawableTextLen + 1;
					}
				}
			}

			return Document.GetCharIndexFromLineColumnIndex( lineIndex, columnIndex );
		}

		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		public override int GetLineHeadIndex( int lineIndex )
		{
			return Document.GetLineHeadIndex( lineIndex );
		}

		/// <summary>
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public override int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			return Document.GetLineHeadIndexFromCharIndex( charIndex );
		}

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public override void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex )
		{
			Document.GetLineColumnIndexFromCharIndex( charIndex, out lineIndex, out columnIndex );
		}

		/// <summary>
		/// Calculates char-index from physical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public override int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex )
		{
			return Document.GetCharIndexFromLineColumnIndex( lineIndex, columnIndex );
		}
		#endregion

		#region Appearance Invalidating and Updating
		protected override void Doc_SelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			Document doc = Document;
			int anchor = doc.AnchorIndex;
			int caret = doc.CaretIndex;
			int anchorLine, anchorColumn;
			int caretLine, caretColumn;

			// calculate line/column index of current anchor/caret
			GetLineColumnIndexFromCharIndex( anchor, out anchorLine, out anchorColumn );
			GetLineColumnIndexFromCharIndex( caret, out caretLine, out caretColumn );

			// if there was no selection and is no selection too,
			// update current line highlight if enabled.
			if( e.OldAnchor == e.OldCaret
				&& anchor == caret )
			{
				if( HighlightsCurrentLine
					&& _PrevCaretLine != caretLine )
				{
					Doc_SelectionChanged_UpdateCaretHighlight( _PrevCaretLine, caretLine );
				}
			}
			// or, does the change release selection?
			else if( e.OldAnchor != e.OldCaret
				&& anchor == caret )
			{
				Doc_SelectionChanged_OnReleaseSel( e );
			}
			// then, the change expands selection.
			else
			{
				// if this is the beginning of selection, remove current line heighlight (underline)
				if( HighlightsCurrentLine && e.OldCaret == e.OldAnchor )
				{
					int y = GetVirPosFromIndex( e.OldCaret ).Y - (FirstVisibleLine * LineSpacing);
					Invalidate(
							new Rectangle(TextAreaX, y+LineHeight, VisibleSize.Width-TextAreaX, 1)
						);
				}

				// if the change occured in a line?
				if( _PrevCaretLine == caretLine )
				{
					if( e.OldCaret < caret )
    					Doc_SelectionChanged_OnExpandSelInLine( e.OldCaret, caret, _PrevCaretLine );
					else
    					Doc_SelectionChanged_OnExpandSelInLine( caret, e.OldCaret, caretLine );
				}
				else
				{
					Doc_SelectionChanged_OnExpandSel( e, caretLine, caretColumn );
				}
			}

			// remember last selection for next invalidation
			_PrevCaretLine = caretLine;
			_PrevCaretColumn = caretColumn;
			_PrevAnchorLine = anchorLine;
			_PrevAnchorColumn = anchorColumn;
		}

		void Doc_SelectionChanged_UpdateCaretHighlight( int oldCaretLine, int newCaretLine )
		{
			// invalidate old underline
			if( _PrevCaretLine == _PrevAnchorLine )
			{
				int y = LineSpacing * (_PrevCaretLine - FirstVisibleLine);
				Invalidate(
						new Rectangle(TextAreaX, y+LineHeight, VisibleSize.Width-TextAreaX, 1)
					);
			}
			
			// draw new underline
			int newCaretY = LineSpacing * (newCaretLine - FirstVisibleLine);
			DrawUnderLine( newCaretY, ColorScheme.HighlightColor );
		}

		void Doc_SelectionChanged_OnExpandSelInLine( int begin, int end, int beginL )
		{
			Rectangle rect = new Rectangle();
			int beginLineHead;
			string token = String.Empty;
			DebugUtl.Assert( beginL < LineCount );

            // get chars at left of invalid rect
			beginLineHead = GetLineHeadIndex( beginL );
            if( beginLineHead < begin )
            {
			    token = Document.GetTextInRange( beginLineHead, begin );
            }
			
			// calculate invalid rect
			rect.X = MeasureTokenEndX( token, 0 );
			rect.Y = LineSpacing * beginL - (FirstVisibleLine * LineSpacing);
			token = Document.GetTextInRange( beginLineHead, end );
			rect.Width = MeasureTokenEndX( token, 0 ) - rect.X;
			rect.Height = LineSpacing;

			// invalidate
			rect.X -= (ScrollPosX - TextAreaX);
			Invalidate( rect );
		}

		void Doc_SelectionChanged_OnExpandSel( SelectionChangedEventArgs e, int caretLine, int caretColumn )
		{
			Document doc = this.Document;
			Rectangle upper, middle, lower;
			int begin, beginL, beginC;
			int end, endL, endC;
			int beginLineHead, endLineHead;

			// get range between old caret and current caret
			if( e.OldCaret < doc.CaretIndex )
			{
				begin = e.OldCaret;
				beginL = _PrevCaretLine;
				beginC = _PrevCaretColumn;
				end = doc.CaretIndex;
				endL = caretLine;
				endC = caretColumn;
			}
			else
			{
				begin = doc.CaretIndex;
				beginL = caretLine;
				beginC = caretColumn;
				end = e.OldCaret;
				endL = _PrevCaretLine;
				endC = _PrevCaretColumn;
			}
			beginLineHead = GetLineHeadIndex( beginL );
			endLineHead = GetLineHeadIndex( endL ); // if old caret is the end pos and if the pos exceeds current text length, this will fail.

			// calculate upper part of the invalid area
			String firstLinePart = doc.GetTextInRange( beginLineHead, begin );
			upper = new Rectangle();
			upper.X = MeasureTokenEndX( firstLinePart, 0 ) - (ScrollPosX - TextAreaX);
			upper.Y = LineSpacing * beginL - (FirstVisibleLine * LineSpacing);
			upper.Width = VisibleSize.Width - upper.X;
			upper.Height = LineSpacing;

			// calculate lower part of the invalid area
			String finalLinePart = doc.GetTextInRange( endLineHead, end );
			lower = new Rectangle();
			lower.X = TextAreaX;
			lower.Y = LineSpacing * endL - (FirstVisibleLine * LineSpacing);
			lower.Width = MeasureTokenEndX( finalLinePart, 0 ) - ScrollPosX;
			lower.Height = LineSpacing;

			// calculate middle part of the invalid area
			middle = new Rectangle();
			middle.X = TextAreaX;
			middle.Y = LineSpacing * (beginL + 1) - (FirstVisibleLine * LineSpacing);
			middle.Width = VisibleSize.Width;
			middle.Height = lower.Y - middle.Y;

			// invalidate three rectangles
			Invalidate( upper );
			if( 0 < middle.Height )
				Invalidate( middle );
			Invalidate( lower );
		}

		void Doc_SelectionChanged_OnReleaseSel( SelectionChangedEventArgs e )
		{
			// in this case, we must invalidate between
			// old anchor pos and old caret pos.
			Document doc = base.Document;
			Rectangle upper = new Rectangle();
			Rectangle middle = new Rectangle();
			Rectangle lower = new Rectangle();
			int beginLineHead, endLineHead;
			int begin, beginL, beginC;
			int end, endL, endC;

			// get old selection range
			if( e.OldAnchor < e.OldCaret )
			{
				begin = e.OldAnchor;
				beginL = _PrevAnchorLine;
				beginC = _PrevAnchorColumn;
				end = e.OldCaret;
				endL = _PrevCaretLine;
				endC = _PrevCaretColumn;
			}
			else
			{
				begin = e.OldCaret;
				beginL = _PrevCaretLine;
				beginC = _PrevCaretColumn;
				end = e.OldAnchor;
				endL = _PrevAnchorLine;
				endC = _PrevAnchorColumn;
			}
			beginLineHead = GetLineHeadIndexFromCharIndex( begin );
			endLineHead = GetLineHeadIndexFromCharIndex( end );

			// if old selection was in one line?
			if( _PrevCaretLine == _PrevAnchorLine )
			{
				string textBeforeSel = doc.GetTextInRange( beginLineHead, begin );
				string textSelected = doc.GetTextInRange( endLineHead, end );
				int left = MeasureTokenEndX( textBeforeSel, 0 ) - (ScrollPosX - TextAreaX);
				int right = MeasureTokenEndX( textSelected, 0 ) - (ScrollPosX - TextAreaX);
				upper.X = left;
				upper.Y = LineSpacing * beginL - (FirstVisibleLine * LineSpacing);
				upper.Width = right - left;
				upper.Height = LineSpacing;
			}
			else
			{
				// calculate upper part of the invalid area
				string leftPart = doc.GetTextInRange( beginLineHead, begin );
				upper.X = MeasureTokenEndX( leftPart, 0 ) - (ScrollPosX - TextAreaX);
				upper.Y = LineSpacing * beginL - (FirstVisibleLine * LineSpacing);
				upper.Width = VisibleSize.Width - upper.X;
				upper.Height = LineSpacing;

				// calculate lower part of the invalid area
				string rightPart = doc.GetTextInRange( endLineHead, end );
				lower.X = TextAreaX;
				lower.Y = LineSpacing * endL - (FirstVisibleLine * LineSpacing);
				lower.Width = MeasureTokenEndX( rightPart, 0 ) - ScrollPosX;
				lower.Height = LineSpacing;

				// calculate middle part of the invalid area
				middle.X = TextAreaX;
				middle.Y = LineSpacing * (beginL + 1) - (FirstVisibleLine * LineSpacing);
				middle.Width = VisibleSize.Width;
				middle.Height = lower.Y - middle.Y;
			}

			// invalidate three rectangles
			Invalidate( upper );
			if( 0 < middle.Height )
				Invalidate( middle );
			if( 0 < lower.Height )
				Invalidate( lower );
		}

		protected override void Doc_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			Point oldCaretPos;
			Rectangle invalidRect1 = new Rectangle();
			Rectangle invalidRect2 = new Rectangle();
			int oldTextLineCount, newTextLineCount;

			// get position of the word replacement occured
			oldCaretPos = GetVirPosFromIndex( e.Index );

			// invalidate the part at right of the old selection
			invalidRect1.X = oldCaretPos.X -(ScrollPosX - TextAreaX);
			invalidRect1.Y = oldCaretPos.Y -(FirstVisibleLine * LineSpacing);
			invalidRect1.Width = VisibleSize.Width - invalidRect1.X;
			invalidRect1.Height = LineSpacing;

			// invalidate all lines below caret
			// if old text or new text contains multiple lines
			oldTextLineCount = LineLogic.CountLine( e.OldText );
			newTextLineCount = LineLogic.CountLine( e.NewText );
			if( 1 < oldTextLineCount || 1 < newTextLineCount )
			{
				//NO_NEED//invalidRect2.X = 0;
				invalidRect2.Y = invalidRect1.Bottom;
				invalidRect2.Width = VisibleSize.Width;
				invalidRect2.Height = VisibleSize.Height - invalidRect2.Top;
			}

			// invalidate the range
			Invalidate( invalidRect1 );
			if( 0 < invalidRect2.Height )
			{
				Invalidate( invalidRect2 );
			}
		}
		#endregion

		#region Painting
		/// <summary>
		/// Paints content to a graphic device.
		/// </summary>
		/// <param name="clipRect">clipping rectangle that covers all invalidated region (in screen coord.)</param>
		internal override void OnPaint( Rectangle clipRect )
		{
			Debug.Assert( Font != null, "invalid state; Font is null" );
			Debug.Assert( Document != null, "invalid state; Document is null" );

			int selBegin, selEnd;
			Point pos = new Point();

			// prepare off-screen buffer
#			if !DRAW_SLOWLY && !PocketPC
			_Gra.BeginPaint( clipRect );
#			endif

			// draw all lines
			_Gra.SetClipRect( clipRect );
			pos.X = -(ScrollPosX - TextAreaX);
			for( int i=FirstVisibleLine; i<LineCount; i++ )
			{
				if( pos.Y < clipRect.Bottom && clipRect.Top <= pos.Y+LineHeight )
				{
					DrawLine( i, pos, clipRect );
				}
				pos.Y += LineSpacing;
			}
			_Gra.RemoveClipRect();

			// fill area below of the text
			_Gra.BackColor = ColorScheme.BackColor;
			_Gra.FillRectangle( 0, pos.Y, VisibleSize.Width, VisibleSize.Height-pos.Y );

			// flush drawing results BEFORE updating current line highlight
			// because the highlight graphic is never limited to clipping rect
#			if !DRAW_SLOWLY && !PocketPC
			_Gra.EndPaint();
#			endif

			// draw underline to highlight current line if there is no selection
			Document.GetSelection( out selBegin, out selEnd );
			if( HighlightsCurrentLine && selBegin == selEnd )
			{
				int caretLine, caretPosY;

				// calculate position of the underline
				caretLine = Document.GetLineIndexFromCharIndex( Document.CaretIndex );
				caretPosY = caretLine * LineSpacing - (FirstVisibleLine * LineSpacing);
				
				// draw underline
				DrawUnderLine( caretPosY, ColorScheme.HighlightColor );
			}
		}

		void DrawLine( int lineIndex, Point pos, Rectangle clipRect )
		{
			// note that given pos is NOT virtual position BUT screen position.
			string token;
			int lineHead, lineEnd;
			int begin, end; // range of the token in the text
			CharClass klass;
			Point tokenEndPos = pos;

			// calc position of head/end of this line
			lineHead = Document.GetLineHeadIndex( lineIndex );
			if( lineIndex+1 < Document.LineCount )
				lineEnd = Document.GetLineHeadIndex( lineIndex + 1 );
			else
				lineEnd = Document.Length;

			// draw line text
			begin = lineHead;
			end = NextPaintToken( Document.InternalBuffer, begin, lineEnd, out klass );
			while( end <= lineEnd // until end-pos reaches line-end
				&& pos.X < clipRect.Right // or reaches right-end of the clip rect
				&& end != -1 ) // or reaches the end of text
			{
				// get this token
				token = Document.GetTextInRange( begin, end );
				Debug.Assert( 0 < token.Length );

				// calc next drawing pos before drawing text
				{
					int virLeft = pos.X - (TextAreaX - ScrollPosX);
					tokenEndPos.X = MeasureTokenEndX( token, virLeft );
					tokenEndPos.X += (TextAreaX - ScrollPosX);
				}

				// if this token is out of the clip-rect, skip drawing.
				if( tokenEndPos.X < clipRect.Left || clipRect.Right < pos.X )
				{
					goto next_token;
				}

				// if the token area crosses the LEFT boundary of the clip-rect, cut off extra
				if( pos.X < clipRect.Left )
				{
					int invisibleCharCount, invisibleWidth;
					int rightLimit = clipRect.Left - pos.X;

					// calculate how many chars will not be in the clip-rect
					invisibleWidth = MeasureTokenEndX( token, 0, rightLimit, out invisibleCharCount );
					if( invisibleCharCount < token.Length )
					{
						// cut extra (invisible) part of the token
						token = token.Substring( invisibleCharCount );

						// advance drawing position as if the cut part was actually drawn
						pos.X += invisibleWidth;
					}
				}

				// if the token area crosses the RIGHT boundary, cut off extra
				if( clipRect.Right < tokenEndPos.X )
				{
					int visibleCharCount, visibleWidth;

					// calculate how many chars can be drawn in the clip-rect
					visibleWidth = MeasureTokenEndX( token, pos.X, clipRect.Right, out visibleCharCount );

					// set the position to cut extra trailings of this token
					if( visibleCharCount == token.Length )
					{
						token = token.Substring( 0, visibleCharCount );
					}
					else if( visibleCharCount+2 < token.Length
						&& Document.IsHighSurrogate(token[visibleCharCount]) )
					{
						token = token.Substring( 0, visibleCharCount + 2 );
					}
					else if( visibleCharCount+1 < token.Length )
					{
						token = token.Substring( 0, visibleCharCount + 1 );
					}

					// set token end position to the right limit to terminate loop
					tokenEndPos.X = clipRect.Right;
				}

				// draw this token
				DrawToken( token, klass, ref pos, ref tokenEndPos, ref clipRect );

			next_token:
				// get next token
				pos = tokenEndPos;
				begin = end;
				end = NextPaintToken( Document.InternalBuffer, begin, lineEnd, out klass );
			}

			// fill right of the line text
			if( pos.X < clipRect.Right )
			{
				// to prevent drawing line number area,
				// make drawing pos to text area's left if the line end does not exceed left of text area
				if( pos.X < TextAreaX )
					pos.X = TextAreaX;
				_Gra.BackColor = ColorScheme.BackColor;
				_Gra.FillRectangle( pos.X, pos.Y, clipRect.Right-pos.X, LineSpacing );
			}

			// if this line is wider than the width of virtual space,
			// calculate full width of this line and make it the width of virtual space.
			if( TextAreaWidth - (VisibleSize.Width >> 1) < (pos.X + ScrollPosX) )
			{
				token = Document.GetTextInRange( lineHead, lineEnd );
				int lineWidth = MeasureTokenEndX( token, 0 );
				TextAreaWidth = lineWidth + (VisibleSize.Width >> 1);
				_UI.UpdateScrollBarRange();
			}

			// draw line number
			if( ShowLineNumber && clipRect.Left < TextAreaX )
			{
				DrawLineNumber( pos.Y, lineIndex+1 );
			}
		}
		#endregion
	}
}
