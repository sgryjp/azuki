// file: PlatWin.cs
// brief: Platform API caller for Windows.
//=========================================================
using System;
using System.Drawing;
using System.Text;
using Sgry.Azuki.Utils;
using Control = System.Windows.Forms.Control;
using SystemInformation = System.Windows.Forms.SystemInformation;
using Marshal = System.Runtime.InteropServices.Marshal;
using Debug = Sgry.DebugUtl;

namespace Sgry.Azuki.WinForms
{
	/// <summary>
	/// Platform API for Windows.
	/// </summary>
	class PlatWin : IPlatform
	{
		#region Fields
		const string LineSelectClipFormatName = "MSDEVLineSelect";
		const string RectSelectClipFormatName = "MSDEVColumnSelect";
		readonly UInt32 _CF_LINEOBJECT = WinApi.CF_PRIVATEFIRST + 1;
		readonly UInt32 _CF_RECTSELECT = WinApi.CF_PRIVATEFIRST + 2;
		#endregion

		#region Init / Dispose
		public PlatWin()
		{
			_CF_LINEOBJECT = WinApi.RegisterClipboardFormatW( LineSelectClipFormatName );
			_CF_RECTSELECT = WinApi.RegisterClipboardFormatW( RectSelectClipFormatName );
		}
		#endregion

		#region UI Notification
		public void MessageBeep()
		{
			WinApi.MessageBeep( 0 );
		}
		#endregion

		#region Clipboard
		/// <summary>
		/// Gets content of the system clipboard.
		/// </summary>
		/// <param name="dataType">The type of the text data in the clipboard</param>
		/// <returns>Text content retrieved from the clipboard if available. Otherwise null.</returns>
		/// <remarks>
		/// <para>
		/// This method gets text from the system clipboard.
		/// If stored text data is a special format (line or rectangle,)
		/// its data type will be set to <paramref name="dataType"/> parameter.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.TextDataType">TextDataType enum</seealso>
		public string GetClipboardText( out TextDataType dataType )
		{
			var clipboardOpened = false;
			var dataHandle = IntPtr.Zero;
			var dataPtr = IntPtr.Zero;
			var formatID = UInt32.MaxValue;
			string data;

			dataType = TextDataType.Normal;

			try
			{
				// open clipboard
				var rc = WinApi.OpenClipboard( IntPtr.Zero );
				if( rc == 0 )
				{
					return null;
				}
				clipboardOpened = true;

				// distinguish type of data in the clipboard
				if( WinApi.IsClipboardFormatAvailable(_CF_LINEOBJECT) != 0 )
				{
					formatID = WinApi.CF_UNICODETEXT;
					dataType = TextDataType.Line;
				}
				else if( WinApi.IsClipboardFormatAvailable(_CF_RECTSELECT) != 0 )
				{
					formatID = WinApi.CF_UNICODETEXT;
					dataType = TextDataType.Rectangle;
				}
				else if( WinApi.IsClipboardFormatAvailable(WinApi.CF_UNICODETEXT) != 0 )
				{
					formatID = WinApi.CF_UNICODETEXT;
				}
				else if( WinApi.IsClipboardFormatAvailable(WinApi.CF_TEXT) != 0 )
				{
					formatID = WinApi.CF_TEXT;
				}
				if( formatID == UInt32.MaxValue )
				{
					return null; // no text data was in clipboard
				}

				// get handle of the clipboard data
				dataHandle = WinApi.GetClipboardData( formatID );
				if( dataHandle == IntPtr.Zero )
				{
					return null;
				}

				// get data pointer by locking the handle
				dataPtr = WinApi.GlobalLock( dataHandle );
				if( dataPtr == IntPtr.Zero )
				{
					return null;
				}

				// retrieve data
				data = (formatID == WinApi.CF_TEXT) ? MyPtrToStringAnsi( dataPtr )
													: Marshal.PtrToStringUni( dataPtr );
			}
			finally
			{
				// unlock handle
				if( dataPtr != IntPtr.Zero )
					WinApi.GlobalUnlock( dataHandle );
				if( clipboardOpened )
					WinApi.CloseClipboard();
			}

			return data;
		}

		/// <summary>
		/// Sets content of the system clipboard.
		/// </summary>
		/// <param name="text">Text data to set.</param>
		/// <param name="dataType">Type of the data to set.</param>
		/// <remarks>
		/// <para>
		/// This method set content of the system clipboard.
		/// If <paramref name="dataType"/> is TextDataType.Normal,
		/// the text data will be just a character sequence.
		/// If <paramref name="dataType"/> is TextDataType.Line or TextDataType.Rectangle,
		/// stored text data would be special format that is compatible with Microsoft Visual Studio.
		/// </para>
		/// </remarks>
		public void SetClipboardText( string text, TextDataType dataType )
		{
			bool clipboardOpened = false;

			try
			{
				// open clipboard
				var rc = WinApi.OpenClipboard( IntPtr.Zero );
				if( rc == 0 )
				{
					return;
				}
				clipboardOpened = true;

				// clear clipboard first
				WinApi.EmptyClipboard();

				// set normal text data
				var dataHdl = Marshal.StringToHGlobalUni( text );
				WinApi.SetClipboardData( WinApi.CF_UNICODETEXT, dataHdl );

				// set addional text data
				if( dataType == TextDataType.Line )
				{
					// allocate dummy text (this is needed for PocketPC)
					dataHdl = Marshal.StringToHGlobalUni( "" );
					WinApi.SetClipboardData( _CF_LINEOBJECT, dataHdl );
				}
				else if( dataType == TextDataType.Rectangle )
				{
					// allocate dummy text (this is needed for PocketPC)
					dataHdl = Marshal.StringToHGlobalUni( "" );
					WinApi.SetClipboardData( _CF_RECTSELECT, dataHdl );
				}
			}
			finally
			{
				if( clipboardOpened )
					WinApi.CloseClipboard();
			}
		}
		#endregion

		#region UI parameters
		/// <summary>
		/// It will be regarded as a drag operation by the system
		/// if mouse cursor moved beyond this rectangle.
		/// </summary>
		public Size DragSize
		{
			get{ return SystemInformation.DragSize; }
		}
		#endregion

		#region Graphic Interface
		/// <summary>
		/// Gets a graphic device context from a window.
		/// </summary>
		public IGraphics GetGraphics( object window )
		{
			var azuki = window as AzukiControl;
			if( azuki != null )
				return new GraWin( azuki.Handle, azuki.FontInfo );

			var control = window as Control;
			if( control != null )
			{
				if( control.Font == null )
					return new GraWin( control.Handle, new FontInfo() );
				else
					return new GraWin( control.Handle, new FontInfo(control.Font) );
			}

			throw new ArgumentException( "an object of unexpected type (" + window.GetType()
										 + ") was given to PlatWin.GetGraphics.", "window" );
		}
		#endregion

		#region Utilities
		static string MyPtrToStringAnsi( IntPtr dataPtr )
		{
			unsafe {
				byte* p = (byte*)dataPtr;
				int byteCount = 0;
					
				// count length
				for( int i=0; *(p + i) != 0; i++ )
				{
					byteCount++;
				}

				// copy data
				byte[] data = new byte[ byteCount ];
				for( int i=0; i<byteCount; i++ )
				{
					data[i] = *(p + i);
				}
					
				return new String( Encoding.Default.GetChars(data) );
			}
		}
		#endregion
	}

	class GraWin : IGraphics
	{
		#region Fields
		readonly IntPtr _Window = IntPtr.Zero;
		readonly IntPtr _DC = IntPtr.Zero;
		IntPtr _MemDC = IntPtr.Zero;
		Size _MemDcSize;
		Point _Offset = Point.Empty;
		IntPtr _MemBmp = IntPtr.Zero;
		IntPtr _OrgMemBmp;
		int _ForeColor;
		IntPtr _Pen = IntPtr.Zero;
		IntPtr _Brush = IntPtr.Zero;
		IntPtr _NullPen = IntPtr.Zero;
		IntPtr _Font = IntPtr.Zero;
		#endregion

		#region Init / Dispose
		public GraWin( IntPtr hWnd, FontInfo fontInfo )
		{
			_Window = hWnd;
			_DC = WinApi.GetDC( _Window );
			FontInfo = fontInfo;
		}

		public void Dispose()
		{
			WinApi.SelectObject( _MemDC, _OrgMemBmp );

			// release DC
			WinApi.ReleaseDC( _Window, _DC );
			WinApi.DeleteDC( _MemDC );

			// free objects lastly used
			SafeDeleteObject( _Pen );
			SafeDeleteObject( _Brush );
			SafeDeleteObject( _NullPen );
			SafeDeleteObject( _Font );
		}
		#endregion

		#region Off-screen Rendering
		/// <summary>
		/// Begin using off-screen buffer and cache drawing which will be done after.
		/// </summary>
		/// <param name="paintRect">painting area (used for creating off-screen buffer).</param>
		public void BeginPaint( Rectangle paintRect )
		{
			Debug.Assert( _MemDC == IntPtr.Zero, "invalid state; _MemDC must be IntPtr.Zero." );
			Debug.Assert( _MemBmp == IntPtr.Zero, "invalid state; _MemBmp must be IntPtr.Zero." );

			// create offscreen from the window DC
			_MemDC = WinApi.CreateCompatibleDC( _DC );
			_MemBmp = WinApi.CreateCompatibleBitmap( _DC, paintRect.Width, paintRect.Height );
			_Offset = paintRect.Location;
			_MemDcSize = paintRect.Size;
			_OrgMemBmp = WinApi.SelectObject( _MemDC, _MemBmp );
		}

		/// <summary>
		/// End using off-screen buffer and flush all drawing results.
		/// </summary>
		public void EndPaint()
		{
			Debug.Assert( _MemDC != IntPtr.Zero, "invalid state; EndPaint was called before"
						  + " BeginPaint." );
			const uint SRCCOPY = 0x00CC0020;

			// flush cached graphic update
			WinApi.BitBlt( _DC, _Offset.X, _Offset.Y, _MemDcSize.Width, _MemDcSize.Height,
						   _MemDC, 0, 0, SRCCOPY );
			RemoveClipRect();

			// dispose resources used in off-screen rendering
			WinApi.SelectObject( _MemDC, _OrgMemBmp );
			WinApi.DeleteDC( _MemDC );
			_MemDC = IntPtr.Zero;
			SafeDeleteObject( _MemBmp );
			_MemBmp = IntPtr.Zero;

			// reset graphic coordinate offset
			_Offset.X = _Offset.Y = 0;
		}
		#endregion

		#region Graphic Setting
		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public FontInfo FontInfo
		{
			set
			{
				if( value == null )
					throw new ArgumentNullException();
				
				// delete old font
				SafeDeleteObject( _Font );

				// create font handle from Font instance of .NET
				unsafe
				{
					WinApi.LOGFONTW logicalFont;

					WinApi.CreateLogFont( IntPtr.Zero, value, out logicalFont );
					
					// apply anti-alias method that user prefered
					if( value.Antialias == Antialias.None )
						logicalFont.quality = 3; // NONANTIALIASED_QUALITY
					else if( value.Antialias == Antialias.Gray )
						logicalFont.quality = 4; // ANTIALIASED_QUALITY
					else if( value.Antialias == Antialias.Subpixel )
						logicalFont.quality = 5; // CLEARTYPE_QUALITY
					
					_Font = WinApi.CreateFontIndirectW( &logicalFont );
				}
			}
		}

		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public Font Font
		{
			set
			{
				if( value == null )
					throw new ArgumentNullException();

				FontInfo = new FontInfo(value);
			}
		}

		/// <summary>
		/// Foreground color used by drawing APIs.
		/// </summary>
		public Color ForeColor
		{
			set
			{
				SafeDeleteObject( _Pen );
				_ForeColor = (value.R) | (value.G << 8) | (value.B << 16);
				_Pen = WinApi.CreatePen( 0, 1, _ForeColor );
			}
		}

		/// <summary>
		/// Background color used by drawing APIs.
		/// </summary>
		public Color BackColor
		{
			set
			{
				SafeDeleteObject( _Brush );
				int colorRef = (value.R) | (value.G << 8) | (value.B << 16);
				_Brush = WinApi.CreateSolidBrush( colorRef );
			}
		}

		/// <summary>
		/// Select specified rectangle as a clipping region.
		/// </summary>
		public void SetClipRect( Rectangle clipRect )
		{
			unsafe
			{
				// make RECT structure
				var r = new WinApi.RECT();
				r.left = clipRect.X - _Offset.X;
				r.top = clipRect.Y - _Offset.Y;
				r.right = r.left + clipRect.Width;
				r.bottom = r.top + clipRect.Height;

				// create rectangle region and select it as a clipping region
				var clipRegion = WinApi.CreateRectRgnIndirect( &r );
				WinApi.SelectClipRgn( DC, clipRegion );
				WinApi.DeleteObject( clipRegion ); // SelectClipRgn copies given region thus we can delete this
			}
		}

		/// <summary>
		/// Remove currently selected clipping region from the offscreen buffer.
		/// </summary>
		public void RemoveClipRect()
		{
			WinApi.SelectClipRgn( DC, IntPtr.Zero );
		}
		#endregion

		#region Text Rendering
		/// <summary>
		/// Draws a text.
		/// </summary>
		public void DrawText( string text, ref Point position, Color foreColor )
		{
			const int TRANSPARENT = 1;

			int x = position.X - _Offset.X;
			int y = position.Y - _Offset.Y;

			var newFont = _Font;
			var oldFont = WinApi.SelectObject( DC, newFont );
			var oldForeColor = WinApi.SetTextColor( DC, foreColor );

			WinApi.SetTextAlign( DC, false );
			WinApi.SetBkMode( DC, TRANSPARENT );
			WinApi.ExtTextOut( DC, x, y, 0, text );

			WinApi.SetTextColor( DC, oldForeColor );
			WinApi.SelectObject( DC, oldFont );
		}

		/// <summary>
		/// Measures graphical size of a string.
		/// </summary>
		/// <param name="text">Text to measure.</param>
		/// <returns>Size of the text.</returns>
		public Size MeasureText( string text )
		{
			int dummy;
			return MeasureText( text, Int32.MaxValue, out dummy );
		}

		/// <summary>
		/// Measures graphical size of a string.
		/// </summary>
		/// <param name="text">Text to measure.</param>
		/// <param name="clipWidth">
		/// Width of clipping area. (in pixel unit if the context is screen)
		/// </param>
		/// <param name="drawableLength">
		/// Number of characters which could be drawn within the specified clipping area.
		/// </param>
		/// <returns>Size of the text.</returns>
		public Size MeasureText( string text, int clipWidth, out int drawableLength )
		{
			var oldFont = IntPtr.Zero;

			try
			{
				int[] extents;

				oldFont = WinApi.SelectObject( DC, _Font ); // Measuring does not need to be done
															// in offscreen buffer.

				// Calculate total width of given text and distance of each character
				var size = WinApi.GetTextExtentExPoint( DC, text, text.Length, clipWidth,
														out drawableLength, out extents );

				// Shurink the width to exclude characters which is invisible or visible partially.
				if( drawableLength == 0 )
				{
					size.Width = 0;
				}
				else if( drawableLength < extents.Length )
				{
					size.Width = extents[ drawableLength - 1 ];
				}
				else
				{
					Debug.Assert( drawableLength == extents.Length,
								  "GetTextExtentExPoint returned an invalid data." );
				}

				// Ensure not to break a grapheme cluster
				if( 0 < drawableLength && TextUtil.IsUndividableIndex(text, drawableLength) )
				{
					do
					{
						drawableLength++;
					}
					while( TextUtil.IsUndividableIndex(text, drawableLength) );
				}
				return size;
			}
			finally
			{
				if( oldFont != IntPtr.Zero )
					WinApi.SelectObject( DC, oldFont );
			}
		}
		#endregion

		#region Graphic Drawing
		/// <summary>
		/// Draws a line with foreground color.
		/// Note that the point where the line extends to will also be painted.
		/// </summary>
		public void DrawLine( int fromX, int fromY, int toX, int toY )
		{
			fromX -= _Offset.X;
			fromY -= _Offset.Y;
			toX -= _Offset.X;
			toY -= _Offset.Y;

			var oldPen = WinApi.SelectObject( DC, _Pen );
			
			WinApi.MoveToEx( DC, fromX, fromY, IntPtr.Zero );
			WinApi.LineTo( DC, toX, toY );
			WinApi.SetPixel( DC, toX, toY, _ForeColor );
			
			WinApi.SelectObject( DC, oldPen );
		}

		/// <summary>
		/// Draws a rectangle with foreground color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		public void DrawRectangle( int x, int y, int width, int height )
		{
			x -= _Offset.X;
			y -= _Offset.Y;

			unsafe
			{
				var points = new WinApi.POINT[5];
				points[0] = new WinApi.POINT( x, y );
				points[1] = new WinApi.POINT( x+width, y );
				points[2] = new WinApi.POINT( x+width, y+height );
				points[3] = new WinApi.POINT( x, y+height );
				points[4] = new WinApi.POINT( x, y );

				var oldPen = WinApi.SelectObject( DC, _Pen );

				fixed( WinApi.POINT* p = points )
				{
					WinApi.Polyline( DC, p, 5 );
				}
				
				WinApi.SelectObject( DC, oldPen );
			}
		}

		/// <summary>
		/// Fills a rectangle with background color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		public void FillRectangle( int x, int y, int width, int height )
		{
			x -= _Offset.X;
			y -= _Offset.Y;

			var oldPen = WinApi.SelectObject( DC, NullPen );
			var oldBrush = WinApi.SelectObject( DC, _Brush );

			WinApi.Rectangle( DC, x, y, x+width+1, y+height+1 );
			WinApi.SelectObject( DC, oldPen );
			WinApi.SelectObject( DC, oldBrush );
		}
		#endregion

		#region Utilities
		IntPtr NullPen
		{
			get
			{
				const int PS_NULL = 5;
				if( _NullPen == IntPtr.Zero )
				{
					_NullPen = WinApi.CreatePen( PS_NULL, 0, 0 );
				}
				return _NullPen;
			}
		}

		IntPtr DC
		{
			get
			{
				if( _MemDC != IntPtr.Zero )
					return _MemDC;
				else
					return _DC;
			}
		}

		static void SafeDeleteObject( IntPtr gdiObj )
		{
			if( gdiObj != IntPtr.Zero )
				WinApi.DeleteObject( gdiObj );
		}
		#endregion
	}
}
