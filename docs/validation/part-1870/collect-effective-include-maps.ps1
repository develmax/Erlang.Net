param([Parameter(Mandatory)][string]$Report)
$ErrorActionPreference='Stop'
$stage='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/late-list-and-tail-copy-provenance-1870'
$root='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed'
$library=Join-Path $root 'lib'
$tool="$stage/corpus-bin/Erlang.BeamCorpus.dll"
$context=[System.Runtime.Loader.AssemblyLoadContext]::Default
foreach($file in Get-ChildItem "$stage/corpus-bin" -Filter 'Erlang.*.dll'){
    try { $null=[Reflection.AssemblyName]::GetAssemblyName($file.FullName) }
    catch {
        if($_.Exception.InnerException -is [BadImageFormatException]){continue}
        throw
    }
    $null=$context.LoadFromAssemblyPath($file.FullName)
}
$asm=$context.LoadFromAssemblyPath((Resolve-Path $tool).Path)
$flags=[Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Public
$profiles=$asm.GetType('OtpWindowsReleaseProfiles',$true).GetMethod('FindForSource',$flags)
$snmpMap=$asm.GetType('OtpWindowsSnmpIncludePaths',$true).GetMethod('Find',$flags)
$kernelMap=$asm.GetType('OtpWindowsKernelIncludePaths',$true).GetMethod('Find',$flags)
$sourceHeaders='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full'
$rows=@(foreach($module in (Get-Content $Report -Raw | ConvertFrom-Json).Modules){
    $path=$module.Beam.Split([IO.Path]::DirectorySeparatorChar)
    $application=if($path.Length -gt 2 -and $path[0] -eq 'lib'){$path[1]}else{$path[0]}
    $isSnmp=$application.StartsWith('snmp-', [StringComparison]::Ordinal)
    if(!$isSnmp -and !$application.StartsWith('kernel-', [StringComparison]::Ordinal)){continue}
    $source=[IO.Path]::GetFullPath($module.Source,$root)
    $profile=$profiles.Invoke($null,[string[]]@($application,$module.Source))
    if($null -eq $profile){throw "Missing source profile $($module.Beam)"}
    $invoke=@([string]$library,[string]$application,[string]$source,$profile.PSObject.BaseObject)
    $resolved=if($isSnmp){
        $snmpMap.Invoke($null,[object[]]$invoke)
    }else{
        $kernelMap.Invoke($null,[object[]]($invoke+([string]$sourceHeaders)))
    }
    [ordered]@{
        Beam=$module.Beam
        Source=$module.Source
        InstalledSourceSha256=(Get-FileHash -LiteralPath $source).Hash
        WorkingDirectory=$profile.WorkingDirectory
        OriginalBuildIncludeOptions=@($profile.IncludeDirectories)
        Map=$resolved
    }
})
$mapped=@($rows | Where-Object {$null -ne $_.Map}).Count
@{
    SourceArchive=(Get-Content "$stage/source-freeze.json" -Raw|ConvertFrom-Json).Source
    CorpusTool=(Get-FileHash -LiteralPath $tool).Hash
    SourceRelease='OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5'
    Mapped=$mapped
    Rows=$rows.Count
    IncludePaths=$rows
} | ConvertTo-Json -Depth 12

