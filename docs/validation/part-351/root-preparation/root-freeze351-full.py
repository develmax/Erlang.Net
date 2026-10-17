from pathlib import Path
import json,hashlib,subprocess,re,shutil,collections
r=Path.cwd();c=r/'artifacts/spawn-priority351-full-candidate';s=r/'artifacts/spawn-priority351-full-source1';parent='c1f219f31726fc93e537080f400651d300c47d72'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==parent
assert not subprocess.check_output(['git','diff','--name-only']).strip()
assert not s.exists();s.mkdir()
def row(path):return dict(Path=path,Bytes=(c/path).stat().st_size,Sha256=sha(c/path))
def save(name,v):
    with (s/name).open('x',encoding='utf-8',newline='\n') as f:json.dump(v,f,indent=2,ensure_ascii=False);f.write('\n')
perm=read(r/'artifacts/spawn-priority351-permanent-preparation2/source-freeze.json')
owned={x['Path'] for x in perm['Files']}|{'.gitattributes','docs/module-readiness-scope.json','docs/mfa-compatibility.json'};assert len(owned)==43
metadata=read(r/'artifacts/priority351-prefull-metadata-proposal/copy-plan.json')['Files']
for x in metadata:assert sha(c/x['TargetPath'])==x['Sha256']
ledger=read(c/'docs/mfa-compatibility.json');counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']));assert counts=={'Verified compatible':10,'Known differences':24,'Needs verification':553}
for x in ledger['VerifiedSourceFiles']:assert sha(c/x['Path'])==x['Sha256']
assert len(ledger['VerifiedSourceFiles'])==74
inputs={'reference-baseline.json','docs/mfa-compatibility.json','docs/supported-mfas.json','docs/module-readiness-scope.json','docs/otp-inventory.json','docs/validation/part-350/full/tests.json'}
for entry in ledger['Entries']:
    for e in entry['Evidence']:inputs.add('docs/'+e['Path']);assert sha(c/'docs'/e['Path'])==e['Sha256']
tracked=[p for p in subprocess.check_output(['git','ls-files','-z']).decode().split('\0') if p]
non_docs={p for p in tracked if not p.startswith('docs/')}
assert all((c/p).exists() for p in non_docs)
unowned=[row(p) for p in sorted(non_docs-owned)]
for x in unowned:assert sha(r/x['Path'])==x['Sha256']
save('unowned-main-raw.json',dict(ParentHead=parent,Files=unowned,RawVariances=0))
packages=[x['Path'] for x in read(r/'artifacts/additional-type-predicate350-full-source3/package-resources.json')['Files']];assert len(packages)==6
for path in packages:assert sha(c/path)==sha(r/path)
save('package-resources.json',dict(Files=[row(p) for p in packages]))
save('readiness-inputs.json',dict(Files=[row(p) for p in sorted(inputs)]))
previous=c/'docs/validation/part-350/full/tests.json';assert sha(previous)=='4D8F1EDACF32E26B882EF4E3D9F16F0F613C6C3DCDDADC4A27A0043C59A8F4F4'
assert read(previous)['Passed']==len(read(previous)['Tests'])==7286 and read(previous)['Failed']==0
shutil.copyfile(previous,s/'previous-tests.json');shutil.copyfile(c/'docs/supported-mfas.json',s/'registry-before.json');assert len(read(s/'registry-before.json'))==587
def constants(path):return {k:json.loads(v) for k,v in re.findall(r'(?:public|internal) const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;',path.read_text(encoding='utf-8'))}
host=constants(c/'tests/Erlang.Tests/SpawnPriorityTestNames.cs');program=(c/'tests/Erlang.Tests/Program.cs').read_text(encoding='utf-8')
keys=re.findall(r'Test\(\s*Erlang.Tests.SpawnPriorityTestNames\.(\w+)\s*,',program);assert len(keys)==8
added=[host[k] for k in keys]
case_name_file=[x['Path'] for x in perm['Files'] if x['Path'].startswith('tools/Erlang.Differential/') and x['Path'].endswith('CaseNames.cs')];assert len(case_name_file)==1
namepath=case_name_file[0];stem=Path(namepath).stem.removesuffix('CaseNames');names=constants(c/namepath)
catalog=(c/f'tools/Erlang.Differential/{stem}Cases.cs').read_text(encoding='utf-8');casekeys=re.findall(stem+r'CaseNames\.(\w+)\s*,',catalog);assert len(casekeys)==3
added+=['oracle/compiled-module/'+names[k] for k in casekeys];assert len(added)==len(set(added))==11 and set(added).isdisjoint(t['Name'] for t in read(previous)['Tests'])
plans=[]
for path,wf,var,n in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1262),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1287)]:
    raw=(c/path).read_bytes();assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf');plan=json.loads(raw);assert len(plan['Files'])==n
    assert {k:v for k,v in plan.items() if k!='Files'}=={k:v for k,v in read(r/path).items() if k!='Files'}
    for x in plan['Files']:
        b=(c/x['Path']).read_bytes();assert x['HashMode'] in ['TextLF','BinaryExact'];b=b.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else b
        assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256'],x['Path']
    assert re.findall(var+r':\s*([0-9A-F]{64})',(c/wf).read_text())==[sha(c/path)];plans.append(dict(Path=path,Files=n,Sha256=sha(c/path),Workflow=wf,WorkflowSha256=sha(c/wf)))
for path in sorted(owned):
    dst=s/'source'/path;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(c/path,dst)
save('source-freeze.json',dict(Status='ROOT_FROZEN_CANONICAL351_BEFORE_FULL',ParentHead=parent,Files=[row(p) for p in sorted(owned)],ProductionFiles=20,FullExecuted=False,MainModified=False))
stable=non_docs|owned|inputs|set(packages)|{x['TargetPath'] for x in metadata};stable.remove('docs/supported-mfas.json')
assert not any(p.startswith(('artifacts/','.git/')) or 'LICENSE.local' in p or '__pycache__' in p for p in stable)
save('source-resource-closure.json',dict(ParentHead=parent,GeneratedRegistryExcluded=True,Files=[row(p) for p in sorted(stable)]))
save('added-tests.json',dict(SourceFreezeSha256=sha(s/'source-freeze.json'),Names=added))
save('preflight.json',dict(Status='ROOT_SOURCE_ONLY351_PREFLIGHT_REQUIRES_INDEPENDENT_GATE',ParentHead=parent,SourceFreezeSha256=sha(s/'source-freeze.json'),SourceClosureSha256=sha(s/'source-resource-closure.json'),OwnedPaths=len(owned),OwnedCSharp=sum(p.endswith('.cs') for p in owned),ClosureRows=len(stable),ReadinessInputs=len(inputs),Registry=587,CompatibilityCounts=counts,CurrentRuntimeBindings=74,PreviousTests=7286,ExpectedNewTestsArithmetic=11,ExpectedFullTestsArithmetic=7297,PackageResources=6,SourcePlans=plans,StableClosureRegistryExcluded=True,FullExecuted=False,MainModified=False))
runner=(r/'artifacts/spawn-priority351-full-runner-proposal/run-priority351-full.py').read_text(encoding='utf-8').replace("r/'artifacts/spawn-priority351-permanent-candidate'","r/'artifacts/spawn-priority351-full-candidate'")
(s/'run-full.py').write_text(runner,encoding='utf-8',newline='\n')
config=dict(ReadyForExecution=True,CandidateRelativePath=c.relative_to(r).as_posix(),SeedRelativePath=s.relative_to(r).as_posix(),OutputRelativePath='artifacts/spawn-priority351-full-actual1',ParentHead=parent,SourceFreezeSha256=sha(s/'source-freeze.json'),SourceClosureSha256=sha(s/'source-resource-closure.json'),PreviousTestsSha256=sha(s/'previous-tests.json'),RegistryBeforeSha256=sha(s/'registry-before.json'),PackageResourcesSha256=sha(s/'package-resources.json'),ReadinessInputsSha256=sha(s/'readiness-inputs.json'),CompatibilityCounts=counts,CurrentRuntimeSourceBindings=74)
save('configuration.json',config)
print(json.dumps(dict(SourceFreeze=config['SourceFreezeSha256'],Closure=config['SourceClosureSha256'],RunnerSha256=sha(s/'run-full.py'),ConfigurationSha256=sha(s/'configuration.json'),Owned=len(owned),ClosureRows=len(stable),Readiness=len(inputs))))
