from pathlib import Path
import hashlib,json,re,copy
r=Path.cwd();d=r/'artifacts/root-part318-composed2';c=d/'candidate';assert not d.exists();c.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
g=r/'artifacts/root-part318-composed1';gf=read(g/'focus-source-freeze.json');assert h((g/'actual-core-focus-guard.json').read_bytes())=='BBC4BEF8E432543AA8DA6F02817E398E88F0F2B9176611DA48718EF3017E0060'
cl=g/'focus-source-resource-closure.json';assert h(cl.read_bytes())==gf['SourceClosureSha256']=='C92009D91F54B1D5942586D8A9426A045134F4453EA74C486484DCFB06A9E9BF'
keys={e['Path'] for e in read(cl)['Files']};owned={e['Path'] for e in gf['Files']}
for e in read(cl)['Files']:
 raw=(g/'candidate'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p=c/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
a=r/'artifacts/group-leader-outbound318-collector1';fp=a/'source-freeze.json';assert h(fp.read_bytes())=='F22C73B90D564804E70EC3C1C922C5ECB9763BF8E097B1696615A7F9AF887BBF';af=read(fp)
for e in af['Files']:
 if not e['Path'].startswith('overlay/'):continue
 n=e['Path'][len('overlay/'):];raw=(a/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p=c/n
 if n=='.gitattributes':assert h(p.read_bytes())==af['ParentAttributesSha256']
 elif n=='.github/workflows/distribution-signal-observations.yml':assert h(p.read_bytes())==af['ParentWorkflowSha256']
 else:assert not p.exists(),n
 p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw);keys.add(n);owned.add(n)
p=c/'.gitattributes';raw=p.read_bytes();assert b'docs/validation/part-318/** binary' not in raw;p.write_bytes(raw.rstrip(b'\r\n')+b'\n'+b'docs/validation/part-318/** binary\n')
assert len(keys)==2272 and len(owned)==16
newsrc={n for n in keys if n.startswith('src/') and not(r/n).exists()};assert newsrc=={'src/Erlang.Runtime/ProcessRuntime.GroupLeaderOutput.cs','src/Erlang.Runtime/RuntimeDistributionPending.GroupLeaderOutput.cs'}
contracts=[('tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','REVIEWED_SOURCE_PLAN_SHA256',1088),('tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml','REVIEWED_REALLOCATION_PLAN_SHA256',1113)]
plans=[]
for name,wf,pin_key,count in contracts:
 before=read(c/name);updated=copy.deepcopy(before);prior={e['Path']:e for e in before['Files']};names=set(prior)|newsrc;assert len(names)==count
 rows=[]
 for n in sorted(names):
  mode=prior[n]['HashMode'] if n in prior else 'TextLF';raw=(c/n).read_bytes();raw=raw.replace(b'\r\n',b'\n') if mode=='TextLF' else raw;rows.append({'Path':n,'Bytes':len(raw),'Sha256':h(raw),'HashMode':mode})
 updated['Files']=rows;assert {k:v for k,v in updated.items() if k!='Files'}=={k:v for k,v in before.items() if k!='Files'}
 write(c/name,updated);pin=h((c/name).read_bytes());raw,matches=re.subn(rb'(?m)^([ \t]*'+pin_key.encode()+rb': )[A-F0-9]{64}[ \t]*\r?$',lambda match:match[1]+pin.encode(),(c/wf).read_bytes());assert matches==1;(c/wf).write_bytes(raw);owned|={name,wf};plans.append({'Path':name,'Inputs':count,'Sha256':pin,'AllNonFilesImportAbiPolicyFieldsUnchanged':True})
for plan in plans:
 assert b'\r' not in (c/plan['Path']).read_bytes()
 for e in read(c/plan['Path'])['Files']:
  raw=(c/e['Path']).read_bytes();raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
tool=c/'tools/Erlang.GroupLeaderOutboundObservations';source_plan=tool/'source-plan.json';assert h(source_plan.read_bytes())==af['SourcePlanSha256']=='ACEE30ABEDDF9EF2572F50D0008699ADDE9B9F02C44470AD7C8E2AC5F58F948F' and b'\r' not in source_plan.read_bytes()
for e in read(source_plan)['Sources']:
 raw=(tool/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
for e in gf['Files']:assert h((c/e['Path']).read_bytes())==e['Sha256']
assert len(owned)==20 and sum(n.endswith('.cs') for n in owned)==6
row=lambda n:{'Path':n,'Bytes':(c/n).stat().st_size,'Sha256':h((c/n).read_bytes())}
closure=[row(n) for n in sorted(keys)]
for e in closure:
 if e['Path'] not in owned:assert (r/e['Path']).read_bytes()==(c/e['Path']).read_bytes(),e['Path']
write(d/'source-resource-closure.json',{'Files':closure});files=[]
for n in sorted(owned):
 e=row(n);e.update(Added=not(r/n).exists(),ParentSha256=h((r/n).read_bytes()) if (r/n).exists() else None);files.append(e)
v={'Status':'ROOT_FINAL318_CORE_AND_ORIGINAL_COLLECTOR_RESEALED_PENDING_PEER','ParentHead':'50cee883796a95c11fcde318098ce8a1156685ca','SourcePaths':20,'CSharpPaths':6,'SourceClosurePaths':2272,'FinalSourceClosureSha256':h((d/'source-resource-closure.json').read_bytes()),'Files':files,'Plans':plans,'CollectorSourcePlanSha256':h(source_plan.read_bytes()),'CoreFocusFreezeSha256':h((g/'focus-source-freeze.json').read_bytes()),'ActualSevenGreenAndHistoricalRedGuardSha256':h((g/'actual-core-focus-guard.json').read_bytes()),'CollectorAuthorFreezeSha256':h(fp.read_bytes()),'CoreSixActualCompiledSourceFilesRawExact':True,'CollectorOriginalExecutionClaimed':False,'ExpectedFullPassed':6668,'ExpectedRegistry':509,'ExpectedErlangMfas':351,'RootCompositionScriptSha256':h(Path(__file__).read_bytes())}
write(d/'source-freeze.json',v);print(json.dumps({'FreezeSha256':h((d/'source-freeze.json').read_bytes()),'SourceClosureSha256':v['FinalSourceClosureSha256'],'Owned':20,'CSharp':6,'Closure':2272,'Plans':plans}))
