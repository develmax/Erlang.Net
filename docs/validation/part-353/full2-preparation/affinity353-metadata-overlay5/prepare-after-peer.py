"""Artifact-only corrected metadata; require Root-accepted actual patch peer first."""
from pathlib import Path
import argparse, collections, copy, hashlib, json, re, shutil, subprocess

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
BASE=ROOT/'artifacts/affinity353-metadata-overlay4'
PATCH=ROOT/'artifacts/scheduler-affinity353-idle-steal-fix-proposal3'
PARENT='0e739220c2d5d7afc66b6fb94c1f2b15d156bd90'
FREEZE_SHA='16B1A5255E268A3CC89D5E673EA5FFB4AF45B6C4AFFF4491A541AFD5E4788921'
DISPATCHER='src/Erlang.Runtime/RuntimeActorDispatcher.cs'
DISPATCHER_SHA='0AF57D1F9F65CE153129E6D9718B3F545F7CE83FFE409E6889F51957040C3555'
REPORT_PATH='docs/validation/part-353/idle-steal-preservation/report.json'
def sha(b):return hashlib.sha256(b).hexdigest().upper()
def load(p):return json.loads(p.read_bytes())
def enc(v):return (json.dumps(v,ensure_ascii=False,indent=2)+'\n').encode()
parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--accepted-peer',type=Path,required=True)
parser.add_argument('--accepted-peer-sha256',required=True)
args=parser.parse_args()
peer_path=args.accepted_peer.resolve(strict=True)
assert peer_path.is_relative_to(ROOT/'artifacts')
assert sha(peer_path.read_bytes())==args.accepted_peer_sha256.upper()
peer=load(peer_path)
assert peer['Status'].startswith('PASS_')
assert FREEZE_SHA in json.dumps(peer), 'Peer must explicitly bind exact immutable patch3 freeze'
assert sha((PATCH/'source-freeze.json').read_bytes())==FREEZE_SHA
patch=load(PATCH/'source-freeze.json')
for row in patch['Files']:
    b=(PATCH/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
dispatcher=(PATCH/'source'/DISPATCHER).read_bytes();assert sha(dispatcher)==DISPATCHER_SHA
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()==PARENT
base_plan=load(BASE/'copy-plan.json')
assert sha((BASE/'copy-plan.json').read_bytes())=='F374C5A2C7124F542CAAA199D24CC1670320458260E9A0F83CEE0B26625078E2'
overlay=OUT/'overlay';assert not overlay.exists()
shutil.copytree(BASE/'overlay',overlay)
def put(path,b):
    p=overlay/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b)
put('docs/ERLANG_COMPATIBILITY.md',(ROOT/'docs/ERLANG_COMPATIBILITY.md').read_bytes())
ledger=load(overlay/'docs/mfa-compatibility.json');old_ledger=copy.deepcopy(ledger)
guards=ledger['VerifiedSourceFiles'];assert len(guards)==89
row=next(r for r in guards if r['Path']==DISPATCHER)
assert row['Sha256']=='62C918298E09201540B631ED3DEF63722E19F1C6299F8678C429A730B912A70E'
row['Sha256']=DISPATCHER_SHA
put('docs/validation/part-353/idle-steal-preservation/source/'+DISPATCHER,dispatcher)
for item in patch['Files']:
    if item['Path']!='source/'+DISPATCHER:
        put('docs/validation/part-353/idle-steal-preservation/patch-source/'+item['Path'],(PATCH/item['Path']).read_bytes())
put('docs/validation/part-353/idle-steal-preservation/patch-source/source-freeze.json',(PATCH/'source-freeze.json').read_bytes())
put('docs/validation/part-353/idle-steal-preservation/independent-source-peer.json',peer_path.read_bytes())

otp_before=(overlay/'docs/OTP-PORTS.md').read_bytes()
sep=b'\r\n' if b'\r\n' in otp_before else b'\n'
heading,tail=otp_before.split(sep*2,1)
addition=('Part 353 idle-lane correction (2026-10-17): the first canonical run failed 7,312/7,313 on the unchanged owned-actor capacity/continuation/retirement HOST timeout; all sixteen new contracts passed. The ten-phase diagnostic retained separate bounded-capacity failures and other passing phases. Source review identifies a reachable idle-steal liveness gap consistent with the first-wave timeouts; it is not a captured historical queue snapshot. When local Select returns null on an online lane, the correction retries the existing bound-aware TrySteal and Select before declaring that pump idle. Existing nonempty priority selection, ownership, bound/offline filters and test timeouts are unchanged. Pinned erl_process.c at ad05823719d77c8faee87348ea39513d4e2f99c5 lines 9881, 9907 and 9939 guides the normal empty-run-queue steal-before-wait behavior; this small CLR adaptation does not implement the complete native load balancer, dirty schedulers or physical CPU scheduling. The accepted patch/source peer is retained under validation/part-353/idle-steal-preservation. Corrected managed focus, a fresh canonical full run, original five-case profile and same-head CI remain separate future gates; no repaired full PASS or native/performance result is claimed by this register correction.\n\n').encode().replace(b'\n',sep)
otp_after=heading+sep*2+addition+tail
assert otp_after.endswith(tail)
put('docs/OTP-PORTS.md',otp_after)
report={'Status':'SOURCE_ONLY_ACCEPTED_IDLE_STEAL_PATCH_RETAINED16_DEPENDENCY_REVIEW_NOT_FULL_ACCEPTANCE',
 'ParentHead':PARENT,'PatchFreezeSha256':FREEZE_SHA,'DispatcherParentSha256':'62C918298E09201540B631ED3DEF63722E19F1C6299F8678C429A730B912A70E','DispatcherSha256':DISPATCHER_SHA,
 'IndependentPeerSha256':sha(peer_path.read_bytes()),'ChangedRuntimeSourceFiles':1,'RuntimeSourceGuardCount':89,
 'PreservationArgument':['Five added lines run only after existing local Select returns null and lane.Online is true, while retaining the same dispatcher gate. Existing nonempty local priority selection and low-priority counter paths are unchanged.','The existing bound-aware TrySteal filters donors and bound/offline work; a second existing Select uses normal admission. No process context is replaced, task or code-image delegate altered, cancellation gate bypassed, or guest term/predicate representation modified.','self/0 and node/0 retain actual process/runtime identity; fourteen unary type predicates retain exact handler, guard/import/named/apply/matchspec/type-tag branches. Only the idle availability of an admitted logical lane changes; complete prior observable scopes and all prior evidence are preserved.','Independent peer accepted the concrete source patch. This is a current dependency preservation review for the existing16 classifications, not certification of scheduler options or proof that source changes repaired the whole failed full.'],
 'PriorLedgerSha256':sha((BASE/'overlay/docs/mfa-compatibility.json').read_bytes()),'GuardChangedPaths':[DISPATCHER],
 'All16VerifiedScopesLimitsAndPriorEvidencePrefixExact':True,'Registry13LimitsExactMetadata4':True,'Counts':{'Verified':16,'Known':24,'Needs':547,'Registry':587},
 'RootCauseScope':'Reachable source interleaving consistent with first-wave starvation; no captured historical queue snapshot.',
 'FailedFirstFull':{'Passed':7312,'Failed':1,'All16NewPassed':True,'OriginalBudgetsAndTestsUnchanged':True,'RetainedIndependentFailureSha256':'E72ACEA3E321236202FD0582F6799825957BDAF5AF199F1E2B581C88A17B0821'},
 'OtpPortsParentSha256':sha(otp_before),'OtpPortsProposedSha256':sha(otp_after),'PriorOtpRegisterBodyExact':True,'PackageResourcesSixRequireNewOtpPortsHash':True,
 'RemainingGates':['Corrected focus','Fresh canonical full and independent audit','Original explicit two-lane native profile and same-head CI after publication'],
 'NoMainCandidateSdkNetworkGitMutation':True}
report_bytes=enc(report);put(REPORT_PATH,report_bytes)
reviewed_verified=0
for old,new in zip(old_ledger['Entries'],ledger['Entries']):
    if new['Status']=='Verified compatible':
        new['Evidence'].append({'Kind':'SourceReview','Path':REPORT_PATH.removeprefix('docs/'),'Sha256':sha(report_bytes),'Scope':'Accepted one-file idle-online-steal dependency correction preserves the existing complete observable supported-term contract and prior evidence; no scheduler API promotion or full/native success inferred.'})
        assert old['Evidence']==new['Evidence'][:-1]
        assert {k:v for k,v in old.items() if k!='Evidence'}=={k:v for k,v in new.items() if k!='Evidence'}
        reviewed_verified+=1
    else: assert old==new
assert reviewed_verified==16
assert collections.Counter(r['Status'] for r in ledger['Entries'])=={'Verified compatible':16,'Known differences':24,'Needs verification':547}
put('docs/mfa-compatibility.json',enc(ledger))
assert (overlay/'docs/supported-mfas.json').read_bytes()==(BASE/'overlay/docs/supported-mfas.json').read_bytes()
assert (overlay/'.gitattributes').read_bytes()==(BASE/'overlay/.gitattributes').read_bytes()

def final_bytes(path):
    if path==DISPATCHER:return dispatcher
    p=overlay/path
    if p.exists():return p.read_bytes()
    p=ROOT/'artifacts/scheduler-affinity353-full-candidate'/path
    return p.read_bytes() if p.exists() else (ROOT/path).read_bytes()
native=[]
for path,workflow,seal,count in [
 ('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1268),
 ('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1293)]:
    plan=load(overlay/path);assert len(plan['Files'])==count
    changes=[]
    for row in plan['Files']:
        b=final_bytes(row['Path']);assert row['HashMode'] in ['TextLF','BinaryExact']
        data=b.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else b
        if (row['Bytes'],row['Sha256'])!=(len(data),sha(data)):
            changes.append(row['Path']);row['Bytes']=len(data);row['Sha256']=sha(data)
    expected=[DISPATCHER] if count==1268 else [DISPATCHER,'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml']
    assert set(changes)==set(expected),changes
    b=enc(plan);put(path,b)
    w=final_bytes(workflow);w,n=re.subn(rb'('+seal.encode()+rb': )[A-F0-9]{64}',lambda m:m.group(1)+sha(b).encode(),w);assert n==1
    put(workflow,w);native.append({'Path':path,'Rows':count,'Sha256':sha(b),'ChangedRows':changes,'WorkflowSha256':sha(w)})
for item in native:
    for row in load(overlay/item['Path'])['Files']:
        b=final_bytes(row['Path']);data=b.replace(b'\r\n',b'\n') if row['HashMode']=='TextLF' else b
        assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
for row in guards:assert sha(final_bytes(row['Path']))==row['Sha256'],row['Path']
rows=[]
for p in sorted(overlay.rglob('*')):
    if p.is_file():
        path=p.relative_to(overlay).as_posix();b=p.read_bytes();parent=ROOT/path
        rows.append({'SourcePath':p.relative_to(ROOT).as_posix(),'TargetPath':path,'Bytes':len(b),'Sha256':sha(b),'ParentSha256':sha(parent.read_bytes()) if parent.exists() else None})
# The original66 completion rows were declared separately and are not duplicated in overlay4.
existing={r['TargetPath'] for r in rows}
for row in base_plan['Files']:
    if row['TargetPath'] not in existing:
        b=(ROOT/row['SourcePath']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256']
        parent=ROOT/row['TargetPath'];rows.append({**row,'ParentSha256':sha(parent.read_bytes()) if parent.exists() else None})
assert len(rows)==len({r['TargetPath'] for r in rows})
plan={'Status':'SOURCE_ONLY_METADATA5_AFTER_ACCEPTED_PATCH_PEER_REQUIRES_ROOT_REVIEW_AND_FRESH_FULL','ParentHead':PARENT,'PatchFreezeSha256':FREEZE_SHA,'DispatcherSourcePath':(PATCH/'source'/DISPATCHER).relative_to(ROOT).as_posix(),'DispatcherSha256':DISPATCHER_SHA,'Files':rows,'FilesCount':len(rows),'NativePlans':native,'Registry13LimitsExactMetadata4':True,'GuardCount':89,'AppendOnlyVerifiedSourceReviewCount':16,'Counts':{'Verified':16,'Known':24,'Needs':547},'PackageOtpPortsSha256':sha(otp_after),'ExactAttributesSha256':sha((overlay/'.gitattributes').read_bytes()),'PreserveCurrentMainIndexMatrix0055':True,'NoMainCandidateSdkNetworkGitMutation':True}
(OUT/'copy-plan.json').write_bytes(enc(plan))
(OUT/'source-freeze.json').write_bytes(enc({'Status':'METADATA5_SOURCE_ONLY_ROOT_REVIEW_REQUIRED','PrepareSha256':sha(Path(__file__).read_bytes()),'CopyPlanSha256':sha((OUT/'copy-plan.json').read_bytes()),'PatchFreezeSha256':FREEZE_SHA,'SourcePeerSha256':sha(peer_path.read_bytes()),'SourceReviewSha256':sha(report_bytes)}))
print(json.dumps({'PlanSha256':sha((OUT/'copy-plan.json').read_bytes()),'FreezeSha256':sha((OUT/'source-freeze.json').read_bytes()),'SourceReviewSha256':sha(report_bytes),'Files':len(rows),'NativePlans':native,'OtpPortsSha256':sha(otp_after)}))
