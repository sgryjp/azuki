﻿// file: WordLogic.cs
// brief: Word detection logic for well Japanese handling
// author: YAMAMOTO Suguru
// encoding: UTF-8
// update: 2008-10-02
//=========================================================
using System;
using System.Text;

namespace Sgry.Azuki
{
	/// <summary>
	/// Custom word detection logic for Azuki
	/// </summary>
	class WordLogic
	{
		delegate bool ClassifyCharProc( TextBuffer text, int index );

		#region Word detection logic
		/// <summary>
		/// Finds a word at specified index.
		/// </summary>
		public static void GetWordAt( TextBuffer text, int index, out int wordBegin, out int wordEnd )
		{
			int ps, pe, ns, ne;

			ps = PrevWordStart( text, index );
			pe = PrevWordEnd( text, index );
			ns = NextWordStartForMove( text, index );
			ne = NextWordEnd( text, index );

			wordBegin = Math.Max( ps, pe );
			wordEnd = Math.Min( ns, ne );
		}

		/// <summary>
		/// Finds a word's start position from specified index to text head (designed for caret movement).
		/// </summary>
		/// <remarks>
		/// Basically this is same logic as PrevWordStart
		/// except that this do not treat EOL code as a word.
		/// </remarks>
		public static int PrevWordStartForMove( Document doc, int startIndex )
		{
			int index, initIndex;
			ClassifyCharProc isSameClass;
			TextBuffer text = doc.InternalBuffer;
			
			// check start index
			initIndex = startIndex - 1;
			if( initIndex <= 0 )
			{
				return 0;
			}
			else if( text.Count <= initIndex )
			{
				initIndex = text.Count - 1;
			}

			// rewind back one character
			initIndex = PrevValidPos( text, initIndex );
			index = initIndex;
			
			// skip white spaces
			while( IsWhiteSpace(text, index) )
			{
				index--;
				if( index <= 0 )
					return 0;
			}

			// if EOL code comes, return just after them
			if( IsEolCode(text, index) )
			{
				// but if already skipped white space, do not skip EOL
				if( index != initIndex )
					return index+1;
				else
					return index;
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( text, index );
			do
			{
				index--;
				if( index < 0 )
					return 0;
			}
			while( isSameClass(text, index) );
			
			return index + 1;
		}

		/// <summary>
		/// Finds a word's start position from specified index to text end (designed for caret movement).
		/// </summary>
		/// <remarks>
		/// Basically this is same logic as NextWordStart
		/// except that this do not treat EOL code as a word.
		/// </remarks>
		public static int NextWordStartForMove( TextBuffer text, int startIndex )
		{
			int index;
			ClassifyCharProc isSameClass;
			
			// check start index
			index = startIndex;
			if( text.Count <= index )
			{
				return text.Count;
			}
			else if( index < 0 )
			{
				index = 0;
			}
			
			// if EOL code comes, return just after them
			if( IsEolCode(text, index) )
			{
				return SkipForwardOneEolCode( text, index );
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( text, index );
			do
			{
				index++;
				if( text.Count <= index )
					return text.Count;
			}
			while( isSameClass(text, index) );
			
			// skip white spaces
			while( IsWhiteSpace(text, index) )
			{
				index++;
				if( text.Count <= index )
					return text.Count;
			}

			return index;
		}

		/// <summary>
		/// Finds previous word start location.
		/// </summary>
		public static int PrevWordStart( TextBuffer text, int startIndex )
		{
			int index;
			ClassifyCharProc isSameClass;
			
			// check start index
			index = startIndex;
			if( index <= 0 )
			{
				return 0;
			}
			else if( text.Count <= index )
			{
				index = text.Count - 1;
			}
			
			// skip white spaces
			while( IsWhiteSpace(text, index) )
			{
				index--;
				if( index <= 0 )
					return 0;
			}

			// if EOL code comes, return just after them
			if( IsEolCode(text, index) )
			{
				return index+1;
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( text, index );
			do
			{
				index--;
				if( index < 0 )
					return 0;
			}
			while( isSameClass(text, index) );
			
			return index + 1;
		}
		
		/// <summary>
		/// Gets previous word end location.
		/// </summary>
		public static int PrevWordEnd( TextBuffer text, int startIndex )
		{
			int index;
			ClassifyCharProc isSameClass;
			
			// check start index
			index = startIndex;
			if( index <= 0 )
			{
				return 0;
			}
			else if( text.Count <= index )
			{
				index = text.Count - 1;
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( text, index );
			do
			{
				index--;
				if( index <= 0 )
					return 0;
			}
			while( isSameClass(text, index) );
			
			// skip EOL codes and white spaces
			while( IsEolCode(text, index) || IsWhiteSpace(text, index) )
			{
				index--;
				if( index <= 0 )
					return 0;
			}

			return index + 1;
		}

		/// <summary>
		/// Gets start location of the next word.
		/// </summary>
		public static int NextWordStart( TextBuffer text, int startIndex )
		{
			int index;
			ClassifyCharProc isSameClass;
			
			// check start index
			index = startIndex - 1;
			if( text.Count < index )
			{
				return text.Count;
			}
			else if( index < 0 )
			{
				index = 0;
			}
			
			// proceed until the char category changes
			isSameClass = ClassifyChar( text, index );
			do
			{
				index++;
				if( text.Count <= index )
					return text.Count;
			}
			while( isSameClass(text, index) );
			
			// skip EOL codes and white spaces
			while( IsEolCode(text, index) || IsWhiteSpace(text, index) )
			{
				index++;
				if( text.Count <= index )
					return text.Count;
			}
			
			return index;
		}

		/// <summary>
		/// get end location of the next word
		/// </summary>
		public static int NextWordEnd( TextBuffer text, int startIndex )
		{
			int index;
			ClassifyCharProc isSameClass;

			// check start index
			index = startIndex;
			if( text.Count <= index )
			{
				return text.Count;
			}
			else if( index < 0 )
			{
				index = 0;
			}
			
			// skip EOL codes and white spaces
			while( IsEolCode(text, index) || IsWhiteSpace(text, index) )
			{
				index++;
				if( text.Count <= index )
					return text.Count;
			}

			// proceed until the char category changes
			isSameClass = ClassifyChar( text, index );
			do
			{
				index++;
				if( text.Count <= index )
					return text.Count;
			}
			while( isSameClass(text, index) );
			
			return index;
		}

		/// <summary>
		/// Skips forward only one EOL code.
		/// </summary>
		static int SkipForwardOneEolCode( TextBuffer text, int startIndex )
		{
			int index = startIndex;
			char ch;
			
			ch = text[index];
			if( ch == 0x0d ) // CR?
			{
				index++;
				if( text.Count <= index )
					return text.Count;
				
				ch = text[index];
				if( ch == 0x0a ) // CR+LF?
				{
					index++;
					if( text.Count <= index )
						return text.Count;
				}
			}
			else if( ch == 0x0a ) // LF?
			{
				index++;
				if( text.Count <= index )
					return text.Count;
			}

			return index;
		}

		/// <summary>
		/// Skips backward only one EOL code.
		/// </summary>
		static int SkipBackwardOneEolCode( TextBuffer text, int startIndex )
		{
			int index = startIndex;
			char ch;
			
			ch = text[index];
			if( ch == 0x0d ) // CR?
			{
				index--;
				if( index <= 0 )
					return 0;
				
				ch = text[index];
				if( ch == 0x0a ) // CR+LF?
				{
					index--;
					if( index <= 0 )
						return 0;
				}
			}
			else if( ch == 0x0a ) // LF?
			{
				index--;
				if( index <= 0 )
					return 0;
			}

			return index;
		}
		#endregion

		#region Character classification
		/// <summary>
		/// Distinguishs character class and get classification delegate object for the class.
		/// </summary>
		static ClassifyCharProc ClassifyChar( TextBuffer text, int index )
		{
			if( IsAlphabet(text, index) )	return IsAlphabet;
			if( IsDigit(text, index) )		return IsDigit;
			if( IsWhiteSpace(text, index) )	return IsWhiteSpace;
			if( IsPunct(text, index) )		return IsPunct;
			if( IsEolCode(text, index) )	return IsEolCode;
			if( IsHiragana(text, index) )	return IsHiragana;
			if( IsKatakana(text, index) )	return IsKatakana;

			return IsUnknown;
		}

		static ClassifyCharProc IsAlphabet = delegate( TextBuffer text, int index )
		{
			char ch = text[index];

			// is alphabet?
			if( ch == 0x5f ) // include '_'
				return true;
			if( 0x41 <= ch && ch <= 0x5a ) // half-width alphabets (1)
				return true;
			if( 0x61 <= ch && ch <= 0x7a ) // half-width alphabets (2)
				return true;
			if( 0xff21 <= ch && ch <= 0xff3a ) // full-width alphabets (1)
				return true;
			if( 0xff41 <= ch && ch <= 0xff5a ) // full-width alphabets (2)
				return true;

			return false;
		};

		static ClassifyCharProc IsDigit = delegate( TextBuffer text, int index )
		{
			char ch = text[index];

			// is digit?
			if( ch == 0x66 || ch == 0x69 || ch == 0x6a || ch == 0x6c || ch == 0x78 )
				return true; // include some pre/postfixes: f, i, j, l, x
			if( 0x30 <= ch && ch <= 0x39 ) // half-width digits
				return true;
			if( 0xff10 <= ch && ch <= 0xff19 ) // full-width digits
				return true;
			if( ch == 0x2e ) // '.' for float literal
				return true;

			return false;
		};

		static ClassifyCharProc IsPunct = delegate( TextBuffer text, int index )
		{
			char ch = text[index];

			if( 0x21 <= ch && ch <= 0x2f )
				return true;
			if( 0x3a <= ch && ch <= 0x40 )
				return true;
			if( 0x5b <= ch && ch <= 0x60 )
				return true;
			if( 0x7b <= ch && ch <= 0x7f )
				return true;
			if( 0x3001 <= ch && ch <= 0x303f )
				return true; // CJK punctuation marks
			if( 0xff01 <= ch && ch <= 0xff0f )
				return true; // "Full width" forms (1)
			if( 0xff1a <= ch && ch <= 0xff20 )
				return true; // "Full width" forms (2)
			if( 0xff3b <= ch && ch <= 0xff40 )
				return true; // "Full width" forms (3)
			if( 0xff5b <= ch && ch <= 0xff65 )
				return true; // "Full width" forms (4)
			if( 0xffe0 <= ch && ch <= 0xffee )
				return true; // "Full width" forms (5)
			
			return false;
		};

		static ClassifyCharProc IsWhiteSpace = delegate( TextBuffer text, int index )
		{
			char ch = text[index];

			if( ch == 0x0a || ch == 0x0d ) // exclude EOL chars
				return false;
			if( 0x00 <= ch && ch <= 0x20 )
				return true;
			if( ch == 0x3000 ) // full-width space
				return true;

			return false;
		};

		static ClassifyCharProc IsEolCode = delegate( TextBuffer text, int index )
		{
			char ch = text[index];

			if( ch == 0x0a || ch == 0x0d )
				return true;
			
			return false;
		};

		static ClassifyCharProc IsHiragana = delegate( TextBuffer text, int index )
		{
			char ch = text[index];

			/*if( ch == 0x30fc ) // cho-inn
				return true;*/
			if( 0x3041 <= ch && ch <= 0x309f )
				return true;
			
			return false;
		};

		static ClassifyCharProc IsKatakana = delegate( TextBuffer text, int index )
		{
			char ch = text[index];

			if( 0x30a0 <= ch && ch <= 0x30ff )
				return true;
			
			return false;
		};

		static ClassifyCharProc IsUnknown = delegate( TextBuffer text, int index )
		{
			if( IsAlphabet(text, index) )
				return false;
			if( IsDigit(text, index) )
				return false;
			if( IsWhiteSpace(text, index) )
				return false;
			if( IsPunct(text, index) )
				return false;
			if( IsEolCode(text, index) )
				return false;
			if( IsHiragana(text, index) )
				return false;
			if( IsKatakana(text, index) )
				return false;
			
			return true;
		};
		#endregion

		static int PrevValidPos( TextBuffer text, int index )
		{
			if( index-1 <= 0 )
				return 0;

			char ch1 = text[ index-1 ];
			char ch2 = text[ index ];

			if( ch1 == '\r' && ch2 == '\n' )
			{
				return index - 1;
			}
			else if( Document.IsLowSurrogate(ch2) )
			{
				return index - 1;
			}

			return index;
		}
	}
}
