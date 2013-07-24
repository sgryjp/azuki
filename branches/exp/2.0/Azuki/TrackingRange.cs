using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range of substring in a text buffer which tracks every modifications on the
	/// buffer and update the range automatically.
	/// </summary>
	public class TrackingRange : IRange
	{
		readonly TextBuffer _Buffer;

		#region Init / Dispose
		/// <summary>
		/// (Use TextBuffer.CreateTrackingRange to create an instance of TrackingRange.)
		/// </summary>
		internal TrackingRange( TextBuffer buf, int begin, int end, BoundaryTrackingMode mode )
		{
			_Buffer = buf;
			Begin = begin;
			End = end;
			BoundaryTrackingMode = mode;
		}
		#endregion

		#region IRange
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

		public Range Intersect( IRange another )
		{
			return Range.Intersect( this, another );
		}
		#endregion

		public BoundaryTrackingMode BoundaryTrackingMode
		{
			get; set;
		}

		#region Event
		internal void OnContentChanged( object sender, ContentChangedEventArgs e )
		{
			// Insertion
			var diff = e.NewText.Length;
			if( e.Index < Begin )
			{
				Begin += diff;
				End += diff;
			}
			else if( e.Index == Begin )
			{
				Begin -= e.OldText.Length;
				if( ((int)BoundaryTrackingMode & 0x01) != 0 )
					Begin += e.NewText.Length;
				End += diff;
			}
			else if( End == e.Index )
			{
				if( ((int)BoundaryTrackingMode & 0x02) != 0 )
					End += e.NewText.Length;
			}

			// Removal
			var shift = Math.Max( 0,
								  Math.Min(e.Index + e.OldText.Length, Begin) - e.Index );
			var intersect = Intersect( new Range(e.Index,
												 e.Index + e.OldText.Length) );
			Begin -= shift;
			End -= shift + intersect.Length;
		}
		#endregion

		#region Utilities
		public override string ToString()
		{
			return String.Format( "[{0}, {1})", Begin, End );
		}

		public static explicit operator Range( TrackingRange r )
		{
			return new Range( r.Begin, r.End );
		}

		public override bool Equals(object obj)
		{
			var another = obj as IRange;
			return (another != null) && (another.Begin == Begin && another.End == End);
		}

		public override int GetHashCode()
		{
			return _Buffer.GetHashCode() + Begin + (End << 5);
		}
		#endregion
	}
}
