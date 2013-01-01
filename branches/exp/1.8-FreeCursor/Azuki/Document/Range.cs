using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	public class Range
	{
		int _Begin;
		int _End;
		bool _Reversed;

		public Range( Range range )
			: this( range.From, range.To )
		{}

		public Range( int from, int to )
		{
			if( from < to )
			{
				_Begin = from;
				_End = to;
				_Reversed = false;
			}
			else
			{
				_Begin = to;
				_End = from;
				_Reversed = true;
			}
		}

		public int Begin
		{
			get{ return _Begin; }
			set{ _Begin = value; }
		}

		public int End
		{
			get{ return _End; }
			set{ _End = value; }
		}

		public int From
		{
			get{ return _Reversed ? _End : _Begin; }
			set
			{
				if( _Reversed )
					_End = value;
				else
					_Begin = value;
			}
		}

		public int To
		{
			get{ return _Reversed ? _Begin : _End; }
			set
			{
				if( _Reversed )
					_Begin = value;
				else
					_End = value;
			}
		}

		public bool IsEmpty
		{
			get{ return (_Begin == _End); }
		}

		public bool Includes( int index )
		{
			return (_Begin <= index && index < _End);
		}

		public int Length
		{
			get{ return _End - _Begin; }
		}
	}
}
