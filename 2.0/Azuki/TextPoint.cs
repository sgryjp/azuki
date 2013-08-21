using System;

namespace Sgry.Azuki
{
	public struct TextPoint
	{
		public int Line
		{
			get; set;
		}

		public int Column
		{
			get; set;
		}

		public TextPoint( int line, int column )
			: this()
		{
			Line = line;
			Column = column;
		}

		public override string ToString()
		{
			return String.Format( "{0}_{1}", Line, Column );
		}
	}
}
