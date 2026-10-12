$ErrorActionPreference = 'Stop'
$cfg = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../SistemaGestionFerreteria/appsettings.json') -Raw | ConvertFrom-Json
if ($cfg.Afip.Ambiente -ne 'Homologacion') { throw 'Solo homologación.' }
$file = Get-ChildItem -LiteralPath $cfg.Afip.RutaTicket -Filter '*.bin' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $file) { throw 'No hay ticket persistido.' }
$clear = [System.Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($file.FullName), $null, [System.Security.Cryptography.DataProtectionScope]::CurrentUser)
$login = [Text.Encoding]::UTF8.GetString($clear) | ConvertFrom-Json
$token = [Security.SecurityElement]::Escape($login.Token)
$sign = [Security.SecurityElement]::Escape($login.Sign)
$body = '<?xml version="1.0" encoding="UTF-8"?><soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/"><soapenv:Body><ar:FECompUltimoAutorizado><ar:Auth><ar:Token>' + $token + '</ar:Token><ar:Sign>' + $sign + '</ar:Sign><ar:Cuit>' + $cfg.Afip.Cuit.Replace('-','') + '</ar:Cuit></ar:Auth><ar:PtoVta>8</ar:PtoVta><ar:CbteTipo>6</ar:CbteTipo></ar:FECompUltimoAutorizado></soapenv:Body></soapenv:Envelope>'
try {
    $response = Invoke-WebRequest -Uri $cfg.Afip.WsfeUrl -Method Post -ContentType 'text/xml; charset=utf-8' -Headers @{SOAPAction='"http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado"'} -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 20 -SkipHttpErrorCheck
    Write-Output ('HTTP: ' + [int]$response.StatusCode + '. Content-Type: ' + $response.Headers['Content-Type'])
    if ([int]$response.StatusCode -ge 400) {
        $safe = $response.Content.Replace($login.Token, '[TOKEN]').Replace($login.Sign, '[SIGN]').Replace($token, '[TOKEN]').Replace($sign, '[SIGN]')
        Write-Output $safe.Substring(0, [Math]::Min(2500, $safe.Length))
        return
    }
    $xml = [xml]$response.Content
    $errors = $xml.SelectNodes('//*[local-name()="Err"]')
    foreach ($err in $errors) { Write-Output ($err.Code.ToString() + ' - ' + $err.Msg) }
    $number = $xml.SelectSingleNode('//*[local-name()="CbteNro"]')
    if ($number) { Write-Output ('WSFE responde. Última Factura B: ' + $number.InnerText) }
} catch { throw ('WSFE: ' + $_.Exception.Message) }
