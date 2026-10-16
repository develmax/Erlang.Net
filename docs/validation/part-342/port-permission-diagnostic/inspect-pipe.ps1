$ErrorActionPreference='Stop'
$root=(Get-Location).Path
$dir=Join-Path $root 'artifacts/runtime342-candidate/tests/Erlang.Tests/bin/Debug/net10.0'
foreach($name in @('Erlang.Terms','Erlang.Runtime')) { [System.Reflection.Assembly]::LoadFrom((Join-Path $dir ($name+'.dll'))) | Out-Null }
$type=[AppDomain]::CurrentDomain.GetAssemblies() | Where-Object {$_.GetName().Name -eq 'Erlang.Runtime'} | ForEach-Object {$_.GetType('Erlang.WindowsPortChildProcess')}
$method=$type.GetMethod('Pipe',[Reflection.BindingFlags]'NonPublic,Static')
$results=@()
foreach($pair in @(@($true,$true),@($false,$true),@($true,$false))) {
 try {
  $value=$method.Invoke($null,[object[]]$pair)
  $value.Item1.Dispose(); $value.Item2.Dispose()
  $results+=@{Arguments=$pair;Passed=$true}
 } catch {
  $ex=$_.Exception
  while($ex.InnerException){$ex=$ex.InnerException}
  $results+=@{Arguments=$pair;Passed=$false;Type=$ex.GetType().FullName;Message=$ex.Message;NativeErrorCode=$ex.NativeErrorCode;Stack=$ex.StackTrace}
 }
}
$results | ConvertTo-Json -Depth 8
