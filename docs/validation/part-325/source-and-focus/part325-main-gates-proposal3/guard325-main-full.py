from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--main-freeze-sha256',required=True);p.add_argument('--closure-sha256',required=True);a=p.parse_args()
r=Path.cwd();d=r/'artifacts/root-part325-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()=='56fa1196fb5507e1dce28e73d530685e7dcbc4d8'
assert not(d/'full-guard.json').exists();exitproof=read(d/'full-exit.json');assert exitproof['ExitCode']==0 and exitproof['Context']=='Outside sandbox'
fp=d/'source-freeze.json';f=read(fp);cl=d/'source-resource-closure.json';assert h(fp.read_bytes())==a.main_freeze_sha256.upper() and h(cl.read_bytes())==f['FinalSourceClosureSha256']==a.closure_sha256.upper();rows=read(cl)['Files'];assert (f['SourcePaths'],f['CSharpPaths'],len(rows))==(23,7,2366)
assert f['ExpectedFullPassed']==6693 and f['SourceClosurePaths']==2366
for e in rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
current=read(r/'artifacts/tests.json');old=read(r/'docs/validation/part-324/final-main/tests.json');assert old['Passed']==6692 and current['Passed']==6693 and current['Failed']==0 and len(current['Tests'])==6693 and all(e['Status']=='Passed' for e in current['Tests']);new={'host_guest_link_retained_ownership_and_async_distribution'};assert len(new)==1;assert {e['Name'] for e in current['Tests']}-{e['Name'] for e in old['Tests']}==new and [e['Name'] for e in current['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
integration=read(r/'artifacts/integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-324/final-main/integration-results.json');assert 'Source layout: 0 files need formatting' in (d/'full-validation.log').read_text(encoding='utf-8-sig')
assert sorted(plan['Inputs'] for plan in f['Plans'])==[1109,1134]
for plan in f['Plans']:
 path=r/plan['Path'];raw=path.read_bytes();assert h(raw)==plan['Sha256'] and b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf');items=read(path)['Files'];assert len(items)==plan['Inputs']
 for e in items:
  raw=(r/e['Path']).read_bytes();assert e['HashMode'] in ['TextLF','BinaryExact'];raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
 workflow='nif-pending-native.yml' if plan['Inputs']==1109 else 'nif-reallocation-native.yml';key='REVIEWED_SOURCE_PLAN_SHA256' if plan['Inputs']==1109 else 'REVIEWED_REALLOCATION_PLAN_SHA256';assert re.findall(r'^\s*'+key+r': ([A-F0-9]{64})$',(r/'.github/workflows'/workflow).read_text(),re.M)==[plan['Sha256']]
for name,sha in [('Erlang.GroupLeaderOutboundObservations','ACEE30ABEDDF9EF2572F50D0008699ADDE9B9F02C44470AD7C8E2AC5F58F948F'),('Erlang.GuestConnectedSendObservations','7E14E4D608DAB6DC61AC1C08CFD53183CE9979FDCA178D2E84CB19811FA45399'),('Erlang.GuestConnectedLinkObservations','5106A602867DA8CB30AF0F42E6384E543B576148D25BAF8FC4E685BDD6C76602'),('Erlang.GuestConnectedExitObservations','F40BA9404BF5BE58BE58108365AB85C6B24E9528DA870355FDFE0126DBD3576B'),('Erlang.DistributionControlWaitObservations','146C1DFA51058C54D8C72DAFEAAB24D82F5684B3EA5C9CB0CE1B35724D070705'),('Erlang.DistributionLinkRetentionObservations','A62852C3F0148AD1381F62CD470BE858C03B9502613C1761E88718E4A5CAC323')]:
 path=r/'tools'/name/'source-plan.json';assert h(path.read_bytes())==sha and b'\r' not in path.read_bytes()
 for e in read(path)['Sources']:
  raw=(path.parent/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
manifest=r/'docs/supported-mfas.json';raw=manifest.read_bytes();entries=json.loads(raw);prior=json.loads(subprocess.check_output(['git','--no-optional-locks','show','HEAD:docs/supported-mfas.json']));key=lambda e:(e['Module'],e['Function'],e['Arity']);before={key(e):e for e in prior};assert len(entries)==len(prior)==509 and [key(e) for e in entries]==[key(e) for e in prior]
oldcustom={('erlang','process_info',1),('erlang','load_nif',2),('erlang','alloc_info',1),('erlang','alloc_sizes',1),('erlang','memory',0),('erlang','memory',1),('erlang','group_leader',2)} | {('erlang',name,arity) for name,arity in [('send',2),('send',3),('send_nosuspend',2),('send_nosuspend',3),('!',2),('process_flag',2),('dist_get_stat',1)]}
linkkeys={('erlang','link',1),('erlang','link',2),('erlang','unlink',1)}
oldcustom |= linkkeys
exitkeys={('erlang','exit',2),('erlang','exit',3),('erlang','exit_signal',2),('erlang','exit_signal',3)}
oldcustom|=exitkeys
oldcustom|={('erlang','process_info',2)}
assert len(oldcustom)==22
processinfo={('erlang','process_info',2)}
restored=[]
for e in entries:
 k=key(e);assert {n:v for n,v in e.items() if n!='Limits'}=={n:v for n,v in before[k].items() if n!='Limits'},k
 if e.get('Limits')!=before[k].get('Limits'):
  assert k in oldcustom,k
  restored.append({'Mfa':list(k),'Raw':e.get('Limits'),'Derived':before[k].get('Limits')});e['Limits']=before[k]['Limits']
 assert e==before[k],k
updated=[]
linkupdatekeys={('erlang','link',1),('erlang','link',2)}
linkscope=' Already-connected guest remote PID LINK output is accepted into finite connection-owned staging before the normal caller waits. Captured-generation LINK, local relation and local receiver priority commit together after actual encoding/capacity preflight. An accepted worker retains its ticket and entry lease across producer cancellation or kill; retained terminal EXIT follows LINK in order. Normal callers cooperatively admit existing owner signals/system queries while awaiting turn or capacity. With async_dist true the caller returns after bounded acceptance without that output wait. Duplicate LINK remains idempotent, link/2 options validate before mutation, and hard staging-cap refusal raises system_limit before relation installation. SDK staging and transport have separately finite accounted-byte/packet budgets; these are SDK resource bounds, not physical native buffer sizes. Accepted UNLINK remains connection-owned and only its guest wait admits caller signals. Pending/autoconnect, remote names/aliases, synchronous embedding setters, complete inbound priority, live SDK network peers, native scheduler/queue-phase identity and general native wire equality remain incomplete. Five actual generated-guest HOST controls cover normal/async turn/capacity ownership and hard-cap refusal. The separate pinned original325 collector remains unexecuted by this pipeline.'
for e in entries:
 k=key(e)
 if k in linkupdatekeys:
  old=before[k]['Limits'];assert 'before producer-owned output waits' in old and 'async_dist LINK publication' in old and 'Source-guided follow-up 325 remains:' in old
  e['Limits']=old.split(' Already-connected guest remote PID links publish',1)[0]+linkscope;updated.append(list(k))
 elif k==('erlang','process_info',2):
  old=before[k]['Limits'];needle='LINK phase/retention follow-up325, ';assert old.count(needle)==1;e['Limits']=old.replace(needle,'');updated.append(list(k))
assert sorted(updated)==[['erlang','link',1],['erlang','link',2],['erlang','process_info',2]]
assert sum(e['Module']=='erlang' for e in entries)==351
for e in entries:
 k=key(e)
 if k not in linkupdatekeys|{('erlang','process_info',2)}:assert e==before[k],k
assert next(e for e in entries if key(e)==('erlang','process_flag',2))==before[('erlang','process_flag',2)]
assert next(e for e in entries if key(e)==('erlang','process_flag',3))==before[('erlang','process_flag',3)]
(d/'raw-supported-mfas.json').write_bytes(raw);write(manifest,entries)
guard={'Status':'PASS_ACTUAL_MAIN325_6693_FULL_AND_ELEVEN_INTEGRATIONS','Passed':6693,'Failed':0,'All6692PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'IntegrationResults':11,'SourcePaths':23,'CSharpPaths':7,'SourceResourceClosureFiles':2366,'SourceFreezeSha256':h(fp.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'Registry':509,'PreviousRegistry':509,'PublicMfaDelta':0,'ErlangMfas':351,'ApiPresenceIsNotSemanticCompletion':True,'RawRegistrySha256':h(raw),'RegistryMetadataDisclosuresRestored':restored,'TwentyTwoPriorCustomLimitsRestoredBeforeUpdate':True,'UpdatedScopeMfas':updated,'UnrelatedMfaRowsExactParent':True,'Unlink1ExactParent':True,'ProcessInfo1DefaultCatalogPreserved':True,'ProcessFlag2And3ExactParent':True,'ConnectedLinkAcceptedOwnershipAndAsyncScopeUpdated':True,'ObsoleteProcessInfo2Followup325TextRemovedOnly':True,'PendingAutoconnectFullPriorityLivePeersNativeParityStillPartial':True,'Plans':f['Plans'],'CollectorOriginalExecutedByFullPipeline':False,'NativeFixtureExecutedByFullPipeline':False,'FullExecutionContext':'Outside sandbox','GuardSourceSha256':h(Path(__file__).read_bytes())};write(d/'full-guard.json',guard);print(json.dumps({'Status':guard['Status'],'GuardSha256':h((d/'full-guard.json').read_bytes())}))
