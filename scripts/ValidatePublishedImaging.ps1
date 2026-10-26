param(
    [Parameter(Mandatory = $true)]
    [string]$PublishDirectory
)

$ErrorActionPreference = 'Stop'
$publishPath = (Resolve-Path -LiteralPath $PublishDirectory).Path
$repositoryPath = Split-Path $PSScriptRoot -Parent
$tempPath = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$scratchPath = [IO.Path]::GetFullPath((Join-Path $tempPath ('ImageGenerator335Smoke_' + [Guid]::NewGuid().ToString('N'))))
$copiedSmokePaths = [System.Collections.Generic.List[string]]::new()
try {
    New-Item -ItemType Directory -Path $scratchPath | Out-Null
    $assemblyPath = [System.Security.SecurityElement]::Escape((Join-Path $publishPath 'ImageGeneratorApp.dll'))
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>ImagingPublishSmoke</AssemblyName>
  </PropertyGroup>
  <ItemGroup><Reference Include="ImageGeneratorApp"><HintPath>$assemblyPath</HintPath><Private>false</Private></Reference></ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $scratchPath 'Smoke.csproj')
    @'
using ImageGeneratorApp;
using System.Drawing;
using System.Runtime.InteropServices;

string fixtures = args[0];
string history = Path.Combine(Path.GetTempPath(), "ImageGenerator335Published_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(history);
try
{
    byte[] input = File.ReadAllBytes(Path.Combine(fixtures, "legacy-alpha-source.png"));
    var metadata = new ImageGenerationMetadata("Smoke", "Été 🌈 <>&", "local-fixture", DateTime.UtcNow, null, null, "Publish smoke");
    foreach (string extension in new[] { "png", "jpg" })
    {
        byte[] output = ImageMetadataEmbedder.Embed(input, metadata, extension);
        using var stream = new MemoryStream(output);
        using var bitmap = new Bitmap(stream);
        if (bitmap.Width != 16 || bitmap.Height != 16) throw new Exception("Export dimensions changed.");
    }
    var service = new ImageProcessingService(history);
    string saved = await service.SaveImageAsWebpAsync(input, "new", metadata);
    using (var bitmap = (Bitmap)await service.LoadWebpForWinFormsAsync(saved))
    {
        File.Delete(saved);
        if (bitmap.GetPixel(8, 8).A != 128) throw new Exception("History alpha changed.");
    }
    foreach (string name in new[] { "legacy-opaque.webp", "legacy-alpha.webp" })
    {
        string copy = Path.Combine(history, name);
        File.Copy(Path.Combine(fixtures, name), copy);
        using var bitmap = (Bitmap)await service.LoadWebpForWinFormsAsync(copy);
        File.Delete(copy);
        if (bitmap.Width != 32 || bitmap.Height != 24) throw new Exception("Legacy dimensions changed.");
        _ = bitmap.GetPixel(16, 12);
    }
    Console.WriteLine($"PASS: published {RuntimeInformation.ProcessArchitecture} binaries, PNG/JPEG exports, metadata encoding, new/legacy WebP, alpha, independent Bitmap and file release.");
}
finally
{
    Directory.Delete(history, recursive: true);
}
'@ | Set-Content -LiteralPath (Join-Path $scratchPath 'Program.cs')

    $dependencies = Get-Content -LiteralPath (Join-Path $publishPath 'ImageGeneratorApp.deps.json') -Raw | ConvertFrom-Json
    $runtimeIdentifier = ($dependencies.runtimeTarget.name -split '/')[-1]
    dotnet build (Join-Path $scratchPath 'Smoke.csproj') -c Release -r $runtimeIdentifier --self-contained false -o (Join-Path $scratchPath 'output')
    if ($LASTEXITCODE -ne 0) { throw 'La compilation du harnais de validation a échoué.' }
    $smokeDllPath = Join-Path $publishPath 'ImagingPublishSmoke.dll'
    $smokeExePath = Join-Path $publishPath 'ImagingPublishSmoke.exe'
    $smokeDepsPath = Join-Path $publishPath 'ImagingPublishSmoke.deps.json'
    $smokeConfigPath = Join-Path $publishPath 'ImagingPublishSmoke.runtimeconfig.json'
    $smokeFiles = @(
        @{ Source = (Join-Path $scratchPath 'output/ImagingPublishSmoke.dll'); Destination = $smokeDllPath },
        @{ Source = (Join-Path $scratchPath 'output/ImagingPublishSmoke.exe'); Destination = $smokeExePath },
        @{ Source = (Join-Path $publishPath 'ImageGeneratorApp.deps.json'); Destination = $smokeDepsPath },
        @{ Source = (Join-Path $publishPath 'ImageGeneratorApp.runtimeconfig.json'); Destination = $smokeConfigPath }
    )
    foreach ($file in $smokeFiles) {
        # Never overwrite or clean up files that were already present before this run.
        if (Test-Path -LiteralPath $file.Destination) {
            throw 'Un fichier du harnais existe déjà dans le dossier de publication.'
        }
        $copiedSmokePaths.Add($file.Destination)
        Copy-Item -LiteralPath $file.Source -Destination $file.Destination
    }
    & $smokeExePath (Join-Path $repositoryPath 'ImageGeneratorApp.Tests/Fixtures')
    if ($LASTEXITCODE -ne 0) { throw 'La validation des binaires publiés a échoué.' }
}
finally {
    foreach ($path in $copiedSmokePaths) {
        try {
            if (Test-Path -LiteralPath $path) {
                Remove-Item -LiteralPath $path -Force -ErrorAction Stop
            }
        }
        catch {
            Write-Warning 'Impossible de supprimer un fichier temporaire du harnais dans le dossier de publication.'
        }
    }

    # Only recursively delete the unique scratch directory directly below the temp root.
    if (![string]::Equals([IO.Path]::GetDirectoryName($scratchPath), [IO.Path]::TrimEndingDirectorySeparator($tempPath), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Le dossier temporaire du harnais ne se trouve pas dans le répertoire temporaire attendu.'
    }
    try {
        if (Test-Path -LiteralPath $scratchPath) {
            Remove-Item -LiteralPath $scratchPath -Recurse -Force -ErrorAction Stop
        }
    }
    catch {
        Write-Warning 'Impossible de supprimer le dossier temporaire du harnais de validation.'
    }
}
