param(
    [Parameter(Mandatory)][string]$RunnerDirectory,
    [Parameter(Mandatory)][string]$Output,
    [string]$SourceManifest
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$selected = @{}
$paths = @(Get-ChildItem (Join-Path $repository 'src/Otp/Managed/Erlang.Otp.Stdlib/Collections/Ets') -Filter '*.cs' | ForEach-Object FullName)
$paths += @(Get-ChildItem (Join-Path $repository 'src/Runtime/Erlang.Runtime.Core/Services/FileSystem') -Filter '*.cs' | ForEach-Object FullName)
$paths += @(Get-ChildItem $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName)
if ($SourceManifest) {
    $paths += @(Get-Content -LiteralPath $SourceManifest -Raw | ConvertFrom-Json |
        Where-Object { $_.EndsWith('.cs', [StringComparison]::Ordinal) } |
        ForEach-Object { Join-Path $repository $_ })
}
$paths += @(
    'src/Runtime/Erlang.Runtime.Core/Bootstrap/RuntimeBootstrapEtsLibrary.cs',
    'src/Runtime/Erlang.Runtime.Core/Bootstrap/RuntimeInitializer.cs',
    'src/Runtime/Erlang.Runtime.Core/Bootstrap/InitializationNames.cs',
    'src/Runtime/Erlang.Runtime.Core/Modules/LibrarySelectionNames.cs',
    'src/Runtime/Erlang.Runtime.Core/Modules/RuntimeManagedLibraryStage456.cs',
    'src/Runtime/Erlang.Runtime.Core/Processes/Context/ProcessRuntime.LibrarySelection.cs',
    'src/Runtime/Erlang.Runtime.Core/Primitives/ErlangFunctionCallErrors.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Regex/ReCaptureMode411.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Regex/ReCaptureNames485.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Regex/ReOptions411.cs',
    'src/Runtime/Erlang.Runtime.Core/Native/Regex/RuntimeRegexEngine411.Operations.cs',
    'tools/Validation/Erlang.EtsCompatibility/Program.cs'
) | ForEach-Object { Join-Path $repository $_ }
foreach ($path in $paths) { $selected[[IO.Path]::GetFullPath($path)] = $false }
$rows = @()
foreach ($assembly in @('Erlang.Otp.Stdlib', 'Erlang.Runtime', 'Erlang.EtsCompatibility')) {
    $pdb = Join-Path $RunnerDirectory ($assembly + '.pdb')
    $stream = [IO.File]::OpenRead([IO.Path]::GetFullPath($pdb))
    $provider = [System.Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream(
        $stream, [System.Reflection.Metadata.MetadataStreamOptions]::LeaveOpen, 0)
    try {
        $reader = $provider.GetMetadataReader([System.Reflection.Metadata.MetadataReaderOptions]::Default, $null)
        foreach ($handle in $reader.Documents) {
            $document = $reader.GetDocument($handle)
            $path = $reader.GetString($document.Name)
            if (-not $selected.ContainsKey($path)) { continue }
            $algorithm = $reader.GetGuid($document.HashAlgorithm)
            if ($algorithm -ne [Guid]'8829d00f-11b8-4213-878b-770e8597ac16') { throw 'Expected SHA-256 source checksums.' }
            $compiled = [Convert]::ToHexString($reader.GetBlobBytes($document.Hash))
            $current = (Get-FileHash -LiteralPath $path).Hash
            $selected[$path] = $true
            $rows += [ordered]@{
                Path = [IO.Path]::GetRelativePath($repository, $path).Replace('\', '/')
                Assembly = $assembly
                Sha256 = $compiled
                CurrentSha256 = $current
                MatchesCurrent = ($current -ceq $compiled)
            }
        }
    }
    finally { $provider.Dispose(); $stream.Dispose() }
}
$missing = @($selected.Keys | Where-Object { -not $selected[$_] })
if ($missing.Count -ne 0) { throw ('Unbound source inputs: ' + ($missing -join ', ')) }
[ordered]@{
    Scope = 'ETS implementation, selection, connected file/buffer and regex/bootstrap sources; not whole-checkout attestation'
    Matched = $rows.Count
    CurrentSourceDifferences = @($rows | Where-Object { -not $_.MatchesCurrent }).Count
    Sources = @($rows | Sort-Object Path)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Output -Encoding utf8
Write-Output "PDB source documents bound: $($rows.Count); current differences: $(@($rows | Where-Object { -not $_.MatchesCurrent }).Count)"
