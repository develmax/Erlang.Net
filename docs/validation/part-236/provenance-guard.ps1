$ErrorActionPreference='Stop'
$head='75ec3fd6b0b62541c40c21d6fb563985c37a92b6'
$tracks=@(@('original',38489508719,115611119315,'ubuntu-latest','differential','failure'),@('ci',38489506935,115611110677,'windows-latest','validation','success'),@('beam',38489515438,115611140611,'ubuntu-latest','beam-fixtures','failure'))
$proof=@(foreach($track in $tracks){
    $kind=$track[0];$latest=Get-Content "artifacts/part-236-$kind-latest-provenance.json" -Raw|ConvertFrom-Json
    $m=Get-Content "artifacts/part-236-$($track[4])-metadata.json" -Raw|ConvertFrom-Json
    if($latest.Run.id -ne $track[1] -or $latest.Run.head_sha -cne $head -or $latest.Run.status -cne 'completed' -or $latest.Run.conclusion -cne $track[5] -or $latest.Job.id -ne $track[2] -or $latest.Job.run_id -ne $track[1] -or $latest.Job.conclusion -cne $track[5] -or $latest.Job.labels -cnotcontains $track[3]){throw 'Terminal track identity differs'}
    $zipHash=(Get-FileHash "artifacts/part-236-$($track[4]).zip").Hash
    if($zipHash -cne $m.ZipSHA256 -or $zipHash -cne $m.Artifact.digest.Substring(7).ToUpperInvariant() -or $m.Artifact.workflow_run.head_sha -cne $head){throw 'Track artifact digest/head differs'}
    $log="artifacts/part-236-$kind-job.log";$digest=Get-Content "$log.digest.json" -Raw|ConvertFrom-Json
    if((Get-FileHash $log).Hash -cne $digest.SHA256){throw 'Raw log digest differs'}
    $plain=[regex]::Replace([IO.File]::ReadAllText($log),"$([char]27)\[[0-?]*[ -/]*[@-~]",'')
    if(!$plain.Contains($head)){throw 'Actual checkout marker missing'}
    if($kind -eq 'original'){
        foreach($marker in @("Note: switching to 'ad05823719d77c8faee87348ea39513d4e2f99c5'",'checking OTP version... 29.1.1','make -j2','make install','otp-oracle/bin/erl')){if(!$plain.Contains($marker)){throw 'Pinned original build/run marker missing'}}
        $build=@($latest.Job.steps|Where-Object name -ceq 'Build development oracle from the exact reference commit')
        if($build.Count -ne 1 -or $build[0].conclusion -cne 'success' -or [regex]::Matches($plain,'(?m)^.*Z PASS module .+$').Count -ne 4797 -or [regex]::Matches($plain,'(?m)^.*Z FAIL module .+$').Count -ne 1 -or [regex]::Matches($plain,'(?m)^.*Z PASS diagnostic .+$').Count -ne 151){throw 'Pinned original raw pass/fail accounting differs'}
    }
    if($kind -eq 'ci' -and (!$plain.Contains('6407/6407 passed') -or !$plain.Contains('Validation passed'))){throw 'FullCI markers missing'}
    if($kind -eq 'beam'){
        if(!$plain.Contains('PINNED_BEAM_SOURCE_VERIFIED ad05823719d77c8faee87348ea39513d4e2f99c5') -or !$plain.Contains('beam-otp-oracle/releases/29/OTP_VERSION: No such file or directory')){throw 'Actual beam path failure marker missing'}
        if(Test-Path 'artifacts/part-236-beam-fixtures/beam-fixtures/manifest.json'){throw 'Unexpected successful fixture manifest'}
    }
    $plain -split "`r?`n"|Where-Object {$_ -match [regex]::Escape($head)+'|PINNED_BEAM_|No such file or directory|Note: switching to ''ad05823719d77c8faee87348ea39513d4e2f99c5''|checking OTP version\.\.\. 29\.1\.1|Z make -j2$|Z make install$|OTP_VERSION.*otp-oracle/lib/erlang/releases/29|Z FAIL module |6407/6407 passed|Validation passed|Artifact (differential|validation|beam-fixtures) successfully finalized'}|Set-Content "artifacts/part-236-$kind-selected-log.txt"
    [pscustomobject]@{Kind=$kind;Run=$track[1];Job=$track[2];Terminal=$track[5];Artifact=$m.Artifact.id;ZipSHA256=$zipHash;RawLogSHA256=$digest.SHA256;ActualSteps=$latest.Job.steps}
})
$reports=@(foreach($item in @(@('differential','differential.json'),@('differential','fd-differential.json'),@('validation','tests.json'),@('validation','integration-results.json'),@('beam-fixtures','beam-otp-build.log'),@('beam-fixtures','beam-fixture-tool-build.log'))){
    $zip=[IO.Compression.ZipFile]::OpenRead((Join-Path (Get-Location) "artifacts/part-236-$($item[0]).zip"))
    try{$entry=$zip.GetEntry($item[1]);if(!$entry){throw 'Report missing in digest-bound ZIP'};$stream=$entry.Open();try{$hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream))}finally{$stream.Dispose()};if($hash -cne (Get-FileHash "artifacts/part-236-$($item[0])/$($item[1])").Hash){throw 'Extracted file differs from ZIP'};[pscustomobject]@{Track=$item[0];File=$item[1];SHA256=$hash}}finally{$zip.Dispose()}
})
[pscustomobject]@{Head=$head;Tracks=$proof;Files=$reports;OriginalComplete=$true;OriginalPassed=5726;OriginalFailed=1;IndependentFdPassed=8;CiPassed=6407;CiFullPipeline=$true;OriginalPinnedBuildVerified=$true;BeamAttestationFailed=$true;ActualBeamFixturesCompiled=0;ActualBeamFixturesExecuted=0;NoNativeSuccessPromotion=$true}|ConvertTo-Json -Depth 14|Set-Content artifacts/part-236-failed-provenance-guard.json
'Actual failed original1/failedBEAMpath and successfulCI/full digest-bound provenance verified.'
