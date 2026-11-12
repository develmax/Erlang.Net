param([string]$Part)
$ErrorActionPreference='Stop'
$tool=(Resolve-Path "artifacts/beam-backend/$Part-tool").Path
foreach($n in 'Erlang.Terms','Erlang.Serialization','Erlang.Runtime','Erlang.Otp.Behaviours','Erlang.Compiler'){
    [Reflection.Assembly]::LoadFrom((Join-Path $tool "$n.dll")) | Out-Null
}
$a=[Reflection.Assembly]::LoadFrom((Join-Path $tool 'Erlang.BeamCorpus.dll'))
$m=$a.GetType('OtpWindowsReleaseProfiles',$true).GetMethod('FindForSource',[Reflection.BindingFlags]'Static, NonPublic')
$r=Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'
$d=New-Item -ItemType Directory -Force "artifacts/beam-backend/$Part-target/lib/wx-2.7/ebin"
$inc=[string[]]@("$r/lib/wx-2.7/include","$r/lib/wx-2.7/src","$r/lib/wx-2.7/examples/demo")
foreach($n in 'wxCursor','wxListItemAttr','wxStaticBoxSizer','wxStyledTextCtrl'){
    $src="lib\wx-2.7\src\gen\$n.erl"
    $profile=$m.Invoke($null,[object[]]@('wx-2.7',$src))
    $beam=[Erlang.Compiler.CodeGeneration]::CompileBeamFile((Join-Path $r $src),"$r/lib",$inc,$profile)
    $out=Join-Path $d "$n.beam"
    [IO.File]::WriteAllBytes($out,$beam)
    $match=(Get-FileHash $out).Hash -eq (Get-FileHash "$r/lib/wx-2.7/ebin/$n.beam").Hash
    "$n exact $match"
}
