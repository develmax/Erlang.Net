from pathlib import Path
import json,hashlib,copy,subprocess
R=Path.cwd();A=R/'artifacts';OLD=A/'priority351-corrected-metadata-proposal';O=A/'priority351-corrected-metadata-proposal2';C=A/'spawn-priority351-corrected-candidate2';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());enc=lambda v:(json.dumps(v,indent=2,ensure_ascii=False)+'\n').encode()
oldproposal=read(OLD/'proposal.json');oldplan=read(OLD/'copy-plan.json');pending={};origins={}
for row in oldplan['Files']:
 b=(R/row['SourcePath']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'];pending[row['TargetPath']]=b;origins[row['TargetPath']]=row['SourcePath']
pendingplan='tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';pendingworkflow='.github/workflows/nif-pending-native.yml';reallocplan='tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json';reallocworkflow='.github/workflows/nif-reallocation-native.yml'
# Pending stage was correct in proposal1: two runtime rows, then its workflow seal.
assert sha(pending[pendingplan])=='DEAEAD6E1DD0C955FC450CE5FEBDB6579E51F93A92D0FFD57BF514B090FB20EE'
assert ('REVIEWED_SOURCE_PLAN_SHA256: '+sha(pending[pendingplan])).encode() in pending[pendingworkflow]
# Reallocation admits the pending metadata as transitive build inputs as well as the two runtime sources.
v=json.loads(pending[reallocplan]);before=copy.deepcopy(v);changed=[]
for row in v['Files']:
 if row['Path'] in [pendingplan,pendingworkflow]:
  b=pending[row['Path']];assert row['HashMode']=='TextLF' and b'\r' not in b;row['Bytes']=len(b);row['Sha256']=sha(b);changed.append(row['Path'])
assert set(changed)=={pendingplan,pendingworkflow};oldhash=sha(pending[reallocplan]);pending[reallocplan]=enc(v);newhash=sha(pending[reallocplan]);needle=('REVIEWED_REALLOCATION_PLAN_SHA256: '+oldhash).encode();assert pending[reallocworkflow].count(needle)==1;pending[reallocworkflow]=pending[reallocworkflow].replace(needle,('REVIEWED_REALLOCATION_PLAN_SHA256: '+newhash).encode())
origins[reallocplan]='proposal1 plus exact two transitive pending metadata rows';origins[reallocworkflow]='one reallocation seal replacement for final transitive plan'
validation=[]
for p,n in [(pendingplan,1262),(reallocplan,1287)]:
 plan=json.loads(pending[p]);assert len(plan['Files'])==len({r['Path'] for r in plan['Files']})==n and b'\r' not in pending[p];rows=[]
 for row in plan['Files']:
  path=row['Path'];b=pending.get(path)
  if b is None:b=(C/path).read_bytes()
  mode=row['HashMode'];assert mode in ['TextLF','BinaryExact']
  if mode=='TextLF':b=b.replace(b'\r\n',b'\n');b.decode('utf-8')
  assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],(p,path,len(b),row['Bytes'],sha(b),row['Sha256'])
  rows.append(dict(Path=path,Bytes=len(b),Sha256=sha(b),HashMode=mode,ResolvedFrom='FinalOverlay' if path in pending else 'ExactCandidate2'))
 original=read(C/p);diff=[a['Path'] for a,b in zip(original['Files'],plan['Files']) if a!=b];expected={'src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs','src/Erlang.Runtime/RuntimePorts.cs'}|({pendingplan,pendingworkflow} if p==reallocplan else set());assert set(diff)==expected and len(diff)==len(expected)
 validation.append(dict(Plan=p,Count=n,FinalPlanSha256=sha(pending[p]),ChangedRowsFromCandidate2=diff,AllRowsVerified=True,Rows=rows))
assert ('REVIEWED_REALLOCATION_PLAN_SHA256: '+sha(pending[reallocplan])).encode() in pending[reallocworkflow]
for p in [pendingworkflow,reallocworkflow]:assert b'\r' not in pending[p]
rows=[]
for target,b in sorted(pending.items()):
 out=O/'overlay'/target;out.parent.mkdir(parents=True,exist_ok=True);assert not out.exists();out.write_bytes(b);old=C/target;rows.append(dict(SourcePath=out.relative_to(R).as_posix(),TargetPath=target,Bytes=len(b),Sha256=sha(b),CandidateParentSha256=sha(old.read_bytes()) if old.exists() else None,MainParentSha256=sha((R/target).read_bytes()) if (R/target).exists() else None,Origin=origins[target]))
assert len(rows)==9
proof=dict(Status='PASS_SOURCE_ONLY_ALL_FINAL_NATIVE_PLAN_ROWS_1262_AND1287',OrderedConstruction=['runtime2 hashes -> pendingplan two runtime rows','pendingplan rawsha -> pendingworkflow seal','runtime2+pendingplan+pendingworkflow hashes -> reallocplan four rows','reallocplan rawsha -> reallocworkflow seal'],PriorProposalPreserved=dict(Path=(OLD/'proposal.json').relative_to(R).as_posix(),Sha256=sha((OLD/'proposal.json').read_bytes())),History='Proposal1 correctly updated two runtime rows but omitted the two transitive pending metadata inputs in reallocation. Root preparation refused before SDK; its partialfullcandidate/seed3 remains historical. This revision changes metadata only; no test expectations, runtime source, statuses or data normalization changed.',Validation=validation,MainChanged=False,CandidateChanged=False,SdkExecuted=False,Network=False)
p=O/'all-native-rows-validation.json';p.write_bytes(enc(proof));proposal={k:v for k,v in oldproposal.items() if k not in ['Files','Plans','FocusStillRunning']};proposal.update(Status='SOURCE_ONLY_CORRECTED351_METADATA2_FULL_TRANSITIVE_PLANS_VALIDATED',Files=rows,FilesCount=9,Plans=[{k:v for k,v in x.items() if k!='Rows'} for x in validation],SourceValidation=dict(Path=p.relative_to(R).as_posix(),Sha256=sha(p.read_bytes())),PriorProposalSha256=sha((OLD/'proposal.json').read_bytes()),FocusEvidence='Root reports59focusedtargets exit0; not independently audited or claimed by this metadata reader.',FullStillPending=True)
p=O/'proposal.json';p.write_bytes(enc(proposal));(O/'copy-plan.json').write_bytes(enc(dict(Status=proposal['Status'],Files=rows)))
print(json.dumps(dict(ProposalSha256=sha(p.read_bytes()),CopyPlanSha256=sha((O/'copy-plan.json').read_bytes()),ValidationSha256=sha((O/'all-native-rows-validation.json').read_bytes()),PendingSha256=sha(pending[pendingplan]),ReallocationSha256=sha(pending[reallocplan]),ChangedRows=4)))
