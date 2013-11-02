@echo off
setlocal

set _OPT=-nologo -v:m -t:Build
set _OPT=%_OPT% -p:Configuration=Release
set _OPT=%_OPT% -clp:ForceNoAlign;ShowCommandLine
set _SOLUTION_FILE=%~1

if "%~1"=="" (
	echo # no solution file was specified; using All.vs10.sln.
	set _SOLUTION_FILE=Azuki.vs10.sln
)

:: build
echo ===== begin msbuild =====
msbuild.exe "%_SOLUTION_FILE%" %_OPT%
echo ===== end msbuild =====
