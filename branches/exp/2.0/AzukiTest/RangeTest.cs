using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	class RangeTest
	{
		[Test]
		public void Ctor()
		{
			IRange range;
			var doc = new Document() { Text = "abcd" };

			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				range = new Range( doc, -1, 0 );
			} );
			Assert.Throws<ArgumentOutOfRangeException>( delegate{
				range = new Range( doc, 0, -1 );
			} );
			Assert.Throws<ArgumentException>( delegate{
				range = new Range( doc, 1, 0 );
			} );
			Assert.DoesNotThrow( delegate{
				range = new Range( doc, 0, 5 );
			} );
		}

		[Test]
		public void BufferAssociatedOps()
		{
			var doc = new Document() { Text = "abcd" };
			var range1 = new Range( 1, 3 );
			var range2 = new Range( doc, 1, 3 );

			Assert.Throws<InvalidOperationException>( delegate{
				range1.AutoUpdateMode = AutoUpdateMode.Backward;
			} );
			Assert.DoesNotThrow( delegate{
				range2.AutoUpdateMode = AutoUpdateMode.Backward;
			} );

			Assert.Throws<InvalidOperationException>( delegate{
				var c = range1.Chars;
			} );
			Assert.DoesNotThrow( delegate{
				var c = range2.Chars;
			} );

			Assert.Throws<InvalidOperationException>( delegate{
				var c = range1.RawChars;
			} );
			Assert.DoesNotThrow( delegate{
				var c = range2.RawChars;
			} );

			Assert.Throws<InvalidOperationException>( delegate{
				var c = range1.Text;
			} );
			Assert.DoesNotThrow( delegate{
				var c = range2.Text;
			} );

			Assert.Throws<InvalidOperationException>( delegate{
				var c = range1[0];
			} );
			Assert.DoesNotThrow( delegate{
				var c = range2[0];
			} );
		}

		[Test]
		public void BasicTest()
		{
			var doc = new Document(){ Text = "\x61\x61\x300\xe0" }; // aàà
			var range = new Range( doc, 0, 0 );
			Assert.AreEqual( true, range.IsEmpty );
			Assert.AreEqual( 0, range.Begin );
			Assert.AreEqual( 0, range.End );
			Assert.AreEqual( 0, range.Length );
			Assert.AreEqual( "", range.Text );

			range = new Range( doc, 0, 3 );
			Assert.AreEqual( false, range.IsEmpty );
			Assert.AreEqual( 0, range.Begin );
			Assert.AreEqual( 3, range.End );
			Assert.AreEqual( 3, range.Length );
			Assert.AreEqual( "\x61\x61\x300", range.Text );

			range = new Range( doc, 0, 2 );
			Assert.AreEqual( false, range.IsEmpty );
			Assert.AreEqual( 0, range.Begin );
			Assert.AreEqual( 2, range.End );
			Assert.AreEqual( 2, range.Length );
			Assert.AreEqual( "\x61\x61\x300", range.Text ); // doesn't split
		}

		[Test]
		public void Iteration()
		{
			var doc = new Document(){ Text = "\x61\x61\x300\xe0" }; // aàà
			var range = new Range( doc, 0, 0 );
			var iter = range.Chars.GetEnumerator();
			Assert.AreEqual( false, iter.MoveNext() );

			range = new Range( doc, 0, 4 );
			iter = range.Chars.GetEnumerator();
			Assert.AreEqual( true, iter.MoveNext() );
			Assert.AreEqual( '\x61', (char)iter.Current );
			Assert.AreEqual( true, iter.MoveNext() );
			Assert.AreEqual( '\x61', (char)iter.Current );
			Assert.AreEqual( "\x61\x300", (string)iter.Current );
			Assert.AreEqual( true, iter.MoveNext() );
			Assert.AreEqual( '\xe0', (char)iter.Current );
			Assert.AreEqual( false, iter.MoveNext() );
		}

		[Test]
		public void AutoUpdate()
		{
			var doc = new Document();

			//    a b c d    ==>   X a b c d
			// B    ~~~~               ~~~~
			// F    ~~~~               ~~~~
			// I    ~~~~               ~~~~
			// O    ~~~~               ~~~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "X", 0, 0 );
				Assert.AreEqual( "[2, 4)", rangeB.ToString() );
				Assert.AreEqual( "[2, 4)", rangeF.ToString() );
				Assert.AreEqual( "[2, 4)", rangeI.ToString() );
				Assert.AreEqual( "[2, 4)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a X b c d
			// B    ~~~~             ~~~~~~
			// F    ~~~~               ~~~~
			// I    ~~~~               ~~~~
			// O    ~~~~             ~~~~~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "X", 1, 1 );
				Assert.AreEqual( "[1, 4)", rangeB.ToString() );
				Assert.AreEqual( "[2, 4)", rangeF.ToString() );
				Assert.AreEqual( "[2, 4)", rangeI.ToString() );
				Assert.AreEqual( "[1, 4)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a b X c d
			// B    ~~~~             ~~~~~~
			// F    ~~~~             ~~~~~~
			// I    ~~~~             ~~~~~~
			// O    ~~~~             ~~~~~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "X", 2, 2 );
				Assert.AreEqual( "[1, 4)", rangeB.ToString() );
				Assert.AreEqual( "[1, 4)", rangeF.ToString() );
				Assert.AreEqual( "[1, 4)", rangeI.ToString() );
				Assert.AreEqual( "[1, 4)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a b c X d
			// B    ~~~~             ~~~~
			// F    ~~~~             ~~~~~~
			// I    ~~~~             ~~~~
			// O    ~~~~             ~~~~~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "X", 3, 3 );
				Assert.AreEqual( "[1, 3)", rangeB.ToString() );
				Assert.AreEqual( "[1, 4)", rangeF.ToString() );
				Assert.AreEqual( "[1, 3)", rangeI.ToString() );
				Assert.AreEqual( "[1, 4)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a b c d X
			// B    ~~~~             ~~~~
			// F    ~~~~             ~~~~
			// I    ~~~~             ~~~~
			// O    ~~~~             ~~~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "X", 4, 4 );
				Assert.AreEqual( "[1, 3)", rangeB.ToString() );
				Assert.AreEqual( "[1, 3)", rangeF.ToString() );
				Assert.AreEqual( "[1, 3)", rangeI.ToString() );
				Assert.AreEqual( "[1, 3)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   b c d
			// B    ~~~~           ~~~~
			// F    ~~~~           ~~~~
			// I    ~~~~           ~~~~
			// O    ~~~~           ~~~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "", 0, 1 );
				Assert.AreEqual( "[0, 2)", rangeB.ToString() );
				Assert.AreEqual( "[0, 2)", rangeF.ToString() );
				Assert.AreEqual( "[0, 2)", rangeI.ToString() );
				Assert.AreEqual( "[0, 2)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   c d
			// B    ~~~~           ~~
			// F    ~~~~           ~~
			// I    ~~~~           ~~
			// O    ~~~~           ~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "", 0, 2 );
				Assert.AreEqual( "[0, 1)", rangeB.ToString() );
				Assert.AreEqual( "[0, 1)", rangeF.ToString() );
				Assert.AreEqual( "[0, 1)", rangeI.ToString() );
				Assert.AreEqual( "[0, 1)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a c d
			// B    ~~~~             ~~
			// F    ~~~~             ~~
			// I    ~~~~             ~~
			// O    ~~~~             ~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "", 1, 2 );
				Assert.AreEqual( "[1, 2)", rangeB.ToString() );
				Assert.AreEqual( "[1, 2)", rangeF.ToString() );
				Assert.AreEqual( "[1, 2)", rangeI.ToString() );
				Assert.AreEqual( "[1, 2)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a b d
			// B    ~~~~             ~~
			// F    ~~~~             ~~
			// I    ~~~~             ~~
			// O    ~~~~             ~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "", 2, 3 );
				Assert.AreEqual( "[1, 2)", rangeB.ToString() );
				Assert.AreEqual( "[1, 2)", rangeF.ToString() );
				Assert.AreEqual( "[1, 2)", rangeI.ToString() );
				Assert.AreEqual( "[1, 2)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a b c
			// B    ~~~~             ~~~~
			// F    ~~~~             ~~~~
			// I    ~~~~             ~~~~
			// O    ~~~~             ~~~~
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 3 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "", 3, 4 );
				Assert.AreEqual( "[1, 3)", rangeB.ToString() );
				Assert.AreEqual( "[1, 3)", rangeF.ToString() );
				Assert.AreEqual( "[1, 3)", rangeI.ToString() );
				Assert.AreEqual( "[1, 3)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a X b c d
			// B   |                  |
			// F   |                  |
			// I   |                  |
			// O   |                  |
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "X", 1, 1 );
				Assert.AreEqual( "[2, 2)", rangeB.ToString() );
				Assert.AreEqual( "[2, 2)", rangeF.ToString() );
				Assert.AreEqual( "[2, 2)", rangeI.ToString() );
				Assert.AreEqual( "[2, 2)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}

			//    a b c d    ==>   a b X c d
			// B   |                |
			// F   |                |
			// I   |                |
			// O   |                |
			{
				doc.Text = "abcd";
				var rangeB = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Backward };
				var rangeF = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Forward };
				var rangeI = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Inward };
				var rangeO = new Range( doc, 1, 1 ) { AutoUpdateMode = AutoUpdateMode.Outward };
				doc.Replace( "X", 2, 2 );
				Assert.AreEqual( "[1, 1)", rangeB.ToString() );
				Assert.AreEqual( "[1, 1)", rangeF.ToString() );
				Assert.AreEqual( "[1, 1)", rangeI.ToString() );
				Assert.AreEqual( "[1, 1)", rangeO.ToString() );
				rangeB.AutoUpdateMode
					= rangeF.AutoUpdateMode
					= rangeI.AutoUpdateMode
					= rangeO.AutoUpdateMode = AutoUpdateMode.None;
			}
		}
	}
}
