$b='artifacts/beam-backend'; $r=Join-Path $env:TEMP 'erl-cohort-7mzi5e1n/view/artifacts/library376-official-compiler/installed'; $tool=Resolve-Path "$b/part1292-tool"
$compiler=[Reflection.Assembly]::LoadFrom("$tool/Erlang.Compiler.dll")
$corpus=[Reflection.Assembly]::LoadFrom("$tool/Erlang.BeamCorpus.dll")
$profileMethod=$corpus.GetType('OtpWindowsReleaseProfiles').GetMethod('FindForSource',[Reflection.BindingFlags]'Static,NonPublic')
$app='wx-2.7'; $relative='lib\wx-2.7\src\gen\gl.erl'; $profile=$profileMethod.Invoke($null,[object[]]@($app,$relative))
$dirs=[string[]]@("$r/lib/$app/include","$r/lib/$app/src","$r/lib/$app/examples/demo")
$beam=$compiler.GetType('Erlang.Compiler.CodeGeneration',$true).GetMethod('CompileBeamFile').Invoke($null,[object[]]@("$r/$relative","$r/lib",$dirs,$profile))
$target="$b/part1292-focus/lib/wx-2.7/ebin"; New-Item -ItemType Directory -Force $target | Out-Null
[IO.File]::WriteAllBytes((Join-Path (Resolve-Path $target) 'gl.beam'),$beam)
$reference = "$r/lib/wx-2.7/ebin/gl.beam"
$generated = "$target/gl.beam"
Get-FileHash $reference,$generated | Format-List Hash,Path
if (!( [System.Linq.Enumerable]::SequenceEqual[byte]([IO.File]::ReadAllBytes($reference), [IO.File]::ReadAllBytes((Resolve-Path $generated).Path)) )) { exit 1 }
"Byte-for-byte equality confirmed."
