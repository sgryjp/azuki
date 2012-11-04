//#define JAVASCRIPT_HIGHLIGHTER_TRACE
using System;
using System.Text;
using System.Text.RegularExpressions;
using Spart;
using Spart.Actions;
using Spart.Parsers;
using Spart.Parsers.NonTerminal;
using Spart.Parsers.Primitives;
using Spart.Scanners;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace Sgry.Azuki.Highlighter
{
	class JavaScriptHighlighter : IHighlighter
	{
		HighlightHook _HookProc = null;
		SplitArray<int> _ReparsePoints = new SplitArray<int>( 64 );

		public JavaScriptHighlighter()
		{
			_ReparsePoints.Add( 0 );
		}

		public void Highlight(Document doc)
		{
			int x=0, y=0;
			Highlight( doc, ref x, ref y );
		}

		public void Highlight(Document doc, ref int dirtyBegin, ref int dirtyEnd)
		{
			// determine where to start highlighting
			int rpIndex = Utl.FindLeastMaximum( _ReparsePoints, dirtyBegin );
			if( 0 <= rpIndex )
			{
				dirtyBegin = _ReparsePoints[rpIndex];
			}
			else
			{
				dirtyBegin = 0;
			}

			// determine where to end highlighting
			int x = Utl.ReparsePointMinimumDistance;
			dirtyEnd += x - (dirtyEnd % x); // next multiple of x
			if( doc.Length < dirtyEnd )
			{
				dirtyEnd = doc.Length;
			}

			IScanner _Scanner = new AzukiDocumentScanner( doc, dirtyBegin, dirtyEnd );

			//--- Rules ---
			Rule program = new Rule();
			Rule mlComment = new Rule();
			Rule slComment = new Rule();
			Rule stringLiteral = new Rule();
			Rule reservedWord = new Rule();
			Rule numericLiteral = new Rule();
			Rule regexLiteral = new Rule();
			Rule otherToken = new Rule();
			Rule token = new Rule();
			Rule ws = new Rule();

			//--- Parsers ---
			Parser digit = Prims.Range('0', '9');
			Parser hexDigit = digit
							  | Prims.Range('A', 'Z')
							  | Prims.Range('a', 'z');

			reservedWord.Parser = new KeywordParser(
				"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ",
				"break", "case", "catch", "class", "const", "continue",
				"debugger", "default", "delete", "do", "else", "enum",
				"export", "extends", "false", "finally", "for", "function",
				"if", "implements", "import", "in", "instanceof", "interface",
				"let", "new", "null", "package", "private", "protected",
				"prototype", "public", "return", "static", "super", "switch",
				"this", "throw", "true", "try", "typeof", "undefined", "var",
				"void", "while", "with"
			);
			reservedWord.Act += Highlight( CharClass.Keyword );

			Parser punctuator = new KeywordParser(
				"",
				"!", "!=", "!==", "%", "%=", "&", "&&", "&=", "'", "(", ")",
				"*", "*=", "+", "++", "+=", ",", "-", "--", "-=", ".", "/",
				"/=", ":", ";", "<", "<<", "<<=", "<=", "=", "==", "===",
				">", ">=", ">>", ">>=", ">>>", ">>>=", "?", "[", "]", "^",
				"^=", "{", "|", "|=", "||", "}", "~"
			);
			punctuator.Act += Highlight( CharClass.Normal );

			numericLiteral.Parser = Ops.Choice(
				Ops.Sequence( // HexIntegerLiteral
					'0', Prims.Ch("xX"),
					Ops.OneOrMore( hexDigit )
				),
				Ops.Choice( // DecimalLiteral
					Ops.Sequence(						// 3.14e+11
						Ops.OneOrMore( digit ),
						'.',
						Ops.ZeroOrMore( digit ),
						Ops.Optional(
							Ops.Sequence(
								Prims.Ch("eE"),
								Ops.Optional( Prims.Ch("+-") ),
								Ops.OneOrMore( digit )
							)
						)
					),
					Ops.Sequence(						// .42e-12
						'.',
						Ops.OneOrMore( digit ),
						Ops.Optional(
							Ops.Sequence(
								Prims.Ch("eE"),
								Ops.Optional( Prims.Ch("+-") ),
								Ops.OneOrMore( digit )
							)
						)
					),
					Ops.OneOrMore( digit )		// 3
				)
			);
			numericLiteral.Act += Highlight( CharClass.Number );

			mlComment.Parser = Ops.Sequence(
				"/*",
				Ops.ZeroOrMore(
					Ops.Choice(
						Prims.AnyChar - '*',
						Ops.Sequence('*', Prims.AnyChar - '/')
					)
				),
				"*/"
			);
			slComment.Parser = Ops.Sequence(
				"//",
				Ops.ZeroOrMore(
					Prims.AnyChar - Prims.Eol
				),
				Prims.Eol
			);
			slComment.Act += HighlightSinglelineComment;
			mlComment.Act += Highlight( CharClass.Comment );

			stringLiteral.Parser = Ops.Choice(
				Ops.Sequence(
					'"',
					Ops.ZeroOrMore(
						Ops.Choice(
							"\\\"",
							Prims.AnyChar - '"' - Prims.Eol,
							Ops.Sequence('\\', Prims.Eol)
						)
					),
					'"'
				),
				Ops.Sequence(
					'\'',
					Ops.ZeroOrMore(
						Ops.Choice(
							Prims.AnyChar - '\'' - '\\' - Prims.Eol,
							"\\'",
							Ops.Sequence('\\', Prims.Eol)
						)
					),
					'\''
				)
			);
			stringLiteral.Act += Highlight( CharClass.String );

			regexLiteral.Parser = Ops.Sequence(
				new RegexLiteralStartingSlash(),
				Ops.OneOrMore(
					Ops.Choice(
						Ops.Sequence('\\', '/'),
						Ops.Sequence('\\', Prims.Eol),
						Prims.AnyChar - '/'
					)
				),
				'/',
				Ops.ZeroOrMore( Prims.Ch("gmi") )
			);
			regexLiteral.Act += Highlight( CharClass.Regex );

			otherToken.Parser = Ops.OneOrMore(
				Prims.AnyChar - punctuator - Prims.WhiteSpace
			);
			otherToken.Act += Highlight( CharClass.Normal );

			token.Parser = Ops.OneOrMore(
				Ops.Choice(
					slComment,
					mlComment,
					stringLiteral,
					regexLiteral,
					numericLiteral,
					reservedWord,
					punctuator,
					otherToken
				)
			);
			token.Act += delegate( object sender, ActionEventArgs e ) {
				Utl.EntryReparsePoint( _ReparsePoints, (int)e.Match.Offset );
			};

			ws.Parser = Ops.OneOrMore(
				Prims.WhiteSpace
			);
			ws.Act += Highlight( CharClass.Normal );

			program.Parser = Ops.ZeroOrMore(
				Ops.Choice(
					token, ws
				)
			);

			program.Parse( _Scanner );
		}

		public bool CanUseHook
		{
			get{ return true; }
		}

		public HighlightHook HookProc
		{
			get{ return _HookProc; }
			set{ _HookProc = value; }
		}

		ActionHandler Highlight( CharClass klass )
		{
			return delegate( object sender, ActionEventArgs e ) {
				ParserMatch m = e.Match;
				Document doc = m.Scanner.Source as Document;
				int begin = (int)m.Offset;
				int end = (int)m.Offset + m.Length;
				Utl.Highlight( doc, begin, end, klass, _HookProc );
				Trace( klass, e );
			};
		}

		void HighlightSinglelineComment( object sender, ActionEventArgs e )
		{
			ParserMatch m = e.Match;
			Document doc = m.Scanner.Source as Document;
			int begin = (int)m.Offset;
			int end = (int)m.Offset + m.Length;
			Utl.Highlight( doc, begin, end-1, CharClass.Comment, _HookProc );
			Utl.Highlight( doc, end-1, end, CharClass.Normal, _HookProc );
			Trace( m.Scanner, CharClass.Comment, begin, end-1 );
		}

		class RegexLiteralStartingSlash : Parser
		{
			protected override ParserMatch ParseMain( IScanner scanner )
			{
				if( scanner == null )
					throw new ArgumentNullException( "scanner" );

				long originalOffset = scanner.Offset;
				char ch;

				// If the character is not a slash, ignore
				if( scanner.Peek() != '/' )
				{
					return ParserMatch.CreateFailureMatch( scanner );
				}

				// Skip previous white spaces
				long offset = originalOffset;
				do
				{
					if( offset <= 0 )
					{
						// No preceding token exists then
						// this must be a regex literal
						scanner.Seek( originalOffset + 1 );
						return ParserMatch.CreateSuccessfulMatch(
								scanner, originalOffset, 1
							);
					}
					scanner.Seek( --offset );
				}
				while( Char.IsWhiteSpace(scanner.Peek()) );

				// Get previous token
				ch = scanner.Peek();
				if( 0 <= ";(){},=[".IndexOf(ch) )
				{
					scanner.Seek( originalOffset + 1 );
					return ParserMatch.CreateSuccessfulMatch(
							scanner, originalOffset, 1
						);
				}

				scanner.Seek( originalOffset );
				return ParserMatch.CreateFailureMatch( scanner );
			}
		}

		[Conditional("JAVASCRIPT_HIGHLIGHTER_TRACE")]
		void Trace( CharClass klass, ActionEventArgs e )
		{
			int begin = (int)e.Match.Offset;
			int end = (int)e.Match.Offset + e.Match.Length;
			Trace( e.Match.Scanner, klass, begin, end );
		}

		[Conditional("JAVASCRIPT_HIGHLIGHTER_TRACE")]
		void Trace( IScanner scanner, CharClass klass, int begin, int end )
		{
			string s = "";

			if( 24 < end-begin )
			{
				s += scanner.Substring(begin, 18);
				s += "...";
				s += scanner.Substring(end-3, 3);
			}
			else
			{
				s = scanner.Substring(begin, end-begin);
			}
			s = s.Replace("\r", "\\r")
				.Replace("\n", "\\n")
				.Replace("\t", "\\t");
			Console.WriteLine( "[{0,4}|{1,8}|{2}]", begin, klass, s );
		}
	}
}
