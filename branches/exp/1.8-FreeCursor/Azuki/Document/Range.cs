using System;

namespace Sgry.Azuki
{
	public class Range
	{
		int _Begin;
		int _End;
		TextDataType _Type;

		public Range()
			: this( 0, 0, TextDataType.Normal )
		{}

		public Range( int begin, int end )
			: this( begin, end, TextDataType.Normal )
		{}

		public Range( int begin, int end, TextDataType type )
		{
			_Begin = begin;
			_End = end;
			_Type = type;
		}

		public int Begin
		{
			get{ return _Begin; }
		}

		public int End
		{
			get{ return _End; }
		}

		public TextDataType Type
		{
			get{ return _Type; }
		}

		public bool IsEmpty
		{
			get{ return (_Begin == _End); }
		}

		[Obsolete("This should not be like this... Range.Length for Rectangle 'range' is impossible define.")]
		public int Length
		{
			get
			{
				if( _Type == TextDataType.Rectangle )
					throw new InvalidOperationException();

				return _End - _Begin;
			}
		}
	}
}
