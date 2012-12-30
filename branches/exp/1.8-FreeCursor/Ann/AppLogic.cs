using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Sgry.Azuki;
using Sgry.Azuki.Highlighter;
using Sgry.Azuki.WinForms;
using Assembly = System.Reflection.Assembly;
using CancelEventArgs = System.ComponentModel.CancelEventArgs;
using Color = System.Drawing.Color;
using Debug = System.Diagnostics.Debug;
using PropertyChangedEventArgs = System.ComponentModel.PropertyChangedEventArgs;
using AzukiDocument = Sgry.Azuki.Document;

namespace Sgry.Ann
{
	class AppLogic : IDisposable
	{
		#region Fields
		const string OpenFileFilter =
			"All files(*.*)|*.*"
			+ "|Supported files|*.txt;*.log;*.ini;*.inf;*.tex;*.htm;*.html;*.css;*.js;*.xml;*.c;*.cpp;*.cxx;*.h;*.hpp;*.hxx;*.cs;*.java;*.py;*.rb;*.pl;*.vbs;*.bat"
			+ "|" + CommonFileFilter;

		const string SaveFileFilter =
			"All files(*.*)|*.*"
			+ "|" + CommonFileFilter;

		const string CommonFileFilter =
			"Text file(*.txt, *.log, *.tex, ...)|*.txt;*.log;*.ini;*.inf;*.tex"
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

		static string _AppInstanceMutexName = null;
		static string _IpcFilePath = null;

		AnnForm _MainForm = null;
		List<Document> _DAD_Documents = new List<Document>(); // Don't Access Directly
		Document _DAD_ActiveDocument = null; // Don't Access Directly
		int _UntitledFileCount = 1;
		string[] _InitOpenFilePaths = null;
		SearchContext _SearchContext = new SearchContext();
		Thread _MonitorThread;
		bool _MonitorThreadCanContinue;
		PseudoPipe _IpcPipe = new PseudoPipe();
		bool _AskingUserToReloadOrNot = false;
		bool _ShouldUpdateTextAreaWidth = false;
		#endregion

		#region Init / Dispose
		public AppLogic( string[] initOpenFilePaths )
		{
			_InitOpenFilePaths = initOpenFilePaths;
		}

		~AppLogic()
		{
			Dispose();
		}

		public void Dispose()
		{
			_MonitorThreadCanContinue = false;
			if( _MonitorThread != null )
			{
				if( _MonitorThread.Join(1000) == false )
				{
					_MonitorThread.Abort();
				}
				_MonitorThread = null;
			}

			if( _IpcPipe != null )
			{
				_IpcPipe.Dispose();
				_IpcPipe = null;
			}

			try
			{
				if( File.Exists(IpcFilePath) )
					File.Delete( IpcFilePath );
			}
			catch
			{}
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets the editor engine.
		/// </summary>
		public AzukiControl Azuki
		{
			get{ return _MainForm.Azuki; }
		}

		/// <summary>
		/// Gets or sets application's main form.
		/// </summary>
		public AnnForm MainForm
		{
			get{ return _MainForm; }
			set
			{
				_MainForm = value;
				_MainForm.Load += MainForm_Load;
				_MainForm.Closing += MainForm_Closing;
				_MainForm.Closed += MainForm_Closed;
				_MainForm.Azuki.Resize += Azuki_Resize;
				_MainForm.Azuki.Click += Azuki_Click;
				_MainForm.Azuki.DoubleClick += Azuki_DoubleClick;
				_MainForm.TabPanel.Items = Documents;
				_MainForm.TabPanel.TabSelected += TabPanel_TabSelected;
				_MainForm.Load += delegate {
					_MonitorThreadCanContinue = true;
					_MonitorThread = new Thread( MonitorThreadProc );
					_MonitorThread.Start();
				};
				_SearchContext.PropertyChanged += delegate( object sender, PropertyChangedEventArgs e ) {
					if( e.PropertyName == "PatternFixed"
						&& _SearchContext.PatternFixed == true )
					{
						OnSearchContextFixed();
					}
					else if( e.PropertyName == "MatchCase"
						|| e.PropertyName == "Regex"
						|| e.PropertyName == "TextPattern"
						|| e.PropertyName == "UseRegex" )
					{
						OnSearchContextChanged( _SearchContext.Forward );
					}
				};

				// register watching pattern for text search
				Marking.Register( new MarkingInfo(0, "Text searching target") );
				_MainForm.Azuki.ColorScheme.SetMarkingDecoration(
						0, new BgColorTextDecoration( Color.Yellow )
					);

				// handle initially set document
				Document doc = new Document();
				AddDocument( doc );
				ActiveDocument = doc;

				// give the search context object to text search UI
				_MainForm.SearchPanel.SetContextRef( _SearchContext );
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
				Azuki.Document = value;
				Azuki.ScrollToCaret();
				Azuki.UpdateCaretGraphic();
				MainForm.TabPanel.SelectedItem = value;

				// update UI
				MainForm.UpdateUI();
				MainForm.TabPanel.Invalidate();
			}
		}

		public static string AppInstanceMutexName
		{
			get
			{
				if( _AppInstanceMutexName == null )
				{
					Assembly exe = Assembly.GetExecutingAssembly();
					string exePath = exe.GetModules()[0].FullyQualifiedName;
					exePath = exePath.Replace( '\\', '.' );
					_AppInstanceMutexName = "Sgry.Ann." + exePath;
				}
				return _AppInstanceMutexName;
			}
		}

		public static string IpcFilePath
		{
			get
			{
				if( _IpcFilePath == null )
				{
					Assembly exe = Assembly.GetExecutingAssembly();
					string exePath = exe.GetModules()[0].FullyQualifiedName;
					string exeDirPath = Path.GetDirectoryName( exePath );
					_IpcFilePath = Path.Combine( exeDirPath, "Ann.ipc" );
				}
				return _IpcFilePath;
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
			doc.DirtyStateChanged += Doc_DirtyStateChanged;
			doc.SelectionModeChanged += Doc_SelectionModeChanged;
			_DAD_Documents.Add( doc );
		}

		void Doc_SelectionModeChanged( object sender, EventArgs e )
		{
			MainForm.UpdateUI();
		}

		void Doc_DirtyStateChanged( object sender, EventArgs e )
		{
			MainForm.UpdateUI();
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
			doc.DirtyStateChanged -= Doc_DirtyStateChanged;
			doc.SelectionModeChanged -= Doc_SelectionModeChanged;
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
			doc.Highlighter = fileType.Highlighter;

			if( doc == ActiveDocument )
			{
				Azuki.AutoIndentHook = fileType.AutoIndentHook;
				MainForm.UpdateUI();
				Azuki.Invalidate();
			}
		}

		/// <summary>
		/// Sets EOL code for input
		/// and unify existing EOL code to the one if user choses so.
		/// </summary>
		public void SetEolCode( string eolCode )
		{
			DialogResult reply;

			if( eolCode != "\r\n" && eolCode != "\r" && eolCode != "\n" )
				throw new ArgumentException( "EOL code must be one of the CR+LF, CR, LF.", "eolCode" );

			// if newly specified EOL code is same as currently set one, do nothing
			if( Azuki.Document.EolCode == eolCode )
			{
				return;
			}

			// set input EOL code
			Azuki.Document.EolCode = eolCode;

			// ask user whether to unify currently existing all EOL codes to the new one
			reply = AskUserToUnifyExistingEolOrNot( eolCode );
			if( reply == DialogResult.Yes )
			{
				//--- unify EOL code ---
				Document doc = ActiveDocument;
				StringBuilder newContent = new StringBuilder( doc.Length*2 );

				// make copy of lines and set EOL to specified one
				for( int i=0; i<doc.LineCount-1; i++ )
				{
					newContent.Append( doc.GetLineContent(i) );
					newContent.Append( eolCode );
				}
				if( 0 < doc.LineCount )
				{
					newContent.Append( doc.GetLineContent(doc.LineCount - 1) );
				}

				// then replace whole content
				doc.Replace( newContent.ToString(), 0, doc.Length );
			}

			MainForm.UpdateUI();
		}
		#endregion

		#region I/O
		/// <summary>
		/// Creates a new document.
		/// </summary>
		public void CreateNewDocument()
		{
			// create a document
			Document doc = new Document();
			AddDocument( doc );

			// activate it
			ActiveDocument = doc;
		}

		/// <summary>
		/// Opens a file with specified encoding and create a Document object.
		/// Give null as 'encoding' parameter to detect encoding automatically.
		/// </summary>
		/// <returns>A Document object or null if failed.</returns>
		Document CreateDocumentFromFile( string filePath, Encoding encoding, bool withBom )
		{
			Debug.Assert( filePath != null );
			Document doc;
			string errorMessage = null;

			// create new document
			try
			{
				doc = new Document();
				LoadFileContentToDocument( doc, filePath, encoding, withBom );
				return doc;
			}
			catch( ArgumentException ex )
			{
				// the path is "wild?cards.txt" for example.
				errorMessage = String.Format( "{0}\n\nPath: {1}", ex.Message, filePath );
			}
			catch( NotSupportedException ex )
			{
				// the path is "http://sgry.jp/" for example.
				errorMessage = String.Format( "{0}\n\nPath: {1}", ex.Message, filePath );
			}
			catch( UnauthorizedAccessException ex )
			{
				// the path is a directory or a file which the user has no permission to read
				errorMessage = ex.Message;
			}
			catch( IOException ex )
			{
				errorMessage = String.Format( "{0}\n\nPath: {1}", ex.Message, filePath );
			}
			catch( System.Security.SecurityException ex )
			{
				errorMessage = ex.Message;
			}
			catch( OutOfMemoryException ex )
			{
				errorMessage = ex.Message;
			}
			
			Alert( errorMessage, MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
			return null;
		}

		/// <summary>
		/// Open existing file.
		/// </summary>
		public void OpenDocument()
		{
			OpenFileDialog dialog = null;
			DialogResult result;
			
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
				dialog.FilterIndex = 2;

				// show dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				// open the file
				OpenDocument( dialog.FileName );
			}
		}

		/// <summary>
		/// Open existing file.
		/// </summary>
		public void OpenDocument( string filePath )
		{
			Document doc;

			// if specified file was already opened, just return the document
			foreach( Document d in Documents )
			{
				if( String.Compare(d.FilePath, filePath, true) == 0 )
				{
					ActiveDocument = d;
					return;
				}
			}

			// load the file
			doc = CreateDocumentFromFile( filePath, null, false );
			if( doc == null )
			{
				return;
			}

			// add this file to document list unless it is loaded already
			if( Documents.Contains(doc) == false )
			{
				AddDocument( doc );
			}

			// activate it
			ActiveDocument = doc;
			SetFileType( doc, FileType.GetFileTypeByFileName(filePath) );
			Azuki.Select( 0, 0, TextDataType.Normal );
			Azuki.ScrollToCaret();
		}

		/// <summary>
		/// Save document.
		/// </summary>
		public void SaveDocument( Document doc )
		{
			string dirPath;

			// if the document is read-only, do nothing
			if( doc.IsReadOnly )
			{
				return;
			}

			// if no file path was associated, switch to SaveAs action
			if( doc.FilePath == null )
			{
				SaveDocumentAs( doc );
				return;
			}

			// ensure that destination directory exists
			dirPath = Path.GetDirectoryName( doc.FilePath );
			if( Directory.Exists(dirPath) == false )
			{
				try
				{
					Directory.CreateDirectory( dirPath );
				}
				catch( IOException ex )
				{
					// case ex: opened file has been on a removable drive
					// and the drive was ejected now
					Alert( ex );
					return;
				}
				catch( UnauthorizedAccessException ex )
				{
					// case example: permission of parent directory was changed
					// and current user lost right to create directory
					Alert( ex );
					return;
				}
			}

			// overwrite
			try
			{
				byte[] bomBytes = new byte[]{};
				byte[] contentBytes = null;

				// decode content to native encoding
				contentBytes = doc.Encoding.GetBytes( doc.Text );
				if( doc.WithBom )
				{
					if( doc.Encoding == Encoding.BigEndianUnicode
						|| doc.Encoding == Encoding.Unicode
						|| doc.Encoding == Encoding.UTF8 )
					{
						bomBytes = doc.Encoding.GetBytes( "\xFEFF" );
					}
				}

				// write file bytes
				using( FileStream file = File.Open(doc.FilePath,
										 FileMode.OpenOrCreate,
										 FileAccess.ReadWrite,
										 FileShare.ReadWrite) )
				{
					file.SetLength( 0 );
					file.Write( bomBytes, 0, bomBytes.Length );
					file.Write( contentBytes, 0, contentBytes.Length );
				}
				doc.IsDirty = false;
				doc.LastSavedTime = File.GetLastWriteTime( doc.FilePath );
			}
			catch( UnauthorizedAccessException ex )
			{
				// case example: target file is readonly.
				// case example: target file was deleted and now there is a directory having same name
				Alert( ex );
			}
			catch( IOException ex )
			{
				// case example: another process is opening the file and does not allow to write
				Alert( ex );
			}

			// Reload configuration if it's the application config file.
			if( doc.FilePath.ToLower() == AppConfig.IniFilePath.ToLower() )
			{
				LoadConfig( false );
			}
		}

		/// <summary>
		/// Save document content as another file.
		/// </summary>
		public void SaveDocumentAs( Document doc )
		{
			Debug.Assert( doc != null );
			SaveFileDialog dialog = null;
			DialogResult result;
			string fileName;
			
			using( dialog = new SaveFileDialog() )
			{
				// setup dialog
				if( doc.FilePath != null )
				{
					// set initial directory to that containing currently active file if exists
#					if !PocketPC
					string dirPath = Path.GetDirectoryName( doc.FilePath );
					if( Directory.Exists(dirPath) )
					{
						dialog.InitialDirectory = dirPath;
					}
#					endif

					// set initial file name
					dialog.FileName = Path.GetFileName( doc.FilePath );
				}
				dialog.Filter = SaveFileFilter;

				// show dialog
				result = dialog.ShowDialog();
				if( result != DialogResult.OK )
				{
					return;
				}

				fileName = dialog.FileName;
			}

			// associate the file path and reset attributes
#			if PocketPC
			// In Windows Mobile's SaveFileDialog,
			// if we select filter item "Text File|*.txt;*.log"
			// and enter file name "foo" and tap OK button, then,
			// FileName property value will be "foo.txt;*.log".
			// Of cource this is not expected so we cut off trailing garbages here.
			Match match = Regex.Match( fileName, @"(;\*\.[a-zA-Z0-9_#!$~]+)+" );
			if( match.Success )
			{
				fileName = fileName.Substring( 0, fileName.Length - match.Length );
			}
#			endif
			doc.FilePath = fileName;
			doc.IsReadOnly = false;

			// delegate to overwrite logic
			SaveDocument( doc );

			// finally, update UI because the name of the document was changed
			MainForm.UpdateUI();
		}

		/// <summary>
		/// Reloads a document.
		/// </summary>
		public void ReloadDocument( Document doc )
		{
			ReloadDocument( doc, null, false );
		}

		/// <summary>
		/// Reloads document.
		/// </summary>
		public void ReloadDocument( Document doc,
									Encoding encoding,
									bool withBom )
		{
			Debug.Assert( doc != null );

			try
			{
				IHighlighter highlighter;
				int line, column;

				// remember caret position
				doc.GetLineColumnIndexFromCharIndex( doc.CaretIndex,
													 out line, out column );

				// detach highlighter temporarily
				highlighter = doc.Highlighter;
				doc.Highlighter = null;

				// reload content
				LoadFileContentToDocument( doc, doc.FilePath, encoding, withBom );

				// attach the highlighter again
				doc.Highlighter = highlighter;

				// restore caret position and scroll to it
				line = Math.Min( line, doc.LineCount-1 );
				column = Math.Min( column, doc.GetLineLength(line) );
				Azuki.Select( line, column, line, column, TextDataType.Normal);

				_MainForm.UpdateUI();
			}
			catch( NotSupportedException ex )
			{
				Alert( ex );
			}
			catch( UnauthorizedAccessException ex )
			{
				Alert( ex );
			}
			catch( IOException ex )
			{
				Alert( ex );
			}
			catch( System.Security.SecurityException ex )
			{
				Alert( ex );
			}
		}

		/// <summary>
		/// Closes a document.
		/// </summary>
		public void CloseDocument( Document doc )
		{
			DialogResult result;

			// confirm to discard modification
			if( doc.IsDirty )
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

		/// <exception cref="System.ArgumentException">Specified path is too long.</exception>
		/// <exception cref="System.IO.PathTooLongException">Specified path is too long.</exception>
		/// <exception cref="System.IO.DirectoryNotFoundException">Specified path string contains unexisting directory.</exception>
		/// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
		/// <exception cref="System.IO.FileNotFoundException">Specified file was not found.</exception>
		/// <exception cref="System.NotSupportedException">Format of the path string is not supported.</exception>
		/// <exception cref="System.UnauthorizedAccessException">The path indicates a directory. -or- The caller does not have the required permission to read the file.</exception>
		/// <exception cref="System.OutOfMemoryException">There is no enough memory to operate.</exception>
		void LoadFileContentToDocument( Document doc, string filePath, Encoding encoding, bool withBom )
		{
			FileStream stream = null;
			StreamReader file = null;

			Debug.Assert( doc != null );
			Debug.Assert( filePath != null );

			// analyze encoding
			if( encoding == null )
			{
				Utl.AnalyzeEncoding( filePath, out encoding, out withBom );
			}
			doc.Encoding = encoding;
			doc.WithBom = withBom;

			// load file content
			try
			{
				char[] buf = null;
				int readCount = 0;

				// open the file
				stream = File.Open( filePath, FileMode.Open,
									FileAccess.Read, FileShare.ReadWrite );
				file = new StreamReader( stream, encoding );

				// make the document content empty first
				doc.Replace( "", 0, doc.Length );

				// expand internal buffer size before loading file
				// (estimating needed buffer size by a half of byte-count of file)
				doc.Capacity = (int)( file.BaseStream.Length / 2 );

				// prepare load buffer
				// (if the file is larger than 1MB, separate by 10 and load for each)
				if( file.BaseStream.Length < 1024*1024 )
				{
					buf = new char[ file.BaseStream.Length ];
				}
				else
				{
					buf = new char[ (file.BaseStream.Length+10) / 10 ];
				}

				// read
				while( !file.EndOfStream )
				{
					readCount = file.Read( buf, 0, buf.Length );
					doc.Replace( new String(buf, 0, readCount), doc.Length, doc.Length );
				}
			}
			finally
			{
#				if !PocketPC
				if( file != null )
					file.Dispose();
				if( stream != null )
					stream.Dispose();
#				endif
			}

			// set document properties
			doc.ClearHistory();
			doc.FilePath = filePath;
			doc.EolCode = Utl.AnalyzeEolCode( doc );
			doc.LastSavedTime = File.GetLastWriteTime( filePath );
			if( (new FileInfo(filePath).Attributes & FileAttributes.ReadOnly) != 0 )
			{
				doc.IsReadOnly = true;
			}
		}
		#endregion

		#region Text Search
		public void UpdateWatchPatternForTextSearch()
		{
			if( _SearchContext.UseRegex )
			{
				ActiveDocument.SearchingPattern = _SearchContext.Regex;
			}
			else
			{
				ActiveDocument.SearchingPattern = new Regex(
						Regex.Escape(_SearchContext.TextPattern),
						_SearchContext.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase
					);
			}
		}

		void OnSearchContextFixed()
		{
			// set text pattern to emphasize
			UpdateWatchPatternForTextSearch();

			// deactivate search panel
			MainForm.DeactivateSearchPanel();
		}

		void OnSearchContextChanged( bool forward )
		{
			if( _MainForm.SearchPanel.Enabled == false )
			{
				return;
			}

			// search incrementally
			if( forward )
				FindNext();
			else
				FindPrev();
		}

		public void FindNext()
		{
			AzukiDocument doc = ActiveDocument;
			int startIndex;
			SearchResult result;
			Regex regex;

			// determine where to start text search
			if( 0 <= _SearchContext.AnchorIndex )
				startIndex = _SearchContext.AnchorIndex;
			else
				startIndex = Math.Max( doc.CaretIndex, doc.AnchorIndex );

			// find
			if( _SearchContext.UseRegex )
			{
				// Regular expression search.
				// get regex object from context
				regex = _SearchContext.Regex;
				if( regex == null )
				{
					// current text pattern was invalid as a regular expression.
					return;
				}

				// ensure that "RightToLeft" option of the regex object is NOT set
				RegexOptions opt = regex.Options;
				if( (opt & RegexOptions.RightToLeft) != 0 )
				{
					opt &= ~( RegexOptions.RightToLeft );
					regex = new Regex( regex.ToString(), opt );
					_SearchContext.Regex = regex;
				}
				result = doc.FindNext( regex, startIndex, doc.Length );
			}
			else
			{
				// normal text pattern matching.
				result = doc.FindNext( _SearchContext.TextPattern, startIndex, doc.Length, _SearchContext.MatchCase );
			}

			// select the result
			if( result != null )
			{
				Azuki.Select( result.Begin, result.End );
Azuki.View.SetDesiredColumn();
				Azuki.ScrollToCaret();
			}
		}

		public void FindPrev()
		{
			AzukiDocument doc = ActiveDocument;
			int startIndex;
			SearchResult result;
			Regex regex;

			// determine where to start text search
			if( 0 <= _SearchContext.AnchorIndex )
				startIndex = _SearchContext.AnchorIndex;
			else
				startIndex = Math.Min( doc.CaretIndex, doc.AnchorIndex );

			// find
			if( _SearchContext.UseRegex )
			{
				// Regular expression search.
				// get regex object from context
				regex = _SearchContext.Regex;
				if( regex == null )
				{
					// current text pattern was invalid as a regular expression.
					return;
				}

				// ensure that "RightToLeft" option of the regex object is set
				RegexOptions opt = _SearchContext.Regex.Options;
				if( (opt & RegexOptions.RightToLeft) == 0 )
				{
					opt |= RegexOptions.RightToLeft;
					_SearchContext.Regex = new Regex( _SearchContext.Regex.ToString(), opt );
				}
				result = doc.FindPrev( _SearchContext.Regex, 0, startIndex );
			}
			else
			{
				// normal text pattern matching.
				result = doc.FindPrev( _SearchContext.TextPattern, 0, startIndex, _SearchContext.MatchCase );
			}

			// select the result
			if( result != null )
			{
				Azuki.Select( result.End, result.Begin );
Azuki.View.SetDesiredColumn();
				Azuki.ScrollToCaret();
			}
		}
		#endregion

		#region Config
		public void LoadConfig( bool includeWindowConfig )
		{
			// load config file
			AppConfig.Load();

			// apply config
			Azuki.FontInfo					= AppConfig.FontInfo;
			MainForm.TabPanelEnabled		= AppConfig.TabPanelEnabled;

			Azuki.DrawsEolCode				= AppConfig.DrawsEolCode;
			Azuki.DrawsFullWidthSpace		= AppConfig.DrawsFullWidthSpace;
			Azuki.DrawsSpace				= AppConfig.DrawsSpace;
			Azuki.DrawsTab					= AppConfig.DrawsTab;
			Azuki.DrawsEofMark				= AppConfig.DrawsEofMark;
			Azuki.HighlightsCurrentLine		= AppConfig.HighlightsCurrentLine;
			Azuki.HighlightsMatchedBracket	= AppConfig.HighlightsMatchedBracket;
			Azuki.ShowsLineNumber			= AppConfig.ShowsLineNumber;
			Azuki.ShowsHRuler				= AppConfig.ShowsHRuler;
			Azuki.ShowsDirtBar				= AppConfig.ShowsDirtBar;
			Azuki.TabWidth					= AppConfig.TabWidth;
			Azuki.LinePadding				= AppConfig.LinePadding;
			Azuki.LeftMargin				= AppConfig.LeftMargin;
			Azuki.TopMargin					= AppConfig.TopMargin;
			Azuki.ViewType					= AppConfig.ViewType;
			Azuki.UsesTabForIndent			= AppConfig.UsesTabForIndent;
			Azuki.ConvertsFullWidthSpaceToSpace = AppConfig.ConvertsFullWidthSpaceToSpace;
			Azuki.HRulerIndicatorType		= AppConfig.HRulerIndicatorType;
			Azuki.ScrollsBeyondLastLine		= AppConfig.ScrollsBeyondLastLine;

			// apply window config
			if( includeWindowConfig )
			{
				MainForm.ClientSize = AppConfig.WindowSize;
				if( AppConfig.WindowMaximized )
				{
					MainForm.WindowState = FormWindowState.Maximized;
				}
			}

			// update UI
			MainForm.UpdateUI();
		}

		public void SaveConfig()
		{
			// update config fields
			AppConfig.FontInfo				= Azuki.FontInfo;
			AppConfig.WindowMaximized		= (MainForm.WindowState == FormWindowState.Maximized);
			if( MainForm.WindowState == FormWindowState.Normal )
			{
				AppConfig.WindowSize = MainForm.ClientSize;
			}
			AppConfig.TabPanelEnabled			= MainForm.TabPanelEnabled;

			AppConfig.DrawsEolCode				= Azuki.DrawsEolCode;
			AppConfig.DrawsFullWidthSpace		= Azuki.DrawsFullWidthSpace;
			AppConfig.DrawsSpace				= Azuki.DrawsSpace;
			AppConfig.DrawsTab					= Azuki.DrawsTab;
			AppConfig.DrawsEofMark				= Azuki.DrawsEofMark;
			AppConfig.HighlightsCurrentLine		= Azuki.HighlightsCurrentLine;
			AppConfig.HighlightsMatchedBracket	= Azuki.HighlightsMatchedBracket;
			AppConfig.ShowsLineNumber			= Azuki.ShowsLineNumber;
			AppConfig.ShowsHRuler				= Azuki.ShowsHRuler;
			AppConfig.ShowsDirtBar				= Azuki.ShowsDirtBar;
			AppConfig.TabWidth					= Azuki.TabWidth;
			AppConfig.LinePadding				= Azuki.LinePadding;
			AppConfig.LeftMargin				= Azuki.LeftMargin;
			AppConfig.TopMargin					= Azuki.TopMargin;
			AppConfig.ViewType					= Azuki.ViewType;
			AppConfig.UsesTabForIndent			= Azuki.UsesTabForIndent;
			AppConfig.ConvertsFullWidthSpaceToSpace = Azuki.ConvertsFullWidthSpaceToSpace;
			AppConfig.HRulerIndicatorType		= Azuki.HRulerIndicatorType;
			AppConfig.ScrollsBeyondLastLine		= Azuki.ScrollsBeyondLastLine;

			// save to file
			AppConfig.Save();
		}
		#endregion

		#region UI Event Handlers
		void MainForm_Load( object sender, EventArgs e )
		{
			if( _InitOpenFilePaths == null || _InitOpenFilePaths.Length < 1 )
				return;

			Document prevActiveDoc;

			// try to open the first file
			prevActiveDoc = ActiveDocument;
			OpenDocument( _InitOpenFilePaths[0] );

			// close default empty document if successfully opened
			if( prevActiveDoc != ActiveDocument )
			{
				CloseDocument( prevActiveDoc );
			}

			// open second or later files
			for( int i=1; i<_InitOpenFilePaths.Length; i++ )
			{
				OpenDocument( _InitOpenFilePaths[i] );
			}
		}

		void MainForm_Closing( object sender, CancelEventArgs e )
		{
			DialogResult result;
			Document activeDoc = ActiveDocument;

			// confirm all document's discard
			foreach( Document doc in Documents )
			{
				// if it's modified, ask to save the document
				if( doc.IsDirty )
				{
					// before showing dialog, activate the document
					this.ActiveDocument = doc;

					// then, show dialog
					result = AlertDiscardModification( doc );
					if( result == DialogResult.Yes )
					{
						SaveDocument( doc );
						if( doc.IsDirty )
						{
							// canceled or failed. cancel closing
							e.Cancel = true;
							ActiveDocument = activeDoc;
							return;
						}
					}
					else if( result == DialogResult.Cancel )
					{
						e.Cancel = true;
						ActiveDocument = activeDoc;
						return;
					}
				}
			}
		}

		void MainForm_Closed( object sender, EventArgs e )
		{
			SaveConfig();
		}

		internal void MainForm_DelayedActivated()
		{
			List<Document> docsToBeReloaded;
			DialogResult result;

			if( InterlockedSetFlag(ref _AskingUserToReloadOrNot, true) == false )
			{
				// list up documents to be reloaded
				docsToBeReloaded = new List<Document>( Documents.Count );
				foreach( Document doc in Documents )
				{
					if( File.Exists(doc.FilePath)
						&& doc.LastSavedTime != File.GetLastWriteTime(doc.FilePath) )
					{
						docsToBeReloaded.Add( doc );
					}
				}

				// ask user to reload each document
				result = DialogResult.OK;
				foreach( Document doc in docsToBeReloaded )
				{
					// once user canceled reloading,
					// silently ignore the update of last documents
					if( result == DialogResult.Cancel )
					{
						doc.LastSavedTime = File.GetLastWriteTime(doc.FilePath);
						continue;
					}

					// activate the document
					ActiveDocument = doc;

					// ask user whether to reload it or not
					result = Alert(
						""+doc.FilePath+" was updated by other program. Do you want to reload?",
						MessageBoxButtons.YesNoCancel, MessageBoxIcon.Asterisk
					);
					if( result != DialogResult.Yes )
					{
						doc.LastSavedTime = File.GetLastWriteTime(doc.FilePath);
						continue;
					}

					// reload it
					ReloadDocument( doc );
				}

				_AskingUserToReloadOrNot = false;
			}
		}

		bool InterlockedSetFlag( ref bool flag, bool newValue )
		{
			lock( this )
			{
				bool prevValue = flag;
				flag = newValue;
				return prevValue;
			}
		}

		void TabPanel_TabSelected( MouseEventArgs e, Document item )
		{
			if( e.Button == MouseButtons.Left )
			{
				ActiveDocument = item;
			}
			else if( e.Button == MouseButtons.Middle )
			{
				CloseDocument( item );
				MainForm.TabPanel.Invalidate();
			}
		}

		void Azuki_Resize( object sender, EventArgs e )
		{
			if( Azuki.ViewType == ViewType.WrappedProportional )
			{
				_ShouldUpdateTextAreaWidth = true;
			}
		}

		void Azuki_Click( object sender, EventArgs e )
		{
			AzukiDocument doc = Azuki.Document;
			IMouseEventArgs mea = (IMouseEventArgs)e;
			int urlBegin, urlEnd, selBegin, selEnd;

			if( mea.Index < doc.Length
				&& doc.IsMarked(mea.Index, Marking.Uri)
				&& Azuki.View.TextAreaRectangle.Contains(mea.Location) )
			{
				// select entire URI if not selected, or deselect if selected.
				doc.GetMarkedRange( mea.Index, Marking.Uri, out urlBegin, out urlEnd );
				doc.GetSelection( out selBegin, out selEnd );
				if( selBegin != urlBegin && selEnd != urlEnd )
				{
					Azuki.Select( urlBegin, urlEnd );
				}
				else
				{
					Azuki.Select( mea.Index, mea.Index );
				}
				mea.Handled = true;
			}
		}

		void Azuki_DoubleClick( object sender, EventArgs e )
		{
			AzukiDocument doc = Azuki.Document;
			IMouseEventArgs mea = (IMouseEventArgs)e;

			if( mea.Index < doc.Length
				&& doc.IsMarked(mea.Index, Marking.Uri) )
			{
				string uriString;
				DialogResult result;

				// ask user to jump to the URI
				uriString = Azuki.Document.GetMarkedText( mea.Index, Marking.Uri );
				if( uriString != null )
				{
					result = MessageBox.Show(
							"Opening the URL. Do you wish to continue?\n" + uriString + "",
							"Ann",
							MessageBoxButtons.OKCancel,
							MessageBoxIcon.Question,
							MessageBoxDefaultButton.Button1
						);
					if( result == DialogResult.OK )
					{
						try
						{
							Process.Start( uriString, "" );
						}
						catch( Exception ex )
						{
							MessageBox.Show( ex.Message );
						}
					}
					mea.Handled = true;
				}
			}
		}
		#endregion

		#region Monitoring
		void MonitorThreadProc()
		{
			DateTime timestamp = DateTime.MinValue;

			_IpcPipe.Create( IpcFilePath );

			while( _MonitorThreadCanContinue )
			{
				Thread.CurrentThread.Join( 250 );

				// if IPC file was updated, parse it
				if( timestamp < _IpcPipe.GetLastWriteTime() )
				{
					// parse and do actions
					ParseIpcFile();

					// remember new timestamp
					timestamp = File.GetLastWriteTime( IpcFilePath );
				}
				if( _ShouldUpdateTextAreaWidth )
				{
					_ShouldUpdateTextAreaWidth = false;
					MainForm.Invoke(
						new ThreadStart(ApplyNewTextAreaWidth)
					);
				}
			}
		}

		public void ApplyNewTextAreaWidth()
		{
			Azuki.ViewWidth = Azuki.ClientSize.Width
							  - Azuki.View.HRulerUnitWidth * 2;
		}

		void ParseIpcFile()
		{
			string[] tokens;

			// read lines and parse them
			foreach( string line in _IpcPipe.ReadLines(1000) )
			{
				// parse this line
				tokens = line.Split( ',' );
				if( tokens[0] == "Activate" )
				{
					_MainForm.Invoke( new ThreadStart(_MainForm.Activate) );
				}
				else if( tokens[0] == "OpenDocument" && 1 < tokens.Length )
				{
					_MainForm.Invoke( new Action<string>(OpenDocument), tokens[1] );
				}
			}
		}
		#endregion

		#region Utilities
		public DialogResult AlertDiscardModification( Document doc )
		{
			return Alert(
					doc.DisplayName + " is modified but not saved. Save changes?",
					MessageBoxButtons.YesNoCancel,
					MessageBoxIcon.Exclamation,
					MessageBoxDefaultButton.Button2
				);
		}

		public DialogResult AskUserToUnifyExistingEolOrNot( string newEolCode )
		{
			string eolCodeName;

			switch( newEolCode )
			{
				case "\r\n":	eolCodeName = "CR+LF";	break;
				case "\n":		eolCodeName = "LF";		break;
				case "\r":		eolCodeName = "CR";		break;
				default:		throw new ArgumentException("EOL code must be one of CR+LF, LF, CR.", "newEolCode");
			}

			return Alert(
					"Do you also want to change all existing line end code to "+eolCodeName+"?",
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question,
					MessageBoxDefaultButton.Button2
				);
		}

		void Alert( Exception ex )
		{
			Alert( ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
		}

		DialogResult Alert( string text, MessageBoxButtons buttons, MessageBoxIcon icon )
		{
			return Alert( text, buttons, icon, MessageBoxDefaultButton.Button1 );
		}

		DialogResult Alert( string text, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton )
		{
#			if !PocketPC
			return MessageBox.Show( _MainForm, text, "Ann", buttons, icon, defaultButton );
#			else
			return MessageBox.Show( text, "Ann", buttons, icon, defaultButton );
#			endif
		}

		static class Utl
		{
			/// <summary>
			/// Analyzes encoding.
			/// </summary>
			public static void AnalyzeEncoding( string filePath, out Encoding encoding, out bool withBom )
			{
				Debug.Assert( filePath != null );

				const int MaxSize = 50 * 1024;
				Stream file = null;
				byte[] data;
				int dataSize;

				try
				{
					using( file = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) )
					{
						// prepare buffer
						if( MaxSize < file.Length )
							dataSize = MaxSize;
						else
							dataSize = (int)file.Length;
						data = new byte[ dataSize ];

						// read data at maximum 50KB
						file.Read( data, 0, dataSize );
						encoding = EncodingAnalyzer.Analyze( data, out withBom );
						if( encoding == null )
						{
							encoding = Encoding.Default;
							withBom = false;
						}

						return;
					}
				}
				catch( ArgumentException )
				{}
				catch( NotSupportedException )
				{}
				catch( UnauthorizedAccessException )
				{}
				catch( IOException )
				{}
				catch( System.Security.SecurityException )
				{}
				catch( OutOfMemoryException )
				{}
				encoding = Encoding.Default;
				withBom = false;
			}

			public static string AnalyzeEolCode( Document doc )
			{
				for( int i=0; i<doc.Length-1; i++ )
				{
					if( doc[i] == '\r' )
					{
						if( doc[i+1] == '\n' )
						{
							return "\r\n";
						}
						else
						{
							return "\r";
						}
					}
					else if( doc[i] == '\n' )
					{
						return "\n";
					}
				}
				return "\r\n";
			}
		}
		#endregion
	}
}
