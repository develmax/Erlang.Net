from pathlib import Path
import hashlib,json,subprocess,os,time,shutil,sys
r=Path(__file__).resolve().parent.parent;c=r/'artifacts/runtime342-candidate';o=r/'artifacts/runtime342-full-actual2'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(p):return json.loads(p.read_bytes())
def check(manifest):
 for row in read(manifest)['Files']:
  p=c/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256'],row['Path']
for name in ['source-freeze.json','source-resource-closure.json','package-resources.json','readiness-inputs.json']:check(o/name)
assert h(o/'source-freeze.json')=='07F9E80B6E43264A7D108C146F6E650CBE101232DD710D1FDCAEC42DDDA08CED'
command=['C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe','-NoProfile','-ExecutionPolicy','Bypass','-File','tools/validate.ps1']
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
assert not(o/'full-exit.json').exists();start=time.monotonic()
with(o/'full-validation.log').open('wb') as log:
 process=subprocess.Popen(command,cwd=c,env=env,stdout=log,stderr=subprocess.STDOUT)
 timedout=False
 try:code=process.wait(timeout=900)
 except subprocess.TimeoutExpired:
  timedout=True;subprocess.run(['taskkill','/PID',str(process.pid),'/T','/F'],capture_output=True);code=process.wait(timeout=30)
receipt=dict(Command=command,ExitCode=code,TimedOut=timedout,Settled=True,ElapsedSeconds=time.monotonic()-start)
(o/'full-exit.json').write_text(json.dumps(receipt,indent=2)+'\n');print('full exit',code,'timeout',timedout,flush=True)
# Preserve all actual products before any assertions/metadata restoration.
for source,dest in [('artifacts/tests.json','tests.json'),('artifacts/integration-results.json','integration-results.json'),('docs/supported-mfas.json','raw-supported-mfas.json')]:
 if(c/source).exists():shutil.copyfile(c/source,o/dest)
if code or timedout:sys.exit(1)
for name in ['source-freeze.json','source-resource-closure.json','package-resources.json']:check(o/name)
report=read(o/'tests.json');tests=report['Tests'];names=[x['Name'] for x in tests];previous=read(o/'previous-tests.json');oldnames=[x['Name'] for x in previous['Tests']];oldset=set(oldnames)
assert previous['Passed']==len(oldnames)==7194 and previous['Failed']==0
assert report['Failed']==0 and report['Passed']==len(names)==len(set(names));assert all(x['Status']=='Passed' for x in tests)
assert [n for n in names if n in oldset]==oldnames
added=read(o/'added-tests.json');assert added['SourceFreezeSha256']==h(o/'source-freeze.json');assert [n for n in names if n not in oldset]==added['Names']
registry=read(o/'raw-supported-mfas.json');before=read(o/'registry-before.json');key=lambda x:(x['Module'],x['Function'],x['Arity']);assert len(registry)==len(before)==587
assert [key(x) for x in registry]==[key(x) for x in before]
assert all({k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'} for x,y in zip(before,registry))
assert sum(x['Module']=='proc_lib' for x in registry)==39 and len({x['Module'] for x in registry})==14
ledger=read(c/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==587
counts={status:sum(x['Status']==status for x in ledger['Entries']) for status in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[5,18,564]
for row in ledger['VerifiedSourceFiles']:assert h(c/row['Path'])==row['Sha256']
integrations=read(o/'integration-results.json');expected=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed');assert integrations==expected
packages=[dict(Path=p.relative_to(c).as_posix(),Bytes=p.stat().st_size,Sha256=h(p)) for p in sorted((c/'artifacts/packages').glob('*.nupkg'))];assert len(packages)==2;(o/'packages.json').write_text(json.dumps(dict(Files=packages),indent=2)+'\n')
guard=dict(Status='PASS_ACTUAL_ISOLATED342_FULL',Passed=report['Passed'],Failed=0,SingleUninterruptedFullPass=True,ParentHead='7b43e00608c41303c1b03a25ebd5385d12517ee5',SourceFreezeSha256=h(o/'source-freeze.json'),SourceClosureSha256=h(o/'source-resource-closure.json'),SourceClosureRows=len(read(o/'source-resource-closure.json')['Files']),OwnedPaths=len(read(o/'source-freeze.json')['Files']),PackageResourceManifestSha256=h(o/'package-resources.json'),FullExitSha256=h(o/'full-exit.json'),TestReportSha256=h(o/'tests.json'),Previous7194TestReportSha256=h(o/'previous-tests.json'),AllPrevious7194OrderedNamesPreserved=True,AddedTestNames=added['Names'],Registry=587,ProcLibMfas=39,ImplementedModules=14,CompatibilityCounts=counts,CustomLimitsRestorationPending=True,RegistryNamesOrderAndNonLimitsFieldsPreserved=True,RawRegistrySha256=h(o/'raw-supported-mfas.json'),SemanticBindingReviewSha256=h(o/'semantic-binding-source-review.json'),IntegrationResults=integrations,PackageOutputsManifestSha256=h(o/'packages.json'),OriginalExecution=False,MainModified=False,InitialLaunchPreSdkFailure=True,InitialLaunchExitSha256=h(r/'artifacts/runtime342-full-actual1/full-exit.json'),InitialLaunchLogSha256=h(r/'artifacts/runtime342-full-actual1/full-validation.log'))
(o/'completion-guard.json').write_text(json.dumps(guard,indent=2)+'\n');print(json.dumps(guard),flush=True);print('guard SHA',h(o/'completion-guard.json'),flush=True)
