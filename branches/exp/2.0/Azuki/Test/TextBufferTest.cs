#if TEST
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Sgry.Azuki.Test
{
	public class TextBufferTest
	{
		public void Test()
		{
			int testNum = 0;
			Console.WriteLine( "[Test for Azuki.TextBuffer]" );

			Console.WriteLine("test {0} - Insert", ++testNum);
			TestUtl.Do( Insert );

			Console.WriteLine("test {0} - Remove", ++testNum);
			TestUtl.Do( Remove );

			Console.WriteLine("test {0} - FindNext", ++testNum);
			TestUtl.Do( Test_FindNext );

			Console.WriteLine("test {0} - FindNext (regex version)", ++testNum);
			TestUtl.Do( Test_FindNextR );

			Console.WriteLine("test {0} - Test_FindPrev", ++testNum);
			TestUtl.Do( Test_FindPrev );

			Console.WriteLine("test {0} - Test_FindPrev (regex version)", ++testNum);
			TestUtl.Do( Test_FindPrevR );

			Console.WriteLine("test {0} - TrackingRange", ++testNum);
			TestUtl.Do( Test_TrackingRange );

			Console.WriteLine("done.");
			Console.WriteLine();
		}

		void Insert()
		{
			// Single line
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_", lds );
				TestUtl.AssertEquals( 1, buf.Count );
				TestUtl.AssertEquals( "_", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 1, lds.Count );
				TestUtl.AssertEquals( "D", MakeLdsText(lds) );
			}

			// Multiple lines
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_\n_", lds );
				TestUtl.AssertEquals( 3, buf.Count );
				TestUtl.AssertEquals( "_\n_", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// \n_ --> \r\n_
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "\n_", lds );
				buf.Insert( 0, "\r", lds );
				TestUtl.AssertEquals( 3, buf.Count );
				TestUtl.AssertEquals( "\r\n_", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// _\r --> _\r\n
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_\r", lds );
				buf.Insert( 2, "\n", lds );
				TestUtl.AssertEquals( 3, buf.Count );
				TestUtl.AssertEquals( "_\r\n", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// _\r\n_ --> _\r_\n_
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_\r\n_", lds );
				buf.Insert( 2, "_", lds );
				TestUtl.AssertEquals( 5, buf.Count );
				TestUtl.AssertEquals( "_\r_\n_", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 3, lds.Count );
				TestUtl.AssertEquals( "DDD", MakeLdsText(lds) );
			}
		}

		void Remove()
		{
			// _\r\n_ --> \r\n_
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_\r\n_", lds );
				buf.Remove( 0, 1, lds );
				TestUtl.AssertEquals( 3, buf.Count );
				TestUtl.AssertEquals( "\r\n_", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// _\r\n_ --> _\n_
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_\r\n_", lds );
				buf.Remove( 1, 2, lds );
				TestUtl.AssertEquals( 3, buf.Count );
				TestUtl.AssertEquals( "_\n_", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// _\r\n_ --> _\r_
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_\r\n_", lds );
				buf.Remove( 2, 3, lds );
				TestUtl.AssertEquals( 3, buf.Count );
				TestUtl.AssertEquals( "_\r_", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// _\r\n_ --> __
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "_\r\n_", lds );
				buf.Remove( 1, 3, lds );
				TestUtl.AssertEquals( 2, buf.Count );
				TestUtl.AssertEquals( "__", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 1, lds.Count );
				TestUtl.AssertEquals( "D", MakeLdsText(lds) );
			}

			// \r_\n --> \r\n
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "\r_\n", lds );
				buf.Remove( 1, 2, lds );
				TestUtl.AssertEquals( 2, buf.Count );
				TestUtl.AssertEquals( "\r\n", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// \r_\n --> \r\n
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "\r_\n", lds );
				buf.Remove( 1, 2, lds );
				TestUtl.AssertEquals( 2, buf.Count );
				TestUtl.AssertEquals( "\r\n", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 2, lds.Count );
				TestUtl.AssertEquals( "DD", MakeLdsText(lds) );
			}

			// \r__\n --> \r_\n
			{
				var buf = new TextBuffer( 256, 256 );
				var lds = new GapBuffer<LineDirtyState>( 256 ){ 0 };

				buf.Insert( 0, "\r__\n", lds );
				buf.Remove( 1, 2, lds );
				TestUtl.AssertEquals( 3, buf.Count );
				TestUtl.AssertEquals( "\r_\n", buf.GetText(new Range(0, buf.Count)) );
				TestUtl.AssertEquals( 3, lds.Count );
				TestUtl.AssertEquals( "DDD", MakeLdsText(lds) );
			}
		}


		static void Test_FindNext()
		{
			var doc = new Document();
			doc.Replace( "aababcabcd" );

			// black box test (interface test)
			{
				// null target
				TestUtl.AssertThrows<ArgumentNullException>( delegate {
					doc.FindNext( (string)null, 0 );
				} );

				// negative index
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					doc.FindNext( "a", -1 );
				} );

				// end index at out of range
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					doc.FindNext( "a", 0, doc.Length+1, true );
				} );

				// inverted range
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					doc.FindNext( "a", 1, 0, true );
				} );

				// empty range
				TestUtl.AssertEquals( null, doc.FindNext("a", 0, 0, true) );

				// find in valid range
				TestUtl.AssertEquals( 0, doc.FindNext("a", 0, 1, true).Begin );
				TestUtl.AssertEquals( 1, doc.FindNext("ab", 0).Begin );
				TestUtl.AssertEquals( 3, doc.FindNext("abc", 0).Begin );
				TestUtl.AssertEquals( 6, doc.FindNext("abcd", 0).Begin );
				TestUtl.AssertEquals( null, doc.FindNext("abcde", 0) );

				// empty pattern (returns begin index)
				TestUtl.AssertEquals( 1, doc.FindNext("", 1).Begin );

				// comp. options
				TestUtl.AssertEquals( null, doc.FindNext("aBcD", 0, doc.Length, true) );
				TestUtl.AssertEquals(  6, doc.FindNext("aBcD", 0, doc.Length, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext("ab", 5, doc.Length, true).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext("ab", 4, doc.Length, true).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext("ba", 2, doc.Length, true).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 3, doc.FindNext("ab", 2, doc.Length, true).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 5, doc.FindNext("cab", 2, doc.Length, true).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 1, doc.FindNext("ab", 0, 4, true).Begin );

					// word at the end
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext("ba", 0, 4, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( null, doc.FindNext("abc", 0, 4, true) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 1, doc.FindNext("ab", 0, 4, true).Begin );
			}
		}

		static void Test_FindPrev()
		{
			var buf = new TextBuffer( 128, 128 );
			buf.Insert( 0, "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				TestUtl.AssertThrows<ArgumentNullException>( delegate {
					buf.FindPrev( (string)null, 0, 10, true );
				} );

				// negative index
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( "a", -1, 10, true );
				} );

				// end index at out of range
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( "a", 0, buf.Count+1, true );
				} );

				// inverted range
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					buf.FindPrev( "a", 1, 0, true );
				} );

				// empty range
				TestUtl.AssertEquals( null, buf.FindPrev("a", 0, 0, true) );

				// find in valid range
				TestUtl.AssertEquals( 9, buf.FindPrev(   "a", 0, 10, true).Begin );
				TestUtl.AssertEquals( 7, buf.FindPrev(  "ab", 0, 10, true).Begin );
				TestUtl.AssertEquals( 4, buf.FindPrev( "abc", 0, 10, true).Begin );
				TestUtl.AssertEquals( 0, buf.FindPrev("abcd", 0, 10, true).Begin );
				TestUtl.AssertEquals( null, buf.FindPrev("abcde", 0, 10, true) );

				// empty pattern (returns end index)
				TestUtl.AssertEquals( 10, buf.FindPrev("", 0, 10, true).Begin );

				// comp. options
				TestUtl.AssertEquals( null, buf.FindPrev("aBcD", 0, 10, true) );
				TestUtl.AssertEquals(  0, buf.FindPrev("aBcD", 0, 10, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( buf, 5 );
				TestUtl.AssertEquals( 7, buf.FindPrev("ab", 7, 10, true).Begin );

				// gap == begin
				{
					MoveGap( buf, 5 );
					TestUtl.AssertEquals( 7, buf.FindPrev("ab", 5, 10, true).Begin );

					// word at the begin
					MoveGap( buf, 5 );
					TestUtl.AssertEquals( 5, buf.FindPrev("bc", 5, 10, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( buf, 5 );
					TestUtl.AssertEquals( null, buf.FindPrev("abca", 5, 10, true) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( buf, 5 );
					TestUtl.AssertEquals( 3, buf.FindPrev("da", 0, 10, true).Begin );

					// word crossing the gap
					MoveGap( buf, 5 );
					TestUtl.AssertEquals( 4, buf.FindPrev("abc", 0, 10, true).Begin );

					// word after the gap
					MoveGap( buf, 5 );
					TestUtl.AssertEquals( 5, buf.FindPrev("bca", 0, 10, true).Begin );
				}

				// gap == end
				MoveGap( buf, 5 );
				TestUtl.AssertEquals( 0, buf.FindPrev("ab", 0, 5, true).Begin );

				// end <= gap
				MoveGap( buf, 5 );
				TestUtl.AssertEquals( 0, buf.FindPrev("ab", 0, 4, true).Begin );
			}
		}

		static void Test_FindNextR()
		{
			var doc = new Document();
			SearchResult result;
			doc.Replace( "aababcabcd" );

			// black box test
			{
				// null argument
				TestUtl.AssertThrows<ArgumentNullException>( delegate{
					doc.FindNext( (Regex)null, 1, 2 );
				} );

				// negative index
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( new Regex("a[^b]+"), -1, 2 );
				} );

				// inverted range
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( new Regex("a[^b]+"), 2, 1 );
				} );

				// empty range
				result = doc.FindNext( new Regex("a[^b]+"), 0, 0 );
				TestUtl.AssertEquals( null, result );

				// range exceeding text length
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate{
					doc.FindNext(new Regex("a[^b]+"), 1, 9999);
				} );

				// invalid Regex option
				TestUtl.AssertThrows<ArgumentException>( delegate{
					doc.FindNext(new Regex("a[^b]+", RegexOptions.RightToLeft), 1, 4);
				} );

				// pattern ord at begin
				result = doc.FindNext( new Regex("a[^b]+"), 0, 2 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 2, result.End );

				// pattern in the range
				result = doc.FindNext( new Regex("a[^a]+"), 0, 3 );
				TestUtl.AssertEquals( 1, result.Begin );
				TestUtl.AssertEquals( 3, result.End );

				// pattern which ends at end
				result = doc.FindNext( new Regex("[ab]+"), 0, 5 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 5, result.End );

				// pattern... well, pretty hard to describe in English for me...
				result = doc.FindNext( new Regex("[abc]+"), 0, 5 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 5, result.End );
				result = doc.FindNext( new Regex("[abc]+"), 0, 10 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 9, result.End );

				// empty pattern
				result = doc.FindNext( new Regex(""), 0, 10 );
				TestUtl.AssertEquals( 0, result.Begin );
				TestUtl.AssertEquals( 0, result.End );

				// comp. options
				result = doc.FindNext( new Regex("aBcD"), 0, doc.Length );
				TestUtl.AssertEquals( null, result );
				result = doc.FindNext( new Regex("aBcD", RegexOptions.IgnoreCase), 0, doc.Length );
				TestUtl.AssertEquals(  6, result.Begin );
				TestUtl.AssertEquals( 10, result.End);
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext(new Regex("ab"), 5, doc.Length).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 6, doc.FindNext(new Regex("ab"), 4, doc.Length).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext(new Regex("ba"), 2, doc.Length).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 3, doc.FindNext(new Regex("ab"), 2, doc.Length).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 5, doc.FindNext(new Regex("cab"), 2, doc.Length).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );

					// word at the end
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( 2, doc.FindNext(new Regex("ba"), 0, 4).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					TestUtl.AssertEquals( null, doc.FindNext(new Regex("abc"), 0, 4) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				TestUtl.AssertEquals( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );
			}
		}

		static void Test_FindPrevR()
		{
			var doc = new Document();
			doc.Replace( "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				TestUtl.AssertThrows<ArgumentNullException>( delegate {
					doc.FindPrev( (Regex)null, 0, 10 );
				} );

				// negative index
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), -1, 10 );
				} );

				// invalid regex option
				TestUtl.AssertThrows<ArgumentException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.None), 0, doc.Length );
				} );

				// end index at out of range
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), 0, doc.Length+1 );
				} );

				// inverted range
				TestUtl.AssertThrows<ArgumentOutOfRangeException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), 1, 0 );
				} );

				// empty range
				TestUtl.AssertEquals( null, doc.FindPrev(new Regex("a", RegexOptions.RightToLeft), 0, 0) );

				// find in valid range
				TestUtl.AssertEquals( 9, doc.FindPrev(new Regex(   "a", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( 7, doc.FindPrev(new Regex(  "ab", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( 4, doc.FindPrev(new Regex( "abc", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( 0, doc.FindPrev(new Regex("abcd", RegexOptions.RightToLeft), 0, 10).Begin );
				TestUtl.AssertEquals( null, doc.FindPrev(new Regex("abcde", RegexOptions.RightToLeft), 0, 10) );

				// empty pattern (returns end index)
				TestUtl.AssertEquals( 10, doc.FindPrev(new Regex("", RegexOptions.RightToLeft), 0, 10).Begin );

				// comp. options
				TestUtl.AssertEquals( null, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft), 0, 10) );
				TestUtl.AssertEquals(  0, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft|RegexOptions.IgnoreCase), 0, 10).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 7, 10).Begin );

				// gap == begin
				{
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 5, 10).Begin );

					// word at the begin
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 5, doc.FindPrev(new Regex("bc", RegexOptions.RightToLeft), 5, 10).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( null, doc.FindPrev(new Regex("abca", RegexOptions.RightToLeft), 5, 10) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 3, doc.FindPrev(new Regex("da", RegexOptions.RightToLeft), 0, 10).Begin );

					// word crossing the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 4, doc.FindPrev(new Regex("abc", RegexOptions.RightToLeft), 0, 10).Begin );

					// word after the gap
					MoveGap( doc, 5 );
					TestUtl.AssertEquals( 5, doc.FindPrev(new Regex("bca", RegexOptions.RightToLeft), 0, 10).Begin );
				}

				// gap == end
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 5).Begin );

				// end <= gap
				MoveGap( doc, 5 );
				TestUtl.AssertEquals( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 4).Begin );
			}
		}

		static void Test_TrackingRange()
		{
			// Insertion before the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Outward );
				buf.Insert( 0, "x" );
				TestUtl.AssertEquals( "[2, 3)", rangeB.ToString() );
				TestUtl.AssertEquals( "[2, 3)", rangeF.ToString() );
				TestUtl.AssertEquals( "[2, 3)", rangeI.ToString() );
				TestUtl.AssertEquals( "[2, 3)", rangeO.ToString() );
			}

			// Insertion at beginning index
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Outward );
				buf.Insert( 1, "x" );
				TestUtl.AssertEquals( "[1, 4)", rangeB.ToString() );
				TestUtl.AssertEquals( "[2, 4)", rangeF.ToString() );
				TestUtl.AssertEquals( "[2, 4)", rangeI.ToString() );
				TestUtl.AssertEquals( "[1, 4)", rangeO.ToString() );
			}

			// Insertion at ending index
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Outward );
				buf.Insert( 3, "x" );
				TestUtl.AssertEquals( "[1, 3)", rangeB.ToString() );
				TestUtl.AssertEquals( "[1, 4)", rangeF.ToString() );
				TestUtl.AssertEquals( "[1, 3)", rangeI.ToString() );
				TestUtl.AssertEquals( "[1, 4)", rangeO.ToString() );
			}

			// Insertion after the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Outward );
				buf.Insert( 3, "x" );
				TestUtl.AssertEquals( "[1, 2)", rangeB.ToString() );
				TestUtl.AssertEquals( "[1, 2)", rangeF.ToString() );
				TestUtl.AssertEquals( "[1, 2)", rangeI.ToString() );
				TestUtl.AssertEquals( "[1, 2)", rangeO.ToString() );
			}

			// Removal before the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 0, 1 );
				TestUtl.AssertEquals( "[0, 3)", rangeB.ToString() );
				TestUtl.AssertEquals( "[0, 3)", rangeF.ToString() );
				TestUtl.AssertEquals( "[0, 3)", rangeI.ToString() );
				TestUtl.AssertEquals( "[0, 3)", rangeO.ToString() );
			}

			// Removal - a range to be removed covers a tracking range's beginning
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 0, 2 );
				TestUtl.AssertEquals( "[0, 2)", rangeB.ToString() );
				TestUtl.AssertEquals( "[0, 2)", rangeF.ToString() );
				TestUtl.AssertEquals( "[0, 2)", rangeI.ToString() );
				TestUtl.AssertEquals( "[0, 2)", rangeO.ToString() );
			}

			// Removal at beginning index
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 1, 2 );
				TestUtl.AssertEquals( "[0, 3)", rangeB.ToString() );
				TestUtl.AssertEquals( "[0, 3)", rangeF.ToString() );
				TestUtl.AssertEquals( "[0, 3)", rangeI.ToString() );
				TestUtl.AssertEquals( "[0, 3)", rangeO.ToString() );
			}

			// Removal - a range to be removed ends at the same position
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 3, 4 );
				TestUtl.AssertEquals( "[1, 3)", rangeB.ToString() );
				TestUtl.AssertEquals( "[1, 3)", rangeF.ToString() );
				TestUtl.AssertEquals( "[1, 3)", rangeI.ToString() );
				TestUtl.AssertEquals( "[1, 3)", rangeO.ToString() );
			}

			// Removal after the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 4, 5 );
				TestUtl.AssertEquals( "[1, 4)", rangeB.ToString() );
				TestUtl.AssertEquals( "[1, 4)", rangeF.ToString() );
				TestUtl.AssertEquals( "[1, 4)", rangeI.ToString() );
				TestUtl.AssertEquals( "[1, 4)", rangeO.ToString() );
			}
		}

		#region Utilities
		static string MakeLdsText( GapBuffer<LineDirtyState> lds )
		{
			var buf = new StringBuilder( 32 );

			for( int i=0; i<lds.Count; i++ )
			{
				char ch = '#';

				switch( lds[i] )
				{
					case LineDirtyState.Clean:	ch = 'C';	break;
					case LineDirtyState.Dirty:	ch = 'D';	break;
					case LineDirtyState.Cleaned:ch = 'S';	break;
					default:
						TestUtl.Fail("invalid LineDirtyState enum value");
						break;
				}
				buf.Append( ch );
			}
			return buf.ToString();
		}

		static void MoveGap( Document doc, int index )
		{
			doc.InternalBuffer.Insert( index, String.Empty );
		}

		static void MoveGap( TextBuffer buf, int index )
		{
			buf.Insert( index, String.Empty );
		}
		#endregion
	}
}
#endif
