"""Root-only exact-plan Main apply + explicit staging; never commits or pushes.

Source proposal only until Root supplies accepted actual full2/readiness/delivery plans.
"""
from pathlib import Path, PurePosixPath
import argparse, collections, hashlib, json, subprocess, sys

ROOT=Path(__file__).resolve().parents[2]
PARENT='0e739220c2d5d7afc66b6fb94c1f2b15d156bd90'
ATTRS_RAW='CF0ED5B8661507CBE25A1B8DB3EDF8CD7D8246A2E9E3AA0E3EB0A6FC95738C85'
COMPOSITION_SHA='3F8C5F95912959D7BE22BDDA3E6F39B696AC644EA98E5461C6B67C64771F06BC'
ACTUAL_PROMOTION_SHA='22B512D445EF46BE807E4DBD8F989F5C9C90FD9416C589942B04C5C656949769'
ACTUAL_READINESS_SHA='1E13CC3287CF938BA5C685B4FE1053CD54E488D54D78542A642E44340C54D470'
ACTUAL_FULL_GUARD_SHA='AC6719076493E263AD89A55311A5B2BE79E7DB0F78946BE89EB0AE92CF260703'
EXTRA_PLAN='artifacts/part353-delivery-proposal/apply-stage-extra-copy-plan-v2.json'
EXTRA_PLAN_SHA='33C57D58FD8161FF83F7D590FF028FF1CE736438023FAE58CFB0E2465914F6BC'
COUNTS={'Verified compatible':16,'Known differences':24,'Needs verification':547}
PACKAGE_PATHS={'README.md','docs/DOTNET-LICENSE.txt','docs/DOTNET-ThirdPartyNotices.txt','docs/OTP-LICENSE.txt','docs/ROSLYN-LICENSE.txt','docs/OTP-PORTS.md'}
GENERATED={'docs/API_COMPATIBILITY.md','docs/MODULE_READINESS.md','docs/module-readiness.json'}
PRESERVE={'docs/API_COMPATIBILITY_INDEX.md','docs/ERLANG_COMPATIBILITY.md'}
# Only these declared non-source text paths may acquire CRLF->LF index conversion.
# Binary/-text attributes and all C# or proof paths always require raw identity.
EOL_ALLOWED={'CHANGELOG.md','.gitattributes','.github/workflows/nif-pending-native.yml','.github/workflows/nif-reallocation-native.yml',
 'docs/changelog/0055.md','docs/OTP-PORTS.md','docs/supported-mfas.json','docs/mfa-compatibility.json',
 'docs/module-readiness-scope.json',*GENERATED,
 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def enc(v):return (json.dumps(v,ensure_ascii=False,indent=2)+'\n').encode()
def read(p):return json.loads(p.read_bytes())
def git(*args,input=None):
    return subprocess.run(['git',*args],cwd=ROOT,input=input,stdout=subprocess.PIPE,stderr=subprocess.PIPE,check=True).stdout
def names(*args):return {p.decode('utf-8') for p in git(*args).split(b'\0') if p}
def read_path(path):
    assert isinstance(path,str) and '\\' not in path and ':' not in path
    p=PurePosixPath(path);assert not p.is_absolute() and '..' not in p.parts and p.parts
    assert not any(x.lower() in {'.git','.aws','.codex','bin','obj','cache','caches','__pycache__'} for x in p.parts)
    assert 'license.local' not in path.lower()
    target=ROOT/path;assert target.resolve().is_relative_to(ROOT) and not target.is_symlink()
    return target
def artifact(path):
    p=Path(path);p=p if p.is_absolute() else ROOT/p;p=p.resolve(strict=True)
    assert p.is_relative_to(ROOT/'artifacts') and p.is_file() and not p.is_symlink()
    return p
def input_plan(path,digest):
    p=artifact(path);assert sha(p.read_bytes())==digest.upper();return p,read(p)
def hashmode(data,mode):
    assert mode in ['TextLF','BinaryExact']
    if mode=='TextLF':
        data.decode('utf-8');return data.replace(b'\r\n',b'\n')
    return data
def index_blob(path):return git('show',':'+path)

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--root-accepted-plans',choices=['FULL2_PASS_READINESS_AND_DELIVERY_PLANS_ACCEPTED'],required=True)
p.add_argument('--promotion-plan',type=Path,required=True)
p.add_argument('--promotion-plan-sha256',required=True)
p.add_argument('--readiness-plan',type=Path,required=True)
p.add_argument('--readiness-plan-sha256',required=True)
p.add_argument('--output',type=Path,required=True)
a=p.parse_args()
assert git('rev-parse','HEAD').decode().strip()==PARENT
assert not names('diff','--name-only','-z') and not names('diff','--cached','--name-only','-z')
plan_path,plan=input_plan(a.promotion_plan,a.promotion_plan_sha256)
ready_path,ready=input_plan(a.readiness_plan,a.readiness_plan_sha256)
assert a.promotion_plan_sha256.upper()==ACTUAL_PROMOTION_SHA and a.readiness_plan_sha256.upper()==ACTUAL_READINESS_SHA
assert plan['IndependentGuardSha256']==ACTUAL_FULL_GUARD_SHA
assert plan['Status']=='ARTIFACT_ONLY_POST_INDEPENDENT_PASS_353_PROMOTION_PROPOSAL_NOT_APPLIED'
assert plan['ParentHead']==PARENT and plan['CompositionPlanSha256']==COMPOSITION_SHA
assert plan['CompositionOwnedSelected']==168 and plan['RootActualReadinessStillRequired']
assert ready['Status']=='ACTUAL_ROOT_READINESS2_GENERATED_OUTPUT_PROPOSAL_NOT_MAIN_APPLIED'
assert ready['IndependentFullGuardSha256']==plan['IndependentGuardSha256']
assert ready['Counts']==[16,24,547] and ready['LocalPassedTests']==7313 and ready['RegistryRows']==587 and ready['RuntimeSourceGuards']==89
assert ready['SdkBuildOrGuestRerun'] is False and ready['NativeAbiOrOriginalExecuted'] is False
assert len(ready['Files'])==3 and {r['TargetPath'] for r in ready['Files']}==GENERATED
rows={};payload={};derived_parent=[]
def add(row,origin,require_parent=True):
    path=row['TargetPath'];target=read_path(path)
    assert path in {'.gitattributes','CHANGELOG.md'} or path.startswith(('src/','tests/','tools/','docs/','.github/workflows/'))
    source=artifact(row['SourcePath']);b=source.read_bytes()
    assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],path
    if path in payload:
        assert payload[path]==b,'Different bytes collide at '+path
        return
    assert path not in PRESERVE
    if require_parent:
        assert 'ParentSha256' in row
        expected=row['ParentSha256']
        assert (not target.exists()) if expected is None else (target.is_file() and sha(target.read_bytes())==expected),path
    else:
        # Generated CLI plan intentionally contains only new output bytes. Its parent is
        # resolved from the pinned clean HEAD before any writes and recorded explicitly.
        expected=sha(target.read_bytes()) if target.exists() else None
        derived_parent.append({'Path':path,'ParentSha256':expected,'Authority':'Pinned clean HEAD and raw preflight snapshot; not a fabricated plan field'})
    rows[path]={**row,'SourcePath':source.relative_to(ROOT).as_posix(),'ParentSha256':expected,'Origin':origin}
    payload[path]=b
for row in plan['Files']:add(row,'Reviewed finalizer4 delivery plan')
for row in ready['Files']:add(row,'Actual Root restored-readiness generated output',False)
extra_path,extra=input_plan(EXTRA_PLAN,EXTRA_PLAN_SHA)
assert extra['ParentHead']==PARENT and len(extra['Files'])==3
for row in extra['Files']:add(row,'Explicit parent-bound one-line index correction and actual independent delivery proof')
assert not (PRESERVE & set(rows))
preserved={path:(ROOT/path).read_bytes() for path in PRESERVE}
for row in plan['PreserveCurrentMain']:assert sha((ROOT/row['Path']).read_bytes())==row['Sha256']
journal_before=(ROOT/'docs/changelog/0055.md').read_bytes()
assert sha(journal_before)==plan['Original0055PrefixSha256']
assert payload['docs/changelog/0055.md'].startswith(journal_before)
assert len(payload['docs/changelog/0055.md'])<=32768 and len(payload['docs/changelog/0055.md'].splitlines())<=400
assert sha(payload['.gitattributes'])==ATTRS_RAW

# Retain the raw CLI receipts and exact generated outputs, separately from current docs.
for source in sorted(ready_path.parent.rglob('*')):
    if not source.is_file() or source.suffix.lower() not in ['.json','.log','.txt','.md'] or '.live.' in source.name:continue
    assert source.resolve().is_relative_to(ROOT/'artifacts') and not source.is_symlink()
    target='docs/validation/part-353/root-readiness2/'+source.relative_to(ready_path.parent).as_posix()
    b=source.read_bytes();parent=ROOT/target
    add({'SourcePath':str(source),'TargetPath':target,'Bytes':len(b),'Sha256':sha(b),
         'ParentSha256':sha(parent.read_bytes()) if parent.exists() else None},'Actual raw CLI proof archive')
for source,target in [(Path(__file__).resolve(),'docs/validation/part-353/root-delivery/apply-stage-after-full2.py'),
                      (plan_path,'docs/validation/part-353/root-delivery/reviewed-promotion-plan.json')]:
    b=source.read_bytes();parent=ROOT/target
    add({'SourcePath':str(source),'TargetPath':target,'Bytes':len(b),'Sha256':sha(b),
         'ParentSha256':sha(parent.read_bytes()) if parent.exists() else None},'Exact Root delivery control source')

def prospective(path):return payload[path] if path in payload else read_path(path).read_bytes()
def verify(provider,phase):
    ledger=json.loads(provider('docs/mfa-compatibility.json'))
    assert collections.Counter(r['Status'] for r in ledger['Entries'])==COUNTS
    assert len(ledger['Entries'])==587 and len(ledger['VerifiedSourceFiles'])==89
    before=json.loads(git('show',PARENT+':docs/mfa-compatibility.json'))
    for old,new in zip(before['Entries'],ledger['Entries'],strict=True):
        assert (old['Module'],old['Function'],old['Arity'],old['Status'])==(new['Module'],new['Function'],new['Arity'],new['Status'])
        if old['Status']=='Verified compatible':
            assert old['Scope']==new['Scope'] and old['Limits']==new['Limits']
            assert old['Evidence']==new['Evidence'][:len(old['Evidence'])]
    for row in ledger['VerifiedSourceFiles']:assert row['Path'].startswith('src/') and sha(provider(row['Path']))==row['Sha256']
    proof_count=0
    for entry in ledger['Entries']:
        for proof in entry['Evidence']:assert sha(provider('docs/'+proof['Path']))==proof['Sha256'];proof_count+=1
    registry=json.loads(provider('docs/supported-mfas.json'));original=json.loads(git('show',PARENT+':docs/supported-mfas.json'))
    assert len(registry)==587
    limits=[]
    for old,new in zip(original,registry,strict=True):
        assert {k:v for k,v in old.items() if k!='Limits'}=={k:v for k,v in new.items() if k!='Limits'}
        if old['Limits']!=new['Limits']:limits.append((new['Module'],new['Function'],new['Arity']))
    expected={('erlang','spawn_opt',n) for n in [2,3,4,5]}|{('erlang','spawn_request',n) for n in [2,3,4,5]}|{('erlang','process_flag',2),('erlang','process_info',1),('erlang','process_info',2),('erlang','system_flag',2),('erlang','system_info',1)}
    assert set(limits)==expected and len(limits)==13
    ready_values=json.loads(provider('docs/module-readiness.json'))
    for key,value in {'RegisteredMfas':587,'VerifiedCompatibleMfas':16,'KnownDifferenceMfas':24,'NeedsVerificationMfas':547,'LocalPassedTests':7313,'ImplementedApiModules':14}.items():assert ready_values[key]==value
    assert ready_values['VerifiedSourceFiles']==ledger['VerifiedSourceFiles']
    native=[]
    for path,workflow,seal,count in [
     ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1268),
     ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1293)]:
        b=provider(path);manifest=json.loads(b);assert len(manifest['Files'])==count
        for row in manifest['Files']:
            value=hashmode(provider(row['Path']),row['HashMode'])
            assert len(value)==row['Bytes'] and sha(value)==row['Sha256'],(phase,row['Path'])
        assert (seal+': '+sha(b)).encode() in provider(workflow)
        native.append({'Path':path,'Rows':count,'Sha256':sha(b),'EveryRowVerified':True})
    package=[];assert len(plan['PackageInputs'])==6 and {r['Path'] for r in plan['PackageInputs']}==PACKAGE_PATHS
    for row in plan['PackageInputs']:
        b=provider(row['Path']);source=artifact(row['SourcePath']);frozen=source.read_bytes()
        assert len(frozen)==row['Bytes'] and sha(frozen)==row['Sha256']
        if phase!='staged-index':assert b==frozen
        else:assert hashmode(b,'TextLF')==hashmode(frozen,'TextLF')
        package.append({'Path':row['Path'],'TestedRawSha256':row['Sha256'],'ObservedSha256':sha(b),
                        'RawExact':b==frozen,'ComparisonMode':'RawExact' if b==frozen else 'DeclaredPackageTextLF'})
    return {'Phase':phase,'SourceGuards':89,'EvidenceRows':proof_count,'NativePlans':native,'PackageInputs':package,'CompatibilityCounts':COUNTS,'RegistryRows':587,'ChangedLimits':13}

# All source/parent/prospective semantic checks finish before Main mutation.
prospective_guard=verify(prospective,'prospective')
output=a.output.resolve();assert output.is_relative_to(ROOT/'artifacts') and not output.exists()
output.mkdir()
def write_receipt(name,value):(output/name).write_bytes(enc(value))
write_receipt('promotion-intent.json',{'Status':'ALL_PREFLIGHT_GATES_PASSED_BEFORE_MAIN_WRITE','ParentHead':PARENT,
 'PromotionPlanSha256':a.promotion_plan_sha256.upper(),'ReadinessPlanSha256':a.readiness_plan_sha256.upper(),
 'Files':list(rows.values()),'ReadinessParentSnapshots':derived_parent,'ProspectiveGuard':prospective_guard,'ExtraPlanSha256':EXTRA_PLAN_SHA,
 'PreserveCurrentIndexMatrix':{p:sha(b) for p,b in preserved.items()},'0055PrefixSha256':sha(journal_before)})
for path,b in payload.items():
    target=read_path(path);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(b)
for path,b in payload.items():assert read_path(path).read_bytes()==b
for path,b in preserved.items():assert (ROOT/path).read_bytes()==b
assert git('rev-parse','HEAD').decode().strip()==PARENT
assert not names('diff','--cached','--name-only','-z')
assert names('diff','--name-only','-z') <= set(rows)
working_guard=verify(lambda path:read_path(path).read_bytes(),'working-tree-after-write')
write_receipt('promotion-result.json',{'Status':'PASS_EXACT_MAIN353_OVERLAY_AFTER_ACCEPTED_FULL2_AND_READINESS','ParentHead':PARENT,
 'Files':list(rows.values()),'Guard':working_guard,'CurrentIndexMatrixAnd0055PrefixPreserved':True,'CommittedOrPushed':False})

# Explicit NUL pathspec avoids shell interpolation, implicit globs or incidental files.
changed=set()
for path,b in payload.items():
    tracked=bool(git('ls-files','--',path))
    cleaned=git('hash-object','--path='+path,'--stdin',input=b).strip()
    if not tracked or git('rev-parse',PARENT+':'+path).strip()!=cleaned:changed.add(path)
assert changed and all('license.local' not in path.lower() for path in changed)
spec=output/'stage-pathspec.nul';spec.write_bytes(b''.join(path.encode()+b'\0' for path in sorted(changed)))
write_receipt('stage-intent.json',{'Status':'EXPLICIT_PATHSPEC_ONLY','Paths':sorted(changed),'PathspecSha256':sha(spec.read_bytes())})
git('add','--pathspec-from-file='+str(spec),'--pathspec-file-nul')
staged=names('diff','--cached','--name-only','-z');assert staged==changed
assert not names('diff','--name-only','-z')
records=[]
for path in sorted(staged):
    working=read_path(path).read_bytes();indexed=index_blob(path)
    mode='RawExact'
    if working!=indexed:
        assert path in EOL_ALLOWED and not path.endswith('.cs') and not path.startswith('docs/validation/')
        assert hashmode(working,'TextLF')==indexed,path
        mode='TextLF'
    records.append({'Path':path,'WorkingBytes':len(working),'WorkingSha256':sha(working),'StagedBytes':len(indexed),
                    'StagedSha256':sha(indexed),'ComparisonMode':mode})
index_guard=verify(index_blob,'staged-index')
assert index_blob('.gitattributes')==payload['.gitattributes'].replace(b'\r\n',b'\n') or index_blob('.gitattributes')==payload['.gitattributes']
for path,b in preserved.items():assert (ROOT/path).read_bytes()==b
assert index_blob('docs/changelog/0055.md').startswith(git('show',PARENT+':docs/changelog/0055.md'))
assert git('rev-parse','HEAD').decode().strip()==PARENT
git('diff','--cached','--check')
write_receipt('staged-guard.json',{'Status':'PASS_EXACT353_ACCEPTED_FULL2_READINESS_EXPLICIT_STAGE_NO_COMMIT',
 'ParentHead':PARENT,'Records':records,'Paths':sorted(staged),'StagedPaths':len(staged),
 'InputPromotionPlanSha256':a.promotion_plan_sha256.upper(),'InputReadinessPlanSha256':a.readiness_plan_sha256.upper(),
 'Guard':index_guard,'ExactReviewedAttributesWorkingSha256':sha(payload['.gitattributes']),
 'RawSourceAndEvidenceIdentityPreserved':True,'DeclaredMetadataTextLfConversions':[r['Path'] for r in records if r['ComparisonMode']=='TextLF'],
 'PackageScope':'Actual full packages bind tested raw six inputs. Index comparison declares only CRLF/LF equivalence for those six text resources; no claim staged package bytes equal prior binary output.',
 'MainCurrentIndexMatrixAnd0055PrefixPreserved':True,'CommittedOrPushed':False,'SdkOrNetworkExecuted':False})
print(sha((output/'staged-guard.json').read_bytes()))
