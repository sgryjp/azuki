using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range to describe where a portion of text begins from and where it ends
	/// </summary>
	public class Range : IRange
	{
		readonly protected TextBuffer _Buffer;

		public Range()
			: this(null, 0, 0)
		{}

		public Range( int begin, int end )
			: this(null, begin, end)
		{}

		internal Range( TextBuffer buf, int begin, int end )
		{
			_Buffer = buf;
			Begin = begin;
			End = end;
		}

		internal TextBuffer TextBuffer
		{
			get{ return _Buffer; }
		}

		public virtual int Begin
		{
			get; set;
		}

		public virtual int End
		{
			get; set;
		}

		public virtual int Length
		{
			get{ return Math.Abs(End - Begin); }
		}

		public virtual string Text
		{
			get
			{
				if( _Buffer == null )
					throw new InvalidOperationException( "A range associated with no text buffer"
														 + " cannot extract a substring." );

				return _Buffer.GetText(this);
			}
		}

		public virtual bool IsEmpty
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

			if( another is Range
				&& TextBuffer != (another as Range).TextBuffer )
				return false;

			return (another != null) && (another.Begin == Begin && another.End == End);
		}

		public override int GetHashCode()
		{
			var codeOfBuf = (_Buffer != null) ? _Buffer.GetHashCode() : 0;
			return codeOfBuf + Begin + (End << 5);
		}

		public static Range Empty
		{
			get{ return new Range(0, 0); }
		}
	}
}
