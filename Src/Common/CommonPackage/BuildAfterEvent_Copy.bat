:: xcopy ����� �Ǵ� �κ��� ������Ʈ�� �ð� �����Ͽ� ����Ѵ�.

echo "Start BuildAfterEvent Copy"

xcopy ".\bin\Release\netstandard2.0\CommonPackage.dll" ..\..\TestTarget\ /y /e /s
xcopy ".\bin\Release\netstandard2.0\CommonPackage.dll" ..\..\TestTarget2\ /y /e /s