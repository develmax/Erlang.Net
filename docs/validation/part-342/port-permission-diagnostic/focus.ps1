$ErrorActionPreference='Stop'
$base=(Get-Location).Path
$dir=Join-Path $base 'artifacts/runtime342-candidate/tests/Erlang.Tests/bin/Debug/net10.0'
Get-ChildItem $dir -Filter '*.dll' | ForEach-Object { [Reflection.Assembly]::LoadFrom($_.FullName) | Out-Null }
Set-Location artifacts/runtime342-candidate
$rows=@()
foreach($fixture in [Erlang.Differential.PortNativeOptionCases]::Modules) {
 $artifact=[Erlang.Differential.GeneratedModuleCompiler]::Compile($fixture.Source)
 try {
  $task=[Erlang.Differential.CompiledModuleExecution]::Run($artifact,$fixture.CallerContext)
  $value=$task.WaitAsync([TimeSpan]::FromSeconds(15)).GetAwaiter().GetResult()
  $passed=$value.Equals($fixture.Expected)
  $rows+=@{Name=$fixture.Name;Passed=$passed;Expected=$fixture.Expected.ToString();Actual=$value.ToString()}
  Write-Host $fixture.Name $passed
 } finally {$artifact.Dispose()}
}
$rows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $base 'artifacts/runtime342-port-diagnostic/focused-escalated.json') -Encoding utf8
if(@($rows | Where-Object {-not $_.Passed}).Count){exit 1}
