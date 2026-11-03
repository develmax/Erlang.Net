param(
    [Parameter(Mandatory = $true)][string]$ErlangExecutable,
    [string]$Output = 'artifacts/math-compatibility'
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
    $outputPath = [IO.Path]::GetFullPath($Output)
    New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
    $escapedOutput = $outputPath.Replace('\', '/').Replace('"', '\"')
    $expression = '{ok,math_acceptance}=compile:file("tools/Validation/Erlang.MathCompatibility/Original/math_acceptance.erl",[{outdir,"' +
        $escapedOutput + '"}]), code:add_patha("' + $escapedOutput + '"), math_acceptance:write("' +
        $escapedOutput + '/original.etf"), halt().'
    $start = [Diagnostics.ProcessStartInfo]::new([IO.Path]::GetFullPath($ErlangExecutable))
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $start.ArgumentList.Add('-noshell')
    $start.ArgumentList.Add('-eval')
    $start.ArgumentList.Add($expression)
    $child = [Diagnostics.Process]::Start($start)
    try {
        $stdout = $child.StandardOutput.ReadToEndAsync()
        $stderr = $child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit(240000)) {
            $child.Kill($true)
            $child.WaitForExit()
            throw 'Pinned OTP math acquisition timed out'
        }
        [IO.File]::WriteAllText((Join-Path $outputPath 'oracle.log'),$stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult())
        if ($child.ExitCode -ne 0) {
            throw "Pinned OTP math acquisition failed with exit code $($child.ExitCode); see oracle.log"
        }
    }
    finally {
        $child.Dispose()
    }
    $files = Get-ChildItem -LiteralPath $outputPath -File | Where-Object Extension -In '.etf','.beam'
    $identities = @($files | ForEach-Object { [ordered]@{ Name = $_.Name; Sha256 = (Get-FileHash -LiteralPath $_.FullName).Hash; Length = $_.Length } })
    [ordered]@{
        Version = '29.1.1'
        ExpectedSourceCommit = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
        ObserverSha256 = (Get-FileHash -LiteralPath 'tools/Validation/Erlang.MathCompatibility/Original/math_acceptance.erl').Hash
        ExecutableSha256 = (Get-FileHash -LiteralPath $start.FileName).Hash
        Files = $identities
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputPath 'oracle-identity.json') -Encoding utf8NoBOM
    Get-Content -LiteralPath (Join-Path $outputPath 'oracle.log')
}
finally {
    Pop-Location
}
