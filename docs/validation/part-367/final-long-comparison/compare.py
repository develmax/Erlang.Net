from pathlib import Path
import hashlib,json,importlib.util,gc,copy,subprocess
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
T=R/'artifacts/queue-original367-original2/tool';A=T.parent/'download/run-39072770120/artifact-12116022082/capture';B=R/'artifacts/queue-integration367/capture3/long-sdk'
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def load(p,n):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
D=load(T/'passive_etf_family.py','actual_long_decoder');P=load(R/'artifacts/queue367-longreverse-policy2/reverse_relation.py','actual_predeclared_policy')
def named(node,names):
 assert node[0]=='list' and len(node[1])==len(names);rows={}
 for n,row in zip(names,node[1]):assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==n and n not in rows;rows[n]=row[1][1]
 return rows
profile=J(T/'profile.json');policy=J(R/'artifacts/queue367-longreverse-policy2/policy.json');assert H(T/'profile.json')==policy['ObservedProposalProfile']['Sha256']
comp=J(A/'completion.json');assert comp['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and not comp['FailedGroups'] and comp['AllGroupsAttempted']
assert comp['ProfileSha256']==H(T/'profile.json') and comp['SourceInputsSha256']==H(T/'source-inputs.corrected.json') and comp['PinnedOtpCommit']==policy['Pin']
review=R/'artifacts/queue-integration367-source-review2';ledger=J(review/'proposed-mfa-compatibility.json');assert len(ledger['VerifiedSourceFiles'])==357
for x in ledger['VerifiedSourceFiles']:assert H(R/x['Path'])==x['Sha256']
for x in J(R/'artifacts/queue-integration367-host-review3/source-bindings.json'):assert H(R/x['Path'])==x['Sha256']
source_proof=J(review/'source-protocol-proof.json');assert source_proof['PinnedOtp']==policy['Pin']
for x in source_proof['Authority']:
 data=subprocess.check_output(['git','-C',str(R.parent/'otp'),'show',policy['Pin']+':'+x['Path']]);assert hashlib.sha256(data).hexdigest().upper()==x['Sha256']
sdk_admission=J(R/'artifacts/queue367-final-long-sdk-peer1/admission.json');assert sdk_admission['Status']=='PASS_FINAL_LONG_SDK_CAPTURE_ADMISSION_ORIGINAL2_PENDING' and sdk_admission['Production357GuardsCurrent']
review_binding=dict(ActualOwnTrapEntrySnapshotsSourceReviewed=True,NativePartitionSeededIntoMain=False,EveryBudgetHeapStateDifferenceExplained=True,SourceProofSha256=H(review/'source-protocol-proof.json'),HostBudget1ReviewSha256=H(R/'artifacts/queue-integration367-host-review3/review.json'),FinalDllClosureSha256=sdk_admission['MainDllClosureSha256'])
def admit_protocol(binding):
 assert binding is not None
 assert binding['ActualOwnTrapEntrySnapshotsSourceReviewed'] is True
 assert binding['NativePartitionSeededIntoMain'] is False
 assert binding['EveryBudgetHeapStateDifferenceExplained'] is True
 assert binding['SourceProofSha256']==sdk_admission['OwnSourceProtocolReviewSha256']==H(review/'source-protocol-proof.json')
 assert binding['HostBudget1ReviewSha256']==sdk_admission['FinalHostReviewSha256']==H(R/'artifacts/queue-integration367-host-review3/review.json')
 closure_path=R/'artifacts/queue-integration367/capture3/main-dll-closure.json'
 assert binding['FinalDllClosureSha256']==sdk_admission['MainDllClosureSha256']==H(closure_path)
 for row in J(closure_path)['Files']:assert H(R/row['Path'])==H(R/row['CopiedTo'])==row['Sha256']
 return binding
admit_protocol(review_binding)
rows=[];native=[];unexpected=[];seed=None;total_values=total_profiles=0
for fixture in profile['Fixtures']:
 group=fixture['Id'];ad=A/group;bd=B/group;names=[('atom',x) for x in fixture['Names']];c=next(x for x in comp['Groups'] if x['Id']==group);assert c['Acquired'] and not c['VmSkipped']
 for filename,key in [('returned.etf','ReturnedSha256'),('stack-observations.etf','RawStacksSha256')]:assert H(ad/filename)==c[key]
 for filename in ['returned.etf','stack-observations.etf']:
  left=D.decode_exact((ad/filename).read_bytes());right=D.decode_exact((bd/filename).read_bytes())
  if filename=='returned.etf':
   ll=named(left,names);rr=named(right,names);pairs=[('values',n,ll[n],rr[n]) for n in names]
  else:
   depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)];ll=named(left,depths);rr=named(right,depths);pairs=[]
   for dep in depths:
    al=named(ll[dep],names);bl=named(rr[dep],names);pairs.extend((repr(dep),n,al[n],bl[n]) for n in names)
  for depth,name,a,b in pairs:
   n=name[1]
   if n in policy['OriginalOnlyDiagnostics']:
    item=dict(Group=group,File=filename,Depth=depth,Case=n,OriginalOnlyDiagnostic=True)
    if filename=='stack-observations.etf':
     count=3 if n=='short_reverse_direct' else int(n.rsplit('_',1)[1]);item['NativeSnapshot']=P.snapshot(P.frame(a)[1][2],count)
    native.append(item);continue
   exact=a==b;item=dict(Group=group,File=filename,Depth=depth,Case=n,StrictWholeDecodedEqual=exact)
   if filename=='returned.etf':total_values+=1
   else:total_profiles+=1
   if n in policy['QueueReverseRelationCases'] and filename=='stack-observations.etf':
    count=policy['QueueReverseRelationCases'][n]
    try:item['SourceBound']=P.compare_reverse(a,b,count,admit_protocol(review_binding));item['Accepted']=item['SourceBound']['Accepted']
    except (AssertionError,IndexError,KeyError,TypeError) as e:item['Accepted']=False;item['ErrorType']=type(e).__name__
    if seed is None and not exact:seed=(a,b,count)
   else:item['Accepted']=exact
   if not exact:item['WholeRawDifferenceRetained']=dict(OriginalFile=(ad/filename).relative_to(R).as_posix(),OriginalSha256=H(ad/filename),SdkFile=(bd/filename).relative_to(R).as_posix(),SdkSha256=H(bd/filename),FirstAllowedField='$.error.stack[0].arguments' if n in policy['QueueReverseRelationCases'] else None)
   if not item['Accepted']:unexpected.append(item)
   rows.append(item)
  del left,right,ll,rr,pairs;gc.collect()
 print(group,'processed;unexpected',len(unexpected),flush=True)
assert total_values==8 and total_profiles==40 and len(rows)==48 and len(native)==18
assert any(x.get('NativeSnapshot',{}).get('Consumed',0)>0 for x in native)
# Corruption controls operate on one actual observed complete error, never a synthetic OTP result.
a,b,count=seed
controls=[]
def replace_first_frame(n,change):
 body=list(n[1]);stack=list(body[3][1]);stack[0]=change(stack[0]);body[3]=('list',tuple(stack));return ('tuple',tuple(body))
def args_change(n,change):return replace_first_frame(n,lambda f:('tuple',(*f[1][:2],change(f[1][2]),f[1][3])))
def reject(label,mutated):
 try:ok=P.compare_reverse(a,mutated,count,admit_protocol(review_binding))['Accepted']
 except (AssertionError,IndexError,KeyError,TypeError):ok=False
 controls.append(dict(Name=label,Rejected=not ok))
reject('frame_module',replace_first_frame(b,lambda f:('tuple',(('atom','erlang'),*f[1][1:]))))
reject('error_class',('tuple',(b[1][0],('atom','throw'),*b[1][2:])))
reject('error_reason',('tuple',(*b[1][:2],('atom','different'),b[1][3])))
reject('frame_metadata',replace_first_frame(b,lambda f:('tuple',(*f[1][:3],('list',())))))
# If native metadata empty, a nonempty alteration is always different.
if not controls[-1]['Rejected']:
 controls.pop();reject('frame_metadata',replace_first_frame(b,lambda f:('tuple',(*f[1][:3],('list',(('atom','invalid'),))))))
def change_terminal(args):
 remaining,accumulator=args[1]
 return ('list',(('improper',remaining[1],('atom','different_tail')),accumulator))
def change_suffix(args):
 remaining,accumulator=args[1]
 changed=(('integer',999999),)+remaining[1][1:]
 return ('list',(('improper',changed,remaining[2]),accumulator))
def change_accumulator(args):
 remaining,accumulator=args[1]
 return ('list',(remaining,('list',(('integer',999999),)+accumulator[1])))
reject('saved_terminal',args_change(b,change_terminal))
reject('suffix_value',args_change(b,change_suffix))
reject('accumulator_value',args_change(b,change_accumulator))
reject('argument_count',args_change(b,lambda args:('list',args[1]+(('list',()),))))
# Preserve complete callers: modify rather than merely print an argument summary.
stack=b[1][3][1]
reject('caller_removed',('tuple',(*b[1][:3],('list',stack[:1]))))
if not controls[-1]['Rejected']:
 controls.pop();reject('caller_added',('tuple',(*b[1][:3],('list',stack+stack[:1]))))
# Decomposition without an admitted source protocol cannot accept a differing split.
empty_review=P.compare_reverse(a,b,count,None)
if not empty_review['StrictArgumentsEqual']:controls.append(dict(Name='missing_own_protocol_review',Rejected=not empty_review['Accepted']))
def admit_reject(label,binding):
 try:
  admitted=admit_protocol(binding)
  okay=P.compare_reverse(a,b,count,admitted)['Accepted']
 except (AssertionError,IndexError,KeyError,TypeError):okay=False
 controls.append(dict(Name=label,Rejected=not okay))
admit_reject('source_binding_missing',None)
for label,field,value in [
 ('source_binding_false','ActualOwnTrapEntrySnapshotsSourceReviewed',False),
 ('source_budget_explanation_false','EveryBudgetHeapStateDifferenceExplained',False),
 ('native_partition_injection','NativePartitionSeededIntoMain',True),
 ('tampered_source_proof_hash','SourceProofSha256','0'*64),
 ('tampered_final_dll_binding','FinalDllClosureSha256','0'*64),
 ('tampered_budget_host_binding','HostBudget1ReviewSha256','0'*64)]:
 altered=dict(review_binding);altered[field]=value;admit_reject(label,altered)
stack=b[1][3][1]
reject('caller_appended',('tuple',(*b[1][:3],('list',stack+stack[:1]))))
# A missing accumulator value is distinct from an altered scalar.
args=P.frame(b)[1][2]
if args[1][1][1]:
 def drop_accumulator(args):return ('list',(args[1][0],('list',args[1][1][1][1:])))
 reject('accumulator_cell_missing',args_change(b,drop_accumulator))
def reverse_suffix(args):return ('list',(('improper',tuple(reversed(args[1][0][1])),args[1][0][2]),args[1][1]))
reject('suffix_order_reversed',args_change(b,reverse_suffix))
assert all(x['Rejected'] for x in controls)
status='PASS_LONG_PAIRED8_40_SOURCE_BOUND_WHOLE_FIELDS' if not unexpected else 'FAIL_NONALLOWED_WHOLE_FIELDS'
report=dict(Status=status,PairedValues=total_values,PairedFullObservations=total_profiles,StrictWholeEqual=sum(x['StrictWholeDecodedEqual'] for x in rows),Accepted=sum(x['Accepted'] for x in rows),OriginalOnlyValues=3,OriginalOnlyProfiles=15,NativeOnlyDiagnostics=native,Rows=rows,Unexpected=unexpected,NegativeControls=controls,PolicySha256=H(R/'artifacts/queue367-longreverse-policy2/policy.json'),PolicyPrimitiveSha256=H(R/'artifacts/queue367-longreverse-policy2/reverse_relation.py'),OwnSourceReview=review_binding,SdkAdmissionSha256=H(R/'artifacts/queue367-final-long-sdk-peer1/admission.json'),OriginalCompletionSha256=H(A/'completion.json'),NativeRun=39072770120,OriginalHead=comp['SubjectHead'],NoNormalization=True,NoProjection=True,NoFramesStripped=True,NativePartitionSeededIntoMain=False,CompatibilityPromoted=False,MainModified=False,OriginalRuntimeOrSdkExecutedByReview=False)
(O/'comparison.json').write_text(json.dumps(report,indent=2)+'\n');print(status,'strict',report['StrictWholeEqual'],'accepted',report['Accepted'],'controls',len(controls));print('UNEXPECTED',[(x['Case'],x['Depth']) for x in unexpected])
