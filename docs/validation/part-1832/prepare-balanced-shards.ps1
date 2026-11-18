param(
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][string]$ReleaseDirectory,
    [Parameter(Mandatory)][string]$BaselineReport,
    [ValidateSet(4,5)][int]$Shards = 5
)

$ErrorActionPreference = 'Stop'
$modules = @((Get-Content -LiteralPath $BaselineReport -Raw | ConvertFrom-Json).Modules)
if ($modules.Count -ne 1366 -or @($modules.Beam | Sort-Object -Unique).Count -ne 1366) {
    throw 'The baseline does not contain 1,366 unique release module paths.'
}

$weighted = @($modules | ForEach-Object {
    [pscustomobject]@{ Path = $_.Beam; Weight = (Get-Item -LiteralPath (Join-Path $ReleaseDirectory $_.Beam)).Length }
} | Sort-Object -Property @{Expression = 'Weight'; Descending = $true}, Path)
$buckets = @(0..($Shards - 1) | ForEach-Object {
    [pscustomobject]@{ Index = $_; Weight = 0L; Paths = [Collections.Generic.List[string]]::new() }
})
foreach ($module in $weighted) {
    $bucket = $buckets | Sort-Object Weight,Index | Select-Object -First 1
    $bucket.Weight += $module.Weight
    $bucket.Paths.Add($module.Path)
}

New-Item -ItemType Directory $OutputDirectory | Out-Null
$summary = foreach ($bucket in $buckets) {
    $shard = Join-Path $OutputDirectory "shard-$($bucket.Index)"
    New-Item -ItemType Directory $shard | Out-Null
    $moduleList = Join-Path $shard 'modules.txt'
    $bucket.Paths | Sort-Object | Set-Content $moduleList
    [pscustomobject]@{
        Index = $bucket.Index
        Modules = $bucket.Paths.Count
        ReferenceSize = $bucket.Weight
        ModuleListSha256 = (Get-FileHash $moduleList).Hash
    }
}
$summary | ConvertTo-Json | Set-Content (Join-Path $OutputDirectory 'partitions.json')
$summary | Format-Table -AutoSize
