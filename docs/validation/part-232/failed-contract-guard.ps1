$ErrorActionPreference='Stop'
$head='d2e2e034fe5d0ff618184bd45eac49cec360fd57'
if((git rev-parse HEAD).Trim() -cne $head -or @(git diff HEAD --name-only -- src tools tests .github build examples global.json reference-baseline.json).Count){throw 'Frozen implementation changed'}
$report=Get-Content artifacts/part-232-differential/differential.json -Raw|ConvertFrom-Json
$baseline=Get-Content docs/validation/part-231/corpus-audit.json -Raw|ConvertFrom-Json
if(!$report.Complete -or !$report.VersionVerified -or $report.Baseline -cne 'OTP-29.1.1' -or $report.Planned -ne 5670 -or $report.Executed -ne 5670 -or $report.Passed -ne 5664 -or $report.Failed -ne 6 -or $report.InfrastructureError -or $report.AbortedSource){throw 'Actual failed original shape differs'}
$excluded=@($baseline.Exclusions.Name)
$comparable=@($baseline.Fixtures|Where-Object {$_.Name -cnotin $excluded})
if($comparable.Count -ne 4741 -or ($comparable.Name -join "`n") -cne ($report.ModuleResults.Name -join "`n")){throw 'Ordered original modules differ'}
for($index=0;$index -lt $comparable.Count;$index++){
    $expected=$comparable[$index];$actual=$report.ModuleResults[$index]
    foreach($key in @('Name','Source','FixtureExpected','SourceSha256','CSharpSha256','CallerContext','CompilerPolicy','ExpectedStandardOutputHex','ExpectedStandardErrorHex')){if($expected.$key -cne $actual.$key){throw "Module contract differs:$($expected.Name)/$key"}}
    if(($expected.ExpectedStandardOutputAlternatives|ConvertTo-Json -Compress) -cne ($actual.ExpectedStandardOutputAlternatives|ConvertTo-Json -Compress) -or $actual.Actual -cne $expected.FixtureExpected){throw 'Independent module outcome/finite alternatives changed'}
}
if($report.DiagnosticResults.Count -ne 151 -or ($report.DiagnosticResults.Name -join "`n") -cne ($baseline.Diagnostics.Name -join "`n")){throw 'Ordered151 diagnoses differ'}
for($index=0;$index -lt 151;$index++){
    $expected=$baseline.Diagnostics[$index];$actual=$report.DiagnosticResults[$index]
    foreach($key in @('Name','Source','SourceSha256','Expected','DiagnosticKind')){if($expected.$key -cne $actual.$key){throw 'Diagnostic contract differs'}}
    if($actual.ImplementationCode -cne $expected.ExpectedCode -or $actual.ImplementationMessage -cne $expected.ExpectedMessage){throw 'Diagnostic implementation differs'}
}
$previous=Get-Content docs/validation/part-230/differential.json -Raw|ConvertFrom-Json
if($report.Results.Count -ne 778){throw 'Expression count differs'}
for($index=0;$index -lt 778;$index++){foreach($key in @('Source','Actual','ExitReason')){if($report.Results[$index].$key -cne $previous.Results[$index].$key){throw 'Historical expression contract differs'}}}
if(($report.OriginalOtpDefectExclusions|ConvertTo-Json -Depth 12 -Compress) -cne ($baseline.Exclusions|ConvertTo-Json -Depth 12 -Compress)){throw 'Exclusions changed'}
$failures=@($report.Results|Where-Object {!$_.Passed})+@($report.ModuleResults|Where-Object {!$_.Passed})+@($report.DiagnosticResults|Where-Object {!$_.Passed})
if($failures.Count -ne 6){throw 'Failure accounting differs'}
foreach($kind in @('differential','validation')){
    $metadata=Get-Content "artifacts/part-232-$kind-metadata.json" -Raw|ConvertFrom-Json
    $hash=(Get-FileHash "artifacts/part-232-$kind.zip").Hash
    if($metadata.Run.head_sha -cne $head -or $metadata.Run.status -cne 'completed' -or $metadata.Run.conclusion -cne 'failure' -or $hash -cne $metadata.ZipSHA256 -or $hash -cne $metadata.Artifact.digest.Substring(7).ToUpperInvariant()){throw 'Terminal failed artifact provenance differs'}
}
$ci=Get-Content artifacts/part-232-ci-failure-audit.json -Raw|ConvertFrom-Json
if($ci.Passed -ne 6346 -or $ci.Failed -ne 1 -or !$ci.OrderedNamesPreserved -or !$ci.ZipDigestsMatch -or $ci.FullPipelinePassed){throw 'Failed CI guard differs'}
$fd=Get-Content artifacts/part-232-differential/fd-differential.json -Raw|ConvertFrom-Json
$fdBaseline=Get-Content docs/validation/part-230/fd-contracts.json -Raw|ConvertFrom-Json
if($fd.Passed -ne 8 -or $fd.Failed -or $fd.Results.Count -ne 8){throw 'FD incomplete'}
for($index=0;$index -lt 8;$index++){foreach($key in @('Name','Body','InputHex','OutputHex','ReferenceSource')){if($fd.Results[$index].$key -cne $fdBaseline[$index].$key){throw 'FD contract differs'}};if(!$fd.Results[$index].Passed -or $fd.Results[$index].ActualOutputHex -cne $fdBaseline[$index].OutputHex){throw 'FD actual bytes differ'}}
foreach($path in @('docs/validation/part-218/closed-volume.json','docs/validation/part-225/closed-volume.json','docs/validation/part-231/closed-volume.json')){$closed=Get-Content $path -Raw|ConvertFrom-Json;if((Get-FileHash $closed.Volume).Hash -cne $closed.WorkingCopySHA256){throw 'Closed journal changed'}}
if(@(git -C ../otp status --porcelain).Count -or (git -C ../otp rev-parse 'OTP-29.1.1^{commit}').Trim() -cne 'ad05823719d77c8faee87348ea39513d4e2f99c5'){throw 'Reference changed'}
$failures|ConvertTo-Json -Depth 20|Set-Content artifacts/part-232-audited-mismatches.json
[pscustomobject]@{Head=$head;Complete=$true;OriginalPlanned=5670;OriginalExecuted=5670;OriginalPassed=5664;OriginalFailed=6;Modules=4741;Diagnoses=151;Expressions=778;InheritedContractsPreserved=$true;ExclusionsUnchanged=6;IndependentFdPassed=8;CiPassed=6346;CiFailed=1;CiFullPipelinePassed=$false;ArtifactDigestsMatch=$true;PinnedSourceBuildProvenancePending=$true;WholeModuleComplete=$false}|ConvertTo-Json|Set-Content artifacts/part-232-failed-contract-guard.json
'Complete failed original contracts and independent fd8 / failed CI6346/6347 preserved; no success promotion.'
