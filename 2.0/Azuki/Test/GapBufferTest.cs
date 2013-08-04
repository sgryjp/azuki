#if TEST
using System;

namespace Sgry.Azuki.Test
{
	static class GapBufferTest
	{
		public static void Test()
		{
			int testNum = 0;
			Console.WriteLine( "[Test for Azuki.GapBuffer]" );

			// init
			Console.WriteLine( "test {0} - initial state", ++testNum );
			TestUtl.Do( Test_Init );

			// add
			Console.WriteLine( "test {0} - Add()", ++testNum );
			TestUtl.Do( Test_Add );

			// clear
			Console.WriteLine( "test {0} - Clear()", ++testNum );
			TestUtl.Do( Test_Clear );

			// Insert
			Console.WriteLine( "test {0} - Insert()", ++testNum );
			TestUtl.Do( Test_Insert_One );
			TestUtl.Do( Test_Insert_Array );

			// Replace
			Console.WriteLine( "test {0} - Replace()", ++testNum );
			TestUtl.Do( Test_Replace );

			// RemoveRange
			Console.WriteLine( "test {0} - RemoveRange()", ++testNum );
			TestUtl.Do( Test_RemoveRange );
			
			// CopyTo
			Console.WriteLine( "test {0} - CopyTo()", ++testNum );
			TestUtl.Do( Test_CopyTo );

			// Binary search
			Console.WriteLine( "test {0} - Binary search()", ++testNum );
			TestUtl.Do( Test_BinarySearch );

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_Init()
		{
			GapBuffer<char> chars = new GapBuffer<char>( 5, 8 );

			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				TestUtl.AssertThrows<AssertException>( delegate{
					chars.GetAt( x );
				} );
				TestUtl.AssertThrows<AssertException>( delegate{
					chars.SetAt( '!', x );
				} );
			}
		}

		static void Test_Add()
		{
			GapBuffer<char> chars = new GapBuffer<char>( 5, 8 );

			chars.Add( 'a' );
			TestUtl.AssertEquals( 1, chars.Count );
			TestUtl.AssertEquals( 'a', chars.GetAt(0) );
			chars.SetAt( 'b', 0 );
			TestUtl.AssertEquals( 'b', chars.GetAt(0) );
			TestUtl.AssertThrows<AssertException>( delegate{
				chars.GetAt( 1 );
			} );
		}

		static void Test_Clear()
		{
			GapBuffer<char> chars = new GapBuffer<char>( 5, 8 );

			chars.Clear();
			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				TestUtl.AssertThrows<AssertException>( delegate{
					chars.GetAt( x );
				} );
				TestUtl.AssertThrows<AssertException>( delegate{
					chars.SetAt( '!', x );
				} );
			}

			chars.Add( "hoge".ToCharArray() );
			chars.Clear();
			TestUtl.AssertEquals( 0, chars.Count );
			for( int x=0; x<10; x++ )
			{
				TestUtl.AssertThrows<AssertException>( delegate{
					chars.GetAt( x );
				} );
				TestUtl.AssertThrows<AssertException>( delegate{
					chars.SetAt( '!', x );
				} );
			}
		}

		static void Test_Insert_One()
		{
			const string InitData = "hogepiyo";
			GapBuffer<char> sary = new GapBuffer<char>( 5, 8 );

			// control-char
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 3, '\0' );
			TestUtl.AssertEquals( "hog\0epiyo", ToString(sary) );

			// before head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			TestUtl.AssertThrows<AssertException>( delegate{
				sary.Insert( -1, 'G' );
			} );

			// head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, 'G' );
			TestUtl.AssertEquals( 9, sary.Count );
			TestUtl.AssertEquals( "Ghogepiyo", ToString(sary) );

			// middle
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 4, 'G' );
			TestUtl.AssertEquals( 9, sary.Count );
			TestUtl.AssertEquals( "hogeGpiyo", ToString(sary) );

			// end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 8, 'G' );
			TestUtl.AssertEquals( 9, sary.Count );
			TestUtl.AssertEquals( "hogepiyoG", ToString(sary) );

			// after end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			TestUtl.AssertThrows<AssertException>( delegate{
				sary.Insert( 9, 'G' );
			} );
		}

		static void Test_Insert_Array()
		{
			const string InitData = "hogepiyo";
			GapBuffer<char> sary = new GapBuffer<char>( 5, 8 );

			// null array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			TestUtl.AssertThrows<AssertException>( delegate{
				sary.Insert( 0, null );
			} );
			
			// empty array
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, "".ToCharArray() );
			TestUtl.AssertEquals( "hogepiyo", ToString(sary) );

			// before head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			TestUtl.AssertThrows<AssertException>( delegate{
				sary.Insert( -1, "FOO".ToCharArray() );
			} );

			// head
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 0, "FOO".ToCharArray() );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "FOOhogepiyo", ToString(sary) );

			// middle
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 4, "FOO".ToCharArray() );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "hogeFOOpiyo", ToString(sary) );

			// end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			sary.Insert( 8, "FOO".ToCharArray() );
			TestUtl.AssertEquals( 11, sary.Count );
			TestUtl.AssertEquals( "hogepiyoFOO", ToString(sary) );

			// after end
			sary.Clear();
			sary.Add( InitData.ToCharArray() );
			TestUtl.AssertThrows<AssertException>( delegate{
				sary.Insert( 9, "FOO".ToCharArray() );
			} );
		}

		static void Test_Replace()
		{
			const string InitData = "hogepiyo";
			GapBuffer<char> sary = new GapBuffer<char>( 5, 8 );

			// replace position
			{
				// before head
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				TestUtl.AssertThrows<AssertException>( delegate{
					sary.Replace( -1, "000".ToCharArray(), 0, 2 );
				} );

				// head
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 0, "000".ToCharArray(), 0, 2 );
				TestUtl.AssertEquals( "00gepiyo", ToString(sary) );

				// middle
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 4, "000".ToCharArray(), 0, 2 );
				TestUtl.AssertEquals( "hoge00yo", ToString(sary) );

				// end
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				sary.Replace( 6, "000".ToCharArray(), 0, 2 );
				TestUtl.AssertEquals( "hogepi00", ToString(sary) );

				// after end
				sary.Clear();
				sary.Add( InitData.ToCharArray() );
				TestUtl.AssertThrows<AssertException>( delegate{
					sary.Replace( 7, "000".ToCharArray(), 0, 2 );
				} );
				TestUtl.AssertThrows<AssertException>( delegate{
					sary.Replace( 8, "000".ToCharArray(), 0, 2 );
				} );
			}

			// value array
			{
				// giving null
				TestUtl.AssertThrows<AssertException>( delegate{
					sary.Replace( 0, null, 0, 1 );
				} );

				// empty array
				sary.Replace( 0, "".ToCharArray(), 0, 0 );
				TestUtl.AssertEquals( "hogepiyo", ToString(sary) );

				// empty range
				sary.Replace( 0, "000".ToCharArray(), 0, 0 );
				TestUtl.AssertEquals( "hogepiyo", ToString(sary) );

				// invalid range (reversed)
				TestUtl.AssertThrows<AssertException>( delegate{
					sary.Replace( 0, "000".ToCharArray(), 1, 0 );
				} );

				// invalid range (before head)
				TestUtl.AssertThrows<AssertException>( delegate{
					sary.Replace( 0, "000".ToCharArray(), -1, 0 );
				} );

				// invalid range (after head)
				TestUtl.AssertThrows<AssertException>( delegate{
					sary.Replace( 0, "000".ToCharArray(), 3, 4 );
				} );
			}
		}

		static void Test_RemoveRange()
		{
			const string InitData = "hogepiyo";
			GapBuffer<char> chars = new GapBuffer<char>( 5, 8 );

			// case 2 (moving gap to buffer head)
			chars.Add( InitData.ToCharArray() );
			chars.RemoveAt( 2 );
			TestUtl.AssertEquals( 7, chars.Count );
			TestUtl.AssertEquals( "hoepiyo", ToString(chars) );
			TestUtl.AssertThrows<AssertException>( delegate{
				chars.GetAt( 7 );
			} );
			
			// case 1 (moving gap to buffer end)
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 5, 7 );
			TestUtl.AssertEquals( 6, chars.Count );
			TestUtl.AssertEquals( "hogepo", ToString(chars) );
			TestUtl.AssertThrows<AssertException>( delegate{
				chars.GetAt( 6 );
			} );
			
			// before head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			TestUtl.AssertThrows<AssertException>( delegate{
				chars.RemoveRange(-1, 2);
			} );
			
			// head to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 0, 2 );
			TestUtl.AssertEquals( "gepiyo", ToString(chars) );
			
			// middle to middle
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 2, 5 );
			TestUtl.AssertEquals( "hoiyo", ToString(chars) );
			
			// middle to end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			chars.RemoveRange( 5, 8 );
			TestUtl.AssertEquals( "hogep", ToString(chars) );
			
			// middle to after end
			chars.Clear();
			chars.Add( InitData.ToCharArray() );
			TestUtl.AssertThrows<AssertException>( delegate{
				chars.RemoveRange( 5, 9 );
			} );
		}

		static void Test_CopyTo()
		{
			const string initBufContent = "123456";
			GapBuffer<char> sary = new GapBuffer<char>( 5, 8 );
			char[] buf;
			sary.Insert( 0, "hogepiyo".ToCharArray() );

			// before head to middle
			buf = initBufContent.ToCharArray();
			TestUtl.AssertThrows<AssertException>( delegate{
				sary.CopyTo( -1, 5, buf );
			} );

			// begin to middle
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 0, 5, buf );
			TestUtl.AssertEquals( "hogep6", new String(buf) );

			// middle to middle
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 1, 5, buf );
			TestUtl.AssertEquals( "ogep56", new String(buf) );

			// middle to end
			buf = initBufContent.ToCharArray();
			sary.CopyTo( 5, 8, buf );
			TestUtl.AssertEquals( "iyo456", new String(buf) );

			// end to after end
			buf = initBufContent.ToCharArray();
			TestUtl.AssertThrows<AssertException>( delegate{
				sary.CopyTo( 5, 9, buf );
			} );
		}

		static void Test_BinarySearch()
		{
			GapBuffer<int> ary = new GapBuffer<int>( 4 );

			ary.Clear();
			TestUtl.AssertEquals( -1, ary.BinarySearch(1234) );

			ary.Clear();
			ary.Add( 3 );
			TestUtl.AssertEquals( ~(0), ary.BinarySearch(2) );
			TestUtl.AssertEquals(  (0), ary.BinarySearch(3) );
			TestUtl.AssertEquals( ~(1), ary.BinarySearch(4) );

			ary.Clear();
			ary.Add( 1, 3 );
			TestUtl.AssertEquals( ~(0), ary.BinarySearch(0) );
			TestUtl.AssertEquals(  (0), ary.BinarySearch(1) );
			TestUtl.AssertEquals( ~(1), ary.BinarySearch(2) );
			TestUtl.AssertEquals(  (1), ary.BinarySearch(3) );
			TestUtl.AssertEquals( ~(2), ary.BinarySearch(4) );

			var points = new GapBuffer<DummyData>( 4 );
			points.Add( new DummyData() );
			TestUtl.AssertThrows<ArgumentException>( delegate{
				points.BinarySearch( new DummyData() );
			} );
		}

		static string ToString( GapBuffer<char> sary )
		{
			return new string( sary.ToArray() );
		}

		class DummyData
		{}
	}
}
#endif
