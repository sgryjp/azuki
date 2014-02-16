using System.Collections.Generic;
using NUnit.Framework;
using Sgry.Azuki.Utils;

namespace Sgry.Azuki.Test
{
	[TestFixture]
	public class TextUtilTest
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

		[Test]
		public void GetCharIndex()
		{
			IList<char> text;
			GapBuffer<int> lhi;
			MakeTestData( out text, out lhi );

			Assert.AreEqual(  0, TextUtil.GetCharIndex(text, lhi, new LineColumnPos(0,  0)) );
			Assert.AreEqual( 34, TextUtil.GetCharIndex(text, lhi, new LineColumnPos(2,  1)) );
			Assert.AreEqual( 71, TextUtil.GetCharIndex(text, lhi, new LineColumnPos(6, 18)) );
			Assert.AreEqual( 71, TextUtil.GetCharIndex(text, lhi, new LineColumnPos(6, 19)) );
			Assert.AreEqual( 32, TextUtil.GetCharIndex(text, lhi, new LineColumnPos(0,100)) );
		}

		[Test]
		public void NextLineHead()
		{
			IList<char> text;
			GapBuffer<int> lhi;
			MakeTestData( out text, out lhi );

			Assert.Throws<AssertException>( delegate{
				TextUtil.NextLineHead( text, -1 );
			} );

			int i = 0;
			for( ; i<32; i++ )
				Assert.AreEqual( 32, TextUtil.NextLineHead(text, i) );
			for( ; i<33; i++ )
				Assert.AreEqual( 33, TextUtil.NextLineHead(text, i) );
			for( ; i<37; i++ )
				Assert.AreEqual( 37, TextUtil.NextLineHead(text, i) );
			for( ; i<38; i++ )
				Assert.AreEqual( 38, TextUtil.NextLineHead(text, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 52, TextUtil.NextLineHead(text, i) );
			for( ; i<53; i++ )
				Assert.AreEqual( 53, TextUtil.NextLineHead(text, i) );
			for( ; i<71; i++ )
				Assert.AreEqual( -1, TextUtil.NextLineHead(text, i) );
			Assert.AreEqual( -1, TextUtil.NextLineHead(text, i) );
		}

		[Test]
		public void PrevLineHead()
		{
			IList<char> text;
			GapBuffer<int> lhi;
			MakeTestData( out text, out lhi );

			int i=71;
			for( ; 53<=i; i-- )
				Assert.AreEqual( 53, TextUtil.PrevLineHead(text, i) );
			for( ; 52<=i; i-- )
				Assert.AreEqual( 52, TextUtil.PrevLineHead(text, i) );
			for( ; 38<=i; i-- )
				Assert.AreEqual( 38, TextUtil.PrevLineHead(text, i) );
			for( ; 37<=i; i-- )
				Assert.AreEqual( 37, TextUtil.PrevLineHead(text, i) );
			for( ; 33<=i; i-- )
				Assert.AreEqual( 33, TextUtil.PrevLineHead(text, i) );
			for( ; 32<=i; i-- )
				Assert.AreEqual( 32, TextUtil.PrevLineHead(text, i) );
			for( ; 0<=i; i-- )
				Assert.AreEqual( 0,  TextUtil.PrevLineHead(text, i) );
		}

		[Test]
		public void GetLineLengthByCharIndex()
		{
			IList<char> text;
			GapBuffer<int> lhi;
			MakeTestData( out text, out lhi );

			int i = 0;
			for( ; i<32; i++ )
				Assert.AreEqual( 32, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<33; i++ )
				Assert.AreEqual(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<37; i++ )
				Assert.AreEqual(  4, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<38; i++ )
				Assert.AreEqual(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 14, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<53; i++ )
				Assert.AreEqual(  1, TextUtil.GetLineLengthByCharIndex(text, i) );
			for( ; i<71; i++ )
				Assert.AreEqual( 17, TextUtil.GetLineLengthByCharIndex(text, i) );
			Assert.AreEqual( 17, TextUtil.GetLineLengthByCharIndex(text, i) ); // EOF
		}

		[Test]
		public void GetLineRange()
		{
			IRange range;
			IList<char> text;
			GapBuffer<int> lhi;
			MakeTestData( out text, out lhi );

			range = TextUtil.GetLineRange( text, lhi, 0, true );
			Assert.AreEqual( 0, range.Begin );
			Assert.AreEqual( 32, range.End );
			range = TextUtil.GetLineRange( text, lhi, 1, true );
			Assert.AreEqual( 32, range.Begin );
			Assert.AreEqual( 33, range.End );
			range = TextUtil.GetLineRange( text, lhi, 2, true );
			Assert.AreEqual( 33, range.Begin );
			Assert.AreEqual( 37, range.End );
			range = TextUtil.GetLineRange( text, lhi, 3, true );
			Assert.AreEqual( 37, range.Begin );
			Assert.AreEqual( 38, range.End );
			range = TextUtil.GetLineRange( text, lhi, 4, true );
			Assert.AreEqual( 38, range.Begin );
			Assert.AreEqual( 52, range.End );
			range = TextUtil.GetLineRange( text, lhi, 5, true );
			Assert.AreEqual( 52, range.Begin );
			Assert.AreEqual( 53, range.End );
			range = TextUtil.GetLineRange( text, lhi, 6, true );
			Assert.AreEqual( 53, range.Begin );
			Assert.AreEqual( 71, range.End );

			range = TextUtil.GetLineRange( text, lhi, 0, false );
			Assert.AreEqual( 0, range.Begin );
			Assert.AreEqual( 30, range.End );
			range = TextUtil.GetLineRange( text, lhi, 1, false );
			Assert.AreEqual( 32, range.Begin );
			Assert.AreEqual( 32, range.End );
			range = TextUtil.GetLineRange( text, lhi, 2, false );
			Assert.AreEqual( 33, range.Begin );
			Assert.AreEqual( 36, range.End );
			range = TextUtil.GetLineRange( text, lhi, 3, false );
			Assert.AreEqual( 37, range.Begin );
			Assert.AreEqual( 37, range.End );
			range = TextUtil.GetLineRange( text, lhi, 4, false );
			Assert.AreEqual( 38, range.Begin );
			Assert.AreEqual( 51, range.End );
			range = TextUtil.GetLineRange( text, lhi, 5, false );
			Assert.AreEqual( 52, range.Begin );
			Assert.AreEqual( 52, range.End );
			range = TextUtil.GetLineRange( text, lhi, 6, false );
			Assert.AreEqual( 53, range.Begin );
			Assert.AreEqual( 71, range.End );
		}

		[Test]
		public void GetLineIndexFromCharIndex()
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
				Assert.AreEqual( 0, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<33; i++ )
				Assert.AreEqual( 1, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<37; i++ )
				Assert.AreEqual( 2, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<38; i++ )
				Assert.AreEqual( 3, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 4, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			for( ; i<53; i++ )
				Assert.AreEqual( 5, TextUtil.GetLineIndexFromCharIndex(lhi, i) );
			Assert.AreEqual( 6, TextUtil.GetLineIndexFromCharIndex(lhi, 54) );
		}

		[Test]
		public void GetLineColumnPos()
		{
			LineColumnPos pos;
			IList<char> text;
			GapBuffer<int> lhi;
			MakeTestData( out text, out lhi );

			pos = TextUtil.GetLineColumnPos( text, lhi, 0 );
			Assert.AreEqual( 0, pos.Line );
			Assert.AreEqual( 0, pos.Column );
			pos = TextUtil.GetLineColumnPos( text, lhi, 2 );
			Assert.AreEqual( 0, pos.Line );
			Assert.AreEqual( 2, pos.Column );
			pos = TextUtil.GetLineColumnPos( text, lhi, 40 );
			Assert.AreEqual( 4, pos.Line );
			Assert.AreEqual( 2, pos.Column );
			pos = TextUtil.GetLineColumnPos( text, lhi, 71 ); // 71 --> EOF
			Assert.AreEqual( 6, pos.Line );
			Assert.AreEqual( 18, pos.Column );
			Assert.Throws<AssertException>( delegate{
				TextUtil.GetLineColumnPos( text, lhi, 72 );
			} );
		}

		[Test]
		public void LineHeadIndexFromCharIndex()
		{
			IList<char> text;
			GapBuffer<int> lhi;
			MakeTestData( out text, out lhi );

			int i=0;
			for( ; i<32; i++ )
				Assert.AreEqual(  0, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<33; i++ )
				Assert.AreEqual( 32, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<37; i++ )
				Assert.AreEqual( 33, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<38; i++ )
				Assert.AreEqual( 37, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<52; i++ )
				Assert.AreEqual( 38, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<53; i++ )
				Assert.AreEqual( 52, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			for( ; i<=71; i++ )
				Assert.AreEqual( 53, TextUtil.GetLineHeadIndexFromCharIndex(text, lhi, i) );
			Assert.Throws<AssertException>( delegate{
				TextUtil.GetLineHeadIndexFromCharIndex( text, lhi, i );
			} );
		}

		#region Utilities
		void MakeTestData( out IList<char> text,
						   out GapBuffer<int> lhi )
		{
			var lds = new GapBuffer<DirtyState>( 8 );
			MakeTestData( out text, out lhi, out lds );
		}

		void MakeTestData( out IList<char> text,
						   out GapBuffer<int> lhi,
						   out GapBuffer<DirtyState> lds )
		{
			text = new List<char>();
			lhi = new GapBuffer<int>( 32 );
			lds = new GapBuffer<DirtyState>( 32 );

			lhi.Add( 0 );
			lds.Add( DirtyState.Clean );

			TextUtil.LHI_Insert( lhi, lds, text, TestData.ToCharArray(), 0 );
			foreach( var ch in TestData )
				text.Add( ch );
		}
		#endregion
	}
}
