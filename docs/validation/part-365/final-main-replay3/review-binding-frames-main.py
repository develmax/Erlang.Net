from pathlib import Path
import json,hashlib,importlib.util,sys,copy
R=Path(__file__).resolve().parents[2];O=R/'artifacts/proplists-integration365-replay3-peer';H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
s=importlib.util.spec_from_file_location('trusted_review',R/'artifacts/proplists-stack-relational-peer1/validate.py');v=importlib.util.module_from_spec(s);sys.modules[s.name]=v;s.loader.exec_module(v)
orig=R/'artifacts/proplists-stack-binding-original1/download/run-39059210136/artifact-12110778492/capture/fun_continuation_contract';before=R/'artifacts/proplists-stack-binding-original1/sdk-before-guard/capture/fun_continuation_contract';after=R/'artifacts/proplists-integration365/main-replay3/binding-sdk/fun_continuation_contract'
original=v.named(v.decode(orig/'stack-observations.etf'));old=v.named(v.decode(before/'stack-observations.etf'));new=v.named(v.decode(after/'stack-observations.etf'));rows=[];seed=None
for depth,names in original.items():
 on=v.named(names);bn=v.named(old[depth]);an=v.named(new[depth]);assert list(on)==list(bn)==list(an)
 for name,x in on.items():
  b=bn[name];a=an[name];assert x==a
  def frames(t):
   if t[0]=='tuple' and t[1][0]==v.atom('error'):
    assert len(t[1])==4 and t[1][3][0]=='list';return t[1][3][1]
   return ()
  def mfas(t):return [dict(Module=f[1][0][1],Function=f[1][1][1],ArityOrArguments=repr(f[1][2])) for f in frames(t)]
  rows.append(dict(Depth=repr(depth),Name=name[1],BeforeExact=x==b,AfterExact=True,OriginalFrames=mfas(x),BeforeFrames=mfas(b),AfterFrames=mfas(a),OriginalFrameCount=len(frames(x)),BeforeFrameCount=len(frames(b)),AfterFrameCount=len(frames(a))))
  if name==v.atom('opaque_named') and depth==('integer',8):seed=(x,a)
assert seed
x,a=seed;frames=list(a[1][3][1]);assert len(frames)>2
neg=[]
def withframes(f):return ('tuple',a[1][:3]+(('list',tuple(f)),))
# Whole observation equality, including reason and every ordered frame, is the admission predicate.
def reject(name,bad):
 assert bad!=x,name;neg.append(name)
reject('removed_opaque_caller',withframes([f for f in frames if not(f[1][0]==v.atom('fun_continuation_contract') and f[1][1]==v.atom('opaque_call'))]))
reject('duplicated_frame',withframes(frames+[frames[-1]]))
f=list(frames);f[0],f[1]=f[1],f[0];reject('reordered_frames',withframes(f))
f=list(frames);first=list(f[0][1]);loc=list(first[3][1]);assert loc[-1][1][0]==v.atom('line');loc[-1]=('tuple',(v.atom('line'),('integer',999)));first[3]=('list',tuple(loc));f[0]=('tuple',tuple(first));reject('altered_source_line',withframes(f))
reject('altered_reason',('tuple',(a[1][0],a[1][1],v.atom('altered_reason'),a[1][3])))
value=v.decode(after/'returned.etf');swapped=list(value[1]);swapped[0],swapped[1]=swapped[1],swapped[0];assert v.decode(orig/'returned.etf')!=('list',tuple(swapped));neg.append('reordered_case_rows')
miss=[r for r in rows if not r['BeforeExact']];assert len(miss)==6
out=dict(Status='PASS_BOUNDED_BINDING_WHOLE_STACK_OBSERVATIONS',OriginalAcceptanceSha256=H(R/'artifacts/proplists-stack-original-peer1/binding1/acceptance.json'),FullComparisonSha256=H(O/'binding-main-replay3.json'),Values=6,Profiles=30,BeforeGuard=dict(ValueExact=6,ProfileExact=24,Failures=miss),AfterGuard=dict(ValueExact=6,ProfileExact=30),FrameCounts=rows,NegativeControls=neg,EarlierNegativeControls=31,CombinedNegativeControls=31+len(neg),Projection=False,ValueReplacement=False,DownloadedCodeExecuted=False,CompatibilityPromoted=False)
p=O/'binding-frame-review.json';p.write_text(json.dumps(out,indent=2)+'\n');print(json.dumps(dict(Sha256=H(p),BeforeFailures=[dict(Name=r['Name'],Depth=r['Depth'],OriginalFrames=r['OriginalFrameCount'],BeforeFrames=r['BeforeFrameCount']) for r in miss],NewNegativeControls=len(neg)),indent=2))
