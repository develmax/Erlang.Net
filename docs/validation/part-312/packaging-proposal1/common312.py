"""Root-only actual312 gates. Unbound preparation refuses before Git or mutation."""
from pathlib import Path
import hashlib,json,subprocess
ROOT=Path.cwd().resolve();OUT=ROOT/'docs/validation/part-312'
CLOSED45='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6'
CLOSED46='CAF1B30E0592EF5502A0642631256E3B4F4A5AD8201D96BBB234DA6F2B163361'
CANON46='A575E93B59D490BB2C41F191607A1AAAE821A280E42B1288BD60DCC245B76D6F'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
read=lambda p:json.loads(Path(p).read_bytes())
def safe(name):
 p=(ROOT/name).resolve();assert p.is_relative_to(ROOT)
 assert not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.relative_to(ROOT).parts)
 assert not any(x.lower().startswith('license') for x in p.relative_to(ROOT).parts)
 return p
def pinned(v):
 assert isinstance(v,dict) and {'Path','Bytes','Sha256'}<=v.keys()
 p=safe(v['Path']);b=p.read_bytes();assert (len(b),sha(b))==(v['Bytes'],v['Sha256']),str(p);return p
def git(*args):return subprocess.check_output(['git',*args],cwd=ROOT)
def render(text,c):
 for name,key in [('Passed','ExpectedPassed'),('Registry','ExpectedRegistry'),('OptionalIo','ExpectedOptionalIo'),('Erlang','ExpectedErlang'),('NativeInputs','ExpectedNativeInputs'),('SourcePaths','SourcePaths'),('CSharpPaths','CSharpPaths')]:text=text.replace('{{'+name+'}}',format(c[key],','))
 text=text.replace('{{ACTUAL_MAIN_FULL_AND_NATIVE_GATE_SENTENCE}}',c['ActualValidationSentence'])
 assert '{{' not in text and '}}' not in text
 return text
def gates(path,reviewed=True):
 c=read(path)
 assert c.get('Part')==312 and c.get('BoundToActualComposite') is True and c.get('ReusedFullValidation') is False,'Actual gates remain unbound'
 assert c.get('RootExecutionApproved') is True if reviewed else c.get('RootExecutionApproved') is False,'Root review is required for mutation'
 required=['FullExit','FullLog','Tests','Integration','FullGuard','NativeGuard','RegistryGuard','OptionalIoGuard','RawRegistry','DerivedRegistry','EvidenceIndex','CompositeAuthority','ReadinessScopeCurrent','ReadinessResult']
 assert all(isinstance(c.get(k),dict) for k in required),'Missing current actual gate pins'
 a=read(pinned(c['CompositeAuthority']));assert a['Status']==('ROOT_REVIEWED_ACTUAL_FINAL312_AUTHORITY' if reviewed else 'ROOT_ACTUAL_FINAL312_AUTHORITY_PENDING_REVIEW') and a['RootExecutionApproved']==reviewed and a['SourceFreeze']==c['SourceFreeze']
 assert git('rev-parse','HEAD').decode().strip()==c['ParentHead']=='021ae3c5fb210bd4f4e1f93123cd79ea690103a7'
 f=read(pinned(c['SourceFreeze']));assert f['ParentHead']==c['ParentHead'] and f['TokensExactForCSharp']
 assert len(f['Files'])==len({x['Path'] for x in f['Files']})==f['SourcePaths']==c['SourcePaths']
 cs={x['Path']:x for x in f['Files'] if x['Path'].endswith('.cs')};assert len(cs)==f['CSharpPaths']==c['CSharpPaths']
 for row in f['Files']:pinned(row)
 closure=read(pinned(c['SourceClosure']));assert c['SourceClosure']['Sha256']==f['FinalSourceClosureSha256'] and len(closure['Files'])==f['SourceClosurePaths']
 for row in closure['Files']:pinned(row)
 covered=set()
 for group in c['FormatGroups']:
  before=read(pinned(group['Before']));after=read(pinned(group['After']));assert len(before)==len(after)==len(group['SourcePaths'])
  assert [x['Tokens'] for x in before]==[x['Tokens'] for x in after]
  if group['Mode']=='FullJsonExact':assert before==after
  else:assert group['Mode']=='TokensIgnoringAbsolutePath'
  if group.get('Guard'):
   g=read(pinned(group['Guard']));assert g['CSharpPaths']==len(group['SourcePaths'])
   assert g['BeforeSha256']==group['Before']['Sha256'] and g['AfterSha256']==group['After']['Sha256']
   assert {x['Path'] for x in g['Files']}==set(group['SourcePaths'])
   for row in g['Files']:assert all(row[k]==cs[row['Path']][k] for k in ['Bytes','Sha256'])
  for name in group['SourcePaths']:assert name in cs and name not in covered;covered.add(name)
 assert covered==set(cs)
 for key,field in [('ExpectedPassed','ExpectedFullPassed'),('ExpectedRegistry','ExpectedRegistry'),('ExpectedErlang','ExpectedErlangPresence'),('ExpectedOptionalIo','ExpectedOptionalIoRegistry'),('ExpectedNativeInputs','ReviewedSourceInputs')]:assert c[key]==f[field]
 assert read(pinned(c['FullExit']))['ExitCode']==0 and b'Validation passed' in pinned(c['FullLog']).read_bytes()
 tests=read(pinned(c['Tests']));old=read(pinned(c['PreviousTests']));assert old['Failed']==0 and tests['Passed']==c['ExpectedPassed'] and tests['Failed']==0
 assert pinned(c['PreviousTests']).read_bytes()==git('show',c['ParentHead']+':docs/validation/part-311/tests.json')
 names=[x['Name'] for x in tests['Tests']];prior=[x['Name'] for x in old['Tests']]
 assert len(names)==len(set(names))==c['ExpectedPassed'] and all(x['Status']=='Passed' for x in tests['Tests'])
 assert [x for x in names if x in set(prior)]==prior and set(names)-set(prior)==set(c['AddedNames']) and len(c['AddedNames'])==c['ExpectedPassed']-old['Passed']
 assert set(c['AddedHostNames'])<=set(c['AddedNames'])
 integration=read(pinned(c['Integration']));assert integration==read(pinned(c['PreviousIntegration'])) and len(integration)==11
 assert pinned(c['PreviousIntegration']).read_bytes()==git('show',c['ParentHead']+':docs/validation/part-311/integration-results.json')
 for label,assertions in c['GuardAssertions'].items():
  guard=read(pinned(c[label]))
  for key,value in assertions.items():assert guard[key]==value,(label,key)
 full=read(pinned(c['FullGuard']));native=read(pinned(c['NativeGuard']))
 for guard in [full,native]:assert guard['SourceHashesExact'] and guard['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
 assert full['Passed']==c['ExpectedPassed'] and full['Failed']==0 and full['AddedNames']==c['AddedNames'] and full['SourcePaths']==c['SourcePaths'] and full['CSharpPaths']==c['CSharpPaths']
 assert native['NativeTrustedWindowsProfilePassed'] and native['ActualGeneratedCSharpRoutePassed'] and native['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 assert c['NativeGuard']['Path']=='artifacts/root-part312-main-native/guard.json'
 plan=read(pinned(c['NativeSourcePlan']));assert len(plan['Files'])==c['ExpectedNativeInputs'] and f['ReviewedSourcePlanSha256']==c['NativeSourcePlan']['Sha256']
 for row in plan['Files']:
  data=safe(row['Path']).read_bytes()
  if row['HashMode']=='TextLF':data=data.replace(b'\r\n',b'\n')
  else:assert row['HashMode']=='BinaryExact'
  assert (len(data),sha(data))==(row['Bytes'],row['Sha256']),row['Path']
 raw=read(pinned(c['RawRegistry']));derived=read(pinned(c['DerivedRegistry']));previous=json.loads(git('show',c['ParentHead']+':docs/supported-mfas.json'));key=lambda x:(x['Module'],x['Function'],x['Arity'])
 assert len(raw)==len(derived)==len(previous)==c['ExpectedRegistry'] and derived==previous and sum(x['Module']=='erlang' for x in derived)==c['ExpectedErlang']
 assert [key(x) for x in raw]==[key(x) for x in derived] and len({key(x) for x in derived})==c['ExpectedRegistry']
 changes=[]
 for before,after in zip(raw,derived):
  if before!=after:
   assert key(before) in {('erlang','process_info',1),('erlang','load_nif',2)} and {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'}
   changes.append({'Mfa':list(key(before)),'Field':'Limits','Raw':before['Limits'],'Derived':after['Limits']})
 assert changes==a['RegistryMetadataChanges']
 reg=read(pinned(c['RegistryGuard']));assert reg['RawSha256']==c['RawRegistry']['Sha256'] and reg['RestoredSha256']==c['DerivedRegistry']['Sha256'] and reg['Changes']==changes
 io=read(pinned(c['OptionalIoGuard']));assert io['FullTestsSha256']==c['Tests']['Sha256'] and io['DefaultExports']==c['ExpectedRegistry'] and io['OptionalIoExports']==c['ExpectedOptionalIo'] and not io['IndependentAdditionalExecution']
 for row in io['Sources']:assert sha(safe(row['Path']).read_bytes())==row['Sha256']
 assert not c['GeneralNifAbiComplete'] and not c['DefaultNativeHandlerInstalled'] and not c['WholeCompatibilityComplete'] and not c['FreshOriginal312Verified'] and c['PublicLoadNifRegistered'] and c['ComparisonTestReport']=='validation/part-311/tests.json'
 index=read(pinned(c['EvidenceIndex']));destinations=set()
 for row in index:
  pinned({'Path':row['SourcePath'],'Bytes':row['Bytes'],'Sha256':row['Sha256']});dest=safe(row['Destination']);assert dest.is_relative_to(OUT) and dest not in destinations;destinations.add(dest)
 terminal=read(pinned(c['PreviousTerminalProofIndex']));assert terminal['Status']=='PASS_PREVIOUS311_STAGE_CI_AND_NATIVE_DETACHED_PROOFS' and terminal['Head']==c['ParentHead']
 for declaration in terminal['Manifests']:
  mp=safe(declaration['Path']);assert sha(mp.read_bytes())==declaration['Sha256']
  for row in read(mp):assert any(all(x[k]==row[k] for k in ['SourcePath','Destination','Bytes','Sha256']) for x in index)
 portable=read(pinned(c['PortableReferenceIndex']));replay=read(pinned(c['PortableReplayGuard']));inputs=read(pinned(c['PortableInputAuthority']))
 assert replay['Status']=='PASS_ARCHIVE_ONLY_EVERY_FROZEN_LOGICAL_MEMBER_REPLAY' and replay['ArchiveSha256']==portable['ArchiveSha256']==c['CompositeArchive']['Sha256'] and replay['LogicalRecords']==portable['LogicalRecords'] and replay['UniquePhysicalMembers']==portable['UniquePhysicalMembers'] and not replay['SourceCandidateDirectoriesRead']
 assert set(portable['Sets'])==set(a['PortableSetNames']) and portable['InputAuthoritySha256']==c['PortableInputAuthority']['Sha256']
 assert inputs['MainSourceFreeze']==c['SourceFreeze'] and portable['Sets']['root312-revised-source-resource']['FreezeSha256']==f['FinalSourceClosureSha256']
 focus=read(pinned(c['CorrectedFocusGuard']));assert focus['Status']=='ACTUAL_MAIN_FOUR_FOCUSED_GROUPS_PASS_SAME_COMPILED_IDENTITIES' and focus['Passed']==4 and focus['ClosedRaceBodyByteExactAfterFormatting'] and focus['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
 assert sha((ROOT/'docs/changelog/0045.md').read_bytes())==CLOSED45 and sha((ROOT/'docs/changelog/0046.md').read_bytes())==CLOSED46
 assert sha(git('show',c['ParentHead']+':docs/changelog/0046.md'))==CANON46
 assert c['RotationTarget']=='docs/changelog/0047.md' and pinned(c['ActiveChangelogBefore']).relative_to(ROOT).as_posix()=='docs/changelog/0046.md'
 assert b'docs/validation/part-312/** binary' in (ROOT/'.gitattributes').read_bytes()
 return c,f,index
