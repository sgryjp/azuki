using System;
using System.Collections.Generic;
using System.Text;

namespace Sgry.Azuki
{
	public class Selections
	{
		List<Range> _Ranges = new List<Range>();
		int _LastRangeIndex;

		public Selections()
		{
			Set( new Range(0, 0) );
		}

		public Selections( IEnumerable<Range> ranges, int lastRangeIndex )
		{
			Set( ranges, lastRangeIndex );
		}

		public Selections Clone()
		{
			return new Selections( _Ranges, _LastRangeIndex );
		}

		public List<Range> Ranges
		{
			get{ return _Ranges; }
		}

		public Range LastRange
		{
			get
			{
				int index = Math.Min( _Ranges.Count - 1,
									  _LastRangeIndex );
				return _Ranges[index];
			}
		}

		public int LastRangeIndex
		{
			get{ return _LastRangeIndex; }
			set{ _LastRangeIndex = value; }
		}

		public Range FirstRange
		{
			get
			{
				int index = (_LastRangeIndex != 0) ? 0
												  : _Ranges.Count - 1;
				return _Ranges[index];
			}
		}

		public int Anchor
		{
			get{ return FirstRange.From; }
		}

		public int Caret
		{
			get{ return LastRange.To; }
		}

		public void Clear()
		{
			int caret = LastRange.To;
			_Ranges.Clear();
			_Ranges.Add( new Range(caret, caret) );
			_LastRangeIndex = 0;
		}

		public void Set( Range range )
		{
			if( range == null )
				throw new ArgumentNullException( "range" );

			_Ranges.Clear();
			_Ranges.Add( range );
			_LastRangeIndex = 0;
		}

		public void Set( IEnumerable<Range> ranges, int lastRangeIndex )
		{
			if( ranges == null )
				throw new ArgumentNullException( "ranges" );

			_Ranges.Clear();
			_Ranges.AddRange( ranges );
			_LastRangeIndex = lastRangeIndex;

			if( _Ranges.Count < 1 )
				throw new ArgumentException( "ranges" );
			if( LastRangeIndex < 0 || _Ranges.Count <= LastRangeIndex )
				throw new ArgumentOutOfRangeException( "lastRangeIndex" );
		}
	}
}
