from pathlib import Path
import hashlib,json,subprocess,os,time,shutil,sys
r=Path(__file__).resolve().parent.parent;c=r/'artifacts/term-order348-candidate';o=r/'artifacts/term-order348-full-actual2'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(p):return json.loads(p.read_bytes())
def check(manifest):
 for row in read(manifest)['Files']:
  p=c/row['Path'];assert p.stat().st_size==row['Bytes'] and h(p)==row['Sha256'],row['Path']
for name in ['source-freeze.json','source-resource-closure.json','package-resources.json','readiness-inputs.json']:check(o/name)
assert h(o/'source-freeze.json')=='08A9020739E9E8C3ADA1741FDBB099779B02D41ECF3D114C24E691DF3A2A488B'
command=['C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe','-NoProfile','-ExecutionPolicy','Bypass','-File','tools/validate.ps1']
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
children=c/'artifacts/term-order-children';priorchildren=set(children.iterdir()) if children.exists() else set();(o/'children-before.json').write_text(json.dumps([p.name for p in priorchildren],indent=2)+'\n')
assert not(o/'full-exit.json').exists();start=time.monotonic()
with(o/'full-validation.log').open('wb') as log:
 process=subprocess.Popen(command,cwd=c,env=env,stdout=log,stderr=subprocess.STDOUT)
 (o/'full-process.json').write_text(json.dumps(dict(Pid=process.pid,Command=command,Candidate=str(c),CpuCount=2,TimeoutSeconds=900),indent=2)+'\n')
 timedout=False
 try:code=process.wait(timeout=900)
 except subprocess.TimeoutExpired:
  timedout=True;subprocess.run(['taskkill','/PID',str(process.pid),'/T','/F'],capture_output=True);code=process.wait(timeout=30)
receipt=dict(Command=command,ExitCode=code,TimedOut=timedout,Settled=True,ElapsedSeconds=time.monotonic()-start)
(o/'full-exit.json').write_text(json.dumps(receipt,indent=2)+'\n');print('full exit',code,'timeout',timedout,flush=True)
for source,dest in [('artifacts/tests.json','tests.json'),('artifacts/integration-results.json','integration-results.json'),('docs/supported-mfas.json','raw-supported-mfas.json')]:
 if(c/source).exists():shutil.copyfile(c/source,o/dest)
if code or timedout:sys.exit(1)
for name in ['source-freeze.json','source-resource-closure.json','package-resources.json']:check(o/name)
report=read(o/'tests.json');tests=report['Tests'];names=[x['Name'] for x in tests];previous=read(o/'previous-tests.json');oldnames=[x['Name'] for x in previous['Tests']];oldset=set(oldnames)
assert previous['Passed']==len(oldnames)==7259 and previous['Failed']==0
assert report['Failed']==0 and report['Passed']==len(names)==len(set(names));assert all(x['Status']=='Passed' for x in tests)
assert [n for n in names if n in oldset]==oldnames
added=read(o/'added-tests.json');assert added['SourceFreezeSha256']==h(o/'source-freeze.json');assert [n for n in names if n not in oldset]==added['Names'];assert len(added['Names'])==9
registry=read(o/'raw-supported-mfas.json');before=read(o/'registry-before.json');key=lambda x:(x['Module'],x['Function'],x['Arity']);assert len(registry)==len(before)==587
assert [key(x) for x in registry]==[key(x) for x in before]
assert all({k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'} for x,y in zip(before,registry))
assert sum(x['Module']=='proc_lib' for x in registry)==39 and len({x['Module'] for x in registry})==14
ledger=read(c/'docs/mfa-compatibility.json');assert len(ledger['Entries'])==587
counts={status:sum(x['Status']==status for x in ledger['Entries']) for status in ['Verified compatible','Known differences','Needs verification']};assert list(counts.values())==[5,19,563]
for row in ledger['VerifiedSourceFiles']:assert h(c/row['Path'])==row['Sha256']
integrations=read(o/'integration-results.json');expected=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed');assert integrations==expected
packages=[dict(Path=p.relative_to(c).as_posix(),Bytes=p.stat().st_size,Sha256=h(p)) for p in sorted((c/'artifacts/packages').glob('*.nupkg'))];assert len(packages)==2;(o/'packages.json').write_text(json.dumps(dict(Files=packages),indent=2)+'\n')
childrows=[]
for directory in sorted(set(children.iterdir())-priorchildren):
 p=directory/'receipt.json';v=read(p)
 assert v['Started'] and v['Settled'] and not v['TimedOut'] and v['ExitCode']==0 and not v['LifecycleFailures']
 assert v['StandardOutputComplete'] and v['StandardErrorComplete'] and v['ProcessId']>0
 assert h(directory/'stdout.txt')==v['StandardOutputSha256'] and h(directory/'stderr.txt')==v['StandardErrorSha256']
 lines=(directory/'stdout.txt').read_text().splitlines();loaded=json.loads(lines[0]);assert loaded['TermsAssemblySha256']==v['TermsDllSha256']==h(Path(loaded['TermsAssemblyPath']));assert lines[-1]=='PASS'
 if v['Operation'] in ['compare','hash']:assert 'CONSTRUCTED' in lines and v['Depth']==32768
 childrows.append(dict(Path=p.relative_to(c).as_posix(),ReceiptSha256=h(p),Value=v))
assert len(childrows)==12
expectedchildren={(family,operation,32768) for family in ['tuple','list-head','map-value','fun-environment','native-record'] for operation in ['compare','hash']}|{('tuple','shallow',6),('tuple','snapshot',0)}
assert {(x['Value']['Family'],x['Value']['Operation'],x['Value']['Depth']) for x in childrows}==expectedchildren
(o/'bounded-child-controls.json').write_text(json.dumps(dict(Status='PASS_ACTUAL_FULL348_CURRENT_TERMS12_BOUNDED_CHILD_CONTROLS',Files=childrows),indent=2)+'\n')

guard=dict(Status='PASS_ACTUAL_ISOLATED348_FULL',Passed=report['Passed'],Failed=0,SingleUninterruptedFullPass=True,ParentHead='2ae381b4f0258d9846bb5b0fc7b9f00ed46b905b',SourceFreezeSha256=h(o/'source-freeze.json'),SourceClosureSha256=h(o/'source-resource-closure.json'),SourceClosureRows=len(read(o/'source-resource-closure.json')['Files']),OwnedPaths=len(read(o/'source-freeze.json')['Files']),PackageResourceManifestSha256=h(o/'package-resources.json'),FullExitSha256=h(o/'full-exit.json'),TestReportSha256=h(o/'tests.json'),Previous7259TestReportSha256=h(o/'previous-tests.json'),AllPrevious7259OrderedNamesPreserved=True,AddedTestNames=added['Names'],Registry=587,ProcLibMfas=39,ImplementedModules=14,CompatibilityCounts=counts,CustomLimitsRestorationPending=True,RegistryNamesOrderAndNonLimitsFieldsPreserved=True,RawRegistrySha256=h(o/'raw-supported-mfas.json'),IntegrationResults=integrations,PackageOutputsManifestSha256=h(o/'packages.json'),OriginalExecution=False,MainModified=False,ExecutionPermissions='Approved outside sandbox for Windows native pipe/process validation after proven342sandboxfailure',PreflightSha256=h(o/'preflight.json'))
guard['PreSdkPackageResourceRawMismatchSha256']=h(r/'artifacts/term-order348-full-actual1/preparation-failure.json')
guard['BoundedChildControls']=12
guard['BoundedChildControlsManifestSha256']=h(o/'bounded-child-controls.json')
guard['UnownedCurrentMainRawManifestSha256']=h(o/'unowned-current-main-raw.json')
(o/'completion-guard.json').write_text(json.dumps(guard,indent=2)+'\n');print(json.dumps(guard),flush=True);print('guard SHA',h(o/'completion-guard.json'),flush=True)
