param([string]$ToolName='part1237-final-tool',[string]$Output='abstract-diagnostics.json')
$ErrorActionPreference='Stop'
$d='docs/validation/part-1238'; $b='artifacts/beam-backend'
$tool=(Resolve-Path "$b/$ToolName").Path
foreach($name in 'Erlang.Terms','Erlang.Serialization','Erlang.Runtime','Erlang.Otp.Behaviours','Erlang.Compiler'){[Reflection.Assembly]::LoadFrom((Join-Path $tool "$name.dll")) | Out-Null}
$a=[Reflection.Assembly]::LoadFrom((Join-Path $tool 'Erlang.BeamCorpus.dll'))
$flags=[Reflection.BindingFlags]'Static, NonPublic'
$profileMethod=$a.GetType('OtpWindowsReleaseProfiles',$true).GetMethod('FindForSource',$flags)
$includeMethod=$a.EntryPoint.DeclaringType.GetMethods($flags) | Where-Object Name -like '*IncludeDirectories*' | Select-Object -First 1
$r=Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$pk=(Resolve-Path "$b/part929-public-key-headers").Path; $overlay=(Resolve-Path "$b/part1058-source-overlay").Path
$report=@{}; (Get-Content docs/validation/part-1237/otp-corpus.json -Raw|ConvertFrom-Json).Modules | ForEach-Object {$report[$_.Beam]=$_}
$rows=foreach($path in Get-Content "$d/chunk-audit.txt" | Select-Object -Skip 3){
  $m=$report[$path.Replace('/','\')]
  $app=($path -split '/')[1]
  $profile=$profileMethod.Invoke($null,[object[]]@($app,$m.Source))
  if(!$profile -or !$profile.AllowIncompleteAbstractCode){[pscustomobject]@{Beam=$path;Error='No incomplete abstract profile'};continue}
  $profile.GetType().GetProperty('AllowIncompleteAbstractCode').SetValue($profile,$false)
  $includes=$includeMethod.Invoke($null,[object[]]@("$r/lib",$app,$pk,$overlay))
  try{
    $null=[Erlang.Compiler.CodeGeneration]::CompileBeamFile((Join-Path $r $m.Source),"$r/lib",$includes,$profile)
    [pscustomobject]@{Beam=$path;Error='Compiled with strict profile'}
  }catch{
    $errorInfo=$_.Exception.InnerException ?? $_.Exception
    [pscustomobject]@{Beam=$path;Error=$errorInfo.Message;Origin=$errorInfo.GetBaseException().StackTrace -split '\r?\n' | Select-Object -First 2}
  }
}
ConvertTo-Json -InputObject @($rows) -Depth 4 | Set-Content "$d/$Output"
$rows | Group-Object {$_.Error -replace '^Cannot write debug forms for .*?\. ' , ''} | Sort-Object Count -Descending | Select-Object -First 12 Count,Name | Format-Table -AutoSize
