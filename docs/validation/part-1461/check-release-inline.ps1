param([string] $OtpRoot, [string] $Tool, [string] $Output, [string] $Source)

$ErrorActionPreference = 'Stop'
$sourcePath = (Resolve-Path $Source).Path
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path (Resolve-Path $Tool).Path 'Erlang.Compiler.dll'))
$compile = $assembly.GetType('Erlang.Compiler.CodeGeneration', $true).GetMethod('CompileBeamFile')
$profileType = $assembly.GetType('Erlang.Compiler.BeamReleaseBuildProfile', $true)
$constructor = $profileType.GetConstructors() | Where-Object { $_.GetParameters().Count -gt 5 }
$parameters = $constructor.GetParameters()
$values = [object[]]::new($parameters.Count)
$values[0] = '10.0.4'
$values[1] = $sourcePath
$values[2] = [IO.Path]::GetDirectoryName($sourcePath)
$values[3] = [string[]]@()
$values[4] = [string[]]@('inline')
for ($index = 5; $index -lt $parameters.Count; $index++) {
    $values[$index] = $parameters[$index].DefaultValue
}
$profile = $constructor.Invoke($values)
$outputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
[IO.File]::WriteAllBytes($outputPath, $compile.Invoke($null, [object[]]@($sourcePath, $null, $null, $profile)))
$oracle = @'
[S,P]=init:get_plain_arguments(),
{ok,M,R}=compile:file(S,[binary,inline]),
{ok,C}=file:read_file(P),
Read=fun(Bin) ->
    {beam_file,M,_,_,_,Fs}=beam_disasm:file(Bin),
    [{function,run,3,_,Body}]=[F||F={function,run,3,_,_}<-Fs],
    []=[F||F={function,fold,2,_,_}<-Fs],
    Names=lists:sort([N||{function,N,3,_,_}<-Fs, lists:prefix("-run/3-anonymous-",atom_to_list(N))]),
    2=length(Names),
    2=length([I||I={call_ext,3,{extfunc,lists,foldl,3}}<-Body]),
    {Names,length(Body)}
end,
{Names,ReferenceCount}=Read(R),
{Names,GeneratedCount}=Read(C),
io:format("Shared callback names ~0p. Entry instructions OTP ~B, C# ~B.~n",[Names,ReferenceCount,GeneratedCount]),
lists:foreach(fun(Bin) ->
    {module,M}=code:load_binary(M,S,Bin),
    {[14,12],[24,22]}=M:run([2,4],10,20),
    {[],[]}=M:run([],10,20),
    true=code:delete(M), code:purge(M)
end,[R,C]),
io:format("Both images inline the helper and return the same values.~n"),halt().
'@
$env:ERL_CRASH_DUMP_SECONDS = '0'
& "$OtpRoot/bin/erl.exe" -noshell -eval $oracle -extra $sourcePath $outputPath
exit $LASTEXITCODE
