// 2008-11-03
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Windows;
using Debug = System.Diagnostics.Debug;
using AzukiDocument = Sgry.Azuki.Document;
using CancelEventArgs = System.ComponentModel.CancelEventArgs;

namespace Sgry.Ann
{
	class AppLogic
	{
		#region Fields
		const string OpenFileFilter = "All files(*.*)|*.*|Text files(*.txt, *.c, ...)|*.txt;*.tex;*.java;*.rb;*.pl;*.py;*.c;*.cpp;*.cxx;*.cs;*.h;*.hpp;*.hxx;*.vbs;*.bat;*.log;*.ini;*.inf;*.js;*.htm;*.html;*.xml";
		const string SaveFileFilter = 
			"Text file(*.txt, *.log, *.ini, ...)|*.txt;*.log;*.ini;*.inf;*.tex"
			+ "|HTML file(*.htm, *.html)|*.htm;*.html"
			+ "|CSS file(*.css)|*.css"
			+ "|Javascript file(*.js)|*.js"
			+ "|XML file(*.xml)|*.xml"
			+ "|C/C++ source(*.c, *.h, ...)|*.c;*.cpp;*.cxx;*.h;*.hpp;*.hxx"
			+ "|C# source(*.cs)|*.cs"
			+ "|Java source(*.java)|*.java"
			+ "|Python script(*.py)|*.py"
			+ "|Ruby script(*.rb)|*.rb"
			+ "|Perl script(*.pl)|*.pl"
			+ "|VB script(*.vbs)|*.vbs"
			+ "|Batch file(*.bat)|*.bat";

		AnnForm _MainForm = null;
		List<Document> _DAD_Documents = new List<Document>(); // Dont Access Directly
		Document _DAD_ActiveDocument = null; // Dont Access Directly
		int _UntitledFileCount = 1;
		#endregion

		#region Properties
		/// <summary>
		/// Gets or sets application's main form.
		/// </summary>
		public AnnForm MainForm
		{
			get{ return _MainForm; }
			set
			{
				_MainForm = value;
				_MainForm.Closing += MainForm_Closing;

				// handle initially set document
				Document doc = new Document( value.Azuki.Document );
				AddDocument( doc );
				ActiveDocument = doc;

				// apply config
				MainForm.Azuki.Font = AppConfig.Font;
			}
		}

		/// <summary>
		/// Gets list of documents currently loaded.
		/// </summary>
		public List<Document> Documents
		{
			get{ return _DAD_Documents; }
		}

		/// <summary>
		/// Gets currently active document.
		/// </summary>
		public Document ActiveDocument
		{
			get
			{
				Debug.Assert( _DAD_ActiveDocument == null || _DAD_Documents.Contains(_DAD_ActiveDocument) );
				return _DAD_ActiveDocument;
			}
			set
			{
				Debug.Assert( _DAD_Documents.Contains(value) );
				if( _DAD_ActiveDocument == value )
					return;

				// activate document
				_DAD_ActiveDocument = value;
				_MainForm.Azuki.Document = ActiveDocument.AzukiDoc;
				MainForm.Azuki.ScrollToCaret();

				// update UI
				MainForm.ResetText();
			}
		}
		#endregion

		#region Document Management
		/// <summary>
		/// Add a document to document list.
		/// </summary>
		public void AddDocument( Document doc )
		{
			Debug.Assert( _DAD_Documents.Contains(doc) == false );
			if( doc.FilePath == null )
			{
				doc.DisplayName = "Untitled" + _UntitledFileCount;
				_UntitledFileCount++;
			}
			doc.AzukiDoc.DirtyStateChanged += Doc_DirtyStateChanged;
			_DAD_Documents.Add( doc );
		}

		void Doc_DirtyStateChanged( object sender, EventArgs e )
		{
			MainForm.ResetText();
		}

		/// <summary>
		/// Removes a document from document list.
		/// </summary>
		public void RemoveDocument( Document doc )
		{
			Debug.Assert( _DAD_Documents.Contains(doc) );

			int index = _DAD_Documents.IndexOf( doc );
			_DAD_Documents.RemoveAt( index );
			if( _DAD_ActiveDocument == doc )
			{
				if( index < _DAD_Documents.Count )
					ActiveDocument = _DAD_Documents[ index ];
				else if( 0 < _DAD_Documents.Count )
					ActiveDocument = _DAD_Documents[ 0 ];
				else
					_DAD_ActiveDocument = null;
			}
		}

		/// <summary>
		/// Switch to next document.
		/// </summary>
		public void ActivateNextDocument()
		{
			if( ActiveDocument == null )
				return;

			int index = _DAD_Documents.IndexOf( _DAD_ActiveDocument );
			if( index+1 < _DAD_Documents.Count )
			{
				ActiveDocument = _DAD_Documents[ index+1 ];
			}
			else
			{
				ActiveDocument = _DAD_Documents[ 0 ];
			}
		}

		/// <summary>
		/// Switch to previous document.
		/// </summary>
		public void ActivatePrevDocument()
		{
			if( ActiveDocument == null )
				return;

			int index = _DAD_Documents.IndexOf( _DAD_ActiveDocument );
			if( 0 <= index-1 )
			{
				ActiveDocument = _DAD_Documents[ index-1 ];
			}
			else
			{
				ActiveDocument = _DAD_Documents[ _DAD_Documents.Count-1 ];
			}
		}

		/// <summary>
		/// Shows document list in a dialog.
		/// </summary>
		public void ShowDocumentList()
		{
			DocumentListForm dialog;
			DialogResult result;
			Document selectedDoc;

			using( dialog = new DocumentListForm() )
			{
				// prepare to show dialog
				dialog.Size = MainForm.Size;
				dialog.Documents = Documents;
				dialog.SelectedDocument = ActiveDocument;

				// show document list dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				// get user's selection
				selectedDoc = dialog.SelectedDocument;
				if( selectedDoc != null )
				{
					ActiveDocument = selectedDoc;
				}
			}
		}

		/// <summary>
		/// Sets file type to the document.
		/// </summary>
		public void SetFileType( Document doc, FileType fileType )
		{
			doc.FileType = fileType;
			doc.AzukiDoc.Highlighter = fileType.Highlighter;

			if( doc == ActiveDocument )
			{
				_MainForm.ResetText();
				_MainForm.Azuki.Invalidate();
			}
		}
		#endregion

		#region I/O
		/// <summary>
		/// Opens a file with specified encoding.
		/// Specify null to encoding parameter estimates encoding automatically.
		/// </summary>
		public Document OpenFile( string filePath, Encoding encoding, bool withBom )
		{
			Document doc = new Document( new AzukiDocument() );
			StreamReader file = null;
			char[] buf = new char[ 1024 ];
			int readCount = 0;

			// analyze encoding
			if( encoding == null )
			{
				Utl.AnalyzeEncoding( filePath, out encoding, out withBom );
			}
			doc.Encoding = encoding;

			// load file content
			file = new StreamReader( filePath, encoding );
			while( !file.EndOfStream )
			{
				readCount = file.Read( buf, 0, buf.Length-1 );
				buf[ readCount ] = '\0';
				unsafe
				{
					fixed( char* p = buf )
					{
						doc.AzukiDoc.Replace( new String(p), doc.AzukiDoc.Length, doc.AzukiDoc.Length );
					}
				}
			}
			file.Close();
			doc.AzukiDoc.ClearHistory();
			doc.AzukiDoc.IsDirty = false;
			doc.FilePath = filePath;

			return doc;
		}

		/// <summary>
		/// Open existing file.
		/// </summary>
		public void OpenDocument()
		{
			OpenFileDialog dialog = null;
			DialogResult result;
			Document doc;
			
			using( dialog = new OpenFileDialog() )
			{
				// setup dialog
				if( ActiveDocument.FilePath != null )
				{
					// set initial directory to directory containing currently active file if exists
					string dirPath = Path.GetDirectoryName( ActiveDocument.FilePath );
					if( Directory.Exists(dirPath) )
					{
						dialog.InitialDirectory = dirPath;
					}
				}
				dialog.Filter = OpenFileFilter;

				// show dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				// load the file
				doc = OpenFile( dialog.FileName, null, false );
				AddDocument( doc );

				// activate it
				ActiveDocument = doc;
				MainForm.Azuki.SetSelection( 0, 0 );
				MainForm.Azuki.ScrollToCaret();
			}
		}

		/// <summary>
		/// Save document.
		/// </summary>
		public void SaveDocument( Document doc )
		{
			StreamWriter writer = null;

			// if no file path was associated, switch to SaveAs action
			if( doc.FilePath == null )
			{
				SaveDocumentAs( doc );
				return;
			}

			// overwrite
			using( writer = new StreamWriter(doc.FilePath, false, doc.Encoding) )
			{
				writer.Write( doc.Text );
			}
			doc.AzukiDoc.IsDirty = false;
		}

		/// <summary>
		/// Save document content as another file.
		/// </summary>
		public void SaveDocumentAs( Document doc )
		{
			Debug.Assert( doc != null );
			SaveFileDialog dialog = null;
			DialogResult result;
			
			using( dialog = new SaveFileDialog() )
			{
				// setup dialog
				if( doc.FilePath != null )
				{
					// set initial directory to that containing currently active file if exists
					string dirPath = Path.GetDirectoryName( doc.FilePath );
					if( Directory.Exists(dirPath) )
					{
						dialog.InitialDirectory = dirPath;
					}
				}
				dialog.Filter = SaveFileFilter;

				// show dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				// associate the file path
				doc.FilePath = dialog.FileName;
			}

			// delegate to overwrite logic
			SaveDocument( doc );
		}

		/// <summary>
		/// Closes a document.
		/// </summary>
		public void CloseDocument( Document doc )
		{
			DialogResult result;

			// confirm to discard modification
			if( doc.AzukiDoc.IsDirty )
			{
				result = AlertDiscardModification( doc );
				if( result == DialogResult.Yes )
				{
					SaveDocument( doc );
				}
				else if( result == DialogResult.Cancel )
				{
					return;
				}
			}

			// close
			RemoveDocument( doc );
			if( Documents.Count == 0 )
			{
				MainForm.Close();
			}
		}

		public DialogResult AlertDiscardModification( Document doc )
		{
			return MessageBox.Show(
					doc.DisplayName + " is modified but not saved. Save changes?",
					"Ann",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Exclamation,
					MessageBoxDefaultButton.Button2
				);
		}
		#endregion

		#region UI Event Handlers
		void MainForm_Closing( object sender, CancelEventArgs e )
		{
			DialogResult result;

			// confirm all document's discard
			foreach( Document doc in Documents )
			{
				// if it's modified, ask to save the document
				if( doc.AzukiDoc.IsDirty )
				{
					result = AlertDiscardModification( doc );
					if( result == DialogResult.Yes )
					{
						SaveDocument( doc );
						if( doc.AzukiDoc.IsDirty )
						{
							// canceled or failed. cancel closing
							e.Cancel = true;
							return;
						}
					}
					else if( result == DialogResult.Cancel )
					{
						e.Cancel = true;
						return;
					}
				}
			}
		}
		#endregion

		#region Utilities
		static class Utl
		{
			public static void AnalyzeEncoding( string filePath, out Encoding encoding, out bool withBom )
			{
				const int OneMega = 1024 * 1024;
				Stream file = null;
				byte[] data;
				int dataSize;

				try
				{
					using( file = File.OpenRead(filePath) )
					{
						// prepare buffer
						if( OneMega < file.Length )
							dataSize = OneMega;
						else
							dataSize = (int)file.Length;
						data = new byte[ dataSize ];

						// read data at maximum 1MB
						file.Read( data, 0, dataSize );
						encoding = EncodingAnalyzer.Analyze( data, out withBom );
						if( encoding == null )
						{
							encoding = Encoding.Default;
							withBom = false;
						}
					}
				}
				catch( IOException )
				{
					encoding = Encoding.Default;
					withBom = false;
				}
			}
		}
		#endregion
	}
}
