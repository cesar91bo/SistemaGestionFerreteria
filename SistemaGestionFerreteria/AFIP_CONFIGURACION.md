# Facturación electrónica MIL-DAN

La configuración actual usa PRODUCCIÓN, CUIT 20252443011, servicio WSAA `wsfe` y punto de venta 8. La empresa es responsable inscripto. El formulario propone Factura B y permite Factura A. Factura C no está habilitada. Guardar una factura nueva solicita autorización real en ARCA.

## Circuito

- Factura A: código WSFE 1; B: 6. Notas de crédito A/B: 3/8 según la factura original seleccionada.
- Los presupuestos y remitos conservan numeración local y no generan ingresos ficticios en caja.
- Desmarcar “Autorizar en ARCA” en una factura A/B genera una Factura X (tipo interno 6), con numeración local independiente y sin ninguna llamada a ARCA. También puede elegirse Factura X directamente. El tipo interno 6 no es el código WSFE 6 de Factura B.
- Factura X registra la venta y, si es en efectivo y la aplicación está en producción, el ingreso en caja dentro de la misma transacción. Mercado Pago y otros medios no incrementan el efectivo físico. La Factura X puede anularse localmente y revertir su ingreso de efectivo.
- El detalle restaura la forma de pago guardada; el vuelto solo se muestra para efectivo. Los nuevos comprobantes conservan el monto recibido. En históricos sin ese dato se muestra el monto aplicado, sin inventar vuelto.
- La numeración fiscal se obtiene de ARCA por ambiente, CUIT, punto de venta y código de comprobante.
- Se guarda una solicitud sin credenciales antes de enviarla. Un corte deja el intento pendiente; se recupera desde el detalle o listado con “Consultar / reintentar ARCA”.
- La consulta verifica serie, receptor, fecha, importes, IVA y comprobantes asociados antes de recuperar el CAE.
- Un envío pendiente bloquea otro de su misma serie. No se cambia su solicitud al reenviarlo.
- Un rechazo explícito permite corregir los datos del cliente y reintentar. La fecha y numeración se vuelven a validar.
- Una factura autorizada no se anula localmente: se emite una nota de crédito asociada. No se puede acreditar más que el total original.
- En producción el CAE y el movimiento de efectivo se guardan en la misma transacción, una sola vez. En homologación no se mueve la caja.
- Los precios incluyen IVA. Los importes se redondean a dos decimales; se soportan 10,5 %, 21 %, 27 % y exentos. El concepto actual es venta de productos, en pesos.
- El límite de identificación de consumidor final es configurable en `Afip:LimiteConsumidorFinalSinDocumento`, actualmente $10.000.000. Para otros regímenes se exige CUIT válido.

## Certificado y ticket

El PFX de producción y la contraseña protegida están en `herramientas/certificado-produccion`, fuera de `wwwroot` y excluidos de Git. `Afip:Certificado:RutaPasswordProtegida` apunta a la contraseña protegida con DPAPI para la cuenta Windows actual; no hay contraseña PFX en texto plano en appsettings. Se reutilizó el certificado de producción existente, vigente hasta el 23/03/2027, y se verificó su firma.

La configuración anterior de pruebas se conserva en `appsettings.Homologacion.json`; no se carga con el ambiente ASP.NET Development o Production. Los certificados y tickets de ambos ambientes se conservan separados. La validación impide usar un certificado de homologación en producción.

Si se instala bajo otra cuenta Windows o en otra máquina, hay que volver a proteger la contraseña para esa cuenta o provisionar el PFX con su secreto mediante configuración segura. No se deben publicar ni adjuntar la clave privada, los PFX o los tickets.

El ticket WSAA se cachea por endpoint, servicio, CUIT y huella del certificado. Se conserva cifrado con DPAPI en `Afip:RutaTicket` para sobrevivir reinicios. Un bloqueo de archivo y un semáforo evitan logins simultáneos. No se muestran Token/Sign ni respuestas SOAP completas en errores.

Los scripts antiguos que llaman WSAA directamente pueden obtener un ticket que el servidor de la aplicación no conoce. Usar la pantalla `/afip/prueba` o la suite de pruebas para obtener tickets persistidos.

## Base de datos

Se aplicaron `AgregarCamposAfipFactura` y `CompletarCircuitoAfip` a la base local después de un respaldo COPY_ONLY verificado. La migración añade identificación de operaciones, ambiente, solicitud y referencia a caja/factura original. Los datos históricos se conservan como registros locales.

La fábrica de migraciones lee la conexión de la aplicación y admite `ConnectionStrings__DefaultConnection`. Ya no usa una instancia SQL distinta fija.

`script_instalacion_bd.sql` es idempotente e incluye todas las migraciones. Para otra instalación, revisar conexión y respaldar antes de aplicar:

```powershell
dotnet ef database update --project SistemaGestionFerreteria.Infrastructure --startup-project SistemaGestionFerreteria
```

## Verificación

```powershell
dotnet build SistemaGestionFerreteria/SistemaGestionFerreteria.csproj
dotnet run --project PruebasAfip/PruebasAfip.csproj -- --sql
```

La opción `--sql` crea y elimina una base temporal independiente, sin cambiar ventas de la base local. Verifica emisión, rechazo, recuperación, idempotencia, caja, notas de crédito y separación de ambientes. Las solicitudes A/B también se validan contra una copia del XSD oficial de homologación.

La opción adicional `--arca` emite comprobantes sintéticos A/B únicamente en homologación y consulta sus CAE. Nunca se permite esa opción en producción. Si el envío remota devuelve timeout, no asumir que fue rechazado: consultar antes de reenviar.

Resultado del 12/10/2026: compilación correcta, 36 comprobaciones aprobadas en la suite y las tres pantallas respondieron HTTP 200. WSAA obtuvo y persistió un ticket válido. Las consultas WSFE posteriores agotaron el tiempo de espera tanto desde .NET como desde PowerShell; no se llegó a emitir los comprobantes reales de homologación. La validación del CAE real queda pendiente.

## Producción e impresión

Se activó producción con su propio certificado y estos endpoints:

- WSAA: https://wsaa.afip.gov.ar/ws/services/LoginCms
- WSFE: https://servicios1.afip.gov.ar/wsfev1/service.asmx

Cambiar conjuntamente ambiente, endpoints, certificado y rutas accesibles a la cuenta del servidor. No reutilizar certificados de homologación para producción.

Verificación de producción del 12/10/2026: login WSAA correcto, relación con WSFE válida y punto de venta 8 habilitado/no bloqueado para CAE de responsable inscripto. Última Factura B autorizada: 15; próxima consultada: 16. Última Factura A: 0; próxima: 1. Los próximos números se consultan nuevamente en cada emisión, no se fijaron esos valores en código. No se emitió ninguna factura durante esta verificación.

Para repetir exclusivamente las consultas de producción, sin emitir comprobantes:

```powershell
dotnet run --project PruebasAfip/PruebasAfip.csproj -- --produccion-check
```

Antes de ejecutar la aplicación actualizada, detener y recompilar/reiniciar Visual Studio para cargar la nueva configuración y el aviso de producción. Para trasladar al servidor del cliente, reprovisionar el secreto bajo su cuenta Windows: el archivo protegido con DPAPI de esta computadora no puede copiarse como si fuera una contraseña portable.

La impresión fiscal/QR en tickeadoras sigue pendiente por pedido del usuario. No se agregó un formato A4.

Se añadió la migración `GuardarMontoRecibido`: únicamente agrega `Facturas.MontoRecibido` nullable decimal(18,2). Aplicada a la base local tras otro respaldo verificado; no cambia pagos ni importes históricos. La suite final de pago/Factura X aprobó 46 comprobaciones en una base temporal, sin emisiones a ARCA.
