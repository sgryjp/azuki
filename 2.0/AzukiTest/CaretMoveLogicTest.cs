using System;
using NUnit.Framework;
using Sgry.Azuki.WinForms;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	public class CaretMoveLogicTest
	{
		static AzukiControl _Azuki;

		[SetUp]
		public void SetUp()
		{
			_Azuki = new AzukiControl();
		}

		[TearDown]
		public void TearDown()
		{
			_Azuki.Dispose();
		}

		[Test]
		public void Right()
		{
			// EOL
			_Azuki.Text = "a\rb\nc\r\nd";
			_Azuki.Document.SetSelection( 0, 0 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 1, 1 );
			Assert.AreEqual( 2, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 2, 2 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 3, 3 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 4, 4 );
			Assert.AreEqual( 5, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 5, 5 );
			Assert.AreEqual( 7, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 7, 7 );
			Assert.AreEqual( 8, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 8, 8 );
			Assert.AreEqual( 8, CaretMoveLogic.Calc_Right(_Azuki.View) );

			// surrogate pair
			_Azuki.Text = "_\xd85a\xdd51_";
			_Azuki.Document.SetSelection( 0, 0 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 1, 1 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 3, 3 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 4, 4 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );

			// combined character sequence
			_Azuki.Text = "_a\x0300_";
			_Azuki.Document.SetSelection( 0, 0 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 1, 1 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 3, 3 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
			_Azuki.Document.SetSelection( 4, 4 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Right(_Azuki.View) );
		}

		[Test]
		public void Left()
		{
			// EOL
			_Azuki.Text = "a\rb\nc\r\nd";
			_Azuki.Document.SetSelection( 8, 8 );
			Assert.AreEqual( 7, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 7, 7 );
			Assert.AreEqual( 5, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 5, 5 );
			Assert.AreEqual( 4, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 4, 4 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 3, 3 );
			Assert.AreEqual( 2, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 2, 2 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 1, 1 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 0, 0 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );

			// surrogate pair
			_Azuki.Text = "a\xd85a\xdd51b";
			_Azuki.Document.SetSelection( 4, 4 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 3, 3 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 1, 1 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 0, 0 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );

			// combined character sequence
			_Azuki.Text = "_a\x0300_";
			_Azuki.Document.SetSelection( 4, 4 );
			Assert.AreEqual( 3, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 3, 3 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 1, 1 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
			_Azuki.Document.SetSelection( 0, 0 );
			Assert.AreEqual( 0, CaretMoveLogic.Calc_Left(_Azuki.View) );
		}

		[Test]
		public void NextWord()
		{
			string[][] samples = new string[][] {
				new string[]{"aaaa",           "aa11",           "aa,,",           "aa\x3042\x3042",           "aa\x30a2\x30a2",           "aa\x963f\x963f",           "aa\n\n",           "aa  "          },
				new string[]{"11aa",           "1111",           "11,,",           "11\x3042\x3042",           "11\x30a2\x30a2",           "11\x963f\x963f",           "11\n\n",           "11  "          },
				new string[]{",,aa",           ",,11",           ",,,,",           ",,\x3042\x3042",           ",,\x30a2\x30a2",           ",,\x963f\x963f",           ",,\n\n",           ",,  "          },
				new string[]{"\x3042\x3042aa", "\x3042\x304211", "\x3042\x3042,,", "\x3042\x3042\x3042\x3042", "\x3042\x3042\x30a2\x30a2", "\x3042\x3042\x963f\x963f", "\x3042\x3042\n\n", "\x3042\x3042  "},
				new string[]{"\x30a2\x30a2aa", "\x30a2\x30a211", "\x30a2\x30a2,,", "\x30a2\x30a2\x3042\x3042", "\x30a2\x30a2\x30a2\x30a2", "\x30a2\x30a2\x963f\x963f", "\x30a2\x30a2\n\n", "\x30a2\x30a2  "},
				new string[]{"\x963f\x963faa", "\x963f\x963f11", "\x963f\x963f,,", "\x963f\x963f\x3042\x3042", "\x963f\x963f\x30a2\x30a2", "\x963f\x963f\x963f\x963f", "\x963f\x963f\n\n", "\x963f\x963f  "},
				new string[]{"\n\naa",         "\n\n11",         "\n\n,,",         "\n\n\x3042\x3042",         "\n\n\x30a2\x30a2",         "\n\n\x963f\x963f",         "\n\n\n\n",         "\n\n  "        },
				new string[]{"  aa",           "  11",           "  ,,",           "  \x3042\x3042",           "  \x30a2\x30a2",           "  \x963f\x963f",           "  \n\n",           "    "          }
			};

			// NextWord
			int[] s = new int[]{4,4,4,4,4}; // same class
			int[] d = new int[]{2,2,4,4,4}; // different class
			int[] l = new int[]{2,2,3,4,4}; // latter half is \n\n
			int[] f = new int[]{1,2,4,4,4}; // former half is \n\n
			int[] b = new int[]{1,2,3,4,4}; // both half are \n\n
			int[] w = new int[]{4,4,4,4,4}; // latter half is whitespace
			int[][][] expected = new int[][][] {
				new int[][]{ s, d, d, d, d, d, l, w },
				new int[][]{ d, s, d, d, d, d, l, w },
				new int[][]{ d, d, s, d, d, d, l, w },
				new int[][]{ d, d, d, s, d, d, l, w },
				new int[][]{ d, d, d, d, s, d, l, w },
				new int[][]{ d, d, d, d, d, s, l, w },
				new int[][]{ f, f, f, f, f, f, b, f },
				new int[][]{ d, d, d, d, d, d, l, s }
			};

			for( int x=0; x<samples.Length; x++ )
			{
				for( int y=0; y<samples[x].Length; y++ )
				{
					_Azuki.Text = samples[x][y];
					for( int i=0; i<_Azuki.TextLength; i++ )
					{
						try
						{
							_Azuki.Document.SetSelection( i, i );
							int actual = CaretMoveLogic.Calc_NextWord( _Azuki.View );
							Assert.AreEqual( expected[x][y][i], actual );
						}
						catch( AssertException ex )
						{
							Console.Error.WriteLine( "### x={0}, y={1}, i={2}, Azuki.Text=[{3}]", x, y, i, _Azuki.Text );
							throw ex;
						}
					}
				}
			}
		}

		[Test]
		public void PrevWord()
		{
			string[][] samples = new string[][] {
				new string[]{"aaaa",           "aa11",           "aa,,",           "aa\x3042\x3042",           "aa\x30a2\x30a2",           "aa\x963f\x963f",           "aa\n\n",           "aa  "          },
				new string[]{"11aa",           "1111",           "11,,",           "11\x3042\x3042",           "11\x30a2\x30a2",           "11\x963f\x963f",           "11\n\n",           "11  "          },
				new string[]{",,aa",           ",,11",           ",,,,",           ",,\x3042\x3042",           ",,\x30a2\x30a2",           ",,\x963f\x963f",           ",,\n\n",           ",,  "          },
				new string[]{"\x3042\x3042aa", "\x3042\x304211", "\x3042\x3042,,", "\x3042\x3042\x3042\x3042", "\x3042\x3042\x30a2\x30a2", "\x3042\x3042\x963f\x963f", "\x3042\x3042\n\n", "\x3042\x3042  "},
				new string[]{"\x30a2\x30a2aa", "\x30a2\x30a211", "\x30a2\x30a2,,", "\x30a2\x30a2\x3042\x3042", "\x30a2\x30a2\x30a2\x30a2", "\x30a2\x30a2\x963f\x963f", "\x30a2\x30a2\n\n", "\x30a2\x30a2  "},
				new string[]{"\x963f\x963faa", "\x963f\x963f11", "\x963f\x963f,,", "\x963f\x963f\x3042\x3042", "\x963f\x963f\x30a2\x30a2", "\x963f\x963f\x963f\x963f", "\x963f\x963f\n\n", "\x963f\x963f  "},
				new string[]{"\n\naa",         "\n\n11",         "\n\n,,",         "\n\n\x3042\x3042",         "\n\n\x30a2\x30a2",         "\n\n\x963f\x963f",         "\n\n\n\n",         "\n\n  "        },
				new string[]{"  aa",           "  11",           "  ,,",           "  \x3042\x3042",           "  \x30a2\x30a2",           "  \x963f\x963f",           "  \n\n",           "    "          }
			};

			int[] s = new int[]{0,0,0,0,0}; // same class
			int[] d = new int[]{0,0,0,2,2}; // different class
			int[] l = new int[]{0,0,0,2,3}; // latter half is \n\n
			int[] f = new int[]{0,0,1,2,2}; // former half is \n\n
			int[] b = new int[]{0,0,1,2,3}; // both half are \n\n
			int[] w = new int[]{0,0,0,0,0}; // latter half is whitespace
			int[][][] expected = new int[][][] {
				new int[][]{ s, d, d, d, d, d, l, w },
				new int[][]{ d, s, d, d, d, d, l, w },
				new int[][]{ d, d, s, d, d, d, l, w },
				new int[][]{ d, d, d, s, d, d, l, w },
				new int[][]{ d, d, d, d, s, d, l, w },
				new int[][]{ d, d, d, d, d, s, l, w },
				new int[][]{ f, f, f, f, f, f, b, f },
				new int[][]{ d, d, d, d, d, d, l, s }
			};

			for( int x=0; x<samples.Length; x++ )
			{
				for( int y=0; y<samples[x].Length; y++ )
				{
					_Azuki.Text = samples[x][y];
					for( int i=0; i<_Azuki.TextLength; i++ )
					{
						try
						{
							_Azuki.Document.SetSelection( i, i );
							int actual = CaretMoveLogic.Calc_PrevWord( _Azuki.View );
							Assert.AreEqual( expected[x][y][i], actual );
						}
						catch( AssertException ex )
						{
							Console.Error.WriteLine( "### x={0}, y={1}, i={2}, Azuki.Text=[{3}]", x, y, i, _Azuki.Text );
							throw ex;
						}
					}
				}
			}

			// EOL code
			_Azuki.Text = "a\r";
			_Azuki.Document.SetSelection( 2, 2 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_PrevWord(_Azuki.View) );

			// EOL code
			_Azuki.Text = "a\r\n";
			_Azuki.Document.SetSelection( 3, 3 );
			Assert.AreEqual( 1, CaretMoveLogic.Calc_PrevWord(_Azuki.View) );
		}
	}
}