using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line, including its EOL code.
	/// </summary>
	internal class RawLineRange : LineRange
	{
		public RawLineRange( TextBuffer buf, int begin, int end, int lineIndex )
			: base( buf, begin, end, lineIndex )
		{}

		/// <summary>
		/// Gets content of this line, with EOL code.
		/// </summary>
		public override string Text
		{
			get{ return base.Text; } // Just to change documentation comment...
		}

		/// <summary>
		/// Gets EOL code which terminates this line.
		/// </summary>
		public override string EolCode
		{
			get
			{
				if( 0 < End && _Buffer[End-1] == '\n' )
				{
					if( 1 < End && _Buffer[End-2] == '\r' )
						return "\r\n";
					else
						return "\n";
				}
				else if( 0 < End && _Buffer[End-1] == '\r' )
				{
					return "\r";
				}
				return String.Empty;
			}
		}
	}
}
