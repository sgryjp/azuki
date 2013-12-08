// file: IView.cs
// brief: Interface for view implementations.
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// Interface for view implementations.
	/// </summary>
	public interface IView : IDisposable
	{
		#region Properties
		/// <summary>
		/// Gets or sets the document displayed in this view.
		/// </summary>
		Document Document
		{
			get;
		}

		/// <summary>
		/// Gets collection of screen line ranges, which does not include EOL codes.
		/// </summary>
		ILineRangeList Lines
		{
			get;
		}

		/// <summary>
		/// Gets collection of screen line ranges, which includes EOL codes.
		/// </summary>
		ILineRangeList RawLines
		{
			get;
		}
		#endregion

		#region Drawing Options
		/// <summary>
		/// Gets or sets top margin of the view in pixel.
		/// </summary>
		int TopMargin
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets left margin of the view in pixel.
		/// </summary>
		int LeftMargin
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets type of the indicator on the horizontal ruler.
		/// </summary>
		HRulerIndicatorType HRulerIndicatorType
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets view drawing options flags.
		/// </summary>
		DrawingOption DrawingOption
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether the current line would be drawn with underline or not.
		/// </summary>
		bool HighlightsCurrentLine
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to highlight matched bracket or not.
		/// </summary>
		bool HighlightsMatchedBracket
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show line number or not.
		/// </summary>
		bool ShowLineNumber
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show horizontal ruler or not.
		/// </summary>
		bool ShowsHRuler
		{
			get; set;
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
		///		<item>DirtyState.Clean: the line is not modified yet.</item>
		///		<item>DirtyState.Dirty: the line is modified and not saved.</item>
		///		<item>DirtyState.Saved: the line is modified but saved.</item>
		/// </list>
		/// <para>
		/// Color of each line dirty state can be customized by setting
		/// ColorScheme.DirtyLineBar, ColorScheme.CleanedLineBar.
		/// </para>
		/// </remarks>
		/// <seealso cref="DirtyState"/>
		/// <seealso cref="Sgry.Azuki.Document.Lines"/>
		/// <seealso cref="ILineRange.DirtyState"/>
		bool ShowsDirtBar
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw half-width space with special graphic or not.
		/// </summary>
		bool DrawsSpace
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw full-width space with special graphic or not.
		/// </summary>
		bool DrawsFullWidthSpace
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw tab character with special graphic or not.
		/// </summary>
		bool DrawsTab
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw EOL code with special graphic or not.
		/// </summary>
		bool DrawsEolCode
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to draw EOF mark or not.
		/// </summary>
		bool DrawsEofMark
		{
			get; set;
		}

		/// <summary>
		/// Color set used for displaying text.
		/// </summary>
		ColorScheme ColorScheme
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets tab width in count of space chars.
		/// </summary>
		int TabWidth
		{
			get; set;
		}

		/// <summary>
		/// Gets width of tab character (U+0009) in pixel.
		/// </summary>
		int TabWidthInPx
		{
			get;
		}

		/// <summary>
		/// Gets width of space character (U+0020) in pixel.
		/// </summary>
		int SpaceWidthInPx
		{
			get;
		}
		#endregion

		#region Desired Column Management
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
		void SetDesiredColumn();

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
		int GetDesiredColumn();
		#endregion

		#region Virtual position and char-index
		/// <summary>
		/// Calculates specified character's position in virtual space.
		/// </summary>
		/// <returns>Position in virutal space.</returns>
		/// <exception cref="ArgumentOutOfRangeException"/>
		Point GetVirtualPos( int index );

		/// <summary>
		/// Calculates specified character's position in virtual space.
		/// </summary>
		/// <returns>Position in virutal space.</returns>
		/// <exception cref="ArgumentOutOfRangeException"/>
		Point GetVirtualPos( int lineIndex, int columnIndex );

		/// <summary>
		/// Calculates char-index of a character at specified position in virtual space.
		/// </summary>
		/// <returns>Index of a character.</returns>
		int GetCharIndex( Point virtualPos );
		#endregion

		#region Screen positions and virtual positions
		/// <summary>
		/// Converts a coordinate in virtual space to a coordinate in client area.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Azuki uses two types of coordinate system -
		/// virtual coordinate system and client coordinate system.
		/// In client coordinate system,
		/// all points are relative to origin of upper-left corner of the 'client area.'
		/// The client area is the entire area used by Azuki
		/// so coordinate of the origin of text area will change
		/// depending on whether meta information areas
		/// (such as line number area) are displayed or not.
		/// In virtual coordinate system,
		/// all points are relative to the origin of text area.
		/// Text area is the main area which displays text content
		/// and it does not contain areas for displaying meta information.
		/// </para>
		/// <para>
		/// This method converts a coordinate in client coordinate system to
		/// a coordinate in virtual coordinate system.
		/// </para>
		/// </remarks>
		/// <seealso cref="ScreenToVirtual"/>
		Point VirtualToScreen( Point pt );

		/// <summary>
		/// Converts a coordinate in client area to a coordinate in virtual space.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Azuki uses two types of coordinate system -
		/// virtual coordinate system and client coordinate system.
		/// In client coordinate system,
		/// all points are relative to origin of upper-left corner of the 'client area.'
		/// The client area is the entire area used by Azuki
		/// so coordinate of the origin of text area will change
		/// depending on whether meta information areas
		/// (such as line number area) are displayed or not.
		/// In virtual coordinate system,
		/// all points are relative to the origin of text area.
		/// Text area is the main area which displays text content
		/// and it does not contain areas for displaying meta information.
		/// </para>
		/// <para>
		/// This method converts a coordinate in virtual coordinate system to
		/// a coordinate in client coordinate system.
		/// </para>
		/// </remarks>
		/// <seealso cref="VirtualToScreen"/>
		Point ScreenToVirtual( Point pt );
		#endregion

		#region Line and column
		/// <summary>
		/// Calculates screen line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		TextPoint GetTextPosition( int charIndex );

		/// <summary>
		/// Calculates char-index from screen line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index is out of range.</exception>
		int GetCharIndex( TextPoint position );
		#endregion

		#region Misc.
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
		int[] GetRectSelectRanges( Rectangle selRect );

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
		int GetHRulerIndex( int charIndex );

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
		int GetHRulerIndex( int lineIndex, int columnIndex );
		#endregion

		#region Operations
		/// <summary>
		/// Scroll to where the caret is.
		/// </summary>
		void ScrollToCaret();

		/// <summary>
		/// Scroll vertically.
		/// </summary>
		void Scroll( int lineDelta );

		/// <summary>
		/// Scroll horizontally.
		/// </summary>
		void HScroll( int columnDelta );

		/// <summary>
		/// Requests to repaint whole area.
		/// </summary>
		void Invalidate();

		/// <summary>
		/// Requests to repaint specified area.
		/// </summary>
		void Invalidate( int x, int y, int width, int height );

		/// <summary>
		/// Requests to repaint specified area.
		/// </summary>
		/// <param name="rect">rectangle area to be repainted (in client area coordinate)</param>
		void Invalidate( Rectangle rect );

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="range">A range of text which is needed to be repainted.</param>
		void Invalidate( IRange range );

		/// <summary>
		/// Requests to repaint area covered by given text range.
		/// </summary>
		/// <param name="beginIndex">Begin text index of the area to be repainted.</param>
		/// <param name="endIndex">End text index of the area to be repainted.</param>
		void Invalidate( int beginIndex, int endIndex );

		/// <summary>
		/// Sets font size to larger one.
		/// </summary>
		void ZoomIn();

		/// <summary>
		/// Sets font size to smaller one.
		/// </summary>
		void ZoomOut();
		#endregion

		#region States
		/// <summary>
		/// Gets or sets index of the line which is displayed at top of this view.
		/// </summary>
		int FirstVisibleLine
		{
			get; set;
		}
		#endregion

		#region Appearance
		/// <summary>
		/// Gets or sets the font used for drawing text.
		/// </summary>
		FontInfo FontInfo
		{
			get; set;
		}

		/// <summary>
		/// Gets height of each lines in pixel.
		/// </summary>
		int LineHeight
		{
			get;
		}

		/// <summary>
		/// Gets or sets size of padding between lines in pixel.
		/// </summary>
		int LinePadding
		{
			get; set;
		}

		/// <summary>
		/// Gets distance between lines in pixel.
		/// </summary>
		int LineSpacing
		{
			get;
		}

		/// <summary>
		/// Gets width of the line number area in pixel.
		/// </summary>
		int LineNumAreaWidth
		{
			get;
		}

		/// <summary>
		/// Gets height of the horizontal ruler.
		/// </summary>
		int HRulerHeight
		{
			get;
		}

		/// <summary>
		/// Gets distance between lines on the horizontal ruler.
		/// </summary>
		int HRulerUnitWidth
		{
			get;
		}

		/// <summary>
		/// Gets or sets width of the virtual text area.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">A negative number was set.</exception>
		/// <remarks>
		///	<para>
		/// This property accesses the width of the *virtual* text area. Text area indicates the
		/// logical space where Azuki draws text content and is not the area which is graphically
		/// visible; visible text area is a portion of the text area.
		/// </para>
		/// <para>
		/// Since Azuki only draws text in the text area, width of it affectes how text lines were
		/// drawn. If <see cref="IUserInterface.ViewType"/> was set to
		/// <see cref="ViewType.Proportional"/>, the width will be expanded as needed to continue
		/// drawing a long logical line. If <see cref="IUserInterface.ViewType"/> was set to
		/// <see cref="ViewType.WrappedProportional"/>, each logical text lines will be wrapped at
		/// right end of the text area.
		/// </para>
		/// <para>
		/// Note that text area does not contain line-number area nor left margin.
		/// </para>
		/// </remarks>
		int TextAreaWidth
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets size of the currently visible area.
		/// This value includes the size of both line-number area and visible text area.
		/// </summary>
		Size VisibleSize
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets size of the currently visible size of the text area (line number area is not included).
		/// </summary>
		Size VisibleTextAreaSize
		{
			get;
		}

		/// <summary>
		/// Gets or sets whether to scroll beyond the last line of the document or not.
		/// </summary>
		bool ScrollsBeyondLastLine
		{
			get; set;
		}

		/// <summary>
		/// Gets location and size of the dirt bar area.
		/// </summary>
		Rectangle DirtBarRect
		{
			get;
		}

		/// <summary>
		/// Gets location and size of the line number area.
		/// </summary>
		Rectangle LineNumberAreaRect
		{
			get;
		}

		/// <summary>
		/// Gets location and size of the horizontal ruler area.
		/// </summary>
		Rectangle HRulerRect
		{
			get;
		}

		/// <summary>
		/// Gets location and size of the visible text area in screen.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This property calculates rectangle of currently visible part of the text area. Note
		/// that, in contrast to this property, <see cref="IView.TextAreaWidth"/> property
		/// calculates 'virtual' size of the text area. Since the virtual size is calculated
		/// including areas which is not visible, these two property are totally different.
		/// </para>
		/// </remarks>
		Rectangle TextAreaRect
		{
			get;
		}
		#endregion
	}
}
