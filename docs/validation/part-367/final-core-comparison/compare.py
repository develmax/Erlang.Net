from pathlib import Path
import hashlib,importlib.util,json,struct,copy
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
T=R/'artifacts/queue-original367-original1/tool';N=T.parent/'download/run-39071324870/artifact-12114654364/capture';A=N/'queue_core';B=R/'artifacts/queue-integration367/capture2/sdk/queue_core'
def H(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def J(p):return json.loads(p.read_bytes())
def load(p,name):
 s=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
D=load(T/'passive_etf_family.py','actual_queue_decoder');bind=load(R/'artifacts/proplists-stack-relational-peer3/source_binding.py','trusted_queue_source_binding');beam=load(R/'artifacts/proplists22-original-peer1/audit.py','trusted_queue_module_md5')
def decode(p):return D.decode_exact(p.read_bytes())
def named(t):
 assert t[0]=='list';rows={}
 for r in t[1]:
  assert r[0]=='tuple' and len(r[1])==2 and r[1][0] not in rows;rows[r[1][0]]=r[1][1]
 return rows
# Reused bounded parser from read-extra-funt.py; downloaded BEAM is data only.
bp=A/'ebin/queue_core_contract.beam';data=bp.read_bytes();md5,_=beam.module_md5(data);chunks={};pos=12
while pos<len(data):
 size=int.from_bytes(data[pos+4:pos+8],'big');key=data[pos:pos+4].decode('ascii');assert key not in chunks and pos+8+size<=len(data);chunks[key]=data[pos+8:pos+8+size];pos+=8+(size+3)//4*4
assert pos==len(data)
ab=chunks.get('AtU8',chunks.get('Atom'));count=int.from_bytes(ab[:4],'big',signed=True);atoms=[None];i=4
while i<len(ab):
 n=ab[i];i+=1
 if count<0:
  if n&8:n=((n&224)<<3)|ab[i];i+=1
  else:n=n>>4
 atoms.append(ab[i:i+n].decode('utf8'));i+=n
assert i==len(ab) and len(atoms)-1==abs(count)
fun=chunks['FunT'];assert len(fun)==4+int.from_bytes(fun[:4],'big')*24
funt=[]
for off in range(4,len(fun),24):
 atom,arity,label,index,free,olduniq=struct.unpack('>6I',fun[off:off+24]);funt.append(dict(Name=atoms[atom],Arity=arity,Label=label,Index=index,FreeVariables=free,OldUnique=olduniq))
assert len(funt)==1 and funt[0]['Name']=='local_identity' and funt[0]['Arity']==1 and funt[0]['FreeVariables']==0
(O/'original-funt.json').write_text(json.dumps(dict(BeamPath=str(bp),BeamSha256=H(bp),OwnModuleMd5=md5.hex().upper(),FunTableRows=funt,ParserReusedFrom='artifacts/proplists-stack-original-peer1/read-extra-funt.py',ParserSha256=H(R/'artifacts/proplists-stack-original-peer1/read-extra-funt.py'),DownloadedCodeExecuted=False),indent=2)+'\n')
smd=dict(x[1] for x in decode(B/'source-module-metadata.etf')[1])[('atom','queue_core_contract')][1];init=decode(B/'runtime-metadata.etf')[1][2];bindings=J(B/'source-function-bindings.json')['Bindings']
profile=J(T/'profile.json');names=[('atom',x) for x in profile['Fixtures'][0]['Names']];depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
completion=J(N/'completion.json');assert completion['PinnedOtpCommit']==profile['PinnedOtpCommit'] and completion['ProfileSha256']==H(T/'profile.json') and completion['SourceInputsSha256']==H(T/'source-inputs.corrected.json') and completion['FailedGroups']==0
comp=completion['Groups'][0]
for filename,key in [('returned.etf','ReturnedSha256'),('stack-observations.etf','RawStacksSha256'),('metadata.etf','MetadataSha256')]:assert H(A/filename)==comp[key]
assert next(x for x in comp['CompiledFiles'] if x['Module']=='queue_core_contract')['Sha256']==H(bp)
class Compare:
 def __init__(self,origin,sdk,ap,bp):
  self.errors=[];self.relations=[];self.spans=[];self.ap=ap;self.bp=bp
  av=origin[1][1][1][0][1];bv=sdk[1][1][1][0][1]
  self.pid=(av[0],bv[0]);self.ref=(av[1],bv[1])
  assert self.pid[0][0]==self.pid[1][0]=='pid' and self.pid[0][1]==self.pid[1][1]==init[1] and self.pid[0][4:]==self.pid[1][4:]==init[4:]
  assert self.ref[0][0]==self.ref[1][0]=='reference' and self.ref[0][1:3]==self.ref[1][1:3] and self.ref[0][4]==self.ref[1][4] and len(self.ref[0][3])==len(self.ref[1][3])
 def fail(self,path,a,b,reason):self.errors.append(dict(Path=path,Reason=reason,Original=repr(a),Sdk=repr(b)));return False
 def span(self,n,raw,path):
  x=n[10];assert x[0]=='raw_span';start,length=x[1:];assert raw[start]==112 and length==n[1]+1 and int.from_bytes(raw[start+1:start+5],'big')==n[1] and 0<=start<start+length<=len(raw)
  self.spans.append(dict(Path=path,Offset=start,Bytes=length,Sha256=hashlib.sha256(raw[start:start+length]).hexdigest().upper()))
 def run(self,a,b,path='$'):
  if not(isinstance(a,tuple) and isinstance(b,tuple)):
   return a==b or self.fail(path,a,b,'exact scalar')
  if a and b and a[0]==b[0]=='new_fun':
   try:
    self.span(a,self.ap,path+'.original');self.span(b,self.bp,path+'.sdk')
    bound=bind.bind_named(a,b,funt,bindings,md5,smd,init)
    self.relations.append(dict(Path=path,Kind='ACTUAL_SOURCE_CODE_TABLE',Binding=bound))
    return True
   except (AssertionError,IndexError,KeyError,TypeError):return self.fail(path,a,b,'named source FUN own metadata')
  if a and b and a[0]==b[0]=='pid':
   if a==self.pid[0] and b==self.pid[1]:self.relations.append(dict(Path=path,Kind='OWN_SELF_PID',Original=repr(a),Sdk=repr(b)));return True
   return a==b or self.fail(path,a,b,'unbound PID field')
  if a and b and a[0]==b[0]=='reference':
   if a==self.ref[0] and b==self.ref[1]:self.relations.append(dict(Path=path,Kind='OWN_FRESH_REFERENCE_FULL_WORDS',Original=repr(a),Sdk=repr(b)));return True
   return a==b or self.fail(path,a,b,'unbound reference full fields')
  if a and b and a[0]==b[0]=='port':
   if a[:4]==b[:4] and a[4] in (102,89,120) and b[4] in (102,89,120):
    self.relations.append(dict(Path=path,Kind='EXACT_PORT_LOGICAL_FIELDS_AUTHORIZED_WIRE_FORM',Original=repr(a),Sdk=repr(b)));return True
   return self.fail(path,a,b,'literal port fields')
  if len(a)!=len(b) or not a or not b or a[0]!=b[0]:return self.fail(path,a,b,'exact term kind/arity')
  if a[0]=='map':
   ad=dict(a[1]);bd=dict(b[1])
   if ad.keys()!=bd.keys():return self.fail(path,a,b,'exact map keys')
   good=True
   for k in ad:good=self.run(ad[k],bd[k],path+'.map['+repr(k)+']') and good
   return good
  if a[0] in ('tuple','list'):
   if len(a[1])!=len(b[1]):return self.fail(path,a,b,'exact ordered cardinality')
   good=True
   for i,(x,y) in enumerate(zip(a[1],b[1])):good=self.run(x,y,path+'['+str(i)+']') and good
   return good
  if a[0]=='improper':
   if len(a[1])!=len(b[1]):return self.fail(path,a,b,'improper prefix cardinality')
   good=self.run(a[2],b[2],path+'.tail')
   for i,(x,y) in enumerate(zip(a[1],b[1])):good=self.run(x,y,path+'['+str(i)+']') and good
   return good
  return a==b or self.fail(path,a,b,'exact nonidentity term')
rows=[];opaque=[];seeds=[]
for filename in ['returned.etf','stack-observations.etf']:
 ap=(A/filename).read_bytes();bp=(B/filename).read_bytes();left=named(D.decode_exact(ap));right=named(D.decode_exact(bp));assert list(left)==list(right)==(names if filename=='returned.etf' else depths)
 pairs=[('values',k,v,right[k]) for k,v in left.items()] if filename=='returned.etf' else [(repr(d),k,v,named(right[d])[k]) for d,sub in left.items() for k,v in named(sub).items()]
 for depth,name,a,b in pairs:
  report=dict(File=filename,Depth=depth,Name=name[1],StrictWholeDecodedEqual=a==b)
  if name==('atom','opaque_raw'):
   c=Compare(a,b,ap,bp);report['SourceBoundWholeFieldEqual']=c.run(a,b);report['Relations']=c.relations;report['Spans']=c.spans;report['Errors']=c.errors;opaque.append(report);seeds.append((a,b,ap,bp))
  else:report['SourceBoundWholeFieldEqual']=a==b
  rows.append(report)
assert len(rows)==960 and len(opaque)==6
# Corrupt exactly one non-anchor occurrence, retaining genuine context anchors.
def change(n,predicate,replacement):
 done=False
 def walk(v):
  nonlocal done
  if not done and isinstance(v,tuple) and predicate(v):done=True;return replacement(v)
  if isinstance(v,tuple):return tuple(walk(x) for x in v)
  if isinstance(v,frozenset):return frozenset(walk(x) for x in v)
  return v
 result=walk(n);assert done;return result
controls=[];a,b,ap,bp=seeds[0]
for label,mutation in [('fun_unique',lambda x:x[:3]+(bytes(16),)+x[4:]),('fun_arity',lambda x:x[:2]+(2,)+x[3:]),('fun_index',lambda x:x[:4]+(x[4]+1,)+x[5:]),('fun_oldindex',lambda x:x[:6]+(('integer',9999),)+x[7:]),('fun_oldunique',lambda x:x[:7]+(('integer',9999),)+x[8:]),('fun_owner',lambda x:x[:8]+(('pid',init[1],9999,0,0,88),)+x[9:]),('fun_env',lambda x:x[:9]+((('integer',99),),)+x[10:])]:
 c=Compare(a,b,ap,bp);altered=change(b,lambda x:bool(x) and x[0]=='new_fun',mutation);ok=c.run(a,altered);controls.append(dict(Name=label,Rejected=not ok,Errors=c.errors))
for label,tag,mutation in [('pid_single_occurrence','pid',lambda x:(x[0],x[1],9999,*x[3:])),('reference_single_occurrence','reference',lambda x:(x[0],x[1],x[2],(9999,)+x[3][1:],x[4])),('reference_word_count','reference',lambda x:(x[0],x[1],x[2],x[3]+(0,),x[4])),('port_literal_id','port',lambda x:(x[0],x[1],9999,*x[3:])),('external_fun_function','export_fun',lambda x:(x[0],x[1],('atom','length'),x[3]))]:
 c=Compare(a,b,ap,bp);altered=change(b,lambda x:bool(x) and x[0]==tag,mutation);ok=c.run(a,altered);controls.append(dict(Name=label,Rejected=not ok,Errors=c.errors))
c=Compare(a,b,ap,bp);altered=change(b,lambda x:bool(x) and x[0]=='integer' and x[1]==340282366920938463463374607431768211456,lambda x:('integer',x[1]+1));controls.append(dict(Name='arbitrary_exact_value',Rejected=not c.run(a,altered),Errors=c.errors))
# Exact error and frame fields have no identity exceptions.
raw_stacks=named(decode(A/'stack-observations.etf'));sdk_stacks=named(decode(B/'stack-observations.etf'))
e= named(raw_stacks[('atom','default')])[('atom','is_empty_bad_tuple0')]
g= named(sdk_stacks[('atom','default')])[('atom','is_empty_bad_tuple0')]
error_mutations=[
 ('error_class',('tuple',(g[1][0],('atom','throw'),*g[1][2:]))),
 ('error_reason',('tuple',(*g[1][:2],('atom','different_reason'),*g[1][3:]))),
 ('source_file',change(g,lambda x:x and x[0]=='tuple' and len(x[1])==2 and x[1][0]==('atom','file'),lambda x:('tuple',(x[1][0],('list',(('integer',120),)))))),
 ('source_line',change(g,lambda x:x and x[0]=='tuple' and len(x[1])==2 and x[1][0]==('atom','line'),lambda x:('tuple',(x[1][0],('integer',9999))))),
 ('effective_args',change(g,lambda x:x and x[0]=='tuple' and len(x[1])==4 and x[1][0]==('atom','queue'),lambda x:('tuple',(x[1][0],x[1][1],('list',(('integer',9999),)),x[1][3])))),
 ('caller_frame_removed',('tuple',(*g[1][:3],('list',g[1][3][1][:1]))))]
for label,altered in error_mutations:controls.append(dict(Name=label,Rejected=e!=altered,Reason='Nonopaque whole decoded equality has no exception'))
for label,field in [('source_module_md5', 'smd'),('original_module_md5','md5'),('source_resolver_binding','bindings')]:
 previous=globals()[field]
 try:
  if field=='bindings':
   changed=copy.deepcopy(bindings)
   for entry in changed:entry['ResolverBound']=False
   globals()[field]=changed
  else:globals()[field]=bytes(16)
  c=Compare(a,b,ap,bp);ok=c.run(a,b);controls.append(dict(Name=label,Rejected=not ok,Errors=c.errors))
 finally:globals()[field]=previous
assert all(x['Rejected'] for x in controls)
result=dict(Status='PASS_ALL960_SOURCE_BOUND_WHOLE_FIELD_OBSERVATIONS' if all(x['SourceBoundWholeFieldEqual'] for x in rows) else 'FAIL_SOURCE_BOUND_OBSERVATIONS',StrictWholeEqual=sum(x['StrictWholeDecodedEqual'] for x in rows),Total=960,Values=160,FullStacks=800,OpaqueBoundObservations=6,AllNonOpaqueExact=all(x['StrictWholeDecodedEqual'] for x in rows if x['Name']!='opaque_raw'),NegativeControls=controls,Rows=rows,OriginalFunTableSha256=H(O/'original-funt.json'),OriginalReturnedSha256=H(A/'returned.etf'),SdkReturnedSha256=H(B/'returned.etf'),OriginalStacksSha256=H(A/'stack-observations.etf'),SdkStacksSha256=H(B/'stack-observations.etf'),ProfileSha256=H(T/'profile.json'),TrustedDecoderSha256=H(T/'passive_etf_family.py'),TrustedSourceBindingSha256=H(R/'artifacts/proplists-stack-relational-peer3/source_binding.py'),Projection=False,Normalization=False,FramesStripped=False,CapturedIdentityInjected=False,DownloadedCodeExecuted=False,CompatibilityPromoted=False,OriginalRunArtifactProvenanceAdmission='Independent map_size_finish owner; this report separately binds completion/profile/source-inputs/raw-file/fixture-BEAM hashes.')
(O/'comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:result[k] for k in ['Status','StrictWholeEqual','Total','AllNonOpaqueExact']},indent=2));print('opaque',[(x['Depth'],x['SourceBoundWholeFieldEqual'],x['Errors']) for x in opaque]);print('controls',len(controls))
