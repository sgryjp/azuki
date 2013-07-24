#if TEST
using System;
using System.Text;
using System.Diagnostics;

namespace Sgry.Azuki.Test
{
	static class TextUtilTest
	{
		// --------------------
		// "keep it as simple as possible\r\n (head: 0, len:31)
		// \n                                 (head:32, len: 1)
		// but\n                              (head:33, len: 4)
		// \r                                 (head:37, len: 1)
		// not simpler."\r                    (head:38, len:14)
		// \r                                 (head:52, len: 1)
		//  - Albert Einstein                 (head:53, len:18)
		// --------------------
		const string TestData = "\"keep it as simple as possible\r\n\nbut\n\rnot simpler.\"\r\r - Albert Einstein";

		public static void Test()
		{
			int testNum = 0;

			Console.WriteLine( "[Test for Azuki.TextUtil]" );

			Console.WriteLine( "test {0} - NextLineHead()", ++testNum );
			TestUtl.Do( Test_NextLineHead );

			Console.WriteLine( "test {0} - PrevLineHead()", ++testNum );
			TestUtl.Do( Test_PrevLineHead );

			Console.WriteLine( "test {0} - GetLineLengthByCharIndex()", ++testNum );
			TestUtl.Do( Test_GetLineLengthByCharIndex );

			Console.WriteLine( "test {0} - GetLineRange()", ++testNum );
			TestUtl.Do( Test_GetLineRange );

			Console.WriteLine( "test {0} - GetCharIndex()", ++testNum );
			TestUtl.Do( Test_GetCharIndex );

			Console.WriteLine( "test {0} - GetLineIndexFromCharIndex()", ++testNum );
			TestUtl.Do( Test_GetLineIndexFromCharIndex );

			Console.WriteLine( "test {0} - GetTextPosition()", ++testNum );
			TestUtl.Do( Test_GetTextPosition );

			Console.WriteLine( "test {0} - LineHeadIndexFromCharIndex()", ++testNum );
			TestUtl.Do( Test_LineHeadIndexFromCharIndex );

			Console.WriteLine( "done." );
			Console.WriteLine();
		}

		static void Test_GetCharIndex()
		{
			TextBuffer text;
			GapBuffer<int> lhi;
			GapBuffer<LineDirtyState> lds;

			MakeTestData( out text, out lhi, out lds );

			TestUtl.AssertEquals(  0, TextUtil.GetCharIndex(text, lhi, new TextPoint(0,  0)) );
			TestUtl.AssertEquals( 34, TextUtil.GetCharIndex(text, lhi, new TextPoint(2,  1)) );
			TestUtl.AssertEquals( 71, TextUtil.GetCharIndex(text, lhi, new TextPoint(6, 18)) );

			try
			{
				TextUtil.GetCharIndex( text, lhi, new TextPoint(6, 19) );
				TestUtl.Fail( "exception must be thrown here." );
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>( ex );
			}

			try
			{
				TextUtil.GetCharIndex( text, lhi, new TextPoint(0, 100) );
				TestUtl.Fail( "exception must be thrown here." );
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>( ex );
			}
		}

		static void Test_NextLineHead()
		{
			var text = new TextBuffer( 1, 32 );

			text.Insert( 0, TestData );

			try
			{
				TextUtil.NextLineHead( text, -1 );
				TestUtl.Fail( "exception must be thrown here." );
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>( ex );
			}

			int i = 0;
			for( ; i<32; i++ )
				TestUtl.AssertEquals( 32, TextUtil.NextLineHead(text, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 33, TextUtil.NextLineHead(text, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 37, TextUtil.NextLineHead(text, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 38, TextUtil.NextLineHead(text, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 52, TextUtil.NextLineHead(text, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 53, TextUtil.NextLineHead(text, i) );
			for( ; i<71; i++ )
				TestUtl.AssertEquals( -1, TextUtil.NextLineHead(text, i) );
			TestUtl.AssertEquals( -1, TextUtil.NextLineHead(text, i) );
		}

		static void Test_PrevLineHead()
		{
			var text = new TextBuffer( 1, 32 );

			text.Insert( 0, TestData );

			int i=71;
			for( ; 53<=i; i-- )
				TestUtl.AssertEquals( 53, TextUtil.PrevLineHead(text, i) );
			for( ; 52<=i; i-- )
				TestUtl.AssertEquals( 52, TextUtil.PrevLineHead(text, i) );
			for( ; 38<=i; i-- )
				TestUtl.AssertEquals( 38, TextUtil.PrevLineHead(text, i) );
			for( ; 37<=i; i-- )
				TestUtl.AssertEquals( 37, TextUtil.PrevLineHead(text, i) );
			for( ; 33<=i; i-- )
				TestUtl.AssertEquals( 33, TextUtil.PrevLineHead(text, i) );
			for( ; 32<=i; i-- )
				TestUtl.AssertEquals( 32, TextUtil.PrevLineHead(text, i) );
			for( ; 0<=i; i-- )
				TestUtl.AssertEquals( 0,  TextUtil.PrevLineHead(text, i) );
		}

		static void Test_GetLineLengthByCharIndex()
		{
			var text = new TextBuffer( 1, 32 );
			int i = 0;

			text.Insert( 0, TestData );

			for( ; i<32; i++ )
				TestUtl.AssertEquals( 32, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals(  4, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 14, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<71; i++ )
				TestUtl.AssertEquals( 17, TextUtil.GetLineLengthByCharIndex(text, i) );
			TestUtl.AssertEquals( 17, TextUtil.GetLineLengthByCharIndex(text, i) ); // EOF
		}

		static void Test_GetLineRange()
		{
			Range range;
			TextBuffer text;
			GapBuffer<int> lhi;
			GapBuffer<LineDirtyState> lds;

			MakeTestData( out text, out lhi, out lds );

			range = TextUtil.GetLineRange( text, lhi, 0, true );
			TestUtl.AssertEquals( 0, range.Begin );
			TestUtl.AssertEquals( 32, range.End );
			range = TextUtil.GetLineRange( text, lhi, 1, true );
			TestUtl.AssertEquals( 32, range.Begin );
			TestUtl.AssertEquals( 33, range.End );
			range = TextUtil.GetLineRange( text, lhi, 2, true );
			TestUtl.AssertEquals( 33, range.Begin );
			TestUtl.AssertEquals( 37, range.End );
			range = TextUtil.GetLineRange( text, lhi, 3, true );
			TestUtl.AssertEquals( 37, range.Begin );
			TestUtl.AssertEquals( 38, range.End );
			range = TextUtil.GetLineRange( text, lhi, 4, true );
			TestUtl.AssertEquals( 38, range.Begin );
			TestUtl.AssertEquals( 52, range.End );
			range = TextUtil.GetLineRange( text, lhi, 5, true );
			TestUtl.AssertEquals( 52, range.Begin );
			TestUtl.AssertEquals( 53, range.End );
			range = TextUtil.GetLineRange( text, lhi, 6, true );
			TestUtl.AssertEquals( 53, range.Begin );
			TestUtl.AssertEquals( 71, range.End );

			range = TextUtil.GetLineRange( text, lhi, 0, false );
			TestUtl.AssertEquals( 0, range.Begin );
			TestUtl.AssertEquals( 30, range.End );
			range = TextUtil.GetLineRange( text, lhi, 1, false );
			TestUtl.AssertEquals( 32, range.Begin );
			TestUtl.AssertEquals( 32, range.End );
			range = TextUtil.GetLineRange( text, lhi, 2, false );
			TestUtl.AssertEquals( 33, range.Begin );
			TestUtl.AssertEquals( 36, range.End );
			range = TextUtil.GetLineRange( text, lhi, 3, false );
			TestUtl.AssertEquals( 37, range.Begin );
			TestUtl.AssertEquals( 37, range.End );
			range = TextUtil.GetLineRange( text, lhi, 4, false );
			TestUtl.AssertEquals( 38, range.Begin );
			TestUtl.AssertEquals( 51, range.End );
			range = TextUtil.GetLineRange( text, lhi, 5, false );
			TestUtl.AssertEquals( 52, range.Begin );
			TestUtl.AssertEquals( 52, range.End );
			range = TextUtil.GetLineRange( text, lhi, 6, false );
			TestUtl.AssertEquals( 53, range.Begin );
			TestUtl.AssertEquals( 71, range.End );
		}

		static void Test_GetLineIndexFromCharIndex()
		{
			var lhi = new GapBuffer<int>( 32, 32 );
			lhi.Add( 0 );
			lhi.Add( 32 );
			lhi.Add( 33 );
			lhi.Add( 37 );
			lhi.Add( 38 );
			lhi.Add( 52 );
			lhi.Add( 53 );

			int i=0;
			for( ; i<32; i++ )
				TestUtl.AssertEquals( 0, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 1, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 2, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 3, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 4, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 5, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			TestUtl.AssertEquals( 6, TextUtil.GetLineIndexFromCharIndex(lhi, 54) );
		}

		static void Test_GetTextPosition()
		{
			TextBuffer text;
			GapBuffer<int> lhi;
			TextPoint pos;

			MakeTestData( out text, out lhi );

			pos = TextUtil.GetTextPosition( text, lhi, 0 );
			TestUtl.AssertEquals( 0, pos.Line );
			TestUtl.AssertEquals( 0, pos.Column );
			pos = TextUtil.GetTextPosition( text, lhi, 2 );
			TestUtl.AssertEquals( 0, pos.Line );
			TestUtl.AssertEquals( 2, pos.Column );
			pos = TextUtil.GetTextPosition( text, lhi, 40 );
			TestUtl.AssertEquals( 4, pos.Line );
			TestUtl.AssertEquals( 2, pos.Column );
			pos = TextUtil.GetTextPosition( text, lhi, 71 ); // 71 --> EOF
			TestUtl.AssertEquals( 6, pos.Line );
			TestUtl.AssertEquals( 18, pos.Column );
			try
			{
				TextUtil.GetTextPosition(text, lhi, 72);
				TestUtl.Fail("exception must be thrown here.");
			}
			catch( Exception ex )
			{
				TestUtl.AssertType<AssertException>(ex);
			}
		}

		static void Test_LineHeadIndexFromCharIndex()
		{
			TextBuffer text;
			GapBuffer<int> lhi;

			MakeTestData( out text, out lhi );

			int i=0;
			for( ; i<32; i++ )
				TestUtl.AssertEquals(  0, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<33; i++ )
				TestUtl.AssertEquals( 32, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<37; i++ )
				TestUtl.AssertEquals( 33, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<38; i++ )
				TestUtl.AssertEquals( 37, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<52; i++ )
				TestUtl.AssertEquals( 38, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<53; i++ )
				TestUtl.AssertEquals( 52, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<=71; i++ )
				TestUtl.AssertEquals( 53, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			try{ TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i); TestUtl.Fail("exception must be thrown here."); }
			catch( Exception ex ){ TestUtl.AssertType<AssertException>(ex); }
		}

		static void MakeTestData( out TextBuffer text, out GapBuffer<int> lhi )
		{
			var lds = new GapBuffer<LineDirtyState>( 8 );
			MakeTestData( out text, out lhi, out lds );
		}

		static void MakeTestData( out TextBuffer text, out GapBuffer<int> lhi, out GapBuffer<LineDirtyState> lds )
		{
			text = new TextBuffer( 1, 1 );
			lhi = new GapBuffer<int>( 1, 8 );
			lds = new GapBuffer<LineDirtyState>( 1 );

			lhi.Add( 0 );
			lds.Add( LineDirtyState.Clean );

			TextUtil.LHI_Insert( lhi, lds, text, TestData.ToCharArray(), 0 );
			text.Insert( 0, TestData );
		}
	}
}
#endif
