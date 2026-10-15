@echo off
title Cerrar Todos los Procesos del Patronato
echo Cerrando instancias de Patronato.Core, Gateway, Caja y Web...
taskkill /f /im Patronato.Caja.exe >nul 2>&1
taskkill /f /im Patronato.Gateway.exe >nul 2>&1
taskkill /f /im Patronato.Core.exe >nul 2>&1
taskkill /f /im Patronato.Web.exe >nul 2>&1
echo Todos los procesos han sido cerrados correctamente.
timeout /t 2 >nul
