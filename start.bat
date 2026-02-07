@echo off
echo Starting Artisan Studio...
echo.

:: Check for .NET
where dotnet >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo .NET SDK not found. Please install .NET 8: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

:: Check for Node.js
where npm >nul 2>nul
if %ERRORLEVEL% neq 0 (
    echo Node.js not found. Please install Node.js 18+: https://nodejs.org/
    pause
    exit /b 1
)

:: Install frontend dependencies if needed
if not exist "frontend\node_modules" (
    echo Installing frontend dependencies...
    cd frontend
    npm install
    cd ..
)

:: Check for API key
if "%REPLICATE_API_KEY%"=="" (
    echo WARNING: REPLICATE_API_KEY not set. Running in demo mode.
    echo Get a free API key at: https://replicate.com/account/api-tokens
    echo.
)

:: Start backend
echo Starting backend server...
start "Backend" cmd /c "cd backend && dotnet run --urls http://localhost:5000"

:: Wait for backend
timeout /t 3 /nobreak >nul

:: Start frontend
echo Starting frontend...
start "Frontend" cmd /c "cd frontend && npm run dev"

echo.
echo Artisan Studio is running!
echo.
echo   Frontend: http://localhost:3000
echo   Backend:  http://localhost:5000
echo.
echo Close this window to stop the servers.
pause
