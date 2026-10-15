$ErrorActionPreference='Stop'
$watcher=Join-Path $PSScriptRoot '../watch-get-only.ps1'
$tokens=$null;$errors=$null
$parsed=[System.Management.Automation.Language.Parser]::ParseFile((Resolve-Path $watcher),[ref]$tokens,[ref]$errors)
if($errors.Count){throw ($errors|Out-String)}
$functions=@($parsed.FindAll({param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'ConvertTo-UtcTimestamp'},$true))
if($functions.Count -ne 1){throw 'One timestamp conversion function required'}
Invoke-Expression $functions[0].Extent.Text
$originalCulture=[Threading.Thread]::CurrentThread.CurrentCulture
$originalUICulture=[Threading.Thread]::CurrentThread.CurrentUICulture
try {
 [Threading.Thread]::CurrentThread.CurrentCulture=[Globalization.CultureInfo]::GetCultureInfo('ru-RU')
 [Threading.Thread]::CurrentThread.CurrentUICulture=[Globalization.CultureInfo]::GetCultureInfo('ru-RU')
 $expected=[DateTimeOffset]::new(2026,10,15,14,37,1,[TimeSpan]::Zero)
 $samples=@(
  @{Name='InvokeRestMethod-DateTimeUtc';Value=[DateTime]::new(2026,10,15,14,37,1,[DateTimeKind]::Utc)},
  @{Name='DateTimeUnspecified-JsonUtc';Value=[DateTime]::new(2026,10,15,14,37,1,[DateTimeKind]::Unspecified)},
  @{Name='DateTimeOffset';Value=$expected},
  @{Name='IsoZuluString';Value='2026-10-15T14:37:01Z'},
  @{Name='IsoOffsetString';Value='2026-10-15T17:37:01+03:00'}
 )
 $rows=@()
 foreach($sample in $samples){
  $actual=ConvertTo-UtcTimestamp $sample.Value
  if($actual -ne $expected -or $actual.Offset -ne [TimeSpan]::Zero){throw "Timestamp control failed: $($sample.Name)"}
  $rows+=@{Name=$sample.Name;Passed=$true;InputType=$sample.Value.GetType().FullName;OutputUtc=$actual.ToString('O',[Globalization.CultureInfo]::InvariantCulture)}
 }
 $attempt=ConvertTo-UtcTimestamp '2026-10-15T14:36:58.0959292Z'
 if(!(($expected -ge $attempt.AddSeconds(-5)) -and ($expected.AddMinutes(-1) -lt $attempt.AddSeconds(-5)))){throw 'Discovery time-window control failed'}
 $postSites=@($parsed.FindAll({param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -in @('Invoke-RestMethod','Invoke-WebRequest') -and $n.Extent.Text -match '\-Method Post'},$true)).Count
 if($postSites -ne 0){throw 'GET-only watcher contains POST'}
 @{Status='WATCHER_TIMESTAMP_RU_SYNTHETIC_PASS';Culture='ru-RU';Controls=$rows;TimeWindowPassed=$true;PowerShellSyntaxPassed=$true;WatcherPostSites=$postSites;NetworkExecuted=$false;OriginalExecuted=$false;WatcherSha256=(Get-FileHash -LiteralPath $watcher).Hash;PriorWatcherSha256=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'watch-before.ps1')).Hash}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'synthetic-result.json')
 Write-Output 'Russian-culture DateTime/DateTimeOffset/ISO timestamp controls PASS; watcher POST sites=0.'
} finally {
 [Threading.Thread]::CurrentThread.CurrentCulture=$originalCulture
 [Threading.Thread]::CurrentThread.CurrentUICulture=$originalUICulture
}
