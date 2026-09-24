param([switch]$CriarUsuario)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $projectRoot 'GestaoViaturasAPI/GestaoViaturasAPI.csproj'
$localFile = Join-Path $projectRoot 'GestaoViaturasAPI/appsettings.Local.json'
if (-not (Test-Path -LiteralPath $localFile)) {
    $bytes = New-Object byte[] 64
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    @{ Jwt = @{ Key = [Convert]::ToBase64String($bytes) } } | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $localFile -Encoding UTF8
    Write-Host 'Chave JWT criada em appsettings.Local.json (ignorado pelo Git).'
} else {
    Write-Host 'Configuração local existente preservada.'
}
if ($CriarUsuario) {
    $previousUser = $env:Bootstrap__Usuario
    $previousPassword = $env:Bootstrap__Senha
    try {
        $env:Bootstrap__Usuario = Read-Host 'Nome do usuário'
        $securePassword = Read-Host 'Senha (12 a 256 caracteres)' -AsSecureString
        $credential = New-Object System.Management.Automation.PSCredential('bootstrap', $securePassword)
        $env:Bootstrap__Senha = $credential.GetNetworkCredential().Password
        dotnet run --project $project --no-launch-profile -- --criar-admin
        if ($LASTEXITCODE -ne 0) { throw 'Falha ao criar usuário. Verifique se as migrations foram aplicadas.' }
    } finally {
        $env:Bootstrap__Usuario = $previousUser
        $env:Bootstrap__Senha = $previousPassword
        if ($securePassword) { $securePassword.Dispose() }
        $credential = $null
    }
}
