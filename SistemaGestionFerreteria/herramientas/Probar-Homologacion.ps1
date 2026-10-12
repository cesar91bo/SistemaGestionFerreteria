$ErrorActionPreference = 'Stop'
$config = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../SistemaGestionFerreteria/appsettings.json') -Raw | ConvertFrom-Json
if ($config.Afip.Ambiente -ne 'Homologacion' -or $config.Afip.WsaaUrl -ne 'https://wsaahomo.afip.gov.ar/ws/services/LoginCms' -or $config.Afip.WsfeUrl -ne 'https://wswhomo.afip.gov.ar/wsfev1/service.asmx') { throw 'Solo se permiten los endpoints de homologación.' }
$password = $config.Afip.Certificado.PasswordPfx
if ($config.Afip.Certificado.RutaPasswordProtegida) {
    $secure = (Get-Content -LiteralPath $config.Afip.Certificado.RutaPasswordProtegida -Raw).Trim() | ConvertTo-SecureString
    $password = [System.Net.NetworkCredential]::new('', $secure).Password
}
$cert = [System.Security.Cryptography.X509Certificates.X509CertificateLoader]::LoadPkcs12FromFile($config.Afip.Certificado.RutaPfx, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
$now = [DateTimeOffset]::UtcNow
$tra = '<loginTicketRequest version="1.0"><header><uniqueId>' + $now.ToUnixTimeSeconds() + '</uniqueId><generationTime>' + $now.AddMinutes(-10).ToString('o') + '</generationTime><expirationTime>' + $now.AddHours(12).ToString('o') + '</expirationTime></header><service>wsfe</service></loginTicketRequest>'
$cms = [System.Security.Cryptography.Pkcs.SignedCms]::new([System.Security.Cryptography.Pkcs.ContentInfo]::new([Text.Encoding]::UTF8.GetBytes($tra)), $false)
$signer = [System.Security.Cryptography.Pkcs.CmsSigner]::new($cert)
$signer.IncludeOption = [System.Security.Cryptography.X509Certificates.X509IncludeOption]::EndCertOnly
$cms.ComputeSignature($signer)
$encoded = [Convert]::ToBase64String($cms.Encode())
$body = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:wsaa="http://wsaa.view.sua.dvadac.desein.afip.gov"><soapenv:Body><wsaa:loginCms><wsaa:in0>' + $encoded + '</wsaa:in0></wsaa:loginCms></soapenv:Body></soapenv:Envelope>'
try {
    $response = Invoke-WebRequest -Uri $config.Afip.WsaaUrl -Method Post -ContentType 'text/xml; charset=utf-8' -Headers @{SOAPAction='""'} -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 30
} catch {
    $detail = $_.ErrorDetails.Message
    if ($detail) { try { $fault = [xml]$detail; $message = $fault.SelectSingleNode('//*[local-name()="faultstring"]').InnerText } catch { $message = 'Respuesta HTTP no SOAP.' } }
    throw ('WSAA: ' + $_.Exception.Message + ' ' + $message)
}
$soap = [xml]$response.Content
$ticketText = $soap.SelectSingleNode('//*[local-name()="loginCmsReturn"]').InnerText
if (-not $ticketText) { throw 'WSAA no devolvió ticket.' }
$ticket = [xml]$ticketText
$token = $ticket.loginTicketResponse.credentials.token
$sign = $ticket.loginTicketResponse.credentials.sign
if (-not $token -or -not $sign) { throw 'WSAA no devolvió Token/Sign.' }
Write-Output ('Login WSAA correcto. Vencimiento: ' + $ticket.loginTicketResponse.header.expirationTime)
foreach ($type in @(6, 1)) {
    $auth = '<ar:Auth><ar:Token>' + [Security.SecurityElement]::Escape($token) + '</ar:Token><ar:Sign>' + [Security.SecurityElement]::Escape($sign) + '</ar:Sign><ar:Cuit>' + $config.Afip.Cuit.Replace('-','') + '</ar:Cuit></ar:Auth>'
    $body = '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" xmlns:ar="http://ar.gov.afip.dif.FEV1/"><soapenv:Body><ar:FECompUltimoAutorizado>' + $auth + '<ar:PtoVta>' + $config.Afip.PuntoVenta + '</ar:PtoVta><ar:CbteTipo>' + $type + '</ar:CbteTipo></ar:FECompUltimoAutorizado></soapenv:Body></soapenv:Envelope>'
    $response = Invoke-WebRequest -Uri $config.Afip.WsfeUrl -Method Post -ContentType 'text/xml; charset=utf-8' -Headers @{SOAPAction='"http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado"'} -Body ([Text.Encoding]::UTF8.GetBytes($body)) -TimeoutSec 30
    $xml = [xml]$response.Content
    $errors = $xml.SelectNodes('//*[local-name()="Err"]')
    if ($errors.Count -gt 0) { foreach ($err in $errors) { Write-Output ('WSFE tipo ' + $type + ': ' + $err.Code + ' - ' + $err.Msg) }; continue }
    $number = $xml.SelectSingleNode('//*[local-name()="CbteNro"]')
    if (-not $number) { throw 'WSFE no devolvió CbteNro.' }
    Write-Output ('WSFE correcto. Punto de venta ' + $config.Afip.PuntoVenta + ', tipo ' + $type + ', último autorizado ' + $number.InnerText)
}
$cert.Dispose()
