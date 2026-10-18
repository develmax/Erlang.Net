"""Passive six-cohort full-field comparison. No runtime, VM, build or network."""
from pathlib import Path
import argparse,copy,gc,hashlib,importlib.util,json,subprocess
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
J=lambda p:json.loads(p.read_bytes())
def load(p,n):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def names(node,expected):
 assert node[0]=='list' and len(node[1])==len(expected);result={}
 for n,row in zip(expected,node[1]):
  assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==n and n not in result
  result[n]=row[1][1]
 return result
def changed_frame(n,change):
 x=list(n[1]);stack=list(x[3][1]);stack[0]=change(stack[0]);x[3]=('list',tuple(stack));return ('tuple',tuple(x))
def changed_args(n,change):return changed_frame(n,lambda f:('tuple',(*f[1][:2],change(f[1][2]),f[1][3])))
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--native',type=Path,required=True);ap.add_argument('--historical-retired',action='store_true');ap.add_argument('--sdk',type=Path,default=R/'artifacts/queue-integration368/capture4/sdk');ap.add_argument('--admission',type=Path,default=R/'artifacts/queue-original368-peer1/actual1/acceptance.json');ap.add_argument('--output',type=Path,required=True);a=ap.parse_args()
 assert not a.output.exists() and a.output.resolve().is_relative_to(O);a.output.mkdir()
 binding=J(O/'bindings.json')
 def admit(b):
  assert b and b['ActualOwnTrapEntrySnapshotsSourceReviewed'] and b['EveryBudgetHeapStateDifferenceExplained'] and not b['NativePartitionSeededIntoMain']
  assert b['Files']==binding['Files']
  for row in b['Files']:
   q=R/row['Path'];assert q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
  return b
 admit(binding)
 original=J(a.admission);assert original['Status']=='PASS_PASSIVE_QUEUE368_ORIGINAL_ADMISSION' and original['Values']==893 and original['FullStackProfiles']==4465
 assert original['Head']=='29be5d7c68c5ad04784a57ee399cc99d14a655a4' and original['Run']==39078144460
 tool=R/'artifacts/queue-original368-original1/tool';profile=J(tool/'profile.json');assert profile['PredeclaredMemberEntryPolicy']['EligibleCases']==['member_miss_256017','member_front_miss_256017']
 assert H(tool/'passive_etf_family.py')=='2A7CE62661669FA91D03A0231D0911FEDACAB6C37B0D2FBE76F2DA325726A519'
 D=load(tool/'passive_etf_family.py','trusted_queue368_decoder');P=load(O/'relation.py','predeclared_queue368_relation')
 comp=J(a.native/'completion.json');sdk=J(a.sdk/'completion.json');seal=J(a.sdk/'capture-seal.json')
 assert comp['SubjectHead']==original['Head'] and comp['Status']=='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' and comp['FailedGroups']==sdk['FailedGroups']==0
 assert comp['ProfileSha256']==H(tool/'profile.json') and comp['SourceInputsSha256']==H(tool/'source-inputs.corrected.json')
 for row in seal['Files']:
  q=(a.sdk/row['Path']).resolve();assert q.is_relative_to(a.sdk.resolve()) and q.stat().st_size==row['Bytes'] and H(q)==row['Sha256']
 closure=J(a.sdk.parent/'main-dll-closure.json');assert closure['MainSource'] and not closure['PrivateCandidateDllsUsed'] and len(closure['Files'])==5
 live_deltas=[]
 for row in closure['Files']:
  current=H(R/row['Path']);copied=H(R/row['CopiedTo'])
  if current!=row['Sha256'] or copied!=row['Sha256']:
   assert a.historical_retired and row['Path']=='src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll'
   live_deltas.append({'Path':row['Path'],'CapturedSha256':row['Sha256'],'CurrentSha256':current,'CurrentCopiedSha256':copied})
 receipt=J(a.sdk.parent/'new-capture.exit.json');assert receipt['Settled'] and not receipt['TimedOut'] and receipt['ExitCode']==0 and not receipt['Failures']
 for stream in ['Stdout','Stderr']:assert H(a.sdk.parent/('new-capture.'+stream.lower()+'.log'))==receipt[stream+'Sha256']
 ledger=J(R/'artifacts/queue-higher368-source-review4/guarded-files-proposal.json')
 source_deltas=[{'Path':row['Path'],'ReviewedSha256':row['Sha256'],'CurrentSha256':H(R/row['Path'])} for row in ledger['VerifiedSourceFiles'] if H(R/row['Path'])!=row['Sha256']]
 assert not source_deltas and len(ledger['VerifiedSourceFiles'])==382
 proof=J(R/'artifacts/queue-integration367-source-review2/source-protocol-proof.json')
 for row in proof['Authority']:
  blob=subprocess.check_output(['git','-C',str(R.parent/'otp'),'show','ad05823719d77c8faee87348ea39513d4e2f99c5:'+row['Path']]);assert hashlib.sha256(blob).hexdigest().upper()==row['Sha256']
 focus=J(R/'artifacts/queue-integration368/focus2/report.json');assert next(x for x in focus['Tests'] if x['Name']=='runtime/queue/member/own-entry-and-short-circuit')['Status']=='Passed'
 assert focus['Passed']==110 and focus['Failed']==1
 assert J(R/'artifacts/queue-integration368/compiled-fix1/report.json')['Passed']==1
 assert J(R/'artifacts/queue-integration368/cooperative1/report.json')['Passed']==1
 selected={'queue_ends_left','queue_ends_right','long_reverse_ends','long_member','long_split','long_last'}
 allowed={'out_r_front256017':'reverse','drop_r_front256017':'reverse','member_miss_256017':'member','member_front_miss_256017':'member'}
 results=[];unexpected=[];seeds={};evidence=[]
 for fixture in profile['Fixtures']:
  group=fixture['Id']
  if group not in selected:continue
  ad=a.native/group;bd=a.sdk/group;wanted=[('atom',n) for n in fixture['Names']]
  c=next(x for x in comp['Groups'] if x['Id']==group);sc=next(x for x in sdk['Groups'] if x['Id']==group)
  assert c['Acquired'] and not c['VmSkipped'] and not sc['ExecutionSkipped']
  for src,cr in zip([fixture['Path']]+fixture['Companions'],sc['Compile']):
   assert cr['Passed'] and cr['SourceSha256']==H(tool/src) and Path(cr['Path']).resolve()==(tool/src).resolve()
  for filename,nkey,skey in [('returned.etf','ReturnedSha256','ReturnedSha256'),('stack-observations.etf','RawStacksSha256','StacksSha256')]:
   assert H(ad/filename)==c[nkey] and H(bd/filename)==sc[skey]
   evidence.append({'Group':group,'File':filename,'OriginalPath':str(ad/filename),'OriginalSha256':H(ad/filename),'SdkPath':str(bd/filename),'SdkSha256':H(bd/filename)})
   left=D.decode_exact((ad/filename).read_bytes());right=D.decode_exact((bd/filename).read_bytes())
   if filename=='returned.etf':
    ll=names(left,wanted);rr=names(right,wanted);pairs=[('values',n,ll[n],rr[n]) for n in wanted]
   else:
    depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
    ll=names(left,depths);rr=names(right,depths);pairs=[]
    for depth in depths:
     x=names(ll[depth],wanted);y=names(rr[depth],wanted);pairs.extend((repr(depth),n,x[n],y[n]) for n in wanted)
   for depth,n,x,y in pairs:
    strict=x==y;row={'Group':group,'Case':n[1],'Lane':depth,'StrictWholeEqual':strict,'Accepted':strict}
    if depth!='values' and n[1] in allowed:
     function=allowed[n[1]]
     try:row['EntryRelationship']=P.compare(x,y,256017,function,admit(binding));row['Accepted']=True
     except (AssertionError,IndexError,KeyError,TypeError) as error:row['Accepted']=False;row['Failure']=type(error).__name__
     if function not in seeds:seeds[function]=(x,y)
    if not strict:row['RawFilesEvidenceIndex']=len(evidence)-1
    if not row['Accepted']:unexpected.append(row)
    results.append(row)
   del left,right,ll,rr,pairs;gc.collect()
  print(group,'processed; unexpected',len(unexpected),flush=True)
 assert len(results)==2574 and sum(x['Lane']=='values' for x in results)==429
 controls=[]
 for function,(x,y) in seeds.items():
  def reject(label,z,b=binding):
   try:ok=P.compare(x,z,256017,function,admit(b))['Accepted']
   except (AssertionError,IndexError,KeyError,TypeError):ok=False
   controls.append({'Function':function,'Name':label,'Rejected':not ok})
  reject('class',('tuple',(y[1][0],('atom','throw'),*y[1][2:])))
  reject('reason',('tuple',(*y[1][:2],('atom','different'),y[1][3])))
  reject('frame_module',changed_frame(y,lambda f:('tuple',(('atom','erlang'),*f[1][1:]))))
  reject('frame_name',changed_frame(y,lambda f:('tuple',(f[1][0],('atom','different'),*f[1][2:]))))
  reject('frame_metadata',changed_frame(y,lambda f:('tuple',(*f[1][:3],('list',(('atom','different'),))))))
  reject('caller_appended',('tuple',(*y[1][:3],('list',y[1][3][1]+y[1][3][1][:1]))))
  reject('argument_count',changed_args(y,lambda z:('list',z[1]+(('atom','extra'),))))
  def suffix(z,change):
   args=list(z[1]);i=0 if function=='reverse' else 1;args[i]=change(args[i]);return ('list',tuple(args))
  reject('terminal',changed_args(y,lambda z:suffix(z,lambda s:('improper',s[1],('atom','other')))))
  reject('integer',changed_args(y,lambda z:suffix(z,lambda s:('improper',(('integer',-999),)+s[1][1:],s[2]))))
  reject('suffix_missing_cell',changed_args(y,lambda z:suffix(z,lambda s:('improper',s[1][1:],s[2]))))
  if function=='member':reject('item',changed_args(y,lambda z:('list',(('atom','changed'),z[1][1]))))
  else:reject('accumulator',changed_args(y,lambda z:('list',(z[1][0],('list',(('integer',-999),)+z[1][1][1])))))
  reject('binding_missing',y,None)
  for field,value in [('ActualOwnTrapEntrySnapshotsSourceReviewed',False),('NativePartitionSeededIntoMain',True),('EveryBudgetHeapStateDifferenceExplained',False)]:
   b=dict(binding);b[field]=value;reject(field,y,b)
  b=copy.deepcopy(binding);b['Files'][0]['Sha256']='0'*64;reject('source_or_dll_binding_tampered',y,b)
 assert len(seeds)==2 and all(x['Rejected'] for x in controls)
 report={'Status':'PASS_2574_WHOLE_OR_SOURCE_BOUND' if not unexpected else 'FAIL_UNEXPECTED_WHOLE_FIELDS','Values':429,'FullStackProfiles':2145,'Observations':2574,'StrictWholeEqual':sum(x['StrictWholeEqual'] for x in results),'Accepted':sum(x['Accepted'] for x in results),'RawDifferences':[x for x in results if not x['StrictWholeEqual']],'Unexpected':unexpected,'Rows':results,'Evidence':evidence,'NegativeControls':controls,'OriginalAdmissionSha256':H(a.admission),'SourceBindingsSha256':H(O/'bindings.json'),'RelationPrimitiveSha256':H(O/'relation.py'),'FinalDllClosureSha256':H(a.sdk.parent/'main-dll-closure.json'),'ReviewedSourceGuards':len(ledger['VerifiedSourceFiles']),'CurrentSourceDeltasOutsideSelectedBindings':source_deltas,'CheckpointOnlyFinalSourceAcceptancePending':bool(source_deltas or live_deltas),'HistoricalLiveDllRetirement':live_deltas,'LiveDllVerificationPassedInFirstAttemptBeforeRetirement':bool(a.historical_retired),'NativePartitionSeeded':False,'Projection':False,'FramesDropped':False,'CompatibilityPromoted':False,'SdkVmOrNativeExecuted':False}
 (a.output/'comparison.json').write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps({k:report[k] for k in ['Status','Observations','StrictWholeEqual','Accepted']}));print('unexpected',[(x['Case'],x['Lane']) for x in unexpected]);print('report',H(a.output/'comparison.json'))
 if unexpected:raise SystemExit(1)
if __name__=='__main__':main()
