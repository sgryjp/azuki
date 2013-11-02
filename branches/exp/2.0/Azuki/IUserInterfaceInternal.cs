using System.Drawing;

namespace Sgry.Azuki
{
	interface IUserInterfaceInternal : IUserInterface
	{
		IGraphics GetIGraphics();

		void UpdateCaretGraphic();
		void UpdateCaretGraphic( Rectangle caretRect );
		void SetCursorGraphic( MouseCursor cursorType );

		void RescheduleHighlighting();

		void UpdateScrollBarRange();

		void InvokeCaretMoved();
		void InvokeOverwriteModeChanged();
		bool InvokeLineDrawing( IGraphics g, int lineIndex, Point pos );
		bool InvokeLineDrawn( IGraphics g, int lineIndex, Point pos );
		void InvokeVScroll();
		void InvokeHScroll();

		bool Focused{ get; }
	}
}
