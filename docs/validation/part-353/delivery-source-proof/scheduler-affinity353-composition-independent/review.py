import ast,hashlib,json,re
from pathlib import Path
root=Path(__file__).resolve().parents[2]
base=root/'artifacts/scheduler-affinity353-composition-plan'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def load(p):return json.loads(p.read_text(encoding='utf-8-sig'))
freeze=base/'source-freeze.json';assert sha(freeze)=='B859A65EFF2F03C3D12FF61548701158B162E28D4F9583BC64338F63747E52BF'
f=load(freeze)
for r in f['Files']:
    p=root/r['Path'];assert p.stat().st_size==r['Bytes'] and sha(p)==r['Sha256']
helper=root/'artifacts/compose-scheduler353.py';ast.parse(helper.read_bytes())
planpath=base/'composition-plan.json';assert sha(planpath)=='B13C31B4A1AC65E8BC8DD2349AD051687E1DEBB438CC4C63BE4FBC5D895CD38A'
plan=load(planpath);assert plan['ComposerSha256']==sha(helper)
rows=plan['Files'];assert len(rows)==3096 and len({r['Path'] for r in rows})==3096
for r in plan['InputPins']+rows:
    p=root/r.get('SourcePath',r['Path']);assert p.stat().st_size==r['Bytes'] and sha(p)==r['Sha256'],p
mapping={r['Path']:root/r['SourcePath'] for r in rows}
assert len(plan['StableClosure'])==3095
assert plan['StableClosure']==[r for r in rows if r['Path']!='docs/supported-mfas.json']
assert len(plan['OwnedPaths'])==159 and set(plan['OwnedPaths'])<=set(mapping)
permanent=load(root/'artifacts/scheduler-affinity353-permanent-proposal2/source-freeze.json')
for r in permanent['Files']:assert sha(mapping[r['Path']])==r['Sha256']
profile=load(root/'artifacts/scheduler-affinity-native-profile-proposal3/source-freeze.json')
for r in profile['Files']:
    if r['Path'].startswith('source/'):
        assert sha(mapping[r['Path'][7:]])==r['Sha256']
assert len(plan['PackageResources'])==6
for r in plan['PackageResources']:assert r in rows
scope=load(mapping['docs/module-readiness-scope.json']);assert scope['ComparisonTestReport']=='validation/part-351/full/tests.json'
previous=load(mapping['docs/validation/part-351/full/tests.json']);assert previous['Passed']==7297 and previous['Failed']==0 and len(previous['Tests'])==7297
ledger=load(mapping['docs/mfa-compatibility.json']);assert len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==89
counts={s:sum(r['Status']==s for r in ledger['Entries']) for s in ['Verified compatible','Known differences','Needs verification']}
assert counts=={'Verified compatible':16,'Known differences':24,'Needs verification':547}
for r in ledger['VerifiedSourceFiles']:assert sha(mapping[r['Path']])==r['Sha256']
for e in ledger['Entries']:
    for r in e['Evidence']:assert sha(mapping['docs/'+r['Path']])==r['Sha256']
plans=[]
for path,workflow,seal,count in [
 ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1268),
 ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1293)]:
    raw=mapping[path].read_bytes();assert b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf')
    pp=load(mapping[path]);assert len(pp['Files'])==count
    for r in pp['Files']:
        data=mapping[r['Path']].read_bytes()
        assert r['HashMode'] in ['TextLF','BinaryExact']
        if r['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
        assert len(data)==r['Bytes'] and hashlib.sha256(data).hexdigest().upper()==r['Sha256'],r['Path']
    ww=mapping[workflow].read_bytes();assert b'\r' not in ww and not ww.startswith(b'\xef\xbb\xbf')
    assert re.findall(seal.encode()+rb': ([A-F0-9]{64})',ww)==[sha(mapping[path]).encode()]
    plans.append({'Path':path,'Rows':count,'Sha256':sha(mapping[path]),'AllRowsAndWorkflowSealExact':True})
assert not Path(plan['Destination']).exists(),'review is before application'
result={
 'Status':'PASS_SOURCE_ONLY_353_COMPOSITION_HELPER_AND_EXACT_PLAN', 'Passed':True,
 'SourceFreezeSha256':sha(freeze),'ComposerSha256':sha(helper),'CompositionPlanSha256':sha(planpath),
 'ParentHead':plan['ParentHead'],'Rows':3096,'StableClosureRows':3095,'OwnedPaths':159,
 'Permanent42SourcePreserved':True,'NativeProfile3SourcesPreserved':True,
 'CurrentSourceGuards':89,'CompatibilityCounts':counts,'RegistryRows':len(load(mapping['docs/supported-mfas.json'])),
 'PackageResources':plan['PackageResources'],'NativePlans':plans,
 'ExpectedOldOrderedPrefix':7297,'ExpectedAdded':16,'ExpectedTotalIfFullPasses':7313,
 'HelperReview':'Approved-plan SHA and self-SHA/parent/all input hashes/all source bytes checked before fresh artifact destination creation; safe relative paths reject traversal/build/cache/license.local. Copy only frozen sources; post-copy hashes. Registry excluded from stable closure with separate before row. Direct ledger evidence and both prior350/351 reports included. Comparison explicitly351 full. No execution inferred from plan.',
 'InitialBuildGate':'Root reported permanent Tests-project build0/no warnings; reviewer has not claimed full success from source plan.',
 'RemainingGates':['Apply exact reviewed plan and freeze actual full inputs','Review bounded full runner and run actual full','Fresh original explicit two-lane profile capture and semantic audit'],
 'ComposeExecutedByReviewer':False,'SdkExecuted':False,'NativeExecuted':False,'NetworkExecuted':False,'MainModified':False}
out=Path(__file__).with_name('report.json');assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Status':result['Status'],'Report':str(out),'Sha256':sha(out)}))
