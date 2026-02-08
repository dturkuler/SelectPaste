@echo off
setlocal

set DOTNET_CMD=dotnet

if exist "%~dp0.dotnet\dotnet.exe" (
    echo [INFO] Using portable .NET SDK from .dotnet folder
    set DOTNET_CMD="%~dp0.dotnet\dotnet.exe"
) else (
    echo Checking for global .NET SDK...
    dotnet --list-sdks >nul 2>&1
    if %errorlevel% neq 0 (
        echo [ERROR] .NET SDK not found!
        echo.
        echo Please install the .NET 8.0 SDK or use the portable install.
        pause
        exit /b 1
    )
)

echo.
echo Found .NET SDK. Building SelectPaste...
echo.

%DOTNET_CMD% publish -r win-x64 -p:PublishSingleFile=true --self-contained -c Release

if %errorlevel% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo Build successful!
echo Executable located at: bin\Release\net8.0-windows\win-x64\publish\SelectPaste.exe
echo.
pause
