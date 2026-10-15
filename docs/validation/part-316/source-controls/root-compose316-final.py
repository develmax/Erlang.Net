from pathlib import Path
import hashlib,json,re
r=Path.cwd();d=r/'artifacts/allocator-public-sys-profile316-composed2';c=d/'candidate';assert not d.exists();c.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,x:p.write_bytes((json.dumps(x,indent=2)+'\n').encode())
b=r/'artifacts/allocator-public-sys-profile316-composed1';base=read(r/'artifacts/allocator-public-sys-profile316-candidate1/source-resource-closure.json');keys={e['Path'] for e in base['Files']};of=read(r/'artifacts/allocator-public-sys-profile316-oracle1/source-freeze.json');keys|={e['Path'] for e in of['Files']};assert len(keys)==2239
for n in sorted(keys):
 p=c/n;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes((b/'candidate'/n).read_bytes())
core=read(b/'formatted-core-source-freeze.json');owned={e['Path'] for e in core['Files']}
for e in core['Files']:assert h((c/e['Path']).read_bytes())==e['Sha256']
o=r/'artifacts/allocator-public-sys-profile316-oracle2';p=o/'source-freeze.json';assert h(p.read_bytes())=='B427BA5D2D6BF853F40AE11225BD03D1D7557A9008780C52D79AD8F11FC89B9A'
obs=read(p)
for e in obs['Files']:
 p=c/e['Path'];assert not p.exists() if e['Added'] else h(p.read_bytes())==e['ParentSha256'];raw=(o/'overlay'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw);keys.add(e['Path']);owned.add(e['Path'])
sealer='tools/Erlang.NifPendingVarargsObservations/seal-source-plan.py';p=c/sealer;s=p.read_bytes();old=b"args.output.write_text(json.dumps(receipt, indent=2) + '\\n', encoding='utf-8')";new=b"args.output.write_bytes((json.dumps(receipt, indent=2) + '\\n').encode('utf-8'))";assert s.count(old)==1;p.write_bytes(s.replace(old,new));owned.add(sealer)
p=c/'.gitattributes';raw=p.read_bytes();assert b'docs/validation/part-316/** binary' not in raw;p.write_bytes(raw.rstrip(b'\r\n')+b'\n'+b'docs/validation/part-316/** binary\n');owned.add('.gitattributes')
oldplan='tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';ownplan='tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json';oldwf='.github/workflows/nif-pending-native.yml';ownwf='.github/workflows/nif-reallocation-native.yml'
newsrc={n for n in keys if n.startswith('src/') and n not in {e['Path'] for e in read(r/'artifacts/root-part315-main1/source-resource-closure.json')['Files']}};assert len(newsrc)==8
plans=[]
for name,wf,key in [(oldplan,oldwf,'REVIEWED_SOURCE_PLAN_SHA256'),(ownplan,ownwf,'REVIEWED_REALLOCATION_PLAN_SHA256')]:
 v=read(c/name);before={e['Path']:e for e in v['Files']};names=set(before)|newsrc
 if name==ownplan:
  for fixture in ['legacy_fixture.so','legacy_bridge.so']:
   imports=v['NativeImports'][fixture];assert '__stack_chk_fail' not in imports;imports.append('__stack_chk_fail')
 rows=[]
 for n in sorted(names):
  mode=before[n]['HashMode'] if n in before else 'TextLF';raw=(c/n).read_bytes();raw=raw.replace(b'\r\n',b'\n') if mode=='TextLF' else raw;rows.append({'Path':n,'Bytes':len(raw),'Sha256':h(raw),'HashMode':mode})
 v['Files']=rows;write(c/name,v);pin=h((c/name).read_bytes());raw,count=re.subn(rb'(?<='+key.encode()+rb': )[A-F0-9]{64}',pin.encode(),(c/wf).read_bytes());assert count==1;(c/wf).write_bytes(raw);owned|={name,wf};plans.append({'Path':name,'Inputs':len(rows),'Sha256':pin})
for plan in [oldplan,ownplan]:
 assert b'\r' not in (c/plan).read_bytes()
 for e in read(c/plan)['Files']:
  raw=(c/e['Path']).read_bytes();raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
assert len(keys)==2241 and len(owned)==25 and sum(n.endswith('.cs') for n in owned)==18
for e in core['Files']:assert h((c/e['Path']).read_bytes())==e['Sha256']
row=lambda n:{'Path':n,'Bytes':(c/n).stat().st_size,'Sha256':h((c/n).read_bytes())}
write(d/'pre-format-source-resource-closure.json',{'Files':[row(n) for n in sorted(keys)]})
write(d/'pre-format-source-freeze.json',{'ParentHead':'32c394232588c275703e29be444f8d84cb5d27a4','SourcePaths':25,'CSharpPaths':18,'SourceClosurePaths':2241,'CoreActualCompiledFilesPreserved':15,'Plans':plans,'Files':[row(n) for n in sorted(owned)],'NativeImportPolicyDelta':{'legacy_fixture.so':['__stack_chk_fail'],'legacy_bridge.so':['__stack_chk_fail']},'NativeOrOriginalExecuted':False})
write(d/'pre-format-token-inputs.json',[str((c/e['Path']).resolve()) for e in obs['Files'] if e['Path'].endswith('.cs')]);print(json.dumps({'SourcePaths':25,'CSharpPaths':18,'SourceClosure':2241,'Plans':plans}))
