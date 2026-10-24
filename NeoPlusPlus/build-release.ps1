$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$appProject = Join-Path $projectRoot "NeoPlusPlusApp\NeoPlusPlusApp.csproj"
$setupProject = Join-Path $projectRoot "NeoPlusPlusSetup\NeoPlusPlusSetup.csproj"
$distribution = Join-Path $projectRoot "dist"
$staging = Join-Path $env:TEMP ("NeoPlusPlus-build-" + [guid]::NewGuid().ToString("N"))
$appPublish = Join-Path $staging "app"
$setupPublish = Join-Path $staging "setup"

try {
    dotnet publish $appProject -c Release -r win-x64 -o $appPublish --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
    if ($LASTEXITCODE -ne 0) { throw "Neo++ app publish failed." }

    dotnet publish $setupProject -c Release -r win-x64 -o $setupPublish --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
    if ($LASTEXITCODE -ne 0) { throw "Neo++ setup publish failed." }

    New-Item -ItemType Directory -Force -Path $distribution | Out-Null
    Copy-Item (Join-Path $appPublish "NeoPlusPlus.exe") (Join-Path $distribution "NeoPlusPlus.exe") -Force
    Copy-Item (Join-Path $setupPublish "NeoPlusPlusSetup.exe") (Join-Path $distribution "NeoPlusPlusSetup.exe") -Force
    Write-Host "Neo++ installer package created in $distribution"
}
finally {
    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath $staging -Recurse -Force
    }
}
