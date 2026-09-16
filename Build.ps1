$ErrorActionPreference = "Stop"

$pluginName = "VerseLink"
$projectDirectory = "$PSScriptRoot\Community.PowerToys.Run.Plugin.VerseLink"
$projectFile = "$projectDirectory\Community.PowerToys.Run.Plugin.VerseLink.csproj"
$solutionFile = "$PSScriptRoot\Community.PowerToys.Run.Plugin.VerseLink.sln"

[xml]$xml = Get-Content -Path $projectFile
$version = "$($xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1)".Trim()
if (-not $version)
{
    throw "Could not read <Version> from $projectFile"
}

foreach ($platform in "ARM64", "x64")
{
    foreach ($dir in "bin", "obj")
    {
        if (Test-Path -Path "$projectDirectory\$dir")
        {
            Remove-Item -Path "$projectDirectory\$dir\*" -Recurse -Force
        }
    }

    dotnet build $solutionFile -c Release /p:Platform=$platform
    if ($LASTEXITCODE -ne 0)
    {
        throw "Build failed for $platform"
    }

    $releaseDirectory = "$projectDirectory\bin\$platform\Release"

    # Strip the host-provided assemblies and debug output, but leave the Bible texts
    # alone -- they are .xml too, and the plugin cannot run without them.
    Get-ChildItem -Path $releaseDirectory -Recurse -Include *.xml, *.pdb, PowerToys.*, Wox.* |
        Where-Object { $_.FullName -notmatch '\\Bibles\\' } |
        Remove-Item -Force

    # Stripping the host assemblies leaves Libs\ behind as empty folders.
    Get-ChildItem -Path $releaseDirectory -Recurse -Directory |
        Sort-Object { $_.FullName.Length } -Descending |
        Where-Object { -not (Get-ChildItem -Path $_.FullName -Recurse -File) } |
        Remove-Item -Recurse -Force

    foreach ($required in "plugin.json", "Community.PowerToys.Run.Plugin.VerseLink.dll")
    {
        if (-not (Test-Path -Path "$releaseDirectory\$required"))
        {
            throw "$required is missing from $releaseDirectory - the package would not load in PowerToys Run."
        }
    }

    if (-not (Test-Path -Path "$releaseDirectory\Bibles"))
    {
        Write-Warning "No Bibles folder in $releaseDirectory - the packaged plugin will not be able to load any translation. See the README."
    }

    Rename-Item -Path $releaseDirectory -NewName $pluginName
    Compress-Archive -Path "$projectDirectory\bin\$platform\$pluginName" -DestinationPath "$PSScriptRoot\$pluginName-$version-$platform.zip" -Force
}
