$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Reflection.Metadata
$hashIlRoot = Split-Path -Parent $PSScriptRoot
$hashIlPath = Join-Path $hashIlRoot 'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'
$hashIlStream = [IO.File]::OpenRead($hashIlPath)
$hashIlReader = [Reflection.PortableExecutable.PEReader]::new($hashIlStream)
$hashIlMetadata = [Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($hashIlReader)
$hashIlCodes = @{}
foreach ($field in [Reflection.Emit.OpCodes].GetFields([Reflection.BindingFlags]'Public,Static')) {
    $code = $field.GetValue($null)
    $hashIlCodes[([int]$code.Value -band 65535)] = $code
}
function HashIlTokenName([int]$token) {
    $row = $token -band 16777215
    switch (($token -shr 24) -band 255) {
        10 { $member = $hashIlMetadata.GetMemberReference([Reflection.Metadata.Ecma335.MetadataTokens]::MemberReferenceHandle($row)); return $hashIlMetadata.GetString($member.Name) }
        6 { $method = $hashIlMetadata.GetMethodDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::MethodDefinitionHandle($row)); return $hashIlMetadata.GetString($method.Name) }
        4 { $field = $hashIlMetadata.GetFieldDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::FieldDefinitionHandle($row)); return $hashIlMetadata.GetString($field.Name) }
        1 { $type = $hashIlMetadata.GetTypeReference([Reflection.Metadata.Ecma335.MetadataTokens]::TypeReferenceHandle($row)); return $hashIlMetadata.GetString($type.Namespace) + '.' + $hashIlMetadata.GetString($type.Name) }
        2 { $type = $hashIlMetadata.GetTypeDefinition([Reflection.Metadata.Ecma335.MetadataTokens]::TypeDefinitionHandle($row)); return $hashIlMetadata.GetString($type.Name) }
        default { return ('token-0x{0:X8}' -f $token) }
    }
}
$hashIlRows = @()
try {
    foreach ($typeHandle in $hashIlMetadata.TypeDefinitions) {
        $type = $hashIlMetadata.GetTypeDefinition($typeHandle)
        if ($hashIlMetadata.GetString($type.Name) -ne '<<Resources>b__0>d') { continue }
        foreach ($methodHandle in $type.GetMethods()) {
            $method = $hashIlMetadata.GetMethodDefinition($methodHandle)
            if ($hashIlMetadata.GetString($method.Name) -ne 'MoveNext') { continue }
            $body = [Reflection.Metadata.PEReaderExtensions]::GetMethodBody($hashIlReader,$method.RelativeVirtualAddress)
            $bytes = $body.GetILBytes(); $position = 0
            while ($position -lt $bytes.Length) {
                $offset = $position; $value = [int]$bytes[$position]; $position++
                if ($value -eq 254) { $value = 65280 -bor [int]$bytes[$position]; $position++ }
                $code = $hashIlCodes[$value]; if ($null -eq $code) { throw 'Unknown IL opcode' }
                $operand = $null; $tokenName = $null
                switch ([string]$code.OperandType) {
                    'InlineNone' { }
                    'ShortInlineVar' { $operand = [int]$bytes[$position]; $position++ }
                    'ShortInlineI' { $operand = [int]$bytes[$position]; $position++ }
                    'ShortInlineBrTarget' { $operand = [int]$bytes[$position]; $position++ }
                    'InlineVar' { $operand = [BitConverter]::ToUInt16($bytes,$position); $position += 2 }
                    'InlineI8' { $operand = [BitConverter]::ToInt64($bytes,$position); $position += 8 }
                    'InlineR' { $operand = [BitConverter]::ToDouble($bytes,$position); $position += 8 }
                    'ShortInlineR' { $operand = [BitConverter]::ToSingle($bytes,$position); $position += 4 }
                    'InlineSwitch' { $length = [BitConverter]::ToInt32($bytes,$position); $position += 4; $operand = $length; $position += 4*$length }
                    default {
                        $operand = [BitConverter]::ToInt32($bytes,$position); $position += 4
                        if ([string]$code.OperandType -in @('InlineMethod','InlineField','InlineType','InlineTok')) { $tokenName = HashIlTokenName $operand }
                    }
                }
                $hashIlRows += [pscustomobject]@{ Offset=$offset; Opcode=$code.Name; Operand=$operand; TokenName=$tokenName }
            }
        }
    }
    if (-not $hashIlRows.Count) { throw 'Actual hash lambda IL not found' }
    $result = @{ Status='ACTUAL_MAIN_IL_READ_ONLY'; AssemblySha256=(Get-FileHash -LiteralPath $hashIlPath -Algorithm SHA256).Hash; Mvid=$hashIlMetadata.GetGuid($hashIlMetadata.GetModuleDefinition().Mvid).ToString(); Method='ErlangHashHostChecks/<<Resources>b__0>d.MoveNext'; Instructions=$hashIlRows; MainModified=$false; RuntimeExecuted=$false }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'hash-caller-actual-main-il.json') -Encoding utf8NoBOM
} finally { $hashIlReader.Dispose(); $hashIlStream.Dispose() }
