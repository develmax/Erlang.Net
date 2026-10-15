from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();d=r/'artifacts/root-part318-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
assert not(d/'full-guard.json').exists() and read(d/'full-exit.json')['ExitCode']==0 and read(d/'full-exit.json')['Context']=='Outside sandbox'
fp=d/'source-freeze.json';f=read(fp);cl=d/'source-resource-closure.json';assert h(fp.read_bytes())=='0368584A69E20A9A5FCB007CF6F6B2218BF3036FE5B77C2F1EFEE7DFEBC0D854'
assert h(cl.read_bytes())==f['FinalSourceClosureSha256']=='7B8C0913A6E46324BF8A40DBD8EB86105990AA6DA76B35EF9BA294E7A44D0C03'
rows=read(cl)['Files'];assert len(rows)==2272
for e in rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
current=read(r/'artifacts/tests.json');old=read(r/'docs/validation/part-317/final-main/tests.json');assert old['Passed']==6667 and current['Passed']==6668 and current['Failed']==0 and all(e['Status']=='Passed' for e in current['Tests'])
new={'host_registered_group_leader_connected_output_ownership'};assert {e['Name'] for e in current['Tests']}-{e['Name'] for e in old['Tests']}==new
assert [e['Name'] for e in current['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
integration=read(r/'artifacts/integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-317/final-main/integration-results.json')
assert 'Source layout: 0 files need formatting' in (d/'full-validation.log').read_text(encoding='utf-8-sig')
for plan in f['Plans']:
 path=r/plan['Path'];assert h(path.read_bytes())==plan['Sha256'] and b'\r' not in path.read_bytes();items=read(path)['Files'];assert len(items)==plan['Inputs']
 for e in items:
  raw=(r/e['Path']).read_bytes();raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
collector=r/'tools/Erlang.GroupLeaderOutboundObservations/source-plan.json';assert h(collector.read_bytes())==f['CollectorSourcePlanSha256']=='ACEE30ABEDDF9EF2572F50D0008699ADDE9B9F02C44470AD7C8E2AC5F58F948F'
for e in read(collector)['Sources']:
 raw=(collector.parent/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
manifest=r/'docs/supported-mfas.json';raw=manifest.read_bytes();entries=json.loads(raw);prior=json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']));key=lambda e:(e['Module'],e['Function'],e['Arity']);before={key(e):e for e in prior};assert len(entries)==len(prior)==509 and [key(e) for e in entries]==[key(e) for e in prior]
allowed={('erlang','process_info',1),('erlang','load_nif',2),('erlang','alloc_info',1),('erlang','alloc_sizes',1),('erlang','memory',0),('erlang','memory',1)};restored=[]
for e in entries:
 k=key(e);assert {n:v for n,v in e.items() if n!='Limits'}=={n:v for n,v in before[k].items() if n!='Limits'},k
 if e.get('Limits')!=before[k].get('Limits'):
  assert k in allowed,k;restored.append({'Mfa':list(k),'Raw':e.get('Limits'),'Derived':before[k].get('Limits')});e['Limits']=before[k]['Limits']
 assert e==before[k],k
group=next(e for e in entries if key(e)==('erlang','group_leader',2));assert group['Evidence']=='mfa/erlang:group_leader/2';group['Limits']='Registered guest connected-remote assignment uses owned ordered publication with capacity waits, producer cancellation and generation isolation. Disconnected/pending autoconnect and the legacy synchronous embedding setter remain incomplete. SDK controller HOST and original two-node observations are separate roles; no live SDK-peer or native wire equality is claimed.'
assert sum(e['Module']=='erlang' for e in entries)==351
(d/'raw-supported-mfas.json').write_bytes(raw);write(manifest,entries)
guard={'Status':'PASS_ACTUAL_MAIN318_6668_FULL_AND_ELEVEN_INTEGRATIONS','Passed':6668,'Failed':0,'All6667PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'IntegrationResults':11,'SourcePaths':20,'CSharpPaths':6,'SourceResourceClosureFiles':2272,'SourceFreezeSha256':h(fp.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'Registry':509,'PreviousRegistry':509,'PublicMfaDelta':0,'ErlangMfas':351,'ApiPresenceIsNotSemanticCompletion':True,'RawRegistrySha256':h(raw),'RegistryMetadataDisclosuresRestored':restored,'ConnectedGroupLeaderScopeDisclosed':True,'Plans':f['Plans'],'CollectorSourcePlanSha256':h(collector.read_bytes()),'CollectorOriginalExecutedByFullPipeline':False,'NativeFixtureExecutedByFullPipeline':False,'FullExecutionContext':'Outside sandbox','GuardSourceSha256':h(Path(__file__).read_bytes())}
write(d/'full-guard.json',guard);print(json.dumps({'Status':guard['Status'],'GuardSha256':h((d/'full-guard.json').read_bytes())}))
