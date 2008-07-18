// file: Actions.cs
// brief: Actions for Azuki engine.
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-07-05
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Common interface of actions of Azuki engine
	/// </summary>
	public delegate void ActionProc( View view );

	/// <summary>
	/// A static class containing predefined actions for Azuki.
	/// </summary>
	public static partial class Actions
	{
		#region Delete
		/// <summary>
		/// Deletes one character before caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void BackSpace( View view )
		{
			Document doc = view.Document;

			// nothing selected?
			if( doc.AnchorIndex == doc.CaretIndex )
			{
				if( doc.CaretIndex <= 0
					|| doc.IsReadOnly )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				int delLen = 1;
				int caret = doc.CaretIndex;

				// calculate how many chars should be deleted
				// if a CR-LF or a surrogate pair is the before, move 2 chars backward
				if( 0 <= caret-2 )
				{
					string prevTwoChars = doc.GetTextInRange( caret-2, caret );
					if( prevTwoChars == "\r\n"
						|| Document.IsHighSurrogate(prevTwoChars[0]) )
					{
						delLen = 2;
					}
				}

				// delete char(s).
				doc.Replace( String.Empty, caret-delLen, caret );
			}
			else
			{
				doc.Replace( String.Empty );
			}

			// update desired column
			view.SetDesiredColumn();
			view.ScrollToCaret();
		}
		
		/// <summary>
		/// Deletes one word before caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void BackSpaceWord( View view )
		{
			Document doc = view.Document;

			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// nothing selected?
			if( doc.AnchorIndex == doc.CaretIndex )
			{
				if( doc.CaretIndex <= 0 )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// delete between previous word start position and the caret position
				int prevWordIndex = WordLogic.PrevWordStartForMove( doc, doc.CaretIndex );
				doc.Replace( String.Empty, prevWordIndex, doc.CaretIndex );
			}
			else
			{
				doc.Replace( String.Empty );
			}

			// update desired column
			view.SetDesiredColumn();
			view.ScrollToCaret();
		}
		
		/// <summary>
		/// Deletes one character after caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void Delete( View view )
		{
			Document doc = view.Document;

			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// nothing selected?
			if( doc.AnchorIndex == doc.CaretIndex )
			{
				if( doc.Length < doc.CaretIndex+1 )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				int delLen = 1;
				int caret = doc.CaretIndex;

				// calculate how many chars should be deleted
				// if a CR-LF or a surrogate pair is the next, move 2 chars forward
				if( caret+2 <= doc.Length )
				{
					string nextTwoChars = doc.GetTextInRange( caret, caret+2 );
					if( nextTwoChars == "\r\n"
						|| Document.IsHighSurrogate(nextTwoChars[0]) )
					{
						delLen = 2;
					}
				}

				// delete char(s).
				doc.Replace( String.Empty, caret, caret+delLen );
			}
			else
			{
				doc.Replace( String.Empty );
			}

			// update desired column
			view.SetDesiredColumn();
			view.ScrollToCaret();
		}

		/// <summary>
		/// Deletes one word after caret if nothing was selected, otherwise delete selection.
		/// </summary>
		public static void DeleteWord( View view )
		{
			Document doc = view.Document;

			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// nothing selected?
			if( doc.AnchorIndex == doc.CaretIndex )
			{
				int nextWordIndex = WordLogic.NextWordStartForMove( doc.InternalBuffer, doc.CaretIndex );
				if( nextWordIndex == doc.Length && doc.CaretIndex == nextWordIndex )
				{
					Plat.Inst.MessageBeep();
					return;
				}

				// delete char(s).
				doc.Replace( String.Empty, doc.CaretIndex, nextWordIndex );
			}
			else
			{
				doc.Replace( String.Empty );
			}

			// update desired column
			view.SetDesiredColumn();
			view.ScrollToCaret();
		}
		#endregion

		#region Clipboard
		/// <summary>
		/// Cuts current selection to clipboard.
		/// </summary>
		public static void Cut( View view )
		{
			Document doc = view.Document;
			string text;
			int begin, end;
			
			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// is there any selection?
			doc.GetSelection( out begin, out end );
			if( begin != end )
			{
				// there is selection. cut selected text.
				text = doc.GetTextInRange( begin, end );
				Plat.Inst.SetClipboardText( text, false );
				doc.Replace( String.Empty, begin, end );
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
				lineIndex = doc.GetLineIndexFromCharIndex( begin );
				text = doc.GetLineContentWithEolCode( lineIndex );
				Plat.Inst.SetClipboardText( text, true );
				
				int nextLineHeadIndex;
				int lineHeadIndex = doc.GetLineHeadIndexFromCharIndex( begin );
				if( lineIndex+1 < doc.LineCount )
					nextLineHeadIndex = doc.GetLineHeadIndex( lineIndex + 1 );
				else
					nextLineHeadIndex = doc.Length;
				doc.Replace( String.Empty, lineHeadIndex, nextLineHeadIndex );
			}
			
			view.SetDesiredColumn();
		}

		/// <summary>
		/// Copies current selection to clipboard.
		/// </summary>
		public static void Copy( View view )
		{
			Document doc = view.Document;
			string text;
			int begin, end;
			
			// is there any selection?
			doc.GetSelection( out begin, out end );
			if( begin != end )
			{
				// there is selection. copy selected text.
				text = doc.GetTextInRange( begin, end );
				Plat.Inst.SetClipboardText( text, false );
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
				lineIndex = doc.GetLineIndexFromCharIndex( begin );
				
				// get line content
				text = doc.GetLineContentWithEolCode( lineIndex );
				Plat.Inst.SetClipboardText( text, true );
			}
		}

		/// <summary>
		/// Pastes clipboard content at where the caret is at.
		/// </summary>
		public static void Paste( View view )
		{
			Document doc = view.Document;
			string text;
			bool isLineObj;
			int begin, end;

			if( doc.IsReadOnly )
			{
				Plat.Inst.MessageBeep();
				return;
			}

			// get clipboard content
			text = Plat.Inst.GetClipboardText( out isLineObj );
			if( text == null )
			{
				return;
			}
			
			// selection exists?
			doc.GetSelection( out begin, out end );
			if( begin != end )
			{
				// there is selection. replace selection with it
				doc.Replace( text );
			}
			// if nothing was selected, insert it to where the caret is.
			else
			{
				int insPos = begin;

				// if its a line object, make the insertion point to caret line head.
				if( isLineObj )
				{
					insPos = doc.GetLineHeadIndexFromCharIndex( begin );
				}

				// insert
				doc.Replace( text, insPos, insPos );
			}

			// move caret
			view.SetDesiredColumn();
			view.ScrollToCaret();
		}
		#endregion

		#region Misc.
		/// <summary>
		/// Undos an action.
		/// </summary>
		public static void Undo( View view )
		{
			if( view.Document.CanUndo == false
				|| view.Document.IsReadOnly )
			{
				return;
			}

			// undo
			view.Document.Undo();
			view.ScrollToCaret();
		}

		/// <summary>
		/// Redos an action.
		/// </summary>
		public static void Redo( View view )
		{
			if( view.Document.CanRedo == false
				|| view.Document.IsReadOnly )
			{
				return;
			}

			// redo
			view.Document.Redo();
			view.ScrollToCaret();
		}

		/// <summary>
		/// Toggles overwrite mode.
		/// </summary>
		public static void ToggleOverwriteMode( View view )
		{
			view.IsOverwriteMode = !view.IsOverwriteMode;
		}

		/// <summary>
		/// Refreshs view and force to redraw text area.
		/// </summary>
		public static void Refresh( View view )
		{
			view.Invalidate();
		}

		/// <summary>
		/// Indent selected lines.
		/// </summary>
		public static void BlockIndent( View view )
		{
			Document doc = view.Document;
			int begin, end;
			int beginL, endL;

			// get range of the selected lines
			doc.GetSelection( out begin, out end );
			beginL = doc.GetLineIndexFromCharIndex( begin );
			endL = doc.GetLineIndexFromCharIndex( end );
			if( end != doc.GetLineHeadIndex(endL) )
			{
				endL++;
			}

			// indent each lines
			for( int i=beginL; i<endL; i++ )
			{
				int lineHead = doc.GetLineHeadIndex( i );
				doc.Replace( "\t", lineHead, lineHead );
			}

			// select whole range
			int beginLineHead = doc.GetLineHeadIndex( beginL );
			int endLineHead = doc.GetLineHeadIndex( endL );
			doc.SetSelection( beginLineHead, endLineHead );
		}

		/// <summary>
		/// Unindent selected lines.
		/// </summary>
		public static void BlockUnIndent( View view )
		{
			Document doc = view.Document;
			int begin, end;
			int beginL, endL;

			// get range of the selected lines
			doc.GetSelection( out begin, out end );
			beginL = doc.GetLineIndexFromCharIndex( begin );
			endL = doc.GetLineIndexFromCharIndex( end );
			if( end != doc.GetLineHeadIndex(endL) )
			{
				endL++;
			}

			// unindent each lines
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
					while( doc[lineHead] == ' ' && n < view.TabWidth )
					{
						doc.Replace( String.Empty, lineHead, lineHead+1 );

						n++;
					}
				}
			}

			// select whole range
			int beginLineHead = doc.GetLineHeadIndex( beginL );
			int endLineHead = doc.GetLineHeadIndex( endL );
			doc.SetSelection( beginLineHead, endLineHead );
		}
		
		/// <summary>
		/// Scrolls down one line.
		/// </summary>
		public static void ScrollDown( View view )
		{
			view.Scroll( 1 );
		}
		
		/// <summary>
		/// Scrolls up one line.
		/// </summary>
		public static void ScrollUp( View view )
		{
			view.Scroll( -1 );
		}
		#endregion
	}
}
