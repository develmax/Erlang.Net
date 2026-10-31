import json,os,runpy
from pathlib import Path
r=Path.cwd(); b=r/'artifacts/statistics-full'; helper=runpy.run_path(str(r/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')); env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_GCConserveMemory='9'); summary=[]
for folder in ['writable-protocol-original02','identifier-original01','group-leader-original01','native-record-original01','mailbox-root-original01','append549-original03']:
 d=b/folder
 for backend in ['source','beam']:
  for profile in ['ordinary','private-owned']:
   old=d/((f'append549-{backend}-{profile}' if folder=='append549-original03' else f'final549c-{backend}-{profile}')+'.exit.json'); cmd=json.loads(old.read_text())['Command'];cmd[1]=str(b/'final550-original-host/bin/Debug/net10.0/WritableReplay.dll'); cmd[5]='final550-'; receipt=helper['owned'](cmd,d,f'final550-{backend}-{profile}',60,env,str(r)); actual=d/f'final550-{backend}-{profile}.etf'; eq=actual.read_bytes()==(d/'original.etf').read_bytes(); assert eq and receipt['ExitCode']==0
   summary.append(dict(Fixture=folder,Backend=backend,Profile=profile,Passed=True,CompleteEtfBytesEqual=eq,Bytes=actual.stat().st_size,Settled=receipt['Settled'])); (b/'final550-original24-summary.json').write_text(json.dumps(summary,indent=2)); print(folder,backend,profile,'PASS',flush=True)
