@echo off
@setlocal enableextensions enabledelayedexpansion
REM --------------------------------------------------------------------------------------
REM 	SET PATHS
REM --------------------------------------------------------------------------------------

SET "OUTPUT_NAME=OTEX"
SET "SOLUTION_PATH=%~dp0.."
SET "BUILD_PATH=%~dp0..\bin"
SET "STAGING_PATH=%~dp0..\staging"
SET "OUTPUT_PATH=%STAGING_PATH%\%OUTPUT_NAME%"
SET "ZIP_PATH=%~dp0..\%OUTPUT_NAME%.zip"

REM  --------------------------------------------------------------------------------------
echo --- STAGING --------------------------------------------------------------------------
REM  --------------------------------------------------------------------------------------

IF EXIST "%STAGING_PATH%" (
	RMDIR /S /Q "%STAGING_PATH%"
	IF %ERRORLEVEL% NEQ 0 (
		ECHO DISTRIBUTE: Deleting old staging directory failed
		pause
		EXIT /B %ERRORLEVEL%
	)
)

MKDIR "%OUTPUT_PATH%"
xcopy "%BUILD_PATH%\x64\Release\*" "%OUTPUT_PATH%\x64" /S /I /Y /J
xcopy "%BUILD_PATH%\x86\Release\*" "%OUTPUT_PATH%\x86" /S /I /Y /J

REM  --------------------------------------------------------------------------------------
echo --- CLEANING -------------------------------------------------------------------------
REM  --------------------------------------------------------------------------------------

SET "CURRENT_DIR=%OUTPUT_PATH%\x64"

:CLEAN_DIR
IF NOT EXIST "%CURRENT_DIR%" (
	ECHO DISTRIBUTE: Path did not exist
	pause
	EXIT /B 1
)

DEL /F /Q "%CURRENT_DIR%\*.xml" "%CURRENT_DIR%\*.pdb" "%CURRENT_DIR%\*.log" "%CURRENT_DIR%\*.vshost*" "%CURRENT_DIR%\*.config"
IF %ERRORLEVEL% NEQ 0 (
	ECHO DISTRIBUTE: Deleting files failed ^(error %ERRORLEVEL%^)
	pause
	EXIT /B %ERRORLEVEL%
)

IF "%CURRENT_DIR%"=="%OUTPUT_PATH%\x64"  (
	SET "CURRENT_DIR=%OUTPUT_PATH%\x86"
	GOTO CLEAN_DIR
)

REM  --------------------------------------------------------------------------------------
echo --- ZIPPING --------------------------------------------------------------------------
REM  --------------------------------------------------------------------------------------

IF EXIST "%ZIP_PATH%" (
	DEL /F /Q "%ZIP_PATH%"
	IF %ERRORLEVEL% NEQ 0 (
		ECHO DISTRIBUTE: Deleting old zip file failed
		pause
		EXIT /B %ERRORLEVEL%
	)
)

CD "%STAGING_PATH%"
zip -S -9 -r "%ZIP_PATH%" "%OUTPUT_NAME%"

REM  --------------------------------------------------------------------------------------
echo --- CLEANING UP ----------------------------------------------------------------------
REM  --------------------------------------------------------------------------------------

CD "%~dp0"
RMDIR /S /Q "%STAGING_PATH%"
IF EXIST "%STAGING_PATH%" (
	timeout /t 2 /nobreak > NUL
	RMDIR /S /Q "%STAGING_PATH%"
	IF %ERRORLEVEL% NEQ 0 (
		ECHO DISTRIBUTE: Deleting staging directory failed
		pause
		EXIT /B %ERRORLEVEL%
	)
)

@endlocal
EXIT /B 0