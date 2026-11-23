param(
    [Parameter(Mandatory)][string]$CorpusBinary,
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$OtpRoot,
    [Parameter(Mandatory)][string]$PublicKeyHeaders,
    [Parameter(Mandatory)][string]$SourceHeaders,
    [Parameter(Mandatory)][string]$Output
)

$ErrorActionPreference = 'Stop'
$directory = Split-Path (Resolve-Path -LiteralPath $CorpusBinary).Path
$load = [System.Runtime.Loader.AssemblyLoadContext]::Default
foreach ($file in (Get-ChildItem -LiteralPath $directory -Filter 'Erlang.*.dll')) {
    try {
        $null = [Reflection.AssemblyName]::GetAssemblyName($file.FullName)
    } catch {
        if ($_.Exception.InnerException -is [BadImageFormatException]) {
            continue
        }

        throw
    }
    $null = $load.LoadFromAssemblyPath($file.FullName)
}
$corpus = $load.LoadFromAssemblyPath((Resolve-Path -LiteralPath $CorpusBinary).Path)
$flags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Public
$profiles = $corpus.GetType('OtpWindowsReleaseProfiles', $true).GetMethod('FindForSource', $flags)
$include = $corpus.EntryPoint.DeclaringType.GetMethods($flags) |
    Where-Object { $_.Name -match 'g__IncludeDirectories\|' }
if ($null -eq $profiles -or @($include).Count -ne 1) {
    throw 'Cannot find the corpus profile and include resolvers.'
}

function Convert-TermDictionary($Dictionary) {
    if ($null -eq $Dictionary) {
        return $null
    }

    $terms = [ordered]@{}
    foreach ($entry in ($Dictionary.GetEnumerator() | Sort-Object Key -CaseSensitive)) {
        $terms[[string]$entry.Key] = [string]$entry.Value
    }

    return $terms
}

$library = Join-Path $OtpRoot 'lib'
$modules = (Get-Content -LiteralPath $Report -Raw | ConvertFrom-Json).Modules
$rows = foreach ($module in $modules) {
    $beam = $module.Beam.Split([IO.Path]::DirectorySeparatorChar)
    $application = if ($beam.Length -gt 2 -and $beam[0] -eq 'lib') { $beam[1] } else { $beam[0] }
    $profile = $profiles.Invoke($null, [string[]]@($application, $module.Source))
    $includes = $include.Invoke($null, [string[]]@($library, $application, $PublicKeyHeaders, $SourceHeaders))
    $resolved = if ($null -ne $profile) {
        @{
            CompilerVersion = $profile.CompilerVersion
            SourcePath = $profile.SourcePath
            WorkingDirectory = $profile.WorkingDirectory
            IncludeDirectories = $profile.IncludeDirectories
            WarningOptions = $profile.WarningOptions
            InsertedVersion = $profile.InsertedVersion
            InsertedAttributeName = $profile.InsertedAttributeName
            WarningsBeforeInsertion = $profile.WarningsBeforeInsertion
            RecordedDefinitions = Convert-TermDictionary $profile.RecordedDefinitions
            RecordedCompileOptions = if ($null -ne $profile.RecordedCompileOptions) {
                @($profile.RecordedCompileOptions | ForEach-Object { [string]$_ })
            }
            EmitAbstractCode = $profile.EmitAbstractCode
            RecordSourceFileInLineTable = $profile.RecordSourceFileInLineTable
            RecordedDebugOptions = if ($null -ne $profile.RecordedDebugOptions) {
                @($profile.RecordedDebugOptions | ForEach-Object { [string]$_ })
            }
            RecordedSourceFile = $profile.RecordedSourceFile
            RecordedIncludeFiles = Convert-TermDictionary $profile.RecordedIncludeFiles
            RecordedIncludeLibraryRoots = Convert-TermDictionary $profile.RecordedIncludeLibraryRoots
            RecordedIncludeSearchDirectories = Convert-TermDictionary $profile.RecordedIncludeSearchDirectories
            PreprocessorDefinitions = Convert-TermDictionary $profile.PreprocessorDefinitions
            AllowIncompleteAbstractCode = $profile.AllowIncompleteAbstractCode
            EmitDocumentation = $profile.EmitDocumentation
        }
    }

    [pscustomobject]@{
        Beam = $module.Beam
        Source = $module.Source
        Application = $application
        PhysicalIncludeDirectories = @($includes)
        Profile = $resolved
    }
}

@{
    ToolSha256 = (Get-FileHash -LiteralPath $CorpusBinary -Algorithm SHA256).Hash
    Count = @($rows).Count
    Modules = @($rows)
} | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Output -Encoding utf8
