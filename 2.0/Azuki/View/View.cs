// file: View.cs
// brief: Platform independent view implementation of Azuki engine.
//=========================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using Debug = System.Diagnostics.Debug;
using StringBuilder = System.Text.StringBuilder;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view of Azuki.
	/// </summary>
	abstract partial class View : IViewInternal
	{
		public int CaretIndex
		{
			get{ return Document.CaretIndex; }
		}

		public int AnchorIndex
		{
			get{ return Document.AnchorIndex; }
		}

		#region Fields and Types
		const float GoldenRatio = 1.6180339887f;
		const int DefaultTabWidth = 8;
		const int MinimumFontSize = 1;
		const int LineNumberAreaPadding = 2;
		static readonly int[] _LineNumberSamples = {
			9999,
			99999,
			999999,
			9999999,
			99999999,
			999999999,
			2000000000
		};
		protected IUserInterfaceInternal _UI;
		int _TextAreaWidth = 4096;
		int _MinimumTextAreaWidth = 300;
		Size _VisibleSize = new Size( 300, 300 );
		int _LastUsedLineNumberSample = _LineNumberSamples[0];
		protected int _LineNumAreaWidth = 0;// Width of the line number area in pixel
		int _SpaceWidth; 					// Width of a space char (U+0020) in pixel
		protected int _FullSpaceWidth = 0;	// Width of a full-width space char (U+3000) in pixel
		int _LineHeight;
		int _LinePadding = 1;
		int _TabWidth = DefaultTabWidth;
		int _TabWidthInPx;
		int _XCharWidth;
		int _DirtBarWidth;
		int _HRulerHeight;	// height of the largest lines of the horizontal ruler
		int _HRulerY_5;		// height of the middle lines of the horizontal ruler
		int _HRulerY_1;		// height of the smallest lines of the horizontal ruler
		HRulerIndicatorType _HRulerIndicatorType = HRulerIndicatorType.Segment;

		ColorScheme _ColorScheme = ColorScheme.Default;
		FontInfo _Font = new FontInfo( "Courier New", 10, FontStyle.Regular );
		FontInfo _HRulerFont;
		int _TopMargin = 1;
		int _LeftMargin = 1;
		DrawingOption _DrawingOption = DrawingOption.DrawsTab
									   | DrawingOption.DrawsFullWidthSpace
									   | DrawingOption.DrawsEol
									   | DrawingOption.HighlightCurrentLine
									   | DrawingOption.ShowsLineNumber
									   | DrawingOption.ShowsDirtBar
									   | DrawingOption.HighlightsMatchedBracket;
		bool _ScrollsBeyondLastLine = true;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal View( IUserInterfaceInternal ui )
		{
			Debug.Assert( ui != null );
			_UI = ui;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="other">another view object to inherit settings</param>
		internal View( View other )
		{
			Debug.Assert( other != null );

			// inherit reference to the UI module
			_UI = other._UI;

			// inherit other parameters
			if( other != null )
			{
				_ColorScheme = new ColorScheme( other._ColorScheme );
				_DrawingOption = other._DrawingOption;
				//DO_NOT//_DirtBarWidth = other._DirtBarWidth;
				//DO_NOT//_HRulerFont = other._HRulerFont;
				//DO_NOT//_LCharWidth = other._LCharWidth;
				//DO_NOT//_LineHeight = other._LineHeight;
				//DO_NOT//_LineNumAreaWidth = other._LineNumAreaWidth;
				//DO_NOT//_SpaceWidth = other._SpaceWidth;
				_TabWidth = other._TabWidth;
				_LinePadding = other._LinePadding;
				_LeftMargin = other._LeftMargin;
				_TopMargin = other.TopMargin;
				//DO_NOT//_TabWidthInPx = other._TabWidthInPx;
				_TextAreaWidth = other._TextAreaWidth;
				//DO_NOT//_UI = other._UI;
				_VisibleSize = other._VisibleSize;

				// set Font through property
				if( other.FontInfo != null )
					FontInfo = other.FontInfo;

				// re-calculate graphic metrics
				// (because there is a metric which needs a reference to Document to be calculated
				// but it cannnot be set Document before setting Font by structural reason)
				using( IGraphics g = _UI.GetIGraphics() )
				{
					UpdateMetrics( g );
				}
			}
		}

		/// <summary>
		/// Disposes resources.
		/// </summary>
		public virtual void Dispose()
		{
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets the document displayed in this view.
		/// </summary>
		public virtual Document Document
		{
			get{ return _UI.Document; }
		}

		public abstract ILineRangeList Lines
		{
			get;
		}

		public abstract ILineRangeList RawLines
		{
			get;
		}

		/// <summary>
		/// Gets or sets width of the virtual text area (line number area is not included).
		/// </summary>
		public virtual int TextAreaWidth
		{
			get{ return _TextAreaWidth; }
			set
			{
				if( value < _MinimumTextAreaWidth )
				{
					value = _MinimumTextAreaWidth;
				}
				_TextAreaWidth = value;
			}
		}

		/// <summary>
		/// Re-calculates and updates x-coordinate of the right end of the virtual text area.
		/// </summary>
		/// <param name="desiredX">X-coordinate of scroll destination desired.</param>
		/// <returns>The largest X-coordinate which Azuki can scroll to.</returns>
		protected abstract int ReCalcRightEndOfTextArea( int desiredX );

		/// <summary>
		/// Gets or sets size of the currently visible area (line number area is included).
		/// </summary>
		public Size VisibleSize
		{
			get{ return _VisibleSize; }
			set{ _VisibleSize = value; }
		}

		/// <summary>
		/// Gets or sets size of the currently visible size of the text area (line number area is not included).
		/// </summary>
		public Size VisibleTextAreaSize
		{
			get
			{
				var size = _VisibleSize;
				size.Width -= ScrXofTextArea;
				size.Height -= ScrYofTextArea;
				return size;
			}
		}

		/// <summary>
		/// Gets or sets whether to scroll beyond the last line of the document or not.
		/// </summary>
		public bool ScrollsBeyondLastLine
		{
			get{ return _ScrollsBeyondLastLine; }
			set{ _ScrollsBeyondLastLine = value; }
		}

		/// <summary>
		/// Gets or sets the font used for drawing text.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public virtual FontInfo FontInfo
		{
			get{ return _Font; }
			set
			{
				if( value == null )
					throw new ArgumentNullException();

				// Apply font
				_Font = value;
				_HRulerFont = new FontInfo( value.Name,
											(int)( value.Size / GoldenRatio ),
											FontStyle.Regular );

				// Update font metrics and graphic
				using( var g = _UI.GetIGraphics() )
					UpdateMetrics( g );
				Invalidate();
				_UI.UpdateCaretGraphic();
			}
		}

		protected void UpdateMetrics( IGraphics g )
		{
			var buf = new StringBuilder( 32 );
			_LastUsedLineNumberSample = _LineNumberSamples[0];

			// Calculate tab width in pixel
			for( int i=0; i<_TabWidth; i++ )
			{
				buf.Append( ' ' );
			}
			_TabWidthInPx = g.MeasureText( buf.ToString() ).Width;

			// Update other metrics
			_SpaceWidth = g.MeasureText( " " ).Width;
			_XCharWidth = g.MeasureText( "x" ).Width;
			_FullSpaceWidth = g.MeasureText( "\x3000" ).Width;
			_LineHeight = g.MeasureText( "Mp" ).Height;
			if( Document != null )
			{
				_LastUsedLineNumberSample = PerDocParam.MaxLineNumber;
			}
			_LineNumAreaWidth
				= g.MeasureText( _LastUsedLineNumberSample.ToString() ).Width + _SpaceWidth;
			_DirtBarWidth = Math.Max( 2, _SpaceWidth >> 1 );

			// Update metrics related with horizontal ruler
			_HRulerHeight = (int)( _LineHeight / GoldenRatio ) + 2;
			_HRulerY_5 = (int)( _HRulerHeight / (GoldenRatio * GoldenRatio) );
			_HRulerY_1 = (int)( _HRulerHeight / (GoldenRatio) );
			g.FontInfo = _HRulerFont;
			g.FontInfo = _Font;

			// Calculate minimum text area width
			_MinimumTextAreaWidth = Math.Max( _FullSpaceWidth, TabWidthInPx ) << 1;
		}
		#endregion

		#region Drawing Options
		/// <summary>
		/// Gets or sets top margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		public int TopMargin
		{
			get{ return _TopMargin; }
			set
			{
				if( value < 0 )
					throw new ArgumentOutOfRangeException( "value", "TopMargin must not be a negative number (value:"+value+")" );

				// Apply value
				_TopMargin = value;

				// Send dummy scroll event to update screen position of the caret
				_UI.Scroll( Rectangle.Empty, 0, 0 );
			}
		}

		/// <summary>
		/// Gets or sets left margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		public int LeftMargin
		{
			get{ return _LeftMargin; }
			set
			{
				if( value < 0 )
					throw new ArgumentOutOfRangeException( "value", "LeftMargin must not be a negative number (value:"+value+")" );

				// Apply value
				_LeftMargin = value;

				// Send dummy scroll event to update screen position of the caret
				_UI.Scroll( Rectangle.Empty, 0, 0 );
			}
		}

		/// <summary>
		/// Gets or sets type of the indicator on the horizontal ruler.
		/// </summary>
		public HRulerIndicatorType HRulerIndicatorType
		{
			get{ return _HRulerIndicatorType; }
			set{ _HRulerIndicatorType = value; }
		}

		/// <summary>
		/// Gets or sets view options.
		/// </summary>
		public DrawingOption DrawingOption
		{
			get{ return _DrawingOption; }
			set{ _DrawingOption = value; }
		}

		/// <summary>
		/// Gets or sets whether the current line would be drawn with underline or not.
		/// </summary>
		public bool HighlightsCurrentLine
		{
			get{ return (DrawingOption & DrawingOption.HighlightCurrentLine) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.HighlightCurrentLine;
				else
					DrawingOption &= ~DrawingOption.HighlightCurrentLine;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to highlight matched bracket or not.
		/// </summary>
		public bool HighlightsMatchedBracket
		{
			get{ return (DrawingOption & DrawingOption.HighlightsMatchedBracket) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.HighlightsMatchedBracket;
				else
					DrawingOption &= ~DrawingOption.HighlightsMatchedBracket;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to show line number or not.
		/// </summary>
		public bool ShowLineNumber
		{
			get{ return (DrawingOption & DrawingOption.ShowsLineNumber) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.ShowsLineNumber;
				else
					DrawingOption &= ~DrawingOption.ShowsLineNumber;

				_UI.UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to show horizontal ruler or not.
		/// </summary>
		public bool ShowsHRuler
		{
			get{ return (DrawingOption & DrawingOption.ShowsHRuler) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.ShowsHRuler;
				else
					DrawingOption &= ~DrawingOption.ShowsHRuler;

				_UI.UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to show 'dirt bar' or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to show 'dirt bar' or not.
		/// The 'dirt bar'
		/// </para>
		/// </remarks>
		public bool ShowsDirtBar
		{
			get{ return (DrawingOption & DrawingOption.ShowsDirtBar) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.ShowsDirtBar;
				else
					DrawingOption &= ~DrawingOption.ShowsDirtBar;

				_UI.UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw half-width space with special graphic or not.
		/// </summary>
		public bool DrawsSpace
		{
			get{ return (DrawingOption & DrawingOption.DrawsSpace) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsSpace;
				else
					DrawingOption &= ~DrawingOption.DrawsSpace;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw full-width space with special graphic or not.
		/// </summary>
		public bool DrawsFullWidthSpace
		{
			get{ return (DrawingOption & DrawingOption.DrawsFullWidthSpace) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsFullWidthSpace;
				else
					DrawingOption &= ~DrawingOption.DrawsFullWidthSpace;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw tab character with special graphic or not.
		/// </summary>
		public bool DrawsTab
		{
			get{ return (DrawingOption & DrawingOption.DrawsTab) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsTab;
				else
					DrawingOption &= ~DrawingOption.DrawsTab;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw EOL code with special graphic or not.
		/// </summary>
		public bool DrawsEolCode
		{
			get{ return (DrawingOption & DrawingOption.DrawsEol) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsEol;
				else
					DrawingOption &= ~DrawingOption.DrawsEol;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw EOF mark by special graphic or not.
		/// </summary>
		public bool DrawsEofMark
		{
			get{ return (DrawingOption & DrawingOption.DrawsEof) != 0; }
			set
			{
				if( value )
					DrawingOption |= DrawingOption.DrawsEof;
				else
					DrawingOption &= ~DrawingOption.DrawsEof;
				Invalidate();
			}
		}

		/// <summary>
		/// Color set used for displaying text.
		/// </summary>
		public ColorScheme ColorScheme
		{
			get{ return _ColorScheme; }
			set
			{
				if( value == null )
					value = ColorScheme.Default;

				_ColorScheme = value;
			}
		}

		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
		public virtual int TabWidth
		{
			get{ return _TabWidth; }
			set
			{
				if( value <= 0 )
					throw new ArgumentOutOfRangeException( "value", "TabWidth must not be a negative number (given value:"+value+".)" );

				using( var g = _UI.GetIGraphics() )
				{
					_TabWidth = value;
					UpdateMetrics( g );
					Invalidate();
				}
			}
		}
		
		/// <summary>
		/// Gets width of tab character (U+0009) in pixel.
		/// </summary>
		public int TabWidthInPx
		{
			get{ return _TabWidthInPx; }
		}

		/// <summary>
		/// Gets width of space character (U+0020) in pixel.
		/// </summary>
		public int SpaceWidthInPx
		{
			get{ return _SpaceWidth; }
		}
		#endregion

		#region States
		/// <summary>
		/// Gets or sets index of the line which is displayed at top of this view.
		/// </summary>
		public int FirstVisibleLine
		{
			get{ return PerDocParam.FirstVisibleLine; }
			set{ PerDocParam.FirstVisibleLine = value; }
		}

		/// <summary>
		/// Gets or sets x-coordinate of the view's origin.
		/// </summary>
		internal int ScrollPosX
		{
			get{ return PerDocParam.ScrollPosX; }
			set{ PerDocParam.ScrollPosX = value; }
		}

		/// <summary>
		/// Gets height of each lines in pixel.
		/// </summary>
		public int LineHeight
		{
			get{ return _LineHeight; }
		}

		/// <summary>
		/// Gets or sets size of padding between lines in pixel.
		/// </summary>
		public int LinePadding
		{
			get{ return _LinePadding; }
			set
			{
				if( value < 1 )
					value = 1;
				_LinePadding = value;
			}
		}

		/// <summary>
		/// Gets distance between lines in pixel.
		/// </summary>
		public int LineSpacing
		{
			get{ return _LineHeight+_LinePadding; }
		}

		/// <summary>
		/// Gets width of the line number area in pixel.
		/// </summary>
		public int LineNumAreaWidth
		{
			get
			{
				return ShowLineNumber ? _LineNumAreaWidth
									  : 0;
			}
		}

		/// <summary>
		/// Gets width of the dirt bar in pixel.
		/// </summary>
		public int DirtBarWidth
		{
			get
			{
				return ShowsDirtBar ? _DirtBarWidth
									: 0;
			}
		}

		/// <summary>
		/// Gets height of the horizontal ruler.
		/// </summary>
		public int HRulerHeight
		{
			get
			{
				return ShowsHRuler ? _HRulerHeight
								   : 0;
			}
		}

		/// <summary>
		/// Gets distance between lines on the horizontal ruler.
		/// </summary>
		public int HRulerUnitWidth
		{
			get{ return _XCharWidth; }
		}
		#endregion

		#region Desired Column Management
		/// <summary>
		/// Sets column index of the current caret position to "desired column" value.
		/// </summary>
		/// <remarks>
		/// <para>
		/// When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value.
		/// Note that "desired column" is associated with each document
		/// so this value may change when Document property was set to another document.
		/// </para>
		/// </remarks>
		public void SetDesiredColumn()
		{
			using( var g = _UI.GetIGraphics() )
				SetDesiredColumn( g );
		}

		/// <summary>
		/// Sets column index of the current caret position to "desired column" value.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Normally the caret tries to keep its x-coordinate
		/// on moving line to line unless user explicitly changes x-coordinate of it.
		/// The term 'Desired Column' means this x-coordinate which the caret tries to stick close to.
		/// </para>
		/// <para>
		/// Note that the desired column is associated with each document.
		/// </para>
		/// </remarks>
		public void SetDesiredColumn( IGraphics g )
		{
			PerDocParam.DesiredColumnX = GetVirtualPos( g, CaretIndex ).X;
		}

		/// <summary>
		/// Gets current "desired column" value.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Normally the caret tries to keep its x-coordinate
		/// on moving line to line unless user explicitly changes x-coordinate of it.
		/// The term 'Desired Column' means this x-coordinate which the caret tries to stick close to.
		/// </para>
		/// <para>
		/// Note that the desired column is associated with each document.
		/// </para>
		/// </remarks>
		public int GetDesiredColumn()
		{
			return PerDocParam.DesiredColumnX;
		}
		#endregion

		#region Position / Index Conversion
		/// <exception cref="ArgumentOutOfRangeException"/>
		public virtual Point GetVirtualPos( IGraphics g, int index )
		{
			Debug.Assert( g != null );
			var pos = GetTextPosition( index );
			return GetVirtualPos( g, pos.Line, pos.Column );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public Point GetVirtualPos( int index )
		{
			using( var g = _UI.GetIGraphics() )
				return GetVirtualPos( g, index );
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public abstract Point GetVirtualPos( IGraphics g, int lineIndex, int columnIndex );

		/// <exception cref="ArgumentOutOfRangeException"/>
		public Point GetVirtualPos( int lineIndex, int columnIndex )
		{
			using( var g = _UI.GetIGraphics() )
				return GetVirtualPos( g, lineIndex, columnIndex );
		}

		public abstract int GetCharIndex( IGraphics g, Point pos );
		public int GetCharIndex( Point pos )
		{
			using( var g = _UI.GetIGraphics() )
				return GetCharIndex( g, pos );
		}

		/// <summary>
		/// Converts a coordinate in virtual space to a coordinate in client area.
		/// </summary>
		public Point VirtualToScreen( Point pt )
		{
			return new Point( (pt.X - ScrollPosX) + ScrXofTextArea,
							  (pt.Y - FirstVisibleLine * LineSpacing) + ScrYofTextArea );
		}

		/// <summary>
		/// Converts a coordinate in virtual space to a coordinate in client area.
		/// </summary>
		public Rectangle VirtualToScreen( Rectangle rect )
		{
			return new Rectangle( (rect.X - ScrollPosX) + ScrXofTextArea,
								  (rect.Y - FirstVisibleLine * LineSpacing) + ScrYofTextArea,
								  rect.Width, rect.Height );
		}

		/// <summary>
		/// Converts a coordinate in client area to a coordinate in virtual text area.
		/// </summary>
		public Point ScreenToVirtual( Point pt )
		{
			return new Point( (pt.X + ScrollPosX) - ScrXofTextArea,
							  (pt.Y + FirstVisibleLine * LineSpacing) - ScrYofTextArea );
		}

		/// <summary>
		/// Calculates screen line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract TextPoint GetTextPosition( int charIndex );

		/// <summary>
		/// Calculates char-index from screen line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract int GetCharIndex( TextPoint position );

		/// <summary>
		/// Calculates and returns text ranges that will be selected by specified rectangle.
		/// </summary>
		/// <param name="selRect">Rectangle to be used to specify selection target.</param>
		/// <returns>Array of indexes (1st begin, 1st end, 2nd begin, 2nd end, ...)</returns>
		/// <remarks>
		/// <para>
		/// (This method is basically for internal use.
		/// I do not recommend to use this from outside of Azuki.)
		/// </para>
		/// <para>
		/// This method calculates text ranges which will be selected by given rectangle.
		/// Because mapping of character indexes and graphical position (layout) are
		/// executed by view implementations, the result of this method will be changed
		/// according to the interface implementation.
		/// </para>
		/// <para>
		/// Return value of this method is an array of text indexes
		/// that is consisted with beginning index of first text range (row),
		/// ending index of first text range,
		/// beginning index of second text range,
		/// ending index of second text range and so on.
		/// </para>
		/// </remarks>
		public int[] GetRectSelectRanges( Rectangle selRect )
		{
			var selRanges = new List<int>();

			// Get text in the rect
			var selRectBottom = Math.Max( selRect.Bottom, 0 );
			var leftPos = new Point( selRect.Left, 0 );
			var rightPos = new Point( selRect.Right, 0 );
			int y = selRect.Top - (selRect.Top % LineSpacing);
			while( y <= selRectBottom )
			{
				// Calculate sub-selection range made with this line
				leftPos.Y = rightPos.Y = y;
				var leftIndex = GetCharIndex( leftPos );
				var rightIndex = GetCharIndex( rightPos );
				if( 1 < selRanges.Count && selRanges[selRanges.Count-1] == rightIndex )
				{
					break; // Reached EOF
				}
				Debug.Assert( Document.IsNotDividableIndex(leftIndex) == false );
				Debug.Assert( Document.IsNotDividableIndex(rightIndex) == false );

				// Add this sub-selection range
				selRanges.Add( leftIndex );
				selRanges.Add( rightIndex );

				// Go to next line
				y += LineSpacing;
			}

			return selRanges.ToArray();
		}

		/// <summary>
		/// Calculates location of character at specified index in horizontal ruler index.
		/// </summary>
		/// <param name="charIndex">The index of the character to calculate its location.</param>
		/// <returns>Horizontal ruler index of the character.</returns>
		/// <remarks>
		/// <para>
		/// This method calculates location of character at specified index
		/// in horizontal ruler index.
		/// </para>
		/// <para>
		/// 'Horizontal ruler index' here means how many small lines drawn on the horizontal ruler
		/// exist between left-end of the text area
		/// and the character at index specified by <paramref name="charIndex"/>.
		/// This value is zero-based index.
		/// </para>
		/// </remarks>
		public int GetHRulerIndex( int charIndex )
		{
			Point virPos;

			if( charIndex < 0 || Document.Length < charIndex )
				throw new ArgumentOutOfRangeException( "charIndex", "Specified index is out of range. (value:"+charIndex+", document length:"+Document.Length+")" );

			// Calculate location of the character in coordinate in virtual text area
			using( var g = _UI.GetIGraphics() )
				virPos = GetVirtualPos( g, charIndex );

			// Calculate how many smallest lines exist at left of the character
			return (virPos.X / HRulerUnitWidth);
		}

		/// <summary>
		/// Calculates location of character at specified index in horizontal ruler index.
		/// </summary>
		/// <param name="lineIndex">The line index of the character to calculate its location.</param>
		/// <param name="columnIndex">The column index of the character to calculate its location.</param>
		/// <returns>Horizontal ruler index of the character.</returns>
		/// <remarks>
		/// <para>
		/// This method calculates location of character at specified index
		/// in horizontal ruler index.
		/// </para>
		/// <para>
		/// 'Horizontal ruler index' here means how many small lines drawn on the horizontal ruler
		/// exist between left-end of the text area
		/// and the character at index specified by <paramref name="charIndex"/>.
		/// This value is zero-based index.
		/// </para>
		/// </remarks>
		public int GetHRulerIndex( int lineIndex, int columnIndex )
		{
			Point virPos;

			if( lineIndex < 0 || Lines.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Specified index is out of range. (value:"+lineIndex+", line count:"+Lines.Count+")" );
			if( columnIndex < 0 )
				throw new ArgumentOutOfRangeException( "columnIndex", "Specified index is out of range. (value:"+columnIndex+")" );

			// Calculate location of the character in coordinate in virtual text area
			using( var g = _UI.GetIGraphics() )
				virPos = GetVirtualPos( g, lineIndex, columnIndex );

			// Calculate how many smallest lines exist at left of the character
			return (virPos.X / HRulerUnitWidth);
		}
		#endregion

		#region Operations
		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		public void ScrollToCaret()
		{
			using( var g = _UI.GetIGraphics() )
				ScrollToCaret( g, _UI.AutoScrollMargin );
		}

		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		public void ScrollToCaret( IGraphics g )
		{
			ScrollToCaret( g, _UI.AutoScrollMargin );
		}

		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		public void ScrollToCaret( IGraphics g, int autoScrollMargin )
		{
			var threshRect = new Rectangle();
			int vDelta = 0, hDelta;

			// Make rentangle of virtual text view
			threshRect.X = ScrollPosX + SpaceWidthInPx;
			threshRect.Y = FirstVisibleLine * LineSpacing;
			threshRect.Width = (_VisibleSize.Width - ScrXofTextArea) - (SpaceWidthInPx * 2);
			threshRect.Height = (_VisibleSize.Height - ScrYofTextArea) - LineSpacing;

			// Shrink the rectangle if some lines must be visible
			if( 0 < autoScrollMargin )
			{
				int yMargin = Math.Max( 0, autoScrollMargin * LineSpacing );
				threshRect.Y += yMargin;
				threshRect.Height -= (yMargin * 2);
			}

			// Calculate caret position
			var caretPos = GetVirtualPos( g, CaretIndex );
			if( threshRect.Left <= caretPos.X
				&& caretPos.X <= threshRect.Right
				&& threshRect.Top <= caretPos.Y
				&& caretPos.Y <= threshRect.Bottom )
			{
				return; // caret is already visible
			}

			// Calculate horizontal offset to the position where we desire to scroll to
			hDelta = 0;
			if( threshRect.Right <= caretPos.X )
				hDelta = caretPos.X - (threshRect.Right - TabWidthInPx); // Scroll to right
			else if( caretPos.X < threshRect.Left )
				hDelta = caretPos.X - (threshRect.Left + TabWidthInPx); // Scroll to left

			// Calculate vertical offset to the position where we desire to scroll to
			vDelta = 0;
			if( threshRect.Bottom <= caretPos.Y )
				vDelta = (caretPos.Y + LineSpacing) - threshRect.Bottom; // Scroll down
			else if( caretPos.Y < threshRect.Top )
				vDelta = caretPos.Y - threshRect.Top; // Scroll up

			// Scroll the view
			Scroll( vDelta / LineSpacing );
			HScroll( hDelta );

			// Update horizontal ruler graphic.
			// Because indicator graphic may have been scrolled out partially (drawn partially),
			// just scrolling horizontal ruler area may make uncompeltely drawn indicator
			if( ShowsHRuler && 0 < hDelta )
				UpdateHRuler( g );
		}

		/// <summary>
		/// Scroll vertically.
		/// </summary>
		public void Scroll( int lineDelta )
		{
			int delta;
			int destLineIndex;
			int maxLineIndex;
			int visibleLineCount;

			if( lineDelta == 0 )
				return;

			// Calculate specified index of new FirstVisibleLine and biggest acceptable value of it
			destLineIndex = FirstVisibleLine + lineDelta;
			if( ScrollsBeyondLastLine )
			{
				maxLineIndex = Lines.Count - 1;
			}
			else
			{
				visibleLineCount = VisibleSize.Height / LineSpacing;
				maxLineIndex = Math.Max( 0, Lines.Count-visibleLineCount+1 );
			}

			// Calculate scroll distance
			if( destLineIndex < 0 )
				delta = -FirstVisibleLine;
			else if( maxLineIndex < destLineIndex )
				delta = maxLineIndex - FirstVisibleLine;
			else
				delta = lineDelta;
			if( delta == 0 )
				return;

			// Make clipping rectangle
			var clipRect = new Rectangle( 0, ScrYofTextArea,
										  _VisibleSize.Width, _VisibleSize.Height );

			// Do scroll
			FirstVisibleLine += delta;
			_UI.Scroll( clipRect, 0, -(delta * LineSpacing) );
			_UI.UpdateCaretGraphic();

			_UI.InvokeVScroll();
		}

		/// <summary>
		/// Scroll horizontally.
		/// </summary>
		public void HScroll( int columnDelta )
		{
			int deltaInPx;
			var clipRect = new Rectangle();
			int rightLimit;
			int desiredX;

			if( columnDelta == 0 )
				return;

			// Calculate the x-coord of right most scroll position
			desiredX = ScrollPosX + columnDelta;
			rightLimit = ReCalcRightEndOfTextArea( desiredX );
			if( rightLimit <= 0 )
			{
				return; // virtual text area is narrower than visible area. no need to scroll
			}

			// Calculate scroll distance
			if( desiredX < 0 )
			{
				//--- scrolling to left of the text area ---
				// do nothing if already at left most position
				if( ScrollPosX == 0 )
					return;
				
				// scroll to left most position
				deltaInPx = -ScrollPosX;
			}
			else if( rightLimit <= desiredX )
			{
				//--- scrolling to right of the text area ---
				// do nothing if already at right most position
				if( rightLimit == ScrollPosX+columnDelta )
					return;

				// scroll to right most position
				deltaInPx = (rightLimit - ScrollPosX);
			}
			else
			{
				deltaInPx = columnDelta;
			}

			// Make clipping rectangle
			clipRect.X = ScrXofTextArea;
			clipRect.Y = 0;
			clipRect.Width = _VisibleSize.Width - ScrXofTextArea;
			clipRect.Height = _VisibleSize.Height;

			// Do scroll
			ScrollPosX += deltaInPx;
			_UI.Scroll( clipRect, -deltaInPx, 0 );

			_UI.InvokeHScroll();
		}

		/// <summary>
		/// Requests to repaint whole area.
		/// </summary>
		public void Invalidate()
		{
			_UI.Invalidate();
		}

		/// <summary>
		/// Requests to repaint specified area.
		/// </summary>
		public void Invalidate( int x, int y, int width, int height )
		{
			Invalidate( new Rectangle(x, y, width, height) );
		}

		/// <summary>
		/// Requests to repaint specified area.
		/// </summary>
		/// <param name="rect">rectangle area to be repainted (in client area coordinate)</param>
		public void Invalidate( Rectangle rect )
		{
//DEBUG//using(IGraphics g = _UI.GetIGraphics() ){g.ForeColor=Color.Red; g.DrawLine(rect.Left,rect.Top,rect.Right-1,rect.Bottom-1);g.DrawLine(rect.Left,rect.Bottom-1,rect.Right-1,rect.Top);DebugUtl.Sleep(400);}
			_UI.Invalidate( rect );
		}

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="range">A range of text which is needed to be repainted.</param>
		public abstract void Invalidate( IRange range );

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be repainted.</param>
		/// <param name="endIndex">End text index of the area to be repainted.</param>
		public abstract void Invalidate( int beginIndex, int endIndex );

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="g">graphic drawing interface to be used.</param>
		/// <param name="beginIndex">Begin text index of the area to be repainted.</param>
		/// <param name="endIndex">End text index of the area to be repainted.</param>
		public abstract void Invalidate( IGraphics g, int beginIndex, int endIndex );

		/// <summary>
		/// Sets font size to larger one.
		/// </summary>
		public void ZoomIn()
		{
			// remember left-end position of text area
			int oldTextAreaX = ScrXofTextArea;

			// calculate next font size
			int newSize = (int)( FontInfo.Size / 0.9 );
			if( newSize <= FontInfo.Size )
			{
				newSize = FontInfo.Size + 1;
			}

			// apply new font size
			FontInfo = new FontInfo( FontInfo.Name, newSize, FontInfo.Style );
			_UI.FontInfo = FontInfo;

			// reset text area to sustain total width of view
			// because changing font size also changes width of line number area,
			TextAreaWidth += oldTextAreaX - ScrXofTextArea;

			_UI.UpdateCaretGraphic();
		}

		/// <summary>
		/// Sets font size to smaller one.
		/// </summary>
		public void ZoomOut()
		{
			// remember left-end position of text area
			int oldTextAreaX = ScrXofTextArea;

			// calculate next font size
			int newSize = (int)(FontInfo.Size * 0.9);
			if( newSize < MinimumFontSize )
			{
				newSize = MinimumFontSize;
			}

			// apply new font size
			FontInfo = new FontInfo( FontInfo.Name, newSize, FontInfo.Style );
			_UI.FontInfo = FontInfo;

			// reset text area to sustain total width of view
			// because changing font size also changes width of line number area,
			TextAreaWidth += oldTextAreaX - ScrXofTextArea;

			_UI.UpdateCaretGraphic();
		}
		#endregion

		#region Communication between UI Module
		/// <summary>
		/// UI module must call this method
		/// to synchronize visible size between UI module and view.
		/// </summary>
		internal void HandleSizeChanged( Size newSize )
		{
			_VisibleSize = newSize;
		}

		/// <summary>
		/// Internal use only.
		/// UI module must call this method
		/// when the document object was changed to another object.
		/// </summary>
		internal virtual void HandleDocumentChanged( Document prevDocument )
		{
			var doc = Document;
			using( var g = _UI.GetIGraphics() )
			{
				// reset width of line number area
				UpdateLineNumberWidth( g );

				// re-calculate line index of caret and anchor
				PerDocParam.PrevCaretLine = Lines.AtOffset( CaretIndex ).LineIndex;
				PerDocParam.PrevAnchorLine = Lines.AtOffset( AnchorIndex ).LineIndex;

				// reset desired column to current caret position
				SetDesiredColumn( g );
			}
		}

		/// <summary>
		/// This method will be called when the selection was changed.
		/// </summary>
		internal abstract void HandleSelectionChanged( object sender, SelectionChangedEventArgs e );

		/// <summary>
		/// This method will be called when the 'dirty' state of document was changed.
		/// </summary>
		internal virtual void HandleDirtyStateChanged( object sender, EventArgs e )
		{
			// if dirty flag has been cleared, redraw entire dirt bar
			if( Document.IsDirty == false )
			{
				var rect = new Rectangle( ScrXofDirtBar,
										  ScrYofTextArea,
										  DirtBarWidth,
										  VisibleSize.Height );
				Invalidate( rect );
			}
		}

		/// <summary>
		/// This method will be called when the content was changed.
		/// </summary>
		internal abstract void HandleContentChanged( object sender, ContentChangedEventArgs e );

		/// <summary>
		/// Updates width of the line number area.
		/// </summary>
		protected void UpdateLineNumberWidth( IGraphics g )
		{
			var doc = Document;
			DebugUtl.Assert( doc != null );

			// if current width of line number area is appropriate, do nothing
			if( doc.Lines.Count <= PerDocParam.MaxLineNumber )
			{
				return;
			}

			// find minimum value from samples for calculating width of line number area
			for( int i=0; i<_LineNumberSamples.Length; i++ )
			{
				if( doc.Lines.Count <= _LineNumberSamples[i] )
				{
					PerDocParam.MaxLineNumber = _LineNumberSamples[i];
					if( _LastUsedLineNumberSample != _LineNumberSamples[i] )
					{
						UpdateMetrics( g );
						Invalidate();
					}
					return;
				}
			}
		}

		internal void HandleGotFocus()
		{
			// draw underline on current line
			if( HighlightsCurrentLine )
			{
				using( var g = _UI.GetIGraphics() )
				{
					int selBegin, selEnd;
					Document.GetSelection( out selBegin, out selEnd );
					if( selBegin == selEnd )
					{
						DrawUnderLine( g,
									   YofLine( Lines.AtOffset(selBegin).LineIndex ),
									   ColorScheme.HighlightColor );
					}
				}
			}
		}

		internal void HandleLostFocus()
		{
			// erase underline on current line
			if( HighlightsCurrentLine )
			{
				using( var g = _UI.GetIGraphics() )
				{
					int selBegin, selEnd;
					Document.GetSelection( out selBegin, out selEnd );
					if( selBegin == selEnd )
					{
						DrawUnderLine( g,
									   YofLine( Lines.AtOffset(selBegin).LineIndex ),
									   ColorScheme.BackColor );
					}
				}
			}
		}
		#endregion

		#region Coordinates of Graphical Parts
		/// <summary>
		/// Gets X coordinate in client area of line number area.
		/// </summary>
		public int ScrXofLineNumberArea
		{
			get{ return 0; }
		}

		/// <summary>
		/// Gets X coordinate in client area of dirt bar area.
		/// </summary>
		public int ScrXofDirtBar
		{
			get{ return ScrXofLineNumberArea + LineNumAreaWidth; }
		}

		/// <summary>
		/// Gets X coordinate in client area of left margin.
		/// </summary>
		public int ScrXofLeftMargin
		{
			get
			{
				int value = ScrXofDirtBar + DirtBarWidth;
				if( 0 < value )
					return value + 1;
				else
					return value;
			}
		}

		/// <summary>
		/// Gets X coordinate in client area of text area.
		/// </summary>
		public int ScrXofTextArea
		{
			get
			{
				int value = ScrXofLeftMargin + LeftMargin;
				if( LeftMargin <= 0 )
					return value + 1;
				else
					return value;
			}
		}

		/// <summary>
		/// Gets Y coordinate in client area of horizontal ruler.
		/// </summary>
		public int ScrYofHRuler
		{
			get{ return 0; }
		}

		/// <summary>
		/// Gets Y coordinate in client area of top margin.
		/// </summary>
		public int ScrYofTopMargin
		{
			get{ return ScrYofHRuler + HRulerHeight; }
		}

		/// <summary>
		/// Gets Y coordinate in client area of text area.
		/// </summary>
		public int ScrYofTextArea
		{
			get{ return ScrYofTopMargin + TopMargin; }
		}

		/// <summary>
		/// Calculates size and location of the dirt bar area.
		/// </summary>
		public Rectangle DirtBarRect
		{
			get
			{
				return new Rectangle( ScrXofDirtBar, ScrYofTextArea,
									  DirtBarWidth, VisibleSize.Height - ScrYofTextArea );
			}
		}

		/// <summary>
		/// Gets location and size of the line number area.
		/// </summary>
		public Rectangle LineNumberAreaRect
		{
			get
			{
				return new Rectangle( ScrXofLineNumberArea, ScrYofTextArea,
									  LineNumAreaWidth, VisibleSize.Height - ScrYofTextArea );
			}
		}

		/// <summary>
		/// Gets location and size of the horizontal ruler area.
		/// </summary>
		public Rectangle HRulerRect
		{
			get
			{
				return new Rectangle( 0, ScrYofHRuler,
									  VisibleSize.Width, ScrYofTopMargin );
			}
		}

		/// <summary>
		/// Gets location and size of the visible text area in screen.
		/// </summary>
		public Rectangle TextAreaRect
		{
			get
			{
				return new Rectangle( ScrXofTextArea,
									  ScrYofTextArea,
									  VisibleSize.Width - ScrXofTextArea,
									  VisibleSize.Height - ScrYofTextArea );
			}
		}
		#endregion

		#region IViewInternal
		public bool IsLineHead( int index )
		{
			if( index < 0 )
			{
				return false;
			}
			else if( index == 0 )
			{
				return true;
			}
			else if( index < Document.Length )
			{
				int lineHeadIndex = Lines.AtOffset( index ).Begin;
				return (lineHeadIndex == index);
			}
			else
			{
				return false;
			}
		}
		#endregion

		#region Utilities
		public int NextTabStopX( int virX )
		{
			virX += TabWidthInPx;
			virX -= (virX % TabWidthInPx);
			return virX;
		}

		/// <summary>
		/// Gets Y coordinate in client area of specified line.
		/// </summary>
		protected int YofLine( int lineIndex )
		{
			return (  (lineIndex - FirstVisibleLine) * LineSpacing  ) + ScrYofTextArea;
		}

		int EolCodeWidthInPx
		{
			get{ return (_LineHeight >> 1) + (_LineHeight >> 2); }
		}
		#endregion

		#region Per document parameters
		readonly Dictionary<int, PerDocParam> _PerDocParams = new Dictionary<int,PerDocParam>();
		public PerDocParam PerDocParam
		{
			get
			{
				var code = Document.GetHashCode();
				PerDocParam param;
				if( _PerDocParams.TryGetValue(code, out param) == false )
				{
					// Collect garbages
					var ids = new List<int>();
					foreach( var key in _PerDocParams.Keys )
						if( _PerDocParams[key].WeakRef.IsAlive == false )
							ids.Add( key );
					foreach( var id in ids )
						_PerDocParams.Remove( id );

					// Allocate a new parameter object
					param = new PerDocParam( Document );
					_PerDocParams[code] = param;
				}
				return param;
			}
		}
		#endregion
	}
}
