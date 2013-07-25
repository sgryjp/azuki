using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range to describe where a portion of text begins from and where it ends
	/// </summary>
	public struct Range : IRange
	{
		readonly TextBuffer _Buffer;

		public Range( int begin, int end )
			: this(null, begin, end)
		{}

		internal Range( TextBuffer buf, int begin, int end )
			: this()
		{
			_Buffer = buf;
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

		public string Text
		{
			get
			{
				if( _Buffer == null )
					throw new InvalidOperationException( "A range associated with no text buffer"
														 + " cannot extract a substring." );

				return _Buffer.GetText(this);
			}
		}

		public bool IsEmpty
		{
			get{ return (Begin == End); }
		}

		public Range Intersect( IRange another )
		{
			return Intersect( this, another );
		}

		public static Range Intersect( IRange x, IRange y )
		{
			var begin = Math.Max( x.Begin, y.Begin );
			var end = Math.Min( x.End, y.End );
			return (begin <= end) ? new Range( begin, end )
								  : Range.Empty;
		}

		public override string ToString()
		{
			return String.Format( "[{0}, {1})", Begin, End );
		}

		public override bool Equals( object obj )
		{
			var another = obj as IRange;
			return (another != null) && (another.Begin == Begin && another.End == End);
		}

		public override int GetHashCode()
		{
			return Begin + (End << 5);
		}

		public static Range Empty
		{
			get{ return new Range(0, 0); }
		}
	}
}
