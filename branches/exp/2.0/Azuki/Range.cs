using System;
using System.Collections.Generic;

namespace Sgry.Azuki
{
	/// <summary>
	/// Basic implementation of IRange.
	/// </summary>
	/// <remarks>
	///   <para>
	///   Note that Range is not a struct but a class so copying a variable of this type does not
	///   copy its values. To copy values, use <see cref="Range.Clone"/> method.
	///   </para>
	/// </remarks>
	public class Range : IRange
	{
		readonly Document _Document;
		protected DateTime _CacheTimestamp = DateTime.MinValue;
		protected string _CachedText;
		AutoUpdateMode _AutoUpdateMode;

		#region Init / Dispose
		/// <summary>
		/// Creates an empty range.
		/// </summary>
		public Range()
			: this(null, 0, 0)
		{}

		/// <summary>
		/// Creates an object describing a specific range.
		/// </summary>
		/// <param name="begin">The index of starting position of the range.</param>
		/// <param name="end">The index of ending position of the range.</param>
		public Range( int begin, int end )
			: this(null, begin, end)
		{}

		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		internal Range( Document doc, int begin, int end )
		{
			if( begin < 0 )
				throw new ArgumentOutOfRangeException( "begin" );
			if( end < 0 )
				throw new ArgumentOutOfRangeException( "end" );
			if( end < begin )
				throw new ArgumentException();

			_Document = doc;
			Begin = begin;
			End = end;
		}

		~Range()
		{
			if( _Document != null )
				AutoUpdateMode = AutoUpdateMode.None;
		}

		/// <summary>
		/// Creates a cloned copy of this Range object.
		/// </summary>
		public virtual IRange Clone()
		{
			return new Range( Document, Begin, End );
		}

		/// <summary>
		/// Creates a cloned copy of this object.
		/// </summary>
		object ICloneable.Clone()
		{
			return Clone();
		}
		#endregion

		public Document Document
		{
			get{ return _Document; }
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
				var doc = Document;
				if( doc == null )
					throw new InvalidOperationException( "No text buffer was associated with this"
														 + " range object." );

				if( _CacheTimestamp < doc.LastModifiedTime )
				{
					_CachedText = doc.GetText( this );
					_CacheTimestamp = doc.LastModifiedTime;
				}

				return _CachedText;
			}
		}

		public virtual bool IsEmpty
		{
			get{ return (Begin == End); }
		}

		/// <exception cref="InvalidOperationException"/>
		public AutoUpdateMode AutoUpdateMode
		{
			get{ return _AutoUpdateMode; }
			set
			{
				if( Document == null )
					throw new InvalidOperationException( "No text buffer was associated with this"
														 + " range object." );
				var buf = Document.Buffer;
				if( value == Azuki.AutoUpdateMode.None )
					buf.RemoveAutoUpdateTarget( this );
				else
					buf.AddAutoUpdateTarget( this );
				_AutoUpdateMode = value;
			}
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

		/// <exception cref="InvalidOperationException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		public CharData this[ int index ]
		{
			get
			{
				var doc = Document;
				if( doc == null )
					throw new InvalidOperationException( "No text buffer was associated with this"
														 + " range object." );
				if( index < 0 || doc.Length <= Begin + index )
					throw new ArgumentOutOfRangeException();

				return new CharData( doc.Buffer, Begin + index );
			}
		}

		/// <exception cref="InvalidOperationException"/>
		public IEnumerable<CharData> Chars
		{
			get
			{
				if( Document == null )
					throw new InvalidOperationException( "No text buffer was associated with this"
														 + " range object." );

				return new CharDataList( Document.Buffer, this );
			}
		}

		/// <exception cref="InvalidOperationException"/>
		public IEnumerable<CharData> RawChars
		{
			get
			{
				if( Document == null )
					throw new InvalidOperationException( "No text buffer was associated with this"
														 + " range object." );

				return new RawCharDataList( Document.Buffer, this );
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
				&& Document != (another as Range).Document )
				return false;

			return (another != null) && (another.Begin == Begin && another.End == End);
		}

		public override int GetHashCode()
		{
			var codeOfBuf = (_Document != null) ? _Document.GetHashCode() : 0;
			return codeOfBuf + Begin + (End << 5);
		}

		public static Range Empty
		{
			get{ return new Range(0, 0); }
		}
	}
}
