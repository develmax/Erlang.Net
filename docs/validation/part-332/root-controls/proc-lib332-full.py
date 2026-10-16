from pathlib import Path
import json,hashlib,subprocess,time,os,shutil
r=Path(__file__).resolve().parent.parent;c=r/'artifacts/proc-lib332-candidate';o=r/'artifacts/proc-lib332-full-source'
h=lambda b:hashlib.sha256(b).hexdigest().upper()
j=lambda p:json.loads(p.read_bytes())
def save(name,value):
 with (o/name).open('xb') as f:f.write((json.dumps(value,indent=2)+'\n').encode())
closure=j(o/'source-resource-closure.json')['Files']
def exact():
 for x in closure:
  b=(c/x['Path']).read_bytes();assert len(b)==x['Bytes'] and h(b)==x['Sha256'],x['Path']
exact()
command=['pwsh','-NoLogo','-NoProfile','-File','tools/validate.ps1'];env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_USE_MSBUILD_SERVER='0',MSBUILDDISABLENODEREUSE='1')
save('full-attempt.json',dict(Command=command,SourceFreezeSha256=h((o/'source-freeze.json').read_bytes()),DeadlineSeconds=1800));start=time.monotonic()
with (o/'full-validation.log').open('xb') as f:
 process=subprocess.Popen(command,cwd=c,env=env,stdout=f,stderr=subprocess.STDOUT);timedout=False
 try:code=process.wait(timeout=1800)
 except subprocess.TimeoutExpired:
  timedout=True;subprocess.run(['taskkill','/PID',str(process.pid),'/T','/F'],capture_output=True,timeout=30);code=process.wait(timeout=30)
save('full-exit.json',dict(ExitCode=code,TimedOut=timedout,Settled=process.poll() is not None,ElapsedSeconds=time.monotonic()-start));exact();assert not timedout and code==0
report=j(c/'artifacts/tests.json');names=[x['Name'] for x in report['Tests']];old=j(r/'docs/validation/part-331/final-main/tests.json');oldnames=[x['Name'] for x in old['Tests']];oldset=set(oldnames);added=[x for x in names if x not in oldset]
assert report['Failed']==0 and report['Passed']==len(names)==len(set(names));assert len(oldnames)==6775 and [x for x in names if x in oldset]==oldnames
integrations=j(c/'artifacts/integration-results.json');expected={'Readiness':'Passed','PackagedReadiness':'Passed','Build':'Passed','Tests':'Passed','OracleUnavailableReport':'Expected failure recorded','HybridOutput':'Hello World','Incremental':'Passed','DisabledPreprocessing':'Expected failure','CleanRebuild':'Passed','PackageConsumer':'Passed','LocalTool':'Passed'};assert integrations==expected
raw=(c/'docs/supported-mfas.json').read_bytes();save('raw-supported-mfas.json',json.loads(raw));registry=json.loads(raw);before=j(o/'registry-before.json');key=lambda x:(x['Module'],x['Function'],x['Arity']);oldrows={key(x):x for x in before};assert len(before)==515 and len(registry)==len({key(x) for x in registry})==521
newrows=[x for x in registry if key(x) not in oldrows];assert {key(x) for x in newrows}=={('proc_lib',n,a) for n,a in [('start',3),('start',4),('start_link',3),('start_link',4),('init_fail',2),('init_fail',3)]}
for x in registry:
 if key(x) in oldrows:assert {k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in oldrows[key(x)].items() if k!='Limits'}
restored=[oldrows.get(key(x),x) for x in registry];(c/'docs/supported-mfas.json').write_bytes((json.dumps(restored,indent=2)+'\n').encode());assert all(next(x for x in restored if key(x)==k)==v for k,v in oldrows.items())
for name,source in [('tests.json',c/'artifacts/tests.json'),('integration-results.json',c/'artifacts/integration-results.json'),('supported-mfas.json',c/'docs/supported-mfas.json')]:shutil.copyfile(source,o/name)
exact();guard=dict(Status='PASS_ACTUAL_ISOLATED332_FULL',Passed=report['Passed'],Failed=report['Failed'],PreviousOrderedNames=len(oldnames),PreviousOrderedNamesUnchanged=True,AddedTestNames=added,Registry=len(registry),PreviousRegistry=len(before),All515PreviousRegistryRowsRestoredExact=True,ErlangMfas=sum(x['Module']=='erlang' for x in registry),ProcLibMfas=sum(x['Module']=='proc_lib' for x in registry),IntegrationResults=integrations,SourceFreezeSha256=h((o/'source-freeze.json').read_bytes()),SourceClosureSha256=h((o/'source-resource-closure.json').read_bytes()),SourceClosurePaths=len(closure),TestReportSha256=h((o/'tests.json').read_bytes()),FullExitSha256=h((o/'full-exit.json').read_bytes()),OriginalExecuted=False);save('full-guard.json',guard);print(json.dumps(guard),flush=True)
