from pathlib import Path
import collections
import hashlib
import json
import re
import shutil
import subprocess

R = Path(__file__).resolve().parent.parent
C = R / 'artifacts/additional-type-predicate-candidate'
O = R / 'artifacts/additional-type-predicate350-full-source'
PARENT = '712933b97a654a3b6ed42250902017bde0740f17'
def h(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def rd(p): return json.loads(p.read_bytes())
def save(p, v):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(v, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
def cp(path):
    target = C / path
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(R / path, target)
def row(path): return dict(Path=path, Bytes=(C/path).stat().st_size, Sha256=h(C/path))

assert subprocess.check_output(['git','rev-parse','HEAD'], cwd=R, text=True).strip() == PARENT
tracked = [x for x in subprocess.check_output(['git','ls-files','-z'],cwd=R).decode().split('\0') if x]
assert not subprocess.check_output(['git','diff','--name-only'],cwd=R).strip()
integration = rd(R/'artifacts/additional-type-predicate-candidate-numeric-integration.json')
assert integration['NewLogicalFixtures'] == 18
owned = {x['Path'] for x in integration['OwnedFiles']}
assert len(owned) == 7
for x in integration['OwnedFiles']: assert row(x['Path']) == x
O.mkdir(exist_ok=False)
for path in tracked:
    if not path.startswith('docs/') and path not in owned:
        assert 'LICENSE.local' not in path and '__pycache__' not in path
        cp(path)
unowned = [row(p) for p in tracked if not p.startswith('docs/') and p not in owned]
assert all(h(C/x['Path']) == h(R/x['Path']) for x in unowned)
save(O/'unowned-main-raw.json', dict(ParentHead=PARENT, Files=unowned, RawVariances=0))
for path in ['docs/mfa-compatibility.json','docs/module-readiness-scope.json','docs/otp-inventory.json','docs/supported-mfas.json']:
    cp(path)
scope = rd(C/'docs/module-readiness-scope.json')
old_comparison = scope['ComparisonTestReport']
scope['ComparisonTestReport'] = 'validation/part-348/full/tests.json'
save(C/'docs/module-readiness-scope.json',scope)
owned.add('docs/module-readiness-scope.json')
cp('docs/'+scope['ComparisonTestReport'])
previous = rd(C/'docs'/scope['ComparisonTestReport'])
assert (previous['Passed'],previous['Failed']) == (7268,0)
assert len(previous['Tests']) == 7268 and all(t['Status']=='Passed' for t in previous['Tests'])
shutil.copyfile(C/'docs'/scope['ComparisonTestReport'],O/'previous-tests.json')
attrs=C/'.gitattributes'
assert b'docs/validation/part-350/** binary' not in attrs.read_bytes()
attrs.write_bytes(attrs.read_bytes()+b'\ndocs/validation/part-350/** binary\n')
owned.add('.gitattributes')
plans=[]
for path,wf,var,count in [
    ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1250),
    ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1275)]:
    original=rd(R/path); updated=[]
    for x in original['Files']:
        raw=(C/x['Path']).read_bytes()
        data=raw.replace(b'\r\n',b'\n') if x['HashMode']=='TextLF' else raw
        assert x['HashMode'] in ['TextLF','BinaryExact']
        updated.append(dict(Path=x['Path'],Bytes=len(data),Sha256=hashlib.sha256(data).hexdigest().upper(),HashMode=x['HashMode']))
    changed=[x['Path'] for x,y in zip(updated,original['Files']) if x!=y]
    assert len(updated)==count
    assert changed == ([] if count==1250 else ['.gitattributes']), changed
    if changed:
        save(C/path,dict(original,Files=updated));owned.add(path)
        raw=(C/wf).read_bytes(); pattern=rb'('+var.encode()+rb': )([0-9A-F]{64})'
        matches=list(re.finditer(pattern,raw));assert len(matches)==1
        assert matches[0].group(2).decode()==h(R/path)
        raw=re.sub(pattern,lambda m:m.group(1)+h(C/path).encode(),raw)
        (C/wf).write_bytes(raw);owned.add(wf)
    assert {k:v for k,v in rd(C/path).items() if k!='Files'}=={k:v for k,v in original.items() if k!='Files'}
    assert b'\r' not in (C/path).read_bytes()
    plans.append(dict(Path=path,Files=count,AddedRows=0,ChangedExistingPaths=changed,NonFilesUnchanged=True,Sha256=h(C/path),Workflow=wf,WorkflowSha256=h(C/wf)))
assert all(row(x['Path'])==x for x in integration['OwnedFiles'])
ledger=rd(C/'docs/mfa-compatibility.json')
counts=dict(collections.Counter(x['Status'] for x in ledger['Entries']))
assert len(ledger['Entries'])==587 and counts=={'Verified compatible':10,'Known differences':19,'Needs verification':558}
assert len(ledger['VerifiedSourceFiles'])==59
inputs={'docs/mfa-compatibility.json','docs/module-readiness-scope.json','docs/otp-inventory.json','docs/supported-mfas.json','reference-baseline.json','docs/'+scope['ComparisonTestReport']}
for entry in ledger['Entries']:
    for e in entry['Evidence']:
        path='docs/'+e['Path'];cp(path);inputs.add(path)
        assert h(C/path)==e['Sha256'],path
for x in ledger['VerifiedSourceFiles']: assert h(C/x['Path'])==x['Sha256']
assert rd(C/'reference-baseline.json')['commit']==ledger['Commit']
for name in ['package-resources.json']:
    package_paths=[x['Path'] for x in rd(R/'artifacts/term-order348-full-actual2'/name)['Files']]
    assert len(package_paths)==6
    for p in package_paths:cp(p);assert h(C/p)==h(R/p)
    save(O/name,dict(Files=[row(p) for p in package_paths]))
save(O/'readiness-inputs.json',dict(Files=[row(p) for p in sorted(inputs)]))
shutil.copyfile(C/'docs/supported-mfas.json',O/'registry-before.json')
assert len(rd(O/'registry-before.json'))==587
names=[]
for stem,n in [('AdditionalTypePredicate',6),('NumericBoundary',12)]:
    constants=dict((key,json.loads(value)) for key,value in re.findall(r'public const string (\w+) = ("(?:[^"\\]|\\.)*");',(C/f'tools/Erlang.Differential/{stem}CaseNames.cs').read_text()))
    keys=re.findall(stem+r'CaseNames\.(\w+),',(C/f'tools/Erlang.Differential/{stem}Cases.cs').read_text())
    assert len(keys)==n
    names.extend('oracle/compiled-module/'+constants[key] for key in keys)
assert len(names)==len(set(names))==18
assert set(names).isdisjoint(t['Name'] for t in previous['Tests'])
for path in sorted(owned):
    target=O/'source'/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(C/path,target)
freeze=dict(Status='FROZEN_SOURCE_ONLY_ADDITIONAL_PREDICATE_NUMERIC350_BEFORE_FULL',ParentHead=PARENT,Files=[row(p) for p in sorted(owned)],OriginalExecuted=False,FullExecuted=False,MainModified=False)
save(O/'source-freeze.json',freeze)
closure=set(p for p in tracked if not p.startswith('docs/'))|owned
save(O/'source-resource-closure.json',dict(ParentHead=PARENT,Files=[row(p) for p in sorted(closure)]))
save(O/'added-tests.json',dict(SourceFreezeSha256=h(O/'source-freeze.json'),Names=names))
for x in unowned:
    if x['Path'] not in owned: assert row(x['Path'])==x
save(O/'preflight.json',dict(Status='PASS_SOURCE_ONLY350_FULL_PREPARATION_REQUIRES_INDEPENDENT_PEER',ParentHead=PARENT,SourceFreezeSha256=h(O/'source-freeze.json'),SourceClosureSha256=h(O/'source-resource-closure.json'),OwnedPaths=len(owned),OwnedCSharpFiles=sum(p.endswith('.cs') for p in owned),ProductionCSharpChanges=0,ClosureRows=len(closure),PackageResources=6,ReadinessInputs=len(inputs),Registry=587,CurrentRuntimeBindings=59,CompatibilityCounts=counts,PreviousTests=7268,NewDeclaredTests=18,ExpectedFullTestsArithmeticOnly=7286,OldComparisonPointer=old_comparison,ComparisonPointer=scope['ComparisonTestReport'],Plans=plans,SevenFocusedSourceRowsPreserved=True,UnownedRawVariances=0,FullExecuted=False,SdkBuildExecuted=False,MainModified=False))
print(json.dumps(dict(SourceFreeze=h(O/'source-freeze.json'),Closure=h(O/'source-resource-closure.json'),Preflight=h(O/'preflight.json'),Owned=len(owned),Rows=len(closure),ReadinessInputs=len(inputs)),indent=2))
