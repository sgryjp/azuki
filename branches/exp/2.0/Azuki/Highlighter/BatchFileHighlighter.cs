namespace Sgry.Azuki.Highlighter
{
	class BatchFileHighlighter : KeywordHighlighter
	{
		public BatchFileHighlighter()
		{
			AddKeywordSet( new[] {
				"assoc", "attrib", "bcdedit", "break", "cacls", "call", "cd",
				"chcp", "chdir", "chkdsk", "chkntfs", "cls", "cmd", "color",
				"comp", "compact", "convert", "copy", "date", "del",
				"dir", "diskcomp", "diskcopy", "diskpart", "doskey",
				"driverquery", "echo", "echo.", "endlocal", "erase", "exit",
				"fc", "find", "findstr", "for", "format", "fsutil", "ftype",
				"goto", "gpresult", "graftabl", "icacls", "if", "label", "md",
				"mkdir", "mklink", "mode", "more", "move", "openfiles", "path",
				"pause", "popd", "print", "prompt", "pushd", "rd", "recover",
				"ren", "rename", "replace", "rmdir", "robocopy", "sc",
				"schtasks", "set", "setlocal", "shift", "shutdown", "sort",
				"start", "subst", "systeminfo", "taskkill", "tasklist", "time",
				"title", "tree", "type", "ver", "verify", "wmic", "xcopy"
			}, CharClass.Keyword, true );

			AddKeywordSet( new[] {
				"AUX",
				"COM0", "COM1", "COM2", "COM3", "COM4",
				"COM5", "COM6", "COM7", "COM8", "COM9",
				"CON",
				"LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
				"LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
				"NUL", "PRN"
			}, CharClass.Keyword2, false );

			AddLineHighlight( "REM", CharClass.Comment, true );
			AddLineHighlight( "::", CharClass.Comment );

			AddRegex( @"(?<=if )not", true, CharClass.Keyword );
			AddRegex( @"@?echo (on|off)", true, CharClass.Keyword2 );
			AddRegex( @"%\w+(:[^%]+)?%", false, CharClass.Variable );
			AddRegex( @"%~?[0-9\*]", false, CharClass.Variable );
			AddRegex( @"%%?~?[fdpnxsatz]*[0-9a-zA-Z]", false, CharClass.Variable );
			AddRegex( @"^:\w+", true, CharClass.Label );
		}
	}
}
