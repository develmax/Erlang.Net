from pathlib import Path
import json,hashlib
r=Path.cwd();old=r/'artifacts/part328-remote-beam310';new=r/'artifacts/part329-remote-new-review';new.mkdir(exist_ok=True);(new/'monitor').mkdir(exist_ok=True)
for name in ['map-and-status.ps1','download-terminal.ps1','extract-and-freeze.py']:
 s=(old/name).read_text().replace('part328-remote-beam310','part329-remote-new-review').replace('part328','part329').replace('bb71b9ce04b21e4ec58ba1d62028f3a2c33c3612','d83ed875d0c77939e20049afcb46fa297fee82dd')
 if name=='map-and-status.ps1':
  s=s.replace("  $expectedRuns=@{ci=38865058842;ordinary=38865257906;native=38865264886;reallocation=38865272148;distribution=38865278645}","  $expectedRuns=@{}; if (!$Initial) { $initialMap=Get-Content \"$out/mapped-runs.json\" -Raw | ConvertFrom-Json; foreach($entry in $initialMap.Runs){$expectedRuns[$entry.Kind]=$entry.RunId} }")
  s=s.replace("if($run.id -ne $expectedRuns[$role.Kind]){throw 'Exact Root-mapped328 run ID differs'}","if(!$Initial -and $run.id -ne $expectedRuns[$role.Kind]){throw 'Exact initial-mapped329 run ID differs'}")
  assert '38865' not in s and 'Root-mapped328' not in s
 (new/name).write_bytes(s.encode())
rows=[dict(Path=n,Bytes=(new/n).stat().st_size,Sha256=hashlib.sha256((new/n).read_bytes()).hexdigest().upper()) for n in ['map-and-status.ps1','download-terminal.ps1','extract-and-freeze.py']]
p=new/'source-freeze.json';p.write_bytes((json.dumps(dict(Status='SOURCE_ONLY_GET_MONITOR_NO_NETWORK_PERFORMED',Head='d83ed875d0c77939e20049afcb46fa297fee82dd',InitialMapping='Unique exact head path event attempt1 and accepted dispatch time window',SubsequentMapping='Exact frozen initial run identifiers',Roles=5,PollSpacingSeconds=120,PostAllowed=False,DownloadedCodeExecutionAllowed=False,Files=rows),indent=2)+'\n').encode());print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
