<#
Envia una transaccion al socket de la Caja (CajaSocketServer) y muestra la respuesta.
Postman no soporta sockets TCP crudos, por eso este script cubre esa parte.

Ejemplos:
  .\probar-socket.ps1
  .\probar-socket.ps1 -CodigoTransaccion PRODUCTO_VENDER -DatosJson '{"productoId":1,"cantidad":2}'
  .\probar-socket.ps1 -CodigoTransaccion PRESTAMO_ABONAR -DatosJson '{"prestamoId":1,"monto":200}'
  .\probar-socket.ps1 -CodigoTransaccion MASAJE_COBRAR -DatosJson '{"id":1}'
#>
param(
    [string]$CodigoTransaccion = "DONACION_REGISTRAR",
    [string]$DatosJson = '{"donante":"Juan Perez","rncCedula":"001-1234567-8","monto":500,"metodoPago":"Efectivo"}',
    [string]$Servidor = "localhost",
    [int]$Puerto = 6000
)

$stan = "{0:000000}" -f (Get-Random -Minimum 1 -Maximum 999999)

$solicitud = [ordered]@{
    mti               = "0200"
    stan              = $stan
    codigoTransaccion = $CodigoTransaccion
    datos             = ($DatosJson | ConvertFrom-Json)
} | ConvertTo-Json -Depth 5 -Compress

$client = New-Object System.Net.Sockets.TcpClient($Servidor, $Puerto)
$stream = $client.GetStream()
$writer = New-Object System.IO.StreamWriter($stream)
$writer.AutoFlush = $true
$reader = New-Object System.IO.StreamReader($stream)

Write-Host "-> $solicitud" -ForegroundColor Cyan
$writer.WriteLine($solicitud)
$respuesta = $reader.ReadLine()
Write-Host "<- $respuesta" -ForegroundColor Green

$client.Close()
