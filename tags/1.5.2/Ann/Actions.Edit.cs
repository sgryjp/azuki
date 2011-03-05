// 2009-11-15
using System;
using System.Windows.Forms;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// UNDO last operation.
		/// </summary>
		public static AnnAction Undo
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Undo();
		};

		/// <summary>
		/// Execute again most recently UNDOed operation.
		/// </summary>
		public static AnnAction Redo
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Redo();
		};

		/// <summary>
		/// Cuts currently selected text or current line if nothing selected.
		/// </summary>
		public static AnnAction Cut
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Cut();
		};

		/// <summary>
		/// Copies currently selected text or current line if nothing selected.
		/// </summary>
		public static AnnAction Copy
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Copy();
		};

		/// <summary>
		/// Pastes clipboard text and replace to currently selected text.
		/// </summary>
		public static AnnAction Paste
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.Paste();
		};

		/// <summary>
		/// Shows find dialog.
		/// </summary>
		public static AnnAction Find
			= delegate( AppLogic app )
		{
			app.MainForm.ActivateSearchPanel();
		};

		/// <summary>
		/// Finds next matching pattern.
		/// </summary>
		public static AnnAction FindNext
			= delegate( AppLogic app )
		{
			app.FindNext();
		};

		/// <summary>
		/// Finds previous matching pattern.
		/// </summary>
		public static AnnAction FindPrev
			= delegate( AppLogic app )
		{
			app.FindPrev();
		};

		/// <summary>
		/// Selects all text.
		/// </summary>
		public static AnnAction SelectAll
			= delegate( AppLogic app )
		{
			app.MainForm.Azuki.SelectAll();
		};

		/// <summary>
		/// Sets EOL code for input to CR+LF
		/// and unify existing EOL code to CR+LF if user choses so.
		/// </summary>
		public static AnnAction SetEolCodeToCRLF
			= delegate( AppLogic app )
		{
			app.SetEolCode( "\r\n" );
		};

		/// <summary>
		/// Sets EOL code for input to LF
		/// and unify existing EOL code to LF if user choses so.
		/// </summary>
		public static AnnAction SetEolCodeToLF
			= delegate( AppLogic app )
		{
			app.SetEolCode( "\n" );
		};

		/// <summary>
		/// Sets EOL code for input to CR
		/// and unify existing EOL code to CR if user choses so.
		/// </summary>
		public static AnnAction SetEolCodeToCR
			= delegate( AppLogic app )
		{
			app.SetEolCode( "\r" );
		};
	}
}