@echo off
title Iniciar Sistema Patronato (Core + Gateway + Caja)
color 0a

echo ======================================================================
echo    PATRONATO NACIONAL DE CIEGOS - INICIANDO SISTEMA
echo ======================================================================
echo.
echo 1. Iniciando Patronato.Core (Puerto 57141)...
start "Patronato.Core" cmd /k "dotnet run --project Patronato.Core"

timeout /t 3 /nobreak >nul

echo 2. Iniciando Patronato.Gateway (Puerto 57171 y Sockets 6000)...
start "Patronato.Gateway" cmd /k "dotnet run --project Patronato.Gateway"

timeout /t 3 /nobreak >nul

echo 3. Iniciando Patronato.Caja (Punto de Venta WPF)...
start "Patronato.Caja" cmd /k "dotnet run --project Patronato.Caja"

echo.
echo Todo el sistema ha sido iniciado correctamente.
echo Ya puedes operar la Caja conectada al Gateway.
echo.
pause
