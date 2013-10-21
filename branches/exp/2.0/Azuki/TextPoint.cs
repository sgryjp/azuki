using System;

namespace Sgry.Azuki
{
	public struct TextPoint
	{
		#region Init / Dispose
		/// <summary>
		/// Creates a new instance.
		/// </summary>
		public TextPoint( int line, int column )
			: this()
		{
			Line = line;
			Column = column;
		}
		#endregion

		#region Properties
		public int Line
		{
			get; set;
		}

		public int Column
		{
			get; set;
		}
		#endregion

		#region Behavior as an object
		public override string ToString()
		{
			return String.Format( "{0}_{1}", Line, Column );
		}
		#endregion
	}
}
