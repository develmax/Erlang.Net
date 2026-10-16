from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--main-freeze-sha256',required=True);p.add_argument('--closure-sha256',required=True);a=p.parse_args()
r=Path.cwd();d=r/'artifacts/root-part321-main2';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
assert not(d/'full-guard.json').exists();exitproof=read(d/'full-exit.json');assert exitproof['ExitCode']==0 and exitproof['Context']=='Outside sandbox'
fp=d/'source-freeze.json';f=read(fp);cl=d/'source-resource-closure.json';assert h(fp.read_bytes())==a.main_freeze_sha256.upper() and h(cl.read_bytes())==f['FinalSourceClosureSha256']==a.closure_sha256.upper();rows=read(cl)['Files'];assert (f['SourcePaths'],f['CSharpPaths'],len(rows))==(24,9,2316)
assert f['ExpectedFullPassed']==6678 and f['SourceClosurePaths']==2316
assert f['PreviousFailedMain1']['Sha256']=='DB948C4DC131A6AE35B542FAB720A5D67E770DDFCD1B853C256C53BBE714DFE4'
assert h((r/f['PreviousFailedMain1']['Path']).read_bytes())==f['PreviousFailedMain1']['Sha256']
for e in rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
current=read(r/'artifacts/tests.json');old=read(r/'docs/validation/part-320/final-main/tests.json');assert old['Passed']==6675 and current['Passed']==6678 and current['Failed']==0 and len(current['Tests'])==6678 and all(e['Status']=='Passed' for e in current['Tests']);new={'host_guest_connected_link_public_control','host_guest_connected_unlink_inbound_public_control','host_guest_connected_link_unlink_phases_ack_and_ownership'};assert {e['Name'] for e in current['Tests']}-{e['Name'] for e in old['Tests']}==new and [e['Name'] for e in current['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
integration=read(r/'artifacts/integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-320/final-main/integration-results.json');assert 'Source layout: 0 files need formatting' in (d/'full-validation.log').read_text(encoding='utf-8-sig')
assert sorted(plan['Inputs'] for plan in f['Plans'])==[1103,1128]
for plan in f['Plans']:
 path=r/plan['Path'];raw=path.read_bytes();assert h(raw)==plan['Sha256'] and b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf');items=read(path)['Files'];assert len(items)==plan['Inputs']
 for e in items:
  raw=(r/e['Path']).read_bytes();assert e['HashMode'] in ['TextLF','BinaryExact'];raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
 workflow='nif-pending-native.yml' if plan['Inputs']==1103 else 'nif-reallocation-native.yml';key='REVIEWED_SOURCE_PLAN_SHA256' if plan['Inputs']==1103 else 'REVIEWED_REALLOCATION_PLAN_SHA256';assert re.findall(r'^\s*'+key+r': ([A-F0-9]{64})$',(r/'.github/workflows'/workflow).read_text(),re.M)==[plan['Sha256']]
for name,sha in [('Erlang.GroupLeaderOutboundObservations','ACEE30ABEDDF9EF2572F50D0008699ADDE9B9F02C44470AD7C8E2AC5F58F948F'),('Erlang.GuestConnectedSendObservations','7E14E4D608DAB6DC61AC1C08CFD53183CE9979FDCA178D2E84CB19811FA45399'),('Erlang.GuestConnectedLinkObservations','5106A602867DA8CB30AF0F42E6384E543B576148D25BAF8FC4E685BDD6C76602')]:
 path=r/'tools'/name/'source-plan.json';assert h(path.read_bytes())==sha and b'\r' not in path.read_bytes()
 for e in read(path)['Sources']:
  raw=(path.parent/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
manifest=r/'docs/supported-mfas.json';raw=manifest.read_bytes();entries=json.loads(raw);prior=json.loads(subprocess.check_output(['git','--no-optional-locks','show','HEAD:docs/supported-mfas.json']));key=lambda e:(e['Module'],e['Function'],e['Arity']);before={key(e):e for e in prior};assert len(entries)==len(prior)==509 and [key(e) for e in entries]==[key(e) for e in prior]
oldcustom={('erlang','process_info',1),('erlang','load_nif',2),('erlang','alloc_info',1),('erlang','alloc_sizes',1),('erlang','memory',0),('erlang','memory',1),('erlang','group_leader',2)} | {('erlang',name,arity) for name,arity in [('send',2),('send',3),('send_nosuspend',2),('send_nosuspend',3),('!',2),('process_flag',2),('dist_get_stat',1)]}
linkkeys={('erlang','link',1),('erlang','link',2),('erlang','unlink',1)};assert len(oldcustom)==14 and not oldcustom.intersection(linkkeys);restored=[]
for e in entries:
 k=key(e);assert {n:v for n,v in e.items() if n!='Limits'}=={n:v for n,v in before[k].items() if n!='Limits'},k
 if e.get('Limits')!=before[k].get('Limits'):
  assert k in oldcustom|linkkeys,k;restored.append({'Mfa':list(k),'Raw':e.get('Limits'),'Derived':before[k].get('Limits')});e['Limits']=before[k]['Limits']
 assert e==before[k],k
updated=[]
for e in entries:
 k=key(e)
 if k in linkkeys:
  assert e['Evidence']=='mfa/erlang:'+e['Function']+'/'+str(e['Arity'])
  if k[1]=='link':
   scope=' Already-connected guest remote PID links publish actual captured-generation LINK control. The local relation and local receiver priority exist before producer-owned output waits; producer cancellation joins retained terminal EXIT ownership. Duplicate LINK is idempotent and link/2 priority options are validated before mutation. Pending/autoconnect, async_dist LINK publication, remote names/aliases, complete priority inbound and live SDK-peer/native wire equality remain incomplete.'
  else:
   scope=' Already-connected guest remote PID unlink suppresses the local relation before a captured-generation UNLINK_ID is accepted by the connection-owned worker. Accepted unlink output survives producer death and joins global output drain; ACK and stale EXIT identities preserve relink state. Duplicate unlink is idempotent; pending/autoconnect, a live SDK network peer and native wire equality remain incomplete.'
  e['Limits']=before[k]['Limits']+scope;updated.append(list(k))
assert len(updated)==3 and sum(e['Module']=='erlang' for e in entries)==351
assert all(next(e for e in entries if key(e)==k)==before[k] for k in oldcustom)
assert next(e for e in entries if key(e)==('erlang','process_flag',3))==before[('erlang','process_flag',3)]
(d/'raw-supported-mfas.json').write_bytes(raw);write(manifest,entries)
guard={'Status':'PASS_ACTUAL_MAIN321_6678_FULL_AND_ELEVEN_INTEGRATIONS','Passed':6678,'Failed':0,'All6675PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'IntegrationResults':11,'SourcePaths':24,'CSharpPaths':9,'SourceResourceClosureFiles':2316,'SourceFreezeSha256':h(fp.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'Registry':509,'PreviousRegistry':509,'PublicMfaDelta':0,'ErlangMfas':351,'ApiPresenceIsNotSemanticCompletion':True,'RawRegistrySha256':h(raw),'RegistryMetadataDisclosuresRestored':restored,'ConnectedLinkUnlinkScopeUpdated':updated,'FourteenPriorCustomLimitsExact':True,'ProcessFlag3ExactParent':True,'ExistingGroupLeader318AndAsync320LimitsPreserved':True,'Plans':f['Plans'],'CollectorOriginalExecutedByFullPipeline':False,'NativeFixtureExecutedByFullPipeline':False,'PreviousFailedMain1':f['PreviousFailedMain1'],'FirstFull6677FailureNotRelabeled':True,'FullExecutionContext':'Outside sandbox','GuardSourceSha256':h(Path(__file__).read_bytes())};write(d/'full-guard.json',guard);print(json.dumps({'Status':guard['Status'],'GuardSha256':h((d/'full-guard.json').read_bytes())}))
