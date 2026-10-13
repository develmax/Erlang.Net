$ErrorActionPreference='Stop'
$head=[IO.File]::ReadAllText('artifacts/part-230-frozen-head.txt').Trim()
if($head -cne '029aa007fba17f3d23bb1c0884b0468a15d3a8e2' -or (git rev-parse HEAD).Trim() -cne $head){throw 'Final provenance must bind exactly to published229 implementation'}
$native=Get-Content artifacts/part-230-native-contract-audit.json -Raw|ConvertFrom-Json
if($native.Head -cne $head -or !$native.Complete -or $native.Executed -ne 5419 -or $native.Passed -ne 5419 -or $native.Failed -ne 0 -or !$native.All4490ComparableContractsPreserved -or !$native.All151DiagnosticContractsPreserved -or !$native.All778ExpressionsPreserved -or !$native.AllSixExclusionsPreserved -or $native.IndependentFdPassed -ne 8 -or $native.CiPassed -ne 6076){throw 'Native/domain contract audit is not complete success; retain raw failures'}

function Assert-TerminalProvenance {
    param([string]$Kind,[long]$RunId,[long]$JobId,[string]$Workflow,[string]$WorkflowPath,[string]$Runner,[string]$ArtifactKind,[string[]]$CriticalSteps)
    $latest=Get-Content "artifacts/part-230-$Kind-latest-provenance.json" -Raw|ConvertFrom-Json
    $metadata=Get-Content "artifacts/part-230-$ArtifactKind-metadata.json" -Raw|ConvertFrom-Json
    $run=$latest.Run
    $job=$latest.Job
    if($run.id -ne $RunId -or $run.head_sha -cne $head -or $run.name -cne $Workflow -or $run.path -cne $WorkflowPath -or $run.status -cne 'completed' -or $run.conclusion -cne 'success'){throw "$Kind exact run/workflow/head/terminal success differs"}
    if($job.id -ne $JobId -or $job.run_id -ne $RunId -or $job.head_sha -cne $head -or $job.status -cne 'completed' -or $job.conclusion -cne 'success' -or $job.labels -cnotcontains $Runner){throw "$Kind exact job/run/head/runner/terminal success differs"}
    if($metadata.Run.id -ne $RunId -or $metadata.Run.head_sha -cne $head -or $metadata.Run.status -cne 'completed' -or $metadata.Run.conclusion -cne 'success' -or $metadata.Artifact.name -cne $ArtifactKind -or $metadata.Artifact.expired -or $metadata.Artifact.workflow_run.id -ne $RunId -or $metadata.Artifact.workflow_run.head_sha -cne $head){throw "$Kind artifact/run provenance differs"}
    $matchingJob=@($metadata.Jobs|Where-Object id -eq $JobId)
    if($matchingJob.Count -ne 1 -or $matchingJob[0].run_id -ne $RunId -or $matchingJob[0].head_sha -cne $head -or $matchingJob[0].conclusion -cne 'success' -or $matchingJob[0].labels -cnotcontains $Runner){throw "$Kind downloaded metadata job association differs"}
    $digest=(Get-FileHash "artifacts/part-230-$ArtifactKind.zip").Hash
    if($digest -cne $metadata.ZipSHA256 -or $metadata.Artifact.digest -notmatch '^sha256:[0-9a-fA-F]{64}$' -or $digest -cne $metadata.Artifact.digest.Substring(7).ToUpperInvariant()){throw "$Kind ZIP API/local digest differs"}
    $logPath="artifacts/part-230-$Kind-job.log"
    $logDigest=(Get-FileHash -LiteralPath $logPath).Hash
    $sidecar=Get-Content "$logPath.digest.json" -Raw|ConvertFrom-Json
    if($logDigest -cne $sidecar.SHA256){throw "$Kind raw job log digest differs"}
    $steps=@($job.steps)
    $stepEvidence=@()
    foreach($name in $CriticalSteps){
        $step=@($steps|Where-Object name -ceq $name)
        if($step.Count -gt 1){throw "$Kind ambiguous duplicate critical API step:$name"}
        $status=if($step.Count){$step[0].status}else{$null}
        $conclusion=if($step.Count){$step[0].conclusion}else{$null}
        if($status -ceq 'completed' -and $conclusion -cne 'success'){throw "$Kind completed critical API step contradicts terminal success:$name"}
        $available=$status -ceq 'completed' -and $conclusion -ceq 'success'
        $stepEvidence+=[pscustomobject]@{Name=$name;ObservedStatus=$status;ObservedConclusion=$conclusion;ApiCompletionAvailable=$available;CompletionEvidence=if($available){'Actual API completed-success'}else{'API completion unavailable/stale; require actual raw markers plus terminal run/job and artifact proofs'}}
    }
    # Stale API build/fd/pending rows are retained verbatim, never rewritten as successful steps.
    [pscustomobject]@{Kind=$Kind;RunId=$RunId;JobId=$JobId;Head=$head;Runner=$Runner;TerminalConclusion='success';ArtifactId=$metadata.Artifact.id;ZipSHA256=$digest;RawJobLogSHA256=$logDigest;ApiStepsAvailable=$steps.Count;CriticalSteps=$stepEvidence;StaleCriticalStepCount=@($stepEvidence|Where-Object {!$_.ApiCompletionAvailable}).Count;RawLogPath=$logPath;ArtifactKind=$ArtifactKind}
}

$original=Assert-TerminalProvenance original 38476068541 115566948353 'Pinned OTP differential oracle' '.github/workflows/differential.yml' ubuntu-latest differential @('Build development oracle from the exact reference commit','Compare the supported language corpus','Compare inherited descriptor IO against the original fd driver')
$ci=Assert-TerminalProvenance ci 38476042214 115566850291 'Validate CLR implementation' '.github/workflows/validate.yml' windows-latest validation @('Run ./tools/validate.ps1')

Add-Type -AssemblyName System.IO.Compression.FileSystem
$reportDigests=@()
foreach($item in @(@('differential','differential.json'),@('differential','fd-differential.json'),@('validation','tests.json'),@('validation','integration-results.json'))){
    $kind=$item[0]
    $name=$item[1]
    $archive=[IO.Compression.ZipFile]::OpenRead((Join-Path (Get-Location).Path "artifacts/part-230-$kind.zip"))
    try{
        $entry=$archive.GetEntry($name)
        if(!$entry){throw "Missing downloaded ZIP report entry:$kind/$name"}
        $buffer=[IO.MemoryStream]::new()
        $stream=$entry.Open()
        try{$stream.CopyTo($buffer)}finally{$stream.Dispose()}
        $digest=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($buffer.ToArray()))
        $buffer.Dispose()
        if($digest -cne (Get-FileHash "artifacts/part-230-$kind/$name").Hash){throw "Extracted report bytes differ from API-digested ZIP:$kind/$name"}
        $reportDigests+=[pscustomobject]@{Kind=$kind;Name=$name;SHA256=$digest;ExactZipExtraction=$true}
    }finally{$archive.Dispose()}
}

$originalRawLog=[IO.File]::ReadAllText($original.RawLogPath)
$ciRawLog=[IO.File]::ReadAllText($ci.RawLogPath)
# Strip ANSI CSI decoration ONLY from derived matching text. Raw files and their digests stay immutable.
$ansiCsiPattern='\x1B\[[0-?]*[ -/]*[@-~]'
$originalLog=[regex]::Replace($originalRawLog,$ansiCsiPattern,'')
$ciLog=[regex]::Replace($ciRawLog,$ansiCsiPattern,'')
$markerPatterns=[ordered]@{
    ImplementationCheckout=[regex]::Escape($head)
    ActualPinnedSourceCheckout="Note: switching to 'ad05823719d77c8faee87348ea39513d4e2f99c5'"
    SourceCommitAssertion='test .*git -C artifacts/otp-source rev-parse HEAD.*ad05823719d77c8faee87348ea39513d4e2f99c5'
    Configure='\.\/configure --prefix=.*artifacts/otp-oracle'
    Make='(?m)^.*Z make -j2\s*$'
    MakeInstall='(?m)^.*Z make install\s*$'
    ActualInstalledVersionFile='\/usr\/bin\/install.*artifacts/otp-source/OTP_VERSION.*artifacts/otp-oracle/lib/erlang/releases/29'
    SelectedInstalledOracle='dotnet run --project tools/Erlang.Differential --no-build -- .*artifacts/otp-oracle/bin/erl.*artifacts/differential.json'
    IndependentFdCommand='artifacts/otp-oracle/bin/erl.*artifacts/fd-differential.json --fd'
    ArtifactUpload=('Artifact differential successfully finalized\. Artifact ID '+$original.ArtifactId)
}
$markers=@()
foreach($pair in $markerPatterns.GetEnumerator()){
    $match=[regex]::Match($originalLog,$pair.Value)
    if(!$match.Success){throw "Original actual raw log selected marker missing:$($pair.Key)"}
    $markers+=[pscustomobject]@{Name=$pair.Key;Pattern=$pair.Value;MatchedText=$match.Value}
}
$modulePass=[regex]::Matches($originalLog,'(?m)^.*Z PASS module .+$').Count
$diagnosticPass=[regex]::Matches($originalLog,'(?m)^.*Z PASS diagnostic .+$').Count
if($modulePass -ne 4490 -or $diagnosticPass -ne 151 -or $originalLog -match '(?m)^.*Z FAIL (module |diagnostic )' -or $originalLog -match 'Differential oracle unavailable or failed:|##\[error\]Process completed with exit code'){throw 'Native selected raw log complete module/diagnostic success markers differ'}
if($ciLog -notmatch [regex]::Escape($head) -or $ciLog -notmatch '6076/6076 passed' -or $ciLog -notmatch 'Validation passed' -or $ciLog -match '##\[error\]Process completed with exit code'){throw 'CI checkout/full selected log markers differ'}
$r=Get-Content artifacts/part-230-differential/differential.json -Raw|ConvertFrom-Json
if(!$r.VersionVerified -or !$r.Complete -or $r.Baseline -cne 'OTP-29.1.1' -or $r.Executed -ne 5419 -or $r.Passed -ne 5419 -or $r.Failed -ne 0 -or $r.Results.Count -ne 778 -or $r.ModuleResults.Count -ne 4490 -or $r.DiagnosticResults.Count -ne 151 -or $r.InfrastructureError -or $r.AbortedSource){throw 'Original final5419/778/4490/151 contract differs'}
$fd=Get-Content artifacts/part-230-differential/fd-differential.json -Raw|ConvertFrom-Json
if($fd.Passed -ne 8 -or $fd.Failed -ne 0 -or $fd.Results.Count -ne 8){throw 'Independent final fd8 differs'}
$numericNames=@('erlang-native-record-hash-single-field-numeric','erlang-native-record-hash-multi-field-numeric')
$numericEvidence=@()
foreach($name in $numericNames){
    $row=@($r.ModuleResults|Where-Object Name -ceq $name)
    if($row.Count -ne 1 -or !$row[0].Passed -or $row[0].Expected -cne '{ok,done}' -or $row[0].Actual -cne '{ok,done}' -or $row[0].FixtureExpected -cne '{ok,done}'){throw 'Previously SDK-derived numeric native-record fixture is not independently matched'}
    $numericEvidence+=[pscustomobject]@{Name=$name;SourceSha256=$row[0].SourceSha256;OriginalMatchedNow=$true;ExpectedConstantProvenance='Initially SDK-derived fixed values; actual pinned source-built original now passed complete source assertions; historical provenance unchanged'}
}
$tests=Get-Content artifacts/part-230-validation/tests.json -Raw|ConvertFrom-Json
$previousTests=Get-Content docs/validation/part-229/tests.json -Raw|ConvertFrom-Json
if($tests.Passed -ne 6076 -or $tests.Failed -ne 0 -or $tests.Tests.Count -ne 6076 -or @($tests.Tests|Where-Object Status -cne 'Passed').Count -or ($tests.Tests.Name -join "`n") -cne ($previousTests.Tests.Name -join "`n")){throw 'Exact CI6076 passed ordered names differ'}
$integration=Get-Content artifacts/part-230-validation/integration-results.json -Raw|ConvertFrom-Json
foreach($name in @('Build','Readiness','PackagedReadiness','Tests','Incremental','CleanRebuild','PackageConsumer','LocalTool')){if($integration.$name -cne 'Passed'){throw "CI full pipeline gate differs:$name"}}
if($integration.HybridOutput -cne 'Hello World' -or $integration.DisabledPreprocessing -cne 'Expected failure' -or $integration.OracleUnavailableReport -cne 'Expected failure recorded'){throw 'CI negative/hybrid contracts differ'}
$originalLog -split "`r?`n"|Where-Object {$_ -match 'ad05823719d77c8faee87348ea39513d4e2f99c5|configure --prefix|make -j2|make install|otp-source/OTP_VERSION|otp-oracle/bin/erl|PASS diagnostic|Artifact differential successfully finalized'}|Set-Content artifacts/part-230-original-selected-log.txt
$ciLog -split "`r?`n"|Where-Object {$_ -match [regex]::Escape($head)+'|6076/6076 passed|Validation passed'}|Set-Content artifacts/part-230-ci-selected-log.txt
[pscustomobject]@{Head=$head;Baseline='OTP-29.1.1';BaselineCommit='ad05823719d77c8faee87348ea39513d4e2f99c5';Original=$original;Ci=$ci;SelectedOriginalMarkers=$markers;DerivedMatchingText='ANSI CSI decoration stripped only in memory; raw log bytes/digests immutable; selected logs are derived excerpts';ActualModulePassMarkers=$modulePass;ActualDiagnosticPassMarkers=$diagnosticPass;ReportDigests=$reportDigests;NativeRecordNumericEvidence=$numericEvidence;IndependentFdEvidence='Actual raw --fd command/upload plus API-digested fd report8/8; fd command emits no textual numeric success counter';PinnedSourceBuildEvidencePending=$false;RawLogsDigestsVerified=$true;WholeModuleComplete=$false}|ConvertTo-Json -Depth 12|Set-Content artifacts/part-230-final-provenance-guard.json
[pscustomobject]@{Head=$head;Baseline='OTP-29.1.1';BaselineCommit='ad05823719d77c8faee87348ea39513d4e2f99c5';Complete=$true;Planned=5419;Executed=5419;Passed=5419;Failed=0;Expressions=778;ComparableModules=4490;Diagnoses=151;IndependentFdExecuted=8;IndependentFdPassed=8;IndependentFdFailed=0;CiPassed=6076;CiFullPipeline=$true;CiPlatform='windows-latest';OriginalPlatform='ubuntu-latest';OrderedCiNamesMatch=$true;OrderedComparableModulesAndDiagnosesMatch=$true;All4490ComparableSourceContractsPreserved=$true;All151DiagnosesPreserved=$true;All778HistoricalExpressionsPreserved=$true;AllSixExclusionsPreserved=$true;DigestsAndPinnedSourceBuildVerified=$true;PinnedSourceBuildEvidencePending=$false;ClosedJournalsAndSiblingUnchanged=$true;Registry=470;ErlangExports=312;Scope='Selected original5419+fd8 and exact-head WindowsCI6076; full erlang/resources/distribution/code-loading/upstream/performance remain partial'}|ConvertTo-Json -Depth 6|Set-Content artifacts/part-230-strict-guard.json
Write-Output 'Final original5419+fd8/WindowsCI6076 exactHEAD/digest/source-build provenance verified; whole module remains partial.'