@echo off

set bindir=%homedrive%%homepath%\Documents\Visual Studio 2015\Projects\SvnCommitManager\SvnCommitManager\bin\x86\Release
set binname=SvnCommitManager.exe

rem �R�s�[�����|�W�g�����w��
set src=C:\Temp\svn\testSrc\srcDir
rem �R�s�[�����|�W�g���̃��O�C�����[�U���w��
set user=user
rem �R�s�[�����|�W�g���̃��O�C���p�X���[�h���w��
set psswd=user
rem ���o�t�@�C���̃t�B���^���X�g�t�@�C���p�X���w��
set filterFl=%CD%\filter.txt
rem ���o�J�n���r�W�������w��
set strtRev=1
rem ���o�I�����r�W�������w��
set endRev=20
rem �폜����̃t�@�C���͑ΏۊO�ɂ���ꍇ�A�ȉ��̍s�̃R�����g���͂���
set excDel=-excDel
rem �R�s�[���˃R�s�[��֒��o�t�@�C�����R�~�b�g����Ƃ��̃o�b�`�t�@�C���̃e���v���[�g�t�@�C���̃p�X���w��
set tmpbat=%CD%\tmpbat.bat
rem ��ƃt�H���_�p�X���w��i���݂��Ȃ��t�H���_���w��B�������Ɏ����쐬�����j�i���o�t�@�C�����Ǘ��j
set workDir=C:\Temp\svn\topath\20170717

"%bindir%\%binname%" -src "%src%" -user "%user%" -psswd "%psswd%" -filterFl "%filterFl%" -strtRev %strtRev% -endRev %endRev% -tmpbat "%tmpbat%" -workDir "%workDir%" %excDel%

pause
