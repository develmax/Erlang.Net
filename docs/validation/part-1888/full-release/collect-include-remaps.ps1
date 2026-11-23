param(
    [Parameter(Mandatory)][string]$CorpusBinary,
    [Parameter(Mandatory)][string]$Report,
    [Parameter(Mandatory)][string]$OtpRoot,
    [Parameter(Mandatory)][string]$SourceHeaders,
    [Parameter(Mandatory)][string]$Output
)
$ErrorActionPreference='Stop'
$resolved=Get-Item -LiteralPath $CorpusBinary
$loader=[System.Runtime.Loader.AssemblyLoadContext]::Default
foreach($assembly in Get-ChildItem -LiteralPath $resolved.Directory.FullName -Filter 'Erlang.*.dll'){
    try { $null=[Reflection.AssemblyName]::GetAssemblyName($assembly.FullName) }
    catch {
        if($_.Exception.InnerException -is [BadImageFormatException]){continue}
        throw
    }
    $null=$loader.LoadFromAssemblyPath($assembly.FullName)
}
$corpus=$loader.LoadFromAssemblyPath($resolved.FullName)
$flags=[Reflection.BindingFlags]'Static, NonPublic, Public'
$profileMethod=$corpus.GetType('OtpWindowsReleaseProfiles',$true).GetMethod('FindForSource',$flags)
$handlers=foreach($typeName in 'OtpWindowsSnmpIncludePaths','OtpWindowsBootstrapLibraryIncludePaths','OtpWindowsInetsIncludePaths','OtpWindowsCoreIncludePaths','OtpWindowsInTreeLibraryIncludePaths'){
    $type=$corpus.GetType($typeName)
    if($null -ne $type){[pscustomobject]@{Name=$typeName;Method=$type.GetMethod('Find',$flags)}}
}
$library=Join-Path $OtpRoot 'lib'
$images=(Get-Content -LiteralPath $Report -Raw | ConvertFrom-Json).Modules
$records=foreach($image in $images){
    $parts=$image.Beam.Split([IO.Path]::DirectorySeparatorChar)
    $application=if($parts[0] -eq 'lib'){$parts[1]}else{$parts[0]}
    $source=[IO.Path]::GetFullPath((Join-Path $OtpRoot $image.Source))
    $profile=$profileMethod.Invoke($null,[string[]]@($application,$image.Source))
    if($null -eq $profile){continue}
    foreach($handler in $handlers){
        $arguments=[object[]]@([string]$library,[string]$application,[string]$source,$profile.PSObject.BaseObject)
        if($handler.Method.GetParameters().Count -eq 5){$arguments+=@([string]$SourceHeaders)}
        if($handler.Method.GetParameters().Count -ne $arguments.Count){throw $handler.Name}
        $map=$handler.Method.Invoke($null,$arguments)
        if($null -eq $map){continue}
        $libraries=[ordered]@{}
        foreach($entry in $map.Libraries.GetEnumerator() | Sort-Object Key -CaseSensitive){
            $libraries[$entry.Key]=@{Physical=$entry.Value.PhysicalDirectory;Recorded=$entry.Value.RecordedDirectory}
        }
        [pscustomobject]@{
            Beam=$image.Beam
            Resolver=$handler.Name
            SourceArgument=$map.SourceArgument
            SourceSha256=(Get-FileHash -LiteralPath $source).Hash
            WorkingDirectory=$map.WorkingDirectory
            PhysicalSourceDirectory=$map.PhysicalSourceDirectory
            IncludeDirectories=@($map.IncludeDirectories)
            Libraries=$libraries
        }
        break
    }
}
@{Count=$images.Count;Mapped=$records.Count;ToolSha256=(Get-FileHash -LiteralPath $CorpusBinary).Hash;Modules=@($records)} |
    ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Output -Encoding utf8
