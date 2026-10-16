from pathlib import Path
import argparse,hashlib,json,re,subprocess
p=argparse.ArgumentParser();p.add_argument('--main-freeze-sha256',required=True);p.add_argument('--closure-sha256',required=True);a=p.parse_args()
r=Path.cwd();d=r/'artifacts/root-part326-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()=='2bd148150ddf2cca49dd2c797d05de3260bf663c'
assert not(d/'full-guard.json').exists();exitproof=read(d/'full-exit.json');assert exitproof['ExitCode']==0 and exitproof['Context']=='Outside sandbox'
fp=d/'source-freeze.json';f=read(fp);cl=d/'source-resource-closure.json';assert h(fp.read_bytes())==a.main_freeze_sha256.upper() and h(cl.read_bytes())==f['FinalSourceClosureSha256']==a.closure_sha256.upper();rows=read(cl)['Files'];assert (f['SourcePaths'],f['CSharpPaths'],len(rows))==(13,8,2370)
assert f['ExpectedFullPassed']==6708 and f['SourceClosurePaths']==2370
for e in rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
current=read(r/'artifacts/tests.json');old=read(r/'docs/validation/part-325/final-main/tests.json');assert old['Passed']==6693 and current['Passed']==6708 and current['Failed']==0 and len(current['Tests'])==6708 and all(e['Status']=='Passed' for e in current['Tests']);new=set(['oracle/compiled-module/spawn-async-dist-fun-first-instruction-true', 'oracle/compiled-module/spawn-async-dist-local-node-fun-first-instruction-true', 'oracle/compiled-module/spawn-async-dist-mfa-first-instruction-true', 'oracle/compiled-module/spawn-async-dist-local-node-mfa-first-instruction-true', 'oracle/compiled-module/spawn-async-dist-false-and-last-option-wins', 'oracle/compiled-module/spawn-async-dist-parent-true-default-child-and-grandchild-false', 'oracle/compiled-module/spawn-async-dist-true-with-link-monitor-and-normal-exit', 'oracle/compiled-module/spawn-async-dist-request-fun-first-instruction-true', 'oracle/compiled-module/spawn-async-dist-request-local-node-mfa-first-instruction-true', 'oracle/compiled-module/spawn-async-dist-request-last-option-false-and-custom-reply-tag', 'oracle/compiled-module/spawn-async-dist-invalid-values-and-shapes-never-create-child', 'oracle/compiled-module/spawn-async-dist-request-invalid-stays-badopt-before-and-after-valid', 'oracle/compiled-module/spawn-async-dist-disconnected-request-defers-creator-options', 'oracle/compiled-module/spawn-async-dist-argument-failure-before-option-validation', 'host_spawn_async_dist_first_instruction_validation_and_dead_parent']);assert len(new)==15;assert {e['Name'] for e in current['Tests']}-{e['Name'] for e in old['Tests']}==new and [e['Name'] for e in current['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
integration=read(r/'artifacts/integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-325/final-main/integration-results.json');assert 'Source layout: 0 files need formatting' in (d/'full-validation.log').read_text(encoding='utf-8-sig')
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
optkeys={('erlang','spawn_opt',arity) for arity in (2,3,4,5)}
requestkeys={('erlang','spawn_request',arity) for arity in (2,3,4,5)}
optscope=' Explicit {async_dist,true|false} is supported by the existing local fun/MFA and local-node spawn_opt routes. Repeated valid options use the last value; invalid values or shapes reject before child creation through existing wrapper error handling. The child flag is initialized before process publication, dispatch and sequence-spawn callbacks. Its default remains false and it does not inherit the parent flag. Existing monitor/link, argument validation and disconnected crasher adaptation remain bounded existing behavior. This does not implement +pad VM defaults, other unsupported priority/allocator spawn options, connected remote creation, pending/autoconnect, full native scheduling or general remote protocol parity. Actual SDK validation covers first child observation, repeated/invalid options, default noninheritance and existing lifecycle; current original326 execution remains pending.'
requestscope=' In existing successful local option-bearing spawn_request overloads, explicit {async_dist,true|false} initializes the child before process publication, dispatch and sequence-spawn callbacks. Repeated valid values use the last option; invalid local creator values retain existing badopt/reply handling. Forms without an options argument keep default false and do not inherit the parent flag. Request/reply mechanics remain separate from the child async_dist flag; existing reply tags, monitor/link options and wrapper validation ordering are preserved. Disconnected requests still defer creator-option validation and preserve sender reply/link/monitor validation, without claiming a real remote child. This does not implement +pad defaults, connected remote spawning, unsupported priority/allocator options, pending/autoconnect or full native protocol/scheduler parity; current original326 execution remains pending.'
for e in entries:
 k=key(e)
 if k in optkeys:e['Limits']=before[k]['Limits']+optscope;updated.append(list(k))
 elif k in requestkeys:e['Limits']=before[k]['Limits']+requestscope;updated.append(list(k))
assert sorted(updated)==sorted([list(k) for k in optkeys|requestkeys]) and len(updated)==8
for e in entries:
 if key(e) not in optkeys|requestkeys:assert e==before[key(e)],key(e)
assert all(next(e for e in entries if key(e)==k)==before[k] for k in oldcustom)
assert next(e for e in entries if key(e)==('erlang','spawn_request',1))==before[('erlang','spawn_request',1)]
assert sum(e['Module']=='erlang' for e in entries)==351
(d/'raw-supported-mfas.json').write_bytes(raw);write(manifest,entries)
guard={'Status':'PASS_ACTUAL_MAIN326_6708_FULL_AND_ELEVEN_INTEGRATIONS','Passed':6708,'Failed':0,'All6693PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'IntegrationResults':11,'SourcePaths':13,'CSharpPaths':8,'SourceResourceClosureFiles':2370,'SourceFreezeSha256':h(fp.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'Registry':509,'PreviousRegistry':509,'PublicMfaDelta':0,'ErlangMfas':351,'ApiPresenceIsNotSemanticCompletion':True,'RawRegistrySha256':h(raw),'RegistryMetadataDisclosuresRestored':restored,'TwentyTwoPriorCustomLimitsExact':True,'UpdatedScopeMfas':updated,'SpawnRequest1ExactParent':True,'UnrelatedMfaRowsExactParent':True,'ExplicitLocalSpawnAsyncDistScopeOnly':True,'DefaultFalseNoninheritanceAndPrepublicationInitialization':True,'DisconnectedRequestCreatorValidationRemainsDeferred':True,'PadPriorityRemoteAndAutoconnectRemainPartial':True,'Plans':f['Plans'],'CollectorOriginalExecutedByFullPipeline':False,'NativeFixtureExecutedByFullPipeline':False,'FullExecutionContext':'Outside sandbox','GuardSourceSha256':h(Path(__file__).read_bytes())};write(d/'full-guard.json',guard);print(json.dumps({'Status':guard['Status'],'GuardSha256':h((d/'full-guard.json').read_bytes())}))
