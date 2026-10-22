"""Passive exact higher/structural replay. Native acquisition is a separate gate."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,struct,copy
R=Path(__file__).resolve().parents[2]
def H(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def load(path,name):
 s=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def named(t):
 assert t[0]=='list';result={}
 for x in t[1]:
  assert x[0]=='tuple' and len(x[1])==2 and x[1][0] not in result
  result[x[1][0]]=x[1][1]
 return result
def funt(path,md):
 data=path.read_bytes();assert data[:4]==b'FOR1' and data[8:12]==b'BEAM';chunks={};pos=12
 while pos<len(data):
  size=int.from_bytes(data[pos+4:pos+8],'big');key=data[pos:pos+4];assert key not in chunks and pos+8+size<=len(data)
  chunks[key]=data[pos+8:pos+8+size];pos+=8+(size+3)//4*4
 assert pos==len(data)
 ab=chunks.get(b'AtU8',chunks.get(b'Atom'));count=int.from_bytes(ab[:4],'big',signed=True);atoms=[None];i=4
 while i<len(ab):
  n=ab[i];i+=1
  if count<0:
   if n&8:n=((n&224)<<3)|ab[i];i+=1
   else:n>>=4
  atoms.append(ab[i:i+n].decode('utf8'));i+=n
 assert i==len(ab) and len(atoms)-1==abs(count)
 fun=chunks.get(b'FunT',bytes(4));assert len(fun)==4+int.from_bytes(fun[:4],'big')*24
 rows=[]
 for off in range(4,len(fun),24):
  atom,arity,label,index,free,olduniq=struct.unpack('>6I',fun[off:off+24]);rows.append(dict(Name=atoms[atom],Arity=arity,Label=label,Index=index,FreeVariables=free,OldUnique=olduniq))
 return md.module_md5(data)[0],rows
class Comparison:
 def __init__(self,allow,beams,bindings,smd,init,bind,rawa,rawb):
  self.allow=allow;self.beams=beams;self.bindings=bindings;self.smd=smd;self.init=init;self.bind=bind;self.rawa=rawa;self.rawb=rawb;self.errors=[];self.relations=[]
 def fail(self,path,a,b,reason):
  self.errors.append(dict(Path=path,Reason=reason,Original=repr(a)[:2000],Sdk=repr(b)[:2000]));return False
 def span(self,n,raw):
  off,length=n[10][1:];assert n[10][0]=='raw_span' and raw[off]==112 and length==n[1]+1 and 0<=off<off+length<=len(raw)
  assert int.from_bytes(raw[off+1:off+5],'big')==n[1]
  return dict(Offset=off,Bytes=length,Sha256=hashlib.sha256(raw[off:off+length]).hexdigest().upper())
 def run(self,a,b,path='$'):
  if not(isinstance(a,tuple) and isinstance(b,tuple)):return a==b or self.fail(path,a,b,'exact scalar')
  if a and b and a[0]==b[0]=='new_fun':
   if not self.allow:return self.fail(path,a,b,'FUN relation outside predeclared case')
   try:
    module=a[5][1];md,rows=self.beams[module]
    relation=self.bind.bind_named(a,b,rows,self.bindings,md,self.smd[('atom',module)][1],self.init)
    self.relations.append(dict(Path=path,Binding=relation,OriginalSpan=self.span(a,self.rawa),SdkSpan=self.span(b,self.rawb)));return True
   except (AssertionError,KeyError,IndexError,TypeError):return self.fail(path,a,b,'actual named source binding rejected')
  if len(a)!=len(b) or not a or not b or a[0]!=b[0]:return self.fail(path,a,b,'exact term kind/arity')
  if a[0]=='map':
   ad=dict(a[1]);bd=dict(b[1])
   if ad.keys()!=bd.keys():return self.fail(path,a,b,'exact map keys')
   good=True
   for k in ad:good=self.run(ad[k],bd[k],path+'.map['+repr(k)+']') and good
   return good
  if a[0] in ('tuple','list','improper'):
   if len(a[1])!=len(b[1]):return self.fail(path,a,b,'exact ordered cardinality')
   good=True
   for i,(x,y) in enumerate(zip(a[1],b[1])):good=self.run(x,y,path+'['+str(i)+']') and good
   if a[0]=='improper':good=self.run(a[2],b[2],path+'.tail') and good
   return good
  return a==b or self.fail(path,a,b,'exact remaining fields')
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--native',type=Path,required=True);ap.add_argument('--sdk',type=Path,required=True);ap.add_argument('--tool',type=Path,required=True);ap.add_argument('--policies',type=Path,required=True);ap.add_argument('--output',type=Path,required=True);args=ap.parse_args()
 assert not args.output.exists(),'no overwrite';args.output.mkdir(parents=True)
 D=load(args.tool/'passive_etf_family.py','queue368_trusted_decoder');bind=load(R/'artifacts/proplists-stack-relational-peer3/source_binding.py','queue368_trusted_source_binding');md=load(R/'artifacts/proplists22-original-peer1/audit.py','queue368_trusted_md5')
 profile=json.loads((args.tool/'profile.json').read_bytes());assert profile['PinnedOtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5'
 policies=json.loads(args.policies.read_bytes());policygroups={x['Id']:{r['Name']:r for r in x['Cases']} for x in policies['Groups']}
 rows=[];inputs=[];controls=[];relationseed=None;errorseed=None
 for g in profile['Fixtures']:
  if g['Id'] not in ('queue_callbacks','queue_structural'):continue
  gid=g['Id'];A=args.native/gid;B=args.sdk/gid;names=[('atom',n) for n in g['Names']]
  smd=named(D.decode_exact((B/'source-module-metadata.etf').read_bytes()));init=D.decode_exact((B/'runtime-metadata.etf').read_bytes())[1][2];bindings=json.loads((B/'source-function-bindings.json').read_bytes())['Bindings']
  beams={}
  for module in [g['Module'],'queue_all368_support']:
   bp=A/'ebin'/f'{module}.beam';beams[module]=funt(bp,md);inputs.append(dict(Path=str(bp),Sha256=H(bp)))
  for filename in ('returned.etf','stack-observations.etf'):
   pa=A/filename;pb=B/filename;ra=pa.read_bytes();rb=pb.read_bytes();left=named(D.decode_exact(ra));right=named(D.decode_exact(rb));inputs.extend([dict(Path=str(pa),Sha256=H(pa)),dict(Path=str(pb),Sha256=H(pb))])
   expected=names if filename=='returned.etf' else [('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
   assert list(left)==list(right)==expected
   pairs=[('values',n,left[n],right[n]) for n in names] if filename=='returned.etf' else [(repr(d),n,named(left[d])[n],named(right[d])[n]) for d in expected for n in names]
   for depth,name,a,b in pairs:
    policy=policygroups[gid][name[1]];allow=gid=='queue_callbacks' and policies['CodeIdentityRule']=='Only actual named local FUN fields, bound to original BEAM FunT/md5 and own resolver/md5; all environments and otherfields exact. External guard functions need no opaque mapping.'
    c=Comparison(allow,beams,bindings,smd,init,bind,ra,rb);ok=c.run(a,b)
    rows.append(dict(Group=gid,Case=name[1],Depth=depth,StrictEqual=a==b,SourceBoundWholeEqual=ok,Errors=c.errors,Relations=c.relations))
    if c.relations and relationseed is None:relationseed=(a,b,c)
    if depth!='values' and isinstance(a,tuple) and a[0]=='tuple' and a[1] and a[1][0]==('atom','error') and errorseed is None:errorseed=(a,b,c)
 def mutate(n,predicate,change):
  done=False
  def walk(v):
   nonlocal done
   if not done and isinstance(v,tuple) and predicate(v):done=True;return change(v)
   if isinstance(v,tuple):return tuple(walk(x) for x in v)
   if isinstance(v,frozenset):return frozenset(walk(x) for x in v)
   return v
  value=walk(n);assert done;return value
 assert relationseed is not None and errorseed is not None
 a,b,base=relationseed
 def check(label,altered,bindings=None,smd=None,allow=True):
  c=Comparison(allow,base.beams,base.bindings if bindings is None else bindings,base.smd if smd is None else smd,base.init,base.bind,base.rawa,base.rawb)
  controls.append(dict(Name=label,Rejected=not c.run(a,altered),Errors=c.errors))
 for label,change in [('fun_unique',lambda x:x[:3]+(bytes(16),)+x[4:]),('fun_arity',lambda x:x[:2]+(x[2]+1,)+x[3:]),('fun_index',lambda x:x[:4]+(x[4]+9999,)+x[5:]),('fun_oldindex',lambda x:x[:6]+(('integer',9999),)+x[7:]),('fun_oldunique',lambda x:x[:7]+(('integer',9999),)+x[8:]),('fun_environment',lambda x:x[:9]+((('integer',9999),),)+x[10:]),('fun_owner',lambda x:x[:8]+(('pid',base.init[1],9999,*base.init[3:]),)+x[9:])]:
  check(label,mutate(b,lambda v:bool(v) and v[0]=='new_fun',change))
 check('relation_outside_declared_case',b,allow=False)
 altered_bindings=copy.deepcopy(base.bindings)
 for x in altered_bindings:x['ResolverBound']=False
 check('missing_actual_resolver',b,bindings=altered_bindings)
 check('missing_own_source_module_md5',b,smd={})
 # No identity relation applies to ordinary error class/reason/frame/caller fields.
 a,b,base=errorseed
 for label,altered in [('error_class',('tuple',(b[1][0],('atom','throw'),*b[1][2:]))),('error_reason',('tuple',(*b[1][:2],('atom','corrupted_reason'),*b[1][3:]))),('frame_removed',('tuple',(*b[1][:3],('list',b[1][3][1][1:]))))]:
  c=Comparison(base.allow,base.beams,base.bindings,base.smd,base.init,base.bind,base.rawa,base.rawb);controls.append(dict(Name=label,Rejected=not c.run(a,altered),Errors=c.errors))
 for label,predicate,change in [
  ('source_line',lambda v:bool(v) and v[0]=='tuple' and len(v[1])==2 and v[1][0]==('atom','line'),lambda v:('tuple',(v[1][0],('integer',9999)))),
  ('source_file',lambda v:bool(v) and v[0]=='tuple' and len(v[1])==2 and v[1][0]==('atom','file'),lambda v:('tuple',(v[1][0],('list',(('integer',120),))))),
  ('effective_args',lambda v:bool(v) and v[0]=='tuple' and len(v[1])==4 and v[1][0]==('atom','queue'),lambda v:('tuple',(v[1][0],v[1][1],('list',(('integer',9999),)),v[1][3]))),
  ('frame_function',lambda v:bool(v) and v[0]=='tuple' and len(v[1])==4 and v[1][0]==('atom','queue'),lambda v:('tuple',(v[1][0],('atom','corrupted_function'),v[1][2],v[1][3])))]:
  c=Comparison(base.allow,base.beams,base.bindings,base.smd,base.init,base.bind,base.rawa,base.rawb);controls.append(dict(Name=label,Rejected=not c.run(a,mutate(b,predicate,change)),Errors=c.errors))
 c=Comparison(base.allow,base.beams,base.bindings,base.smd,base.init,base.bind,base.rawa,base.rawb);extra=('tuple',(*b[1][:3],('list',b[1][3][1]+b[1][3][1][-1:])));controls.append(dict(Name='caller_appended',Rejected=not c.run(a,extra),Errors=c.errors))
 assert all(x['Rejected'] for x in controls)

 result=dict(Status='PASS' if all(x['SourceBoundWholeEqual'] for x in rows) else 'FAIL',Rows=rows,Total=len(rows),StrictEqual=sum(x['StrictEqual'] for x in rows),SourceBoundEqual=sum(x['SourceBoundWholeEqual'] for x in rows),Inputs=inputs,ProfileSha256=H(args.tool/'profile.json'),PoliciesSha256=H(args.policies),DecoderSha256=H(args.tool/'passive_etf_family.py'),BindingSha256=H(R/'artifacts/proplists-stack-relational-peer3/source_binding.py'),NegativeControls=controls,NegativeControlGateComplete=True,Projection=False,Normalization=False,FramesRemoved=False,NativeCodeExecuted=False,CompatibilityPromoted=False)
 (args.output/'comparison.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:result[k] for k in ('Status','Total','StrictEqual','SourceBoundEqual')}))
 return 0 if result['Status']=='PASS' else 1
if __name__=='__main__':raise SystemExit(main())
