﻿using System.Drawing;

namespace Sgry.Azuki
{
	internal interface IViewInternal : IView
	{
		bool IsLineHead( int index );

		int XofLineNumberArea{ get; }
		int XofDirtBar{ get; }
		int XofLeftMargin{ get; }
		int XofTextArea{ get; }

		int YofHRuler{ get; }
		int YofTopMargin{ get; }
		int YofTextArea{ get; }

		Rectangle DirtBarRectangle{ get; }
		Rectangle LineNumberAreaRectangle{ get; }
		Rectangle HRulerRectangle{ get; }
		Rectangle TextAreaRectangle{ get; }
	}
}
