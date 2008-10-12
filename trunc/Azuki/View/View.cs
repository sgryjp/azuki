﻿// file: View.cs
// brief: Platform independent view implementation of Azuki engine.
// author: YAMAMOTO Suguru
// update: 2008-10-12
//=========================================================
using System;
using System.Drawing;
using StringBuilder = System.Text.StringBuilder;
using Debug = System.Diagnostics.Debug;

namespace Sgry.Azuki
{
	/// <summary>
	/// Platform independent view of Azuki.
	/// </summary>
	/// <remarks>
	/// HandleXXX method is designed to be called by UI module (AzukiControl) to handle platform event.
	/// </remarks>
	public abstract partial class View : IDisposable
	{
		#region Fields and Types
		[Flags]
		enum SpecialChar : int
		{
			Space = 1, FullWidthSpace = 2, Tab = 4, Eol = 8
		}

		/// <summary>platform dependent layer of user interface</summary>
		protected IUserInterface _UI;
		Document _Document = null;
		Font _Font;
		int _FirstVisibleLine = 0;
		int _ScrollPosX;
		bool _ShowLineNumber = true;
		bool _HighlightCurrentLine = true;
		int _TextAreaWidth = 300;

		/// <summary>when the caret moves up or down, Azuki tries to set next caret's column index to this value.</summary>
		int _DesiredColumn = 0;

		//--- for drawing ---
		Size _VisibleSize = new Size( 300, 300 );

		/// <summary>Interface to draw graphic.</summary>
		protected IGraphics _Gra = null;
		
		/// <summary>Width of the line number area in pixel.</summary>
		protected int _LineNumWidth = 0;

		/// <summary>Width of a space char (U+0020) in pixel.</summary>
		protected int _SpaceWidth;

		/// <summary>Width of a full-width space char (U+3000) in pixel.</summary>
		protected int _FullSpaceWidth = 0;
		int _LineHeight;
		int _TabWidth = 8;
		int _TabWidthInPx;
		int _LCharWidth;
		SpecialChar _DrawSpecialCharFlag = SpecialChar.Tab | SpecialChar.FullWidthSpace | SpecialChar.Eol;
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="ui">Implementation of the platform dependent UI module.</param>
		internal View( IUserInterface ui )
		{
			_UI = ui;
			_Gra = ui.GetIGraphics();
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="other">another view object to inherit settings</param>
		internal View( View other )
		{
			// inherit reference to the UI module
			this._UI = other._UI;
			this._Gra = _UI.GetIGraphics();

			// inherit other parameters
			this._DrawSpecialCharFlag = other._DrawSpecialCharFlag;
			this._FirstVisibleLine = other._FirstVisibleLine;
			this._HighlightCurrentLine = other._HighlightCurrentLine;
			this._ScrollPosX = other._ScrollPosX;
			this._ShowLineNumber = other._ShowLineNumber;
			this._TabWidth = other._TabWidth;
			this._VisibleSize = other._VisibleSize;

			if( other.Document != null )
				Document = other.Document;
			if( other._Font != null )
				Font = other.Font;
			TextAreaWidth = other._TextAreaWidth;
		}

#		if DEBUG
		~View()
		{
			Debug.Assert( _Gra == null, ""+GetType()+"("+GetHashCode()+") was destroyed but not disposed." );
		}
#		endif

		/// <summary>
		/// Disposes resources.
		/// </summary>
		public virtual void Dispose()
		{
			// dispose graphic resources
			_Gra.Dispose();
			_Gra = null;

			// uninstall event handlers from document
			if( _Document != null )
			{
				_Document.SelectionChanged -= Doc_SelectionChanged;
				_Document.ContentChanged -= Doc_ContentChanged;
			}
		}
		#endregion

		/// <summary>
		/// This method will be called when the selection was changed.
		/// </summary>
		protected abstract void Doc_SelectionChanged( object sender, SelectionChangedEventArgs e );

		/// <summary>
		/// This method will be called when the content was changed.
		/// </summary>
		protected abstract void Doc_ContentChanged( object sender, ContentChangedEventArgs e );

		#region Properties
		/// <summary>
		/// Gets or sets the document displayed in this view.
		/// </summary>
		public virtual Document Document
		{
			get{ return _Document; }
			set
			{
				if( value == null )
					throw new ArgumentNullException();

				// uninstall event handlers from old document
				if( _Document != null )
				{
					_Document.SelectionChanged -= Doc_SelectionChanged;
					_Document.ContentChanged -= Doc_ContentChanged;
				}

				// replace document
				_Document = value;
				
				// install event handlers to the new document
				value.SelectionChanged += Doc_SelectionChanged;
				value.ContentChanged += Doc_ContentChanged;
			}
		}

		/// <summary>
		/// Gets number of the physical lines.
		/// </summary>
		public abstract int LineCount
		{
			get;
		}

		/// <summary>
		/// Gets or sets width of the virtual text area (line number area is not included).
		/// </summary>
		public virtual int TextAreaWidth
		{
			get{ return _TextAreaWidth; }
			set{ _TextAreaWidth = value; }
		}

		/// <summary>
		/// Gets or sets size of the currently visible area (line number area is included).
		/// </summary>
		public Size VisibleSize
		{
			get{ return _VisibleSize; }
			set{ _VisibleSize = value; }
		}

		/// <summary>
		/// Gets or sets the font used for drawing text.
		/// </summary>
		public virtual Font Font
		{
			get{ return _Font; }
			set
			{
				if( value == null )
					throw new ArgumentException( "invalid operation; View.Font was set to null." );

				// because UI module's Font property must be set before this,
				// set UI module's one if it's not set yet
				if( _UI.Font.Name != value.Name
					|| _UI.Font.Size != value.Size )
				{
					_UI.Font = value;
					return;
				}

				// apply font
				_Font = value;
				_Gra.Font = value;

				// update font metrics
				UpdateMetrics();
				Invalidate();
			}
		}

		void UpdateMetrics()
		{
			// re-calc tab width (in px)
			StringBuilder buf = new StringBuilder();
			for( int i=0; i<_TabWidth; i++ )
				buf.Append( ' ' );
			_TabWidthInPx = _Gra.MeasureText( buf.ToString() ).Width;

			// update font metrics
			_LineNumWidth = _Gra.MeasureText( "0000" ).Width;
			_SpaceWidth = _Gra.MeasureText( " " ).Width;
			_LCharWidth = _Gra.MeasureText( "l" ).Width;
			_FullSpaceWidth = _Gra.MeasureText( "\x3000" ).Width;
			_LineHeight = _Gra.MeasureText( "Mp" ).Height;
		}

		/// <summary>
		/// Gets or sets whether the current line would be drawn with underline or not.
		/// </summary>
		public bool HighlightsCurrentLine
		{
			get{ return _HighlightCurrentLine; }
			set
			{
				_HighlightCurrentLine = value;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to show line number or not.
		/// </summary>
		public bool ShowLineNumber
		{
			get{ return _ShowLineNumber; }
			set
			{
				// update field
				_ShowLineNumber = value;

				// send dummy scroll event
				// to update screen position of the caret
				_UI.Scroll( Rectangle.Empty, 0, 0 );
			}
		}

		/// <summary>
		/// Gets or sets whether to draw half-width space with special graphic or not.
		/// </summary>
		public bool DrawsSpace
		{
			get{ return (_DrawSpecialCharFlag & SpecialChar.Space) != 0; }
			set
			{
				if( value )
					_DrawSpecialCharFlag |= SpecialChar.Space;
				else
					_DrawSpecialCharFlag &= ~SpecialChar.Space;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw full-width space with special graphic or not.
		/// </summary>
		public bool DrawsFullWidthSpace
		{
			get{ return (_DrawSpecialCharFlag & SpecialChar.FullWidthSpace) != 0; }
			set
			{
				if( value )
					_DrawSpecialCharFlag |= SpecialChar.FullWidthSpace;
				else
					_DrawSpecialCharFlag &= ~SpecialChar.FullWidthSpace;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw tab character with special graphic or not.
		/// </summary>
		public bool DrawsTab
		{
			get{ return (_DrawSpecialCharFlag & SpecialChar.Tab) != 0; }
			set
			{
				if( value )
					_DrawSpecialCharFlag |= SpecialChar.Tab;
				else
					_DrawSpecialCharFlag &= ~SpecialChar.Tab;
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets whether to draw EOL code with special graphic or not.
		/// </summary>
		public bool DrawsEolCode
		{
			get{ return (_DrawSpecialCharFlag & SpecialChar.Eol) != 0; }
			set
			{
				if( value )
					_DrawSpecialCharFlag |= SpecialChar.Eol;
				else
					_DrawSpecialCharFlag &= ~SpecialChar.Eol;
				Invalidate();
			}
		}

		/// <summary>
		/// Color set used for displaying text.
		/// </summary>
		public ColorScheme ColorScheme
		{
			get{ return Document.ColorScheme; }
			set{ Document.ColorScheme = value; }
		}

		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
		public int TabWidth
		{
			get{ return _TabWidth; }
			set
			{
				if( value <= 0 )
					throw new InvalidOperationException( "View.TabWidth must be a positive integer." );

				_TabWidth = value;
				UpdateMetrics();
				Invalidate();
			}
		}
		
		/// <summary>
		/// Gets tab width in pixel.
		/// </summary>
		public int TabWidthInPx
		{
			get{ return _TabWidthInPx; }
		}

		internal int DragThresh
		{
			get{ return _LCharWidth; }
		}
		#endregion

		#region Appearance States
		/// <summary>
		/// Gets or sets index of the line which is displayed at top of this view.
		/// </summary>
		public int FirstVisibleLine
		{
			get{ return _FirstVisibleLine; }
			set
			{
				if( value < 0 )
					throw new ArgumentException( "invalid operation; View.FirstVisibleLine was set to "+value+"." );
				_FirstVisibleLine = value;
			}
		}

		/// <summary>
		/// Gets or sets x-coordinate of the view's origin currently displayed.
		/// </summary>
		internal int ScrollPosX
		{
			get{ return _ScrollPosX; }
			set
			{
				if( value < 0 )
					throw new ArgumentException( "invalid operation; View.ScrollPosX was set to "+value+"." );
				_ScrollPosX = value;
			}
		}

		/// <summary>
		/// Gets height of a line in pixel.
		/// </summary>
		public int LineHeight
		{
			get{ return _LineHeight; }
		}

		/// <summary>
		/// Gets distance between lines in pixel.
		/// </summary>
		public int LineSpacing
		{
			get{ return _LineHeight+1; }
		}
		#endregion

		#region Desired Column Management
		/// <summary>
		/// Sets desired column to current column index
		/// (When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value).
		/// </summary>
		public void SetDesiredColumn()
		{
			_DesiredColumn = GetVirPosFromIndex( _Document.CaretIndex ).X;
		}

		/// <summary>
		/// Gets "desired column"
		/// (When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value).
		/// </summary>
		internal int GetDesiredColumn()
		{
			return _DesiredColumn;
		}
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		public abstract Point GetVirPosFromIndex( int index );

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		public abstract Point GetVirPosFromIndex( int lineIndex, int columnIndex );

		/// <summary>
		/// Gets char-index of the char at the point specified by location in the virtual space.
		/// </summary>
		/// <returns>the index of the char or -1 if invalid point was specified.</returns>
		public abstract int GetIndexFromVirPos( Point pt );

		/// <summary>
		/// Converts a coordinate in virtual space to a coordinate in screen.
		/// </summary>
		public void VirtualToScreen( ref Point pt )
		{
			pt.Offset( -(_ScrollPosX - TextAreaX), -(_FirstVisibleLine * LineSpacing) );
		}

		/// <summary>
		/// Converts a coordinate in screen to a coordinate in virtual space.
		/// </summary>
		public void ScreenToVirtual( ref Point pt )
		{
			pt.Offset( _ScrollPosX - TextAreaX, _FirstVisibleLine * LineSpacing );
		}

		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public abstract int GetLineHeadIndex( int lineIndex );

		/// <summary>
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public abstract int GetLineHeadIndexFromCharIndex( int charIndex );

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public abstract void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex );

		/// <summary>
		/// Calculates char-index from physical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public abstract int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex );
		#endregion

		#region Scroll
		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		public void ScrollToCaret()
		{
			Rectangle threshRect = new Rectangle();
			Point caretPos;
			int vDelta = 0, hDelta;

			// make rentangle of virtual text view
			threshRect.X = ScrollPosX;
			threshRect.Y = FirstVisibleLine * LineSpacing;
			threshRect.Width = _VisibleSize.Width - TextAreaX;
			threshRect.Height = _VisibleSize.Height - LineSpacing;

			// calculate threshold to do ScrollToCaret
			if( UserPref.AutoScrollNearWindowBorder )
			{
				threshRect.X += _SpaceWidth;
				threshRect.Width -= _SpaceWidth << 1;
				threshRect.Y += LineSpacing;
				threshRect.Height -= LineSpacing + (LineSpacing >> 1); // (*1.5)
			}

			// calculate caret position
			caretPos = GetVirPosFromIndex( Document.CaretIndex );
			if( threshRect.Left <= caretPos.X
				&& caretPos.X <= threshRect.Right
				&& threshRect.Top <= caretPos.Y
				&& caretPos.Y <= threshRect.Bottom )
			{
				return; // caret is already visible
			}

			// calculate horizontal offset to the position where we desire to scroll to
			hDelta = 0;
			if( threshRect.Right <= caretPos.X )
			{
				// scroll to right
				hDelta = caretPos.X - (threshRect.Right - TabWidthInPx);
			}
			else if( caretPos.X < threshRect.Left )
			{
				// scroll to left
				hDelta = caretPos.X - (threshRect.Left + TabWidthInPx);
			}

			// calculate vertical offset to the position where we desire to scroll to
			vDelta = 0;
			if( threshRect.Bottom <= caretPos.Y )
			{
				// scroll down
				vDelta = (caretPos.Y + LineSpacing) - threshRect.Bottom;
			}
			else if( caretPos.Y < threshRect.Top )
			{
				// scroll up
				vDelta = caretPos.Y - threshRect.Top;
			}

			// return offset
			Scroll( vDelta / LineSpacing );
			HScroll( hDelta );
		}

		/// <summary>
		/// Scroll vertically.
		/// </summary>
		public void Scroll( int lineDelta )
		{
			int delta;
			Rectangle clipRect;

			if( lineDelta == 0 )
				return;

			// calculate scroll distance
			if( _FirstVisibleLine + lineDelta < 0 )
			{
				delta = -FirstVisibleLine;
			}
			else if( LineCount-1 < _FirstVisibleLine + lineDelta )
			{
				delta = LineCount - 1 - _FirstVisibleLine;
			}
			else
			{
				delta = lineDelta;
			}

			// make clipping rectangle
			clipRect = new Rectangle( 0, 0, _VisibleSize.Width, _VisibleSize.Height );

			// do scroll
			_FirstVisibleLine += delta;
			_UI.Scroll( clipRect, 0, -(delta * LineSpacing) );
		}

		/// <summary>
		/// Scroll horizontally.
		/// </summary>
		public void HScroll( int columnDelta )
		{
			int deltaInPx;
			Rectangle clipRect = new Rectangle();

			if( columnDelta == 0 )
				return;

			// calculate scroll distance
			if( ScrollPosX + columnDelta < 0 )
			{
				if( ScrollPosX == 0 )
					return;
				deltaInPx = -ScrollPosX;
			}
			else if( TextAreaWidth <= ScrollPosX+columnDelta )
			{
				if( TextAreaWidth == ScrollPosX+columnDelta )
					return;
				deltaInPx = (TextAreaWidth - ScrollPosX);
			}
			else
			{
				deltaInPx = columnDelta;
			}

			// make clipping rectangle
			clipRect.X = TextAreaX;
			//NO_NEED//clipRect.Y = 0;
			clipRect.Width = _VisibleSize.Width - TextAreaX;
			clipRect.Height = _VisibleSize.Height;

			// do scroll
			_ScrollPosX += deltaInPx;
			_UI.Scroll( clipRect, -deltaInPx, 0 );
		}
		#endregion

		#region Communication between UI Module
		/// <summary>
		/// UI module must call this to synchronize
		/// visible size between UI module and view.
		/// </summary>
		internal void HandleSizeChanged( Size newSize )
		{
			_VisibleSize = newSize;
		}

		/// <summary>
		/// Requests invalidation of whole area.
		/// </summary>
		public void Invalidate()
		{
			_UI.Invalidate();
		}

		/// <summary>
		/// Requests invalidation of specified area.
		/// </summary>
		/// <param name="rect">rectangle area to be invalidate (in screen coordinate)</param>
		public void Invalidate( Rectangle rect )
		{
//DEBUG//_Gra.ForeColor=Color.Red;_Gra.DrawLine(rect.Left,rect.Top,rect.Right,rect.Bottom);_Gra.DrawLine(rect.Left,rect.Bottom,rect.Right,rect.Top);DebugUtl.Sleep(400);
			_UI.Invalidate( rect );
		}
		#endregion

		#region Utilities
		internal int TextAreaX
		{
			get
			{
				if( _ShowLineNumber )
					return _LineNumWidth + 4;
				else
					return 0;
			}
		}
		#endregion
	}
}
