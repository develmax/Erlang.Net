"""Bounded same-failed-DLL discriminator; no rebuilding or expectation edits."""
from pathlib import Path
import hashlib,json,os,subprocess,time
r=Path.cwd();a=r/'artifacts';snapshot=a/'part328-main1-failure';d=a/'part328-main1-five-legacy-red';assert not d.exists()
j=lambda p:json.loads(p.read_bytes());sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert sha(snapshot/'capture.json')=='C1252335323D98761C5B548C07F4ED498EECEF60E51087A0438C68D3AA8A0065'
records=j(snapshot/'capture.json')['Files']
def exact():
 for row in records:
  p=snapshot/row['Path'];assert p.stat().st_size==row['Bytes'] and sha(p)==row['Sha256']
exact();d.mkdir();tool=a/'source-purge-stage315-focus/bin/Debug/net10.0/Focus.dll';assert sha(tool)=='F1428153C4656F003F8E98BD63B58A5D8D59D6CD8CAFB969E98C7E03603B1FED'
selectors=[('ConnectedIngressHostChecks',142),('DistributionRawInputHostChecks',138),('DeferredOwnerDrainHostChecks',197),('DistributionPayloadExitHostChecks',120),('DistributionPayloadMonitorHostChecks',183)]
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_USE_MSBUILD_SERVER='0',MSBUILDDISABLENODEREUSE='1');summary=[]
for name,line in selectors:
 out=d/name;out.mkdir();report=out/'actual.json';selector='Erlang.Tests.'+name+':Run';cmd=['dotnet',str(tool),str(snapshot/'bin/Erlang.Tests.dll'),str(report),selector];start=time.monotonic();proc=subprocess.Popen(cmd,stdout=subprocess.PIPE,stderr=subprocess.PIPE,env=env);timed=False
 try:stdout,stderr=proc.communicate(timeout=45)
 except subprocess.TimeoutExpired:
  timed=True;subprocess.run(['taskkill','/PID',str(proc.pid),'/T','/F'],capture_output=True,timeout=10);stdout,stderr=proc.communicate(timeout=10)
 (out/'stdout.log').write_bytes(stdout);(out/'stderr.log').write_bytes(stderr)
 v=j(report) if report.exists() else None;case=v['Cases'][0] if v and len(v['Cases'])==1 else None;error=case.get('Exception','') if case else ''
 ok=not timed and proc.returncode==1 and case is not None and case['Name']==selector and case['Status']=='FAIL' and name+'.cs:line '+str(line) in error and not any(x in error for x in ('TimeoutException','AggregateException','MissingMethodException','TypeLoadException'))
 if v:
  for identity in v['Identities']:assert sha(Path(identity['Path']))==identity['Sha256'] and Path(identity['Path']).parent.resolve()==(snapshot/'bin').resolve()
 receipt={'Selector':selector,'ExpectedOriginalAssertionLine':line,'ExitCode':proc.returncode,'OwnedProcessSettled':proc.poll() is not None,'TimedOut':timed,'ElapsedSeconds':time.monotonic()-start,'SameLegacyAssertionReproduced':ok,'Command':cmd,'RawReportSha256':sha(report) if report.exists() else None}
 (out/'process.json').write_text(json.dumps(receipt,indent=2)+'\n');summary.append(receipt);exact()
value={'Status':'PASS_SAME_FAILED_MAIN1_DLL_FIVE_EXISTING_ASSERTION_REDS' if all(x['SameLegacyAssertionReproduced'] for x in summary) else 'DIAGNOSTIC_FAILURE_OR_MISMATCH','Passed':all(x['SameLegacyAssertionReproduced'] for x in summary),'SnapshotCaptureSha256':sha(snapshot/'capture.json'),'NoBuildOrSourceEdit':True,'Cases':summary}
(d/'summary.json').write_text(json.dumps(value,indent=2)+'\n');print(json.dumps({'Passed':value['Passed'],'Sha256':sha(d/'summary.json'),'Cases':len(summary)}));raise SystemExit(0 if value['Passed'] else 1)
