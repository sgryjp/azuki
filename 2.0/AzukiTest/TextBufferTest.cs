using System;
using System.Collections.Generic;
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
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_" );
				Assert.AreEqual( 1, buf.Count );
				Assert.AreEqual( "_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 1, buf.Lines.Count );
				Assert.AreEqual( "D", MakeLdsText(buf) );
			}

			// Multiple lines
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_\n_" );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// \n_ --> \r\n_
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "\n_" );
				buf.Insert( 0, "\r" );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "\r\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r --> _\r\n
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_\r" );
				buf.Insert( 2, "\n" );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\r\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> _\r_\n_
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_\r\n_" );
				buf.Insert( 2, "_" );
				Assert.AreEqual( 5, buf.Count );
				Assert.AreEqual( "_\r_\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 3, buf.Lines.Count );
				Assert.AreEqual( "DDD", MakeLdsText(buf) );
			}
		}

		[Test]
		public void Remove()
		{
			// _\r\n_ --> \r\n_
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 0, 1 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "\r\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> _\n_
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\n_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> _\r_
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 2, 3 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "_\r_", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// _\r\n_ --> __
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "_\r\n_" );
				buf.Remove( 1, 3 );
				Assert.AreEqual( 2, buf.Count );
				Assert.AreEqual( "__", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 1, buf.Lines.Count );
				Assert.AreEqual( "D", MakeLdsText(buf) );
			}

			// \r_\n --> \r\n
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "\r_\n" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 2, buf.Count );
				Assert.AreEqual( "\r\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// \r_\n --> \r\n
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "\r_\n" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 2, buf.Count );
				Assert.AreEqual( "\r\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 2, buf.Lines.Count );
				Assert.AreEqual( "DD", MakeLdsText(buf) );
			}

			// \r__\n --> \r_\n
			{
				var buf = new TextBuffer( 256, 256 );

				buf.Insert( 0, "\r__\n" );
				buf.Remove( 1, 2 );
				Assert.AreEqual( 3, buf.Count );
				Assert.AreEqual( "\r_\n", buf.GetText(new Range(0, buf.Count)) );
				Assert.AreEqual( 3, buf.Lines.Count );
				Assert.AreEqual( "DDD", MakeLdsText(buf) );
			}
		}

		[Test]
		public void FindNext()
		{
			var doc = new Document();
			doc.Replace( "aababcabcd" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate {
					doc.FindNext( (string)null, 0 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.FindNext( "a", -1 );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.FindNext( "a", 0, doc.Length+1, true );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.FindNext( "a", 1, 0, true );
				} );

				// empty range
				Assert.AreEqual( null, doc.FindNext("a", 0, 0, true) );

				// find in valid range
				Assert.AreEqual( 0, doc.FindNext("a", 0, 1, true).Begin );
				Assert.AreEqual( 1, doc.FindNext("ab", 0).Begin );
				Assert.AreEqual( 3, doc.FindNext("abc", 0).Begin );
				Assert.AreEqual( 6, doc.FindNext("abcd", 0).Begin );
				Assert.AreEqual( null, doc.FindNext("abcde", 0) );

				// empty pattern (returns begin index)
				Assert.AreEqual( 1, doc.FindNext("", 1).Begin );

				// comp. options
				Assert.AreEqual( null, doc.FindNext("aBcD", 0, doc.Length, true) );
				Assert.AreEqual(  6, doc.FindNext("aBcD", 0, doc.Length, false).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext("ab", 5, doc.Length, true).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext("ab", 4, doc.Length, true).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext("ba", 2, doc.Length, true).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 3, doc.FindNext("ab", 2, doc.Length, true).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 5, doc.FindNext("cab", 2, doc.Length, true).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					Assert.AreEqual( 1, doc.FindNext("ab", 0, 4, true).Begin );

					// word at the end
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext("ba", 0, 4, true).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					Assert.AreEqual( null, doc.FindNext("abc", 0, 4, true) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				Assert.AreEqual( 1, doc.FindNext("ab", 0, 4, true).Begin );
			}
		}

		[Test]
		public void FindPrev()
		{
			var buf = new TextBuffer( 128, 128 );
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
			var doc = new Document();
			IRange result;
			doc.Replace( "aababcabcd" );

			// black box test
			{
				// null argument
				Assert.Throws<ArgumentNullException>( delegate{
					doc.FindNext( (Regex)null, 1, 2 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( new Regex("a[^b]+"), -1, 2 );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext( new Regex("a[^b]+"), 2, 1 );
				} );

				// empty range
				result = doc.FindNext( new Regex("a[^b]+"), 0, 0 );
				Assert.AreEqual( null, result );

				// range exceeding text length
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					doc.FindNext(new Regex("a[^b]+"), 1, 9999);
				} );

				// invalid Regex option
				Assert.Throws<ArgumentException>( delegate{
					doc.FindNext(new Regex("a[^b]+", RegexOptions.RightToLeft), 1, 4);
				} );

				// pattern ord at begin
				result = doc.FindNext( new Regex("a[^b]+"), 0, 2 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 2, result.End );

				// pattern in the range
				result = doc.FindNext( new Regex("a[^a]+"), 0, 3 );
				Assert.AreEqual( 1, result.Begin );
				Assert.AreEqual( 3, result.End );

				// pattern which ends at end
				result = doc.FindNext( new Regex("[ab]+"), 0, 5 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 5, result.End );

				// pattern... well, pretty hard to describe in English for me...
				result = doc.FindNext( new Regex("[abc]+"), 0, 5 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 5, result.End );
				result = doc.FindNext( new Regex("[abc]+"), 0, 10 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 9, result.End );

				// empty pattern
				result = doc.FindNext( new Regex(""), 0, 10 );
				Assert.AreEqual( 0, result.Begin );
				Assert.AreEqual( 0, result.End );

				// comp. options
				result = doc.FindNext( new Regex("aBcD"), 0, doc.Length );
				Assert.AreEqual( null, result );
				result = doc.FindNext( new Regex("aBcD", RegexOptions.IgnoreCase), 0, doc.Length );
				Assert.AreEqual(  6, result.Begin );
				Assert.AreEqual( 10, result.End);
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: aaba......bcabcd)

				// gap < begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext(new Regex("ab"), 5, doc.Length).Begin );

				// gap == begin
				MoveGap( doc, 4 );
				Assert.AreEqual( 6, doc.FindNext(new Regex("ab"), 4, doc.Length).Begin );

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext(new Regex("ba"), 2, doc.Length).Begin );

					// word crossing the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 3, doc.FindNext(new Regex("ab"), 2, doc.Length).Begin );

					// word after the gap
					MoveGap( doc, 4 );
					Assert.AreEqual( 5, doc.FindNext(new Regex("cab"), 2, doc.Length).Begin );
				}

				// gap == end
				{
					MoveGap( doc, 4 );
					Assert.AreEqual( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );

					// word at the end
					MoveGap( doc, 4 );
					Assert.AreEqual( 2, doc.FindNext(new Regex("ba"), 0, 4).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 4 );
					Assert.AreEqual( null, doc.FindNext(new Regex("abc"), 0, 4) );
				}

				// end <= gap
				MoveGap( doc, 4 );
				Assert.AreEqual( 1, doc.FindNext(new Regex("ab"), 0, 4).Begin );
			}
		}

		[Test]
		public void FindPrevR()
		{
			var doc = new Document();
			doc.Replace( "abcdabcaba" );

			// black box test (interface test)
			{
				// null target
				Assert.Throws<ArgumentNullException>( delegate {
					doc.FindPrev( (Regex)null, 0, 10 );
				} );

				// negative index
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), -1, 10 );
				} );

				// invalid regex option
				Assert.Throws<ArgumentException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.None), 0, doc.Length );
				} );

				// end index at out of range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), 0, doc.Length+1 );
				} );

				// inverted range
				Assert.Throws<ArgumentOutOfRangeException>( delegate {
					doc.FindPrev( new Regex("a", RegexOptions.RightToLeft), 1, 0 );
				} );

				// empty range
				Assert.AreEqual( null, doc.FindPrev(new Regex("a", RegexOptions.RightToLeft), 0, 0) );

				// find in valid range
				Assert.AreEqual( 9, doc.FindPrev(new Regex(   "a", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 7, doc.FindPrev(new Regex(  "ab", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 4, doc.FindPrev(new Regex( "abc", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( 0, doc.FindPrev(new Regex("abcd", RegexOptions.RightToLeft), 0, 10).Begin );
				Assert.AreEqual( null, doc.FindPrev(new Regex("abcde", RegexOptions.RightToLeft), 0, 10) );

				// empty pattern (returns end index)
				Assert.AreEqual( 10, doc.FindPrev(new Regex("", RegexOptions.RightToLeft), 0, 10).Begin );

				// comp. options
				Assert.AreEqual( null, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft), 0, 10) );
				Assert.AreEqual(  0, doc.FindPrev(new Regex("aBcD", RegexOptions.RightToLeft|RegexOptions.IgnoreCase), 0, 10).Begin );
			}

			// white box test (test of the gap condition. test only result.)
			{
				// (buf: abcda......bcaba)

				// gap < begin
				MoveGap( doc, 5 );
				Assert.AreEqual( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 7, 10).Begin );

				// gap == begin
				{
					MoveGap( doc, 5 );
					Assert.AreEqual( 7, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 5, 10).Begin );

					// word at the begin
					MoveGap( doc, 5 );
					Assert.AreEqual( 5, doc.FindPrev(new Regex("bc", RegexOptions.RightToLeft), 5, 10).Begin );

					// partially matched word but overruning boundary
					MoveGap( doc, 5 );
					Assert.AreEqual( null, doc.FindPrev(new Regex("abca", RegexOptions.RightToLeft), 5, 10) );
				}

				// begin < gap < end
				{
					// word before the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 3, doc.FindPrev(new Regex("da", RegexOptions.RightToLeft), 0, 10).Begin );

					// word crossing the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 4, doc.FindPrev(new Regex("abc", RegexOptions.RightToLeft), 0, 10).Begin );

					// word after the gap
					MoveGap( doc, 5 );
					Assert.AreEqual( 5, doc.FindPrev(new Regex("bca", RegexOptions.RightToLeft), 0, 10).Begin );
				}

				// gap == end
				MoveGap( doc, 5 );
				Assert.AreEqual( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 5).Begin );

				// end <= gap
				MoveGap( doc, 5 );
				Assert.AreEqual( 0, doc.FindPrev(new Regex("ab", RegexOptions.RightToLeft), 0, 4).Begin );
			}
		}

		[Test]
		public void TrackingRange()
		{
			// Insertion before the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Outward );
				buf.Insert( 0, "x" );
				Assert.AreEqual( "[2, 3)", rangeB.ToString() );
				Assert.AreEqual( "[2, 3)", rangeF.ToString() );
				Assert.AreEqual( "[2, 3)", rangeI.ToString() );
				Assert.AreEqual( "[2, 3)", rangeO.ToString() );
			}

			// Insertion at beginning index
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Outward );
				buf.Insert( 1, "x" );
				Assert.AreEqual( "[1, 4)", rangeB.ToString() );
				Assert.AreEqual( "[2, 4)", rangeF.ToString() );
				Assert.AreEqual( "[2, 4)", rangeI.ToString() );
				Assert.AreEqual( "[1, 4)", rangeO.ToString() );
			}

			// Insertion at ending index
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 3, BoundaryTrackingMode.Outward );
				buf.Insert( 3, "x" );
				Assert.AreEqual( "[1, 3)", rangeB.ToString() );
				Assert.AreEqual( "[1, 4)", rangeF.ToString() );
				Assert.AreEqual( "[1, 3)", rangeI.ToString() );
				Assert.AreEqual( "[1, 4)", rangeO.ToString() );
			}

			// Insertion after the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcd" };
				var rangeB = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 2, BoundaryTrackingMode.Outward );
				buf.Insert( 3, "x" );
				Assert.AreEqual( "[1, 2)", rangeB.ToString() );
				Assert.AreEqual( "[1, 2)", rangeF.ToString() );
				Assert.AreEqual( "[1, 2)", rangeI.ToString() );
				Assert.AreEqual( "[1, 2)", rangeO.ToString() );
			}

			// Removal before the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 0, 1 );
				Assert.AreEqual( "[0, 3)", rangeB.ToString() );
				Assert.AreEqual( "[0, 3)", rangeF.ToString() );
				Assert.AreEqual( "[0, 3)", rangeI.ToString() );
				Assert.AreEqual( "[0, 3)", rangeO.ToString() );
			}

			// Removal - a range to be removed covers a tracking range's beginning
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 0, 2 );
				Assert.AreEqual( "[0, 2)", rangeB.ToString() );
				Assert.AreEqual( "[0, 2)", rangeF.ToString() );
				Assert.AreEqual( "[0, 2)", rangeI.ToString() );
				Assert.AreEqual( "[0, 2)", rangeO.ToString() );
			}

			// Removal at beginning index
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 1, 2 );
				Assert.AreEqual( "[0, 3)", rangeB.ToString() );
				Assert.AreEqual( "[0, 3)", rangeF.ToString() );
				Assert.AreEqual( "[0, 3)", rangeI.ToString() );
				Assert.AreEqual( "[0, 3)", rangeO.ToString() );
			}

			// Removal - a range to be removed ends at the same position
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 3, 4 );
				Assert.AreEqual( "[1, 3)", rangeB.ToString() );
				Assert.AreEqual( "[1, 3)", rangeF.ToString() );
				Assert.AreEqual( "[1, 3)", rangeI.ToString() );
				Assert.AreEqual( "[1, 3)", rangeO.ToString() );
			}

			// Removal after the range
			{
				var buf = new TextBuffer( 256, 256 ){ "abcde" };
				var rangeB = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Backward );
				var rangeF = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Forward );
				var rangeI = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Inward );
				var rangeO = buf.CreateTrackingRange( 1, 4, BoundaryTrackingMode.Outward );
				buf.Remove( 4, 5 );
				Assert.AreEqual( "[1, 4)", rangeB.ToString() );
				Assert.AreEqual( "[1, 4)", rangeF.ToString() );
				Assert.AreEqual( "[1, 4)", rangeI.ToString() );
				Assert.AreEqual( "[1, 4)", rangeO.ToString() );
			}
		}

		[Test]
		public void Range()
		{
			{
				IRange range;
				IEnumerator<CharData> iter;
				var doc = new Document(); doc.Replace( "aa\x0300a" );

				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					range = new Range( doc.Buffer, -1, 0 );
				} );
				Assert.Throws<ArgumentOutOfRangeException>( delegate{
					range = new Range( doc.Buffer, 0, -1 );
				} );
				Assert.Throws<ArgumentException>( delegate{
					range = new Range( doc.Buffer, 1, 0 );
				} );

				range = new Range( doc.Buffer, 0, 0 );
				Assert.AreEqual( true, range.IsEmpty );
				Assert.AreEqual( 0, range.Begin );
				Assert.AreEqual( 0, range.End );
				Assert.AreEqual( 0, range.Length );
				Assert.AreEqual( "", range.Text );
				iter = range.Chars.GetEnumerator();
				Assert.AreEqual( false, iter.MoveNext() );

				range = new Range( doc.Buffer, 1, 4 );
				Assert.AreEqual( false, range.IsEmpty );
				Assert.AreEqual( 1, range.Begin );
				Assert.AreEqual( 4, range.End );
				Assert.AreEqual( 3, range.Length );
				Assert.AreEqual( "a\x0300a", range.Text );
				iter = range.Chars.GetEnumerator();
				Assert.AreEqual( true, iter.MoveNext() );
				Assert.AreEqual( 2, iter.Current.Length );
				Assert.AreEqual( 'a', iter.Current.ToChar() );
				Assert.AreEqual( "a\x0300", iter.Current.ToString() );
				Assert.AreEqual( true, iter.MoveNext() );
				Assert.AreEqual( 1, iter.Current.Length );
				Assert.AreEqual( 'a', iter.Current.ToChar() );
				Assert.AreEqual( "a", iter.Current.ToString() );
				Assert.AreEqual( false, iter.MoveNext() );
			}
		}

		#region Utilities
		static string MakeLdsText( TextBuffer text )
		{
			var buf = new StringBuilder( 32 );

			for( int i=0; i<text.Lines.Count; i++ )
			{
				char ch = '#';

				switch( text.Lines[i].DirtyState )
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
