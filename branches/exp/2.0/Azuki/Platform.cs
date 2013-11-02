// file: Platform.cs
// brief: Platform API caller.
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// The interface for invoking Platform API.
	/// </summary>
	public interface IPlatform
	{
		#region UI Notification
		/// <summary>
		/// Notify user by platform-dependent method
		/// (may be auditory or graphically.)
		/// </summary>
		void MessageBeep();
		#endregion

		#region Clipboard
		/// <summary>
		/// Gets content of the system clipboard.
		/// </summary>
		/// <param name="dataType">The type of the text data in the clipboard</param>
		/// <returns>Text content retrieved from the clipboard if available. Otherwise null.</returns>
		/// <remarks>
		/// This method gets text from the system clipboard.
		/// If stored text data is a special format (line or rectangle,)
		/// its data type will be set to <paramref name="dataType"/> parameter.
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.TextDataType">TextDataType enum</seealso>
		string GetClipboardText( out TextDataType dataType );

		/// <summary>
		/// Sets content of the system clipboard.
		/// </summary>
		/// <param name="text">Text data to set.</param>
		/// <param name="dataType">Type of the data to set.</param>
		/// <remarks>
		/// This method set content of the system clipboard.
		/// If <paramref name="dataType"/> is TextDataType.Normal,
		/// the text data will be just a character sequence.
		/// If <paramref name="dataType"/> is TextDataType.Line or TextDataType.Rectangle,
		/// stored text data would be special format that is compatible with Microsoft Visual Studio.
		/// </remarks>
		void SetClipboardText( string text, TextDataType dataType );
		#endregion

		#region UI parameters
		/// <summary>
		/// It will be regarded as a drag operation by the system
		/// if mouse cursor moved beyond this rectangle.
		/// </summary>
		Size DragSize
		{
			get;
		}
		#endregion

		#region Graphic Interface
		/// <summary>
		/// Gets a graphic device context from a window.
		/// </summary>
		IGraphics GetGraphics( object window );
		#endregion
	}

	/// <summary>
	/// Graphic drawing interface.
	/// </summary>
	public interface IGraphics : IDisposable
	{
		#region Off-screen Rendering
		/// <summary>
		/// Begin using off-screen buffer and cache drawing which will be done after.
		/// </summary>
		/// <param name="paintRect">painting area (used for creating off-screen buffer).</param>
		void BeginPaint( Rectangle paintRect );

		/// <summary>
		/// End using off-screen buffer and flush all drawing results.
		/// </summary>
		void EndPaint();
		#endregion

		#region Graphic Setting
		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		Font Font
		{
			set;
		}

		/// <summary>
		/// Font used for drawing/measuring text.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		FontInfo FontInfo
		{
			set;
		}

		/// <summary>
		/// Foreground color used by drawing APIs.
		/// </summary>
		Color ForeColor
		{
			set;
		}

		/// <summary>
		/// Background color used by drawing APIs.
		/// </summary>
		Color BackColor
		{
			set;
		}

		/// <summary>
		/// Select specified rectangle as a clipping region.
		/// </summary>
		void SetClipRect( Rectangle clipRect );

		/// <summary>
		/// Remove currently selected clipping region.
		/// </summary>
		void RemoveClipRect();
		#endregion

		#region Text Drawing
		/// <summary>
		/// Draws a string.
		/// </summary>
		void DrawText( string text, ref Point position, Color color );

		/// <summary>
		/// Measures graphical size of a string.
		/// </summary>
		/// <param name="text">Text to measure.</param>
		/// <returns>Size of the text.</returns>
		Size MeasureText( string text );

		/// <summary>
		/// Measures graphical size of a text within the specified clipping width.
		/// </summary>
		/// <param name="text">Text to measure</param>
		/// <param name="clipWidth">
		/// Width of the clipping area. (in pixel unit if the context is screen)
		/// </param>
		/// <param name="drawableLength">
		/// Number of characters which could be drawn within the specified clipping area.
		/// </param>
		/// <returns>Size of the text.</returns>
		Size MeasureText( string text, int clipWidth, out int drawableLength );
		#endregion

		#region Graphic Drawing
		/// <summary>
		/// Draws a line with foreground color.
		/// Note that the point where the line extends to will also be painted.
		/// </summary>
		void DrawLine( int fromX, int fromY, int toX, int toY );
		
		/// <summary>
		/// Draws a rectangle with foreground color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		void DrawRectangle( int x, int y, int width, int height );

		/// <summary>
		/// Fills a rectangle with background color.
		/// Note that right and bottom edge will also be painted.
		/// </summary>
		void FillRectangle( int x, int y, int width, int height );
		#endregion
	}

	/// <summary>
	/// Information about font.
	/// </summary>
	public class FontInfo
	{
		#region Properties
		/// <summary>
		/// Font face name of this font.
		/// </summary>
		public string Name
		{
			get; set;
		}

		/// <summary>
		/// Size of this font in pt (point).
		/// </summary>
		public int Size
		{
			get; set;
		}

		/// <summary>
		/// Style of this font.
		/// </summary>
		public FontStyle Style
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets type of anti-alias feature to be used for rendering text.
		/// </summary>
		public Antialias Antialias
		{
			get; set;
		}
		#endregion

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public FontInfo()
			: this( SystemFonts.DefaultFont.Name,
					(int)SystemFonts.DefaultFont.Size,
					SystemFonts.DefaultFont.Style,
					Antialias.Default )
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public FontInfo( string name, int size, FontStyle style )
			: this( name, size, style, Antialias.Default )
		{}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public FontInfo( string name, int size, FontStyle style, Antialias antialias )
		{
			if( name == null )
				throw new ArgumentNullException( "name" );
			if( size < 0 )
				throw new ArgumentOutOfRangeException( "size" );

			Name = name;
			Size = size;
			Style = style;
			Antialias = antialias;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public FontInfo( FontInfo fontInfo )
		{
			if( fontInfo == null )
				throw new ArgumentNullException( "fontInfo" );

			Name = fontInfo.Name;
			Size = fontInfo.Size;
			Style = fontInfo.Style;
			Antialias = fontInfo.Antialias;
		}

		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <exception cref="ArgumentNullException"/>
		public FontInfo( Font font )
		{
			if( font == null )
				throw new ArgumentNullException( "font" );

			Name = font.Name;
			Size = (int)font.Size;
			Style = font.Style;
			Antialias = Antialias.Default;
		}
		#endregion

		#region Utilities
		/// <summary>
		/// Gets user readable text of this font information.
		/// </summary>
		public override string ToString()
		{
			return String.Format( "\"{0}\", {1}, {2}", Name, Size, Style );
		}

		/// <summary>
		/// Creates new instance of System.Drawing.Font with same information.
		/// </summary>
		/// <exception cref="System.ArgumentException">Failed to create System.Font object.</exception>
		public Font ToFont()
		{
			try
			{
				return new Font( Name, Size, Style );
			}
			catch( ArgumentException ex )
			{
				// ArgumentException will be thrown
				// if the font specified the name does not support
				// specified font style.
				// try to find available font style for the font.
				FontStyle[] styles = new FontStyle[5];
				styles[0] = FontStyle.Regular;
				styles[1] = FontStyle.Bold;
				styles[2] = FontStyle.Italic;
				styles[3] = FontStyle.Underline;
				styles[4] = FontStyle.Strikeout;
				foreach( FontStyle s in styles )
				{
					try
					{
						return new Font( Name, Size, s );
					}
					catch
					{}
				}

				// there is nothing Azuki can do...
				throw ex;
			}
		}

		/// <summary>
		/// Creates new instance of System.Drawing.Font with same information.
		/// </summary>
		public static implicit operator Font( FontInfo other )
		{
			return other.ToFont();
		}
		#endregion
	}

	/// <summary>
	/// The singleton class of platform API caller.
	/// </summary>
	public static class Plat
	{
		static IPlatform _Plat = null;

		#region Interface
		/// <summary>
		/// The instance of platform API caller object.
		/// </summary>
		public static IPlatform Inst
		{
			get
			{
				if( _Plat == null )
				{
					if( IsWindows() )
						_Plat = new Sgry.Azuki.WinForms.PlatWin();
					else
						throw new NotSupportedException( "Not supported!" );
				}

				return _Plat;
			}
		}

		internal static bool IsWindows()
		{
			PlatformID platform = Environment.OSVersion.Platform;
			
			if( platform == PlatformID.Win32Windows
				|| platform == PlatformID.Win32NT
				|| platform == PlatformID.WinCE )
			{
				return true;
			}
			
			return false;
		}

		/*internal static bool IsMono()
		{
			Type type = Type.GetType( "Mono.Runtime" );
	        return (type != null);
		}*/
		#endregion
	}

	/// <summary>
	/// Describes information about mouse event.
	/// </summary>
	public interface IMouseEventArgs
	{
		/// <summary>
		/// Gets the index of the mouse button which invoked this event.
		/// </summary>
		int ButtonIndex { get; }

		/// <summary>
		/// Gets the index of the character at where the mouse cursor points when this event occured.
		/// </summary>
		int Index { get; }

		/// <summary>
		/// Gets the location of the mouse cursor when this event occured.
		/// </summary>
		Point Location { get; }

		/// <summary>
		/// Gets x-coordinate of the mouse cursor when this event occured.
		/// </summary>
		int X { get; }

		/// <summary>
		/// Gets y-coordinate of the mouse cursor when this event occured.
		/// </summary>
		int Y { get; }

		/// <summary>
		/// Gets whether Shift key was pressed down when this event occured.
		/// </summary>
		bool Shift { get; }

		/// <summary>
		/// Gets whether Control key was pressed down when this event occured.
		/// </summary>
		bool Control { get; }

		/// <summary>
		/// Gets whether Alt key was pressed down when this event occured.
		/// </summary>
		bool Alt { get; }

		/// <summary>
		/// Gets whether Special key (Windows key) was pressed down when this event occured.
		/// </summary>
		bool Special { get; }

		/// <summary>
		/// If set true by an event handler, Azuki does not execute built-in default action.
		/// </summary>
		bool Handled { get; set; }
	}

	/// <summary>
	/// Methods of Anti-Alias to be used for text rendering.
	/// </summary>
	/// <seealso cref="Sgry.Azuki.FontInfo.Antialias"/>
	public enum Antialias
	{
		/// <summary>Uses system default setting.</summary>
		Default,

		/// <summary>Applies no anti-alias process.</summary>
		None,

		/// <summary>Uses single color anti-alias.</summary>
		Gray,

		/// <summary>Uses sub-pixel rendering.</summary>
		Subpixel
	}
}
