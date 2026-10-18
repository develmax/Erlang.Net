from pathlib import Path
import importlib.util,json,sys,copy,hashlib
R=Path(__file__).resolve().parents[2];O=R/'artifacts/proplists-integration365-replay2-peer';H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();J=lambda p:json.loads(p.read_bytes())
def load(name,p):
 s=importlib.util.spec_from_file_location(name,p);m=importlib.util.module_from_spec(s);sys.modules[name]=m;s.loader.exec_module(m);return m
v=load('trusted_existing_review',R/'artifacts/proplists-stack-relational-peer1/validate.py');binding=load('own_binding',O/'source_binding.py')
orig=R/'artifacts/proplists-stack-extra-original1/download/run-39055797975/artifact-12110170182/capture';sdk=R/'artifacts/proplists-integration365/main-replay2/extra-sdk';seal,files=v.admit_sdk(sdk);profile=R/'artifacts/proplists-stack-extra-original1/tool/profile.json';oa=v.admit_original(orig,profile)
g='lc_return_frames';od=orig/g;sd=sdk/g
for f in ['source-module-metadata.etf','source-function-bindings.json','runtime-metadata.etf']:assert g+'/'+f in files
mds=dict(x[1] for x in v.decode(sd/'source-module-metadata.etf')[1]);md5=mds[v.atom(g)][1];omd5=bytes.fromhex('D037EB91963096285BBC5D650D4E0C0C');pid=v.decode(sd/'runtime-metadata.etf')[1][2]
b=J(sd/'source-function-bindings.json');assert b['SourceResolverInvoked'] and not b['GuestCodeExecutedByDiagnostic']
funt=J(O/'original-funt.json')['FunTableRows']
class BoundCompare(v.Compare):
 def __init__(self,bindings=None,module_md5=None):super().__init__();self.bindings=b['Bindings'] if bindings is None else bindings;self.module_md5=md5 if module_md5 is None else module_md5
 def run(self,a,c,ac,bc,path='$'):
  if isinstance(a,tuple) and a and a[0]=='new_fun' and a[5]==v.atom(g):
   if not(isinstance(c,tuple) and len(c)==11 and c[0]=='new_fun'):return self.fail(path,'SOURCE_FUN_SHAPE',a,c)
   okay=self.span(a,ac,path+'.original_span') & self.span(c,bc,path+'.sdk_span')
   try:
    result=binding.bind_named(a,c,funt,self.bindings,omd5,self.module_md5,pid)
    matches=[r for r in self.bindings if r['Index']==c[4] and r['UniqueHex']==c[3].hex().upper()]
    for r in matches:
     assert r['SourceIndex']==c[4] and r['SourceOldIndex']==c[6][1] and r['SourceOldUnique']==c[7][1] and r['SourceUniqueHex']==c[3].hex().upper()
    self.relations.append(dict(Path=path,Kind='OWN_CODE_TABLE_NAMED_SOURCE_BINDING',Binding=result,LegacyAnnotations=[dict(SourceLegacyIndex=r['SourceLegacyIndex'],ObservedLegacyIndex=r['ObservedLegacyIndex']) for r in matches]))
   except (AssertionError,KeyError,IndexError,TypeError) as e:okay=self.fail(path,'SOURCE_FUN_BINDING_FAILED',str(e),None) and okay
   # All non-code identity fields, including every environment item, remain recursively exact.
   for i in [1,2,5,8,9]:okay=self.run(a[i],c[i],ac,bc,path+'.fun['+str(i)+']') and okay
   return okay
  return super().run(a,c,ac,bc,path)
reports=[];seed=None
for filename in ['returned.etf','stack-observations.etf']:
 op=od/filename;sp=sd/filename;ac=v.Context(omd5,None,op.read_bytes(),str(op));bc=v.Context(md5,pid,sp.read_bytes(),str(sp));left=v.decode(op);right=v.decode(sp)
 if filename=='returned.etf':pairs=[('values',k,x,v.named(right)[k]) for k,x in v.named(left).items()]
 else:
  l=v.named(left);rr=v.named(right);assert list(l)==list(rr);pairs=[]
  for depth in l:
   names=v.named(l[depth]);rn=v.named(rr[depth]);assert list(names)==list(rn);pairs.extend((repr(depth),k,x,rn[k]) for k,x in names.items())
 for depth,name,x,y in pairs:
  c=BoundCompare();ok=c.run(x,y,ac,bc);reports.append(dict(File=filename,Depth=depth,Name=repr(name),StrictDecodedEqual=x==y,RelationalAllFields=ok,Errors=c.errors,Relations=c.relations,Spans=c.spans))
  if name==v.atom('apply_badarity') and filename=='stack-observations.etf' and depth==repr(v.atom('default')):seed=(x,y,ac,bc)
assert seed
x,y,ac,bc=seed
def change_first_fun(value,index,replacement):
 done=False
 def walk(n):
  nonlocal done
  if isinstance(n,tuple) and n and n[0]=='new_fun' and not done:
   done=True;t=list(n);t[index]=replacement;return tuple(t)
  if isinstance(n,tuple):return tuple(walk(z) for z in n)
  return n
 result=walk(value);assert done;return result
neg=[]
for name,index,value in [('wrong_index',4,99),('wrong_oldindex',6,('integer',99)),('wrong_unique',3,b'X'*16),('wrong_oldunique',7,('integer',0)),('wrong_arity',2,2),('wrong_environment',9,(v.atom('extra'),)),('wrong_module',5,v.atom('wrong'))]:
 c=BoundCompare();assert not c.run(x,change_first_fun(y,index,value),ac,bc),name;neg.append(name)
for key,value in [('ResolvedNamedFunction','wrong'),('ResolverBound',False),('NamedDefinitionExists',False),('SourceIndex',99),('SourceUniqueHex','00'*16)]:
 bad=copy.deepcopy(b['Bindings'])
 for row in bad:row[key]=value
 c=BoundCompare(bad);assert not c.run(x,y,ac,bc),key;neg.append('wrong_binding_'+key)
c=BoundCompare(module_md5=b'X'*16);assert not c.run(x,y,ac,bc);neg.append('wrong_actual_module_md5')
items=list(y[1]);reason=list(items[2][1]);pair=list(reason[1][1]);arguments=list(pair[1][1]);arguments[0]=v.atom('altered_actual_argument');pair[1]=('list',tuple(arguments));reason[1]=('tuple',tuple(pair));items[2]=('tuple',tuple(reason));c=BoundCompare();assert not c.run(x,('tuple',tuple(items)),ac,bc);neg.append('altered_badarity_supplied_argument')
# Preserve whole badarity supplied arguments, reason and frames.
assert y[0]=='tuple' and y[1][0]==v.atom('error')
items=list(y[1]);items[2]=v.atom('wrong_reason');c=BoundCompare();assert not c.run(x,('tuple',tuple(items)),ac,bc);neg.append('wrong_entire_reason')
items=list(y[1]);frames=list(items[3][1]);assert len(frames)>1;frames.pop();items[3]=('list',tuple(frames));c=BoundCompare();assert not c.run(x,('tuple',tuple(items)),ac,bc);neg.append('dropped_stack_frame')
items=list(y[1]);frames=list(items[3][1]);frames[0],frames[1]=frames[1],frames[0];items[3]=('list',tuple(frames));c=BoundCompare();assert not c.run(x,('tuple',tuple(items)),ac,bc);neg.append('reordered_stack_frames')
stats={}
for filename in ['returned.etf','stack-observations.etf']:
 rows=[r for r in reports if r['File']==filename];stats[filename]=dict(Total=len(rows),StrictEqual=sum(r['StrictDecodedEqual'] for r in rows),RelationalAllFields=sum(r['RelationalAllFields'] for r in rows))
out=dict(Status='BOUNDED_SOURCE_BOUND_RELATIONAL_EXTRA_REVIEW',Stats=stats,Rows=reports,NegativeControls=neg,SdkCaptureSealSha256=seal,OriginalAdmission=oa,ActualSdkModuleMd5=md5.hex().upper(),OriginalModuleMd5=omd5.hex().upper(),RawUniqueMatchesOwnActualModuleMd5=True,BindingFileSha256=H(sd/'source-function-bindings.json'),MetadataFileSha256=H(sd/'source-module-metadata.etf'),StrictFailuresRemainFailures=True,Projection=False,ValueReplacement=False,CompatibilityPromoted=False)
p=O/'extra-main-replay2.json';p.write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(dict(Stats=stats,NegativeControls=len(neg),ReportSha256=H(p)),indent=2))
