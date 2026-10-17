from pathlib import Path
import copy,json,hashlib,subprocess
R=Path.cwd();A=R/'artifacts';O=A/'priority351-corrected-metadata-proposal';C=A/'spawn-priority351-corrected-candidate2';sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
parent='add05fc61d8c6f5f024ffcdad1ea19aaea4e3725';assert subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()==parent
pending={};origins={}
def add(path,b,origin):assert path not in pending;pending[path]=b;origins[path]=origin
def encoded(v):return (json.dumps(v,indent=2,ensure_ascii=False)+'\n').encode()
peerpath=A/'spawn-priority351-port-retired-independent/report.json';peerhash=sha(peerpath.read_bytes());assert peerhash.startswith('C412'),peerhash
peer=read(peerpath);assert peer['Status']=='PASS_SOURCE_ONLY_TWO_RUNTIME_CORRECTIONS_FOR_BOUNDED_FOCUS'
proofroot='docs/validation/part-351/corrected-source-review/'
add(proofroot+'two-runtime-independent.json',peerpath.read_bytes(),peerpath.relative_to(R).as_posix())
paths=['src/Erlang.Runtime/RuntimePorts.cs','src/Erlang.Runtime/ModuleRegistry.ProcessScheduling.cs'];expected=['E519FA5E969E5400B90CD4C20634CC0846B9373454FDA946DFC27A9EFE897A7A','B2032518A4CE88DD09325AFCCDF2E02EB8738F7D1233AC77D8B30DC4A310F17D'];sources=[]
for p,h in zip(paths,expected):
 b=(C/p).read_bytes();assert sha(b)==h;add(proofroot+'source/'+p,b,str((C/p).relative_to(R)).replace('\\','/'));sources.append(dict(Path=p,Bytes=len(b),RawSha256=h,TextLFBytes=len(b.replace(b'\r\n',b'\n')),TextLFSha256=sha(b.replace(b'\r\n',b'\n'))))
ledgerpath='docs/mfa-compatibility.json';original=read(C/ledgerpath);ledger=copy.deepcopy(original);guards=ledger['VerifiedSourceFiles'];assert len(guards)==74
for x in guards:
 if x['Path']==paths[1]:assert x['Sha256']=='3BE87389672FAA120DD21EABEE50C998170C5A45D22879FC98D8C2CB237555AD';x['Sha256']=expected[1]
 else:assert sha((C/x['Path']).read_bytes())==x['Sha256'],x['Path']
review=dict(Status='SOURCE_ONLY_CORRECTED351_TEN_CONTRACT_PRESERVATION_REQUIRES_FOCUSED_AND_FULL_VALIDATION',ParentPublishedHead=parent,PriorInputLedgerSha256=sha((C/ledgerpath).read_bytes()),CompatibilityCountsUnchanged=[10,24,553],RuntimeGuards=74,OnlyUpdatedGuard=paths[1],CorrectedSourceFiles=sources,IndependentRuntimePeer=dict(Path=proofroot+'two-runtime-independent.json',Sha256=peerhash),Argument=['Existing ten primitive self/node/type predicate handlers and guard/admission argument remain source-identical outside exact existing scheduling gate correction. All other73 guarded dependency hashes revalidated against currentcandidate2.','Scheduling delta adds noproc only for an already cancelled context absent from the process registry, before selectedbody/errorhandler. Halt still first, live cancellation remains OCE, unchanged catch disposes selected code lease/restores flow. This is managed dead-context HOST contract, not an OTP deadguest caller capability.','RuntimePorts changes only three infrastructure Write awaits to ConfigureAwait(false); queued-byte finally, chaining, lock/tracking and caller behavior unchanged. It is not on the current ten primitive handler/guard dependency graph; no new Verified API inferred from portbehavior.','Current published four Tool display-label files are reporting source only, separate from runtime guard graph. Future generated Markdown must retain explicit100%compatible/pendinglabels.','Current59target focused process is running; this source review claims no focus/build/full success, no new native or current351CI evidence. After focused/source gate acceptance, freeze metadata as explicitnewfullinput. Original failedfull43/3002 and F8 historical evidence remain unchanged.'],PreservedTenScopeLimitsAndPriorEvidence=True,NoStatusPromotions=True,FocusCompletedClaim=False,FullCompletedClaim=False,NativeExecuted=False,SdkExecuted=False,MainChanged=False,CandidateChanged=False)
reviewbytes=encoded(review);reviewpath=proofroot+'preservation.json';add(reviewpath,reviewbytes,'authored source-only narrow preservation supplement')
evidence=dict(Kind='SourceReview',Path='validation/part-351/corrected-source-review/preservation.json',Sha256=sha(reviewbytes),Scope='Source-only narrow corrected351 admission/port-boundary review; ten existing verified scopes and prior evidence retained,74 current dependency bindings. Focus/full still pending; no new API promotion.')
changed=[]
for before,after in zip(original['Entries'],ledger['Entries']):
 if after['Status']=='Verified compatible':after['Evidence'].append(evidence);changed.append((after['Module'],after['Function'],after['Arity']));assert before['Scope']==after['Scope'] and before['Limits']==after['Limits'] and after['Evidence'][:-1]==before['Evidence']
 else:assert before==after
assert len(changed)==10
from collections import Counter
assert Counter(x['Status'] for x in ledger['Entries'])=={'Verified compatible':10,'Known differences':24,'Needs verification':553}
add(ledgerpath,encoded(ledger),'candidate2 ledger plus oneguardhash and tenappend-only source evidence refs')
planrecords=[]
for p,workflow,key in [('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256'),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256')]:
 raw=(C/p).read_bytes();v=read(C/p);old=copy.deepcopy(v);n=0
 for row in v['Files']:
  if row['Path'] in paths:
   source=next(x for x in sources if x['Path']==row['Path']);assert row['HashMode']=='TextLF';row['Bytes']=source['TextLFBytes'];row['Sha256']=source['TextLFSha256'];n+=1
 assert n==2
 changes=[a['Path'] for a,b in zip(old['Files'],v['Files']) if a!=b];assert changes==paths
 b=encoded(v);assert b'\r' not in b;newhash=sha(b);oldhash=sha(raw);add(p,b,'exacttwoexistingTextLFsource rows; allotherfields/policies preserved')
 w=(C/workflow).read_bytes();needle=(key+': '+oldhash).encode();assert w.count(needle)==1;updated=w.replace(needle,(key+': '+newhash).encode());assert b'\r' not in updated;add(workflow,updated,'exactone source-plan digest seal replacement')
 planrecords.append(dict(Path=p,OldSha256=oldhash,NewSha256=newhash,Rows=len(v['Files']),ChangedRows=changes,Workflow=workflow,Seal=key))
rows=[]
for target,b in sorted(pending.items()):
 p=O/'overlay'/target;p.parent.mkdir(parents=True,exist_ok=True);assert not p.exists();p.write_bytes(b);old=C/target;rows.append(dict(SourcePath=p.relative_to(R).as_posix(),TargetPath=target,Bytes=len(b),Sha256=sha(b),CandidateParentSha256=sha(old.read_bytes()) if old.exists() else None,MainParentSha256=sha((R/target).read_bytes()) if (R/target).exists() else None,Origin=origins[target]))
proposal=dict(Status='SOURCE_ONLY_CORRECTED351_METADATA_OVERLAY_REQUIRES_ROOT_REVIEW_NO_EXECUTION',ParentPublishedHead=parent,CandidatePath=C.relative_to(R).as_posix(),Files=rows,FilesCount=len(rows),Plans=planrecords,CountsUnchanged=[10,24,553],RuntimeGuards=74,OnlyExistingGuardHashChanged=paths[1],ExistingTenEvidencePrefixScopeLimitsPreserved=True,Other577LedgerEntriesUnchanged=True,FocusStillRunning=True,FullStillPending=True,RegistryUntouched=True,MainChanged=False,CandidateChanged=False,GitChanged=False,SdkExecuted=False,Network=False)
p=O/'proposal.json';assert not p.exists();p.write_bytes(encoded(proposal));(O/'copy-plan.json').write_bytes(encoded(dict(Status=proposal['Status'],Files=rows)))
print(json.dumps(dict(ProposalSha256=sha(p.read_bytes()),CopyPlanSha256=sha((O/'copy-plan.json').read_bytes()),Files=len(rows),Plans=planrecords)))
