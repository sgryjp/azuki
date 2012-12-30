using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Windows.Forms;
using Path = System.IO.Path;

namespace Sgry.Ann
{
	static partial class Actions
	{
		/// <summary>
		/// Shows current memory usage.
		/// </summary>
		public static AnnAction ShowMemoryUsage
			= delegate( AppLogic app )
		{
			StringBuilder message = new StringBuilder( 256 );

			// make message (getting total memory also invokes GC)
			message.AppendFormat( null, "Total memory: {0:#,#} Bytes\n", GC.GetTotalMemory(true) );
			message.AppendFormat( null, "----\n" );
			foreach( Document doc in app.Documents )
			{
				message.AppendFormat( null, "  {0}: {1:#,#} Bytes\n", doc.DisplayName, doc.MemoryUsage );
			}

			// show usage
			MessageBox.Show( message.ToString(), "Estimated memory usage" );
		};

		/// <summary>
		/// Shows the "About" dialog.
		/// </summary>
		public static AnnAction ShowAboutDialog
			= delegate( AppLogic app )
		{
			AssemblyName	annAsmName;
			string			annNameStr;
			string			annVerStr;
			Version			azukiAsmVer;
			string			azukiNameStr;
			string			azukiVerStr;
			string			message;

			try
			{
				// extract file name and version of Ann
				annAsmName = Assembly.GetExecutingAssembly().GetName();
				annNameStr = annAsmName.Name;
				annVerStr = annAsmName.Version.Major
					+ "." + annAsmName.Version.Minor
					+ "." + annAsmName.Version.Build
					+ " rev. " + annAsmName.Version.Revision;

				// extract file name and version of Azuki
				azukiAsmVer = app.Azuki.Version;
				azukiNameStr = typeof(Azuki.Document).Module.Name;
				azukiVerStr = azukiAsmVer.Major
					+ "." + azukiAsmVer.Minor
					+ "." + azukiAsmVer.Build
					+ " rev. " + azukiAsmVer.Revision;

				message = String.Format( "{0} {1}\nwith {2} {3}",
						annNameStr, annVerStr,
						azukiNameStr, azukiVerStr
					);
				MessageBox.Show( message, annNameStr );
			}
			catch( Exception ex )
			{
				MessageBox.Show( "failed to get assembly versions!\n" + ex.ToString(), "Ann bug",
					MessageBoxButtons.OK, MessageBoxIcon.Hand, MessageBoxDefaultButton.Button1 );
			}
		};
	}
}
