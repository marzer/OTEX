@echo off
@setlocal enableextensions enabledelayedexpansion
REM --------------------------------------------------------------------------------------
REM 	SET PATHS
REM --------------------------------------------------------------------------------------

SET "OUTPUT_NAME=OTEX"
SET "SOLUTION_PATH=%~dp0.."
SET "README_PATH=%SOLUTION_PATH%\OTEX Readme.pdf"
SET "BUILD_PATH=%SOLUTION_PATH%\bin"
SET "LEGAL_PATH=%SOLUTION_PATH%\licenses"
SET "STAGING_PATH=%SOLUTION_PATH%\staging"
SET "OUTPUT_PATH=%STAGING_PATH%\%OUTPUT_NAME%"
SET "ZIP_PATH=%SOLUTION_PATH%\%OUTPUT_NAME%.zip"

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
xcopy "%LEGAL_PATH%\*" "%OUTPUT_PATH%\licenses" /S /I /Y /J
xcopy "%README_PATH%" "%OUTPUT_PATH%" /Y /J /F

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

DEL /F /Q "%CURRENT_DIR%\*.xml" "%CURRENT_DIR%\*.pdb" "%CURRENT_DIR%\*.log" "%CURRENT_DIR%\*.vshost*" ^
	"%CURRENT_DIR%\*.config" "%CURRENT_DIR%\*.txt" "%CURRENT_DIR%\plugins\*.xml" "%CURRENT_DIR%\plugins\*.pdb" ^
	"%CURRENT_DIR%\plugins\*.log" "%CURRENT_DIR%\plugins\*.vshost*" "%CURRENT_DIR%\plugins\*.config" "%CURRENT_DIR%\plugins\*.txt"
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