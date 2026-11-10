$ErrorActionPreference = "Stop"

dotnet build -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build fallita. DLL non copiata."
    exit 1
}

$source = "bin\Release\netstandard2.1\Inkognito.Core.dll"
$destination = "D:\UnityProjects\Inkognito\Assets\plugin"

if (-not (Test-Path $source)) {
    Write-Error "DLL non trovata: $source"
    exit 1
}

New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -Path $source -Destination $destination -Force

Write-Host "Build completata e DLL copiata in Unity." -ForegroundColor Green