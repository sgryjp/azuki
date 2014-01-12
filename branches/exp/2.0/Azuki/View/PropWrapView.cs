// file: PropWrapView.cs
// brief: Platform independent view (proportional, line-wrap).
//=========================================================
//DEBUG//#define SLHI_DEBUG
//DEBUG//#define DRAW_SLOWLY
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view implementation to display wrapped text with proportional font.
	/// </summary>
	class PropWrapView : PropView
	{
		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal PropWrapView( IUserInterfaceInternal ui )
			: base( ui )
		{
			PerDocParam.ScrollPosX = 0;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		internal PropWrapView( View other )
			: base( other )
		{
			PerDocParam.ScrollPosX = 0;
		}
		#endregion

		#region Properties
		public override ILineRangeList Lines
		{
			get{ return new WrappedLineRangeList(this); }
		}

		public override ILineRangeList RawLines
		{
			get{ return new WrappedRawLineRangeList(this); }
		}

		/// <summary>
		/// Gets or sets width of the virtual text area (line number area is not included).
		/// </summary>
		public override int TextAreaWidth
		{
			get{ return base.TextAreaWidth; }
			set
			{
				// ignore if negative integer given.
				// (This case may occur when minimizing window.
				// Note that lower boundary check will be done in
				// base.TextAreaWidth so there is no need to check it here.)
				if( value < 0 )
					return;

				if( base.TextAreaWidth != value )
				{
					using( var g = _UI.GetIGraphics() )
					{
						// update property
						base.TextAreaWidth = value;

						// update screen line head indexes
						var doc = Document;
						var text = doc.Text;
						SLHI.Clear();
						SLHI.Add( 0 );
						UpdateSLHI( g, 0, "", text );

						// re-calculate line index of caret and anchor
						PerDocParam.PrevCaretLine = Lines.AtOffset( CaretIndex ).LineIndex;
						PerDocParam.PrevAnchorLine = Lines.AtOffset( AnchorIndex ).LineIndex;

						// update desired column
						// (must be done after UpdateSLHI)
						SetDesiredColumn( g );
					}
				}
			}
		}

		/// <summary>
		/// Re-calculates and updates x-coordinate of the right end of the virtual text area.
		/// </summary>
		/// <param name="desiredX">X-coordinate of scroll destination desired.</param>
		/// <returns>The largest X-coordinate which Azuki can scroll to.</returns>
		protected override int ReCalcRightEndOfTextArea( int desiredX )
		{
			return TextAreaWidth - VisibleTextAreaSize.Width;
		}
		#endregion

		#region Drawing Options
		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
		public override int TabWidth
		{
			get{ return base.TabWidth; }
			set
			{
				base.TabWidth = value;

				// refresh SLHI
				var text = Document.Text;
				SLHI.Clear();
				SLHI.Add( 0 );
				UpdateSLHI( 0, "", text );
			}
		}
		#endregion

		#region Position / Index Conversion
		/// <exception cref="ArgumentOutOfRangeException"/>
		public override Point GetVirtualPos( IGraphics g, int lineIndex, int columnIndex )
		{
			Debug.Assert( g != null );
			if( lineIndex < 0 )
				throw new ArgumentOutOfRangeException( "lineIndex", "Specified index is out of"
													   + " range. (value:" + lineIndex + ")" );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Specified index is out of"
													   + " range. (value:" + columnIndex + ")" );

			// set value for when the columnIndex is 0
			var pos = new Point( 0, (lineIndex * LineSpacing) + (LinePadding >> 1) );

			// if the location is not the head of the line, calculate x-coord.
			if( 0 < columnIndex )
			{
				// Get partial content of the line which exists before the caret
				var line = RawLines[lineIndex];
				var leftPart = Document.GetText( line.Begin, line.Begin + columnIndex );

				// measure the characters
				pos.X = MeasureTokenEndX( g, leftPart, pos.X );
			}

			return pos;
		}

		public override int GetCharIndex( IGraphics g, Point pos )
		{
			int lineIndex, columnIndex;

			// calc line index
			lineIndex = (pos.Y / LineSpacing);
			if( lineIndex < 0 )
			{
				lineIndex = 0;
			}
			else if( SLHI.Count <= lineIndex
				&& Document.Lines.Count != 0 )
			{
				// the point indicates beyond the final line.
				// treat as if the final line was specified
				lineIndex = SLHI.Count - 1;
			}

			// calc column index
			columnIndex = 0;
			if( 0 < pos.X )
			{
				bool isWrapLine = false;
				int drawableTextLen;

				// get content of the line
				var line = RawLines[lineIndex];
				if( line.End+1 < Document.Length
					&& !TextUtil.IsEolChar(Document[line.End]) )
				{
					isWrapLine = true;
				}

				// calc maximum length of chars in line
				int rightLimitX = pos.X;
				int leftPartWidth = MeasureTokenEndX( g, line.Text, 0, rightLimitX,
													  out drawableTextLen );
				columnIndex = drawableTextLen;

				// if the location is nearer to the NEXT of that char,
				// we should return the index of next one.
				if( drawableTextLen < line.Text.Length )
				{
					var nextChar = line.Text[drawableTextLen].ToString();
					int nextCharWidth = MeasureTokenEndX( g, nextChar, leftPartWidth )
										- leftPartWidth;
					if( leftPartWidth + nextCharWidth/2 < pos.X ) // == "x of middle of next char"
					{											  // < "x of click in virtual text area"
						columnIndex = drawableTextLen + 1;
					}
				}
				// if the whole line can be drawn and is a wrapped line,
				// decrease column to avoid indicating invalid position
				else if( isWrapLine )
				{
					columnIndex--;
				}
			}

			return TextUtil.GetCharIndex( Document.Buffer, SLHI,
										  new TextPoint(lineIndex, columnIndex) );
		}

		/// <summary>
		/// Calculates screen line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public override TextPoint GetTextPosition( int charIndex )
		{
			if( charIndex < 0 || Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Invalid index was given (charIndex:"+charIndex+", document.Length:"+Document.Length+")." );

			return TextUtil.GetTextPosition( Document.Buffer, SLHI, charIndex );
		}

		/// <summary>
		/// Calculates char-index from screen line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public override int GetCharIndex( TextPoint position )
		{
			if( position.Line < 0 || Lines.Count < position.Line || position.Column < 0 )
				throw new ArgumentOutOfRangeException( "position", "Invalid index was given"
													   + " (position:" + position + ", line count:"
													   + Lines.Count + ")." );

			return TextUtil.GetCharIndex( Document.Buffer, SLHI, position );
		}
		#endregion

		#region Event Handlers
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
			using( var g = _UI.GetIGraphics() )
			{
				var doc = base.Document;
				var invalidRect1 = new Rectangle();
				var invalidRect2 = new Rectangle();

				// Get position of the replacement
				var oldCaretVirPos = GetVirtualPos( g, e.Index );
				if( IsWrappedLineHead(doc, SLHI, e.Index) )
				{
					oldCaretVirPos.Y -= LineSpacing;
					if( oldCaretVirPos.Y < 0 )
					{
						oldCaretVirPos.X = 0;
						oldCaretVirPos.Y = 0;
					}
				}

				// Update screen line head indexes
				var prevLineCount = Lines.Count;
				UpdateSLHI( g, e.Index, e.OldText, e.NewText );
#				if SLHI_DEBUG
				{
					var result = SLHI.ToString();
					SLHI.Clear(); SLHI.Add( 0 );
					UpdateSLHI( g, 0, String.Empty, Document.Text );
					if( result != SLHI.ToString() )
					{
						System.Windows.Forms.MessageBox.Show("sync error");
						Console.Error.WriteLine( result );
						Console.Error.WriteLine( SLHI );
						Console.Error.WriteLine();
					}
				}
#				endif

				// Update indicator graphic on horizontal ruler
				UpdateHRuler( g );

				// Invalidate the part at right of the old selection
				if( TextUtil.IsCombiningCharacter(e.OldText, 0)
					|| TextUtil.IsCombiningCharacter(e.NewText, 0) )
				{
					invalidRect1.X = 0; // [*1]
				}
				else
				{
					invalidRect1.X = oldCaretVirPos.X;
				}
				invalidRect1.Y = oldCaretVirPos.Y - (LinePadding >> 1);
				invalidRect1.Width = VisibleSize.Width - invalidRect1.X;
				invalidRect1.Height = LineSpacing;
				invalidRect1 = VirtualToScreen( invalidRect1 );

				// Invalidate all lines below caret if old or new text contains multiple lines
				var isMultiLine = TextUtil.IsMultiLine( e.NewText );
				if( prevLineCount != SLHI.Count || isMultiLine )
				{
					//NO_NEED//invalidRect2.X = 0;
					invalidRect2.Y = invalidRect1.Bottom;
					invalidRect2.Width = VisibleSize.Width;
					invalidRect2.Height = VisibleSize.Height - invalidRect2.Top;
				}
				else
				{
					// Invalidate this *logical* line if the replacement changed screen line count

					// Get position of the char at the end of the logical line
					var logLineEnd = doc.Lines.AtOffset( e.Index ).End;
					var logLineEndPos = VirtualToScreen( GetVirtualPos(g, logLineEnd) );
					var logLineBottom = logLineEndPos.Y - (LinePadding >> 1);

					// Make a rectangle that covers the logical line area
					//NO_NEED//invalidRect2.X = 0;
					invalidRect2.Y = invalidRect1.Bottom;
					invalidRect2.Width = VisibleSize.Width;
					invalidRect2.Height = (logLineBottom + LineSpacing) - invalidRect2.Top;
				}

				// Invalidate the range
				Invalidate( invalidRect1 );
				if( 0 < invalidRect2.Height )
				{
					//--- Multiple logical lines are affected ---
					Invalidate( invalidRect2 );
				}

				// Update left side of text area
				UpdateDirtBar( g, doc.Lines.AtOffset(e.Index).LineIndex );
				UpdateLineNumberWidth( g );

				//DO_NOT//base.HandleContentChanged( sender, e );
			}
		}

		/// <summary>
		/// Update dirt bar area.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="logLineIndex">dirt bar area for the line indicated by this index will be updated.</param>
		void UpdateDirtBar( IGraphics g, int logLineIndex )
		{
			var doc = Document;
			var logLine = doc.Lines[ logLineIndex ];

			// Get minimum and maximum Y in the screen coord
			var top = VirtualToScreen( GetVirtualPos(g, logLine.Begin) );
			var bottom = VirtualToScreen( GetVirtualPos(g, logLine.End) );
			if( bottom.Y < ScrYofTextArea )
			{
				return;
			}
			bottom.Y += LineSpacing;

			// Prevent to draw on horizontal ruler
			if( top.Y < ScrYofTextArea )
			{
				top.Y = ScrYofTextArea;
			}

			// Adjust drawing position for line padding
			// (move it up, a half of the height of line padding)
			top.Y -= (LinePadding >> 1);
			bottom.Y -= (LinePadding >> 1);

			// Overdraw dirt bar
			for( int y=top.Y; y<bottom.Y; y+=LineSpacing )
			{
				DrawDirtBar( g, y, logLineIndex );
			}
		}

		internal override void HandleDocumentChanged( Document prevDocument )
		{
			// update screen line head indexes if needed
			var doc = Document;
			if( PerDocParam.LastModifiedTime != doc.LastModifiedTime
				|| PerDocParam.LastFontHashCode != FontInfo.GetHashCode()
				|| PerDocParam.LastTextAreaWidth != TextAreaWidth )
			{
				DebugUtl.Assert( 0 < SLHI.Count );
				UpdateSLHI( 0, "", doc.Text );
			}

			base.HandleDocumentChanged( prevDocument );
		}
		#endregion

		#region Layout Logic
		/// <summary>
		/// Maintain line head indexes.
		/// </summary>
		/// <param name="index">The index of the place where replacement was occured.</param>
		/// <param name="oldText">The text which is removed by the replacement.</param>
		/// <param name="newText">The text which is inserted by the replacement.</param>
		void UpdateSLHI( int index, string oldText, string newText )
		{
			using( var g = _UI.GetIGraphics() )
				UpdateSLHI( g, index, oldText, newText );
		}

		/// <summary>
		/// Maintain line head indexes.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="index">The index of the place where replacement was occured.</param>
		/// <param name="oldText">The text which is removed by the replacement.</param>
		/// <param name="newText">The text which is inserted by the replacement.</param>
		void UpdateSLHI( IGraphics g, int index, string oldText, string newText )
		{
			Debug.Assert( 0 < this.TabWidth );
			var doc = Document;
			int delBeginL, delEndL;
			int reCalcBegin, reCalcEnd;
			int shiftBeginL;
			int diff = newText.Length - oldText.Length;
			int replaceEnd;

			// calculate where to recalculate SLHI from
			int firstDirtyLineIndex = TextUtil.GetLineIndexFromCharIndex( SLHI, index );
			if( 0 < firstDirtyLineIndex )
			{
				// we should always recalculate SLHI from previous line of the line replacement occured
				// because word-wrapping may move token at line head to previous line
				firstDirtyLineIndex--;
			}

			// [phase 3] calculate range of indexes to be deleted
			delBeginL = firstDirtyLineIndex + 1;
			int lastDirtyLogLineIndex = doc.Lines.AtOffset( index + newText.Length ).LineIndex;
			if( lastDirtyLogLineIndex+1 < doc.Lines.Count )
			{
				int delEnd = doc.Lines[ lastDirtyLogLineIndex + 1 ].Begin - diff;
				delEndL = TextUtil.GetLineIndexFromCharIndex( SLHI, delEnd );
			}
			else
			{
				delEndL = SLHI.Count;
			}
#			if SLHI_DEBUG
			Console.Error.WriteLine("[3] del:[{0}, {1})", delBeginL, delEndL);
#			endif
			
			// [phase 2] calculate range of indexes to be re-calculated
			reCalcBegin = SLHI[ firstDirtyLineIndex ];
			replaceEnd = index + newText.Length;
			reCalcEnd = doc.RawLines.AtOffset( replaceEnd ).End;
#			if SLHI_DEBUG
			Console.Error.WriteLine("[2] rc:[{0}, {1})", reCalcBegin, reCalcEnd);
#			endif

			// [phase 1] calculate range of indexes to be shifted
			if( replaceEnd == doc.Length )
			{
				// there are no more chars following.
				shiftBeginL = Int32.MaxValue;
			}
			else if( replaceEnd < doc.Length
				&& TextUtil.NextLineHead(doc.Buffer, replaceEnd) == -1 )
			{
				// there exists more characters but no lines.
				shiftBeginL = Int32.MaxValue;
			}
			else
			{
				// there are following lines.
				shiftBeginL = TextUtil.GetLineIndexFromCharIndex( SLHI, reCalcEnd - diff );
			}
#			if SLHI_DEBUG
			Console.Error.WriteLine("[1] shift:[{0}, {1})", shiftBeginL, SLHI.Count);
#			endif

			//--- apply ----
			// [phase 1] shift all followings
			for( int i=shiftBeginL; i<SLHI.Count; i++ )
			{
				SLHI[i] += newText.Length - oldText.Length;
			}

			// [phase 2] delete LHI of affected screen lines except first one
			if( delBeginL < delEndL && delEndL <= SLHI.Count )
				SLHI.RemoveRange( delBeginL, delEndL );

			// [phase 3] re-calculate screen line indexes
			// (here we should divide the text into small segments to avoid making unnecessary
			// copy of the text many times)
			const int segmentLen = 32;
			int x = 0;
			int drawableLen;
			int begin, end;
			int line = delBeginL;
			end = reCalcBegin;
			do
			{
				// calc next segment range
				begin = end;
				if( begin+segmentLen < reCalcEnd )
				{
					end = begin + segmentLen;
				}
				else
				{
					end = reCalcEnd;
				}
				while( TextUtil.IsNotDividableIndex(doc.Buffer, end) )
				{
					end++;
				}

				// get next segment
				var str = doc.GetText( begin, end );
				x = MeasureTokenEndX( g, str, x, TextAreaWidth, out drawableLen );

				// can this segment be written in this screen line?
				if( drawableLen < str.Length
					|| TextUtil.IsEolChar(str, drawableLen-1) )
				{
					// hit right limit. end this screen line
					end = begin + drawableLen;
					if( TextUtil.IsEolChar(str, drawableLen-1) == false )
					{
						// wrap word
						int newEndIndex = doc.WordProc.HandleWordWrapping( doc, begin+drawableLen );
						if( SLHI[line-1] < newEndIndex )
						{
							end = newEndIndex;
						}
					}
					Debug.Assert( SLHI[line-1] < end, "INTERNAL ERROR" );
					SLHI.Insert( line, end );
					line++;
					x = 0;
				}
			}
			while( end < reCalcEnd );

			// then, remove extra last screen line index made as the result of phase 3
			if( line != delBeginL && line < SLHI.Count )
			{
				SLHI.RemoveAt( line-1 );
			}

			// remember the condition of the calculation
			PerDocParam.LastTextAreaWidth = TextAreaWidth;
			PerDocParam.LastFontHashCode = FontInfo.GetHashCode();
			PerDocParam.LastModifiedTime = doc.LastModifiedTime;
		}
		#endregion

		#region Painting
		/// <summary>
		/// Paints content to a graphic device.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="clipRect">clipping rectangle that covers all invalidated region (in client area coordinate)</param>
		public override void Paint( IGraphics g, Rectangle clipRect )
		{
			Debug.Assert( FontInfo != null, "invalid state; FontInfo is null" );
			Debug.Assert( Document != null, "invalid state; Document is null" );

			int selBegin, selEnd;
			var pos = new Point();
			bool shouldRedraw1, shouldRedraw2;

			// prepare off-screen buffer
#			if !DRAW_SLOWLY
			g.BeginPaint( clipRect );
#			endif

#			if DRAW_SLOWLY
			g.ForeColor = Color.Fuchsia;
			g.DrawRectangle( clipRect.X, clipRect.Y, clipRect.Width-1, clipRect.Height-1 );
			g.DrawLine( clipRect.X, clipRect.Y, clipRect.X+clipRect.Width-1, clipRect.Y+clipRect.Height-1 );
			g.DrawLine( clipRect.X+clipRect.Width-1, clipRect.Y, clipRect.X, clipRect.Y+clipRect.Height-1 );
			DebugUtl.Sleep(400);
#			endif

			// Draw horizontal ruler and top margin
			DrawHRuler( g, clipRect );
			DrawTopMargin( g );

			// Draw all lines
			pos.Y = ScrYofTextArea;
			for( int i=FirstVisibleLine; i<Lines.Count; i++ )
			{
				if( pos.Y < clipRect.Bottom && clipRect.Top <= pos.Y+LineSpacing )
				{
					// Reset x-coord of drawing position
					pos.X = -(ScrollPosX - ScrXofTextArea);

					// Draw the line
					shouldRedraw1 = _UI.InvokeLineDrawing( g, i, pos );
					DrawLine( g, i, pos, clipRect );
					shouldRedraw2 = _UI.InvokeLineDrawn( g, i, pos );

					// [*1] Invalidate the line graphic if needed
					if( (shouldRedraw1 || shouldRedraw2)
						&& 0 < clipRect.Left ) // prevent infinite loop
					{
						Invalidate( 0, clipRect.Y, VisibleSize.Width, clipRect.Height );
					}
				}
				pos.Y += LineSpacing;
			}

			// Fill area below of the text
			g.BackColor = ColorScheme.BackColor;
			g.FillRectangle( 0, pos.Y, VisibleSize.Width, VisibleSize.Height-pos.Y );
			for( int y=pos.Y; y<VisibleSize.Height; y+=LineSpacing )
			{
				DrawLeftOfLine( g, y, -1, false );
			}

			// Flush drawing results BEFORE updating current line highlight
			// because the highlight graphic is never limited to clipping rect
#			if !DRAW_SLOWLY
			g.EndPaint();
#			endif

			// Draw right edge
			var x = (ScrXofTextArea + TextAreaWidth) - ScrollPosX;
			g.ForeColor = ColorScheme.RightEdgeColor;
			g.DrawLine( x, ScrYofTextArea, x, VisibleSize.Height );

			// draw underline to highlight current line if there is no selection
			Document.GetSelection( out selBegin, out selEnd );
			if( HighlightsCurrentLine && selBegin == selEnd )
			{
				var caretLine = TextUtil.GetLineIndexFromCharIndex( SLHI, CaretIndex );
				var caretPosY = ScrYofTextArea + (caretLine - FirstVisibleLine) * LineSpacing;
				DrawUnderLine( g, caretPosY, ColorScheme.HighlightColor );
			}
		}

		void DrawLine( IGraphics g, int lineIndex, Point pos, Rectangle clipRect )
		{
			Debug.Assert( FontInfo != null );
			Debug.Assert( Document != null );

			// note that given pos is NOT virtual position BUT screen position.
			string token;
			int begin, end; // range of the token in the text
			CharClass klass;
			Point tokenEndPos = pos;
			bool inSelection;

			// Calculate range of this screen line
			var screenLine = RawLines[lineIndex];

			// adjust and set clipping rect
			if( clipRect.X < ScrXofTextArea )
			{
				// Given clip rectangle includes line number area. Redraw line nubmer and exclude
				// its area from the clip rectangle to avoid overwriting
				DrawLeftOfLine( g, pos.Y,
								Document.Lines.AtOffset(screenLine.Begin).LineIndex + 1,
								!IsWrappedLineHead(Document, SLHI, screenLine.Begin) );
				clipRect.Width -= (ScrXofTextArea - clipRect.X);
				clipRect.X = ScrXofTextArea;
			}
#			if !DRAW_SLOWLY
			g.SetClipRect( clipRect );
#			endif

			// draw line text
			begin = screenLine.Begin;
			end = NextPaintToken( Document, begin, screenLine.End, out klass, out inSelection );
			while( end <= screenLine.End && end != -1 )
			{
				// get this token
				token = Document.GetText( begin, end );
				Debug.Assert( 0 < token.Length, "@View.Paint. NextPaintToken returns empty range." );

				// calc next drawing pos before drawing text
				{
					int virLeft = pos.X - (ScrXofTextArea - ScrollPosX);
					tokenEndPos.X = MeasureTokenEndX( g, token, virLeft );
					tokenEndPos.X += (ScrXofTextArea - ScrollPosX);
				}

				// if this token is at right of the clip-rect, no need to draw more.
				if( clipRect.Right < pos.X )
				{
					break;
				}

				// if this token is not visible yet, skip this token.
				if( tokenEndPos.X < clipRect.Left )
				{
					goto next_token;
				}

				// if the token area crosses the LEFT boundary of the clip-rect, cut off extra
				if( pos.X < clipRect.Left )
				{
					int invisibleCharCount;
					int rightLimit = clipRect.Left - pos.X;

					// calculate how many chars will not be in the clip-rect
					var invisibleWidth = MeasureTokenEndX( g, token, 0, rightLimit, out invisibleCharCount );
					if( 0 < invisibleCharCount && invisibleCharCount < token.Length )
					{
						// cut extra (invisible) part of the token
						token = token.Substring( invisibleCharCount );
						begin += invisibleCharCount;
						pos.X += invisibleWidth;
					}
				}

				// if the token area crosses the RIGHT boundary, cut off extra
				if( clipRect.Right < tokenEndPos.X )
				{
					int visCharCount; // visible char count
					string peekingChar;
					int peekingCharRight = 0;

					// Calculate the number of characters which fits within the clip-rect
					var visPartRight = MeasureTokenEndX( g, token, pos.X, clipRect.Right,
														 out visCharCount );

					// (if the clip-rect's right boundary is NOT the text area's right boundary,
					// we must write one more char so that the peeking char appears at the boundary.)

					// Try to get graphically peeking (drawn over the border line) char
					peekingChar = String.Empty;
					if( visCharCount+1 <= token.Length )
					{
						if( TextUtil.IsNotDividableIndex(token, visCharCount+1) )
							peekingChar = token.Substring( visCharCount, 2 );
						else
							peekingChar = token.Substring( visCharCount, 1 );
					}

					// Calculate right end coordinate of the peeking char
					if( peekingChar != String.Empty )
					{
						peekingCharRight = MeasureTokenEndX( g, peekingChar, visPartRight );
					}

					// Cut trailing extra
					token = token.Substring( 0, visCharCount+peekingChar.Length );
					tokenEndPos.X = (peekingCharRight != 0) ? peekingCharRight : visPartRight;

					// To terminate this loop, set token end position to invalid one
					end = Int32.MaxValue;
				}

				// Draw this token
				DrawToken( g, Document, begin, token, klass, ref pos, ref tokenEndPos, ref clipRect, inSelection );

			next_token:
				pos.X = tokenEndPos.X;
				begin = end;
				end = NextPaintToken( Document, begin, screenLine.End, out klass, out inSelection );
			}

			// Draw EOF mark
			if( DrawsEofMark && screenLine.End == Document.Length )
			{
				if( screenLine.IsEmpty
					|| (0 < screenLine.End && TextUtil.IsEolChar(Document[screenLine.End-1]) == false) )
				{
					DrawEofMark( g, ref pos );
				}
			}

			// Fill right of the line text
			if( pos.X < clipRect.Right )
			{
				// to prevent drawing line number area, make drawing pos to text area's left if the
				// line end does not exceed left of text area
				if( pos.X < ScrXofTextArea )
					pos.X = ScrXofTextArea;
				g.BackColor = ColorScheme.BackColor;
				g.FillRectangle( pos.X, pos.Y, clipRect.Right-pos.X, LineSpacing );
			}

#			if !DRAW_SLOWLY
			g.RemoveClipRect();
#			endif
		}

		/// <summary>
		/// Draws underline for the line specified by it's Y coordinate.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="lineTopY">Y-coordinate of the target line.</param>
		/// <param name="color">Color to be used for drawing the underline.</param>
		protected override void DrawUnderLine( IGraphics g, int lineTopY, Color color )
		{
			if( lineTopY < 0 )
				return;

			DebugUtl.Assert( (lineTopY % LineSpacing) == (ScrYofTextArea % LineSpacing), "lineTopY:"+lineTopY+", LineSpacing:"+LineSpacing+", ScrYofTextArea:"+ScrYofTextArea );

			// calculate position to underline
			int bottom = lineTopY + LineHeight + (LinePadding >> 1);

			// draw underline
			Point rightEnd = VirtualToScreen( new Point(TextAreaWidth, 0) );
			g.ForeColor = color;
			g.DrawLine( ScrXofTextArea, bottom, rightEnd.X-1, bottom );
		}
		#endregion

		#region Utilities
		GapBuffer<int> SLHI
		{
			get
			{
				Debug.Assert( Document != null );
				return PerDocParam.SLHI;
			}
		}

		static bool IsWrappedLineHead( Document doc, GapBuffer<int> slhi, int index )
		{
			int lineHeadIndex = TextUtil.GetLineHeadIndexFromCharIndex( doc.Buffer,
																		slhi, index );
			if( lineHeadIndex <= 0 )
			{
				return false;
			}

			char lastCharOfPrevLine = doc[lineHeadIndex-1];
			return ( TextUtil.IsEolChar(lastCharOfPrevLine) == false );
		}

		class WrappedLineRangeList : ILineRangeList
		{
			protected readonly PropWrapView _View;

			public WrappedLineRangeList( PropWrapView view )
			{
				Debug.Assert( view != null );
				_View = view;
			}

			/// <exception cref="ArgumentOutOfRangeException"/>
			public virtual ILineRange this[ int lineIndex ]
			{
				get
				{
					if( lineIndex < 0 || _View.SLHI.Count < lineIndex )
						throw new ArgumentOutOfRangeException();

					// Do not use "IView.Lines" here...
					var buf = _View.Document.Buffer;
					var range = TextUtil.GetLineRange( buf, _View.SLHI, lineIndex, false );
					return new LineRange( _View.Document, range.Begin, range.End, lineIndex );
				}
			}

			/// <exception cref="ArgumentOutOfRangeException"/>
			public ILineRange AtOffset( int charIndex )
			{
				if( charIndex < 0 || _View.Document.Length < charIndex )
					throw new ArgumentOutOfRangeException( "charIndex", charIndex, "Invalid index"
														   + " was given. (charIndex:" + charIndex
														   + ", Document.Length:"
														   + _View.Document.Length + ")." );

				return this[ _View.GetTextPosition(charIndex).Line ];
			}

			public int Count
			{
				get{ return _View.SLHI.Count; }
			}

			public IEnumerator<ILineRange> GetEnumerator()
			{
				for( int i=0; i<_View.SLHI.Count; i++ )
					yield return this[i];
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return GetEnumerator();
			}
		}

		class WrappedRawLineRangeList : WrappedLineRangeList
		{
			public WrappedRawLineRangeList( PropWrapView view )
				: base( view )
			{}

			/// <exception cref="ArgumentOutOfRangeException"/>
			public override ILineRange this[ int lineIndex ]
			{
				get
				{
					if( lineIndex < 0 || _View.SLHI.Count < lineIndex )
						throw new ArgumentOutOfRangeException();

					// Do not use "IView.RawLines" here...
					var buf = _View.Document.Buffer;
					var range = TextUtil.GetLineRange( buf, _View.SLHI, lineIndex, true );
					return new LineRange( _View.Document, range.Begin, range.End, lineIndex );
				}
			}
		}
		#endregion
	}
}
