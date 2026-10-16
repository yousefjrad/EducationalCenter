@echo off
title Educational Center Launcher

echo Launching Backend (.NET)...
start "Backend" /min cmd /c "cd /d C:\Users\HP\Desktop\EducationalCenter && dotnet run --project src\educationalcenter.api"

echo Launching Frontend (Vite)...
start "Frontend" /min cmd /c "cd /d C:\Users\HP\Desktop\EducationalCenter-Front-End && npm run dev"

echo Waiting for servers to start...
timeout /t 8 /nobreak >nul

echo Opening Browser in dedicated window...
:: تشغيل كروم وتجميد السكربت هنا حتى تطلق النافذة
start /wait chrome.exe --app=http://localhost:3000 --user-data-dir="%temp%\dev_chrome_profile"

echo Browser closed. Cleaning up background processes...
taskkill /f /im dotnet.exe >nul 2>&1
taskkill /f /im node.exe >nul 2>&1
exit