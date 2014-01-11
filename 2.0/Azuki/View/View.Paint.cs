// file: View.Paint.cs
// brief: Common painting logic
//=========================================================
//DEBUG//#define DRAW_SLOWLY
using System;
using System.Drawing;
using Debug = System.Diagnostics.Debug;
using StringBuilder = System.Text.StringBuilder;

namespace Sgry.Azuki
{
	abstract partial class View
	{
		/// <summary>
		/// Paints content to a graphic device.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="clipRect">clipping rectangle that covers all invalidated region (in client area coordinate)</param>
		public abstract void Paint( IGraphics g, Rectangle clipRect );

		#region Drawing graphical units of view
		/// <summary>
		/// Paints a token including special characters.
		/// </summary>
		protected void DrawToken( IGraphics g, Document doc, int tokenIndex,
								  string token, CharClass klass,
								  ref Point tokenPos, ref Point tokenEndPos,
								  ref Rectangle clipRect, bool inSelection )
		{
			Debug.Assert( g != null, "IGraphics must not be null." );
			Debug.Assert( token != null, "given token is null." );
			Debug.Assert( 0 < token.Length, "given token is empty." );
			var textPos = tokenPos;
			Color foreColor, backColor;
			TextDecoration[] decorations;
			uint markingBitMask;

			// Calculate top coordinate of text
			textPos.Y += (LinePadding >> 1);

#			if DRAW_SLOWLY
			if(!WinForms.WinApi.IsKeyDownAsync(System.Windows.Forms.Keys.ControlKey))
			{ g.BackColor=Color.Red; g.FillRectangle(tokenPos.X, tokenPos.Y, 2, LineHeight); DebugUtl.Sleep(400); }
#			endif

			// Get drawing style for this token
			ColorFromCharClass( ColorScheme, klass, inSelection, out foreColor, out backColor );
			g.BackColor = backColor;
			markingBitMask = doc.GetMarkingBitMaskAt( tokenIndex );
			decorations = ColorScheme.GetMarkingDecorations( markingBitMask );

			// Overwrite bg color if this token was decorated with solid background decoration
			if( inSelection == false )
			{
				foreach( var decoration in decorations )
				{
					var bgColorDecoration = decoration as BgColorTextDecoration;
					if( bgColorDecoration != null )
					{
						g.BackColor = backColor
									= bgColorDecoration.BackgroundColor;
					}
				}
			}

			//--- Draw graphic ---
			// Space
			if( token == " " )
			{
				g.FillRectangle( tokenPos.X, tokenPos.Y, _SpaceWidth, LineSpacing );
				if( DrawsSpace )
				{
					g.ForeColor = ColorScheme.WhiteSpaceColor;
					g.DrawRectangle( tokenPos.X + (_SpaceWidth >> 1) - 1,
									 textPos.Y + (_LineHeight >> 1),
									 1,
									 1 );
				}
			}
			// Full-width space
			else if( token == "\x3000" )
			{
				// Calc desired foreground graphic position
				var graLeft = tokenPos.X + 2;
				var graWidth = _FullSpaceWidth - 5;
				var graTop = (textPos.Y + _LineHeight / 2) - (graWidth / 2);
				var graBottom = (textPos.Y + _LineHeight / 2) + (graWidth / 2);

				// Draw
				g.FillRectangle( tokenPos.X, tokenPos.Y, _FullSpaceWidth, LineSpacing );
				if( DrawsFullWidthSpace )
				{
					g.ForeColor = ColorScheme.WhiteSpaceColor;
					g.DrawRectangle( graLeft, graTop, graWidth, graBottom-graTop );
				}
			}
			// Tab
			else if( token == "\t" )
			{
				int fgTop = textPos.Y + (_LineHeight * 1 / 3);
				int fgBottom = textPos.Y + (_LineHeight * 2 / 3);

				// Calc next tab stop (calc in virtual space and convert it to screen coordinate)
				var tokenVirPos = ScreenToVirtual( tokenPos );
				var bgRight = CalcNextTabStop( tokenVirPos.X, TabWidthInPx );
				bgRight -= ScrollPosX - ScrXofTextArea;
				
				// Calc desired foreground graphic position
				var fgLeft = tokenPos.X + 2;
				var fgRight = bgRight - 2;
				var bgLeft = tokenPos.X;

				// Draw
				g.FillRectangle( bgLeft, tokenPos.Y, bgRight-bgLeft, LineSpacing );
				if( DrawsTab )
				{
					g.ForeColor = ColorScheme.WhiteSpaceColor;
					g.DrawLine( fgLeft, fgBottom, fgRight, fgBottom );
					g.DrawLine( fgRight, fgBottom, fgRight, fgTop );
				}
			}
			// EOL-Code
			else if( TextUtil.IsEolChar(token, 0) )
			{
				if( inSelection == false )
					g.BackColor = ColorScheme.BackColor;

				// Draw background
				var width = EolCodeWidthInPx;
				g.FillRectangle( tokenPos.X, tokenPos.Y, width, LineSpacing );

				// Draw foreground
				if( DrawsEolCode )
				{
					// Calc metric
					int middleY = tokenPos.Y + (LineSpacing >> 1);
					int middleX = tokenPos.X + (width >> 1); // width/2
					int halfSpaceWidth = (_SpaceWidth >> 1); // _SpaceWidth/2
					int left = tokenPos.X + 1;
					int right = tokenPos.X + width - 2;
					int bottom = middleY + (width >> 1);

					// Draw EOL char's graphic
					g.ForeColor = ColorScheme.EolColor;
					if( token == "\r" ) // CR (left arrow)
					{
						g.DrawLine( left, middleY, left+halfSpaceWidth, middleY-halfSpaceWidth );
						g.DrawLine( left, middleY, tokenPos.X+width-2, middleY );
						g.DrawLine( left, middleY, left+halfSpaceWidth, middleY+halfSpaceWidth );
					}
					else if( token == "\n" ) // LF (down arrow)
					{
						g.DrawLine( middleX, bottom,
									middleX - halfSpaceWidth, bottom - halfSpaceWidth );
						g.DrawLine( middleX, middleY-(width>>1), middleX, bottom );
						g.DrawLine( middleX, bottom,
									middleX + halfSpaceWidth, bottom - halfSpaceWidth );
					}
					else // CRLF (snapped arrow)
					{
						g.DrawLine( right, middleY-(width>>1), right, middleY+2 );

						g.DrawLine( left, middleY+2,
									left + halfSpaceWidth, middleY + 2 - halfSpaceWidth );
						g.DrawLine( right, middleY+2, left, middleY+2 );
						g.DrawLine( left, middleY+2,
									left + halfSpaceWidth, middleY + 2 + halfSpaceWidth );
					}
				}
			}
			// matched bracket
			else if( HighlightsMatchedBracket
				&& doc.CaretIndex == doc.AnchorIndex // ensure nothing is selected
				&& IsMatchedBracket(tokenIndex) )
			{
				var fore = ColorScheme.MatchedBracketFore;
				var back = ColorScheme.MatchedBracketBack;
				if( fore == Color.Transparent )
					fore = foreColor;
				if( back == Color.Transparent )
					back = backColor;
				g.BackColor = back;

				g.FillRectangle( tokenPos.X, tokenPos.Y, tokenEndPos.X-tokenPos.X, LineSpacing );
				g.DrawText( token, ref textPos, fore );
			}
			else
			{
				// Draw normal visible text
				g.FillRectangle( tokenPos.X, tokenPos.Y, tokenEndPos.X-tokenPos.X, LineSpacing );
				g.DrawText( token, ref textPos, foreColor );
			}

			// Decorate token
			foreach( var decoration in decorations )
			{
				var ulDecoration = decoration as UnderlineTextDecoration;
				var olDecoration = decoration as OutlineTextDecoration;
				if( ulDecoration != null )
				{
					DrawToken_Underline( g, tokenPos, tokenEndPos,
										 ulDecoration, foreColor );
				}
				else if( olDecoration != null )
				{
					DrawToken_Outline( g, doc, token, tokenIndex, tokenPos, tokenEndPos,
									   olDecoration, foreColor, markingBitMask );
				}
			}
		}

		void DrawToken_Underline( IGraphics g, Point tokenPos, Point tokenEndPos,
								  UnderlineTextDecoration decoration, Color currentForeColor )
		{
			Debug.Assert( g != null );
			Debug.Assert( decoration != null );

			if( decoration.LineStyle == LineStyle.None )
				return;

			// prepare drawing
			if( decoration.LineColor == Color.Transparent )
			{
				g.ForeColor = currentForeColor;
				g.BackColor = currentForeColor;
			}
			else
			{
				g.ForeColor = decoration.LineColor;
				g.BackColor = decoration.LineColor;
			}

			// draw underline
			if( decoration.LineStyle == LineStyle.Dotted )
			{
				int dotSize = (_Font.Size / 13) + 1;
				int dotSpacing = dotSize << 1;
				int offsetX = tokenPos.X % dotSpacing;
				for( int x=tokenPos.X-offsetX; x<tokenEndPos.X; x += dotSpacing )
				{
					g.FillRectangle( x, tokenPos.Y + LineHeight - dotSize, dotSize, dotSize );
				}
			}
			else if( decoration.LineStyle == LineStyle.Dashed )
			{
				int lineWidthSize = (_Font.Size / 13) + 1;
				int lineLength = lineWidthSize + (lineWidthSize << 2);
				int lineSpacing = lineWidthSize << 3;
				int offsetX = tokenPos.X % lineSpacing;
				for( int x=tokenPos.X-offsetX; x<tokenEndPos.X; x +=lineSpacing )
				{
					g.FillRectangle( x, tokenPos.Y + LineHeight - lineWidthSize,
									 lineLength, lineWidthSize );
				}
			}
			else if( decoration.LineStyle == LineStyle.Waved )
			{
				int lineWidthSize = (_Font.Size / 24) + 1;
				int waveHeight = (_Font.Size / 6) + 1;
				int offsetX = tokenPos.X % (waveHeight << 1);

				int valleyY = tokenPos.Y + LineHeight - lineWidthSize;
				int ridgeY = valleyY - waveHeight;
				for( int x=tokenPos.X-offsetX; x<tokenEndPos.X; x += (waveHeight<<1) )
				{
					int ridgeX = x + waveHeight;
					int valleyX = ridgeX + waveHeight;
					g.DrawLine( x, valleyY, ridgeX, ridgeY );
					g.DrawLine( ridgeX, ridgeY, valleyX, valleyY );
				}
			}
			else if( decoration.LineStyle == LineStyle.Double )
			{
				int lineWidth = (_Font.Size / 24) + 1;

				g.FillRectangle( tokenPos.X, tokenPos.Y + LineHeight - (3 * lineWidth),
								 tokenEndPos.X, lineWidth );

				g.FillRectangle( tokenPos.X, tokenPos.Y + LineHeight - lineWidth,
								 tokenEndPos.X, lineWidth );
			}
			else if( decoration.LineStyle == LineStyle.Solid )
			{
				int lineWidth = (_Font.Size / 24) + 1;
				g.FillRectangle( tokenPos.X, tokenPos.Y + LineHeight - lineWidth,
								 tokenEndPos.X, lineWidth );
			}
		}

		void DrawToken_Outline( IGraphics g, Document doc, string token, int tokenIndex,
								Point tokenPos, Point tokenEndPos,
								OutlineTextDecoration decoration,
								Color currentForeColor, uint markingBitMask
			)
		{
			Debug.Assert( g != null );
			Debug.Assert( doc != null );
			Debug.Assert( 0 <= tokenIndex && tokenIndex < doc.Length );
			Debug.Assert( token != null );
			Debug.Assert( decoration != null );

			int tokenEndIndex = tokenIndex + token.Length;

			// prepare drawing
			if( decoration.LineColor == Color.Transparent )
				g.BackColor = currentForeColor;
			else
				g.BackColor = decoration.LineColor;
			int w = (_Font.Size / 24) + 1;
			var rect = new Rectangle( tokenPos.X,
									  tokenPos.Y + 1,
									  tokenEndPos.X - tokenPos.X,
									  LineSpacing - w - 2 );

			// draw top line
			g.FillRectangle( rect.Left, rect.Top, rect.Width, w );

			// draw right line if previous character is marked same value
			if( doc.Length <= tokenEndIndex
				|| doc.GetMarkingBitMaskAt(tokenEndIndex) != markingBitMask )
			{
				g.FillRectangle( rect.Right - w, rect.Top, w, rect.Height );
			}

			// draw bottom line
			g.FillRectangle( rect.Left, rect.Bottom - w, rect.Width, w );

			// draw left line
			if( tokenIndex-1 < 0
				|| doc.GetMarkingBitMaskAt(tokenIndex-1) != markingBitMask )
			{
				g.FillRectangle( rect.Left, rect.Top, w, rect.Height );
			}
		}

		/// <summary>
		/// Draws underline for the line specified by it's Y coordinate.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="lineTopY">Y-coordinate of the target line.</param>
		/// <param name="color">Color to be used for drawing the underline.</param>
		protected virtual void DrawUnderLine( IGraphics g, int lineTopY, Color color )
		{
			if( lineTopY < 0 )
				return;

			DebugUtl.Assert( (lineTopY % LineSpacing) == (ScrYofTextArea % LineSpacing),
							 "lineTopY:" + lineTopY + ", LineSpacing:" + LineSpacing
							 + ", ScrYofTextArea:" + ScrYofTextArea );

			// calculate position to underline
			int bottom = lineTopY + LineHeight + (LinePadding >> 1);

			// determine color of the underline
			if( _UI.Focused )
				g.ForeColor = color;
			else
				g.ForeColor = ColorScheme.BackColor;

			// draw under line
			g.DrawLine( ScrXofTextArea, bottom, _VisibleSize.Width, bottom );
		}

		/// <summary>
		/// Draws dirt bar.
		/// </summary>
		protected void DrawDirtBar( IGraphics g, int lineTopY, int logicalLineIndex )
		{
			Debug.Assert( ((lineTopY-ScrYofTextArea) % LineSpacing) == 0,
						  "((lineTopY-ScrYofTextArea) % LineSpacing) is not 0 but "
						  + (lineTopY-ScrYofTextArea) % LineSpacing );
			DirtyState dirtyState;
			Color backColor;

			// Get dirty state of the line
			if( 0 <= logicalLineIndex && logicalLineIndex < Document.Lines.Count )
				dirtyState = Document.Lines[logicalLineIndex].DirtyState;
			else
				dirtyState = DirtyState.Clean;

			// Choose background color
			if( dirtyState == DirtyState.Saved )
			{
				backColor = ColorScheme.CleanedLineBar;
				if( backColor == Color.Transparent )
					backColor = BackColorOfLineNumber( ColorScheme );
			}
			else if( dirtyState == DirtyState.Dirty )
			{
				backColor = ColorScheme.DirtyLineBar;
				if( backColor == Color.Transparent )
					backColor = BackColorOfLineNumber( ColorScheme );
			}
			else
			{
				backColor = BackColorOfLineNumber( ColorScheme );
			}

			// Fill
			g.BackColor = backColor;
			g.FillRectangle( ScrXofDirtBar, lineTopY, DirtBarWidth, LineSpacing );
		}

		/// <summary>
		/// Draws line number area at specified line.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="lineTopY">Y-coordinate of the target line.</param>
		/// <param name="lineNumber">line number to be drawn.</param>
		/// <param name="drawsText">specify true if line number text should be drawn.</param>
		protected void DrawLeftOfLine( IGraphics g, int lineTopY, int lineNumber, bool drawsText )
		{
			DebugUtl.Assert( (lineTopY % LineSpacing) == (ScrYofTextArea % LineSpacing),
							 "lineTopY:" + lineTopY + ", LineSpacing:" + LineSpacing
							 + ", ScrYofTextArea:" + ScrYofTextArea );
			var pos = new Point( ScrXofLineNumberArea, lineTopY );
			
			// Fill line number area
			if( ShowLineNumber )
			{
				g.BackColor = BackColorOfLineNumber( ColorScheme );
				g.FillRectangle( ScrXofLineNumberArea, pos.Y, LineNumAreaWidth, LineSpacing );
			}

			// Fill dirt bar
			if( ShowsDirtBar )
			{
				DrawDirtBar( g, lineTopY, lineNumber-1 );
			}
			
			// Fill left margin area
			if( 0 < LeftMargin )
			{
				g.BackColor = ColorScheme.BackColor;
				g.FillRectangle( ScrXofLeftMargin, pos.Y, LeftMargin, LineSpacing );
			}
			
			// Draw line number text
			if( ShowLineNumber && drawsText )
			{
				var lineNumText = lineNumber.ToString();
				pos.X = ScrXofDirtBar - g.MeasureText( lineNumText ).Width - LineNumberAreaPadding;
				var textPos = pos;
				textPos.Y += (LinePadding >> 1);
				g.DrawText( lineNumText, ref textPos, ForeColorOfLineNumber(ColorScheme) );
			}

			// Draw margin line between the line number area and text area
			if( ShowLineNumber || ShowsDirtBar )
			{
				pos.X = ScrXofLeftMargin - 1;
				g.ForeColor = ForeColorOfLineNumber( ColorScheme );
				g.DrawLine( pos.X, pos.Y, pos.X, pos.Y+LineSpacing );
			}
		}

		/// <summary>
		/// Draws horizontal ruler on top of the text area.
		/// </summary>
		protected void DrawHRuler( IGraphics g, Rectangle clipRect )
		{
			string columnNumberText;
			int lineX, rulerIndex;
			int leftMostLineX, leftMostRulerIndex;

			if( ShowsHRuler == false || ScrYofTopMargin < clipRect.Y )
				return;

			g.SetClipRect( clipRect );

			// Fill ruler area
			g.ForeColor = ForeColorOfLineNumber( ColorScheme );
			g.BackColor = BackColorOfLineNumber( ColorScheme );
			g.FillRectangle( 0, ScrYofHRuler, VisibleSize.Width, HRulerHeight );

			// If clipping rectangle covers left of text area,
			// reset clipping rect that does not covers left of text area
			if( clipRect.X < ScrXofLeftMargin )
			{
				clipRect.Width -= ScrXofLeftMargin - clipRect.X;
				clipRect.X = ScrXofLeftMargin;
				g.RemoveClipRect();
				g.SetClipRect( clipRect );
			}

			// Calculate first line to be drawn
			leftMostRulerIndex = ScrollPosX / HRulerUnitWidth;
			leftMostLineX = ScrXofTextArea + (leftMostRulerIndex * HRulerUnitWidth) - ScrollPosX;
			while( leftMostLineX < clipRect.Left )
			{
				leftMostRulerIndex++;
				leftMostLineX += HRulerUnitWidth;
			}

			// Align beginning column index to largest multiple of 10
			// to ensure column number graphic will not be cut off
			var indexDiff = (leftMostRulerIndex % 10);
			if( 1 <= indexDiff && indexDiff <= 5 )
			{
				leftMostRulerIndex -= indexDiff;
				leftMostLineX -= indexDiff * HRulerUnitWidth;
			}

			// Draw lines on the ruler
			g.FontInfo = _HRulerFont;
			lineX = leftMostLineX;
			rulerIndex = leftMostRulerIndex;
			while( lineX < clipRect.Right )
			{
				// Draw ruler line
				if( (rulerIndex % 10) == 0 )
				{
					// Draw largest line
					g.DrawLine( lineX, ScrYofHRuler, lineX, ScrYofHRuler+HRulerHeight );

					// Draw column text
					columnNumberText = (rulerIndex / 10).ToString();
					var pos = new Point( lineX+2, ScrYofHRuler );
					g.DrawText( columnNumberText, ref pos, ForeColorOfLineNumber(ColorScheme) );
				}
				else if( (rulerIndex % 5) == 0 )
				{
					// Draw middle-length line
					g.DrawLine( lineX, ScrYofHRuler+_HRulerY_5, lineX, ScrYofHRuler+HRulerHeight );
				}
				else
				{
					// Draw smallest line
					g.DrawLine( lineX, ScrYofHRuler+_HRulerY_1, lineX, ScrYofHRuler+HRulerHeight );
				}

				// Go to next ruler line
				rulerIndex++;
				lineX += HRulerUnitWidth;
			}
			g.FontInfo = _Font;

			// Draw bottom border line
			g.DrawLine( ScrXofLeftMargin-1, ScrYofHRuler + HRulerHeight - 1,
						VisibleSize.Width, ScrYofHRuler + HRulerHeight - 1 );

			// Draw indicator of caret column
			g.BackColor = ColorScheme.ForeColor;
			if( HRulerIndicatorType == HRulerIndicatorType.Position )
			{
				int indicatorWidth = 2;

				// Calculate indicator region
				var caretPos = VirtualToScreen( GetVirtualPos(g, Document.CaretIndex) );
				if( caretPos.X < ScrXofTextArea )
				{
					indicatorWidth -= ScrXofTextArea - caretPos.X;
					caretPos.X = ScrXofTextArea;
				}

				// Draw indicator
				if( 0 < indicatorWidth )
				{
					g.FillRectangle( caretPos.X, ScrYofHRuler, indicatorWidth, HRulerHeight );
				}

				// Remember lastly drawn ruler bar position
				PerDocParam.PrevHRulerVirX = caretPos.X - ScrXofTextArea + ScrollPosX;
			}
			else if( HRulerIndicatorType == HRulerIndicatorType.CharCount )
			{
				// Calculate indicator region
				var caretPos = GetTextPosition( Document.CaretIndex );
				var indicatorWidth = HRulerUnitWidth - 1;
				var indicatorX = leftMostLineX
								 + (caretPos.Column - leftMostRulerIndex) * HRulerUnitWidth;
				if( indicatorX < ScrXofTextArea )
				{
					indicatorWidth -= ScrXofTextArea - indicatorX;
					indicatorX = ScrXofTextArea;
				}
				
				// Draw indicator
				if( 0 < indicatorWidth )
					g.FillRectangle( indicatorX+1, ScrYofHRuler, indicatorWidth, HRulerHeight-1 );

				// Remember lastly filled ruler segmentr position
				PerDocParam.PrevHRulerVirX = indicatorX - ScrXofTextArea + ScrollPosX;
			}
			else// if( HRulerIndicatorType == HRulerIndicatorType.Segment )
			{
				// Calculate indicator region
				int indicatorWidth = HRulerUnitWidth - 1;
				var indicatorPos = GetVirtualPos( g, Document.CaretIndex );
				indicatorPos.X -= (indicatorPos.X % HRulerUnitWidth);
				indicatorPos = VirtualToScreen( indicatorPos );
				if( indicatorPos.X < ScrXofTextArea )
				{
					indicatorWidth -= ScrXofTextArea - indicatorPos.X;
					indicatorPos.X = ScrXofTextArea;
				}

				// Draw indicator
				if( 0 < indicatorWidth )
				{
					g.FillRectangle( indicatorPos.X + 1, ScrYofHRuler,
									 indicatorWidth, HRulerHeight - 1 );
				}

				// Remember lastly filled ruler segmentr position
				PerDocParam.PrevHRulerVirX = indicatorPos.X - ScrXofTextArea + ScrollPosX;
			}

			g.RemoveClipRect();
		}

		/// <summary>
		/// Draws top margin.
		/// </summary>
		protected void DrawTopMargin( IGraphics g )
		{
			// Fill area above the line-number area [copied from DrawLineNumber]
			g.BackColor = BackColorOfLineNumber( ColorScheme );
			g.FillRectangle( ScrXofLineNumberArea, ScrYofTopMargin,
							 ScrXofTextArea-ScrXofLineNumberArea, TopMargin );
			
			// Fill left margin area [copied from DrawLineNumber]
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( ScrXofLeftMargin, ScrYofTopMargin, LeftMargin, TopMargin );

			// Draw margin line between the line number area and text area [copied from DrawLineNumber]
			int x = ScrXofLeftMargin - 1;
			g.ForeColor = ForeColorOfLineNumber( ColorScheme );
			g.DrawLine( x, ScrYofTopMargin, x, ScrYofTopMargin+TopMargin );

			// Fill area above the text area
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( ScrXofTextArea, ScrYofTopMargin,
							 VisibleSize.Width - ScrXofTextArea, TopMargin );
		}

		/// <summary>
		/// Draws EOF mark.
		/// </summary>
		protected void DrawEofMark( IGraphics g, ref Point pos )
		{
			g.BackColor = ColorScheme.BackColor;
			int margin = (_SpaceWidth >> 2);

			// Fill background
			int width = g.MeasureText( "[EOF]" ).Width;
			g.FillRectangle( pos.X, pos.Y, width+margin, LineSpacing );

			// Calculate text position
			pos.X += margin;
			var textPos = pos;
			textPos.Y += (LinePadding >> 1);

			// Draw text
			g.DrawText( "[EOF]", ref textPos, ColorScheme.EofColor );
			pos.X += width;
		}
		#endregion

		#region Special updating logic
		protected void UpdateHRuler( IGraphics g )
		{
			if( ShowsHRuler == false )
				return;

			Rectangle oldUpdateRect;
			Rectangle newUdpateRect;
			var doc = Document;

			if( HRulerIndicatorType == HRulerIndicatorType.Position )
			{
				// Get virtual position of the new caret
				var newCaretScreenPos = VirtualToScreen( GetVirtualPos(g, doc.CaretIndex) );

				// Get previous screen position of the caret
				int oldCaretX = PerDocParam.PrevHRulerVirX + ScrXofTextArea - ScrollPosX;
				if( oldCaretX == newCaretScreenPos.X )
				{
					return; // horizontal poisition of the caret not changed
				}

				// Calculate indicator rectangle for old caret position
				oldUpdateRect = new Rectangle( oldCaretX, ScrYofHRuler,
											   2, HRulerHeight );

				// Calculate indicator rectangle for new caret position
				newUdpateRect = new Rectangle( newCaretScreenPos.X, ScrYofHRuler,
											   2, HRulerHeight );
			}
			else if( HRulerIndicatorType == HRulerIndicatorType.CharCount )
			{
				// Calculate new segment of horizontal ruler
				var newCaretPos = GetTextPosition( doc.CaretIndex );
				var newSegmentX = (newCaretPos.Column * HRulerUnitWidth)
								  + ScrXofTextArea
								  - ScrollPosX;

				// Calculate previous segment of horizontal ruler
				var oldSegmentX = PerDocParam.PrevHRulerVirX + ScrXofTextArea - ScrollPosX;
				if( oldSegmentX == newSegmentX )
				{
					return; // horizontal poisition of the caret not changed
				}

				// Calculate indicator rectangle for old caret position
				oldUpdateRect = new Rectangle( oldSegmentX, ScrYofHRuler,
											   HRulerUnitWidth, HRulerHeight );

				// calculate indicator rectangle for new caret position
				newUdpateRect = new Rectangle( newSegmentX, ScrYofHRuler,
											   HRulerUnitWidth, HRulerHeight );
			}
			else// if( HRulerIndicatorType == HRulerIndicatorType.Segment )
			{
				int oldSegmentX, newSegmentX;

				// Get virtual position of the new caret
				var newCaretScreenPos = VirtualToScreen( GetVirtualPos(g, doc.CaretIndex) );

				// Calculate new segment of horizontal rulse
				int leftMostRulerIndex = ScrollPosX / HRulerUnitWidth;
				int leftMostLineX = ScrXofTextArea
									+ (leftMostRulerIndex * HRulerUnitWidth)
									- ScrollPosX;
				newSegmentX = leftMostLineX;
				while( newSegmentX+HRulerUnitWidth <= newCaretScreenPos.X )
				{
					newSegmentX += HRulerUnitWidth;
				}

				// Calculate previous segment of horizontal ruler
				oldSegmentX = PerDocParam.PrevHRulerVirX + ScrXofTextArea - ScrollPosX;
				if( oldSegmentX == newSegmentX )
				{
					return; // segment was not changed
				}

				// Calculate invalid rectangle
				oldUpdateRect = new Rectangle( oldSegmentX, ScrYofHRuler,
												HRulerUnitWidth, HRulerHeight );
				newUdpateRect = new Rectangle( newSegmentX, ScrYofHRuler,
												HRulerUnitWidth, HRulerHeight );
			}

			// not invalidate but DRAW old and new indicator here
			// (because if all invalid rectangles was combined,
			// invalidating area in horizontal ruler makes
			// large invalid rectangle and has bad effect on performance,
			// especially on mobile devices.)
			DrawHRuler( g, oldUpdateRect );
			DrawHRuler( g, newUdpateRect );
		}
		#endregion

		#region Measuring paint text token
		/// <summary>
		/// Calculates x-coordinate of the right end of given token drawed at specified position
		/// with specified tab-width.
		/// </summary>
		internal int MeasureTokenEndX( IGraphics g, string token, int virX )
		{
			int dummy;
			int rightLimitX = Int32.MaxValue;

			// ensure "(rightLimitX - virX) < Int32.MaxValue"
			if( virX < 0 )
			{
				rightLimitX = Int32.MaxValue + virX;
			}
			return MeasureTokenEndX( g, token, virX, rightLimitX, out dummy );
		}

		/// <summary>
		/// Calculates x-coordinate of the right end of given token
		/// drawed at specified position with specified tab-width.
		/// </summary>
		protected int MeasureTokenEndX( IGraphics g, string token, int virX, int rightLimitX,
										out int drawableLength )
		{
			int x = virX;
			int relDLen; // relatively calculated drawable length
			int subTokenWidth;
			bool hitRightLimit;

			drawableLength = 0;
			if( token.Length == 0 )
			{
				return x;
			}

			var subToken = new StringBuilder( token.Length );
			for( int i=0; i<token.Length; i++ )
			{
				if( token[i] == '\t' )
				{
					//--- Found a tab ---
					// Calculate drawn length of cached characters
					hitRightLimit = MeasureTokenEndX_TreatSubToken( g, i, subToken, rightLimitX,
																	ref x, ref drawableLength );
					if( hitRightLimit )
					{
						// before this tab, cached characters already hit the limit.
						return x;
					}

					// Calc next tab stop
					subTokenWidth = CalcNextTabStop( x, TabWidthInPx );
					if( rightLimitX <= subTokenWidth )
					{
						// This tab hit the right limit.
						Debug.Assert( drawableLength == i );
						drawableLength = i;
						return x;
					}
					drawableLength++;
					x = subTokenWidth;
				}
				else if( TextUtil.IsEolChar(token, i) )
				{
					//--- Detected an EOL char ---
					// Calculate drawn length of cached characters
					hitRightLimit = MeasureTokenEndX_TreatSubToken( g, i, subToken, rightLimitX,
																	ref x, ref drawableLength );
					if( hitRightLimit )
					{
						// Before this EOL char, cached characters already hit the limit.
						return x;
					}

					// Check whether this EOL code can be drawn or not
					if( rightLimitX <= x + EolCodeWidthInPx )
					{
						// This EOL code hit the right limit.
						return x;
					}
					x += EolCodeWidthInPx;

					// Treat this EOL code
					drawableLength++;
					if( token[i] == '\r'
						&& i+1 < token.Length && token[i+1] == '\n' )
					{
						drawableLength++;
					}
					return x;
				}
				else
				{
					if( 64 < subToken.Length )
					{
						// Pretty long text was cached.
						// calculate its width and check whether drawable or not
						hitRightLimit = MeasureTokenEndX_TreatSubToken( g, i, subToken,
																		rightLimitX,
																		ref x,
																		ref drawableLength );
						if( hitRightLimit )
						{
							return x; // hit the right limit
						}
					}

					// Append one grapheme cluster
					subToken.Append( token[i] );
					while( TextUtil.IsNotDividableIndex(token, i+1) )
					{
						subToken.Append( token[i+1] );
						i++;
					}
				}
			}

			// Calc last sub-token
			if( 0 < subToken.Length )
			{
				x += g.MeasureText( subToken.ToString(), rightLimitX-x, out relDLen ).Width;
				if( relDLen < subToken.Length )
				{
					drawableLength = token.Length - (subToken.Length - relDLen);
					Debug.Assert( TextUtil.IsNotDividableIndex(token, drawableLength) == false );
					return x; // hit the right limit.
				}
				drawableLength += subToken.Length;
			}
			Debug.Assert( TextUtil.IsNotDividableIndex(token, drawableLength) == false );

			// Whole part of the given token can be drawn at given width.
			return x;
		}

		/// <returns>true if measured right poisition hit the limit.</returns>
		static bool MeasureTokenEndX_TreatSubToken( IGraphics gra, int i, StringBuilder subToken,
													int rightLimitX, ref int x,
													ref int drawableLength )
		{
			int relDLen;

			if( subToken.Length == 0 )
				return false;

			var subTokenWidth = gra.MeasureText( subToken.ToString(), rightLimitX-x, out relDLen ).Width;
			if( relDLen < subToken.Length )
			{
				// given width is too narrow to draw this sub-token.
				// chop after the limit and re-calc subtoken's width
				drawableLength = i - (subToken.Length - relDLen);
				x += gra.MeasureText( subToken.ToString(0, relDLen) ).Width;
				subToken.Length = 0;
				return true;
			}
			Debug.Assert( TextUtil.IsNotDividableIndex(subToken.ToString(), relDLen) == false );

			x += subTokenWidth;
			drawableLength += subToken.Length;
			subToken.Length = 0;

			return false;
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Calculates end index of a drawing token at longest case
		/// according to selection state etc.
		/// </summary>
		int CalcTokenEndAtMost( Document doc,
								int index,
								int nextLineHead,
								out bool inSelection )
		{
			DebugUtl.Assert( doc != null );
			DebugUtl.Assert( index < doc.Length );
			DebugUtl.Assert( index<nextLineHead && nextLineHead<=doc.Length );
			const int MaxPaintTokenLen = 128;
			int selBegin, selEnd;

			// Get selection range on the line
			doc.GetSelection( out selBegin, out selEnd );
			if( doc.RectSelectRanges != null )
			{
				int i;

				// Determine whether a part of the line is selected by the
				// rectangular selection or not, and get the selection range
				// in this line. After determining it, drawing logic will be
				// the same as case of normal selection.
				for( i=0; i<doc.RectSelectRanges.Length; i+=2 )
				{
					selBegin = doc.RectSelectRanges[i];
					selEnd = doc.RectSelectRanges[i+1];
					if( index <= selEnd && selEnd < nextLineHead )
					{
						break; // Selected.
					}
				}
				if( doc.RectSelectRanges.Length <= i )
				{
					// Not selected.
					selBegin = selEnd = Int32.MaxValue;
				}
			}

			if( index < selBegin )
			{
				// Token being drawn exist before a selection
				// so we must extract characters not in a selection range.
				inSelection = false;
				return Math.Min( Math.Min( selBegin,
										   nextLineHead ),
								 index + MaxPaintTokenLen );
			}
			else if( index < selEnd )
			{
				// Token being drawin exist in a selection
				// so we must extract characters in a selection range.
				inSelection = true;
				return Math.Min( Math.Min( selEnd,
										   nextLineHead ),
								 index + MaxPaintTokenLen );
			}
			else
			{
				// Token being drawin exist after a selection or there is no
				// characters selected so we don't need to care about selection
				inSelection = false;
				return Math.Min( nextLineHead,
								 index + MaxPaintTokenLen );
			}
		}

		/// <summary>
		/// Gets next token for painting.
		/// </summary>
		protected int NextPaintToken( Document doc, int index, int nextLineHead,
									  out CharClass out_klass, out bool out_inSelection )
		{
			DebugUtl.Assert( nextLineHead <= doc.Length, "param 'nextLineHead'(" + nextLineHead
							 + ") must not be greater than 'doc.Length'(" + doc.Length + ")." );

			char firstCh, ch;
			CharClass firstKlass, klass;
			uint firstMarkingBitMask, markingBitMask;

			out_inSelection = false;

			if( nextLineHead <= index )
			{
				out_klass = CharClass.Normal;
				return -1; // terminate outer loop
			}

			// Calculate how many chars should be drawn as one token
			var tokenEndLimit = CalcTokenEndAtMost( doc, index, nextLineHead,
													out out_inSelection );
			if( IsMatchedBracket(index) )
			{
				// If specified index is a bracket paired with a bracket at caret,
				// paint this single char
				out_klass = doc.GetCharClass( index );
				return index + 1;
			}

			// Get first char class and selection state
			firstCh = doc[ index ];
			firstKlass = doc.GetCharClass( index );
			firstMarkingBitMask = doc.GetMarkingBitMaskAt( index );
			out_klass = firstKlass;
			if( IsSpecialChar(firstCh) )
			{
				// Treat 1 special char as 1 token
				if( firstCh == '\r'
					&& index+1 < doc.Length
					&& doc[index+1] == '\n' )
					return index + 2;
				else
					return index + 1;
			}
			
			// Seek until token end appears
			while( index+1 < tokenEndLimit )
			{
				// Get next char
				index++;
				ch = doc[ index ];
				klass = doc.GetCharClass( index );
				markingBitMask = doc.GetMarkingBitMaskAt( index );

				if( IsSpecialChar(ch)							// special char
					|| IsMatchedBracket(index)					// matched bracket
					|| klass != firstKlass						// different character class
					|| markingBitMask != firstMarkingBitMask )	// different marking
				{
					return index;
				}
			}

			// Reached to the limit
			return tokenEndLimit;
		}

		/// <summary>
		/// Gets fore/back color pair from scheme according to char class.
		/// </summary>
		static void ColorFromCharClass( ColorScheme cs, CharClass klass, bool inSelection,
										out Color fore, out Color back )
		{
			cs.GetColor( klass, out fore, out back );

			if( inSelection )
			{
				back = cs.SelectionBack;
				if( cs.SelectionFore != Color.Transparent )
					fore = cs.SelectionFore;
			}

			if( fore == Color.Transparent )
				fore = cs.ForeColor;
			if( back == Color.Transparent )
				back = cs.BackColor;
		}

		static Color ForeColorOfLineNumber( ColorScheme cs )
		{
			return (cs.LineNumberFore != Color.Transparent) ? cs.LineNumberFore
															: cs.ForeColor;
		}

		static Color BackColorOfLineNumber( ColorScheme cs )
		{
			return (cs.LineNumberBack != Color.Transparent) ? cs.LineNumberBack
															: cs.BackColor;
		}

		static int CalcNextTabStop( int x, int tabWidthInPx )
		{
			DebugUtl.Assert( 0 < tabWidthInPx );
			return ((x / tabWidthInPx) + 1) * tabWidthInPx;
		}

		static bool IsSpecialChar( char ch )
		{
			if( ch == ' '
				|| ch == '\x3000' // full-width space
				|| ch == '\t'
				|| ch == '\r'
				|| ch == '\n' )
			{
				return true;
			}

			return false;
		}

		protected static int Min( int a, int b, int c, int d )
		{
			return Math.Min( a, Math.Min(b, Math.Min(c,d) ) );
		}

		protected static int Max( int a, int b, int c, int d )
		{
			return Math.Max( a, Math.Max(b, Math.Max(c,d) ) );
		}

		/// <summary>
		/// Returnes whether the index points to one of the paired matching bracket or not.
		/// </summary>
		bool IsMatchedBracket( int index )
		{
			Debug.Assert( 0 <= index && index < Document.Length );

			return ( 0 <= Array.IndexOf(PerDocParam.MatchedBracketIndexes, index) );
		}
		#endregion
	}
}
