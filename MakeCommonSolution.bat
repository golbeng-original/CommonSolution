:: Src/Common ���� �ش� ������Ʈ�� ����

@echo off
set /p str=��� ������Ʈ ��� : 

if not exist %str% (
	echo %str%�� �������� �ʽ��ϴ�.
	goto EXIT
)

set targetdir=%str%\Common\

xcopy ".\Src\Common\" %targetdir% /y /e /s

echo %targetdir%�� CommonPackage ���� �Ϸ�

:EXIT
pause>nul