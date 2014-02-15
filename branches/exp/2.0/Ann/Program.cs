using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

namespace Sgry.Ann
{
	class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			bool createdNew;
			var initOpenFilePaths = new List<string>();

			// get mutex object to control application instance
			using( var mutex = new Mutex(true, AppLogic.AppInstanceMutexName, out createdNew) )
			{
				// parse arguments
				for( int i=0; i<args.Length; i++ )
				{
					initOpenFilePaths.Add( args[i] );
				}

				// if another instance already exists, activate it and exit
				if( createdNew )
				{
					Main_LaunchFirstInstance( initOpenFilePaths.ToArray() );
					mutex.ReleaseMutex();
				}
				else
				{
					Main_ActivateFirstInstance( initOpenFilePaths.ToArray() );
				}
			}
		}

		static void Main_LaunchFirstInstance( string[] initOpenFilePaths )
		{
			AppLogic app;

			// launch new application instance
			using( app = new AppLogic(initOpenFilePaths) )
			{
				app.MainForm = new AnnForm( app );
				app.LoadConfig( true );

				Application.EnableVisualStyles();
				Application.Run( app.MainForm );
			}
		}

		static void Main_ActivateFirstInstance( string[] initOpenFilePaths )
		{
			PseudoPipe pipe = new PseudoPipe();

			// write IPC file to tell existing instance what user wants to do
			try
			{
				pipe.Connect( AppLogic.IpcFilePath );
				pipe.WriteLine( "Activate", 10000 );
				foreach( string path in initOpenFilePaths )
				{
					pipe.WriteLine( "OpenDocument " + path, 10000 );
				}
			}
			catch
			{}
			finally
			{
				pipe.Dispose();
			}
		}
	}
}
