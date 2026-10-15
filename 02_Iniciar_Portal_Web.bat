@echo off
title Portal Web Patronato (React + Vite)
color 0b

echo ======================================================================
echo    PATRONATO NACIONAL DE CIEGOS - PORTAL WEB (REACT + VITE)
echo ======================================================================
echo.
echo Iniciando servidor de desarrollo en http://localhost:5173 ...
echo.

set "PATH=C:\Program Files\nodejs;%PATH%"
cd /d "%~dp0Patronato.Web"
start http://localhost:5173
call npm run dev
pause
