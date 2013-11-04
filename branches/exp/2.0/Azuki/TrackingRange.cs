using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Represents a range of substring in a text buffer which tracks every modifications on the
	/// buffer and update the range automatically.
	/// </summary>
	public class TrackingRange : Range
	{
		#region Init / Dispose
		/// <summary>
		/// (Use TextBuffer.CreateTrackingRange to create an instance of TrackingRange.)
		/// </summary>
		/// <exception cref="ArgumentNullException">Parameter <paramref name="buf"/> was null.
		/// </exception>
		internal TrackingRange( Document doc, int begin, int end, BoundaryTrackingMode mode )
			: base( doc, begin, end )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );

			BoundaryTrackingMode = mode;
		}

		/// <summary>
		/// Creates a cloned copy of this TrackingRange object.
		/// </summary>
		public override IRange Clone()
		{
			return new TrackingRange( Document, Begin, End, BoundaryTrackingMode );
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
	}
}
