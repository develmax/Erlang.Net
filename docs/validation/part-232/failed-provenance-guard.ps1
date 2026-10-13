$ErrorActionPreference='Stop'
$head='d2e2e034fe5d0ff618184bd45eac49cec360fd57'
$tracks=@(@('original',38480191522,115580605297,'ubuntu-latest','differential'),@('ci',38480174285,115580543615,'windows-latest','validation'))
$proof=@(foreach($track in $tracks){
    $kind=$track[0];$latest=Get-Content "artifacts/part-232-$kind-latest-provenance.json" -Raw|ConvertFrom-Json
    if($latest.Run.id -ne $track[1] -or $latest.Run.head_sha -cne $head -or $latest.Run.status -cne 'completed' -or $latest.Run.conclusion -cne 'failure' -or $latest.Job.id -ne $track[2] -or $latest.Job.run_id -ne $track[1] -or $latest.Job.status -cne 'completed' -or $latest.Job.conclusion -cne 'failure' -or $latest.Job.labels -cnotcontains $track[3]){throw 'Exact failed terminal run/job/head/runner differs'}
    $log="artifacts/part-232-$kind-job.log";$digest=Get-Content "$log.digest.json" -Raw|ConvertFrom-Json
    if((Get-FileHash $log).Hash -cne $digest.SHA256){throw 'Raw job log digest differs'}
    $plain=[regex]::Replace([IO.File]::ReadAllText($log),"$([char]27)\[[0-?]*[ -/]*[@-~]",'')
    if(!$plain.Contains($head)){throw 'Implementation checkout marker absent'}
    $selected=@($plain -split "`r?`n"|Where-Object {$_ -match [regex]::Escape($head)+'|Note: switching to ''ad05823719d77c8faee87348ea39513d4e2f99c5''|checking OTP version\.\.\. 29\.1\.1|Z make -j2$|Z make install$|OTP_VERSION.*otp-oracle/lib/erlang/releases/29|Z FAIL module |Z FAIL erlang/hash-iterative|6346/6347 passed|Artifact (differential|validation) successfully finalized'})
    $selected|Set-Content "artifacts/part-232-$kind-selected-log.txt"
    if($kind -eq 'original'){
        foreach($marker in @("Note: switching to 'ad05823719d77c8faee87348ea39513d4e2f99c5'",'checking OTP version... 29.1.1','make -j2','make install','otp-oracle/bin/erl')){if(!$plain.Contains($marker)){throw 'Pinned original source/build/run marker absent'}}
        $build=@($latest.Job.steps|Where-Object name -ceq 'Build development oracle from the exact reference commit')
        if($build.Count -ne 1 -or $build[0].conclusion -cne 'success'){throw 'Actual pinned build step not successful'}
        if([regex]::Matches($plain,'(?m)^.*Z PASS module .+$').Count -ne 4735 -or [regex]::Matches($plain,'(?m)^.*Z FAIL module .+$').Count -ne 6 -or [regex]::Matches($plain,'(?m)^.*Z PASS diagnostic .+$').Count -ne 151){throw 'Actual original log pass/fail accounting differs'}
    }
    [pscustomobject]@{Kind=$kind;Run=$track[1];Job=$track[2];Runner=$track[3];Terminal='failure';RawLogSHA256=$digest.SHA256;SelectedLogSHA256=(Get-FileHash "artifacts/part-232-$kind-selected-log.txt").Hash;ActualSteps=$latest.Job.steps}
})
$reports=@(foreach($item in @(@('differential','differential.json'),@('differential','fd-differential.json'),@('validation','tests.json'))){
    $zip=[IO.Compression.ZipFile]::OpenRead((Join-Path (Get-Location).Path "artifacts/part-232-$($item[0]).zip"))
    try{$entry=$zip.GetEntry($item[1]);if(!$entry){throw 'Report not in exact downloaded ZIP'};$stream=$entry.Open();try{$hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream))}finally{$stream.Dispose()};if($hash -cne (Get-FileHash "artifacts/part-232-$($item[0])/$($item[1])").Hash){throw 'Extracted report differs from ZIP'};[pscustomobject]@{Track=$item[0];Report=$item[1];SHA256=$hash}}finally{$zip.Dispose()}
})
[pscustomobject]@{Head=$head;Pin='ad05823719d77c8faee87348ea39513d4e2f99c5';Tracks=$proof;Reports=$reports;OriginalComplete=$true;OriginalPassed=5664;OriginalFailed=6;FdPassed=8;CiPassed=6346;CiFailed=1;FullCiPassed=$false;PinnedBuildVerified=$true;NoSuccessPromotion=$true}|ConvertTo-Json -Depth 12|Set-Content artifacts/part-232-failed-provenance-guard.json
'Failed original and CI terminal/digest/source-build/report provenance verified.'
