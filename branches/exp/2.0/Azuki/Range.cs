// file: Range.cs
// brief: Range to describe where a portion of text begins from and where it ends
//=========================================================
using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range to describe where a portion of text begins from and where it ends
	/// </summary>
	public class Range
	{
		public Range()
		{}

		public Range( int begin, int end )
		{
			Begin = begin;
			End = end;
		}

		public int Begin
		{
			get; set;
		}

		public int End
		{
			get; set;
		}

		public int Length
		{
			get{ return Math.Abs(End - Begin); }
		}

		public bool IsEmpty
		{
			get{ return (Begin == End); }
		}

		public override string ToString()
		{
			return String.Format( "[{0}, {1})", Begin, End );
		}
	}
}
