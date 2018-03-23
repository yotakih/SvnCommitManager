@echo off

set bindir=%homedrive%%homepath%\Documents\Visual Studio 2015\Projects\SvnCommitManager\SvnCommitManager\bin\x86\Release
set binname=SvnCommitManager.exe

rem コピー元リポジトリを指定
set src=C:\Temp\svn\testSrc\srcDir
rem コピー元リポジトリのログインユーザを指定
set user=user
rem コピー元リポジトリのログインパスワードを指定
set psswd=user
rem 抽出ファイルのフィルタリストファイルパスを指定
set filterFl=%CD%\filter.txt
rem 抽出開始リビジョンを指定
set strtRev=1
rem 抽出終了リビジョンを指定
set endRev=20
rem 削除操作のファイルは対象外にする場合、以下の行のコメントをはずす
set excDel=-excDel
rem コピー元⇒コピー先へ抽出ファイルをコミットするときのバッチファイルのテンプレートファイルのパスを指定
set tmpbat=%CD%\tmpbat.bat
rem 作業フォルダパスを指定（存在しないフォルダを指定。処理中に自動作成される）（抽出ファイルを管理）
set workDir=C:\Temp\svn\topath\20170717

"%bindir%\%binname%" -src "%src%" -user "%user%" -psswd "%psswd%" -filterFl "%filterFl%" -strtRev %strtRev% -endRev %endRev% -tmpbat "%tmpbat%" -workDir "%workDir%" %excDel%

pause
