param([Parameter(Mandatory = $true)][string]$Repository)
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath($Repository)
$providerRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts/library376-official-compiler'))
if (-not $providerRoot.StartsWith($repoRoot.TrimEnd('\') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Provider path escapes repository' }
$archiveHash = '337480AF988ECA993C2E98B68850F00FC6724D366346565E38941AC0D1B7C56B'
$imageHash = '27E75CEAEB1C33BAA8089071A47DB1F3473B537906353F874D687AE9A0854B05'
$pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
$url = 'https://github.com/erlang/otp/releases/download/OTP-29.1.1/otp_win64_29.1.1.zip'
$archive = Join-Path $providerRoot 'otp_win64_29.1.1.zip'
$installed = Join-Path $providerRoot 'installed'
$manifest = Join-Path $providerRoot 'execution-files.json'
$admission = Join-Path $providerRoot 'official-admission.json'
if ((Test-Path -LiteralPath $admission) -or (Test-Path -LiteralPath $manifest)) {
    throw 'Use a fresh provider directory; existing source-bound admission and manifest must not be overwritten'
}
[IO.Directory]::CreateDirectory($providerRoot) | Out-Null
function Hash([string]$Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }
if (-not (Test-Path -LiteralPath $archive)) {
    $partial = Join-Path $providerRoot ('download-' + [Guid]::NewGuid().ToString('N') + '.zip.partial')
    Invoke-WebRequest -Uri $url -OutFile $partial
    if ((Get-Item -LiteralPath $partial).Length -ne 187232000 -or (Hash $partial) -ne $archiveHash) { throw 'Pinned release archive length/hash mismatch; partial retained' }
    Move-Item -LiteralPath $partial -Destination $archive
}
if ((Get-Item -LiteralPath $archive).Length -ne 187232000 -or (Hash $archive) -ne $archiveHash) { throw 'Pinned release archive changed' }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [IO.Compression.ZipFile]::OpenRead($archive)
try {
    if ($zip.Entries.Count -ne 6546) { throw 'Pinned archive entry count changed' }
    $files = @($zip.Entries | Where-Object { -not $_.FullName.EndsWith('/') })
    if ($files.Count -ne 6427) { throw 'Pinned archive file count changed' }
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $zip.Entries) {
        $relative = $entry.FullName.Replace('\','/')
        if ($relative.StartsWith('/') -or $relative.Contains(':') -or @($relative.Split('/') | Where-Object { $_ -eq '..' }).Count -ne 0) { throw 'Archive traversal/absolute path refused' }
        if ((($entry.ExternalAttributes -shr 16) -band 0xF000) -eq 0xA000) { throw 'Archive symlink refused' }
        if (-not $names.Add($relative.TrimEnd('/'))) { throw 'Archive duplicate path refused' }
    }
    $fresh = -not (Test-Path -LiteralPath $installed)
    $target = if ($fresh) { Join-Path $providerRoot ('installed.preparing-' + [Guid]::NewGuid().ToString('N')) } else { $installed }
    if ($fresh) { [IO.Directory]::CreateDirectory($target) | Out-Null }
    $rows = [Collections.Generic.List[object]]::new()
    $expectedFiles = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $files) {
        $destination = [IO.Path]::GetFullPath((Join-Path $target $entry.FullName))
        if (-not $destination.StartsWith($target.TrimEnd('\') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Extracted file path escapes provider' }
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
        if ($fresh) { [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $false) }
        $inputStream = $entry.Open()
        try { $sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($inputStream)) }
        finally { $inputStream.Dispose() }
        if ((Hash $destination) -ne $sourceHash -or (Get-Item -LiteralPath $destination).Length -ne $entry.Length) { throw ('Extracted file differs from verified archive: ' + $entry.FullName) }
        $expectedFiles.Add($destination) | Out-Null
        $rows.Add([ordered]@{ Path = $entry.FullName.Replace('\','/'); Sha256 = $sourceHash })
    }
    $actual = @([IO.Directory]::EnumerateFiles($target, '*', [IO.SearchOption]::AllDirectories))
    if ($actual.Count -ne 6427 -or @($actual | Where-Object { -not $expectedFiles.Contains($_) }).Count -ne 0) { throw 'Provider file closure differs' }
    if ((Hash (Join-Path $target 'erts-17.1/bin/beam.smp.dll')) -ne $imageHash) { throw 'Pinned PCRE-export image mismatch' }
    if ($fresh) {
        if (-not ([IO.Path]::GetFullPath($target)).StartsWith($providerRoot.TrimEnd('\') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Staged provider path escapes intended directory' }
        Move-Item -LiteralPath $target -Destination $installed
    }
    # Admission paths are generated for this actual runner, never copied from a
    # developer machine or a previous absolute cache location.
    $rows | Sort-Object { $_.Path } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifest -Encoding utf8NoBOM
    $record = [ordered]@{
        Provider = 'official-release'; ReleaseTag = 'OTP-29.1.1'; SourceCommit = $pin; ReleaseVersion = '29.1.1'; ReleaseUrl = $url
        ArchivePath = $archive; ArchiveSha256 = $archiveHash; PublisherAssetId = 581038947
        InstalledOtpRoot = $installed; ExecutionManifestPath = $manifest; ExecutionManifestSha256 = (Hash $manifest)
        EveryExtractedFileMatchesArchive = $true; ExecutionFileCount = 6427
        SystemInstallOrPathChanged = $false; SourceBuildCompletedClaimed = $false; OriginalVmExecuted = $false
    }
    $record | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $admission -Encoding utf8NoBOM
    [ordered]@{ Passed=$true; Admission=$admission; ArchiveSha256=$archiveHash; ImageSha256=$imageHash; VerifiedFiles=6427; OriginalVmExecuted=$false; InstallerExecuted=$false; PathOrRegistryChanged=$false } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $providerRoot 'ci-provider-ready.json') -Encoding utf8NoBOM
    Write-Output 'Pinned native PCRE provider ready: exact archive,6427 verified files; no original VM or installer execution.'
}
finally { $zip.Dispose() }
