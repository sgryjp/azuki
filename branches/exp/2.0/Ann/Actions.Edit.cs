using System.Windows.Forms;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// UNDO last operation.
		/// </summary>
		public static void Undo( AppLogic app )
		{
			app.MainForm.Azuki.Undo();
		}

		/// <summary>
		/// Execute again most recently UNDOed operation.
		/// </summary>
		public static void Redo( AppLogic app )
		{
			app.MainForm.Azuki.Redo();
		}

		/// <summary>
		/// Cuts currently selected text or current line if nothing selected.
		/// </summary>
		public static void Cut( AppLogic app )
		{
			app.MainForm.Azuki.Cut();
		}

		/// <summary>
		/// Copies currently selected text or current line if nothing selected.
		/// </summary>
		public static void Copy( AppLogic app )
		{
			app.MainForm.Azuki.Copy();
		}

		/// <summary>
		/// Pastes clipboard text and replace to currently selected text.
		/// </summary>
		public static void Paste( AppLogic app )
		{
			app.MainForm.Azuki.Paste();
		}

		/// <summary>
		/// Shows find dialog.
		/// </summary>
		public static void ShowFindDialog( AppLogic app )
		{
			app.MainForm.ActivateSearchPanel();
		}

		/// <summary>
		/// Finds next matching pattern.
		/// </summary>
		public static void FindNext( AppLogic app )
		{
			// set text pattern to emphasize
			app.UpdateWatchPatternForTextSearch();

			// seek to next occurrence
			app.FindNext();
		}

		/// <summary>
		/// Finds previous matching pattern.
		/// </summary>
		public static void FindPrev( AppLogic app )
		{
			// set text pattern to emphasize
			app.UpdateWatchPatternForTextSearch();

			// seek to previous occurrence
			app.FindPrev();
		}

		/// <summary>
		/// Shows "GotoLine" dialog.
		/// </summary>
		public static void ShowGotoDialog( AppLogic app )
		{
			using( var form = new GotoForm() )
			{
				var doc = app.ActiveDocument;
				form.LineNumber = doc.Lines.AtOffset( doc.CaretIndex ).LineIndex + 1;

				var result = form.ShowDialog();
				if( result == DialogResult.OK
					&& form.LineNumber < doc.Lines.Count)
				{
					int index = doc.Lines[ form.LineNumber - 1 ].Begin;
					doc.SetSelection( index, index );
					app.MainForm.Azuki.ScrollToCaret();
				}
			}
		}

		/// <summary>
		/// Removes whitespaces at end of each line.
		/// </summary>
		public static void TrimTrailingSpace( AppLogic app )
		{
			Azuki.Actions.TrimTrailingSpace( app.MainForm.Azuki );
		}

		/// <summary>
		/// Removes whitespaces at beginning of each line.
		/// </summary>
		public static void TrimLeadingSpace( AppLogic app )
		{
			Azuki.Actions.TrimLeadingSpace( app.MainForm.Azuki );
		}

		/// <summary>
		/// Convertes tab characters in selection to equivalent amount ofspaces.
		/// </summary>
		public static void ConvertTabsToSpaces( AppLogic app )
		{
			Azuki.Actions.ConvertTabsToSpaces( app.MainForm.Azuki );
		}

		/// <summary>
		/// Convertes space characters in selection to tab characters as much as possible.
		/// </summary>
		public static void ConvertSpacesToTabs( AppLogic app )
		{
			Azuki.Actions.ConvertSpacesToTabs( app.MainForm.Azuki );
		}

		/// <summary>
		/// Selects all text.
		/// </summary>
		public static void SelectAll( AppLogic app )
		{
			app.MainForm.Azuki.SelectAll();
		}

		/// <summary>
		/// Sets EOL code for input to CR+LF
		/// and unify existing EOL code to CR+LF if user choses so.
		/// </summary>
		public static void SetEolCodeToCRLF( AppLogic app )
		{
			app.SetEolCode( "\r\n" );
		}

		/// <summary>
		/// Sets EOL code for input to LF
		/// and unify existing EOL code to LF if user choses so.
		/// </summary>
		public static void SetEolCodeToLF( AppLogic app )
		{
			app.SetEolCode( "\n" );
		}

		/// <summary>
		/// Sets EOL code for input to CR and unify existing EOL code to CR if user choses so.
		/// </summary>
		public static void SetEolCodeToCR( AppLogic app )
		{
			app.SetEolCode( "\r" );
		}
	}
}
