from pathlib import Path
import json,hashlib,subprocess
r=Path.cwd();p=r/'artifacts/root-part316-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,x:p.write_bytes((json.dumps(x,indent=2)+'\n').encode())
assert not (p/'full-guard.json').exists();assert read(p/'full-exit.json')['ExitCode']==0
fp=p/'source-freeze.json';f=read(fp);cl=p/'source-resource-closure.json';assert h(fp.read_bytes())=='5561E5E2095E247B50AC60194D5FAABCCE18DD5DE7A4935EEEC657C4A6FFB8D2';assert h(cl.read_bytes())==f['FinalSourceClosureSha256']=='87CC2B5E1C8DDEF229E95DA38875431E73BA715D6EE71275F7DAEA5755E00299'
rows=read(cl)['Files'];assert len(rows)==2241
for e in rows:
 b=(r/e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256'],e['Path']
tests=read(r/'artifacts/tests.json');old=read(r/'docs/validation/part-315/final-main/tests.json');assert old['Passed']==6642 and tests['Passed']==6654 and tests['Failed']==0 and all(e['Status']=='Passed' for e in tests['Tests'])
new={'mfa/erlang:alloc_info/1','mfa/erlang:alloc_sizes/1','runtime/public-allocator-sys-clr-profile-and-complete-validation'}|{'oracle/compiled-module/'+e['Name'] for e in read(r/'artifacts/allocator-public-sys-profile316-oracle1/case-manifest.json')['ComparableCases']};assert len(new)==12
assert {e['Name'] for e in tests['Tests']} - {e['Name'] for e in old['Tests']}==new
assert [e['Name'] for e in tests['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
integration=read(r/'artifacts/integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-315/final-main/integration-results.json')
assert 'Source layout: 0 files need formatting' in (p/'full-validation.log').read_text(encoding='utf-8-sig')
for contract in f['Plans']:
 plan=r/contract['Path'];assert h(plan.read_bytes())==contract['Sha256'] and b'\r' not in plan.read_bytes();items=read(plan)['Files'];assert len(items)==contract['Inputs']
 for e in items:
  b=(r/e['Path']).read_bytes();b=b.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else b;assert len(b)==e['Bytes'] and h(b)==e['Sha256'],e['Path']
manifest=r/'docs/supported-mfas.json';raw=manifest.read_bytes();current=json.loads(raw);previous=json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']));key=lambda e:(e['Module'],e['Function'],e['Arity']);prior={key(e):e for e in previous};added={('erlang','alloc_info',1),('erlang','alloc_sizes',1)};assert len(current)==507 and len(previous)==505 and {key(e) for e in current}-prior.keys()==added
assert [key(e) for e in current if key(e) not in added]==[key(e) for e in previous]
restored=[]
for e in current:
 k=key(e)
 if k in added:
  assert e['Evidence']=='mfa/erlang:'+k[1]+'/1'
  e['Limits']='CLR sys_alloc metadata only; m=dotnet_native_memory is a declared backend adaptation. Recognized unsupported allocators raise notsup. Full OTP allocator/carrier sizes and disabled-backend profiles remain unsupported.'
  continue
 if k in {('erlang','process_info',1),('erlang','load_nif',2)}:
  assert {n:v for n,v in e.items() if n!='Limits'}=={n:v for n,v in prior[k].items() if n!='Limits'};restored.append({'Mfa':list(k),'Raw':e['Limits'],'Derived':prior[k]['Limits']});e['Limits']=prior[k]['Limits']
 assert e==prior[k],k
assert len(restored)==2 and sum(e['Module']=='erlang' for e in current)==349
(p/'raw-supported-mfas.json').write_bytes(raw);write(manifest,current)
g={'Status':'PASS_ACTUAL_MAIN316_6654_FULL_AND_ELEVEN_INTEGRATIONS','Passed':6654,'Failed':0,'All6642PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'IntegrationResults':11,'SourcePaths':25,'CSharpPaths':18,'SourceResourceClosureFiles':2241,'SourceFreezeSha256':h(fp.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'Registry':507,'PreviousRegistry':505,'PublicMfaDelta':2,'ErlangMfas':349,'AddedMfas':[list(k) for k in sorted(added)],'RawRegistrySha256':h(raw),'RegistryMetadataDisclosuresRestored':restored,'NewAllocatorLimitsAreExplicitClrProfileDisclosures':True,'Plans':f['Plans'],'NewOriginalCasesExecutedByFullPipeline':False,'NativeFixtureExecutedByFullPipeline':False,'GuardSourceSha256':h(Path(__file__).read_bytes())};write(p/'full-guard.json',g);print(json.dumps({'Status':g['Status'],'GuardSha256':h((p/'full-guard.json').read_bytes())}))
