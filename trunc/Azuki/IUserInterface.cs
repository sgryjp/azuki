// file: IUserInterface.cs
// brief: interface of user interface module (platform dependent)
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-09-10
//=========================================================
using System;
using System.Drawing;

namespace Sgry.Azuki
{
	/// <summary>
	/// Interface of user interface module.
	/// </summary>
	public interface IUserInterface
	{
		/// <summary>
		/// Gets a graphic interface.
		/// </summary>
		IGraphics GetIGraphics();

		#region Associated View and Document
		/// <summary>
		/// Gets or sets the document which is the current editing target.
		/// </summary>
		Document Document
		{
			get; set;
		}

		/// <summary>
		/// Gets the associated view object.
		/// </summary>
		View View
		{
			get;
		}

		/// <summary>
		/// Gets or sets type of the view.
		/// View type determine how to render text content.
		/// </summary>
		ViewType ViewType
		{
			get; set;
		}
		#endregion

		#region KeyBind
		/// <summary>
		/// Reset keybind to default.
		/// </summary>
		void ResetKeyBind();

		/// <summary>
		/// Gets an action which is already associated with given key.
		/// If no action was associate with given key, returns null.
		/// </summary>
		/// <param name="keyCode">key code</param>
		ActionProc GetKeyBind( int keyCode );

		/// <summary>
		/// Sets or removes key-bind entry.
		/// Note that giving null to action will remove the key-bind.
		/// </summary>
		/// <param name="keyCode">key code to set/remove new action</param>
		/// <param name="action">action to be associated or null in case of removing key-bind.</param>
		void SetKeyBind( int keyCode, ActionProc action );
		#endregion

		#region Appearance
		/// <summary>
		/// Updates size and position of the caret graphic.
		/// </summary>
		void UpdateCaretGraphic();

		/// <summary>
		/// Font to be used for displaying text.
		/// </summary>
		Font Font
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
		/// Whether to show line number or not.
		/// </summary>
		bool ShowLineNumber
		{
			get; set;
		}

		/// <summary>
		/// Whether to show horizontal scroll bar or not.
		/// </summary>
		bool ShowHScrollBar
		{
			get; set;
		}

		/// <summary>
		/// Whether the current line would be drawn with underline or not.
		/// </summary>
		bool HighlightCurrentLine
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show half-width space with special graphic or not.
		/// </summary>
		bool DrawsSpace
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show full-width space with special graphic or not.
		/// </summary>
		bool DrawsFullWidthSpace
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show tab character with special graphic or not.
		/// </summary>
		bool DrawsTab
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to show EOL code with special graphic or not.
		/// </summary>
		bool DrawsEolCode
		{
			get; set;
		}

		/// <summary>
		/// Height of each lines in pixel.
		/// </summary>
		int LineHeight
		{
			get;
		}

		/// <summary>
		/// Sets width of the content area (including line number area).
		/// </summary>
		int ViewWidth
		{
			set;
		}

		/// <summary>
		/// Invalidate and make 'dirty' whole area
		/// (force to be redrawn by next paint event message).
		/// </summary>
		void Invalidate();

		/// <summary>
		/// Invalidate and make 'dirty' specified area
		/// (force to be redrawn by next paint event message).
		/// </summary>
		void Invalidate( Rectangle rect );
		#endregion

		#region Editing Behavior
		/// <summary>
		/// Gets or sets whether this document is read-only or not.
		/// </summary>
		bool IsReadOnly
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether overwrite mode is enabled or not.
		/// In overwrite mode, input character will not be inserted
		/// but replace the character at where the caret is on.
		/// </summary>
		bool IsOverwriteMode
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input tab character to equivalent amount of spaces.
		/// </summary>
		bool ConvertsTabToSpaces
		{
			get; set;
		}

		/// <summary>
		/// Gets or sets whether to automatically convert
		/// an input full-width space to a space.
		/// </summary>
		bool ConvertsFullWidthSpaceToSpace
		{
			get; set;
		}
		#endregion

		#region Edit Actions
		/// <summary>
		/// Execute UNDO.
		/// </summary>
		void Undo();

		/// <summary>
		/// Whether an available undo action exists or not.
		/// </summary>
		bool CanUndo
		{
			get;
		}

		/// <summary>
		/// Clears all stacked undo actions.
		/// </summary>
		void ClearHistory();

		/// <summary>
		/// Whether the edit actions will be recorded or not.
		/// </summary>
		bool IsRecordingHistory
		{
			get; set;
		}

		/// <summary>
		/// Executes REDO.
		/// </summary>
		void Redo();

		/// <summary>
		/// Gets whether an available REDO action exists or not.
		/// </summary>
		bool CanRedo
		{
			get;
		}

		/// <summary>
		/// Executes cut action.
		/// </summary>
		void Cut();
		
		/// <summary>
		/// Executes copy action.
		/// </summary>
		void Copy();
		
		/// <summary>
		/// Executes paste action.
		/// </summary>
		void Paste();

		/// <summary>
		/// Executes delete action.
		/// </summary>
		void Delete();
		#endregion

		#region Selection
		/// <summary>
		/// Gets the index of where the caret is at (in char-index).
		/// </summary>
		int CaretIndex
		{
			get;
		}

		/// <summary>
		/// Sets selection range and update the desired column.
		/// </summary>
		/// <param name="anchor">the position where the selection begins</param>
		/// <param name="caret">the position where the caret is</param>
		void SetSelection( int anchor, int caret );

		/// <summary>
		/// Gets range of current selection.
		/// Note that this method does not return [anchor, caret) pair but [begin, end) pair.
		/// </summary>
		/// <param name="begin">index of where the selection begins.</param>
		/// <param name="end">index of where the selection ends (selection do not includes the char at this index).</param>
		void GetSelection( out int begin, out int end );

		/// <summary>
		/// Selects all text.
		/// </summary>
		void SelectAll();
		#endregion

		#region Content Access
		/// <summary>
		/// Gets or sets currently inputted text.
		/// </summary>
		string Text
		{
			get; set;
		}

		/// <summary>
		/// Gets currently inputted character's count.
		/// Note that a surrogate pair will be counted as two chars.
		/// </summary>
		int TextLength
		{
			get;
		}

		/// <summary>
		/// Gets text in the range [begin, end).
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified range was invalid.</exception>
		string GetTextInRange( int begin, int end );

		/// <summary>
		/// Gets number of lines currently inputted.
		/// </summary>
		int LineCount
		{
			get;
		}
		#endregion

		/// <summary>
		/// Gets this component is focused by user or not.
		/// </summary>
		bool Focused
		{
			get;
		}

		#region Position / Index Conversion
		/// <summary>
		/// Calculate screen location of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Invalid index was given.</exception>
		Point GetPositionFromIndex( int index );

		/// <summary>
		/// Calculate screen location of the character at specified index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Invalid index was given.</exception>
		Point GetPositionFromIndex( int lineIndex, int columnIndex );

		/// <summary>
		/// Get char-index of the char at the point specified by screen location.
		/// </summary>
		int GetIndexFromPosition( Point pt );
		#endregion

		#region Physical Line/Column Index
		/// <summary>
		/// Gets the index of the first char in the line.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		int GetLineHeadIndex( int lineIndex );

		/// <summary>
		/// Gets the index of the first char in the physical line
		/// which contains the specified char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		int GetLineHeadIndexFromCharIndex( int charIndex );

		/// <summary>
		/// Calculates physical line/column index from char-index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		void GetLineColumnIndexFromCharIndex( int charIndex, out int lineIndex, out int columnIndex );

		/// <summary>
		/// Calculates char-index from physical line/column index.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">Specified index was invalid.</exception>
		int GetCharIndexFromLineColumnIndex( int lineIndex, int columnIndex );
		#endregion

		#region Events
		/// <summary>
		/// Occurs soon after the document's caret was moved.
		/// </summary>
		event EventHandler CaretMoved;
		#endregion

		#region Scroll
		/// <summary>
		/// Scrolls a portion of the window.
		/// </summary>
		void Scroll( Rectangle rect, int vOffset, int hOffset );
		
		/// <summary>
		/// Scrolls to where the caret is.
		/// </summary>
		void ScrollToCaret();

		/// <summary>
		/// Updates scrollbar's range.
		/// </summary>
		void UpdateScrollBarRange();
		#endregion
	}
}
