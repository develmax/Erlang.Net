"""Passive data-only successor; never executes a VM, SDK subject or downloaded code."""
from pathlib import Path
from dataclasses import replace
from collections import Counter
import argparse,ast,copy,gc,hashlib,importlib.util,json,sys
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
def H(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def J(p):return json.loads(Path(p).read_bytes())
def load(p,n):
 s=importlib.util.spec_from_file_location(n,p);m=importlib.util.module_from_spec(s);sys.modules[n]=m;s.loader.exec_module(m);return m
def save(p,x):p.write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8',newline='\n')
def named(n):
 assert n[0]=='list';d={}
 for x in n[1]:assert x[0]=='tuple' and len(x[1])==2 and x[1][0] not in d;d[x[1][0]]=x[1][1]
 return d
def mutate(n,predicate,change):
 done=False
 def walk(x):
  nonlocal done
  if not done and isinstance(x,tuple) and predicate(x):done=True;return change(x)
  if isinstance(x,tuple):return tuple(walk(y) for y in x)
  if isinstance(x,frozenset):return frozenset(walk(y) for y in x)
  return x
 result=walk(n);assert done;return result

def main():
 ap=argparse.ArgumentParser();ap.add_argument('--capture',type=Path,required=True);ap.add_argument('--output',type=Path,required=True);a=ap.parse_args()
 assert not a.output.exists() and a.output.resolve().is_relative_to(O);a.output.mkdir(parents=True)
 manifest=J(R/'artifacts/binary369-retained-replay-proposal1/inputs.json')
 settlement=J(a.capture/'completion.json');assert settlement['Status']=='CAPTURED_PENDING_PASSIVE_COMPARISON' and settlement['MainDllClosureUnchanged']
 assert len(settlement['Stages'])==len(manifest['Stages'])==9 and all(x['Passed'] for x in settlement['Stages'])
 D=load(R/'artifacts/queue-original368-original1/tool/passive_etf_family.py','retained_decoder8m')
 V=load(R/'artifacts/proplists-stack-relational-peer1/validate.py','retained_proplist_relation')
 Q=load(R/'artifacts/queue-higher368-comparison-peer1/compare.py','retained_queue_named')
 bind=load(R/'artifacts/proplists-stack-relational-peer3/source_binding.py','retained_named_binding')
 md=load(R/'artifacts/proplists22-original-peer1/audit.py','retained_beam_md5')
 E=load(R/'artifacts/queue-ends368-comparison-peer3/relation.py','retained_saved_entry')
 L=load(R/'artifacts/queue367-longreverse-policy2/reverse_relation.py','retained_long_saved_entry')
 # Reuse the exact trusted queue-core comparison class, excluding its old top-level IO.
 oldcore=R/'artifacts/queue367-original-comparison-peer1/compare.py';tree=ast.parse(oldcore.read_text(encoding='utf-8-sig'))
 cls=next(x for x in tree.body if isinstance(x,ast.ClassDef) and x.name=='Compare')
 coreglobals={'hashlib':hashlib,'bind':bind}
 exec(compile(ast.Module(body=[cls],type_ignores=[]),str(oldcore),'exec'),coreglobals)
 Core=coreglobals['Compare']
 oldproof=J(R/'artifacts/queue-ends368-comparison-peer3/bindings.json')
 proof=J(O/'source-binding.json')
 assert H(O/'source-binding.json')=='B8F4E214B2F8C06D08F4C9FBDBDBA217CC0584799CD1B88F427BC3885511E3B2'
 assert proof['GuardCount']==496 and H(R/proof['GuardReviewPath'])==proof['GuardReviewSha256'] and H(R/proof['BindingsPath'])==proof['BindingsSha256']
 guards=J(R/proof['BindingsPath']);assert len(guards)==496 and len({x['Path'] for x in guards})==496
 for row in guards:assert H(R/row['Path'])==row['Sha256'],row['Path']
 actual=[]
 for row in oldproof['Files']:
  if row['Path'].endswith('.dll'):continue
  now=H(R/row['Path'])
  if now!=row['Sha256']:actual.append({'Path':row['Path'],'Before':row['Sha256'],'After':now})
 assert actual==[{k:x[k] for k in ['Path','Before','After']} for x in proof['ExactOldBindingChanges']]
 source_proof=proof['ExactOldBindingChanges']
 binding={'ActualOwnTrapEntrySnapshotsSourceReviewed':True,'EveryBudgetHeapStateDifferenceExplained':True,'NativePartitionSeededIntoMain':False,'SourceReviewSha256':proof['GuardReviewSha256']}
 binding['Files']=[{'Path':r['Path'],'Sha256':H(R/r['Path'])} for r in oldproof['Files'] if not r['Path'].endswith('.dll')]
 binding['Files'].extend({'Path':r['CopiedTo'],'Sha256':r['Sha256']} for r in J(a.capture/'queue-all/main-dll-closure.json')['Files'])
 def admit(b):
  assert b is not None and b['SourceReviewSha256']==binding['SourceReviewSha256'] and b['Files']==binding['Files']
  assert b['ActualOwnTrapEntrySnapshotsSourceReviewed'] and b['EveryBudgetHeapStateDifferenceExplained'] and not b['NativePartitionSeededIntoMain']
  for r in b['Files']:assert H(R/r['Path'])==r['Sha256']
  return b
 # Actual core reverse/member protocol source files must retain their old reviewed bytes.
 for row in oldproof['Files']:
  if row['Path'].startswith('src/Erlang.Runtime/') and not row['Path'].endswith('.dll'):assert H(R/row['Path'])==row['Sha256'],row['Path']
 results=[];controls=[];seeds={};stage_reports=[];evidence=[];retirements=[]
 expected_depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
 ownallowed={'out_r_front256017':'reverse','drop_r_front256017':'reverse','member_miss_256017':'member','member_front_miss_256017':'member'}
 longpolicy=J(R/'artifacts/queue367-longreverse-policy2/policy.json')
 for st in manifest['Stages']:
  stage=st['Name'];base=a.capture/stage;sdk=base/'sdk';original=R/st['Native'];profile=J(base/'tool/profile.json')
  assert H(base/'tool/profile.json')==st['ProfileSha256']
  for row in st['NativeFiles']:assert H(R/row['Path'])==row['Sha256']
  closure=J(base/'main-dll-closure.json');assert closure['MainSource'] and not closure['PrivateCandidateDllsUsed'] and len(closure['Files'])==5
  for row in closure['Files']:
   assert H(R/row['CopiedTo'])==row['Sha256']
   live=H(R/row['Path'])
   assert live==row['Sha256'],('fresh final current DLL required',row['Path'])
  receipt=J(base/'capture.exit.json');assert receipt['Settled'] and not receipt['TimedOut'] and receipt['ExitCode']==0 and not receipt['Failures']
  for stream in ['Stdout','Stderr']:assert H(base/('capture.'+stream.lower()+'.log'))==receipt[stream+'Sha256']
  V.admit_sdk(sdk)
  cc=J(sdk/'completion.json');ng=J(original/'completion.json');assert ng['SubjectHead']==st['NativeHead'] and ng['FailedGroups']==cc['FailedGroups']==0
  count0=len(results)
  for f in profile['Fixtures']:
   group=f['Id'];ad=original/group;bd=sdk/group;wanted=[('atom',x) for x in f['Names']]
   actual=next(x for x in cc['Groups'] if x['Id']==group);assert len(actual['Compile'])==1+len(f['Companions']) and all(x['Passed'] for x in actual['Compile'])
   smd=named(D.decode_exact((bd/'source-module-metadata.etf').read_bytes()));runtime=D.decode_exact((bd/'runtime-metadata.etf').read_bytes());init=runtime[1][2]
   bindings=J(bd/'source-function-bindings.json')['Bindings']
   meta=D.decode_exact((ad/'metadata.etf').read_bytes());amd=meta[1][5][1];bmd=runtime[1][1][1]
   beams={}
   for p in (ad/'ebin').glob('*.beam'):beams[p.stem]=Q.funt(p,md)
   # Per-runtime provenance anchor for original proplists fun_info; no PID guess.
   original_values=named(D.decode_exact((ad/'returned.etf').read_bytes()))
   original_pid=None
   anchor=original_values.get(('atom','raw_fun_info_all'))
   if anchor:original_pid=V.info_fields(anchor[1][1])[('atom','pid')]
   for filename in ['returned.etf','stack-observations.etf']:
    pa=ad/filename;pb=bd/filename;ra=pa.read_bytes();rb=pb.read_bytes();left=D.decode_exact(ra);right=D.decode_exact(rb)
    evidence.append({'Stage':stage,'Group':group,'File':filename,'OriginalSha256':H(pa),'SdkSha256':H(pb),'OriginalPath':str(pa),'SdkPath':str(pb)})
    if filename=='returned.etf':
     ll=named(left);rr=named(right);assert list(ll)==list(rr)==wanted;pairs=[('values',n,ll[n],rr[n]) for n in wanted]
    elif f.get('StackShape')=='old59_twelve':
     assert left[0]==right[0]=='list' and len(left[1])==len(right[1])==12;pairs=[('old59',str(i),x,y) for i,(x,y) in enumerate(zip(left[1],right[1]))]
    else:
     ll=named(left);rr=named(right);assert list(ll)==list(rr)==expected_depths;pairs=[]
     for dep in expected_depths:
      x=named(ll[dep]);y=named(rr[dep]);assert list(x)==list(y)==wanted;pairs.extend((repr(dep),n,x[n],y[n]) for n in wanted)
    for depth,name,x,y in pairs:
     nm=name[1] if isinstance(name,tuple) else name;strict=x==y;accepted=strict;relations=[];errors=[];kind='STRICT'
     if stage=='queue-all' and nm in ownallowed and depth!='values':
      fun=ownallowed[nm]
      try:relations=[E.compare(x,y,256017,fun,admit(binding))];accepted=True;kind='ACTUAL_SAVED_ENTRY'
      except (AssertionError,IndexError,KeyError,TypeError):accepted=False;errors=['saved-entry wholefield/source predicate rejected']
      seeds.setdefault('entry_'+fun,(x,y,fun))
     elif stage=='queue-long' and nm in longpolicy['OriginalOnlyDiagnostics']:
      kind='NATIVE_ONLY_DIAGNOSTIC';accepted=False
     elif stage=='queue-long' and nm in longpolicy['QueueReverseRelationCases'] and depth!='values':
      try:
       z=L.compare_reverse(x,y,longpolicy['QueueReverseRelationCases'][nm],admit(binding));relations=[z];accepted=z['Accepted'];kind='ACTUAL_SAVED_ENTRY'
      except (AssertionError,IndexError,KeyError,TypeError):accepted=False;errors=['longreverse wholefield/source predicate rejected']
     elif stage=='queue-core' and nm=='opaque_raw':
      amdcore,funt=beams['queue_core_contract'];coreglobals.update(md5=amdcore,funt=funt,bindings=bindings,smd=smd[('atom','queue_core_contract')][1],init=init)
      c=Core(x,y,ra,rb);accepted=c.run(x,y);relations=c.relations;errors=c.errors;kind='ACTUAL_CORE_IDENTITY'
      seeds.setdefault('core',(x,y,ra,rb,dict(coreglobals)))
     elif stage=='queue-all' and group=='queue_callbacks':
      c=Q.Comparison(True,beams,bindings,smd,init,bind,ra,rb);accepted=c.run(x,y);relations=c.relations;errors=c.errors;kind='ACTUAL_NAMED_CODE'
      if relations:seeds.setdefault('named',(x,y,c))
     elif stage.startswith('proplist'):
      ac=V.Context(amd,original_pid,ra,str(pa),nm=='raw_wire',nm=='raw_fun_info_all');bc=V.Context(bmd,init,rb,str(pb),nm=='raw_wire',nm=='raw_fun_info_all')
      if stage=='proplist-extra' and nm=='apply_badarity':
       c=Q.Comparison(True,beams,bindings,smd,init,bind,ra,rb);accepted=c.run(x,y);relations=c.relations;errors=c.errors;kind='ACTUAL_NAMED_CODE'
       if relations:seeds.setdefault('named_extra',(x,y,c))
      else:
       c=V.Compare();accepted=c.run(x,y,ac,bc);relations=c.relations;errors=c.errors;kind='ACTUAL_PROPLIST_CODE' if relations else 'STRICT'
       if relations:seeds.setdefault('proplist',(x,y,ac,bc))
       if nm=='raw_wire':seeds.setdefault('wire',(x,y,ac,bc))
       if nm=='raw_fun_info_all':seeds.setdefault('info',(x,y,ac,bc))
      if group=='family148' and nm=='case_083' and not accepted:
       assert x[0]==y[0]=='tuple' and x[1][0]==y[1][0]==('atom','ok') and len(x[1])==len(y[1])==2 and x[1][1][0]==y[1][1][0]=='list'
       accepted=Counter(x[1][1][1])==Counter(y[1][1][1]);kind='SOURCE_UNSPECIFIED_GET_KEYS_ORDER';relations=[{'EveryExactKeyMultiplicityChecked':accepted,'NoFrameOrErrorRelation':True}]
      if stage=='proplist-raw' and nm in {'hamt33_tuple_keys','hamt33_atom_keys'} and not accepted:kind='OWN_HAMT_TOPOLOGY_SOURCE_REVIEW_REQUIRED'
     row={'Stage':stage,'Group':group,'File':filename,'Depth':depth,'Case':nm,'StrictWholeEqual':strict,'AcceptedWholeFields':accepted,'Kind':kind,'Relations':relations,'Errors':errors}
     results.append(row)
    del left,right,pairs;gc.collect()
   print(stage,group,'reviewed',flush=True)
  stage_reports.append({'Stage':stage,'Observations':len(results)-count0,'ReceiptSha256':H(base/'capture.exit.json'),'ClosureSha256':H(base/'main-dll-closure.json')})
  save(a.output/'progress.json',{'Completed':stage_reports,'Rows':results,'Acceptance':False})
 # Negative controls are executed on genuine seeds and preserve source/state admission.
 for label in ['named','named_extra']:
  if label not in seeds:continue
  x,y,c=seeds[label]
  variants=[('uniq',lambda f:f[:3]+(bytes(16),)+f[4:]),('arity',lambda f:f[:2]+(f[2]+1,)+f[3:]),('index',lambda f:f[:4]+(f[4]+9999,)+f[5:]),('oldindex',lambda f:f[:6]+(('integer',9999),)+f[7:]),('oldunique',lambda f:f[:7]+(('integer',9999),)+f[8:]),('env',lambda f:f[:9]+((('integer',9999),),)+f[10:]),('pid',lambda f:f[:8]+(('pid',init[1],9999,*init[3:]),)+f[9:])]
  for name,fn in variants:
   bad=mutate(y,lambda n:bool(n) and n[0]=='new_fun',fn);z=Q.Comparison(True,c.beams,c.bindings,c.smd,c.init,c.bind,c.rawa,c.rawb);controls.append({'Seed':label,'Name':name,'Rejected':not z.run(x,bad)})
  for name,bindings2,smd2 in [('missing_resolver',[dict(r,ResolverBound=False) for r in c.bindings],c.smd),('missing_actual_md5',c.bindings,{})]:
   z=Q.Comparison(True,c.beams,bindings2,smd2,c.init,c.bind,c.rawa,c.rawb);controls.append({'Seed':label,'Name':name,'Rejected':not z.run(x,y)})
 for label,(x,y,fun) in [(k,v) for k,v in seeds.items() if k.startswith('entry_')]:
  def reject(name,bad,b=binding):
   try:okay=E.compare(x,bad,256017,fun,admit(b))['Accepted']
   except (AssertionError,IndexError,KeyError,TypeError):okay=False
   controls.append({'Seed':label,'Name':name,'Rejected':not okay})
  def framechange(fn):
   fields=list(y[1]);stack=list(fields[3][1]);stack[0]=fn(stack[0]);fields[3]=('list',tuple(stack));return ('tuple',tuple(fields))
  def argschange(fn):return framechange(lambda f:('tuple',(*f[1][:2],fn(f[1][2]),f[1][3])))
  reject('class',('tuple',(y[1][0],('atom','throw'),*y[1][2:])))
  reject('reason',('tuple',(*y[1][:2],('atom','different'),y[1][3])))
  reject('frame_module',framechange(lambda f:('tuple',(('atom','erlang'),*f[1][1:]))))
  reject('frame_function',framechange(lambda f:('tuple',(f[1][0],('atom','different'),*f[1][2:]))))
  reject('frame_metadata',framechange(lambda f:('tuple',(*f[1][:3],('list',(('atom','different'),))))))
  reject('caller_appended',('tuple',(*y[1][:3],('list',y[1][3][1]+y[1][3][1][:1]))))
  reject('argcount',argschange(lambda z:('list',z[1]+(('atom','extra'),))))
  def suffix(z,fn):
   args=list(z[1]);i=0 if fun=='reverse' else 1;args[i]=fn(args[i]);return ('list',tuple(args))
  reject('tail',argschange(lambda z:suffix(z,lambda s:('improper',s[1],('atom','other')))))
  reject('integer',argschange(lambda z:suffix(z,lambda s:('improper',(('integer',-999),)+s[1][1:],s[2]))))
  reject('missing_cell',argschange(lambda z:suffix(z,lambda s:('improper',s[1][1:],s[2]))))
  if fun=='member':reject('item',argschange(lambda z:('list',(('atom','different'),z[1][1]))))
  else:reject('accumulator',argschange(lambda z:('list',(z[1][0],('list',(('integer',-999),)+z[1][1][1])))))
  reject('binding_missing',y,None)
  for field,value in [('ActualOwnTrapEntrySnapshotsSourceReviewed',False),('EveryBudgetHeapStateDifferenceExplained',False),('NativePartitionSeededIntoMain',True)]:reject(field,y,dict(binding,**{field:value}))
  wrong=copy.deepcopy(binding);wrong['Files'][0]['Sha256']='0'*64;reject('tampered_source_or_dll',y,wrong)
 if 'core' in seeds:
  x,y,ra,rb,env=seeds['core'];coreglobals.update(env)
  variants=[('uniq',lambda f:f[:3]+(bytes(16),)+f[4:]),('arity',lambda f:f[:2]+(f[2]+1,)+f[3:]),('index',lambda f:f[:4]+(f[4]+9999,)+f[5:]),('oldindex',lambda f:f[:6]+(('integer',9999),)+f[7:]),('oldunique',lambda f:f[:7]+(('integer',9999),)+f[8:]),('env',lambda f:f[:9]+((('integer',9999),),)+f[10:])]
  for name,fn in variants:
   bad=mutate(y,lambda n:bool(n) and n[0]=='new_fun',fn);c=Core(x,y,ra,rb);controls.append({'Seed':'core','Name':name,'Rejected':not c.run(x,bad)})
  for tag,fn in [('pid',lambda n:(n[0],n[1],9999,*n[3:])),('reference',lambda n:(n[0],n[1],n[2],(9999,)+n[3][1:],n[4])),('port',lambda n:(n[0],n[1],9999,*n[3:]))]:
   bad=mutate(y,lambda n:bool(n) and n[0]==tag,fn);c=Core(x,y,ra,rb);controls.append({'Seed':'core','Name':tag,'Rejected':not c.run(x,bad)})
 if 'proplist' in seeds:
  x,y,ac,bc=seeds['proplist']
  for label,field,value in [('uniq',3,bytes(16)),('index',4,9999),('oldindex',6,('integer',9999)),('oldunique',7,('integer',9999)),('env',9,(('integer',9999),))]:
   bad=mutate(y,lambda n:bool(n) and n[0]=='new_fun',lambda n:n[:field]+(value,)+n[field+1:]);c=V.Compare();controls.append({'Seed':'proplist','Name':label,'Rejected':not c.run(x,bad,ac,bc)})
  for name,ctx in [('actual_md5',replace(bc,md5=bytes(16))),('actual_initpid',replace(bc,pid=('atom','invalid')))]:
   c=V.Compare();controls.append({'Seed':'proplist','Name':name,'Rejected':not c.run(x,y,ac,ctx)})
 if 'wire' in seeds:
  x,y,ac,bc=seeds['wire'];bad=mutate(y,lambda n:bool(n) and n[0]=='binary',lambda n:('binary',n[1][:-1]));c=V.Compare();controls.append({'Seed':'nested_wire','Name':'truncate','Rejected':not c.run(x,bad,ac,bc)})
 if 'info' in seeds:
  x,y,ac,bc=seeds['info'];fields=list(y[1][1][1]);fields[0],fields[1]=fields[1],fields[0];bad=('tuple',(y[1][0],('list',tuple(fields))));c=V.Compare();controls.append({'Seed':'fun_info','Name':'ordered_fields','Rejected':not c.run(x,bad,ac,bc)})
 assert seeds.get('entry_reverse') and seeds.get('entry_member') and seeds.get('named') and seeds.get('proplist') and seeds.get('core')
 assert controls and all(c['Rejected'] for c in controls)
 unexpected=[x for x in results if not x['AcceptedWholeFields'] and x['Kind'] not in {'NATIVE_ONLY_DIAGNOSTIC','OWN_HAMT_TOPOLOGY_SOURCE_REVIEW_REQUIRED'}]
 result={'Status':'PASS_BOUNDED_FIELDS_WITH_EXPLICIT_OWN_TOPOLOGY_PENDING' if not unexpected else 'FAIL_UNEXPECTED_FIELDS','Stages':stage_reports,'Observations':len(results),'StrictWholeEqual':sum(x['StrictWholeEqual'] for x in results),'AcceptedWholeFields':sum(x['AcceptedWholeFields'] for x in results),'RawDifferences':[x for x in results if not x['StrictWholeEqual']],'Unexpected':unexpected,'Rows':results,'NegativeControls':controls,'Evidence':evidence,'SourceDeltasReviewed':source_proof,'ExplicitCapturedDllRetirement':retirements,'OwnTopologyPending':[x for x in results if x['Kind']=='OWN_HAMT_TOPOLOGY_SOURCE_REVIEW_REQUIRED'],'All76Claimed':False,'FramesProjected':False,'CaptureRewritten':False,'DownloadedCodeExecuted':False,'RuntimeExecutedByReviewer':False,'CompatibilityPromoted':False}
 save(a.output/'comparison.json',result);print(json.dumps({k:result[k] for k in ['Status','Observations','StrictWholeEqual','AcceptedWholeFields']}));print('unexpected',len(unexpected),'controls',len(controls))
 return 1 if unexpected else 0
if __name__=='__main__':raise SystemExit(main())
