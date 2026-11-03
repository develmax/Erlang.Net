$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$out = Join-Path $repo 'artifacts/architecture373-queue-beam-admission1'
$null = New-Item -ItemType Directory -Force -Path $out
$dllDir = Join-Path $repo 'src/Erlang.Runtime/bin/Debug/net10.0'
foreach ($dll in Get-ChildItem -LiteralPath $dllDir -Filter '*.dll') {
    [void][System.Reflection.Assembly]::LoadFrom($dll.FullName)
}
$assembly = [System.Reflection.Assembly]::LoadFrom((Join-Path $dllDir 'Erlang.Runtime.dll'))
$path = Join-Path $repo 'docs/validation/part-368/original-receipts-and-raw/artifact-12117323576/capture/queue-installed.beam'
$bytes = [System.IO.File]::ReadAllBytes($path)
$memory = [System.ReadOnlyMemory[byte]]::new($bytes)
$receipt = [ordered]@{
    Input = $path.Substring($repo.Length + 1)
    InputBytes = $bytes.Length
    InputSha256 = (Get-FileHash -LiteralPath $path).Hash
    RuntimeSha256 = (Get-FileHash -LiteralPath (Join-Path $dllDir 'Erlang.Runtime.dll')).Hash
    PowerShell = $PSVersionTable.PSVersion.ToString()
    DotNet = [System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    Executed = $false
    ProductionChanged = $false
}
function Failure($errorRecord) {
    $ex = $errorRecord.Exception
    $messages = @()
    while ($null -ne $ex) {
        $messages += [ordered]@{ Type = $ex.GetType().FullName; Message = $ex.Message; Stack = $ex.StackTrace }
        $ex = $ex.InnerException
    }
    return $messages
}
try {
    $type = $assembly.GetType('Erlang.BeamStructureReader', $true)
    $structure = $type.GetMethod('Read').Invoke($null, [object[]]@($memory, $null))
    $receipt.StructureRead = $true
    $instructions = $assembly.GetType('Erlang.BeamInstructionReader', $true).GetMethod('Read').Invoke($null, [object[]]@($structure))
    $receipt.InstructionRead = $true
    $receipt.InstructionCount = $instructions.Count
    $inventory = @($instructions | Group-Object { $_.Definition.Name } | Sort-Object Name | ForEach-Object { [ordered]@{ Name = $_.Name; Count = $_.Count } })
    $receipt.OpcodeInventory = $inventory
} catch {
    $receipt.DecodeFailure = @(Failure $_)
}
try {
    $beam = $assembly.GetType('Erlang.BeamModule', $true).GetMethod('Prepare').Invoke($null, [object[]]@($memory))
    $receipt.Prepared = $true
    $receipt.Module = $beam.Name
    $receipt.Exports = @($beam.Exports | ForEach-Object { [ordered]@{ Name = $_.Name; Arity = $_.Arity; Supports = $beam.Supports($_.Name, $_.Arity) } })
} catch {
    $receipt.PrepareFailure = @(Failure $_)
}
$json = $receipt | ConvertTo-Json -Depth 18
[System.IO.File]::WriteAllText((Join-Path $out 'receipt.json'), $json, [System.Text.UTF8Encoding]::new($false))
Write-Output $json
