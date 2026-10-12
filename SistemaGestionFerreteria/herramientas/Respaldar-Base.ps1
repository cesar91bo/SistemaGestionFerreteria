$ErrorActionPreference = 'Stop'
$cfg = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../SistemaGestionFerreteria/appsettings.json') -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.DefaultConnection)
$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))"
    $folder = [string]$command.ExecuteScalar()
    if (-not $folder) { throw 'No se encontró la carpeta de backups de SQL Server.' }
    $backup = Join-Path $folder ($builder.InitialCatalog + '_antes_afip_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.bak')
    $command.CommandText = 'BACKUP DATABASE @database TO DISK=@path WITH COPY_ONLY, CHECKSUM;'
    $command.Parameters.AddWithValue('@database', $builder.InitialCatalog) | Out-Null
    $command.Parameters.AddWithValue('@path', $backup) | Out-Null
    $command.CommandTimeout = 120
    $command.ExecuteNonQuery() | Out-Null
    $command.CommandText = 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM;'
    $command.Parameters.RemoveAt('@database')
    $command.ExecuteNonQuery() | Out-Null
    Write-Output ('Respaldo creado y verificado: ' + $backup)
} finally { $connection.Dispose() }
