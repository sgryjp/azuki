using System;
using System.Diagnostics;

namespace Sgry.Azuki
{
	/// <summary>
	/// Range of a text line, excluding its EOL code.
	/// </summary>
	internal class LineRange : Range, ILineRange
	{
		#region Init / Dispose
		/// <exception cref="ArgumentException"/>
		/// <exception cref="ArgumentNullException"/>
		/// <exception cref="ArgumentOutOfRangeException"/>
		internal LineRange( Document doc, int begin, int end, int lineIndex )
			: base( doc, begin, end )
		{
			if( doc == null )
				throw new ArgumentNullException( "doc" );
			if( lineIndex < 0 )
				throw new ArgumentOutOfRangeException( "lineIndex", "Parameter 'lineIndex' must"
													  + " not be null." );
			if( doc.Lines.Count <= lineIndex )
				throw new ArgumentOutOfRangeException( "lineIndex", "Parameter 'lineIndex' was"
													   + " too large. (lineIndex:" + lineIndex
													   + ", LineCount:" + doc.Lines.Count + ")" );

			LineIndex = lineIndex;
		}

		/// <summary>
		/// Creates a cloned copy of this LineRange object.
		/// </summary>
		public override IRange Clone()
		{
			return new LineRange( Document, Begin, End, LineIndex );
		}
		#endregion

		public int LineIndex
		{
			get;
			private set;
		}

		public virtual string EolCode
		{
			get
			{
				Debug.Assert( Document != null );

				var buf = Document;
				var begin = End;
				var end = (LineIndex+1 < buf.Lines.Count) ? buf.Lines[LineIndex+1].Begin
														  : buf.Length;
				return buf.GetText( new Range(begin, end) );
			}
		}

		public DirtyState DirtyState
		{
			get
			{
				Debug.Assert( Document != null );

				var buf = Document.Buffer;
				return (LineIndex < buf.LDS.Count) ? buf.LDS[ LineIndex ]
												   : DirtyState.Clean;
			}
			set
			{
				Debug.Assert( Document != null );
				Debug.Assert( 0 <= LineIndex );
				Debug.Assert( LineIndex <= Document.Buffer.LDS.Count );

				var lds = Document.Buffer.LDS;
				if( LineIndex < lds.Count )
					lds[LineIndex] = value;
			}
		}
	}
}
