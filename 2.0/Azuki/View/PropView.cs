// file: PropView.cs
// brief: Platform independent view (proportional).
//=========================================================
//DEBUG//#define DRAW_SLOWLY
using System;
using System.Drawing;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view implementation to display text with proportional font.
	/// </summary>
	class PropView : View
	{
		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal PropView( IUserInterfaceInternal ui )
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
				Document.SetSelection( CaretIndex, CaretIndex );

				// scroll to caret manually.
				// (because text graphic was not drawn yet,
				// maximum line length is unknown
				// so ScrollToCaret does not work properly)
				using( var g = _UI.GetIGraphics() )
				{
					var pos = GetVirtualPos( g, CaretIndex );
					int newValue = pos.X - (VisibleTextAreaSize.Width / 2);
					if( 0 < newValue )
					{
						ScrollPosX = newValue;
						_UI.UpdateScrollBarRange();
					}
				}
			}
		}
		#endregion

		#region Properties
		public override ILineRangeList Lines
		{
			get{ return Document.Lines; }
		}

		public override ILineRangeList RawLines
		{
			get{ return Document.RawLines; }
		}

		/// <summary>
		/// Re-calculates and updates x-coordinate of the right end of the virtual text area.
		/// </summary>
		/// <param name="desiredX">X-coordinate of scroll destination desired.</param>
		/// <returns>The largest X-coordinate which Azuki can scroll to.</returns>
		protected override int ReCalcRightEndOfTextArea( int desiredX )
		{
			if( TextAreaWidth < desiredX )
			{
				TextAreaWidth = desiredX + (VisibleTextAreaSize.Width >> 3);
				_UI.UpdateScrollBarRange();
			}
			return TextAreaWidth;
		}
		#endregion

		#region Position / Index Conversion
		/// <exception cref="ArgumentOutOfRangeException"/>
		public override Point GetVirtualPos( IGraphics g, int lineIndex, int columnIndex )
		{
			Debug.Assert( g != null );
			if( lineIndex < 0 || Lines.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Specified index is out of"
													   + " range. (value:" + lineIndex
													   + ", line count:" + Lines.Count + ")" );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Specified index is out of"
													   + " range. (value:" + columnIndex + ")" );

			// set value for when the columnIndex is 0
			var pos = new Point( 0, (lineIndex * LineSpacing) + (LinePadding >> 1) );

			// if the location is not the head of the line, calculate x-coord.
			if( 0 < columnIndex )
			{
				// get partial content of the line which exists before the caret
				var leftPart = Document.GetText( lineIndex, 0, lineIndex, columnIndex );

				// measure the characters
				pos.X = MeasureTokenEndX( g, leftPart, pos.X );
			}

			return pos;
		}

		public override int GetCharIndex( IGraphics g, Point pos )
		{
			Debug.Assert( g != null );
			int lineIndex, columnIndex;
			int drawableTextLen;

			// calc line index
			lineIndex = (pos.Y / LineSpacing);
			if( lineIndex < 0 )
			{
				lineIndex = 0;
			}
			else if( Document.Lines.Count <= lineIndex
				&& Document.Lines.Count != 0 )
			{
				// the point indicates beyond the final line.
				// treat as if the final line was specified
				lineIndex = Document.Lines.Count - 1;
			}

			// calc column index
			columnIndex = 0;
			if( 0 < pos.X )
			{
				// get content of the line
				var line = Document.Lines[ lineIndex ].Text;

				// calc maximum length of chars in line
				int rightLimitX = pos.X;
				int leftPartWidth = MeasureTokenEndX( g, line, 0, rightLimitX,
													  out drawableTextLen );
				Debug.Assert( TextUtil.IsNotDividableIndex(line, drawableTextLen) == false );
				columnIndex = drawableTextLen;

				// if the location is nearer to the NEXT of that char,
				// we should return the index of next one.
				if( drawableTextLen < line.Length )
				{
					// get next grapheme cluster
					var nextChar = line[drawableTextLen].ToString();
					int nextCharEnd = drawableTextLen + 1;
					while( TextUtil.IsNotDividableIndex(line, nextCharEnd) )
					{
						nextChar += line[ nextCharEnd ];
						nextCharEnd++;
					}

					// determine which side the location is near
					int nextCharWidth = MeasureTokenEndX( g, nextChar, leftPartWidth )
										- leftPartWidth;
					if( leftPartWidth + nextCharWidth/2 < pos.X )
					{
						columnIndex = drawableTextLen + 1;
						while( TextUtil.IsNotDividableIndex(line, columnIndex) )
							columnIndex++;
					}
				}
			}

			return Document.GetCharIndex( new LineColumnPos(lineIndex, columnIndex) );
		}

		/// <summary>
		/// Calculates screen line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public override LineColumnPos GetLineColumnPos( int charIndex )
		{
			return Document.GetLineColumnPos( charIndex );
		}

		/// <summary>
		/// Calculates char-index from screen line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public override int GetCharIndex( LineColumnPos pos )
		{
			return Document.GetCharIndex( pos );
		}
		#endregion

		#region Appearance Invalidating and Updating
		internal override void HandleSelectionChanged( object sender, SelectionChangedEventArgs e )
		{
			if( e.ByContentChanged )
				return;

			int anchor = AnchorIndex;
			int caret = CaretIndex;
			int prevCaretLine = PerDocParam.PrevCaretLine;

			// calculate line/column index of current anchor/caret
			var anchorPos = GetLineColumnPos( anchor );
			var caretPos = GetLineColumnPos( caret );

			using( var g = _UI.GetIGraphics() )
			try
			{
				UpdateHRuler( g );

				// If the anchor moved, firstly invalidate old selection area
				// because invalidation logic bellow does not expect the anchor's move.
				if( e.OldAnchor != anchor )
				{
					if( e.OldAnchor < e.OldCaret )
						Invalidate( e.OldAnchor, e.OldCaret );
					else
						Invalidate( e.OldCaret, e.OldAnchor );
				}

				// If in rectangle selection mode, execute special logic
				if( e.OldRectSelectRanges != null )
				{
					HandleSelectionChanged_OnRectSelect( g, e );
					return;
				}

				if( e.OldAnchor == e.OldCaret && anchor == caret ) // is and was no selection
				{
					if( HighlightsCurrentLine && prevCaretLine != caretPos.Line )
						HandleSelectionChanged_UpdateCurrentLineHighlight( g, caretPos.Line );
				}
				else if( e.OldAnchor != e.OldCaret && anchor == caret ) // selection released
				{
					HandleSelectionChanged_OnReleaseSel( g, e );
				}
				else // selection expanded
				{
					// Remove current line highlight if this is the beginning of selection
					if( HighlightsCurrentLine && e.OldCaret == e.OldAnchor )
					{
						var oldCaretLineY = YofLine( GetLineColumnPos(e.OldCaret).Line );
						DrawUnderLine( g, oldCaretLineY, ColorScheme.BackColor );
					}

					if( prevCaretLine == caretPos.Line )
					{
						if( e.OldCaret < caret )
							HandleSelectionChanged_OnExpandSelInLine( g, e, e.OldCaret, caret,
																	  prevCaretLine );
						else
							HandleSelectionChanged_OnExpandSelInLine( g, e, caret, e.OldCaret,
																	  caretPos.Line );
					}
					else
					{
						HandleSelectionChanged_OnExpandSel( g, e, caretPos.Line );
					}
				}
			}
			catch( Exception ex )
			{
				// It unlikely be a fatal error so avoid crashing the application
				Invalidate();
				Debug.Fail( ex.ToString() );
			}
			finally
			{
				// Remember last selection for next invalidation
				PerDocParam.PrevCaretLine = caretPos.Line;
				PerDocParam.PrevAnchorLine = anchorPos.Line;
			}
		}

		void HandleSelectionChanged_UpdateCurrentLineHighlight( IGraphics g, int newCaretLine )
		{
			int prevAnchorLine = PerDocParam.PrevAnchorLine;
			int prevCaretLine = PerDocParam.PrevCaretLine;

			// Invalidate old underline if it is still visible
			if( prevCaretLine == prevAnchorLine && FirstVisibleLine <= prevCaretLine )
				DrawUnderLine( g, YofLine(prevCaretLine), ColorScheme.BackColor );
			
			// Draw new underline if it is visible
			if( FirstVisibleLine <= newCaretLine )
				DrawUnderLine( g, YofLine(newCaretLine), ColorScheme.HighlightColor );
		}

		void HandleSelectionChanged_OnRectSelect( IGraphics g, SelectionChangedEventArgs e )
		{
			//--- make rectangle that covers ---
			// 1) all lines covered by the selection rectangle and
			// 2) extra lines for both upper and lower direction
			
			// Calculate rectangle in virtual space
			var firstBegin = e.OldRectSelectRanges[0];
			var lastEnd = e.OldRectSelectRanges[ e.OldRectSelectRanges.Length - 1 ];
			Debug.Assert( 0 <= firstBegin && firstBegin <= Document.Length );
			Debug.Assert( 0 <= lastEnd && lastEnd <= Document.Length );
			var firstBeginPos = GetVirtualPos( g, firstBegin );
			var lastEndPos = GetVirtualPos( g, lastEnd );
			firstBeginPos.Y -= (LinePadding >> 1);
			lastEndPos.Y -= (LinePadding >> 1);

			// Convert it to screen screen coordinate
			firstBeginPos = VirtualToScreen( firstBeginPos );
			lastEndPos = VirtualToScreen( lastEndPos );

			// Then, invalidate that rectangle
			// (triple of line-spacing: a line above, the line, and a line below)
			var invalidRect = new Rectangle( 0,
											 firstBeginPos.Y - LineSpacing,
											 VisibleSize.Width,
											 (lastEndPos.Y - firstBeginPos.Y)+(LineSpacing * 3) );
			Invalidate( invalidRect );
		}

		void HandleSelectionChanged_OnExpandSelInLine( IGraphics g, SelectionChangedEventArgs e,
													   int begin, int end, int beginL )
		{
			DebugUtl.Assert( beginL < Lines.Count );
			var doc = Document;
			var rect = new Rectangle();
			var token = String.Empty;

			// If anchor was moved, invalidate largest range made with four indexes
			if( e.OldAnchor != AnchorIndex )
			{
				begin = Min( e.OldAnchor, e.OldCaret, AnchorIndex, CaretIndex );
				end = Max( e.OldAnchor, e.OldCaret, AnchorIndex, CaretIndex );
				Invalidate( begin, end );

				return;
			}

			// Get chars at left of invalid rect
			var beginLineHead = Lines[ beginL ].Begin;
			if( beginLineHead < begin )
			{
				token = Document.GetText( beginLineHead, begin );
			}

			// Calculate invalid rect
			rect.X = MeasureTokenEndX( g, token, 0 );
			rect.Y = YofLine( beginL );
			token = Document.GetText( beginLineHead, end );
			rect.Width = MeasureTokenEndX( g, token, 0 ) - rect.X;
			rect.Height = LineSpacing;

			// Invalidate
			rect.X -= (ScrollPosX - ScrXofTextArea);
			Invalidate( rect );
		}

		void HandleSelectionChanged_OnExpandSel( IGraphics g, SelectionChangedEventArgs e,
												 int caretLine )
		{
			var doc = Document;
			int begin, beginL;
			int end, endL;

			// if anchor was moved, invalidate largest range made with four indexes
			if( e.OldAnchor != AnchorIndex )
			{
				begin = Min( e.OldAnchor, e.OldCaret, AnchorIndex, CaretIndex );
				end = Max( e.OldAnchor, e.OldCaret, AnchorIndex, CaretIndex );
				Invalidate( begin, end );

				return;
			}

			// get range between old caret and current caret
			if( e.OldCaret < CaretIndex )
			{
				begin = e.OldCaret;
				beginL = PerDocParam.PrevCaretLine;
				end = CaretIndex;
				endL = caretLine;
			}
			else
			{
				begin = CaretIndex;
				beginL = caretLine;
				end = e.OldCaret;
				endL = PerDocParam.PrevCaretLine;
			}
			var beginLineHead = Lines[ beginL ].Begin;
			var endLineHead = Lines[ endL ].Begin; // if old caret is the end pos and if the pos
												   // exceeds current text length, this will fail.

			Invalidate_MultiLines( g, begin, end, beginL, endL, beginLineHead, endLineHead );
		}

		void HandleSelectionChanged_OnReleaseSel( IGraphics g, SelectionChangedEventArgs e )
		{
			// in this case, we must invalidate between
			// old anchor pos and old caret pos.
			var doc = base.Document;
			int begin, beginL;
			int end, endL;
			int prevAnchorLine = PerDocParam.PrevAnchorLine;
			int prevCaretLine = PerDocParam.PrevCaretLine;

			// get old selection range
			if( e.OldAnchor < e.OldCaret )
			{
				begin = e.OldAnchor;
				beginL = prevAnchorLine;
				end = e.OldCaret;
				endL = prevCaretLine;
			}
			else
			{
				begin = e.OldCaret;
				beginL = prevCaretLine;
				end = e.OldAnchor;
				endL = prevAnchorLine;
			}
			var beginLineHead = Lines.AtOffset( begin ).Begin;
			var endLineHead = Lines.AtOffset( end ).Begin;

			// if old selection was in one line?
			if( prevCaretLine == prevAnchorLine )
			{
				var rect = new Rectangle();
				var textBeforeSel = doc.GetText( beginLineHead, begin );
				var textSelected = doc.GetText( endLineHead, end );
				int left = MeasureTokenEndX( g, textBeforeSel, 0 ) - (ScrollPosX - ScrXofTextArea);
				int right = MeasureTokenEndX( g, textSelected, 0 ) - (ScrollPosX - ScrXofTextArea);
				rect.X = left;
				rect.Y = YofLine( beginL );
				rect.Width = right - left;
				rect.Height = LineSpacing;

				Invalidate( rect );
			}
			else
			{
				Invalidate_MultiLines( g, begin, end, beginL, endL, beginLineHead, endLineHead );
			}
		}

		internal override void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			// [*1] if replacement breaks or creates
			// a combining character sequence at left boundary of the range,
			// at least one grapheme cluster left must be redrawn.
			// 
			// One case of that e.OldText has combining char at first:
			//    aa^aa --(replace [2, 4) to "AA")--> aaAAa
			// 
			// One case of that e.NewText has combining char at first:
			//    aaaa --(replace [2, 3) to "^A")--> aa^Aa

			Point invalidStartPos;
			int invalidStartIndex;
			var invalidRect1 = new Rectangle();
			var invalidRect2 = new Rectangle();

			using( var g = _UI.GetIGraphics() )
			{
				// Calculate where to start invalidation
				invalidStartIndex = e.Index;
				if( TextUtil.IsCombiningCharacter(e.OldText, 0)
					|| TextUtil.IsCombiningCharacter(e.NewText, 0) )
				{
					// [*1]
					invalidStartIndex = Lines.AtOffset( invalidStartIndex ).Begin;
				}

				// Get graphical position of the place
				invalidStartPos = VirtualToScreen( GetVirtualPos(g, invalidStartIndex) );

				// Update indicator graphic on horizontal ruler
				UpdateHRuler( g );

				// Invalidate the part at right of the old selection
				invalidRect1.X = invalidStartPos.X;
				invalidRect1.Width = VisibleSize.Width - invalidRect1.X;
				invalidRect1.Y = invalidStartPos.Y - (LinePadding >> 1);
				invalidRect1.Height = LineSpacing;

				// Invalidate all lines below caret if old text or new text contains multiple lines
				if( TextUtil.IsMultiLine(e.OldText) || TextUtil.IsMultiLine(e.NewText) )
				{
					//NO_NEED//invalidRect2.X = 0;
					invalidRect2.Y = invalidRect1.Bottom;
					invalidRect2.Width = VisibleSize.Width;
					invalidRect2.Height = VisibleSize.Height - invalidRect2.Top;
				}

				// Invalidate the range
				Invalidate( invalidRect1 );
				if( 0 < invalidRect2.Height )
				{
					Invalidate( invalidRect2 );
				}

				// Update left side of text area
				DrawDirtBar( g, invalidRect1.Top, Document.Lines.AtOffset(e.Index).LineIndex );
				UpdateLineNumberWidth( g );

				//DO_NOT//base.HandleContentChanged( sender, e );
			}
		}

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="range">A range of text which is needed to be repainted.</param>
		public override void Invalidate( IRange range )
		{
			Invalidate( range.Begin, range.End );
		}

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be repainted.</param>
		/// <param name="endIndex">End text index of the area to be repainted.</param>
		public override void Invalidate( int beginIndex, int endIndex )
		{
			using( var g = _UI.GetIGraphics() )
				Invalidate( g, beginIndex, endIndex );
		}

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="beginIndex">Begin text index of the area to be repainted.</param>
		/// <param name="endIndex">End text index of the area to be repainted.</param>
		public override void Invalidate( IGraphics g, int beginIndex, int endIndex )
		{
			DebugUtl.Assert( 0 <= beginIndex, "cond: 0 <= beginIndex("+beginIndex+")" );
			DebugUtl.Assert( beginIndex <= endIndex, "cond: beginIndex(" + beginIndex
							 + ") <= endIndex(" + endIndex + ")" );
			DebugUtl.Assert( endIndex <= Document.Length, "endIndex(" + endIndex
							 + ") must not exceed document length (" + Document.Length + ")" );
			if( beginIndex == endIndex )
				return;
			
			int beginLineHead, endLineHead;

			// get needed coordinates
			var beginPos = GetLineColumnPos( beginIndex );
			var endPos = GetLineColumnPos( endIndex );
			beginLineHead = Lines[ beginPos.Line ].Begin;

			// switch invalidation logic by whether the invalidated area is multiline or not
			if( beginPos.Line != endPos.Line )
			{
				endLineHead = Lines[ endPos.Line ].Begin; // for invalidating multiline selection
				Invalidate_MultiLines( g, beginIndex, endIndex, beginPos.Line, endPos.Line,
									   beginLineHead, endLineHead );
			}
			else
			{
				Invalidate_InLine( g, beginIndex, endIndex, beginPos.Line, beginLineHead );
			}
		}
		
		void Invalidate_InLine( IGraphics g, int begin, int end, int beginL, int beginLineHead )
		{
			DebugUtl.Assert( g != null, "null was given to PropView.Invalidate_InfLine." );
			DebugUtl.Assert( 0 <= begin, "cond: 0 <= begin(" + begin + ")" );
			DebugUtl.Assert( begin <= end, "cond: begin(" + begin + ") <= end(" + end + ")" );
			DebugUtl.Assert( end <= Document.Length, "cond: end(" + end + ") <= Document.Length("
							 + Document.Length + ")" );
			DebugUtl.Assert( 0 <= beginL, "cond: 0 <= beginL(" + beginL + ")" );
			DebugUtl.Assert( beginL <= Lines.Count, "cond: beginL(" + beginL
							 + ") <= Lines.Count(" + Lines.Count + ")" );
			DebugUtl.Assert( beginLineHead <= begin, "cond: beginLineHead(" + beginLineHead
							 + ") <= begin(" + begin + ")" );
			if( begin == end )
				return;

			var rect = new Rectangle();

			// Calculate position of the invalid rect
			var textBeforeSelBegin = Document.GetText( beginLineHead, begin );
			rect.X = MeasureTokenEndX( g, textBeforeSelBegin, 0 );
			rect.Y = YofLine( beginL );

			// Calculate width and height of the invalid rect
			var textSelected = Document.GetText( begin, end );
			rect.Width = MeasureTokenEndX( g, textSelected, rect.X ) - rect.X;
			rect.Height = LineSpacing;
			Debug.Assert( 0 <= rect.Width );

			// Invalidate
			rect.X -= (ScrollPosX - ScrXofTextArea);
			Invalidate( rect );
		}

		void Invalidate_MultiLines( IGraphics g, int begin, int end, int beginLine, int endLine,
									int beginLineHead, int endLineHead )
		{
			DebugUtl.Assert( g != null, "null was given to PropView.Invalidate_MultiLines." );
			DebugUtl.Assert( 0 <= begin, "cond: 0 <= begin(" + begin + ")" );
			DebugUtl.Assert( begin <= end, "cond: begin(" + begin + ") <= end(" + end + ")" );
			DebugUtl.Assert( end <= Document.Length, "cond: end(" + end + ") <= Document.Length("
							 + Document.Length + ")" );
			DebugUtl.Assert( 0 <= beginLine, "cond: 0 <= beginLine(" + beginLine + ")" );
			DebugUtl.Assert( beginLine < endLine, "cond: beginLine(" + beginLine + ") < endLine("
							 + endLine + ")" );
			DebugUtl.Assert( endLine <= Lines.Count, "cond: endLine(" + endLine + ") <= "
							 + "Lines.Count(" + Lines.Count + ")" );
			DebugUtl.Assert( beginLineHead <= begin, "cond: beginLineHead(" + beginLineHead
							 + ") <= begin(" + begin + ")" );
			DebugUtl.Assert( beginLineHead < endLineHead, "cond: beginLineHead(" + beginLineHead
							 + " < endLineHead(" + endLineHead + ")" );
			DebugUtl.Assert( endLineHead <= end, "cond: endLineHead(" + endLineHead
							 + ") <= end(" + end + ")" );
			if( begin == end )
				return;

			Rectangle upper, lower, middle;
			var doc = Document;

			// calculate upper part of the invalid area
			var firstLinePart = doc.GetText( beginLineHead, begin );
			upper = new Rectangle();
			if( FirstVisibleLine <= beginLine ) // if not visible, no need to invalidate
			{
				upper.X = MeasureTokenEndX( g, firstLinePart, 0 ) - (ScrollPosX - ScrXofTextArea);
				upper.Y = YofLine( beginLine );
				upper.Width = VisibleSize.Width - upper.X;
				upper.Height = LineSpacing;
			}

			// calculate lower part of the invalid area
			var finalLinePart = doc.GetText( endLineHead, end );
			lower = new Rectangle();
			if( FirstVisibleLine <= endLine ) // if not visible, no need to invalidate
			{
				lower.X = ScrXofTextArea;
				lower.Y = YofLine( endLine );
				lower.Width = MeasureTokenEndX( g, finalLinePart, 0 ) - ScrollPosX;
				lower.Height = LineSpacing;
			}

			// calculate middle part of the invalid area
			middle = new Rectangle();
			if( FirstVisibleLine < beginLine+1 )
			{
				middle.Y = YofLine( beginLine + 1 );
			}
			middle.X = ScrXofTextArea;
			middle.Width = VisibleSize.Width;
			middle.Height = lower.Y - middle.Y;

			// invalidate three rectangles
			if( 0 < upper.Height )
				Invalidate( upper );
			if( 0 < middle.Height )
				Invalidate( middle );
			if( 0 < lower.Height )
				Invalidate( lower );
		}
		#endregion

		#region Painting
		/// <summary>
		/// Paints content to a graphic device.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="clipRect">
		/// clipping rectangle that covers all invalidated region (in client area coordinate)
		/// </param>
		public override void Paint( IGraphics g, Rectangle clipRect )
		{
			// [*1] if the graphic of a line should be redrawn by owner draw,
			// Azuki does not redraw the line but invalidate
			// the area of the line and let it be drawn on next drawing chance
			// so that the graphic will not flicker.
			DebugUtl.Assert( g != null, "invalid argument; IGraphics is null" );
			DebugUtl.Assert( FontInfo != null, "invalid state; FontInfo is null" );
			DebugUtl.Assert( Document != null, "invalid state; Document is null" );

			int selBegin, selEnd;
			var pos = new Point();
			int longestLineLength = 0;

			// Prepare off-screen buffer
#			if !DRAW_SLOWLY
			g.BeginPaint( clipRect );
#			endif

#			if DRAW_SLOWLY
			g.ForeColor = Color.Blue;
			g.DrawRectangle( clipRect.X, clipRect.Y, clipRect.Width-1, clipRect.Height-1 );
			g.DrawLine( clipRect.X, clipRect.Y, clipRect.X+clipRect.Width-1, clipRect.Y+clipRect.Height-1 );
			g.DrawLine( clipRect.X+clipRect.Width-1, clipRect.Y, clipRect.X, clipRect.Y+clipRect.Height-1 );
			DebugUtl.Sleep(400);
#			endif

			// Draw horizontal ruler and top margin
			DrawHRuler( g, clipRect );
			DrawTopMargin( g );

			// Draw all lines
			g.SetClipRect( clipRect );
			pos.X = -(ScrollPosX - ScrXofTextArea);
			pos.Y = ScrYofTextArea;
			for( int i=FirstVisibleLine; i<Lines.Count; i++ )
			{
				if( pos.Y < clipRect.Bottom && clipRect.Top <= pos.Y+LineSpacing )
				{
					// Draw the line
					var shouldRedraw1 = _UI.InvokeLineDrawing( g, i, pos );
					DrawLine( g, i, pos, clipRect, ref longestLineLength );
					var shouldRedraw2 = _UI.InvokeLineDrawn( g, i, pos );

					// [*1] Invalidate the line graphic if needed
					if( (shouldRedraw1 || shouldRedraw2)
						&& 0 < clipRect.Left ) // prevent infinite loop
					{
						Invalidate( 0, clipRect.Y, VisibleSize.Width, clipRect.Height );
					}
				}
				pos.Y += LineSpacing;
			}
			g.RemoveClipRect();

			// Expand text area width for graphically longest line
			ReCalcRightEndOfTextArea( longestLineLength );

			// Fill area below of the text
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( ScrXofTextArea,
							 pos.Y,
							 VisibleSize.Width - ScrXofTextArea,
							 VisibleSize.Height - pos.Y );
			for( int y=pos.Y; y<VisibleSize.Height; y+=LineSpacing )
			{
				DrawLeftOfLine( g, y, -1, false );
			}

			// Flush drawing results BEFORE updating current line highlight
			// because the highlight graphic can be drawn outside of the clipping rect
#			if !DRAW_SLOWLY
			g.EndPaint();
#			endif

			// Draw underline to highlight current line if there is no selection
			Document.GetSelection( out selBegin, out selEnd );
			if( HighlightsCurrentLine && selBegin == selEnd )
			{
				// Draw underline only when the current line is visible
				int caretLine = Document.Lines.AtOffset( CaretIndex ).LineIndex;
				if( FirstVisibleLine <= caretLine )
				{
					var lineDiff = caretLine - FirstVisibleLine;
					var caretPosY = (lineDiff * LineSpacing) + ScrYofTextArea;
					DrawUnderLine( g, caretPosY, ColorScheme.HighlightColor );
				}
			}
		}

		void DrawLine( IGraphics g, int lineIndex, Point pos, Rectangle clipRect,
					   ref int longestLineLength )
		{
			// note that given pos is NOT virtual position BUT screen position.
			string token;
			int begin, end; // range of the token in the text
			CharClass klass;
			Point tokenEndPos = pos;
			bool inSelection;

			var line = Document.RawLines[ lineIndex ];

			// Draw line text
			begin = line.Begin;
			end = NextPaintToken( Document, begin, line.End, out klass, out inSelection );
			while( end <= line.End // until end-pos reaches line-end
				&& pos.X < clipRect.Right // or reaches right-end of the clip rect
				&& end != -1 ) // or reaches the end of text
			{
				// Get this token
				token = Document.GetText( begin, end );
				DebugUtl.Assert( 0 < token.Length );

				// Calc next drawing pos before drawing text
				{
					int virLeft = pos.X - (ScrXofTextArea - ScrollPosX);
					tokenEndPos.X = MeasureTokenEndX( g, token, virLeft );
					tokenEndPos.X += (ScrXofTextArea - ScrollPosX);
				}

				// If this token is out of the clip-rect, skip drawing.
				if( tokenEndPos.X < clipRect.Left || clipRect.Right < pos.X )
				{
					goto next_token;
				}

				// If the token area crosses the LEFT boundary of the clip-rect, cut off extra
				if( pos.X < clipRect.Left )
				{
					int invisibleCharCount;
					int rightLimit = clipRect.Left - pos.X;

					// Calculate how many chars will not be in the clip-rect
					var invisibleWidth = MeasureTokenEndX( g, token, 0, rightLimit,
														   out invisibleCharCount );
					if( 0 < invisibleCharCount && invisibleCharCount < token.Length )
					{
						// Cut extra (invisible) part of the token
						token = token.Substring( invisibleCharCount );
						begin += invisibleCharCount;
						pos.X += invisibleWidth;
					}
				}

				// If the token area crosses the RIGHT boundary, cut off extra
				if( clipRect.Right < tokenEndPos.X )
				{
					int visibleCharCount;

					// Calculate how many chars can be drawn in the clip-rect
					MeasureTokenEndX( g, token, pos.X, clipRect.Right, out visibleCharCount );

					// Set the position to cut extra trailings of this token
					if( visibleCharCount+1 <= token.Length )
					{
						if( TextUtil.IsNotDividableIndex(token, visibleCharCount+1) )
							token = token.Substring( 0, visibleCharCount + 2 );
						else
							token = token.Substring( 0, visibleCharCount + 1 );
					}
					else
					{
						token = token.Substring( 0, visibleCharCount );
					}
					end = begin + token.Length;
				}

				// Draw this token
				DrawToken( g, Document, begin, token, klass, ref pos, ref tokenEndPos,
						   ref clipRect, inSelection );

			next_token:
				pos = tokenEndPos;
				begin = end;
				end = NextPaintToken( Document, begin, line.End, out klass, out inSelection );
			}

			// Draw EOF mark
			if( DrawsEofMark && line.End == Document.Length )
			{
				DebugUtl.Assert( line.Begin <= line.End );
				if( line.Begin == line.End
					|| (0 < line.End && TextUtil.IsEolChar(Document[line.End-1]) == false) )
				{
					DrawEofMark( g, ref pos );
				}
			}

			// Fill right of the line text
			if( pos.X < clipRect.Right )
			{
				// To prevent drawing line number area, make drawing pos to text area's left if the
				// line end does not exceed left of text area
				if( pos.X < ScrXofTextArea )
					pos.X = ScrXofTextArea;
				g.BackColor = ColorScheme.BackColor;
				g.FillRectangle( pos.X, pos.Y, clipRect.Right-pos.X, LineSpacing );
			}

			// If this line is wider than the width of virtual space, calculate full width of this
			// line and make it the width of virtual space.
			var virPos = ScreenToVirtual( pos );
			if( TextAreaWidth < virPos.X + (VisibleSize.Width >> 3) )
			{
				// Remember length of this line if it is the longest ever
				var lineContent = Document.GetText( line.Begin, line.End );
				var lineWidth = MeasureTokenEndX( g, lineContent, 0 );
				if( longestLineLength < lineWidth )
					longestLineLength = lineWidth;
			}

			// Draw graphics at left of text
			DrawLeftOfLine( g, pos.Y, lineIndex+1, true );
		}
		#endregion
	}
}
