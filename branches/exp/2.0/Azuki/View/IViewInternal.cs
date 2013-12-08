namespace Sgry.Azuki
{
	internal interface IViewInternal : IView
	{
		int ScrXofLineNumberArea{ get; }
		int ScrXofDirtBar{ get; }
		int ScrXofLeftMargin{ get; }
		int ScrXofTextArea{ get; }
		int ScrYofHRuler{ get; }
		int ScrYofTopMargin{ get; }
		int ScrYofTextArea{ get; }

		bool IsLineHead( int index );
		int NextTabStopX( int virX );
	}
}
