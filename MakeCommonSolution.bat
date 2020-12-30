:: Src/Common 폴더 해당 프로젝트에 복사

@echo off
set /p str=대상 프로젝트 경로 : 

if not exist %str% (
	echo %str%는 존재하지 않습니다.
	goto EXIT
)

set targetdir=%str%\Common\

xcopy ".\Src\Common\" %targetdir% /y /e /s

echo %targetdir%에 CommonPackage 생성 완료

:EXIT
pause>nul