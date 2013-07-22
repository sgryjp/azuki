using System;
using System.Collections.Generic;
using System.Text;

namespace Sgry.Azuki
{
	internal class CrlfLineManager : LineManagerBase
	{
		public CrlfLineManager( TextBuffer buf )
			: base( buf )
		{}

		public override Range GetLineRange( int lineIndex, bool printableOnly )
		{
			// Check whether

			// Calculate where the line starts if we don't know yet
			if( _Lhi.Count < lineIndex )
			{

			}

			// Use already calculated value

			return null;
		}
	}
}
