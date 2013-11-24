using System;

namespace Sgry.Azuki
{
	class UndoContext : IDisposable
	{
		readonly Document _Document;

		public UndoContext( Document doc )
		{
			_Document = doc;
		}

		public void Dispose()
		{
			_Document.EndUndo();
		}
	}
}
