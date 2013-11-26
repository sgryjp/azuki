using System;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	public class TextBufferTest
	{
		[Test]
		public void Insert()
		{
			// Single line
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_" );
				Assert.AreEqual( 1, buf.Count );
				Assert.AreEqual( "_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 1, buf.GetLineCount() );
				Assert.AreEqual( "D", MakeLdsText(buf) );
			}

			// Multiple lines
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_\n_" );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// \n_ --> \r\n_
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "\n_" );
				buf.Insert( 0, "\r" );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "\r\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r --> _\r\n
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_\r" );
				buf.Insert( 2, "\n" );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\r\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> _\r_\n_
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_\r\n_" );
				buf.Insert( 2, "_" );
				Assert.AreEqual( 5, buf.Count );
				Assert.AreEqual( "_\r_\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 3, buf.GetLineCount() );
				Assert.AreEqual( "DDD", MakeLdsText(buf) );
			}
		}

		[Test]
		public void Remove()
		{
			// _\r\n_ --> \r\n_
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 0, 1 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "\r\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> _\n_
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> _\r_
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 2, 3 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\r_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> __
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 1, 3 );
				Assert.AreEqual( 2, buf.Count );
				Assert.AreEqual( "__", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 1, buf.GetLineCount() );
				Assert.AreEqual( "D", MakeLdsText(buf) );
			}

			// \r_\n --> \r\n
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "\r_\n" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 2, buf.Count );
				Assert.AreEqual( "\r\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// \r_\n --> \r\n
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "\r_\n" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 2, buf.Count );
				Assert.AreEqual( "\r\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.GetLineCount() );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// \r__\n --> \r_\n
			{
				var buf = new Document().Buffer;

				buf.Insert( 0, "\r__\n" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "\r_\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 3, buf.GetLineCount() );
				Assert.AreEqual( "DDD", MakeLdsText(buf) );
			}
		}

		[Test]
		public void GetLineLength()
		{
			// 0 keep it\r
			// 1 \r
			// 2 as simple as possible\r\n
			// 3 \n
			// 4 but\n
			// 5 \r\n
			// 6 not simpler.
			var buf = new Document().Buffer;
			buf.Add( "keep it\r\ras simple as possible\r\n\nbut\n\r\nnot simpler." );

			Assert.AreEqual( 7, buf.GetLineRange(0, false).Length );
			Assert.AreEqual( 0, buf.GetLineRange(1, false).Length );
			Assert.AreEqual( 21, buf.GetLineRange(2, false).Length );
			Assert.AreEqual( 0, buf.GetLineRange(3, false).Length );
			Assert.AreEqual( 3, buf.GetLineRange(4, false).Length );
			Assert.AreEqual( 0, buf.GetLineRange(5, false).Length );
			Assert.AreEqual( 12, buf.GetLineRange(6, false).Length );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				buf.GetLineRange( 7, false );
			} );
		}

		[Test]
		public void FindNext()
		{
			var buf = new Document().Buffer;
			buf.Add( "aababcabcd" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate {
					buf.FindNext( (string)null, 0, buf.Count, true );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindNext( "a", -1, buf.Count, true );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindNext( "a", 0, buf.Count+1, true );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindNext( "a", 1, 0, true );
				} );

				// empty range
				Assert.AreEqual( null, buf.FindNext("a", 0, 0, true) );

				// find in valid range
				Assert.AreEqual( 0, buf.FindNext("a", 0, 1, true).Begin );
				Assert.AreEqual( 1, buf.FindNext("ab", 0, buf.Count, true).Begin );
				Assert.AreEqual( 3, buf.FindNext("abc", 0, buf.Count, true).Begin );
				Assert.AreEqual( 6, buf.FindNext("abcd", 0, buf.Count, true).Begin );
				Assert.AreEqual( null, buf.FindNext("abcde", 0, buf.Count, true) );

				// empty pattern (returns begin index)
				Assert.AreEqual( 1, buf.FindNext("", 1, buf.Count, true).Begin );

				// comp. options
				Assert.AreEqual( null, buf.FindNext("aBcD", 0, buf.Count, true) );
				Assert.AreEqual(  6, buf.FindNext("aBcD", 0, buf.Count, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( buf, 4 );
				Assert.AreEqual( 6, buf.FindNext("ab", 5, buf.Count, true).Begin );

				// gap == begin
				MoveGap( buf, 4 );
				Assert.AreEqual( 6, buf.FindNext("ab", 4, buf.Count, true).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( buf, 4 );
					Assert.AreEqual( 2, buf.FindNext("ba", 2, buf.Count, true).Begin );

					// word crossing the gap
					MoveGap( buf, 4 );
					Assert.AreEqual( 3, buf.FindNext("ab", 2, buf.Count, true).Begin );

					// word after the gap
					MoveGap( buf, 4 );
					Assert.AreEqual( 5, buf.FindNext("cab", 2, buf.Count, true).Begin );
				}

				// gap == end
				{
					MoveGap( buf, 4 );
					Assert.AreEqual( 1, buf.FindNext("ab", 0, 4, true).Begin );

					// word at the end
					MoveGap( buf, 4 );
					Assert.AreEqual( 2, buf.FindNext("ba", 0, 4, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( buf, 4 );
					Assert.AreEqual( null, buf.FindNext("abc", 0, 4, true) );
				}

				// end <= gap
				MoveGap( buf, 4 );
				Assert.AreEqual( 1, buf.FindNext("ab", 0, 4, true).Begin );
			}
		}

		[Test]
		public void FindPrev()
		{
			var buf = new Document().Buffer;
			buf.Insert( 0, "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate {
					buf.FindPrev( (string)null, 0, 10, true );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( "a", -1, 10, true );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( "a", 0, buf.Count+1, true );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( "a", 1, 0, true );
				} );

				// empty range
				Assert.AreEqual( null, buf.FindPrev("a", 0, 0, true) );

				// find in valid range
				Assert.AreEqual( 9, buf.FindPrev(   "a", 0, 10, true).Begin );
				Assert.AreEqual( 7, buf.FindPrev(  "ab", 0, 10, true).Begin );
				Assert.AreEqual( 4, buf.FindPrev( "abc", 0, 10, true).Begin );
				Assert.AreEqual( 0, buf.FindPrev("abcd", 0, 10, true).Begin );
				Assert.AreEqual( null, buf.FindPrev("abcde", 0, 10, true) );

				// empty pattern (returns end index)
				Assert.AreEqual( 10, buf.FindPrev("", 0, 10, true).Begin );

				// comp. options
				Assert.AreEqual( null, buf.FindPrev("aBcD", 0, 10, true) );
				Assert.AreEqual(  0, buf.FindPrev("aBcD", 0, 10, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( buf, 5 );
				Assert.AreEqual( 7, buf.FindPrev("ab", 7, 10, true).Begin );

				// gap == begin
				{
					MoveGap( buf, 5 );
					Assert.AreEqual( 7, buf.FindPrev("ab", 5, 10, true).Begin );

					// word at the begin
					MoveGap( buf, 5 );
					Assert.AreEqual( 5, buf.FindPrev("bc", 5, 10, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( buf, 5 );
					Assert.AreEqual( null, buf.FindPrev("abca", 5, 10, true) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( buf, 5 );
					Assert.AreEqual( 3, buf.FindPrev("da", 0, 10, true).Begin );

					// word crossing the gap
					MoveGap( buf, 5 );
					Assert.AreEqual( 4, buf.FindPrev("abc", 0, 10, true).Begin );

					// word after the gap
					MoveGap( buf, 5 );
					Assert.AreEqual( 5, buf.FindPrev("bca", 0, 10, true).Begin );
				}

				// gap == end
				MoveGap( buf, 5 );
				Assert.AreEqual( 0, buf.FindPrev("ab", 0, 5, true).Begin );

				// end <= gap
				MoveGap( buf, 5 );
				Assert.AreEqual( 0, buf.FindPrev("ab", 0, 4, true).Begin );
			}
		}

		[Test]
		public void FindNextR()
		{
			var buf = new Document().Buffer;
			IRange result;
			buf.Add( "aababcabcd" );

			// black box test
			{
				// null argument
				Assert.Throws<ArgumentNullException>( delegate{
					buf.FindNext( (Regex)null, 1, 2 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					buf.FindNext( new Regex("a[^b]+"), -1, 2 );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					buf.FindNext( new Regex("a[^b]+"), 2, 1 );
				} );

				// empty range
				result = buf.FindNext( new Regex("a[^b]+"), 0, 0 );
				Assert.AreEqual( null, result );

				// range exceeding text length
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					buf.FindNext(new Regex("a[^b]+"), 1, 9999);
				} );

				// invalid Regex option
				Assert.Throws<ArgumentException>( delegate{
					buf.FindNext(new Regex("a[^b]+", RegexOptions.RightToLeft), 1, 4);
				} );

				// pattern ord at begin
				result = buf.FindNext( new Regex("a[^b]+"), 0, 2 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 2, result.End );

				// pattern in the range
				result = buf.FindNext( new Regex("a[^a]+"), 0, 3 );
				Assert.AreEqual( 1, result.Begin );
				Assert.AreEqual( 3, result.End );

				// pattern which ends at end
				result = buf.FindNext( new Regex("[ab]+"), 0, 5 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 5, result.End );

				// pattern... well, pretty hard to describe in English for me...
				result = buf.FindNext( new Regex("[abc]+"), 0, 5 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 5, result.End );
				result = buf.FindNext( new Regex("[abc]+"), 0, 10 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 9, result.End );

				// empty pattern
				result = buf.FindNext( new Regex(""), 0, 10 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 0, result.End );

				// comp. options
				result = buf.FindNext( new Regex("aBcD"), 0, buf.Count );
				Assert.AreEqual( null, result );
				result = buf.FindNext( new Regex("aBcD", RegexOptions.IgnoreCase), 0, buf.Count );
				Assert.AreEqual(  6, result.Begin );
				Assert.AreEqual( 10, result.End);
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( buf, 4 );
				Assert.AreEqual( 6, buf.FindNext(new Regex("ab"), 5, buf.Count).Begin );

				// gap == begin
				MoveGap( buf, 4 );
				Assert.AreEqual( 6, buf.FindNext(new Regex("ab"), 4, buf.Count).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( buf, 4 );
					Assert.AreEqual( 2, buf.FindNext(new Regex("ba"), 2, buf.Count).Begin );

					// word crossing the gap
					MoveGap( buf, 4 );
					Assert.AreEqual( 3, buf.FindNext(new Regex("ab"), 2, buf.Count).Begin );

					// word after the gap
					MoveGap( buf, 4 );
					Assert.AreEqual( 5, buf.FindNext(new Regex("cab"), 2, buf.Count).Begin );
				}

				// gap == end
				{
					MoveGap( buf, 4 );
					Assert.AreEqual( 1, buf.FindNext(new Regex("ab"), 0, 4).Begin );

					// word at the end
					MoveGap( buf, 4 );
					Assert.AreEqual( 2, buf.FindNext(new Regex("ba"), 0, 4).Begin );

					// partially matched word but overruning boundary
					MoveGap( buf, 4 );
					Assert.AreEqual( null, buf.FindNext(new Regex("abc"), 0, 4) );
				}

				// end <= gap
				MoveGap( buf, 4 );
				Assert.AreEqual( 1, buf.FindNext(new Regex("ab"), 0, 4).Begin );
			}
		}

		[Test]
		public void FindPrevR()
		{
			var buf = new Document().Buffer;
			buf.Add( "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate {
					buf.FindPrev( (Regex)null, 0, 10 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( new Regex("a", RegexOptions.RightToLeft), -1, 10 );
				} );

				// invalid regex option
				Assert.Throws<ArgumentException>( delegate {
					buf.FindPrev( new Regex("a", RegexOptions.None), 0, buf.Count );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( new Regex("a", RegexOptions.RightToLeft), 0, buf.Count+1 );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( new Regex("a", RegexOptions.RightToLeft), 1, 0 );
				} );

				// empty range
				Assert.AreEqual( null, buf.FindPrev(new Regex("a", RegexOptions.RightToLeft), 0, 0) );

				// find in valid range
				Assert.AreEqual( 9, buf.FindPrev(new Regex(   "a", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 7, buf.FindPrev(new Regex(  "ab", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 4, buf.FindPrev(new Regex( "abc", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 0, buf.FindPrev(new Regex("abcd", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( null, buf.FindPrev(new Regex("abcde", RegexOptions.RightToLeft), 0, 10) );

				// empty pattern (returns end index)
				Assert.AreEqual( 10, buf.FindPrev(new Regex("", RegexOptions.RightToLeft), 0, 10).Begin );

				// comp. options
				Assert.AreEqual( null, buf.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft), 0, 10) );
				Assert.AreEqual(  0, buf.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft|RegexOptions.IgnoreCase), 0, 10).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( buf, 5 );
				Assert.AreEqual( 7, buf.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 7, 10).Begin );

				// gap == begin
				{
					MoveGap( buf, 5 );
					Assert.AreEqual( 7, buf.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 5, 10).Begin );

					// word at the begin
					MoveGap( buf, 5 );
					Assert.AreEqual( 5, buf.FindPrev(new Regex("bc", RegexOptions.RightToLeft), 5, 10).Begin );

					// partially matched word but overruning boundary
					MoveGap( buf, 5 );
					Assert.AreEqual( null, buf.FindPrev(new Regex("abca", RegexOptions.RightToLeft), 5, 10) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( buf, 5 );
					Assert.AreEqual( 3, buf.FindPrev(new Regex("da", RegexOptions.RightToLeft), 0, 10).Begin );

					// word crossing the gap
					MoveGap( buf, 5 );
					Assert.AreEqual( 4, buf.FindPrev(new Regex("abc", RegexOptions.RightToLeft), 0, 10).Begin );

					// word after the gap
					MoveGap( buf, 5 );
					Assert.AreEqual( 5, buf.FindPrev(new Regex("bca", RegexOptions.RightToLeft), 0, 10).Begin );
				}

				// gap == end
				MoveGap( buf, 5 );
				Assert.AreEqual( 0, buf.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 5).Begin );

				// end <= gap
				MoveGap( buf, 5 );
				Assert.AreEqual( 0, buf.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 4).Begin );
			}
		}

		#region Utilities
		static string MakeLdsText( TextBuffer text )
		{
			var buf = new StringBuilder( 32 );

			for( int i=0; i<text.GetLineCount(); i++ )
			{
				char ch = '#';

				switch( text.GetLineRange(i).DirtyState )
				{
					case DirtyState.Clean:	ch = 'C';	break;
					case DirtyState.Dirty:	ch = 'D';	break;
					case DirtyState.Saved:	ch = 'S';	break;
					default:
						Assert.Fail( "invalid DirtyState enum value" );
						break;
				}
				buf.Append( ch );
			}
			return buf.ToString();
		}

		static void MoveGap( Document doc, int index )
		{
			doc.Buffer.Insert( index, String.Empty );
		}

		static void MoveGap( TextBuffer buf, int index )
		{
			buf.Insert( index, String.Empty );
		}
		#endregion
	}
}
