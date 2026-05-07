@echo off
setlocal
:: %~dp0 = 이 스크립트가 있는 폴더 (scripts\), 루트는 그 상위
set ROOT=%~dp0..

echo [Exhibition] Publishing ASP.NET server for Windows x64...

dotnet publish "%ROOT%\Server-AspNet\ExhibitionServer\ExhibitionServer.csproj" ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output "%ROOT%\Build\publish"

if %ERRORLEVEL% neq 0 (
    echo [Error] Server publish failed.
    exit /b %ERRORLEVEL%
)

echo.
echo [Done] Created self-contained server at Build\publish\ExhibitionServer.exe
echo The packaged Unreal client (Build\Windows\) can launch this server automatically.
pause
