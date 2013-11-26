using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	internal class RawLineRangeList : LineRangeList
	{
		internal RawLineRangeList( Document doc )
			: base( doc )
		{}

		public override ILineRange this[ int lineIndex ]
		{
			get
			{
				if( lineIndex < 0 || _Document.Lines.Count < lineIndex )
					throw new ArgumentOutOfRangeException();

				var range = _Document.Buffer.GetLineRange( lineIndex, true );
				Debug.Assert( range.End == _Document.Length
							  || (0 < range.End && _Document[range.End-1] == '\r')
							  || (0 < range.End && _Document[range.End-1] == '\n') );
				return new RawLineRange( _Document, range.Begin, range.End, lineIndex );
			}
		}
	}
}
