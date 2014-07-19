﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

[assembly: AssemblyTitle("Azuki Text Editor Engine")]

#if !PocketPC
[assembly: AssemblyProduct("Azuki")]
[assembly: AssemblyDescription("Azuki Text Editor Engine for .NET Framework")]
#else
[assembly: AssemblyProduct("Azuki")]
[assembly: AssemblyDescription("Azuki Text Editor Engine for .NET Compact Framework")]
#endif

[assembly: AssemblyCompany("Suguru YAMAMOTO")]
[assembly: AssemblyCopyright("Copyright (C) 2007-2014, Suguru YAMAMOTO")]

[assembly: ComVisible(true)]
[assembly: Guid("272d419b-f573-42b4-9b0b-32cc5fd85a31")]
[assembly: AssemblyVersion("1.7.11.*")]
