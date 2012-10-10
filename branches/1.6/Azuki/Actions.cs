// file: Actions.cs
// brief: Actions for Azuki engine.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2011-08-07
//=========================================================
using System;
using System.Drawing;
using System.Text;

namespace Sgry.Azuki
{
	/// <summary>
	/// Common interface of actions of Azuki engine
	/// </summary>
	public delegate void ActionProc( IUserInterface ui );

	/// <summary>
	/// A static class containing predefined actions for Azuki.
	/// </summary>
	public static partial class Actions
	{
		#region Delete
		/// <summary>
		/// Deletes one character before caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void BackSpace( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				doc.BeginUndo();
				doc.DeleteRectSelectText();
				doc.EndUndo();
				ui.View.Invalidate();
			}
			else if( doc.AnchorIndex != doc.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				int delLen = 1;
				int caret = doc.CaretIndex;

				// if the caret is at document head, there is no chars to delete
				if( caret <= 0 )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// avoid dividing a CR-LF or a surrogate pair,
				// but not combining character sequence
				if( 0 <= caret-2 )
				{
					if( doc.IsNotDividableIndex(caret-1)
						&& doc.IsCombiningCharacter(caret-1) == false )
					{
						delLen = 2;
					}
				}

				// delete char(s).
				doc.Replace( String.Empty, caret-delLen, caret );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}
		
		/// <summary>
		/// Deletes one word before caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void BackSpaceWord( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				doc.BeginUndo();
				doc.DeleteRectSelectText();
				doc.EndUndo();
				ui.View.Invalidate();
			}
			else if( doc.AnchorIndex != doc.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				
				// if the caret is at document head, there is no chars to delete
				if( doc.CaretIndex <= 0 )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// delete between previous word start position and the caret position
				int prevWordIndex = CaretMoveLogic.Calc_PrevWord( view );
				doc.Replace( String.Empty, prevWordIndex, doc.CaretIndex );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}
		
		/// <summary>
		/// Deletes one character after caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void Delete( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				doc.BeginUndo();
				doc.DeleteRectSelectText();
				doc.EndUndo();
				ui.View.Invalidate();
			}
			else if( doc.AnchorIndex != doc.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				int begin = doc.CaretIndex;
				int end = begin + 1;

				// if the caret is at document end, there is no chars to delete
				if( doc.Length <= doc.CaretIndex )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// avoid dividing a CR-LF, a surrogate pair,
				// or a combining character sequence
				while( doc.IsNotDividableIndex(end) )
				{
					end++;
				}

				// delete char(s).
				doc.Replace( String.Empty, begin, end );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}

		/// <summary>
		/// Deletes one word after caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void DeleteWord( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// switch logic according to selection state
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				doc.BeginUndo();
				doc.DeleteRectSelectText();
				doc.EndUndo();
				ui.View.Invalidate();
			}
			else if( doc.AnchorIndex != doc.CaretIndex )
			{
				//--- case of normal selection ---
				doc.Replace( String.Empty );
			}
			else
			{
				//--- case of no selection ---
				int nextWordIndex = CaretMoveLogic.Calc_NextWord( view );
				if( nextWordIndex == doc.Length && doc.CaretIndex == nextWordIndex )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// delete char(s).
				doc.Replace( String.Empty, doc.CaretIndex, nextWordIndex );
			}

			// update desired column
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}
		#endregion

		#region Clipboard
		/// <summary>
		/// Cuts current selection to clipboard.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action cuts currently selected text to clipboard.
		/// If nothing selected and invokes this action,
		/// result will be different according to
		/// <see cref="Sgry.Azuki.UserPref.CopyLineWhenNoSelection">UserPref.CopyLineWhenNoSelection</see>
		/// property value.
		/// If that property is true, current line will be cut.
		/// If that property is false, Azuki will do nothing.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.UserPref.CopyLineWhenNoSelection">UserPref.CopyLineWhenNoSelection</seealso>
		public static void Cut( IUserInterface ui )
		{
			Document doc = ui.Document;
			string text;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// is there any selection?
			text = ui.GetSelectedText();
			if( 0 < text.Length )
			{
				// there is selection.
				
				// delete selected text
				if( doc.RectSelectRanges != null )
				{
					doc.BeginUndo();
					doc.DeleteRectSelectText();
					doc.EndUndo();
					Plat.Inst.SetClipboardText( text, TextDataType.Rectangle );
				}
				else
				{
					doc.Replace( String.Empty );
					Plat.Inst.SetClipboardText( text, TextDataType.Normal );
				}
			}
			else
			{
				// no selection.
				if( !UserPref.CopyLineWhenNoSelection )
				{
					return; // nothing should be done
				}

				// if the user prefers to use cuting/copying without selection
				// to cut/copy current line, change the begin/end position to line head/end
				int lineIndex;
				lineIndex = doc.GetLineIndexFromCharIndex( doc.CaretIndex );
				text = doc.GetLineContentWithEolCode( lineIndex );
				Plat.Inst.SetClipboardText( text, TextDataType.Line );
				
				int nextLineHeadIndex;
				int lineHeadIndex = doc.GetLineHeadIndexFromCharIndex( doc.CaretIndex );
				if( lineIndex+1 < doc.LineCount )
					nextLineHeadIndex = doc.GetLineHeadIndex( lineIndex + 1 );
				else
					nextLineHeadIndex = doc.Length;
				doc.Replace( String.Empty, lineHeadIndex, nextLineHeadIndex );
			}
			
			if( ui.UsesStickyCaret == false )
			{
				ui.View.SetDesiredColumn();
			}
		}

		/// <summary>
		/// Copies current selection to clipboard.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action copies currently selected text to clipboard.
		/// If nothing selected and invokes this action,
		/// result will be different according to
		/// <see cref="Sgry.Azuki.UserPref.CopyLineWhenNoSelection">UserPref.CopyLineWhenNoSelection</see>
		/// property value.
		/// If that property is true, current line will be copied.
		/// If that property is false, Azuki will do nothing.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.UserPref.CopyLineWhenNoSelection">UserPref.CopyLineWhenNoSelection</seealso>
		public static void Copy( IUserInterface ui )
		{
			Document doc = ui.Document;
			string text;
			
			// is there any selection?
			text = ui.GetSelectedText();
			if( 0 < text.Length )
			{
				// there is selection.
				// copy selected text.
				if( doc.RectSelectRanges != null )
				{
					Plat.Inst.SetClipboardText( text, TextDataType.Rectangle );
				}
				else
				{
					Plat.Inst.SetClipboardText( text, TextDataType.Normal );
				}
			}
			else
			{
				// no selection.
				if( !UserPref.CopyLineWhenNoSelection )
				{
					return; // nothing should be done
				}

				// if the user prefers to use cuting/copying without selection
				// to cut/copy current line, change the begin/end position to line head/end
				int lineIndex;
				lineIndex = doc.GetLineIndexFromCharIndex( doc.CaretIndex );
				
				// get line content
				text = doc.GetLineContentWithEolCode( lineIndex );
				Plat.Inst.SetClipboardText( text, TextDataType.Line );
			}
		}

		/// <summary>
		/// Pastes clipboard content at where the caret is at.
		/// </summary>
		public static void Paste( IUserInterface ui )
		{
			Document doc = ui.Document;
			string clipboardText;
			int begin, end;
			int insertIndex;
			TextDataType dataType;

			// do nothing if the document is read-only
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// get clipboard content
			clipboardText = Plat.Inst.GetClipboardText( out dataType );
			if( clipboardText == null )
			{
				return;
			}

			// limit the content in a single line if it's in single line mode
			if( ui.IsSingleLineMode )
			{
				int eolIndex = clipboardText.IndexOfAny( new char[]{'\r', '\n'} );
				if( 0 <= eolIndex )
				{
					clipboardText = clipboardText.Remove( eolIndex, clipboardText.Length-eolIndex );
				}
			}
			
			// begin grouping edit action
			doc.BeginUndo();

			// delete currently selected text before insertion
			doc.GetSelection( out begin, out end );
			if( doc.RectSelectRanges != null )
			{
				//--- case of rectangle selection ---
				// delete selected text
				doc.DeleteRectSelectText();
				ui.View.Invalidate();
			}
			else if( begin != end )
			{
				//--- case of normal selection ---
				// delete selected text
				doc.Replace( "" );
			}

			// paste according type of the text data
			if( dataType == TextDataType.Rectangle )
			{
				//--- rectangle text data ---
				Point insertPos;
				int rowBegin;
				int rowEnd;
				string rowText;
				string padding;

				// insert all rows that consisting the rectangle
				insertPos = ui.View.GetVirPosFromIndex( doc.CaretIndex );
				rowBegin = 0;
				rowEnd = LineLogic.NextLineHead( clipboardText, rowBegin );
				while( 0 <= rowEnd )
				{
					// get this row content
					if( clipboardText[rowEnd-1] == '\n' )
					{
						if( clipboardText[rowEnd-2] == '\r' )
							rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin-2 );
						else
							rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin-1 );
					}
					else if( clipboardText[rowEnd-1] == '\r' )
					{
						rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin-1 );
					}
					else
					{
						rowText = clipboardText.Substring( rowBegin, rowEnd-rowBegin );
					}

					// pad tabs if needed
					padding = UiImpl.GetNeededPaddingChars( ui, insertPos, false );

					// insert this row
					insertIndex = ui.View.GetIndexFromVirPos( insertPos );
					doc.Replace( padding.ToString() + rowText, insertIndex, insertIndex );

					// goto next line
					insertPos.Y += ui.LineSpacing;
					rowBegin = rowEnd;
					rowEnd = LineLogic.NextLineHead( clipboardText, rowBegin );
				}
			}
			else
			{
				//--- normal or line text data ---
				// calculate insertion index
				insertIndex = begin;
				if( dataType == TextDataType.Line )
				{
					// make the insertion point to caret line head if it is line data type
					insertIndex = doc.GetLineHeadIndexFromCharIndex( begin );
				}

				// insert
				doc.Replace( clipboardText, insertIndex, insertIndex );
			}

			// end grouping UNDO action
			doc.EndUndo();

			// move caret
			if( ui.UsesStickyCaret == false )
			{
				ui.View.SetDesiredColumn();
			}
			ui.View.ScrollToCaret();
		}
		#endregion

		#region Misc.
		/// <summary>
		/// Undos an action.
		/// </summary>
		public static void Undo( IUserInterface ui )
		{
			IView view = ui.View;
			if( view.Document.CanUndo == false
				|| view.Document.IsReadOnly )
			{
				return;
			}

			// undo
			view.Document.Undo();
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();

			// redraw graphic of dirt-bar
			if( ui.ShowsDirtBar )
			{
				ui.View.Invalidate( ui.View.DirtBarRectangle );
			}
		}

		/// <summary>
		/// Redos an action.
		/// </summary>
		public static void Redo( IUserInterface ui )
		{
			IView view = ui.View;
			if( view.Document.CanRedo == false
				|| view.Document.IsReadOnly )
			{
				return;
			}

			// redo
			view.Document.Redo();
			if( ui.UsesStickyCaret == false )
			{
				view.SetDesiredColumn();
			}
			view.ScrollToCaret();
		}

		/// <summary>
		/// Toggles overwrite mode.
		/// </summary>
		public static void ToggleOverwriteMode( IUserInterface ui )
		{
			ui.IsOverwriteMode = !ui.IsOverwriteMode;
		}

		/// <summary>
		/// Toggles overwrite mode.
		/// </summary>
		public static void ToggleRectSelectMode( IUserInterface ui )
		{
			if( ui.SelectionMode != TextDataType.Rectangle )
				ui.SelectionMode = TextDataType.Rectangle;
			else
				ui.SelectionMode = TextDataType.Normal;
		}

		/// <summary>
		/// Refreshes view and force to redraw text area.
		/// </summary>
		public static void Refresh( IUserInterface ui )
		{
			ui.View.Invalidate();
		}

		/// <summary>
		/// Inserts a new line above the cursor.
		/// </summary>
		public static void BreakPreviousLine( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			int caretLine;
			int insIndex;

			if( doc.IsReadOnly )
				return;

			// get index of the head of current line
			caretLine = view.GetLineIndexFromCharIndex( doc.CaretIndex );

			// calculate index of the insertion point
			if( 0 < caretLine )
			{
				//-- insertion point is end of previous line --
				insIndex = view.GetLineHeadIndex( caretLine-1 )
					+ view.GetLineLength( caretLine-1 );
			}
			else
			{
				//-- insertion point is head of current line --
				insIndex = view.GetLineHeadIndex( caretLine );
			}

			// insert an EOL code
			doc.SetSelection( insIndex, insIndex );
			ui.HandleTextInput( "\n" );
			ui.ScrollToCaret();
		}

		/// <summary>
		/// Inserts a new line below the cursor.
		/// </summary>
		public static void BreakNextLine( IUserInterface ui )
		{
			Document doc = ui.Document;
			IView view = ui.View;
			string eol = doc.EolCode;
			int caretLine, caretLineHeadIndex;
			int insIndex;

			if( doc.IsReadOnly )
				return;

			// get index of the end of current line
			caretLine = view.GetLineIndexFromCharIndex( doc.CaretIndex );
			caretLineHeadIndex = view.GetLineHeadIndexFromCharIndex( doc.CaretIndex );
			insIndex = caretLineHeadIndex + view.GetLineLength( caretLine );

			// insert an EOL code
			doc.SetSelection( insIndex, insIndex );
			ui.HandleTextInput( "\n" );
			ui.ScrollToCaret();
		}

		/// <summary>
		/// Indent selected lines.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This action indents all selected lines at once.
		/// The indent characters will be tabs (U+0009) if
		/// <see cref="Sgry.Azuki.IUserInterface.UsesTabForIndent">IUserInterface.UsesTabForIndent</see>
		/// property is true, otherwise spaces (U+0020).
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.IUserInterface.UsesTabForIndent">IUserInterface.UsesTabForIndent property</seealso>
		public static void BlockIndent( IUserInterface ui )
		{
			Document doc = ui.Document;
			string indentChars;
			int begin, end;
			int beginL, endL;
			int beginLineHead, endLineHead;

			// if read-only document, do nothing
			if( doc.IsReadOnly )
			{
				return;
			}

			// get range of the selected lines
			doc.GetSelection( out begin, out end );
			beginL = doc.GetLineIndexFromCharIndex( begin );
			endL = doc.GetLineIndexFromCharIndex( end );
			if( end != doc.GetLineHeadIndex(endL) )
			{
				endL++;
			}

			// prepare indent character
			if( ui.UsesTabForIndent )
			{
				indentChars = "\t";
			}
			else
			{
				StringBuilder buf = new StringBuilder( 64 );
				for( int i=0; i<ui.TabWidth; i++ )
				{
					buf.Append( ' ' );
				}
				indentChars = buf.ToString();
			}

			// indent each lines
			doc.BeginUndo();
			for( int i=beginL; i<endL; i++ )
			{
				int lineHead = doc.GetLineHeadIndex( i );
				doc.Replace( indentChars, lineHead, lineHead );
			}
			doc.EndUndo();

			// select whole range
			beginLineHead = doc.GetLineHeadIndex( beginL );
			if( endL < doc.LineCount )
			{
				endLineHead = doc.GetLineHeadIndex( endL );
			}
			else
			{
				endLineHead = doc.Length;
			}
			doc.SetSelection( beginLineHead, endLineHead );
		}

		/// <summary>
		/// Unindent selected lines.
		/// </summary>
		public static void BlockUnIndent( IUserInterface ui )
		{
			Document doc = ui.Document;
			int begin, end;
			int beginL, endL;
			int beginLineHead, endLineHead;

			// if read-only document, do nothing
			if( doc.IsReadOnly )
			{
				return;
			}
			
			// get range of the selected lines
			doc.GetSelection( out begin, out end );
			beginL = doc.GetLineIndexFromCharIndex( begin );
			endL = doc.GetLineIndexFromCharIndex( end );
			if( end != doc.GetLineHeadIndex(endL) )
			{
				endL++;
			}

			// unindent each lines
			doc.BeginUndo();
			for( int i=beginL; i<endL; i++ )
			{
				int lineHead = doc.GetLineHeadIndex( i );
				if( doc.Length <= lineHead )
				{
					// no more chars available. exit.
					break;
				}
				else if( doc[lineHead] == '\t' )
				{
					// there is a tab. remove it
					doc.Replace( String.Empty, lineHead, lineHead+1 );
				}
				else if( doc[lineHead] == ' ' )
				{
					// there is a space.
					// remove them until the count reaches to the tab-width
					int n = 0;
					while( doc[lineHead] == ' ' && n < ui.View.TabWidth )
					{
						doc.Replace( String.Empty, lineHead, lineHead+1 );

						n++;
					}
				}
			}
			doc.EndUndo();

			// select whole range
			beginLineHead = doc.GetLineHeadIndex( beginL );
			if( endL < doc.LineCount )
			{
				endLineHead = doc.GetLineHeadIndex( endL );
			}
			else
			{
				endLineHead = doc.Length;
			}
			doc.SetSelection( beginLineHead, endLineHead );
		}
		
		/// <summary>
		/// Scrolls down one line.
		/// </summary>
		public static void ScrollDown( IUserInterface ui )
		{
			ui.View.Scroll( 1 );
		}
		
		/// <summary>
		/// Scrolls up one line.
		/// </summary>
		public static void ScrollUp( IUserInterface ui )
		{
			ui.View.Scroll( -1 );
		}

		/// <summary>
		/// Moves caret to the matched bracket.
		/// </summary>
		public static void GoToMatchedBracket( IUserInterface ui )
		{
			int caretIndex;
			int pairIndex;

			// find pair and go there
			caretIndex = ui.CaretIndex;
			pairIndex = ui.Document.FindMatchedBracket( caretIndex );
			if( pairIndex != -1 )
			{
				// found.
				ui.SetSelection( pairIndex, pairIndex );
				ui.ScrollToCaret();
				return;
			}

			// not found.
			// if the char at CaretIndex (at right of the caret) is not a bracket,
			// then we try again for the char at CaretIndex-1 (at left of the caret.)
			if( 1 <= caretIndex )
			{
				char ch = ui.Document[ caretIndex-1 ];
				if( ch != '(' && ch != ')'
					|| ch != '{' && ch != '}'
					|| ch != '[' && ch != ']' )
				{
					pairIndex = ui.Document.FindMatchedBracket( caretIndex-1 );
					if( pairIndex != -1 )
					{
						// found.
						ui.SetSelection( pairIndex, pairIndex );
						ui.ScrollToCaret();
						return;
					}
				}
			}

			// not found.
			Plat.Inst.MessageBeep();
			return;
		}
		#endregion
	}
}
