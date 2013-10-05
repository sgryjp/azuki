using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// Basic range.
	/// </summary>
	public class Range : IRange
	{
		readonly protected TextBuffer _Buffer;
		protected DateTime _CacheTimestamp = DateTime.MinValue;
		protected string _CachedText;

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

		/// <exception cref="InvalidOperationException"/>
		public virtual string Text
		{
			get
			{
				if( _Buffer == null )
					throw new InvalidOperationException( "A range associated with no text buffer"
														 + " cannot extract a substring." );

				if( _CacheTimestamp < _Buffer.LastModifiedTime )
				{
					_CachedText = _Buffer.GetText( this );
					_CacheTimestamp = _Buffer.LastModifiedTime;
				}

				return _CachedText;
			}
		}

		public virtual bool IsEmpty
		{
			get{ return (Begin == End); }
		}

		/// <exception cref="ArgumentNullException"/>
		public Range Intersect( IRange another )
		{
			if( another == null )
				throw new ArgumentNullException( "another" );

			return Intersect( this, another );
		}

		/// <exception cref="ArgumentNullException"/>
		public static Range Intersect( IRange x, IRange y )
		{
			if( x == null )
				throw new ArgumentNullException( "x" );
			if( y == null )
				throw new ArgumentNullException( "y" );

			var begin = Math.Max( x.Begin, y.Begin );
			var end = Math.Min( x.End, y.End );
			return (begin <= end) ? new Range( begin, end )
								  : Range.Empty;
		}

		/// <exception cref="ArgumentOutOfRangeException"/>
		public CharData this[ int index ]
		{
			get
			{
				if( index < 0 || _Buffer.Count <= Begin + index )
					throw new ArgumentOutOfRangeException();

				return new CharData( _Buffer, Begin + index );
			}
		}

		/// <exception cref="InvalidOperationException"/>
		public IEnumerable<CharData> Chars
		{
			get
			{
				if( _Buffer == null )
					throw new InvalidOperationException();

				return new CharDataList( _Buffer, this );
			}
		}

		/// <exception cref="InvalidOperationException"/>
		public IEnumerable<CharData> RawChars
		{
			get
			{
				if( _Buffer == null )
					throw new InvalidOperationException();

				return new RawCharDataList( _Buffer, this );
			}
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
