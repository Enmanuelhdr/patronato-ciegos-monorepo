@echo off
title Iniciar Todo el Sistema (Core + Gateway + Web + Caja)
color 0e

cd /d "%~dp0"
set "PATH=C:\Program Files\nodejs;%PATH%"

echo ======================================================================
echo    PATRONATO NACIONAL DE CIEGOS - INICIANDO SISTEMA COMPLETO
echo ======================================================================
echo.
echo Cerrando instancias anteriores si existian...
taskkill /f /im Patronato.Caja.exe >nul 2>&1
taskkill /f /im Patronato.Gateway.exe >nul 2>&1
taskkill /f /im Patronato.Core.exe >nul 2>&1
taskkill /f /im Patronato.Web.exe >nul 2>&1
echo.

echo 1. Iniciando Patronato.Core (API Base de Datos en puerto 57141)...
start "Patronato.Core" cmd /k "cd /d "%~dp0" && dotnet run --project Patronato.Core"

timeout /t 3 /nobreak >nul

echo 2. Iniciando Patronato.Gateway (API Gateway en 57171 y Sockets en 6000)...
start "Patronato.Gateway" cmd /k "cd /d "%~dp0" && dotnet run --project Patronato.Gateway"

timeout /t 3 /nobreak >nul

echo 3. Iniciando Patronato.Web (Portal Web en http://localhost:5173)...
start "Patronato.Web" cmd /k "set PATH=C:\Program Files\nodejs;%%PATH%% && cd /d "%~dp0Patronato.Web" && npm run dev"

timeout /t 3 /nobreak >nul

echo 4. Abriendo pestañas en el navegador...
echo    - Portal Web:           http://localhost:5173
start http://localhost:5173

timeout /t 1 /nobreak >nul
echo    - Core Interactivo:     http://localhost:57141/monitor/operaciones
start http://localhost:57141/monitor/operaciones

timeout /t 1 /nobreak >nul
echo    - Core Swagger:         http://localhost:57141/swagger
start http://localhost:57141/swagger

timeout /t 1 /nobreak >nul
echo    - Gateway Swagger:      http://localhost:57171/swagger
start http://localhost:57171/swagger

timeout /t 2 /nobreak >nul

echo 5. Iniciando Patronato.Caja (Punto de Venta WPF)...
start "Patronato.Caja" cmd /k "cd /d "%~dp0" && dotnet run --project Patronato.Caja"

echo.
echo ======================================================================
echo  TODO EL SISTEMA HA SIDO INICIADO EXITOSAMENTE.
echo  - Web:              http://localhost:5173
echo  - Core Interactivo: http://localhost:57141/monitor/operaciones
echo  - Core Swagger:     http://localhost:57141/swagger
echo  - Gateway Swagger:  http://localhost:57171/swagger
echo  - Caja:             Ventana WPF conectada al Gateway
echo ======================================================================
echo.
pause
