from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--main-freeze-sha256',required=True);p.add_argument('--closure-sha256',required=True);a=p.parse_args()
r=Path.cwd();d=r/'artifacts/root-part322-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()=='a1a7fe4b8ef81e3cefdd39bc9717ab6855376b4a'
assert not(d/'full-guard.json').exists();exitproof=read(d/'full-exit.json');assert exitproof['ExitCode']==0 and exitproof['Context']=='Outside sandbox'
fp=d/'source-freeze.json';f=read(fp);cl=d/'source-resource-closure.json';assert h(fp.read_bytes())==a.main_freeze_sha256.upper() and h(cl.read_bytes())==f['FinalSourceClosureSha256']==a.closure_sha256.upper();rows=read(cl)['Files'];assert (f['SourcePaths'],f['CSharpPaths'],len(rows))==(25,10,2332)
assert f['ExpectedFullPassed']==6681 and f['SourceClosurePaths']==2332
for e in rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
current=read(r/'artifacts/tests.json');old=read(r/'docs/validation/part-321/final-main/tests.json');assert old['Passed']==6678 and current['Passed']==6681 and current['Failed']==0 and len(current['Tests'])==6681 and all(e['Status']=='Passed' for e in current['Tests']);new={'host_guest_connected_exit2_public_control','host_guest_connected_exit3_public_control','host_guest_connected_exit_profiles_tokens_and_ownership'};assert {e['Name'] for e in current['Tests']}-{e['Name'] for e in old['Tests']}==new and [e['Name'] for e in current['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
integration=read(r/'artifacts/integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-321/final-main/integration-results.json');assert 'Source layout: 0 files need formatting' in (d/'full-validation.log').read_text(encoding='utf-8-sig')
assert sorted(plan['Inputs'] for plan in f['Plans'])==[1107,1132]
for plan in f['Plans']:
 path=r/plan['Path'];raw=path.read_bytes();assert h(raw)==plan['Sha256'] and b'\r' not in raw and not raw.startswith(b'\xef\xbb\xbf');items=read(path)['Files'];assert len(items)==plan['Inputs']
 for e in items:
  raw=(r/e['Path']).read_bytes();assert e['HashMode'] in ['TextLF','BinaryExact'];raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
 workflow='nif-pending-native.yml' if plan['Inputs']==1107 else 'nif-reallocation-native.yml';key='REVIEWED_SOURCE_PLAN_SHA256' if plan['Inputs']==1107 else 'REVIEWED_REALLOCATION_PLAN_SHA256';assert re.findall(r'^\s*'+key+r': ([A-F0-9]{64})$',(r/'.github/workflows'/workflow).read_text(),re.M)==[plan['Sha256']]
for name,sha in [('Erlang.GroupLeaderOutboundObservations','ACEE30ABEDDF9EF2572F50D0008699ADDE9B9F02C44470AD7C8E2AC5F58F948F'),('Erlang.GuestConnectedSendObservations','7E14E4D608DAB6DC61AC1C08CFD53183CE9979FDCA178D2E84CB19811FA45399'),('Erlang.GuestConnectedLinkObservations','5106A602867DA8CB30AF0F42E6384E543B576148D25BAF8FC4E685BDD6C76602'),('Erlang.GuestConnectedExitObservations','F40BA9404BF5BE58BE58108365AB85C6B24E9528DA870355FDFE0126DBD3576B')]:
 path=r/'tools'/name/'source-plan.json';assert h(path.read_bytes())==sha and b'\r' not in path.read_bytes()
 for e in read(path)['Sources']:
  raw=(path.parent/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256']
manifest=r/'docs/supported-mfas.json';raw=manifest.read_bytes();entries=json.loads(raw);prior=json.loads(subprocess.check_output(['git','--no-optional-locks','show','HEAD:docs/supported-mfas.json']));key=lambda e:(e['Module'],e['Function'],e['Arity']);before={key(e):e for e in prior};assert len(entries)==len(prior)==509 and [key(e) for e in entries]==[key(e) for e in prior]
oldcustom={('erlang','process_info',1),('erlang','load_nif',2),('erlang','alloc_info',1),('erlang','alloc_sizes',1),('erlang','memory',0),('erlang','memory',1),('erlang','group_leader',2)} | {('erlang',name,arity) for name,arity in [('send',2),('send',3),('send_nosuspend',2),('send_nosuspend',3),('!',2),('process_flag',2),('dist_get_stat',1)]}
linkkeys={('erlang','link',1),('erlang','link',2),('erlang','unlink',1)}
oldcustom |= linkkeys
exitkeys={('erlang','exit',2),('erlang','exit',3),('erlang','exit_signal',2),('erlang','exit_signal',3)}
assert len(oldcustom)==17 and not oldcustom.intersection(exitkeys)
restored=[]
for e in entries:
 k=key(e);assert {n:v for n,v in e.items() if n!='Limits'}=={n:v for n,v in before[k].items() if n!='Limits'},k
 if e.get('Limits')!=before[k].get('Limits'):
  assert k in oldcustom,k
  restored.append({'Mfa':list(k),'Raw':e.get('Limits'),'Derived':before[k].get('Limits')});e['Limits']=before[k]['Limits']
 assert e==before[k],k
updated=[]
for e in entries:
 k=key(e)
 if k in exitkeys:
  assert e['Evidence']=='mfa/erlang:'+e['Function']+'/'+str(e['Arity'])
  scope=' Already-connected guest remote PID explicit exits publish captured-generation EXIT2 or token-bearing EXIT2_TT control; exit/3 and exit_signal/3 validate the complete options list and preserve duplicate priority semantics before remote mutation. Accepted async_dist output has finite staged and transport budgets; normal producer-owned output retains cancellation and generation checks. Local and remote exit aliases preserve their existing public contracts. Pending/autoconnect, remote names/aliases, a live SDK network peer and general native wire equality remain incomplete. Blocked normal output waits still do not fully admit control signals or system queries (follow-up 323); idle HOST snapshots are a separate test observation.'
  e['Limits']=before[k]['Limits']+scope;updated.append(list(k))
assert len(updated)==4 and sum(e['Module']=='erlang' for e in entries)==351
assert all(next(e for e in entries if key(e)==k)==before[k] for k in oldcustom)
assert next(e for e in entries if key(e)==('erlang','process_flag',3))==before[('erlang','process_flag',3)]
(d/'raw-supported-mfas.json').write_bytes(raw);write(manifest,entries)
guard={'Status':'PASS_ACTUAL_MAIN322_6681_FULL_AND_ELEVEN_INTEGRATIONS','Passed':6681,'Failed':0,'All6678PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'IntegrationResults':11,'SourcePaths':25,'CSharpPaths':10,'SourceResourceClosureFiles':2332,'SourceFreezeSha256':h(fp.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'Registry':509,'PreviousRegistry':509,'PublicMfaDelta':0,'ErlangMfas':351,'ApiPresenceIsNotSemanticCompletion':True,'RawRegistrySha256':h(raw),'RegistryMetadataDisclosuresRestored':restored,'ConnectedExplicitExitScopeUpdated':updated,'SeventeenPriorCustomLimitsExact':True,'ProcessFlag3ExactParent':True,'ExistingGroupLeaderLinkAndAsyncLimitsPreserved':True,'Plans':f['Plans'],'CollectorOriginalExecutedByFullPipeline':False,'NativeFixtureExecutedByFullPipeline':False,'BlockedWait323RemainsIncomplete':True,'FullExecutionContext':'Outside sandbox','GuardSourceSha256':h(Path(__file__).read_bytes())};write(d/'full-guard.json',guard);print(json.dumps({'Status':guard['Status'],'GuardSha256':h((d/'full-guard.json').read_bytes())}))
