$ErrorActionPreference='Stop'
$head=[IO.File]::ReadAllText('artifacts/part-236-frozen-head.txt').Trim()
if((git rev-parse HEAD).Trim() -cne $head -or @(git diff HEAD --name-only -- src tools tests .github build examples global.json reference-baseline.json Directory.Build.props Directory.Build.targets).Count){throw 'Frozen implementation changed'}
$r=Get-Content artifacts/part-236-differential/differential.json -Raw|ConvertFrom-Json
$a=Get-Content docs/validation/part-235/corpus-audit.json -Raw|ConvertFrom-Json
if(!$r.Complete -or !$r.VersionVerified -or $r.Baseline -cne 'OTP-29.1.1' -or $r.InvestigationOnly -or $r.Planned -ne 5727 -or $r.Executed -ne 5727 -or $r.Passed+$r.Failed -ne 5727 -or $r.InfrastructureError -or $r.AbortedSource -or $r.Results.Count -ne 778 -or $r.ModuleResults.Count -ne 4798 -or $r.DiagnosticResults.Count -ne 151){throw 'Original run is not complete; preserve any abort separately'}
$excludedNames=@($a.Exclusions.Name)
$comparable=@($a.Fixtures|Where-Object {$excludedNames -cnotcontains $_.Name})
if($a.Fixtures.Count -ne 4804 -or $a.Diagnostics.Count -ne 151 -or $excludedNames.Count -ne 6 -or $comparable.Count -ne 4798){throw 'Immutable part231 corpus shape differs'}
if(($r.ModuleResults.Name -join "`n") -cne ($comparable.Name -join "`n") -or @($r.ModuleResults|Where-Object {$excludedNames -ccontains $_.Name}).Count){throw 'Exact comparable4798 order/exclusion absence differs'}
if(($r.DiagnosticResults.Name -join "`n") -cne ($a.Diagnostics.Name -join "`n") -or @($r.DiagnosticResults|Group-Object Name -CaseSensitive|Where-Object Count -ne 1).Count){throw 'Exact151 diagnostic order/uniqueness differs'}
$ledger=@{};foreach($f in $a.Fixtures){$ledger[$f.Name]=$f}
foreach($row in $r.ModuleResults){
 $f=$ledger[$row.Name]
 if(!$f){throw 'Unknown original fixture'}
 foreach($key in @('Source','FixtureExpected','SourceSha256','CSharpSha256','CallerContext','CompilerPolicy','ExpectedStandardOutputHex','ExpectedStandardErrorHex')){if($row.$key -cne $f.$key){throw "Fixture contract differs:$($row.Name)/$key"}}
 if(($row.ExpectedStandardOutputAlternatives|ConvertTo-Json -Compress) -cne ($f.ExpectedStandardOutputAlternatives|ConvertTo-Json -Compress)){throw 'Finite output alternatives changed'}
 if($row.Actual -cne $f.FixtureExpected){throw 'SDK independent outcome changed'}
}
if(@($r.ModuleResults|Group-Object Name|Where-Object Count -ne 1).Count){throw 'Duplicate original module rows'}
foreach($row in $r.DiagnosticResults){$f=@($a.Diagnostics|Where-Object Name -ceq $row.Name);if($f.Count -ne 1 -or $row.Source -cne $f[0].Source -or $row.SourceSha256 -cne $f[0].SourceSha256 -or $row.Expected -cne $f[0].Expected -or $row.ImplementationCode -cne $f[0].ExpectedCode -or $row.ImplementationMessage -cne $f[0].ExpectedMessage -or $row.DiagnosticKind -cne $f[0].DiagnosticKind){throw "Diagnostic contract differs:$($row.Name)"}}
$prior=Get-Content docs/validation/part-230/differential.json -Raw|ConvertFrom-Json
for($i=0;$i -lt 778;$i++){foreach($key in @('Source','Actual','ExitReason')){if($r.Results[$i].$key -cne $prior.Results[$i].$key){throw "Historical expression SDK/source contract changed:$i/$key"}}}
if(($r.OriginalOtpDefectExclusions|ConvertTo-Json -Depth 12 -Compress) -cne ($a.Exclusions|ConvertTo-Json -Depth 12 -Compress)){throw 'Exclusions changed'}
$failures=@($r.Results|Where-Object {!$_.Passed})+@($r.ModuleResults|Where-Object {!$_.Passed})+@($r.DiagnosticResults|Where-Object {!$_.Passed})
if($failures.Count -ne $r.Failed){throw 'Failure count differs'}
foreach($kind in @('differential','validation')){
 $m=Get-Content "artifacts/part-236-$kind-metadata.json" -Raw|ConvertFrom-Json
 $hash=(Get-FileHash "artifacts/part-236-$kind.zip").Hash
 if($hash -cne $m.ZipSHA256 -or $hash -cne $m.Artifact.digest.Substring(7).ToUpperInvariant() -or $m.Run.head_sha -cne $head -or $m.Artifact.workflow_run.head_sha -cne $head){throw 'Artifact digest/head provenance differs'}
}
$fd=Get-Content artifacts/part-236-differential/fd-differential.json -Raw|ConvertFrom-Json
$contracts=Get-Content docs/validation/part-230/fd-contracts.json -Raw|ConvertFrom-Json
if($fd.Passed -ne 8 -or $fd.Failed -or $fd.Results.Count -ne 8 -or $contracts.Count -ne 8){throw 'Independent fd track failed/incomplete'}
for($i=0;$i -lt 8;$i++){$row=$fd.Results[$i];$f=$contracts[$i];foreach($key in @('Name','Body','InputHex','OutputHex','ReferenceSource')){if($row.$key -cne $f.$key){throw "FD contract differs:$key"}};if(!$row.Passed -or $row.Native -cne '{ok,done}' -or $row.Sdk -cne '{ok,done}' -or $row.ActualOutputHex -cne $f.OutputHex -or $row.ExitCode -ne 0 -or $row.InfrastructureError){throw 'FD result/bytes differ'}}
$ci=Get-Content artifacts/part-236-ci-preliminary-audit.json -Raw|ConvertFrom-Json
if($ci.Head -cne $head -or $ci.Passed -ne 6407 -or !$ci.FullPipeline -or !$ci.OrderedMainNamesMatch -or !$ci.DigestsMatch -or $ci.RunnerLabels -cnotcontains 'windows-latest'){throw 'CI guard differs'}
foreach($path in @('docs/validation/part-218/closed-volume.json','docs/validation/part-225/closed-volume.json','docs/validation/part-231/closed-volume.json')){$g=Get-Content $path -Raw|ConvertFrom-Json;if((Get-FileHash $g.Volume).Hash -cne $g.WorkingCopySHA256){throw 'Closed journal changed'}}
$journalFiles=@(git ls-files 'docs/changelog/*.md'|Sort-Object)
if($journalFiles.Count -lt 2){throw 'Journal inventory missing'}
$closedFiles=@($journalFiles|Select-Object -SkipLast 1)
if(@(git diff HEAD --name-only -- $closedFiles).Count){throw 'A historical closed journal differs from frozen HEAD'}
if((git -C ../otp rev-parse HEAD).Trim() -cne '516126e9209377f003a4bcaa9822cdcb139f9a13' -or @(git -C ../otp status --porcelain).Count){throw 'Reference checkout changed'}
foreach($row in $r.ModuleResults|Where-Object {$null -ne $_.ExpectedStandardOutputHex -or $null -ne $_.ExpectedStandardErrorHex}){$f=$ledger[$row.Name];$allowed=@(@($f.ExpectedStandardOutputHex)+@($f.ExpectedStandardOutputAlternatives)|Where-Object {$null -ne $_});if($row.SdkStandardOutputHex -cnotin $allowed -or $row.SdkStandardErrorHex -cne $f.ExpectedStandardErrorHex){throw 'SDK finite byte contract changed'};$match=$row.NativeStandardOutputHex -cin $allowed -and $row.NativeStandardErrorHex -ceq $f.ExpectedStandardErrorHex;if($row.StandardStreamsMatch -ne $match -or (!$match -and $row.Passed)){throw 'Stream mismatch incorrectly reported'}}
ConvertTo-Json -InputObject @($failures) -Depth 16|Set-Content artifacts/part-236-audited-mismatches.json
[pscustomobject]@{Head=$head;Complete=$r.Complete;Executed=$r.Executed;Passed=$r.Passed;Failed=$r.Failed;All4798ComparableContractsPreserved=$true;All151DiagnosticContractsPreserved=$true;All778ExpressionsPreserved=$true;AllSixExclusionsPreserved=$true;IndependentFdPassed=8;CiPassed=6407;CiPlatform='windows-latest';DigestsMatch=$true;PinnedSourceBuildEvidencePending=$true;WholeModuleComplete=$false}|ConvertTo-Json|Set-Content artifacts/part-236-native-contract-audit.json
if($r.Failed){throw 'Original mismatches retained; do not promote success'}
& artifacts/audit-reference236-final-provenance.ps1
if($LASTEXITCODE){throw 'Final provenance guard failed'}
Write-Output 'Complete5727 original contracts +fd8 andCI6407 with final pinned build/provenance guard verified.'
