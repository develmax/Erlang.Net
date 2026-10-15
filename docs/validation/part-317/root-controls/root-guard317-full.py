from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();d=r/'artifacts/root-part317-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
assert not(d/'full-guard.json').exists() and read(d/'full-exit.json')['ExitCode']==0
assert read(d/'full-exit.json')['Context']=='Outside sandbox'
first=read(d/'tests-first-failed.json');assert first['Passed']==6651 and first['Failed']==16
failed=[e for e in first['Tests'] if e['Status']!='Passed'];assert len(failed)==16 and all(e['Name'].startswith('oracle/compiled-module/erlang-port-native-option-') and 'got {error,error,eacces}' in e['Error'] for e in failed)
first_mfas=[e for e in first['Tests'] if e['Name'].startswith('mfa/')];assert len(first_mfas)==509 and all(e['Status']=='Passed' for e in first_mfas)
assert len(read(d/'raw-supported-mfas-first-failed.json'))==507
probe=r/'artifacts/root-part317-pipe-diagnostic';restricted=read(probe/'default-result.json');unrestricted=read(probe/'outside-sandbox-result.json')
assert restricted['RuntimeSha256']==unrestricted['RuntimeSha256'] and len(restricted['Results'])==len(unrestricted['Results'])==2
assert all(not e['Passed'] and e['NativeErrorCode']==5 for e in restricted['Results']) and all(e['Passed'] for e in unrestricted['Results'])
fp=d/'source-freeze.json';f=read(fp);cl=d/'source-resource-closure.json';assert f['AuthorFinalFreeze']['Sha256']=='BA75B3175ED3F773CC0B4B1B64DFD93677E1AEFBADE7C9FF3CC20FFE819610DB'
assert h(cl.read_bytes())==f['FinalSourceClosureSha256']=='A39996D476F7B7D19F1CC7D641E3C2916A2AD66CD4E16C75E0222FEFB099FD26'
rows=read(cl)['Files'];assert len(rows)==2260
for e in rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
current=read(r/'artifacts/tests.json');old=read(r/'docs/validation/part-316/final-main/tests.json')
assert old['Passed']==6654 and current['Passed']==6667 and current['Failed']==0 and all(e['Status']=='Passed' for e in current['Tests'])
new={'mfa/erlang:memory/0','mfa/erlang:memory/1','host/clr-observed-payload-memory'}|{'oracle/compiled-module/'+e['Name'] for e in read(r/'artifacts/clr-memory-public317-oracle1/case-manifest.json')['ComparableCases']}
assert len(new)==13 and {e['Name'] for e in current['Tests']}-{e['Name'] for e in old['Tests']}==new
assert [e['Name'] for e in current['Tests'] if e['Name'] not in new]==[e['Name'] for e in old['Tests']]
integration=read(r/'artifacts/integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-316/final-main/integration-results.json')
assert 'Source layout: 0 files need formatting' in (d/'full-validation.log').read_text(encoding='utf-8-sig')
for plan in f['Plans']:
 path=r/plan['Path'];assert h(path.read_bytes())==plan['Sha256'] and b'\r' not in path.read_bytes();items=read(path)['Files'];assert len(items)==plan['Inputs']
 for e in items:
  raw=(r/e['Path']).read_bytes();raw=raw.replace(b'\r\n',b'\n') if e['HashMode']=='TextLF' else raw;assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
manifest=r/'docs/supported-mfas.json';raw=manifest.read_bytes();entries=json.loads(raw);prior=json.loads(subprocess.check_output(['git','show','HEAD:docs/supported-mfas.json']));key=lambda e:(e['Module'],e['Function'],e['Arity']);before={key(e):e for e in prior};added={('erlang','memory',0),('erlang','memory',1)}
assert len(entries)==509 and len(prior)==507 and {key(e) for e in entries}-before.keys()==added
assert [key(e) for e in entries if key(e) not in added]==[key(e) for e in prior]
restored=[]
for e in entries:
 k=key(e)
 if k in added:
  assert e['Evidence']=='mfa/erlang:memory/'+str(k[2])
  e['Limits']='Measured CLR SDK-observed payload profile within the loaded SDK type universe: real backing identities, process slot/mailbox payloads and joined native backing-request bytes. ETS has no storage implementation. Excludes CLR headers, unrelated application heaps, JIT, OS footprint and ERTS allocator/physical byte parity; captures are not atomic across sources.'
 else:
  assert {n:v for n,v in e.items() if n!='Limits'}=={n:v for n,v in before[k].items() if n!='Limits'},k
  if e.get('Limits')!=before[k].get('Limits'):
   assert k in {('erlang','process_info',1),('erlang','load_nif',2),('erlang','alloc_info',1),('erlang','alloc_sizes',1)},k
   restored.append({'Mfa':list(k),'Raw':e.get('Limits'),'Derived':before[k].get('Limits')});e['Limits']=before[k]['Limits']
  assert e==before[k],k
assert sum(e['Module']=='erlang' for e in entries)==351
(d/'raw-supported-mfas.json').write_bytes(raw);write(manifest,entries)
guard={'Status':'PASS_ACTUAL_MAIN317_6667_FULL_AND_ELEVEN_INTEGRATIONS','Passed':6667,'Failed':0,'All6654PreviousOrderedNamesPreserved':True,'AddedTestNames':sorted(new),'IntegrationResults':11,'SourcePaths':38,'CSharpPaths':33,'SourceResourceClosureFiles':2260,'SourceFreezeSha256':h(fp.read_bytes()),'SourceClosureSha256':h(cl.read_bytes()),'Registry':509,'PreviousRegistry':507,'PublicMfaDelta':2,'ErlangMfas':351,'ApiPresenceIsNotSemanticCompletion':True,'RawRegistrySha256':h(raw),'RegistryMetadataDisclosuresRestored':restored,'Plans':f['Plans'],'NewOriginalCasesExecutedByFullPipeline':False,'NativeFixtureExecutedByFullPipeline':False,'GuardSourceSha256':h(Path(__file__).read_bytes())}
guard.update(FirstRestrictedFullPassed=6651,FirstRestrictedFullFailed=16,FirstFailureNames=[e['Name'] for e in failed],SameDllNamedPipeRestrictionVerified=True,FullExecutionContext='Outside sandbox',ProductionSourceOrExpectedResultsChangedForRerun=False,FirstFailedReportSha256=h((d/'tests-first-failed.json').read_bytes()),RestrictedProbeSha256=h((probe/'default-result.json').read_bytes()),UnrestrictedProbeSha256=h((probe/'outside-sandbox-result.json').read_bytes()))
guard.update(FirstRestrictedDirectMfasPassed=509,FirstFailedSavedRegistryWasInherited316Count=507,FirstFailureDidNotRegenerateDerivedRegistry=True)
write(d/'full-guard.json',guard);print(json.dumps({'Status':guard['Status'],'GuardSha256':h((d/'full-guard.json').read_bytes())}))
