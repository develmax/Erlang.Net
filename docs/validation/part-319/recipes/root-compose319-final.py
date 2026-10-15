from pathlib import Path
import hashlib,json,re,copy
r=Path.cwd();d=r/'artifacts/root-part319-composed3';c=d/'candidate';assert not d.exists();c.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
g=r/'artifacts/root-part319-composed2';gf=read(g/'focus-source-freeze.json');assert h((g/'actual-core-focus-guard.json').read_bytes())=='B730924F784ADC032CB16F259AB66ECE96AC44E4E0905F91221E38F04B496005'
cl=g/'focus-source-resource-closure.json';assert h(cl.read_bytes())==gf['SourceClosureSha256']=='CFCE55AC406ECAEE5C00E30EB19212A6B163BBC5481877D6AF0F5B0BD018DFA2'
keys={e['Path'] for e in read(cl)['Files']};owned={e['Path'] for e in gf['Files']}
for e in read(cl)['Files']:
 raw=(g/'candidate'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p=c/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
a=r/'artifacts/guest-connected-send319-original-candidate2';fp=a/'source-freeze.json';assert h(fp.read_bytes())=='67FEFC493A2D8D6B2BD9A5CB012D27B19428CD399D52539C6AF4DA6954D7DC41';af=read(fp)
for e in af['Files']:
 name=e['Path'];raw=(a/'overlay'/name).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p=c/name
 if p.exists():assert h(p.read_bytes())==e['ParentSha256'],name
 else:assert not e.get('ParentSha256'),name
 p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw);keys.add(name);owned.add(name)
p=c/'.gitattributes';raw=p.read_bytes();assert b'docs/validation/part-319/** binary' not in raw;p.write_bytes(raw.rstrip(b'\r\n')+b'\n'+b'docs/validation/part-319/** binary\n')
assert len(keys)==2290 and len(owned)==27
newsrc={n for n in keys if n.startswith('src/') and not(r/n).exists()};assert len(newsrc)==6
contracts=[('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1094),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1119)]
plans=[]
for name,wf,key,count in contracts:
 before=read(c/name);updated=copy.deepcopy(before);prior={e['Path']:e for e in before['Files']};names=set(prior)|newsrc;assert len(names)==count
 rows=[]
 for n in sorted(names):
  mode=prior[n]['HashMode'] if n in prior else 'TextLF';raw=(c/n).read_bytes();raw=raw.replace(b'\r\n',b'\n') if mode=='TextLF' else raw;rows.append({'Path':n,'Bytes':len(raw),'Sha256':h(raw),'HashMode':mode})
 updated['Files']=rows;assert {k:v for k,v in updated.items() if k!='Files'}=={k:v for k,v in before.items() if k!='Files'};write(c/name,updated)
 pin=h((c/name).read_bytes());raw,matches=re.subn(rb'(?m)^([ \t]*'+key.encode()+rb': )[A-F0-9]{64}[ \t]*\r?$',lambda m:m[1]+pin.encode(),(c/wf).read_bytes());assert matches==1;(c/wf).write_bytes(raw);owned|={name,wf};plans.append({'Path':name,'Inputs':count,'Sha256':pin,'AllNonFilesImportAbiPolicyFieldsUnchanged':True})
 for e in rows:
  raw=(c/e['Path']).read_bytes();raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
tool=c/'tools/Erlang.GuestConnectedSendObservations';plan=tool/'source-plan.json';assert h(plan.read_bytes())=='5E047B44B2B1905843EC99DC8FB7142B65BBD6D220B28A3B102A3D412A5E40F5' and b'\r' not in plan.read_bytes()
for e in read(plan)['Sources']:
 raw=(tool/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
for e in gf['Files']:assert h((c/e['Path']).read_bytes())==e['Sha256']
assert len(owned)==31 and sum(n.endswith('.cs') for n in owned)==17
row=lambda n:{'Path':n,'Bytes':(c/n).stat().st_size,'Sha256':h((c/n).read_bytes())}
closure=[row(n) for n in sorted(keys)]
for e in closure:
 if e['Path'] not in owned:assert (r/e['Path']).read_bytes()==(c/e['Path']).read_bytes(),e['Path']
write(d/'source-resource-closure.json',{'Files':closure});files=[]
for n in sorted(owned):
 e=row(n);e.update(Added=not(r/n).exists(),ParentSha256=h((r/n).read_bytes()) if (r/n).exists() else None);files.append(e)
v={'Status':'ROOT_FINAL319_CORE_AND_ORIGINAL_COLLECTOR_RESEALED_PENDING_PEER','ParentHead':'f7ef36d1ddba1af2e52f77b7536f34e5db71a04f','SourcePaths':31,'CSharpPaths':17,'SourceClosurePaths':2290,'FinalSourceClosureSha256':h((d/'source-resource-closure.json').read_bytes()),'Files':files,'Plans':plans,'CollectorSourcePlanSha256':h(plan.read_bytes()),'CoreFocusFreezeSha256':h((g/'focus-source-freeze.json').read_bytes()),'ActualNineGreenAndHistoricalRedGuardSha256':h((g/'actual-core-focus-guard.json').read_bytes()),'CollectorAuthorFreezeSha256':h(fp.read_bytes()),'CoreSeventeenActualCompiledSourceFilesRawExact':True,'CollectorOriginalExecutionClaimed':False,'ExpectedFullPassed':6669,'ExpectedRegistry':509,'ExpectedErlangMfas':351,'RootCompositionScriptSha256':h(Path(__file__).read_bytes())}
write(d/'source-freeze.json',v);print(json.dumps({'FreezeSha256':h((d/'source-freeze.json').read_bytes()),'SourceClosureSha256':v['FinalSourceClosureSha256'],'Owned':31,'CSharp':17,'Closure':2290,'Plans':plans}))
