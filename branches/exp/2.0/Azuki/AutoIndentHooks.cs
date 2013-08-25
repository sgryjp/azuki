﻿// file: AutoIndentLogic.cs
// brief: Logic around auto-indentation.
// author: YAMAMOTO Suguru
//=========================================================
using System;
using System.Text;
using Point = System.Drawing.Point;

namespace Sgry.Azuki
{
	/// <summary>
	/// Hook delegate called every time a character was inserted.
	/// </summary>
	/// <param name="ui">User interface object such as AzukiControl.</param>
	/// <param name="ch">Character about to be inserted.</param>
	/// <returns>
	/// Whether this hook delegate successfully executed or not.
	/// If true, Azuki itself will input nothing.
	/// </returns>
	public delegate bool AutoIndentHook( IUserInterface ui, char ch );

	/// <summary>
	/// Static class containing hook delegates for auto-indentation.
	/// </summary>
	/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.AutoIndentHook">AzukiControl.AutoIndentHook property</seealso>
	public static class AutoIndentHooks
	{
		/// <summary>
		/// Hook delegate to execute basic auto-indentation;
		/// indent same amount of spaces as the previous line.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This member is a hook delegate to execute auto-indentation.
		/// This delegate just copies previous indentation characters
		/// on making a new line.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.AutoIndentHook">AzukiControl.AutoIndentHook property</seealso>
		public static readonly AutoIndentHook GenericHook = delegate( IUserInterface ui, char ch )
		{
			Document doc = ui.Document;
			StringBuilder str = new StringBuilder();
			int lineHead;
			int newCaretIndex;

			// do nothing if Azuki is in single line mode
			if( ui.IsSingleLineMode )
			{
				return false;
			}

			// if EOL code was detected, perform indentation
			if( TextUtil.IsEolChar(ch) )
			{
				str.Append( doc.EolCode );

				// get indent chars
				lineHead = doc.Lines.AtOffset( doc.CaretIndex ).Begin;
				for( int i=lineHead; i<doc.CaretIndex; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						str.Append( doc[i] );
					else
						break;
				}

				// replace selection
				newCaretIndex = Math.Min( doc.AnchorIndex, doc.CaretIndex ) + str.Length;
				doc.Replace( str.ToString() );
				doc.SetSelection( newCaretIndex, newCaretIndex );

				return true;
			}

			return false;
		};

		/// <summary>
		/// Hook delegate to execute auto-indentation for C styled source code.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This member is a hook delegate to execute auto-indentation for C styled source code.
		/// Here 'C style' means that curly brackets are used to enclose each logical block.
		/// </para>
		/// <para>
		/// Note that if user hits the Enter key on a line
		///	that ends with a closing curly bracket (<c> } </c>),
		///	newly generated line will be indented one more level
		///	by inserting additional indent characters.
		///	The additional indent characters will be chosen according to the value of
		///	<see cref="Sgry.Azuki.WinForms.AzukiControl.UsesTabForIndent">AzukiControl.UsesTabForIndent</see>
		/// property.
		/// </para>
		/// </remarks>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.AutoIndentHook">AzukiControl.AutoIndentHook property</seealso>
		/// <seealso cref="Sgry.Azuki.WinForms.AzukiControl.UsesTabForIndent">AzukiControl.UsesTabForIndent property</seealso>
		public static readonly AutoIndentHook CHook = delegate( IUserInterface ui, char ch )
		{
			Document doc = ui.Document;
			StringBuilder indentChars = new StringBuilder( 64 );
			int newCaretIndex;
			int selBegin, selEnd;
			
			doc.GetSelection( out selBegin, out selEnd );
			var line = doc.Lines.AtOffset( selBegin );

			// user hit Enter key?
			if( TextUtil.IsEolChar(ch) )
			{
				int i;
				bool extraPaddingNeeded = false;

				// do nothing if it's in single line mode
				if( ui.IsSingleLineMode )
				{
					return false;
				}

				indentChars.Append( doc.EolCode );

				// if the line is empty, do nothing
				if( line.IsEmpty )
				{
					return false;
				}

				// get indent chars
				for( i=line.Begin; i<selBegin; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// if there are following white spaces, remove them
				for( i=selEnd; i<line.End; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' || doc[i] == '\x3000' )
						selEnd++;
					else
						break;
				}

				// determine whether extra padding is needed or not
				// (because replacement changes line end index
				// determination after replacement will be much harder)
				if( Utl.FindPairedBracket_Backward(doc, selBegin, line.Begin, '}', '{') != -1
					&& Utl.IndexOf(doc, '}', selBegin, line.End) == -1 )
				{
					extraPaddingNeeded = true;
				}

				// replace selection
				newCaretIndex = Math.Min( doc.AnchorIndex, selBegin ) + indentChars.Length;
				doc.Replace( indentChars.ToString(), selBegin, selEnd );

				// if there is a '{' without pair before caret
				// and is no '}' after caret, add indentation
				if( extraPaddingNeeded )
				{
					// make indentation characters
					string extraPadding;
					Point pos = ui.View.GetVirtualPos( newCaretIndex );
					pos.X += ui.View.TabWidthInPx;
					extraPadding = UiImpl.GetNeededPaddingChars( ui, pos, true );
					doc.Replace( extraPadding, newCaretIndex, newCaretIndex );
					newCaretIndex += extraPadding.Length;
				}

				doc.SetSelection( newCaretIndex, newCaretIndex );

				return true;
			}
			// user hit '}'?
			else if( ch == '}' )
			{
				// ensure this line contains only white spaces
				for( int i=line.Begin; i<line.End; i++ )
				{
					if( TextUtil.IsEolChar(doc[i]) )
					{
						break;
					}
					else if( doc[i] != ' ' && doc[i] != '\t' )
					{
						return false; // this line contains a non white space char
					}
				}

				// find the paired open bracket
				var pairIndex = Utl.FindPairedBracket_Backward( doc, selBegin, 0, '}', '{' );
				if( pairIndex == -1 )
				{
					return false; // no pair exists. nothing to do
				}

				// Get indent chars from a line containing the pair
				var pairedLine = doc.Lines.AtOffset( pairIndex );
				for( int i=pairedLine.Begin; i<pairedLine.End; i++ )
				{
					if( doc[i] == ' ' || doc[i] == '\t' )
						indentChars.Append( doc[i] );
					else
						break;
				}

				// replace indent chars of current line
				indentChars.Append( '}' );
				doc.Replace( indentChars.ToString(), line.Begin, selBegin );

				return true;
			}

			return false;
		};

		#region Utilities
		static class Utl
		{
			public static int IndexOf( Document doc, char value, int startIndex, int endIndex )
			{
				for( int i=startIndex; i<endIndex; i++ )
				{
					if( doc[i] == value )
					{
						return i;
					}
				}

				return -1;
			}

			public static int LastIndexOf( Document doc, char value, int startIndex, int endIndex )
			{
				for( int i=startIndex-1; endIndex<=i; --i )
				{
					if( doc[i] == value )
						return i;
				}

				return -1;
			}

			public static int FindPairedBracket_Backward( Document doc, int startIndex, int endIndex, char bracket, char pairBracket )
			{
				int depth = 1;

				// seek backward until paired bracket was found
				for( int i=startIndex-1; endIndex<=i; i-- )
				{
					if( doc[i] == bracket && doc.IsCDATA(i) == false )
					{
						// a bracket was found.
						// increase depth count
						depth++;
					}
					else if( doc[i] == pairBracket && doc.IsCDATA(i) == false )
					{
						// a paired bracket was found.
						// decrease count and if the count fell down to zero,
						// return the position.
						depth--;
						if( depth == 0 )
						{
							return i; // found the pair
						}
					}
				}

				// not found
				return -1;
			}
		}
		#endregion
	}
}
