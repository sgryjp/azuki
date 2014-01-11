using System;

namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line, including its EOL code.
	/// </summary>
	internal class RawLineRange : LineRange
	{
		public RawLineRange( Document doc, int begin, int end, int lineIndex )
			: base( doc, begin, end, lineIndex )
		{}

		/// <summary>
		/// Gets EOL code which terminates this line.
		/// </summary>
		public override string EolCode
		{
			get
			{
				if( 0 < End && Document[End-1] == '\n' )
				{
					if( 1 < End && Document[End-2] == '\r' )
						return "\r\n";
					else
						return "\n";
				}
				else if( 0 < End && Document[End-1] == '\r' )
				{
					return "\r";
				}
				return String.Empty;
			}
		}
	}
}
