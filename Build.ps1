param(
    [string]$GamePath = "${env:ProgramFiles(x86)}\Steam\steamapps\common\Casualties Unknown"
)

$ErrorActionPreference = 'Stop'
$managed = Join-Path $GamePath 'CasualtiesUnknown_Data\Managed'
$core = Join-Path $GamePath 'BepInEx\core'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$references = @(
    (Join-Path $managed 'mscorlib.dll')
    (Join-Path $managed 'System.dll')
    (Join-Path $managed 'System.Core.dll')
    (Join-Path $managed 'netstandard.dll')
    (Join-Path $managed 'UnityEngine.dll')
    (Join-Path $managed 'UnityEngine.CoreModule.dll')
    (Join-Path $managed 'UnityEngine.UI.dll')
    (Join-Path $managed 'UnityEngine.UIModule.dll')
    (Join-Path $managed 'Unity.TextMeshPro.dll')
    (Join-Path $core 'BepInEx.dll')
    (Join-Path $core '0Harmony.dll')
)

foreach ($path in @($compiler) + $references) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required file missing: $path"
    }
}

$dist = Join-Path $PSScriptRoot 'dist'
$null = New-Item -ItemType Directory -Path $dist -Force
$dll = Join-Path $dist 'CasualtiesTogetherHangulCompat.dll'
$source = Join-Path $PSScriptRoot 'HangulSpeechCompat.cs'
$referenceArgs = $references | ForEach-Object { "/reference:$_" }
& $compiler /nologo /noconfig /nostdlib+ /target:library /optimize+ "/out:$dll" @referenceArgs $source
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed (exit $LASTEXITCODE)."
}

$version = [Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString(3)
$zip = Join-Path $dist "CasualtiesTogetherHangulCompat-v$version.zip"
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stream = [IO.File]::Open($zip, [IO.FileMode]::Create)
try {
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $true)
    try {
        $entry = 'BepInEx/plugins/CasualtiesTogetherHangulCompat/CasualtiesTogetherHangulCompat.dll'
        $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $dll, $entry)
    }
    finally {
        $archive.Dispose()
    }
}
finally {
    $stream.Dispose()
}

Get-Item -LiteralPath $dll, $zip
