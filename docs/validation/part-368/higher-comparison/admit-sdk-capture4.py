from pathlib import Path
import json,hashlib,importlib.util,collections
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent;S=R/'artifacts/queue-integration368/capture4/sdk';T=R/'artifacts/queue-original368-original1/tool';H=lambda f:hashlib.sha256(f.read_bytes()).hexdigest().upper();J=lambda f:json.loads(f.read_bytes())
spec=importlib.util.spec_from_file_location('newqueue_decoder',T/'passive_etf_family.py');D=importlib.util.module_from_spec(spec);spec.loader.exec_module(D)
def names(t,expected):
 assert t[0]=='list' and len(t[1])==len(expected);r={}
 for n,x in zip(expected,t[1]):
  assert x[0]=='tuple' and len(x[1])==2 and x[1][0]==n and n not in r
  r[n]=x[1][1]
 return r
profile=J(T/'profile.json');comp=J(S/'completion.json');seal=J(S/'capture-seal.json');assert comp['FailedGroups']==0 and len(comp['Groups'])==len(profile['Fixtures'])==8
for x in seal['Files']:
 f=(S/x['Path']).resolve();assert f.is_relative_to(S.resolve()) and f.stat().st_size==x['Bytes'] and H(f)==x['Sha256']
rows=[];depths=[('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)]
for f,g in zip(profile['Fixtures'],comp['Groups']):
 assert f['Id']==g['Id'] and not g['ExecutionSkipped'];A=S/f['Id'];wanted=[('atom',n) for n in f['Names']];sources=[T/f['Path']]+[T/x for x in f['Companions']];assert len(sources)==len(g['Compile'])==3
 for src,c in zip(sources,g['Compile']):assert c['Passed'] and Path(c['Path']).resolve()==src.resolve() and c['SourceSha256']==H(src) and c['DeclaredSourceFile']==src.name
 assert g['ReturnedSha256']==H(A/'returned.etf') and g['StacksSha256']==H(A/'stack-observations.etf')
 val=names(D.decode_exact((A/'returned.etf').read_bytes()),wanted);stack=names(D.decode_exact((A/'stack-observations.etf').read_bytes()),depths);counts=collections.Counter()
 for lane,group in [('values',val)]+[(repr(k),names(v,wanted)) for k,v in stack.items()]:
  for v in group.values():
   assert v[0]=='tuple';x=v[1]
   if x[0]==('atom','ok'):assert len(x)==2;counts['ok']+=1
   else:
    assert x[0]==('atom','error') and x[1][0]=='atom' and len(x)==(3 if lane=='values' else 4);counts['error']+=1
    if lane!='values':assert x[3][0]=='list' and all(z[0]=='tuple' and len(z[1])==4 for z in x[3][1])
 b=J(A/'source-function-bindings.json');assert (not b['Bindings'] or b['SourceResolverInvoked']) and not b['GuestCodeExecutedByDiagnostic']
 for x in b['Bindings']:assert x['ResolverBound'] and x['NamedDefinitionExists'] and x['Name']==x['ResolvedName']==x['ResolvedNamedFunction'] and x['EnvironmentCount']==0
 rows.append({'Group':f['Id'],'Values':len(wanted),'FullStacks':len(wanted)*5,'SourceCompiles':3,'SourceBindings':len(b['Bindings']),'Counts':dict(counts)})
closure=J(S.parent/'main-dll-closure.json');assert closure['MainSource'] and not closure['PrivateCandidateDllsUsed'] and len(closure['Files'])==5
for x in closure['Files']:assert H(R/x['Path'])==H(R/x['CopiedTo'])==x['Sha256']
q=J(S.parent/'new-capture.exit.json');assert q['Settled'] and not q['TimedOut'] and q['ExitCode']==0 and not q['Failures']
assert sum(x['Values'] for x in rows)==893
report={'Status':'PASS_PASSIVE_NEW_CAPTURE_SHAPE_CLOSURE_ONLY','Rows':rows,'Values':893,'FullStackObservations':4465,'ProfileSha256':H(T/'profile.json'),'SealSha256':H(S/'capture-seal.json'),'CaptureReceiptSha256':H(S.parent/'new-capture.exit.json'),'DllClosureSha256':H(S.parent/'main-dll-closure.json'),'OriginalCompared':False,'Normalization':False,'Projection':False,'CompatibilityPromoted':False}
(O/'final-capture4/sdk-admission.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
