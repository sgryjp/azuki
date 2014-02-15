﻿// file: TextDecoration.cs
// brief: Text decoration classes.
//=========================================================
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents how text should be decorated graphically.
	/// </summary>
	public class TextDecoration
	{
		static TextDecoration _None = null;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		protected TextDecoration()
		{}
		#endregion

		/// <summary>
		/// Text should not be decorated.
		/// </summary>
		public static TextDecoration None
		{
			get
			{
				if( _None == null )
				{
					_None = new TextDecoration();
				}
				return _None;
			}
		}
	}

	/// <summary>
	/// Represents how text should be decorated with special background color.
	/// </summary>
	public class BgColorTextDecoration : TextDecoration
	{
		readonly Color bgColor;

		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public BgColorTextDecoration( Color backgroundColor )
		{
			bgColor = backgroundColor;
		}
		#endregion

		/// <summary>
		/// Gets the background color of decorated tokens.
		/// </summary>
		public Color BackgroundColor
		{
			get{ return bgColor; }
		}
	}

	/// <summary>
	/// Represents how text should be decorated with underline.
	/// </summary>
	public class UnderlineTextDecoration : TextDecoration
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="lineStyle">Style of the underline.</param>
		/// <param name="lineColor">
		/// 	The color used to draw the underline.
		/// 	If Color.Transparent is specified, underline will be drawn in same color
		/// 	as the text part.
		/// </param>
		public UnderlineTextDecoration( LineStyle lineStyle, Color lineColor )
		{
			LineStyle = lineStyle;
			LineColor = lineColor;
		}

		/// <summary>
		/// Gets or sets style of underline.
		/// </summary>
		public LineStyle LineStyle { get; set; }

		/// <summary>
		/// Gets or sets color of underline.
		/// </summary>
		public Color LineColor { get; set; }
	}

	/// <summary>
	/// - EXPERIMENTAL - Decorates text with a transparent rectangle.
	/// </summary>
	public class OutlineTextDecoration : TextDecoration
	{
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		/// <param name="outlineColor">The color of the outline.</param>
		public OutlineTextDecoration( Color outlineColor )
		{
			LineColor = outlineColor;
		}

		/// <summary>
		/// The color of the outline.
		/// </summary>
		public Color LineColor { get; set; }
	}

	/// <summary>
	/// Indicates style of line for text decoration.
	/// </summary>
	public enum LineStyle
	{
		/// <summary>Does not draw line.</summary>
		None,

		/// <summary>Solid line.</summary>
		Solid,

		/// <summary>Doubled line.</summary>
		Double,

		/// <summary>Dashed line.</summary>
		Dashed,

		/// <summary>Line written with many dots.</summary>
		Dotted,

		/// <summary>Waved line.</summary>
		Waved
	}

}
