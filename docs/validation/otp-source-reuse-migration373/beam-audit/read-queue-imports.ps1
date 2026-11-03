$ErrorActionPreference = 'Stop'
$repo = (Get-Location).Path
$out = Join-Path $repo 'artifacts/architecture373-beam-audit1'
$authority = Get-Content -LiteralPath (Join-Path $repo 'artifacts/architecture373-queue-beam-admission1/receipt.json') -Raw | ConvertFrom-Json
$dllDir = Join-Path $repo 'src/Erlang.Runtime/bin/Debug/net10.0'
$dllPath = Join-Path $dllDir 'Erlang.Runtime.dll'
if ((Get-FileHash -LiteralPath $dllPath).Hash -ne $authority.RuntimeSha256) {
    throw 'Runtime bytes changed after root admission; refuse an unbound inventory.'
}
foreach ($dll in Get-ChildItem -LiteralPath $dllDir -Filter '*.dll') {
    [void][System.Reflection.Assembly]::LoadFrom($dll.FullName)
}
$assembly = [System.Reflection.Assembly]::LoadFrom($dllPath)
$path = Join-Path $repo $authority.Input
$bytes = [System.IO.File]::ReadAllBytes($path)
if ((Get-FileHash -LiteralPath $path).Hash -ne $authority.InputSha256) {
    throw 'Input bytes differ from root admission.'
}
$structure = $assembly.GetType('Erlang.BeamStructureReader', $true).GetMethod('Read').Invoke($null, [object[]]@([System.ReadOnlyMemory[byte]]::new($bytes), $null))
$instructions = $assembly.GetType('Erlang.BeamInstructionReader', $true).GetMethod('Read').Invoke($null, [object[]]@($structure))
$sourceOpcodes = Get-Content -LiteralPath (Join-Path $out 'source-opcodes.json') -Raw | ConvertFrom-Json
$roles = $sourceOpcodes.BeamLinkedCodeReader
$roleMethod = $assembly.GetType('Erlang.BeamLinkedCodeReader', $true).GetMethod('ValidateRoles', [System.Reflection.BindingFlags]'NonPublic,Static')
$roleFailures = @()
$importSites = @()
for ($index = 0; $index -lt $instructions.Count; $index++) {
    $instruction = $instructions[$index]
    if ($instruction.Definition.Name -notin $roles) {
        try {
            # Probe only source-default unsupported opcode names. Their default
            # branch does not inspect function/lambda context; no supported
            # opcode is evaluated with missing context.
            $null = $roleMethod.Invoke($null, [object[]]@($instruction, $structure, $null, $null))
            throw 'A source-default unsupported role unexpectedly passed.'
        } catch {
            $exception = $_.Exception
            while ($null -ne $exception.InnerException) { $exception = $exception.InnerException }
            if ($exception.GetType().FullName -ne 'System.NotSupportedException' -or $exception.Message -ne 'Unsupported BEAM opcode role in bounded linker') { throw }
            $roleFailures += [ordered]@{ Index = $index; Offset = $instruction.Offset; Opcode = $instruction.Definition.Name; ErrorType = $exception.GetType().FullName; Error = $exception.Message }
        }
    }
    $importOperand = switch ($instruction.Definition.Name) {
        'call_ext' { 1 }
        'call_ext_last' { 1 }
        'call_ext_only' { 1 }
        'bif0' { 0 }
        'bif1' { 1 }
        'bif2' { 1 }
        'gc_bif1' { 2 }
        'gc_bif2' { 2 }
        'gc_bif3' { 2 }
        default { $null }
    }
    if ($null -ne $importOperand) {
        $import = $structure.Imports[[int]$instruction.Operands[$importOperand].Value]
        $importSites += [ordered]@{ Index = $index; Opcode = $instruction.Definition.Name; Module = $structure.Atoms[$import.ModuleAtomIndex]; Function = $structure.Atoms[$import.FunctionAtomIndex]; Arity = $import.Arity }
    }
}
$imports = @($structure.Imports | ForEach-Object {
    [ordered]@{
        Module = $structure.Atoms[$_.ModuleAtomIndex]
        Function = $structure.Atoms[$_.FunctionAtomIndex]
        Arity = $_.Arity
    }
})
$exports = @($structure.Exports | ForEach-Object {
    [ordered]@{
        Function = $structure.Atoms[$_.FunctionAtomIndex]
        Arity = $_.Arity
        EntryLabel = $_.Label
    }
})
$receipt = [ordered]@{
    Status = 'ACTUAL_EXISTING_STRUCTURE_READER_IMPORT_EXPORT_INVENTORY'
    Input = $authority.Input
    InputSha256 = $authority.InputSha256
    RuntimeSha256 = $authority.RuntimeSha256
    Module = $structure.Atoms[1]
    Imports = $imports
    Exports = $exports
    SourceDefaultRoleFailureProbe = 'Actual rejected instruction objects; only source-default opcode cases; no supported-role validation fabricated'
    RoleFailures = $roleFailures
    ImportSites = $importSites
    GuestExecuted = $false
    BuildExecuted = $false
    ProductionChanged = $false
    OpcodeImplementationAdded = $false
}
[System.IO.File]::WriteAllText((Join-Path $out 'queue-imports.json'), ($receipt | ConvertTo-Json -Depth 12), [System.Text.UTF8Encoding]::new($false))
$receipt | ConvertTo-Json -Depth 12
