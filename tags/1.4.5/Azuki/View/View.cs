﻿// file: View.cs
// brief: Platform independent view implementation of Azuki engine.
// author: YAMAMOTO Suguru
// update: 2009-11-14
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
	abstract partial class View : IView, IDisposable
	{
		#region Fields and Types
		const int DefaultTabWidth = 8;
		static readonly int[] _LineNumberSamples = new int[]{
			9999,
			99999,
			999999,
			9999999,
			99999999,
			999999999,
			2000000000
		};
		protected IUserInterface _UI;
		Font _Font;
		int _TextAreaWidth = 1024;

		//--- for drawing ---
		ColorScheme _ColorScheme = ColorScheme.Default;
		Size _VisibleSize = new Size( 300, 300 );
		protected IGraphics _Gra = null;
		int _LastUsedLineNumberSample = _LineNumberSamples[0];
		protected int _LineNumAreaWidth = 0;// Width of the line number area in pixel
		protected int _SpaceWidth;			// Width of a space char (U+0020) in pixel
		protected int _FullSpaceWidth = 0;	// Width of a full-width space char (U+3000) in pixel
		int _LineHeight;
		int _TabWidth = DefaultTabWidth;
		int _TabWidthInPx;
		int _LCharWidth;
		DrawingOption _DrawingOption
			= DrawingOption.DrawsTab
			| DrawingOption.DrawsFullWidthSpace
			| DrawingOption.DrawsEol
			| DrawingOption.ShowsLineNumber
			| DrawingOption.HighlightCurrentLine;
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
			this._ColorScheme = other._ColorScheme;
			this._DrawingOption = other._DrawingOption;
			//DO_NOT//this._Gra = other._Gra;
			//DO_NOT//this._LCharWidth = other._LCharWidth;
			//DO_NOT//this._LineHeight = other._LineHeight;
			//DO_NOT//this._LineNumAreaWidth = other._LineNumAreaWidth;
			//DO_NOT//this._SpaceWidth = other._SpaceWidth;
			this._TabWidth = other._TabWidth;
			//DO_NOT//this._TabWidthInPx = other._TabWidthInPx;
			this._TextAreaWidth = other._TextAreaWidth;
			//DO_NOT//this._UI = other._UI;
			this._VisibleSize = other._VisibleSize;

			// set Font through property
			if( other._Font != null )
				this.Font = other.Font;

			// finally, re-calculate graphic metrics
			// (because there is a metric which needs a reference to Document to be calculated
			// but it cannnot be set Document before setting Font by structural reason)
			UpdateMetrics();
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
					throw new ArgumentNullException( "View.Font was set to null." );

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

		protected void UpdateMetrics()
		{
			StringBuilder buf = new StringBuilder( 32 );
			_LastUsedLineNumberSample = _LineNumberSamples[0];

			// calculate tab width in pixel
			for( int i=0; i<_TabWidth; i++ )
			{
				buf.Append( ' ' );
			}
			_TabWidthInPx = _Gra.MeasureText( buf.ToString() ).Width;

			// update other metrics
			_SpaceWidth = _Gra.MeasureText( " " ).Width;
			_LCharWidth = _Gra.MeasureText( "l" ).Width;
			_FullSpaceWidth = _Gra.MeasureText( "\x3000" ).Width;
			_LineHeight = _Gra.MeasureText( "Mp" ).Height;
			if( this.Document != null )
			{
				_LastUsedLineNumberSample = Document.ViewParam.MaxLineNumber;
			}
			_LineNumAreaWidth = _Gra.MeasureText( _LastUsedLineNumberSample.ToString() ).Width + 1;
		}
		#endregion

		#region Drawing Options
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

				// send dummy scroll event
				// to update screen position of the caret
				_UI.Scroll( Rectangle.Empty, 0, 0 );
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
		public int TabWidth
		{
			get{ return _TabWidth; }
			set
			{
				if( value <= 0 )
					throw new ArgumentOutOfRangeException( "value", "TabWidth must not be a negative number (given value:"+value+".)" );

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

		/// <summary>
		/// Gets width of space character in pixel.
		/// </summary>
		public int SpaceWidthInPx
		{
			get{ return _SpaceWidth; }
		}

		internal int DragThresh
		{
			get{ return _LCharWidth; }
		}
		#endregion

		#region States
		/// <summary>
		/// Gets or sets index of the line which is displayed at top of this view.
		/// </summary>
		/// <remarks>
		/// This property simply accesses Document.ViewParam.FirstVisibleLine property.
		/// </remarks>
		public int FirstVisibleLine
		{
			get{ return Document.ViewParam.FirstVisibleLine; }
			set{ Document.ViewParam.FirstVisibleLine = value; }
		}

		/// <summary>
		/// Gets or sets x-coordinate of the view's origin.
		/// </summary>
		/// <remarks>
		/// This property simply accesses Document.ViewParam.ScrollPosX property.
		/// </remarks>
		internal int ScrollPosX
		{
			get{ return Document.ViewParam.ScrollPosX; }
			set{ Document.ViewParam.ScrollPosX = value; }
		}

		/// <summary>
		/// Gets height of each lines in pixel.
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
		/// Sets column index of the current caret position to "desired column" value.
		/// </summary>
		/// <remarks>
		/// When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value.
		/// Note that "desired column" is associated with each document
		/// so this value may change when Document property was set to another document.
		/// </remarks>
		public void SetDesiredColumn()
		{
			Document.ViewParam.DesiredColumn = GetVirPosFromIndex( Document.CaretIndex ).X;
		}

		/// <summary>
		/// Gets current "desired column" value.
		/// </summary>
		/// <remarks>
		/// When the caret moves up or down,
		/// Azuki tries to set next caret's column index to this value.
		/// </remarks>
		public int GetDesiredColumn()
		{
			return Document.ViewParam.DesiredColumn;
		}
		#endregion

		#region Position / Index Conversion
		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		public abstract Point GetVirPosFromIndex( int index );

		/// <summary>
		/// Calculates location in the virtual space of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
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
			pt.Offset( -(ScrollPosX - TextAreaX), -(FirstVisibleLine * LineSpacing) );
		}

		/// <summary>
		/// Converts a coordinate in screen to a coordinate in virtual space.
		/// </summary>
		public void ScreenToVirtual( ref Point pt )
		{
			pt.Offset( ScrollPosX - TextAreaX, FirstVisibleLine * LineSpacing );
		}

		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract int GetLineHeadIndex( int lineIndex );

		/// <summary>
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract int GetLineHeadIndexFromCharIndex( int charIndex );

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex );

		/// <summary>
		/// Calculates char-index from physical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public abstract int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex );
		#endregion

		#region Operations
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
				if( (this is PropWrapView) == false )
				{
					threshRect.X += _SpaceWidth;
					threshRect.Width -= _SpaceWidth << 1;
				}
				if( 0 < FirstVisibleLine )
				{
					threshRect.Y += LineSpacing;
					threshRect.Height -= LineSpacing;
				}
				threshRect.Height -= (LineSpacing >> 1); // (*1.5)
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
			if( FirstVisibleLine + lineDelta < 0 )
			{
				delta = -FirstVisibleLine;
			}
			else if( LineCount-1 < FirstVisibleLine + lineDelta )
			{
				delta = LineCount - 1 - FirstVisibleLine;
			}
			else
			{
				delta = lineDelta;
			}

			// make clipping rectangle
			clipRect = new Rectangle( 0, 0, _VisibleSize.Width, _VisibleSize.Height );

			// do scroll
			FirstVisibleLine += delta;
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
			ScrollPosX += deltaInPx;
			_UI.Scroll( clipRect, -deltaInPx, 0 );
		}

		/// <summary>
		/// Requests to invalidate whole area.
		/// </summary>
		public void Invalidate()
		{
			_UI.Invalidate();
		}

		/// <summary>
		/// Requests to invalidate specified area.
		/// </summary>
		/// <param name="rect">rectangle area to be invalidate (in screen coordinate)</param>
		public void Invalidate( Rectangle rect )
		{
//DEBUG//_Gra.ForeColor=Color.Red;_Gra.DrawLine(rect.Left,rect.Top,rect.Right,rect.Bottom);_Gra.DrawLine(rect.Left,rect.Bottom,rect.Right,rect.Top);DebugUtl.Sleep(400);
			_UI.Invalidate( rect );
		}

		/// <summary>
		/// Requests to invalidate area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be invalidated.</param>
		/// <param name="endIndex">End text index of the area to be invalidated.</param>
		public abstract void Invalidate( int beginIndex, int endIndex );

		/// <summary>
		/// Sets font size to larger one.
		/// </summary>
		public void ZoomIn()
		{
			// calculate next font size
			float newSize = (float)(Font.Size / 0.9);

			// apply
			Font = new Font( Font.Name, newSize, Font.Style );
		}

		/// <summary>
		/// Sets font size to smaller one.
		/// </summary>
		public void ZoomOut()
		{
			// calculate next font size
			float newSize = (float)(Font.Size * 0.9);
			if( newSize < 1 )
			{
				return;
			}

			// apply
			Font = new Font( Font.Name, newSize, Font.Style );
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
			// adjust for new document
			UpdateLineNumberWidth();
		}

		/// <summary>
		/// This method will be called when the selection was changed.
		/// </summary>
		internal abstract void HandleSelectionChanged( object sender, SelectionChangedEventArgs e );

		/// <summary>
		/// This method will be called when the content was changed.
		/// </summary>
		internal virtual void HandleContentChanged( object sender, ContentChangedEventArgs e )
		{
			UpdateLineNumberWidth();
		}

		/// <summary>
		/// Updates width of the line number area.
		/// </summary>
		void UpdateLineNumberWidth()
		{
			DebugUtl.Assert( this.Document != null );

			// find minimum value from samples for calculating width of line number area
			for( int i=0; i<_LineNumberSamples.Length; i++ )
			{
				if( Document.LineCount <= _LineNumberSamples[i] )
				{
					Document.ViewParam.MaxLineNumber = _LineNumberSamples[i];
					if( _LastUsedLineNumberSample != _LineNumberSamples[i] )
					{
						UpdateMetrics();
						Invalidate();
					}
					return;
				}
			}
		}
		#endregion

		#region Utilities
		internal int TextAreaX
		{
			get
			{
				if( ShowLineNumber )
					return _LineNumAreaWidth + 4;
				else
					return 0;
			}
		}
		#endregion
	}
}