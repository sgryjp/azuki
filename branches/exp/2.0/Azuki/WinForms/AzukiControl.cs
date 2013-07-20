﻿// file: AzukiControl.cs
// brief: User interface for WinForms framework
//=========================================================
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace Sgry.Azuki.WinForms
{
	using IHighlighter = Highlighter.IHighlighter;

	/// <summary>
	/// Azuki user interface for Windows.Forms framework.
	/// </summary>
	/// <remarks>
	/// <para>
	/// AzukiControl class is a GUI component class provided for Windows.Forms framework.
	/// In programming of Windows.Forms framework,
	/// this class will be the most important class
	/// and thus very basic operations in Azuki can be done through this class.
	/// </para>
	/// <para>
	/// AzukiControl class is designed to cooperate with Microsoft Visual Studio
	/// so that it can be added to toolbox of visual designer.
	/// Once AzukiControl was added to toolbox,
	/// it can be used like standard GUI components such as System.Windows.Forms.Button;
	/// placing and layout with drag&amp;drop or resizing by dragging edge of component and so on.
	/// </para>
	/// <para>
	/// AzukiControl is an implementation of IUserInterface
	/// which expresses the user interface
	/// (front-end which directly interact with user action)
	/// of Azuki engine.
	/// Although currently there is no other implementation for other framework or platform,
	/// If programmer want to make platform independent program,
	/// using AzukiControl through IUserInterface will be much appropriate.
	/// </para>
	/// </remarks>
	public class AzukiControl : Control, IUserInterface
	{
		#region Types, Constants and Fields
		static int _ScrollBarWidth = 0;
		
		delegate IGraphics GetIGraphicsProc();
		delegate void InvalidateProc1();
		delegate void InvalidateProc2( Rectangle rect );
		
		UiImpl _Impl;
		Size _CaretSize = new Size( UiImpl.DefaultCaretWidth, 10 );
		bool _AcceptsReturn = true;
		bool _AcceptsTab = true;
		bool _ShowsHScrollBar = true;
		bool _ShowsVScrollBar = true;
		bool _UseCtrlTabToMoveFocus = true;
		int _WheelPos = 0;
		BorderStyle _BorderStyle = BorderStyle.Fixed3D;
		Point _LastMouseDownPos;
		bool _LastAltWasForRectSelect = false;
		
		GetIGraphicsProc _getIGraphicsProc = null;
		InvalidateProc1 _invalidateProc1 = null;
		InvalidateProc2 _invalidateProc2 = null;
		WinFormsTimer _HighlighterDelayTimer;
		Action<MouseCursor> _SetCursorGraphicDelegate = null;
		
		IntPtr _OriginalWndProcObj = IntPtr.Zero;
		WinApi.WNDPROC _CustomWndProcObj = null;
		#endregion
		
		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public AzukiControl()
		{
			// generate core implementation
			_Impl = new UiImpl( this );
			Document = new Document();
			ViewType = ViewType.Proportional; // (setting ViewType installs document event handlers)

			// setup default keybind
			ResetKeyBind();
		}

		/// <summary>
		/// Disposes resources used by this AzukiControl.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
			if( _Impl != null )
			{
				_Impl.Dispose();
				_Impl = null;
			}
		}

		/// <summary>
		/// Invokes HandleCreated event.
		/// </summary>
		protected override void OnHandleCreated( EventArgs e )
		{
			base.OnHandleCreated( e );

			_HighlighterDelayTimer = new WinFormsTimer();
			_HighlighterDelayTimer.Tick += delegate {
				_HighlighterDelayTimer.Enabled = false;
				if( _Impl != null )
					_Impl.ExecHighlighter();
			};

			// rewrite window procedure at first
			RewriteWndProc();

			// set default value for each scroll bar
			// (setting scroll bar range forces the window to have style of WS_VSCROLL/WS_HSCROLL)
			WinApi.SetScrollRange( Handle, false, 0, 1, 1 );
			WinApi.SetScrollRange( Handle, true, 0, 1, 1 );
			
			base.Cursor = Cursors.IBeam;
			this.Font = base.Font;
			this.BorderStyle = _BorderStyle;

			WinApi.CreateCaret( Handle, _CaretSize );
			WinApi.SetCaretPos( 0, 0 );

			// calculate scrollbar width
			using( ScrollBar sb = new VScrollBar() )
			{
				_ScrollBarWidth = sb.Width;
			}
		}

		/// <summary>
		/// Invokes HandleDestroyed event.
		/// </summary>
		protected override void OnHandleDestroyed( EventArgs e )
		{
			base.OnHandleDestroyed( e );

			// destroy caret
			WinApi.DestroyCaret();
		}
		#endregion

		#region IUserInterface - Associated View and Document
		/// <summary>
		/// Gets or sets the document which is the current editing target.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Document Document
		{
			get
			{
				if( _Impl == null )
				{
					return null;
				}
				else
				{
					return _Impl.Document;
				}
			}
			set
			{
				if( value == null )
					throw new ArgumentNullException();

				// uninstall event handler
				if( _Impl.Document != null )
				{
					_Impl.Document.ContentChanged -= Document_ContentChanged;
				}

				// switch to the new document
				_Impl.Document = value;

				// install event handler
				_Impl.Document.ContentChanged += Document_ContentChanged;
			}
		}

		void Document_ContentChanged( object sender, ContentChangedEventArgs e )
		{
			// just invoke TextChanged event of this Control.
			base.OnTextChanged( e );
		}

		/// <summary>
		/// Gets the associated view object.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IView View
		{
			get
			{
				if( _Impl == null )
					return null;

				return _Impl.View;
			}
		}

		/// <summary>
		/// Gets or sets type of the view.
		/// View type determine how to render text content.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(ViewType.Proportional)]
		[Description("Specify how to draw text content. Wrapped proportional view shows text wrapped within the width specified as ViewWidth property. Proportional view do not wrap text but draws faster.")]
		public ViewType ViewType
		{
			get{ return _Impl.ViewType; }
			set{ _Impl.ViewType = value; }
		}
		#endregion

		#region IUserInterface - KeyBind
		/// <summary>
		/// Reset keybind to default.
		/// </summary>
		public void ResetKeyBind()
		{
			const int VK_OEM4 = 219; // key code of '['
			const int VK_OEM6 = 221; // key code of ']'

			_Impl.ClearKeyBind();

			// bind keys to move caret
			SetKeyBind( Keys.Right, Actions.MoveRight );
			SetKeyBind( Keys.Left, Actions.MoveLeft );
			SetKeyBind( Keys.Down, Actions.MoveDown );
			SetKeyBind( Keys.Up, Actions.MoveUp );
			SetKeyBind( Keys.Right|Keys.Control, Actions.MoveToNextWord );
			SetKeyBind( Keys.Left|Keys.Control, Actions.MoveToPrevWord );
			SetKeyBind( Keys.Home, Actions.MoveToLineHeadSmart );
			SetKeyBind( Keys.End, Actions.MoveToLineEnd );
			SetKeyBind( Keys.PageDown, Actions.MovePageDown );
			SetKeyBind( Keys.PageUp, Actions.MovePageUp );
			SetKeyBind( Keys.Home|Keys.Control, Actions.MoveToFileHead );
			SetKeyBind( Keys.End|Keys.Control, Actions.MoveToFileEnd );

			// bind keys to set selection
			SetKeyBind( Keys.Right|Keys.Shift, Actions.SelectToRight );
			SetKeyBind( Keys.Left|Keys.Shift, Actions.SelectToLeft );
			SetKeyBind( Keys.Down|Keys.Shift, Actions.SelectToDown );
			SetKeyBind( Keys.Up|Keys.Shift, Actions.SelectToUp );
			SetKeyBind( Keys.Right|Keys.Shift|Keys.Control, Actions.SelectToNextWord );
			SetKeyBind( Keys.Left|Keys.Shift|Keys.Control, Actions.SelectToPrevWord );
			SetKeyBind( Keys.Home|Keys.Shift, Actions.SelectToLineHeadSmart );
			SetKeyBind( Keys.End|Keys.Shift, Actions.SelectToLineEnd );
			SetKeyBind( Keys.PageDown|Keys.Shift, Actions.SelectToPageDown );
			SetKeyBind( Keys.PageUp|Keys.Shift, Actions.SelectToPageUp );
			SetKeyBind( Keys.Home|Keys.Control|Keys.Shift, Actions.SelectToFileHead );
			SetKeyBind( Keys.End|Keys.Control|Keys.Shift, Actions.SelectToFileEnd );
			SetKeyBind( Keys.Right|Keys.Alt, Actions.RectSelectToRight );
			SetKeyBind( Keys.Left|Keys.Alt, Actions.RectSelectToLeft );
			SetKeyBind( Keys.Up|Keys.Alt, Actions.RectSelectToUp );
			SetKeyBind( Keys.Down|Keys.Alt, Actions.RectSelectToDown );
			SetKeyBind( Keys.Up|Keys.Alt|Keys.Shift, Actions.LineSelectToUp );
			SetKeyBind( Keys.Down|Keys.Alt|Keys.Shift, Actions.LineSelectToDown );
			SetKeyBind( Keys.A|Keys.Control, Actions.SelectAll );

			// bind keys to edit document
			SetKeyBind( Keys.Back, Actions.BackSpace );
			SetKeyBind( Keys.Back|Keys.Control, Actions.BackSpaceWord );
			SetKeyBind( Keys.Delete, Actions.Delete );
			SetKeyBind( Keys.Delete|Keys.Control, Actions.DeleteWord );
			SetKeyBind( Keys.V|Keys.Control, Actions.Paste );
			SetKeyBind( Keys.C|Keys.Control, Actions.Copy );
			SetKeyBind( Keys.X|Keys.Control, Actions.Cut );
			SetKeyBind( Keys.Z|Keys.Control, Actions.Undo );
			SetKeyBind( Keys.Z|Keys.Control|Keys.Shift, Actions.Redo );
			SetKeyBind( Keys.Y|Keys.Control, Actions.Redo );
			SetKeyBind( Keys.Enter|Keys.Control, Actions.BreakPreviousLine );
			SetKeyBind( Keys.Enter|Keys.Shift|Keys.Control, Actions.BreakNextLine );

			// bind misc keys
			SetKeyBind( (Keys)VK_OEM4|Keys.Control, Actions.GoToMatchedBracket );
			SetKeyBind( (Keys)VK_OEM6|Keys.Control, Actions.GoToMatchedBracket );
			SetKeyBind( Keys.B|Keys.Control, Actions.ToggleRectSelectMode );
			SetKeyBind( Keys.Insert, Actions.ToggleOverwriteMode );
			SetKeyBind( Keys.F5, Actions.Refresh );

			SetKeyBind( Keys.Up|Keys.Control, Actions.ScrollUp );
			SetKeyBind( Keys.Down|Keys.Control, Actions.ScrollDown );
		}

		/// <summary>
		/// Gets an action which is already associated with given key.
		/// If no action was associate with given key, returns null.
		/// </summary>
		/// <param name="keyCode">key code</param>
		public ActionProc GetKeyBind( uint keyCode )
		{
			return _Impl.GetKeyBind( keyCode );
		}

		/// <summary>
		/// Gets an action which is already associated with given key.
		/// If no action was associate with given key, returns null.
		/// </summary>
		/// <param name="keyCode">key code</param>
		public ActionProc GetKeyBind( Keys keyCode )
		{
			return _Impl.GetKeyBind( (uint)keyCode );
		}

		/// <summary>
		/// Sets or removes key-bind entry.
		/// Note that giving null to action will remove the key-bind.
		/// </summary>
		/// <param name="keyCode">key code to set/remove new action</param>
		/// <param name="action">action to be associated or null in case of removing key-bind.</param>
		public void SetKeyBind( uint keyCode, ActionProc action )
		{
			_Impl.SetKeyBind( keyCode, action );
		}

		/// <summary>
		/// Sets or removes key-bind entry.
		/// Note that giving null to action will remove the key-bind.
		/// </summary>
		/// <param name="keyCode">key code to set/remove new action</param>
		/// <param name="action">action to be associated or null in case of removing key-bind.</param>
		public void SetKeyBind( Keys keyCode, ActionProc action )
		{
			SetKeyBind( (uint)keyCode, action );
		}
		#endregion

		#region IUserInterface - Appearance
		/// <summary>
		/// Gets or sets top margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		[Category("Appearance")]
		[DefaultValue(1)]
		[Description("Specify margin at top of the text area in pixel.")]
		public int TopMargin
		{
			get{ return View.TopMargin; }
			set
			{
				View.TopMargin = value;
				UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets left margin of the view in pixel.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		[Category("Appearance")]
		[DefaultValue(1)]
		[Description("Specify margin at left of the text area in pixel.")]
		public int LeftMargin
		{
			get{ return View.LeftMargin; }
			set
			{
				View.LeftMargin = value;
				UpdateCaretGraphic();
				Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets type of the indicator on the horizontal ruler.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(HRulerIndicatorType.Segment)]
		[Description("Specify type of the indicator on the horizontal ruler.")]
		public HRulerIndicatorType HRulerIndicatorType
		{
			get{ return View.HRulerIndicatorType; }
			set{ View.HRulerIndicatorType = value; }
		}

		/// <summary>
		/// Updates size and position of the caret graphic.
		/// </summary>
		public void UpdateCaretGraphic()
		{
			if( Document == null )
				return;
			
			// calculate caret size for current caret position
			Point pos = GetPositionFromIndex( Document.CaretIndex );
			using( IGraphics g = GetIGraphics() )
			{
				_CaretSize.Height = View.LineHeight;
				_CaretSize.Width = Utl.CalcOverwriteCaretWidth(
						g, Document, _Impl.View, CaretIndex, IsOverwriteMode
					);
			}

			// update graphic
			UpdateCaretGraphic(
					new Rectangle(pos.X, pos.Y, _CaretSize.Width, _CaretSize.Height)
				);
		}

		/// <summary>
		/// Updates size and position of the caret graphic.
		/// </summary>
		public void UpdateCaretGraphic( Rectangle caretRect )
		{
			if( DesignMode )
				return;

			// do nothing if not focused
			if( Focused == false )
				return;

			// if document is already set, update caret size according to the state
			if( Document != null )
			{
				// calculate caret position and show/hide caret
				if( caretRect.X < _Impl.View.XofTextArea
					|| caretRect.Y < _Impl.View.YofTextArea )
				{
					WinApi.SetCaretPos( caretRect.X, caretRect.Y );
					WinApi.HideCaret( Handle );
				}
				else
				{
					//NO_NEED//_Caret.Destroy();
					WinApi.CreateCaret( Handle, _CaretSize );
					WinApi.SetCaretPos( caretRect.X, caretRect.Y ); // must be called after creation in CE
					
					WinApi.ShowCaret( Handle );
				}

				// move IMM window to there if exists
				WinApi.SetImeWindowPos( Handle, caretRect.Location );
			}
		}

		/// <summary>
		/// Sets graphic of mouse cursor.
		/// </summary>
		public void SetCursorGraphic( MouseCursor cursorType )
		{
			if( _SetCursorGraphicDelegate == null )
			{
				_SetCursorGraphicDelegate = SetCursorGraphic_Impl;
			}
			Invoke( _SetCursorGraphicDelegate, new object[]{cursorType} );
		}

		void SetCursorGraphic_Impl( MouseCursor cursorType )
		{
			switch( cursorType )
			{
				case MouseCursor.DragAndDrop:
					this.Cursor = Cursors.UpArrow;
					break;
				case MouseCursor.Hand:
					this.Cursor = Cursors.Hand;
					break;
				case MouseCursor.IBeam:
					this.Cursor = Cursors.IBeam;
					break;
				default:
					this.Cursor = Cursors.Arrow;
					break;
			}
		}

		/// <summary>
		/// Gets or sets font to be used for displaying text.
		/// </summary>
		public override Font Font
		{
			get{ return base.Font; }
			set
			{
				if( value == null )
					throw new ArgumentException( "invalid operation; AzukiControl.Font was set to null." );

				base.Font = value;
				if( View != null )
				{
					View.FontInfo = new FontInfo( value );
				}
			}
		}

		/// <summary>
		/// Gets or sets raw font information to be used for displaying text.
		/// </summary>
		[Browsable(false)]
		public FontInfo FontInfo
		{
			get{ return View.FontInfo; }
			set
			{
				if( value == null )
					throw new ArgumentException( "invalid operation; AzukiControl.Font was set to null." );

				try
				{
					base.Font = value.ToFont();
				}
				catch
				{}
				View.FontInfo = new FontInfo( value.Name, value.Size, value.Style );
			}
		}

		/// <summary>
		/// Gets or sets graphical style of border of this control.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(BorderStyle.Fixed3D)]
		public BorderStyle BorderStyle
		{
			get{ return _BorderStyle; }
			set
			{
				long style, exStyle;

				if( value == BorderStyle.FixedSingle )
				{
					// enable WS_BORDER window style
					style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
					style |= WinApi.WS_BORDER;
					WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );

					// disable WS_EX_CLIENTEDGE window style
					exStyle = WinApi.GetWindowLong( Handle, WinApi.GWL_EXSTYLE ).ToInt64();
					exStyle &= ~(WinApi.WS_EX_CLIENTEDGE);
					WinApi.SetWindowLong( Handle, WinApi.GWL_EXSTYLE, new IntPtr(exStyle) );
				}
				else if( value == BorderStyle.Fixed3D )
				{
					// disable WS_BORDER window style
					style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
					style &= ~(WinApi.WS_BORDER);
					WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );

					// enable WS_EX_CLIENTEDGE window style
					exStyle = WinApi.GetWindowLong( Handle, WinApi.GWL_EXSTYLE ).ToInt64();
					exStyle |= WinApi.WS_EX_CLIENTEDGE;
					WinApi.SetWindowLong( Handle, WinApi.GWL_EXSTYLE, new IntPtr(exStyle) );
				}
				else// if( value == BorderStyle.None )
				{
					// disable WS_BORDER window style
					style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
					style &= ~(WinApi.WS_BORDER);
					WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );

					// disable WS_EX_CLIENTEDGE window style
					exStyle = WinApi.GetWindowLong( Handle, WinApi.GWL_EXSTYLE ).ToInt64();
					exStyle &= ~(WinApi.WS_EX_CLIENTEDGE);
					WinApi.SetWindowLong( Handle, WinApi.GWL_EXSTYLE, new IntPtr(exStyle) );
				}

				// remember the last style
				// and force to redraw border by recalculating window dimension
				_BorderStyle = value;
				WinApi.SetWindowPos( Handle, IntPtr.Zero, Left, Top, Width, Height, WinApi.SWP_FRAMECHANGED );
			}
		}

		/// <summary>
		/// Gets or sets the index of the first visible (graphically top most) line
		/// of currently active document.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets the index of the first visible (graphically top most) line
		/// of currently active document.
		/// </para>
		/// <para>
		/// This property is just a synonym of Document.ViewParam.FirstVisibleLine
		/// so changing Document property will also changes this property value.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Document.ViewParam">Document.ViewParam</seealso>
		/// <seealso cref="Sgry.Azuki.ViewParam.FirstVisibleLine">ViewParam.FirstVisibleLine</seealso>
		[Browsable(false)]
		public int FirstVisibleLine
		{
			get{ return Document.ViewParam.FirstVisibleLine; }
			set{ Document.ViewParam.FirstVisibleLine = value; }
		}

		/// <summary>
		/// Color set used for displaying text.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ColorScheme ColorScheme
		{
			get{ return View.ColorScheme; }
			set{ View.ColorScheme = value; }
		}

		/// <summary>
		/// Gets or sets drawing options.
		/// </summary>
		[Browsable(false)]
		public DrawingOption DrawingOption
		{
			get{ return View.DrawingOption; }
			set{ View.DrawingOption = value; }
		}

		/// <summary>
		/// Gets or sets whether to show line number or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		[Description("Set true to show line number area.")]
		public bool ShowsLineNumber
		{
			get{ return View.ShowLineNumber; }
			set
			{
				if( View.ShowLineNumber != value )
				{
					View.ShowLineNumber = value;
					Invalidate();
				}
			}
		}

		/// <summary>
		/// Gets or sets whether to show horizontal ruler or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(false)]
		[Description("Set true to show horizontal ruler.")]
		public bool ShowsHRuler
		{
			get{ return View.ShowsHRuler; }
			set
			{
				if( View.ShowsHRuler != value )
				{
					View.ShowsHRuler = value;
					Invalidate();
				}
			}
		}

		/// <summary>
		/// Whether to show horizontal scroll bar or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		[Description("Set true to show horizontal scroll bar.")]
		public bool ShowsHScrollBar
		{
			get{ return _ShowsHScrollBar; }
			set
			{
				_ShowsHScrollBar = value;

				// make new style bits
				long style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
				if( _ShowsHScrollBar )
					style |= WinApi.WS_HSCROLL;
				else
					style &= ~(WinApi.WS_HSCROLL);

				// apply
				WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );
				WinApi.SetWindowPos( Handle, IntPtr.Zero, Left, Top, Width, Height, WinApi.SWP_FRAMECHANGED );
				UpdateScrollBarRange();
			}
		}

		/// <summary>
		/// Whether to show vertical scroll bar or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		[Description("Set true to show vertical scroll bar.")]
		public bool ShowsVScrollBar
		{
			get{ return _ShowsVScrollBar; }
			set
			{
				_ShowsVScrollBar = value;

				// make new style bits
				long style = WinApi.GetWindowLong( Handle, WinApi.GWL_STYLE ).ToInt64();
				if( _ShowsVScrollBar )
					style |= WinApi.WS_VSCROLL;
				else
					style &= ~(WinApi.WS_VSCROLL);

				// apply
				WinApi.SetWindowLong( Handle, WinApi.GWL_STYLE, new IntPtr(style) );
				WinApi.SetWindowPos( Handle, IntPtr.Zero, Left, Top, Width, Height, WinApi.SWP_FRAMECHANGED );
				UpdateScrollBarRange();
			}
		}

		/// <summary>
		/// Gets or sets whether to show 'dirt bar' or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether to show 'dirt bar' or not.
		/// The dirt bar is graphically a thin bar at right end of the line number area
		/// that indicates the dirty state of each text line.
		/// The state of line is one of the following states.
		/// </para>
		/// <list type="bullet">
		///		<item>LineDirtyState.Clean: the line is not modified yet.</item>
		///		<item>LineDirtyState.Dirty: the line is modified and not saved.</item>
		///		<item>LineDirtyState.Cleaned: the line is modified but saved.</item>
		/// </list>
		/// <para>
		/// Color of each line dirty state can be customized by setting
		/// ColorScheme.DirtyLineBar, ColorScheme.CleanedLineBar.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.LineDirtyState">LineDirtyState enum</seealso>
		/// <seealso cref="Sgry.Azuki.Document.GetLineDirtyState">Document.GetLineDirtyState method</seealso>
		[Category("Appearance")]
		[DefaultValue(true)]
		[Description("Set true to show a thin bar at right end of the line number area which indicates the dirty state of each text line.")]
		public bool ShowsDirtBar
		{
			get{ return View.ShowsDirtBar; }
			set{ View.ShowsDirtBar = value; }
		}

		/// <summary>
		/// Gets or sets whether the current line would be drawn with underline or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool HighlightsCurrentLine
		{
			get{ return View.HighlightsCurrentLine; }
			set{ View.HighlightsCurrentLine = value; }
		}

		/// <summary>
		/// Gets or sets whether to highlight matched bracket or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool HighlightsMatchedBracket
		{
			get{ return View.HighlightsMatchedBracket; }
			set{ View.HighlightsMatchedBracket = value; }
		}

		/// <summary>
		/// Gets or sets whether to show half-width space with special graphic or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool DrawsSpace
		{
			get{ return View.DrawsSpace; }
			set{ View.DrawsSpace = value; }
		}

		/// <summary>
		/// Gets or sets whether to show full-width space with special graphic or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool DrawsFullWidthSpace
		{
			get{ return View.DrawsFullWidthSpace; }
			set{ View.DrawsFullWidthSpace = value; }
		}

		/// <summary>
		/// Gets or sets whether to show tab character with special graphic or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool DrawsTab
		{
			get{ return View.DrawsTab; }
			set{ View.DrawsTab = value; }
		}

		/// <summary>
		/// Gets or sets whether to show EOF mark or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(false)]
		public bool DrawsEofMark
		{
			get{ return View.DrawsEofMark; }
			set{ View.DrawsEofMark = value; }
		}

		/// <summary>
		/// Gets or sets whether to show EOL code with special graphic or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool DrawsEolCode
		{
			get{ return View.DrawsEolCode; }
			set{ View.DrawsEolCode = value; }
		}

		/// <summary>
		/// Gets or sets tab width in count of space characters.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(8)]
		public int TabWidth
		{
			get{ return View.TabWidth; }
			set{ View.TabWidth = value; }
		}

		/// <summary>
		/// Gets or sets whether to scroll beyond the last line of the document or not.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(true)]
		public bool ScrollsBeyondLastLine
		{
			get{ return View.ScrollsBeyondLastLine; }
			set{ View.ScrollsBeyondLastLine = value; }
		}

		/// <summary>
		/// Gets height of each lines in pixel.
		/// </summary>
		[Browsable(false)]
		public int LineHeight
		{
			get{ return View.LineHeight; }
		}

		/// <summary>
		/// Gets or sets size of padding between lines in pixel.
		/// </summary>
		[Category("Appearance")]
		[DefaultValue(1)]
		[Description("Height of padding between lines.")]
		public int LinePadding
		{
			get{ return View.LinePadding; }
			set{ View.LinePadding = value; }
		}

		/// <summary>
		/// Gets distance between lines in pixel.
		/// </summary>
		[Browsable(false)]
		public int LineSpacing
		{
			get{ return View.LineSpacing; }
		}

		/// <summary>
		/// Gets or sets width of the content area (including line number area).
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets width by pixel of the whole graphical area
		/// containing line number area, dirt bar area, left border, and text area.
		/// </para>
		/// <para>
		/// If you want to specify this property not by pixels but by number of characters,
		/// you can use
		/// <see cref="Sgry.Azuki.IView.HRulerUnitWidth">IView.HRulerUnitWidth</see>
		/// value as 'reasonable' avarage width of characters.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.View">AzukiControl.View property</seealso>
		[Browsable(true)]
		[Category("Appearance")]
		[Description("Width of the text content area. In proportional view, highlight line will be drawn in this width and this will be automatically expanded to enough width to show the input text. In wrapped-proportional view, text will be wrapped in this width.")]
		public int ViewWidth
		{
			get{ return _Impl.View.TextAreaWidth + _Impl.View.XofTextArea; }
			set
			{
				_Impl.View.TextAreaWidth = value - _Impl.View.XofTextArea;
				UpdateCaretGraphic();
				UpdateScrollBarRange(); // (needed for PropWrapView)
				Refresh();
			}
		}

		/// <summary>
		/// Invalidate graphic of whole area
		/// (force to be redrawn by next paint event message).
		/// </summary>
		public new void Invalidate()
		{
			if( IsHandleCreated )
			{
				if( _invalidateProc1 == null )
					_invalidateProc1 = base.Invalidate;
				Invoke( _invalidateProc1 );
			}
		}

		/// <summary>
		/// Invalidate graphic of the specified area
		/// (force to be redrawn by next paint event message).
		/// </summary>
		public new void Invalidate( Rectangle rect )
		{
			if( IsHandleCreated )
			{
				if( _invalidateProc2 == null )
					_invalidateProc2 = base.Invalidate;
				Invoke( _invalidateProc2, new object[]{rect} );
			}
		}
		#endregion

		#region IUserInterface - Behavior and Modes
		/// <summary>
		/// Gets or sets whether this document is read-only or not.
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If true, nothing can be input or deleted by keyboard and mouse.")]
		public bool IsReadOnly
		{
			get{ return Document.IsReadOnly; }
			set{ Document.IsReadOnly = value; }
		}

		/// <summary>
		/// Gets or sets whether overwrite mode is enabled or not. In overwrite
		/// mode, input character will not be inserted but replaces a character
		/// at where the caret is on.
		/// </summary>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.OverwriteModeChanged">
		/// AzukiControl.OverwriteModeChanged event
		/// </seealso>
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If true, input character will not be inserted but replaces the character at where the caret is on.")]
		public bool IsOverwriteMode
		{
			get{ return _Impl.IsOverwriteMode; }
			set{ _Impl.IsOverwriteMode = value; }
		}

		/// <summary>
		/// Gets or sets hook delegate to execute auto-indentation.
		/// If null, auto-indentation will not be performed.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets a delegate object to execute auto-indentation.
		/// There are some built-in auto-indentation hook delegates
		/// declared as members of
		/// <see cref="Sgry.Azuki.AutoIndentHooks">AutoIndentHooks</see> class.
		/// Use one of the member of AutoIndentHooks or user-made hook to enable auto-indentation,
		/// otherwise, set null to this property to disable auto-indentation.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.AutoIndentHooks">AutoIndentHooks</seealso>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AutoIndentHook AutoIndentHook
		{
			get{ return _Impl.AutoIndentHook; }
			set{ _Impl.AutoIndentHook = value; }
		}

		/// <summary>
		/// Gets or sets whether tab characters are used for indentation,
		/// instead of space characters.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property is a synonym of <see
		/// cref="Sgry.Azuki.IUserInterface.UsesTabForIndent">UsesTabForIndent
		/// </see> property.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.UsesTabForIndent">
		/// AzukiControl.UsesTabForIndent property
		/// </seealso>
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If false, tab characters are used for indentation, instead of space characters.")]
		public bool ConvertsTabToSpaces
		{
			get{ return !UsesTabForIndent; }
			set{ UsesTabForIndent = !(value); }
		}

		/// <summary>
		/// Gets or sets whether tab characters are used for indentation, instead of space characters.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets whether tab characters are used for indentation,
		/// instead of space characters.
		/// </para>
		/// <para>
		/// In addition to the case of inserting a new tab character,
		/// This property affects some other cases like next:
		/// </para>
		/// <list type="bullet">
		///		<item>
		///		When executing block-indent.
		///		</item>
		///		<item>
		///		When additional indent characters are needed.
		///		This case is about auto-indentation for specific syntax such as C/C++ language
		///		(term <term>smart-indentation</term> is more appropriate here.)
		///		In C/C++, if user hits the Enter key on a line
		///		that ends with a closing curly bracket (<c> } </c>),
		///		newly generated line will be indented one more level
		///		by inserting additional indent characters.
		///		</item>
		///		<item>
		///		When pasting rectangle selection data.
		///		Let's suppose pasting the text data
		///		when the caret is at end of a long line
		///		and the lines below is shorter than the line caret is at.
		///		In this case, whitespaces will be appended automatically
		///		for the lines below as a padding to make pasted result a 'rectangle.'
		///		</item>
		/// </list>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.TabWidth">AzukiControl.TabWidth property</seealso>
		/// <seealso cref="Sgry.Azuki.Actions.BlockIndent">Actions.BlockIndent action</seealso>
		/// <seealso cref="Sgry.Azuki.Actions.BlockUnIndent">Actions.BlockUnIndent action</seealso>
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("If true, tab characters are used for indentation, instead of space characters.")]
		public bool UsesTabForIndent
		{
			get{ return _Impl.UsesTabForIndent; }
			set{ _Impl.UsesTabForIndent = value; }
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input full-width space to a space.
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If true, an input full-width space will be automatically converted to a half-width space.")]
		public bool ConvertsFullWidthSpaceToSpace
		{
			get{ return _Impl.ConvertsFullWidthSpaceToSpace; }
			set{ _Impl.ConvertsFullWidthSpaceToSpace = value; }
		}

		/// <summary>
		/// Gets or sets whether the content will be limited to a single line.
		/// </summary>
		/// <remarks>
		/// The default value is false.
		/// </remarks>
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If this is true, the content will be limited to a single line.")]
		public bool IsSingleLineMode
		{
			get{ return _Impl.IsSingleLineMode; }
			set{ _Impl.IsSingleLineMode = value; }
		}

		/// <summary>
		/// If this is true, treats Enter key as an input and
		/// prevent pressing dialog default button.
		/// </summary>
		/// <remarks>
		/// The default value is true.
		/// </remarks>
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("If this is true, treats Enter key as an input and prevent pressing dialog default button.")]
		public bool AcceptsReturn
		{
			get{ return _AcceptsReturn; }
			set{ _AcceptsReturn = value; }
		}

		/// <summary>
		/// If this is true, treats Tab key as an input and
		/// prevent moving focus to other control in a dialog.
		/// </summary>
		/// <remarks>
		/// The default value is true.
		/// </remarks>
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("If this is true, treats Tab key as an input and prevent moving focus to other control in a dialog.")]
		public bool AcceptsTab
		{
			get{ return _AcceptsTab; }
			set{ _AcceptsTab = value; }
		}

		/// <summary>
		/// Gets whether Azuki is in line selection mode or not.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsLineSelectMode
		{
			get{ return (SelectionMode == TextDataType.Line); }
			set{ SelectionMode = TextDataType.Line; }
		}

		/// <summary>
		/// Gets whether Azuki is in rectangle selection mode or not.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsRectSelectMode
		{
			get{ return (SelectionMode == TextDataType.Rectangle); }
			set{ SelectionMode = TextDataType.Rectangle; }
		}

		/// <summary>
		/// Gets or sets how to select text.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public TextDataType SelectionMode
		{
			get{ return Document.SelectionMode; }
			set
			{
				Document.SelectionMode = value;
				Point cursorPos = PointToClient( Cursor.Position );
				_Impl.ResetCursorGraphic( cursorPos );
			}
		}

		/// <summary>
		/// Gets or sets whether caret behavior is 'sticky' or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property determines whether the caret behaves
		/// 'sticky' or not.
		/// </para>
		/// <para>
		/// Sticky caret tries to keep its desired column position
		/// unless user explicitly changes it, by hitting right or left key for instance.
		/// Normal caret updates desired column position on typing text
		/// so if user moves up or down the caret after typing,
		/// column position of it will be as same as the position
		/// finally the caret was located.
		/// On the other hand, sticky caret does not update
		/// desired column position by typing text
		/// (because user does not 'explicitly' changed it,)
		/// so column position will be restored to the position
		/// where the caret was placed before user typed text.
		/// </para>
		/// </remarks>
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If this is true, carets tries to keep its desired column position unless user explicitly changes it.")]
		public bool UsesStickyCaret
		{
			get{ return _Impl.UsesStickyCaret; }
			set{ _Impl.UsesStickyCaret = value; }
		}

		/// <summary>
		/// Gets or sets whether URIs in the active document
		/// should be marked automatically with built-in URI marker or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Note that built-in URI marker marks URIs in document
		/// and then Azuki shows the URIs as 'looks like URI,'
		/// but (1) clicking mouse button on them, or
		/// (2) pressing keys when the caret is at middle of a URI,
		/// makes NO ACTION BY DEFAULT.
		/// To define action on such event,
		/// programmer must implement such action as a part of 
		/// event handler of standard mouse event or keyboard event.
		/// Please refer to the <see cref="Sgry.Azuki.Marking">document of marking feature</see> for details.
		/// </para>
		/// </remarks>
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("If this is true, URIs written in the document will automatically marked.")]
		public bool MarksUri
		{
			get{ return _Impl.MarksUri; }
			set{ _Impl.MarksUri = value; }
		}
		#endregion

		#region IUserInterface - Edit Actions
		/// <summary>
		/// Executes UNDO.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method restores the modification lastly done for currently active document.
		/// If there is no UNDOable action, this method will do nothing.
		/// </para>
		/// <para>
		/// To get whether any UNDOable action exists or not,
		/// use <see cref="Sgry.Azuki.WinForms.AzukiControl.CanUndo">CanUndo</see> property.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.CanUndo">AzukiControl.CanUndo property</seealso>
		/// <seealso cref="Sgry.Azuki.Document.Undo">Document.Undo method</seealso>
		public void Undo()
		{
			Actions.Undo( this );
		}

		/// <summary>
		/// Gets whether an available UNDO action exists or not.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets whether one or more UNDOable action exists or not.
		/// </para>
		/// <para>
		/// To execute UNDO, use <see cref="Sgry.Azuki.WinForms.AzukiControl.Undo">Undo</see> method.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.Undo">AzukiControl.Undo method</seealso>
		/// <seealso cref="Sgry.Azuki.Document.CanUndo">Document.CanUndo property</seealso>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CanUndo
		{
			get{ return Document.CanUndo; }
		}

		/// <summary>
		/// Clears all stacked edit histories in currently active document.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This method clears all editing histories for
		/// UNDO or REDO action in currently active document.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IUserInterface.ClearHistory">IUserInterface.ClearHistory method</seealso>
		/// <seealso cref="Sgry.Azuki.Document.ClearHistory">Document.ClearHistory method</seealso>
		public void ClearHistory()
		{
			Document.ClearHistory();
			Invalidate();
		}

		/// <summary>
		/// Gets or sets whether the edit actions will be recorded or not.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool IsRecordingHistory
		{
			get{ return Document.IsRecordingHistory; }
			set{ Document.IsRecordingHistory = value; }
		}

		/// <summary>
		/// Executes REDO.
		/// </summary>
		public void Redo()
		{
			Actions.Redo( this );
		}

		/// <summary>
		/// Gets whether an available REDO action exists or not.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CanRedo
		{
			get{ return Document.CanRedo; }
		}

		/// <summary>
		/// Executes cut action.
		/// </summary>
		public void Cut()
		{
			Actions.Cut( this );
		}

		/// <summary>
		/// Gets whether cut action can be executed or not.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CanCut
		{
			get{ return _Impl.CanCut; }
		}

		/// <summary>
		/// Executes copy action.
		/// </summary>
		public void Copy()
		{
			Actions.Copy( this );
		}

		/// <summary>
		/// Gets whether copy action can be executed or not.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CanCopy
		{
			get{ return _Impl.CanCopy; }
		}
		
		/// <summary>
		/// Executes paste action.
		/// </summary>
		public void Paste()
		{
			Actions.Paste( this );
		}

		/// <summary>
		/// Gets whether paste action can be executed or not.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CanPaste
		{
			get{ return _Impl.CanPaste; }
		}

		/// <summary>
		/// Executes delete action.
		/// </summary>
		public void Delete()
		{
			Actions.Delete( this );
		}

		/// <summary>
		/// Processes specified text as an input by user.
		/// </summary>
		/// <param name="text">The string to be processed.</param>
		/// <exception cref="System.InvalidOperationException">This object is already disposed.</exception>
		/// <exception cref="System.ArgumentNullException">Parameter 'text' is null.</exception>
		/// <remarks>
		/// <para>
		/// This method processes specified text as an input by user.
		/// Because this method is the implementation of user input,
		/// some special pre-processes will be done.
		/// The example of pre-processes are next:
		/// </para>
		/// <list type="bullet">
		///		<item>If Document.ReadOnly property is true, this method will do nothing.</item>
		///		<item>This method applies AutoIndentHook for each characters in the text.</item>
		///		<item>This method applies built-in hook processes such as converting tab to spaces.</item>
		/// </list>
		/// </remarks>
		public void HandleTextInput( string text )
		{
			_Impl.HandleTextInput( text );
		}
		#endregion

		#region IUserInterface - Selection
		/// <summary>
		/// Gets the index of where the caret is at (in char-index).
		/// </summary>
		[Browsable(false)]
		public int CaretIndex
		{
			get{ return Document.CaretIndex; }
		}

		/// <summary>
		/// Sets selection range and update the desired column.
		/// </summary>
		/// <param name="anchor">the position where the selection begins</param>
		/// <param name="caret">the position where the caret is</param>
		/// <remarks>
		/// <para>
		/// This method sets the selection range and also updates
		/// the desired column.
		/// </para>
		/// <para>
		/// Normally the caret tries to keep its x-coordinate
		/// on moving line to line unless user explicitly changes x-coordinate of it.
		/// The term 'Desired Column' means this x-coordinate which the caret tries to stick close to.
		/// </para>
		/// </remarks>
		public void SetSelection( int anchor, int caret )
		{
			Document.SetSelection( anchor, caret );
			View.SetDesiredColumn();
		}

		/// <summary>
		/// Gets range of current selection.
		/// Note that this method does not return [anchor, caret) pair but [begin, end) pair.
		/// </summary>
		/// <param name="begin">index of where the selection begins.</param>
		/// <param name="end">index of where the selection ends (selection do not includes the char at this index).</param>
		public void GetSelection( out int begin, out int end )
		{
			Document.GetSelection( out begin, out end );
		}

		/// <summary>
		/// Selects all text.
		/// </summary>
		public void SelectAll()
		{
			Actions.SelectAll( this );
		}
		#endregion

		#region IUserInterface - Content Access
		/// <summary>
		/// Gets or sets currently inputted text.
		/// </summary>
		public override string Text
		{
			get
			{
				if( Document == null )
					return null;

				return Document.Text;
			}
			set
			{
				if( Document == null )
					return;

				Document.Text = value;
				View.SetDesiredColumn();
				ScrollToCaret();
			}
		}

		/// <summary>
		/// Gets currently inputted character's count.
		/// Note that a surrogate pair or a combining character sequence
		/// will be counted as two characters.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TextLength
		{
			get{ return Document.Length; }
		}

		/// <summary>
		/// Gets text in the range [begin, end).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified range was invalid.</exception>
		public string GetTextInRange( int begin, int end )
		{
			return Document.GetText( begin, end );
		}

		/// <summary>
		/// Gets number of characters currently selected.
		/// </summary>
		/// <returns>Number of characters currently selected.</returns>
		/// <remarks>
		/// <para>
		/// This method gets number of characters currently selected,
		/// properly even if the selection mode is rectangle selection.
		/// </para>
		/// <para>
		/// Note that the difference between the end of selection and the beginning of selection
		/// is not a number of selected characters if they are selected by rectangle selection.
		/// </para>
		/// </remarks>
		public int GetSelectedTextLength()
		{
			return _Impl.GetSelectedTextLength();
		}

		/// <summary>
		/// Gets currently selected text.
		/// </summary>
		/// <returns>Currently selected text.</returns>
		/// <remarks>
		/// <para>
		/// This method gets currently selected text.
		/// </para>
		/// <para>
		/// If current selection is rectangle selection,
		/// return value will be a string that are consisted with selected partial lines (rows)
		/// joined with CR+LF.
		/// </para>
		/// </remarks>
		public string GetSelectedText()
		{
			return _Impl.GetSelectedText();
		}

		/// <summary>
		/// Gets currently selected text.
		/// </summary>
		/// <returns>Currently selected text.</returns>
		/// <remarks>
		/// <para>
		/// This method gets currently selected text.
		/// </para>
		/// <para>
		/// If current selection is rectangle selection,
		/// return value will be a string that are consisted with selected partial lines (rows)
		/// joined with specified string.
		/// </para>
		/// </remarks>
		public string GetSelectedText( string separator )
		{
			return _Impl.GetSelectedText( separator );
		}

		/// <summary>
		/// Gets length of the specified line.
		/// </summary>
		/// <param name="lineIndex">Index of the line of which to get the length.</param>
		/// <returns>Length of the specified line in character count.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">Specified index is out of valid range.</exception>
		public int GetLineLength( int lineIndex )
		{
			return Document.GetLineRange( lineIndex ).Length;
		}

		/// <summary>
		/// Gets number of lines currently inputted.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int LineCount
		{
			get{ return Document.LineCount; }
		}
		#endregion

		#region IUserInterface - Position / Index Conversion
		/// <summary>
		/// Calculates screen location of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Invalid index was given.</exception>
		public Point GetPositionFromIndex( int index )
		{
			Point virPos = View.GetVirPosFromIndex( index );
			View.VirtualToScreen( ref virPos );
			return virPos;
		}

		/// <summary>
		/// Calculates screen location of the character at specified index.
		/// </summary>
		/// <returns>The location of the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Invalid index was given.</exception>
		public Point GetPositionFromIndex( int lineIndex, int columnIndex )
		{
			Point vPos = View.GetVirPosFromIndex( lineIndex, columnIndex );
			View.VirtualToScreen( ref vPos );
			return vPos;
		}

		/// <summary>
		/// Gets char-index of the char at the point specified by screen location.
		/// </summary>
		/// <returns>The index of the character at specified location.</returns>
		public int GetIndexFromPosition( Point pt )
		{
			View.ScreenToVirtual( ref pt );
			return View.GetIndexFromVirPos( pt );
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
			return View.GetHRulerIndex( charIndex );
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
			return View.GetHRulerIndex( lineIndex, columnIndex );
		}
		#endregion

		#region IUserInterface - Screen Line/Column Index
		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineHeadIndex( int lineIndex )
		{
			return View.GetLineHeadIndex( lineIndex );
		}

		/// <summary>
		/// Gets the index of the first char in the screen line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetLineHeadIndexFromCharIndex( int charIndex )
		{
			return View.GetLineHeadIndexFromCharIndex( charIndex );
		}

		/// <summary>
		/// Calculates screen line index from char-index.
		/// </summary>
		/// <param name="charIndex">The index of the line which contains the char at this parameter will be calculated.</param>
		/// <returns>The index of the line which contains the character at specified index.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was out of range.</exception>
		public int GetLineIndexFromCharIndex( int charIndex )
		{
			return View.GetLineIndexFromCharIndex( charIndex );
		}

		/// <summary>
		/// Calculates screen line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public TextPoint GetTextPosition( int charIndex )
		{
			return View.GetTextPosition( charIndex );
		}

		/// <summary>
		/// Calculates char-index from screen line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		public int GetCharIndex( TextPoint position )
		{
			return View.GetCharIndex( position );
		}
		#endregion

		#region IUserInterface - Events
		/// <summary>
		/// Occurs soon after the document's caret was moved.
		/// </summary>
		public event EventHandler CaretMoved;

		/// <summary>
		/// For internal use only. Invokes CaretMoved event.
		/// </summary>
		public void InvokeCaretMoved()
		{
			if( CaretMoved != null )
			{
				CaretMoved( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Occures soon after rectangular selection mode was changed.
		/// </summary>
		[Obsolete("Use SelectionModeChanged event instead.", false)]
		public event EventHandler IsRectSelectModeChanged;

		/// <summary>
		/// For internal use only. Invokes IsRectSelectModeChanged event.
		/// </summary>
		[Obsolete("Use Document.InvokeSelectionModeChanged method instead.", false)]
		public void InvokeIsRectSelectModeChanged()
		{
			if( IsRectSelectModeChanged != null )
			{
				IsRectSelectModeChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Occurs soon after the overwrite mode was changed.
		/// </summary>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.IsOverwriteMode">AzukiControl.IsOverwriteMode property</seealso>
		public event EventHandler OverwriteModeChanged;

		/// <summary>
		/// For internal use only. Invokes OverwriteModeChanged event.
		/// </summary>
		public void InvokeOverwriteModeChanged()
		{
			if( OverwriteModeChanged != null )
			{
				OverwriteModeChanged( this, EventArgs.Empty );
			}
		}

		/// <summary>
		/// Occurres before a screen line was drawn.
		/// </summary>
		public event LineDrawEventHandler LineDrawing;

		/// <summary>
		/// Invokes LineDrawing event.
		/// </summary>
		public bool InvokeLineDrawing( IGraphics g, int lineIndex, Point pos )
		{
			if( LineDrawing != null )
			{
				LineDrawEventArgs e = new LineDrawEventArgs( g, lineIndex, pos );
				e.ShouldBeRedrawn = false;
				LineDrawing( this, e );
				return e.ShouldBeRedrawn;
			}
			return false;
		}

		/// <summary>
		/// Occurres after a screen line was drawn.
		/// </summary>
		public event LineDrawEventHandler LineDrawn;

		/// <summary>
		/// Invokes LineDrawn event.
		/// </summary>
		public bool InvokeLineDrawn( IGraphics g, int lineIndex, Point pos )
		{
			if( LineDrawn != null )
			{
				LineDrawEventArgs e = new LineDrawEventArgs( g, lineIndex, pos );
				e.ShouldBeRedrawn = false;
				LineDrawn( this, e );
				return e.ShouldBeRedrawn;
			}
			return false;
		}

		/// <summary>
		/// Occurres after vertical scroll happened.
		/// </summary>
		public event EventHandler VScroll;

		/// <summary>
		/// Invokes VScroll event.
		/// </summary>
		public void InvokeVScroll()
		{
			if( VScroll != null )
				VScroll( this, EventArgs.Empty );
		}

		/// <summary>
		/// Occurres after norizontal scroll happened.
		/// </summary>
		public event EventHandler HScroll;

		/// <summary>
		/// Invokes HScroll event.
		/// </summary>
		public void InvokeHScroll()
		{
			if( HScroll != null )
				HScroll( this, EventArgs.Empty );
		}
		#endregion

		#region IUserInterface - Scroll
		/// <summary>
		/// Scrolls a portion of the window.
		/// </summary>
		public void Scroll( Rectangle rect, int vOffset, int hOffset )
		{
			WinApi.ScrollWindow( Handle, vOffset, hOffset, rect );
			WinApi.SetScrollPos( Handle, false, _Impl.View.FirstVisibleLine );
			WinApi.SetScrollPos( Handle, true, _Impl.View.ScrollPosX );
			UpdateCaretGraphic();
		}

		/// <summary>
		/// Scrolls to where the caret is.
		/// </summary>
		public void ScrollToCaret()
		{
			View.ScrollToCaret();
		}

		/// <summary>
		/// Updates scrollbar's range.
		/// </summary>
		public void UpdateScrollBarRange()
		{
			int vMax, hMax;
			int vPageSize, hPageSize;
			int visibleLineCount;

			if( Document == null )
				return;

			// calculate vertical range and page size
			visibleLineCount = View.VisibleSize.Height / View.LineSpacing;
			vPageSize = Math.Max( 0, visibleLineCount-1 );
			vMax = View.LineCount - 1;
			if( ScrollsBeyondLastLine )
			{
				vMax += vPageSize - 1;
			}

			// calculate horizontal range and page size
			hMax = View.TextAreaWidth;
			hPageSize = Math.Max( 0, View.VisibleTextAreaSize.Width );

			// update the range of vertical scrollbar
			WinApi.SetScrollRange( Handle, false, 0, vMax, vPageSize );
			
			// update the range of horizontal scrollbar
			if( ShowsHScrollBar == false || ViewType == ViewType.WrappedProportional )
				WinApi.SetScrollRange( Handle, true, 0, 0, hPageSize ); // bar will be hidden
			else
				WinApi.SetScrollRange( Handle, true, 0, hMax, hPageSize );

			// then, update scroll position and caret graphic
			WinApi.SetScrollPos( Handle, false, _Impl.View.FirstVisibleLine );
			WinApi.SetScrollPos( Handle, true, _Impl.View.ScrollPosX );
		}
		#endregion

		#region IUserInterface - Others
		/// <summary>
		/// Gets a graphic interface.
		/// </summary>
		public IGraphics GetIGraphics()
		{
			if( IsHandleCreated && InvokeRequired )
			{
				if( _getIGraphicsProc == null )
					_getIGraphicsProc = GetIGraphics;
				return (IGraphics)Invoke( _getIGraphicsProc );
			}
			return Plat.Inst.GetGraphics( this );
		}

		/// <summary>
		/// Gets or sets highlighter object to highlight currently active document
		/// or null to disable highlighting.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property gets or sets highlighter for this document.
		/// </para>
		/// <para>
		/// Highlighter objects are used to highlight syntax of documents.
		/// They implements
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		/// interface and called
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter.Highlight(Sgry.Azuki.Document, ref int, ref int)">Highlight</see>
		/// method every time slightly after user input stopped to execute own highlighting logic.
		/// If null was set to this property, highlighting feature will be disabled.
		/// </para>
		/// <para>
		/// Azuki provides some built-in highlighters.
		/// See
		/// <see cref="Sgry.Azuki.Highlighter.Highlighters">Highlighter.Highlighters</see>
		/// class members.
		/// </para>
		/// <para>
		/// User can create and use custom highlighter object.
		/// If you want to create a keyword-based highlighter,
		/// you can extend
		/// <see cref="Sgry.Azuki.Highlighter.KeywordHighlighter">KeywordHighlighter</see>.
		/// If you want ot create not a keyword based one,
		/// create a class which implements
		/// <see cref="Sgry.Azuki.Highlighter.IHighlighter">IHighlighter</see>
		/// and write your own highlighting logic.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.Highlighter.Highlighters">Highlighter.Highlighters</seealso>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IHighlighter Highlighter
		{
			get{ return _Impl.Highlighter; }
			set{ _Impl.Highlighter = value; }
		}

		/// <summary>
		/// (Internal use only.) Make a highlighter run after a little moment.
		/// </summary>
		public void RescheduleHighlighting()
		{
			// reset the timer to schedule of re-highlighting
			_HighlighterDelayTimer.Enabled = false;
			_HighlighterDelayTimer.Interval = UiImpl.HighlightDelay;
			_HighlighterDelayTimer.Enabled = true;
		}

		/// <summary>
		/// Gets version of Azuki.dll.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Version Version
		{
			get
			{
				return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			}
		}
		#endregion

		#region GUI Event Handling
		/// <summary>
		/// Invokes MouseDown event with additional information through IMouseEventArgs.
		/// </summary>
		protected override void OnMouseDown( MouseEventArgs e )
		{
			// set focus manually (this is needed to get focus by mouse click)
			this.Focus();

			// store information
			_LastMouseDownPos = new Point( e.X, e.Y );

			// invoke event
			WinFormsMouseEventArgs amea = Utl.CreateWinFormsMouseEventArgs( View, e );
			base.OnMouseDown( amea );
			if( amea.Handled )
			{
				return;
			}
			if( _Impl == null )
			{
				return;
			}

			// do built-in action
			_Impl.HandleMouseDown( amea );

			// do Windows specific special actions
			if( amea.Alt )
			{
				// set flag to prevent opening menu
				// by Alt key for rectangular selection mode
				if( IsRectSelectMode )
				{
					_LastAltWasForRectSelect = true;
				}
			}
		}

		/// <summary>
		/// Invokes MouseUp event with additional information through IMouseEventArgs.
		/// </summary>
		protected override void OnMouseUp( MouseEventArgs e )
		{
			// invoke event
			WinFormsMouseEventArgs amea = Utl.CreateWinFormsMouseEventArgs( View, e );
			base.OnMouseUp( amea );
			if( amea.Handled )
			{
				return;
			}

			// do built-in action
			_Impl.HandleMouseUp( amea );
		}

		/// <summary>
		/// Invokes MouseClick event with additional information through IMouseEventArgs.
		/// </summary>
		protected override void OnMouseClick( MouseEventArgs e )
		{
			Point currentPos = PointToClient( Control.MousePosition );
			if( Utl.IsClick(currentPos, _LastMouseDownPos) )
			{
				return;
			}

			WinFormsMouseEventArgs amea = Utl.CreateWinFormsMouseEventArgs( View, e );
			base.OnMouseClick( amea );
		}

		/// <summary>
		/// Invokes MouseDoubleClick event with additional information through IMouseEventArgs.
		/// </summary>
		protected override void OnMouseDoubleClick( MouseEventArgs e )
		{
			Point currentPos = PointToClient( Control.MousePosition );
			if( Utl.IsClick(currentPos, _LastMouseDownPos) )
			{
				return;
			}

			WinFormsMouseEventArgs amea = Utl.CreateWinFormsMouseEventArgs( View, e );
			base.OnMouseDoubleClick( amea );
		}

		/// <summary>
		/// Invokes Click event with additional information through IMouseEventArgs.
		/// </summary>
		protected override void OnClick( EventArgs e )
		{
			Point currentPos = PointToClient( Control.MousePosition );
			if( Utl.IsClick(currentPos, _LastMouseDownPos) )
			{
				return;
			}

			// gather information about the event
			MouseEventArgs mea = new MouseEventArgs(
					MouseButtons.Left, 2, currentPos.X, currentPos.Y, 0
				);

			// invoke event
			WinFormsMouseEventArgs amea = Utl.CreateWinFormsMouseEventArgs( View, mea );
			base.OnClick( amea );
		}

		/// <summary>
		/// Invokes DoubleClick event with additional information through IMouseEventArgs.
		/// </summary>
		protected override void OnDoubleClick( EventArgs e )
		{
			Point currentPos = PointToClient( Control.MousePosition );
			if( Utl.IsClick(currentPos, _LastMouseDownPos) )
			{
				return;
			}

			// gather information about the event
			MouseEventArgs mea = new MouseEventArgs(
					MouseButtons.Left, 2, currentPos.X, currentPos.Y, 0
				);

			// invoke event
			WinFormsMouseEventArgs amea = Utl.CreateWinFormsMouseEventArgs( View, mea );
			base.OnDoubleClick( amea );
			if( amea.Handled )
			{
				return;
			}

			// do built-in action
			_Impl.HandleDoubleClick( amea );
		}

		/// <summary>
		/// Invokes MouseMove event with additional information through IMouseEventArgs.
		/// </summary>
		protected override void OnMouseMove( MouseEventArgs e )
		{
			// invoke event
			WinFormsMouseEventArgs amea = Utl.CreateWinFormsMouseEventArgs( View, e );
			base.OnMouseMove( amea );
			if( amea.Handled || _Impl == null )
			{
				return;
			}
			
			// do built-in action
			_Impl.HandleMouseMove( amea );
		}

		void HandleWheelEvent( int scrollOffset )
		{
			// get modifier key state
			bool shift = WinApi.IsKeyDown( Keys.ShiftKey );
			bool control = WinApi.IsKeyDown( Keys.ControlKey );
			bool alt = WinApi.IsKeyDown( Keys.Menu );

			// dispatch mouse event
			if( !control && !alt && shift )
			{
				int type = (scrollOffset < 0) ? WinApi.SB_LINEUP : WinApi.SB_LINEDOWN;
				HandleHScrollEvent( type );
			}
			else if( control && !alt && !shift )
			{
				if( 0 < scrollOffset )
					View.ZoomOut();
				else
					View.ZoomIn();
			}
			else if( !control && !alt && !shift )
			{
				View.Scroll( scrollOffset );
			}
		}

		void HandleVScrollEvent( int scrollType )
		{
			int newPos = View.FirstVisibleLine;
			if( scrollType == WinApi.SB_LINEUP )
				newPos--;
			else if( scrollType == WinApi.SB_LINEDOWN )
				newPos++;
			else if( scrollType == WinApi.SB_PAGEUP )
				newPos -= (ClientSize.Height / View.LineSpacing);
			else if( scrollType == WinApi.SB_PAGEDOWN )
				newPos += (ClientSize.Height / View.LineSpacing);
			else if( scrollType == WinApi.SB_TOP )
				newPos = 0;
			else if( scrollType == WinApi.SB_BOTTOM )
				newPos = View.LineCount - 1;
			else if( scrollType == WinApi.SB_THUMBPOSITION
				|| scrollType == WinApi.SB_THUMBTRACK )
				newPos = WinApi.GetScrollTrackPos( Handle, false );
			else if( scrollType == WinApi.SB_ENDSCROLL )
				return;

			int delta = newPos - View.FirstVisibleLine;
			View.Scroll( delta );
		}

		void HandleHScrollEvent( int scrollType )
		{
			int newPos = _Impl.View.ScrollPosX;
			int scrollUnit = View.TabWidthInPx >> 1;

			if( scrollType == WinApi.SB_LINEUP )
				newPos -= scrollUnit;
			else if( scrollType == WinApi.SB_LINEDOWN )
				newPos += scrollUnit;
			else if( scrollType == WinApi.SB_PAGEUP )
				newPos -= (Width - _ScrollBarWidth);
			else if( scrollType == WinApi.SB_PAGEDOWN )
				newPos += (Width - _ScrollBarWidth);
			else if( scrollType == WinApi.SB_TOP )
				newPos = 0;
			else if( scrollType == WinApi.SB_BOTTOM )
			{
				int min, max;
				WinApi.GetScrollRange( Handle, true, out min, out max );
				newPos = max;
			}
			else if( scrollType == WinApi.SB_THUMBPOSITION
				|| scrollType == WinApi.SB_THUMBTRACK )
			{
				// align to scroll unit (half of tab width)
				newPos = WinApi.GetScrollTrackPos( Handle, true );
				int leftScrollPos = (newPos / scrollUnit) * scrollUnit;
				if( newPos < leftScrollPos + scrollUnit/2 )
					newPos = leftScrollPos;
				else
					newPos = leftScrollPos + scrollUnit;
			}
			else if( scrollType == WinApi.SB_ENDSCROLL )
				return;
			
			int delta = newPos - _Impl.View.ScrollPosX;
			View.HScroll( delta );
		}

		/// <summary>
		/// Invokes GotFocus event.
		/// </summary>
		protected override void OnGotFocus( EventArgs e )
		{
			base.OnGotFocus( e );
			if( _Impl == null )
			{
				return;
			}

			WinApi.CreateCaret( Handle, _CaretSize );
			UpdateCaretGraphic();
			_Impl.HandleGotFocus();
		}

		/// <summary>
		/// Invokes LostFocus event.
		/// </summary>
		protected override void OnLostFocus( EventArgs e )
		{
			base.OnLostFocus( e );
			if( _Impl == null )
			{
				return;
			}

			WinApi.HideCaret( Handle );
			_Impl.HandleLostFocus();
		}

		/// <summary>
		/// Invokes KeyDown event.
		/// </summary>
		protected override void OnKeyDown( KeyEventArgs e )
		{
			base.OnKeyDown( e );
			if( e.Handled )
			{
				return;
			}
			if( _Impl == null )
			{
				return;
			}

			_Impl.HandleKeyDown( (uint)e.KeyData );
		}

		/// <summary>
		/// Invokes KeyPress event.
		/// </summary>
		protected override void OnKeyPress( KeyPressEventArgs e )
		{
			base.OnKeyPress( e );
			if( e.Handled )
			{
				return;
			}
			if( _Impl == null )
			{
				return;
			}

			// TranslateMessage API (I think) treats some key combination specially
			// (Ctrl+I as an a HT(HorizontalTab), Ctrl+M as a LF(LineFeed) for example).
			// These behavior should not be expected by editor component users
			// and thus such char event is ignored here
			if( (e.KeyChar == '\t' && WinApi.IsKeyDown(Keys.I))
				|| (e.KeyChar == '\r' && WinApi.IsKeyDown(Keys.M))
				|| (e.KeyChar == '\n' && WinApi.IsKeyDown(Keys.J))
				|| (e.KeyChar == '\r' && WinApi.IsKeyDown(Keys.ShiftKey))
				|| (e.KeyChar == '\n' && WinApi.IsKeyDown(Keys.ControlKey))
				|| (e.KeyChar == '\r' && WinApi.IsKeyDown(Keys.LWin))
				|| (e.KeyChar == '\r' && WinApi.IsKeyDown(Keys.RWin)) )
			{
				return;
			}

			// ignore unwelcomed chars such as invisible control codes
			if( !MyIsInputChar(e.KeyChar) )
			{
				return;
			}

			// in addition, pressing TAB key with multi-line selection triggers
			// block-indent command
			if( e.KeyChar == '\t' )
			{
				int selBegin, selEnd;
				int selBeginL, selEndL;

				Document.GetSelection( out selBegin, out selEnd );
				selBeginL = Document.GetLineIndexFromCharIndex( selBegin );
				selEndL = Document.GetLineIndexFromCharIndex( selEnd );
				if( selBeginL != selEndL )
				{
					if( WinApi.IsKeyDown(Keys.ShiftKey) )
						Actions.BlockUnIndent( this );
					else
						Actions.BlockIndent( this );
					e.Handled = true;
					return;
				}
			}

			// otherwise, handle key-char event normally
			_Impl.HandleKeyPress( e.KeyChar );
			e.Handled = true;
		}

		/// <summary>
		/// Invokes Resize event.
		/// </summary>
		protected override void OnResize( EventArgs e )
		{
			base.OnResize( e );
			if( _Impl == null )
			{
				return;
			}

			if( _Impl.View != null )
			{
				_Impl.View.HandleSizeChanged( ClientSize );
			}
			UpdateScrollBarRange();
			Invalidate();
		}
		#endregion

		#region IME Reconversion
		unsafe int HandleImeReconversion( WinApi.RECONVERTSTRING* reconv )
		{
			/*
			 * There are three 'string's on executing IME reconversion.
			 * First is target string.
			 * This string will be reconverted.
			 * Second is composition string.
			 * This contains target string and MAY be the target string.
			 * Third is, string body.
			 * This contains composition string and MAY be the composition string.
			 * Typical usage is set each range as next:
			 * 
			 * - target string: selected text or adjusted by IME
			 * - composition string: selected text or adjusted by IME
			 * - string body: selected text or current line or entire text buffer it self.
			 * 
			 * IME reconversion takes two steps to be executed.
			 * On first step, IME sends WM_IME_REQUEST with IMR_RECONVERTSTRING parameter to a window
			 * to query size of string body (plus RECONVERTSTRING structure).
			 * This time, Azuki returns selected text length or length of current line if nothing selected.
			 * On second step, IME allocates memory block to store
			 * RECONVERTSTRING structure and string body for the application.
			 * This time, Azuki copies string body to the buffer and
			 * set structure members and return non-zero value (meaning OK).
			 * Then, IME will execute reconversion.
			 */
			const int MaxRangeLength = 40;
			int		rc;
			int		selBegin, selEnd;
			string	stringBody;
			int		stringBodyIndex;
			IntPtr	ime;
			char*	strPos;
			int		infoBufSize;

			// determine string body
			Document.GetSelection( out selBegin, out selEnd );
			if( selBegin != selEnd )
			{
				// something selected.
				// set them as string body, composition string, and target string.
				int end;

				// shrink range if it is unreasonably big
				end = selEnd;
				if( MaxRangeLength < end - selBegin )
				{
					end = selBegin + MaxRangeLength;
					while( Document.IsNotDividableIndex(end) )
					{
						end++;
					}
				}

				// get selected text
				stringBody = Document.GetText( selBegin, end );
				stringBodyIndex = selBegin;
			}
			else
			{
				// nothing selected.
				// set current line as string body
				// and let IME to determine composition string and target string.
				int lineIndex, lineHeadIndex, lineEndIndex;
				int begin, end;

				// get current line range
				lineIndex = Document.GetLineIndexFromCharIndex( selBegin );
				lineHeadIndex = Document.GetLineHeadIndex( lineIndex );
				lineEndIndex = lineHeadIndex + Document.GetLineRange( lineIndex ).Length;
				begin = Math.Max( lineHeadIndex, selBegin - (MaxRangeLength / 2) );
				end = Math.Min( selBegin + (MaxRangeLength / 2), lineEndIndex );

				// get current line content
				stringBody = Document.GetText( begin, end );
				stringBodyIndex = begin;
			}

			// calculate size of information buffer to communicate with IME
			infoBufSize = sizeof(WinApi.RECONVERTSTRING)
					+ Encoding.Unicode.GetByteCount(stringBody) + 1;
			if( reconv == null )
			{
				// this is the first call for re-conversion.
				// just inform IME the size of information buffer this time.
				return infoBufSize;
			}

			// validate parameters
			if( reconv->dwSize != (UInt32)infoBufSize
				|| reconv->dwVersion != 0 )
			{
				return 0;
			}

			// get IME context
			ime = WinApi.ImmGetContext( this.Handle );
			if( ime == IntPtr.Zero )
			{
				return 0;
			}

			// copy string body
			reconv->dwStrLen = (UInt32)stringBody.Length;
			reconv->dwStrOffset = (UInt32)sizeof(WinApi.RECONVERTSTRING);
			strPos = (char*)( (byte*)reconv + reconv->dwStrOffset );
			for( int i=0; i<stringBody.Length; i++ )
			{
				strPos[i] = stringBody[i];
			}
			strPos[stringBody.Length] = '\0';

			// calculate range of composition string and target string
			if( selBegin != selEnd )
			{
				// set selected range as reconversion target
				reconv->dwCompStrLen = (UInt32)( stringBody.Length );
				reconv->dwCompStrOffset = 0;
				reconv->dwTargetStrLen = reconv->dwCompStrLen;
				reconv->dwTargetStrOffset = reconv->dwCompStrOffset;
			}
			else
			{
				// let IME adjust RECONVERTSTRING parameters
				reconv->dwCompStrLen = 0;
				reconv->dwCompStrOffset = (UInt32)( (selBegin - stringBodyIndex) * 2);
				reconv->dwTargetStrLen = 0;
				reconv->dwTargetStrOffset = reconv->dwCompStrOffset;
				rc = WinApi.ImmSetCompositionStringW(
						ime,
						WinApi.SCS_QUERYRECONVERTSTRING,
						reconv, (uint)infoBufSize, null, 0
					);
				if( rc == 0 )
				{
					return 0;
				}
			}

			// select target string to make it being replaced by reconverted new string
			selBegin = stringBodyIndex + (int)(reconv->dwTargetStrOffset / 2);
			selEnd = selBegin + (int)reconv->dwTargetStrLen;
			Document.SetSelection( selBegin, selEnd );

			// adjust position of IME composition window
			WinApi.SetImeWindowPos( this.Handle,
					GetPositionFromIndex(selBegin)
				);

			// release context object
			WinApi.ImmReleaseContext( this.Handle, ime );

			return infoBufSize;
		}
		#endregion

		#region Behavior as a .NET Control
		/// <summary>
		/// Gets or sets default text color.
		/// </summary>
		/// <remarks>
		/// This property gets or sets default foreground color.
		/// Note that this is a synonym of
		/// <see cref="Sgry.Azuki.WinForms.AzukiControl.ColorScheme">AzukiControl.ColorScheme</see>.BackColor
		/// .
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.ColorScheme">AzukiControl.ColorScheme</seealso>
		[DefaultValue(0xff000000)]
		public override Color ForeColor
		{
			get
			{
				if( View != null )
					return View.ColorScheme.ForeColor;
				else
					return base.ForeColor;
			}
			set
			{
				if( View != null )
					View.ColorScheme.ForeColor = value;
				else
					base.ForeColor = value;
			}
		}

		/// <summary>
		/// Gets or sets default background color.
		/// </summary>
		/// <remarks>
		/// This property gets or sets default background color.
		/// Note that this is a synonym of
		/// <see cref="Sgry.Azuki.WinForms.AzukiControl.ColorScheme">AzukiControl.ColorScheme</see>.BackColor
		/// .
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.ColorScheme">AzukiControl.ColorScheme</seealso>
		[DefaultValue(0xfffffaf0)]
		public override Color BackColor
		{
			get
			{
				if( View != null )
					return View.ColorScheme.BackColor;
				else
					return base.BackColor;
			}
			set
			{
				if( View != null )
					View.ColorScheme.BackColor = value;
				else
					base.BackColor = value;
			}
		}
		
		/// <summary>
		/// Gets or sets whether this control uses Ctrl+Tab and Ctrl+Shift+Tab
		/// for moving focus to other controls in a dialog.
		/// </summary>
		/// <remarks>
		/// The default value is true.
		/// </remarks>
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Whether this control uses Ctrl+Tab and Ctrl+Shift+Tab for moving focus to other controls in a dialog.")]
		public bool UseCtrlTabToMoveFocus
		{
			get{ return _UseCtrlTabToMoveFocus; }
			set{ _UseCtrlTabToMoveFocus = value; }
		}

		/// <summary>
		/// This defines the characters which must be treated as input for this control.
		/// This affects mnemonic key event in a dialog and does not affect to KeyPress (WM_CHAR) event.
		/// </summary>
		protected override bool IsInputChar( char charCode )
		{
			return MyIsInputChar( charCode );
		}
		
		/// <summary>
		/// This defines the keys which must be treated as input for this control.
		/// This affects mnemonic key event in a dialog and does not affect to KeyPress (WM_CHAR) event.
		/// </summary>
		protected override bool IsInputKey( Keys keyData )
		{
			// is there an action associted with that key?
			if( _Impl.IsKeyBindDefined((uint)keyData) )
			{
				return true;
			}
			else if( _AcceptsTab
				&& (keyData == Keys.Tab || keyData == (Keys.Tab|Keys.Shift)) )
			{
				return true;
			}
			else if( _AcceptsReturn
				&& keyData == Keys.Return )
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Pre-processes window messages to override
		/// system default behavior.
		/// </summary>
		public override bool PreProcessMessage( ref Message msg )
		{
			if( WinApi.WM_KEYFIRST <= msg.Msg && msg.Msg <= WinApi.WM_KEYLAST )
			{
				if( msg.Msg == WinApi.WM_SYSKEYUP
					&& msg.WParam.ToInt32() == (int)Keys.Menu
					&& _LastAltWasForRectSelect )
				{
					_LastAltWasForRectSelect = false;
					return true;
				}
			}

			return base.PreProcessMessage( ref msg );
		}

		/// <summary>
		/// This overrides focusing strategy.
		/// </summary>
		protected override bool ProcessDialogKey( Keys keyData )
		{
			// if input key was Ctrl+Tab or Ctrl+Shift+Tab,
			// move focus to next/previous control if so configured
			if( _UseCtrlTabToMoveFocus )
			{
				if( keyData == (Keys.Tab|Keys.Control) )
					return base.ProcessDialogKey( Keys.Tab );
				else if( keyData == (Keys.Tab | Keys.Control | Keys.Shift) )
					return base.ProcessDialogKey( (Keys.Tab | Keys.Shift) );
			}

			return base.ProcessDialogKey( keyData );
		}

		bool MyIsInputChar( char charCode )
		{
			if( !_AcceptsReturn && TextUtil.IsEolChar(charCode) )
				return false;
			if( !_AcceptsTab && charCode == '\t' )
				return false;

			if( (0x20 <= charCode && charCode <= 0x7e)
				|| charCode == '\r'
				|| charCode == '\n'
				|| charCode == ' '
				|| charCode == '\t'
				|| charCode == '\x3000' // full-space-width
				|| 0x7f < charCode )
			{
				return true;
			}

			return false;
		}
		#endregion

		#region Custom Window Procedure (handling v/h scroll and paint event etc.)
		IntPtr CustomWndProc( IntPtr window, UInt32 message, IntPtr wParam, IntPtr lParam )
		{
			if( message == WinApi.WM_PAINT )
			{
				WinApi.PAINTSTRUCT ps;

				// .NET's Paint event does not inform invalidated region when double buffering was disabled.
				// In addition to this, Control.SetStyle is not supported in Compact Framework
				// and thus enabling double buffering seems impossible.
				// Therefore painting logic is called here.
				unsafe
				{
					WinApi.BeginPaint( window, &ps );

					Rectangle rect = new Rectangle( ps.paint.left, ps.paint.top, ps.paint.right-ps.paint.left, ps.paint.bottom-ps.paint.top );
					_Impl.HandlePaint( rect );

					WinApi.EndPaint( window, &ps );
				}

				// return zero here to prevent executing original painting logic of Control class.
				// (if the original logic runs,
				// we will get invalid(?) update region from BeginPaint API in Windows XP or former.)
				return IntPtr.Zero;
			}
			else if( DesignMode )
			{
				; // do nothing
			}
			else if( message == WinApi.WM_VSCROLL )
			{
				HandleVScrollEvent( wParam.ToInt32() & 0xffff );
			}
			else if( message == WinApi.WM_HSCROLL )
			{
				HandleHScrollEvent( wParam.ToInt32() & 0xffff );
			}
			else if( message == WinApi.WM_MOUSEWHEEL )
			{
				// [*] on Vista x64, wParam value SHOULD be signed 64 bit like next:
				// 0xFFFFFFFFFF880000
				// but sometimes invalid value like next will be sent for same scroll action:
				// 0x00000000FE980000
				// so we should get extract 3rd and 4th byte and make it 16-bit int

				const int threashold = 120;
				int linesPerWheel;
				Int16 wheelDelta;
				int scrollCount;

				// get line count to scroll on each wheel event
				linesPerWheel = SystemInformation.MouseWheelScrollLines;

				// calculate wheel position
				wheelDelta = (Int16)( wParam.ToInt64() << 32 >> 48 ); // [*]
				_WheelPos += wheelDelta;

				// do scroll when the scroll position exceeds threashould
				scrollCount = _WheelPos / threashold;
				_WheelPos = _WheelPos % threashold;
				if( 0 != scrollCount )
				{
					HandleWheelEvent( -(linesPerWheel * scrollCount) );
				}
			}
			else if( message == WinApi.WM_IME_CHAR )
			{
				if( IsOverwriteMode == false )
					return IntPtr.Zero;
			}
			else if( message == WinApi.WM_IME_COMPOSITION )
			{
				if( IsOverwriteMode == false
					&& (lParam.ToInt32() & WinApi.GCS_RESULTSTR) != 0 )
				{
					string text;

					unsafe
					{
						IntPtr ime;
						int len;

						ime = WinApi.ImmGetContext( Handle );
						len = WinApi.ImmGetCompositionStringW( ime, WinApi.GCS_RESULTSTR, null, 0 );
						fixed( char* buf = new char[len+1] )
						{
							WinApi.ImmGetCompositionStringW( ime, WinApi.GCS_RESULTSTR, (void*)buf, (uint)len );
							buf[len] = '\0';
							text = new String( buf );
						}
						WinApi.ImmReleaseContext( Handle, ime );
					}

					_Impl.HandleTextInput( text );
				}
			}
			else if( message == WinApi.WM_IME_STARTCOMPOSITION )
			{
				// move IMM window to caret position
				WinApi.SetImeWindowFont( Handle, View.FontInfo );
			}
			else if( message == WinApi.WM_IME_REQUEST
				&& wParam.ToInt64() == (long)WinApi.IMR_RECONVERTSTRING )
			{
				int rc;

				unsafe {
					rc = HandleImeReconversion( (WinApi.RECONVERTSTRING*)lParam.ToPointer() );
				}

				return new IntPtr( rc );
			}

			return WinApi.CallWindowProc( _OriginalWndProcObj, window, message, wParam, lParam );
		}

		/// <summary>
		/// Erases background.
		/// Note that Azuki does nothing on an event of redrawing background
		/// so just ignores WM_ERASEBKGND message.
		/// </summary>
		protected override void OnPaintBackground( PaintEventArgs e )
		{
			//DO_NOT//base.OnPaintBackground( e );
		}

		void RewriteWndProc()
		{
			const int GWL_WNDPROC = -4;

			_OriginalWndProcObj = WinApi.GetWindowLong( Handle, GWL_WNDPROC );
			if( _CustomWndProcObj == null )
			{
				_CustomWndProcObj = new WinApi.WNDPROC( this.CustomWndProc );
			}
			
			WinApi.SetWindowLong( Handle, GWL_WNDPROC, _CustomWndProcObj );
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static bool IsClick( Point lastMouseUpPos, Point lastMouseDownPos )
			{
				if( Math.Abs(lastMouseUpPos.X - lastMouseDownPos.X) <= Plat.Inst.DragSize.Width
					&& Math.Abs(lastMouseUpPos.Y - lastMouseDownPos.Y) <= Plat.Inst.DragSize.Height )
				{
					return false;
				}

				return true;
			}

			public static WinFormsMouseEventArgs CreateWinFormsMouseEventArgs( IView view, MouseEventArgs e )
			{
				Point pt = new Point( e.X, e.Y );
				view.ScreenToVirtual( ref pt );
				int index = view.GetIndexFromVirPos( pt );

				int clicks = 1;
				clicks = e.Clicks;

				bool shift = WinApi.IsKeyDown( Keys.ShiftKey );
				bool ctrl = WinApi.IsKeyDown( Keys.ControlKey );
				bool alt = WinApi.IsKeyDown( Keys.Menu );
				bool special = ( WinApi.IsKeyDown(Keys.LWin) || WinApi.IsKeyDown(Keys.RWin) );

				return new WinFormsMouseEventArgs( e, index, clicks, shift, ctrl, alt, special );
			}

			public static int CalcOverwriteCaretWidth( IGraphics g, Document doc, View view, int caretIndex, bool isOverwriteMode )
			{
				int begin, end;
				char ch;

				// if it's no in overwrite mode, return default width
				if( !isOverwriteMode )
				{
					return UiImpl.DefaultCaretWidth;
				}

				// if something selected, return default width
				doc.GetSelection( out begin, out end );
				if( begin != end || doc.Length <= end )
				{
					return UiImpl.DefaultCaretWidth;
				}

				// calculate and return width
				ch = doc.GetCharAt( begin );
				if( ch != '\t' )
				{
					// this is not a tab so return width of this char
					return view.MeasureTokenEndX( g, ch.ToString(), 0 );
				}
				else
				{
					// this is a tab so calculate distance
					// from current position to next tab-stop and return it
					int lineHead = view.GetLineHeadIndexFromCharIndex( caretIndex );
					string leftPart = doc.GetText( lineHead, caretIndex );
					int currentX = view.MeasureTokenEndX( g, leftPart, 0 );
					int nextTabStopX = view.MeasureTokenEndX( g, leftPart+'\t', 0 );
					return nextTabStopX - currentX;
				}
			}
		}
		#endregion
	}
}
