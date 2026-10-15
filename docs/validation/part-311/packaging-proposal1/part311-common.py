"""Actual current-Main full/native gates; unbound proposals refuse mutation."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path.cwd().resolve();OUT=ROOT/'docs/validation/part-311';CLOSED45='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(Path(p).read_bytes());git=lambda *x:subprocess.check_output(['git',*x],cwd=ROOT)
def safe(name):
 p=(ROOT/name).resolve();assert p.is_relative_to(ROOT)
 assert not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.relative_to(ROOT).parts)
 assert not any(x.lower().startswith('license') for x in p.relative_to(ROOT).parts);return p
def pinned(pin):
 p=safe(pin['Path']);data=p.read_bytes();assert len(data)==pin['Bytes'] and sha(data)==pin['Sha256'],str(p);return p
def render(text,c):
 for name,key in [('Passed','ExpectedPassed'),('Registry','ExpectedRegistry'),('OptionalIo','ExpectedOptionalIo'),('Erlang','ExpectedErlang'),('NativeInputs','ExpectedNativeInputs'),('SourcePaths','SourcePaths'),('CSharpPaths','CSharpPaths')]:text=text.replace('{{'+name+'}}',format(c[key],','))
 assert '{{' not in text and '}}' not in text;return text
def gates(path):
 c=read(path);assert c['BoundToActualComposite'] and c['Part']==311 and not c['ReusedFullValidation']
 assert c['ParentHead']=='79bfaecaa7d89a03e2248149ae55c3b5bf03e6a8' and git('rev-parse','HEAD').decode().strip()==c['ParentHead']
 authority=read(pinned(c['CompositeAuthority']));assert authority['Part']==311 and authority['SourceFreeze']==c['SourceFreeze']
 f=read(pinned(c['SourceFreeze']));assert f['ParentHead']==c['ParentHead'] and len(f['Files'])==len({x['Path'] for x in f['Files']})==f['SourcePaths']==c['SourcePaths']
 assert sum(x['Path'].endswith('.cs') for x in f['Files'])==f['CSharpPaths']==c['CSharpPaths'] and f['TokensExactForCSharp']
 for row in f['Files']:pinned(row)
 formatting=read(pinned(c['FormatGuard']));assert formatting['CSharpPaths']==c['CSharpPaths']
 assert formatting['BeforeSha256']==formatting['AfterSha256']==c['TokensBefore']['Sha256']==c['TokensAfter']['Sha256']
 assert pinned(c['TokensBefore']).read_bytes()==pinned(c['TokensAfter']).read_bytes()
 source_cs={x['Path']:x for x in f['Files'] if x['Path'].endswith('.cs')};assert {x['Path'] for x in formatting['Files']}==set(source_cs)
 for row in formatting['Files']:assert all(row[k]==source_cs[row['Path']][k] for k in ['Bytes','Sha256'])
 for key,field in [('ExpectedPassed','ExpectedFullPassed'),('ExpectedRegistry','ExpectedRegistry'),('ExpectedErlang','ExpectedErlangPresence'),('ExpectedOptionalIo','ExpectedOptionalIoRegistry'),('ExpectedNativeInputs','ReviewedSourceInputs')]:assert c[key]==f[field]
 assert c['ExpectedRegistry']==505 and c['ExpectedOptionalIo']==507 and c['ExpectedErlang']==347 and f['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 assert read(pinned(c['FullExit']))['ExitCode']==0 and b'Validation passed' in pinned(c['FullLog']).read_bytes()
 t=read(pinned(c['Tests']));old=read(pinned(c['PreviousTests']));assert old['Passed']==6638 and old['Failed']==0 and (t['Passed'],t['Failed'])==(c['ExpectedPassed'],0)
 assert pinned(c['PreviousTests']).read_bytes()==git('show',c['ParentHead']+':docs/validation/part-310/tests.json')
 names=[x['Name'] for x in t['Tests']];prior=[x['Name'] for x in old['Tests']];assert len(names)==len(set(names))==c['ExpectedPassed'] and all(x['Status']=='Passed' for x in t['Tests'])
 assert [x for x in names if x in set(prior)]==prior and set(names)-set(prior)==set(c['AddedNames']) and len(c['AddedNames'])==c['ExpectedPassed']-6638
 assert set(c['AddedHostNames'])<=set(c['AddedNames'])
 integration=read(pinned(c['Integration']));assert integration==read(pinned(c['PreviousIntegration'])) and len(integration)==11
 assert pinned(c['PreviousIntegration']).read_bytes()==git('show',c['ParentHead']+':docs/validation/part-310/integration-results.json')
 required={'FullGuard','NativeGuard','RegistryGuard','OptionalIoGuard','Previous310Ci','Previous310Native','PortableReplayGuard'}
 assert required<=set(c['GuardAssertions'])
 for label,assertions in c['GuardAssertions'].items():
  guard=read(pinned(c[label]))
  for key,value in assertions.items():assert guard[key]==value,(label,key)
 full=read(pinned(c['FullGuard']));native=read(pinned(c['NativeGuard']))
 for guard in [full,native]:assert guard['SourceHashesExact'] and guard['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
 assert full['Passed']==c['ExpectedPassed'] and full['Failed']==0 and full['AddedNames']==c['AddedNames']
 assert native['NativeTrustedWindowsProfilePassed'] and native['ActualGeneratedCSharpRoutePassed'] and native['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 assert c['NativeGuard']['Path']=='artifacts/root-part311-main-native/guard.json'
 plan=read(pinned(c['NativeSourcePlan']));assert len(plan['Files'])==c['ExpectedNativeInputs']
 for row in plan['Files']:
  data=safe(row['Path']).read_bytes()
  if row['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  else:assert row['HashMode']=='BinaryExact'
  assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
 raw=read(pinned(c['RawRegistry']));derived=read(pinned(c['DerivedRegistry']));previous=json.loads(git('show',c['ParentHead']+':docs/supported-mfas.json'));key=lambda x:(x['Module'],x['Function'],x['Arity'])
 assert len(raw)==len(derived)==len(previous)==505 and derived==previous and sum(x['Module']=='erlang' for x in derived)==347
 assert [key(x) for x in raw]==[key(x) for x in derived] and len({key(x) for x in derived})==505
 actual_changes=[]
 for before,after in zip(raw,derived):
  if before!=after:
   assert key(before) in {('erlang','process_info',1),('erlang','load_nif',2)} and {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'}
   actual_changes.append({'Mfa':list(key(before)),'Field':'Limits','Raw':before['Limits'],'Derived':after['Limits']})
 assert actual_changes==authority['RegistryMetadataChanges']
 registry=read(pinned(c['RegistryGuard']));assert registry['RawSha256']==c['RawRegistry']['Sha256'] and registry['RestoredSha256']==c['DerivedRegistry']['Sha256'] and registry['Changes']==actual_changes
 optional=read(pinned(c['OptionalIoGuard']));assert optional['FullTestsSha256']==c['Tests']['Sha256'] and optional['DefaultExports']==505 and optional['OptionalIoExports']==507 and not optional['IndependentAdditionalExecution']
 for row in optional['Sources']:assert sha(safe(row['Path']).read_bytes())==row['Sha256']
 assert not c['GeneralNifAbiComplete'] and not c['DefaultNativeHandlerInstalled'] and not c['WholeCompatibilityComplete'] and not c['FreshOriginal311Verified']
 assert c['PublicLoadNifRegistered'] and c['ComparisonTestReport']=='validation/part-310/tests.json'
 index=read(pinned(c['EvidenceIndex']));destinations=set()
 for row in index:
  pinned({'Path':row['SourcePath'],'Bytes':row['Bytes'],'Sha256':row['Sha256']});dest=safe(row['Destination']);assert dest.is_relative_to(OUT) and dest not in destinations;destinations.add(dest)
 terminal=read(pinned(c['PreviousTerminalProofIndex']));assert terminal['Status']=='PASS_PREVIOUS310_STAGE_CI_AND_NATIVE_DETACHED_PROOFS' and terminal['Head']==c['ParentHead']
 for pin in terminal['ManifestPins']:
  for row in read(pinned(pin)):assert any(all(x[k]==row[k] for k in ['SourcePath','Destination','Bytes','Sha256']) for x in index)
 portable=read(pinned(c['PortableReferenceIndex']));replay=read(pinned(c['PortableReplayGuard']))
 assert replay['Status']=='PASS_ARCHIVE_ONLY_EVERY_FROZEN_LOGICAL_MEMBER_REPLAY' and replay['ArchiveSha256']==portable['ArchiveSha256']==c['CompositeArchive']['Sha256']
 assert replay['LogicalRecords']==portable['LogicalRecords'] and replay['UniquePhysicalMembers']==portable['UniquePhysicalMembers'] and not replay['SourceCandidateDirectoriesRead']
 assert set(portable['Sets'])==set(authority['PortableSetNames'])
 isolated=read(pinned(c['IsolatedReallocationReceipt']));assert isolated['Status']=='ISOLATED_ACTUAL_PERMANENT_TESTS_DLL_REALLOCATION_RED_GREEN_PASS' and isolated['Passed'] and isolated['ActualNativeMemoryExecuted']
 assert isolated['CompiledAuthoredInputs']==2159 and len(isolated['NewPermanentHostGroups'])==5 and isolated['GreenResult']['Passed'] and isolated['RedFailure']['Passed'] is False
 assert isolated['RedFailure']['Message']=='Actual native reallocation extent invariant failed.' and isolated['SyntheticCounterOverflowOnly'] and not isolated['ActualAllocatorOom'] and not isolated['NativeFixtureExecuted'] and not isolated['AbiCallbackOrExportChanged'] and not isolated['PublicMemoryApiExecuted']
 eof=read(pinned(c['EofLayoutMap']));assert eof['ParentHead']==c['ParentHead'] and not eof['MainEdited'] and not eof['RuntimeChanged'] and len(eof['Files'])==1
 for row in eof['Files']:
  assert sha(git('show',c['ParentHead']+':'+row['Path']))==row['ParentSha256'];current=next(x for x in f['Files'] if x['Path']==row['Path']);assert (current['Bytes'],current['Sha256'])==(row['ResultBytes'],row['ResultSha256'])
 archive_inputs=read(pinned(c['PortableInputAuthority']));binding=read(pinned(c['PortableReplayBinding']))
 assert archive_inputs['MainSourceFreeze']=={'Path':c['SourceFreeze']['Path'],'Sha256':c['SourceFreeze']['Sha256']}
 assert portable['InputAuthoritySha256']==c['PortableInputAuthority']['Sha256'] and portable['Sets']['final-main-source-resource']['FreezeSha256']==f['FinalSourceClosureSha256']
 assert binding['Status']=='PASS_ACTUAL_ARCHIVE_ONLY_REPLAY_INPUT_OUTPUT_BINDINGS' and binding['ExitCode']==0
 assert binding['PortableIndexSha256']==c['PortableReferenceIndex']['Sha256'] and binding['InputAuthoritySha256']==c['PortableInputAuthority']['Sha256'] and binding['ReplayGuardSha256']==c['PortableReplayGuard']['Sha256'] and binding['ArchiveSha256']==c['CompositeArchive']['Sha256'] and binding['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
 assert sha(safe(binding['VerifierPath']).read_bytes())==binding['VerifierSha256']
 assert authority['CounterfactualIsRealNativeMemoryOwnerAccountingRed'] and not authority['CounterfactualClaimsNativeFixtureExecution'] and len(authority['RetainedCounterfactualEvidence'])>=1
 for ref in authority['RetainedCounterfactualEvidence']:
  if 'ArchiveSet' in ref:assert any(x['Path']==ref['Path'] for x in portable['Sets'][ref['ArchiveSet']]['Records'])
  else:assert any(all(x[k]==ref[k] for k in ['SourcePath','Bytes','Sha256']) for x in index)
 assert sha((ROOT/'docs/changelog/0045.md').read_bytes())==CLOSED45
 volume=pinned(c['ActiveChangelogBefore']).read_bytes();assert len(volume)<=32768 and len(volume.splitlines())<=400
 assert b'docs/validation/part-311/** binary' in (ROOT/'.gitattributes').read_bytes()
 return c,f,index
