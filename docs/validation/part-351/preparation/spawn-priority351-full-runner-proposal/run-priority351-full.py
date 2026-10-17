from pathlib import Path
import argparse,collections,hashlib,json,os,shutil,subprocess,time
r=Path(__file__).resolve().parents[2]
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
def save(p,v):
    with p.open('x',encoding='utf-8',newline='\n') as f:json.dump(v,f,indent=2);f.write('\n')
def check(p):
    for row in read(p)['Files']:
        source=c/row['Path'];assert source.is_file() and source.stat().st_size==row['Bytes'] and sha(source)==row['Sha256'],row['Path']
p=argparse.ArgumentParser();p.add_argument('--configuration',type=Path,required=True);p.add_argument('--configuration-sha256',required=True);p.add_argument('--peer',type=Path,required=True);p.add_argument('--peer-sha256',required=True);a=p.parse_args()
assert sha(a.configuration)==a.configuration_sha256.upper();config=read(a.configuration);assert config['ReadyForExecution'] is True
c=r/config['CandidateRelativePath'];seed=r/config['SeedRelativePath'];o=r/config['OutputRelativePath'];assert not o.exists(),'Refuse existing output before writes or process launch'
assert c.resolve()==(r/'artifacts/spawn-priority351-permanent-candidate').resolve()
assert sha(a.peer)==a.peer_sha256.upper();peer=read(a.peer);assert peer['Status'].startswith('PASS')
source=config['SourceFreezeSha256'];closure=config['SourceClosureSha256'];parent=config['ParentHead']
assert peer['SourceFreezeSha256']==source and peer['SourceClosureSha256']==closure
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=r,text=True).strip()==parent
assert sha(seed/'source-freeze.json')==source and sha(seed/'source-resource-closure.json')==closure
assert sha(seed/'previous-tests.json')==config['PreviousTestsSha256']
assert sha(seed/'registry-before.json')==config['RegistryBeforeSha256']
assert sha(seed/'package-resources.json')==config['PackageResourcesSha256']
assert sha(seed/'readiness-inputs.json')==config['ReadinessInputsSha256']
for name in ['source-freeze.json','source-resource-closure.json','package-resources.json','readiness-inputs.json']:check(seed/name)
# Refuse a repeated invocation before touching any previous receipt or launching a process.
o.mkdir(exist_ok=False)
for name in ['source-freeze.json','source-resource-closure.json','package-resources.json','readiness-inputs.json','previous-tests.json','registry-before.json','added-tests.json','preflight.json','unowned-main-raw.json']:
    shutil.copyfile(seed/name,o/name)
shutil.copyfile(a.peer,o/'preflight-independent.json')
children=c/'artifacts/term-order-children';before={p.name for p in children.iterdir()} if children.exists() else set();save(o/'children-before.json',sorted(before))
command=['C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe','-NoProfile','-ExecutionPolicy','Bypass','-File','tools/validate.ps1']
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1');start=time.monotonic();timedout=False;failures=[]
with (o/'full-validation.log').open('xb') as log:
    process=subprocess.Popen(command,cwd=c,env=env,stdout=log,stderr=subprocess.STDOUT)
    save(o/'full-process.json',dict(Pid=process.pid,Command=command,Candidate=str(c),CpuCount=2,TimeoutSeconds=900,SourceFreezeSha256=source))
    try:process.wait(timeout=900)
    except subprocess.TimeoutExpired:
        timedout=True
        try:
            killed=subprocess.run(['taskkill','/PID',str(process.pid),'/T','/F'],capture_output=True,timeout=30);(o/'kill.stdout.txt').write_bytes(killed.stdout);(o/'kill.stderr.txt').write_bytes(killed.stderr);process.wait(timeout=30)
        except Exception as error:failures.append(repr(error))
exit=dict(Command=command,Pid=process.pid,ExitCode=process.poll(),TimedOut=timedout,Settled=process.poll() is not None,ElapsedSeconds=time.monotonic()-start,LifecycleFailures=failures);save(o/'full-exit.json',exit);print(json.dumps(exit),flush=True)
for src,dst in [('artifacts/tests.json','tests.json'),('artifacts/integration-results.json','integration-results.json'),('docs/supported-mfas.json','raw-supported-mfas.json')]:
    if (c/src).exists():shutil.copyfile(c/src,o/dst)
assert exit['ExitCode']==0 and exit['Settled'] and not timedout and not failures
for name in ['source-freeze.json','source-resource-closure.json','package-resources.json']:check(o/name)
t=read(o/'tests.json');old=read(o/'previous-tests.json');names=[x['Name'] for x in t['Tests']];previous=[x['Name'] for x in old['Tests']];oldset=set(previous)
assert old['Passed']==len(previous)==7286 and old['Failed']==0 and len(oldset)==7286
assert t['Passed']==len(names)==len(set(names))==7297 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
assert [n for n in names if n in oldset]==previous
added=read(o/'added-tests.json');assert added['SourceFreezeSha256']==source and len(added['Names'])==11 and [n for n in names if n not in oldset]==added['Names']
registry=read(o/'raw-supported-mfas.json');baseline=read(o/'registry-before.json');assert len(registry)==len(baseline)==587
for x,y in zip(registry,baseline,strict=True):assert {k:v for k,v in x.items() if k!='Limits'}=={k:v for k,v in y.items() if k!='Limits'}
ledger=read(c/'docs/mfa-compatibility.json');counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));assert counts==config['CompatibilityCounts']
assert len(ledger['VerifiedSourceFiles'])==config['CurrentRuntimeSourceBindings']
for row in ledger['VerifiedSourceFiles']:assert sha(c/row['Path'])==row['Sha256']
expected=dict(Readiness='Passed',PackagedReadiness='Passed',Build='Passed',Tests='Passed',OracleUnavailableReport='Expected failure recorded',HybridOutput='Hello World',Incremental='Passed',DisabledPreprocessing='Expected failure',CleanRebuild='Passed',PackageConsumer='Passed',LocalTool='Passed');assert read(o/'integration-results.json')==expected
packages=[dict(Path=p.relative_to(c).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p)) for p in sorted((c/'artifacts/packages').glob('*.nupkg'))];assert len(packages)==2;save(o/'packages.json',dict(Files=packages))
rows=[]
for d in sorted(children.iterdir()):
    if d.name in before:continue
    p=d/'receipt.json';v=read(p);assert v['Started'] and v['Settled'] and not v['TimedOut'] and v['ExitCode']==0 and not v['LifecycleFailures'] and v['ProcessId']>0 and v['StandardOutputComplete'] and v['StandardErrorComplete']
    assert sha(d/'stdout.txt')==v['StandardOutputSha256'] and sha(d/'stderr.txt')==v['StandardErrorSha256']
    lines=(d/'stdout.txt').read_text().splitlines();loaded=json.loads(lines[0]);assert loaded['TermsAssemblySha256']==v['TermsDllSha256']==sha(Path(loaded['TermsAssemblyPath'])) and lines[-1]=='PASS'
    if v['Operation'] in ['compare','hash']:assert 'CONSTRUCTED' in lines and v['Depth']==32768
    rows.append(dict(Path=p.relative_to(c).as_posix(),ReceiptSha256=sha(p),Value=v))
matrix={(family,op,32768) for family in ['tuple','list-head','map-value','fun-environment','native-record'] for op in ['compare','hash']}|{('tuple','shallow',6),('tuple','snapshot',0)}
assert len(rows)==12 and {(x['Value']['Family'],x['Value']['Operation'],x['Value']['Depth']) for x in rows}==matrix
save(o/'bounded-child-controls.json',dict(Status='PASS_ACTUAL_FULL351_CURRENT_TERMS12_BOUNDED_CHILD_CONTROLS',Files=rows))
guard=dict(Status='PASS_ACTUAL_ISOLATED351_FULL',ParentHead=parent,Passed=7297,Failed=0,SingleUninterruptedFullPass=True,SourceFreezeSha256=source,SourceClosureSha256=closure,OwnedPaths=len(read(seed/'source-freeze.json')['Files']),SourceClosureRows=len(read(seed/'source-resource-closure.json')['Files']),PreviousTests=7286,AllPrevious7286OrderedNamesPreserved=True,AddedTestNames=added['Names'],Registry=587,ProcLibMfas=sum(x['Module']=='proc_lib' for x in registry),ImplementedModules=len({x['Module'] for x in registry}),CompatibilityCounts=counts,CurrentRuntimeSourceBindings=config['CurrentRuntimeSourceBindings'],CustomLimitsRestorationPending=True,IntegrationResults=expected,PackageOutputs=2,BoundedChildControls=12,BoundedChildManifestSha256=sha(o/'bounded-child-controls.json'),FullExitSha256=sha(o/'full-exit.json'),TestReportSha256=sha(o/'tests.json'),RawRegistrySha256=sha(o/'raw-supported-mfas.json'),IndependentPreflightSha256=sha(a.peer),OriginalExecuted=False,MainModified=False)
save(o/'completion-guard.json',guard);print(json.dumps(guard),flush=True)
