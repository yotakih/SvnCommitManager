@echo off

set 01=C:\temp\svn\testDist\dstDir
set 02=C:\temp\svn\testDist2\dstDir2

set lst=01,02


cd %~dp0
setlocal enabledelayedexpansion

for %%i in (%lst%) do (
  call ".\10_抽出ファイル\{0}" "!%%i!"
)

endlocal

pause
