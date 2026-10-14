$ErrorActionPreference='Stop'
$head=(git rev-parse HEAD).Trim()
if($head -eq '2c1f6dadcf97b7301b60ea153dd6895a6a1a4687'){throw 'Commit the finished reviewed part272 first'}
py -3 artifacts/reference-checkpoint272-source-guard.py $head
if($LASTEXITCODE -ne 0){throw 'Committed272 source guard failed'}
git push origin HEAD:main
if($LASTEXITCODE -ne 0){throw 'Publication failed; no workflow dispatched'}
$remote=(git ls-remote origin refs/heads/main).Split()[0]
if($remote -cne $head){throw 'Remote publication head mismatch; no dispatch'}
& artifacts/reference-checkpoint272.ps1 -Mode Dispatch
