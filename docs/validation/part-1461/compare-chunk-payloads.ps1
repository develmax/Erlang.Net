param([string]$Otp, [string]$Report, [string]$Generated)
$ErrorActionPreference = 'Stop'
function Read-Chunks([string]$Path) {
    $data = [IO.File]::ReadAllBytes($Path)
    $sections = @{}
    $hash = [Security.Cryptography.SHA256]::Create()
    try {
        for ($offset = 12; $offset -lt $data.Length;) {
            $name = [Text.Encoding]::ASCII.GetString($data, $offset, 4)
            $size = [Net.IPAddress]::NetworkToHostOrder([BitConverter]::ToInt32($data, $offset + 4))
            $sections.Add($name, [Convert]::ToHexString($hash.ComputeHash($data, $offset + 8, $size)))
            $offset += 8 + (($size + 3) -band -4)
        }
    }
    finally {
        $hash.Dispose()
    }
    $sections
}
$reportData = Get-Content $Report -Raw | ConvertFrom-Json
$rows = foreach ($entry in $reportData.Modules | Where-Object Status -eq Different) {
    $original = Read-Chunks (Join-Path $otp $entry.Beam)
    $current = Read-Chunks (Join-Path $Generated $entry.Beam)
    $changed = @(($original.Keys + $current.Keys | Sort-Object -Unique) | Where-Object { $original[$_] -ne $current[$_] })
    [pscustomobject]@{ Count=$changed.Count; Chunks=$changed; Beam=$entry.Beam }
}
'Differing section counts per image'
$rows | Group-Object Count | Sort-Object { [int]$_.Name } | ForEach-Object { "$($_.Name): $($_.Count)" }
'Differing sections across images'
$rows.Chunks | Group-Object | Sort-Object Count -Descending | ForEach-Object { "$($_.Name): $($_.Count)" }
'Single differing section'
($rows | Where-Object Count -eq 1).Chunks | Group-Object | Sort-Object Count -Descending | ForEach-Object { "$($_.Name): $($_.Count)" }
'Sections differing alongside Code'
($rows | Where-Object { 'Code' -in $_.Chunks }).Chunks | Where-Object { $_ -ne 'Code' } | Group-Object | Sort-Object Count -Descending | ForEach-Object { "$($_.Name): $($_.Count)" }
