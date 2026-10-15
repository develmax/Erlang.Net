"""Unbound template: root must supply a separately pinned actual composite authority."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path.cwd().resolve();OUT=ROOT/'docs/validation/part-308';CLOSED45='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(Path(p).read_bytes());git=lambda *x:subprocess.check_output(['git',*x],cwd=ROOT)
def safe(name):
 p=(ROOT/name).resolve();assert p.is_relative_to(ROOT)
 assert not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.relative_to(ROOT).parts)
 assert not any(x.lower().startswith('license') for x in p.relative_to(ROOT).parts)
 return p
def pinned(pin):
 p=safe(pin['Path']);data=p.read_bytes();assert len(data)==pin['Bytes'] and sha(data)==pin['Sha256'],str(p);return p
def render(text,c):
 for name,key in [('Passed','ExpectedPassed'),('Registry','ExpectedRegistry'),('OptionalIo','ExpectedOptionalIo'),('Erlang','ExpectedErlang'),('NativeInputs','ExpectedNativeInputs'),('SourcePaths','SourcePaths'),('CSharpPaths','CSharpPaths')]:text=text.replace('{{'+name+'}}',format(c[key],','))
 assert '{{' not in text and '}}' not in text;return text
def gates(path):
 c=read(path);assert c['BoundToActualComposite'] and c['Part']==308
 assert c['ParentHead']=='8b83560372b63b4760604ac088802fb74ac1c112' and git('rev-parse','HEAD').decode().strip()==c['ParentHead']
 authority=read(pinned(c['CompositeAuthority']));assert authority['SourceFreeze']==c['SourceFreeze']
 assert c['SourceFreeze']['Sha256']=='C1A2377E970ECDE5E2AF5C298D4EBC32BD0E45E59FD6F9EFDC2EB57BE6F5492D'
 f=read(pinned(c['SourceFreeze']));assert f['ParentHead']==c['ParentHead'] and len(f['Files'])==f['SourcePaths']==c['SourcePaths']
 assert len({x['Path'] for x in f['Files']})==len(f['Files']) and sum(x['Path'].endswith('.cs') for x in f['Files'])==f['CSharpPaths']==c['CSharpPaths'] and f['TokensExactForCSharp']
 for row in f['Files']:pinned(row)
 assert f['ExpectedFullPassed']==c['ExpectedPassed'] and f['ExpectedRegistry']==c['ExpectedRegistry'] and f['ExpectedErlangPresence']==c['ExpectedErlang']
 assert f['ReviewedSourceInputs']==c['ExpectedNativeInputs'] and f['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 assert read(pinned(c['FullExit']))['ExitCode']==0 and b'Validation passed' in pinned(c['FullLog']).read_bytes()
 t=read(pinned(c['Tests']));old=read(pinned(c['PreviousTests']));assert old['Passed']==6627 and (t['Passed'],t['Failed'])==(c['ExpectedPassed'],0)
 names=[x['Name'] for x in t['Tests']];prior=[x['Name'] for x in old['Tests']];assert len(names)==len(set(names))==c['ExpectedPassed'] and all(x['Status']=='Passed' for x in t['Tests'])
 assert [x for x in names if x in set(prior)]==prior and set(names)-set(prior)==set(c['AddedNames']) and len(c['AddedNames'])==c['ExpectedPassed']-6627
 assert read(pinned(c['Integration']))==read(pinned(c['PreviousIntegration'])) and len(read(pinned(c['Integration'])))==11
 for label,assertions in c['GuardAssertions'].items():
  guard=read(pinned(c[label]))
  for key,value in assertions.items():assert guard[key]==value,(label,key)
 assert {'FullGuard','NativeGuard','RegistryGuard','OptionalIoGuard','Previous307Ci','Previous307Native','Previous307Ordinary'}<=set(c['GuardAssertions'])
 for label in ['FullGuard','NativeGuard']:
  guard=read(pinned(c[label]));assert guard['SourceHashesExact'] and guard['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
 assert read(pinned(c['FullGuard']))['AddedNames']==c['AddedNames'] and set(c['AddedHostNames'])<=set(c['AddedNames'])
 assert f['PublicLoadNifRegistered'] and not f['DefaultNativeLoadInstructionHandlerInstalled'] and not f['GeneralNifAbiComplete']
 assert read(pinned(c['NativeGuard']))['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 plan=read(pinned(c['NativeSourcePlan']));assert len(plan['Files'])==c['ExpectedNativeInputs']
 for row in plan['Files']:
  data=safe(row['Path']).read_bytes()
  if row['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  else:assert row['HashMode']=='BinaryExact'
  assert len(data)==row['Bytes'] and sha(data)==row['Sha256'],row['Path']
 raw=read(pinned(c['RawRegistry']));derived=read(pinned(c['DerivedRegistry']));previous=json.loads(git('show',c['ParentHead']+':docs/supported-mfas.json'));key=lambda x:(x['Module'],x['Function'],x['Arity'])
 assert len(raw)==len(derived)==c['ExpectedRegistry'] and len(previous)==504 and sum(x['Module']=='erlang' for x in derived)==c['ExpectedErlang']
 assert [key(x) for x in raw]==[key(x) for x in derived] and len({key(x) for x in derived})==len(derived)
 assert [key(x) for x in derived if key(x) in {key(y) for y in previous}]==[key(x) for x in previous]
 assert {key(x) for x in derived}-{key(x) for x in previous}=={tuple(x) for x in authority['NewPublicMfas']}
 for row in previous:assert row==next(x for x in derived if key(x)==key(row))
 for before,after in zip(raw,derived):
  assert before==after or (key(before) in {('erlang','process_info',1),('erlang','load_nif',2)} and {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'})
 registry=read(pinned(c['RegistryGuard']));assert registry['RawSha256']==c['RawRegistry']['Sha256'] and registry['RestoredSha256']==c['DerivedRegistry']['Sha256']
 changes=registry['Changes'];assert len(changes)==2 and {tuple(x['Mfa']) for x in changes}=={('erlang','process_info',1),('erlang','load_nif',2)}
 for change in changes:
  assert change['Field']=='Limits';mfa=tuple(change['Mfa']);assert change['Raw']==next(x['Limits'] for x in raw if key(x)==mfa) and change['Derived']==next(x['Limits'] for x in derived if key(x)==mfa)
 expected_note='Exact pinned indirect BIF only: apply/3 and external-fun invocation return the original bad_lib error. Successful literal native loading, the default instruction handler and general native ABI remain incomplete.'
 assert next(x['Limits'] for x in derived if key(x)==('erlang','load_nif',2))==expected_note
 assert registry['PublicLoadNifRegistered'] and not registry['DefaultNativeLoadInstructionHandlerInstalled'] and not registry['GeneralNifAbiComplete']
 optional=read(pinned(c['OptionalIoGuard']));assert optional['FullTestsSha256']==c['Tests']['Sha256'] and optional['DefaultExports']==c['ExpectedRegistry'] and optional['OptionalIoExports']==c['ExpectedOptionalIo'] and not optional['IndependentAdditionalExecution']
 for row in optional['Sources']:assert sha(safe(row['Path']).read_bytes())==row['Sha256']
 assert sha((ROOT/'docs/changelog/0045.md').read_bytes())==CLOSED45
 volume=pinned(c['ActiveChangelogBefore']).read_bytes();assert len(volume)<=32768 and len(volume.splitlines())<=400
 assert b'docs/validation/part-308/** binary' in (ROOT/'.gitattributes').read_bytes()
 index=read(pinned(c['EvidenceIndex']));destinations=set()
 for row in index:
  pinned({'Path':row['SourcePath'],'Bytes':row['Bytes'],'Sha256':row['Sha256']});dest=safe(row['Destination']);assert dest.is_relative_to(OUT) and dest not in destinations;destinations.add(dest)
 for kind,pin in c['Previous307Manifests'].items():
  assert kind in ['ci','native','original']
  for row in read(pinned(pin)):assert any(all(x[k]==row[k] for k in ['SourcePath','Destination','Bytes','Sha256']) for x in index)
 assert set(c['Previous307Manifests'])=={'ci','native','original'}
 portable=read(pinned(c['PortableReplayGuard']));assert portable['Status']=='PASS_INDEPENDENT_COMPLETE_COMPOSITE_AND_CANDIDATE_ARCHIVE_REPLAY' and portable['CompositeMembersVerified']==2189 and portable['CandidateLogicalFilesVerified']==1032
 assert portable['CompositeFreezeSha256']=='E0B165B9DEB90664D3C60D053E3851E5A2EFA47D1B67441C9559309591772455' and portable['ArchiveSha256']==c['CompositeArchive']['Sha256']
 assert portable['CandidateReferenceIndexSha256']==c['PortableReferenceIndex']['Sha256'] and portable['CompiledClosureSha256']==c['PortableCompiledClosure']['Sha256']
 assert c['ComparisonTestReport']=='validation/part-307/tests.json'
 assert not c['GeneralNifAbiComplete'] and not c['DefaultNativeHandlerInstalled'] and not c['WholeCompatibilityComplete'] and not c['FreshOriginal308Verified']
 return c,f,index
