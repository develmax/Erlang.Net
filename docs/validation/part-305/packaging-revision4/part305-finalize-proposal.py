"""Root-only; actual terminal validation, source and native gates precede every mutation."""
from pathlib import Path
import hashlib,json,re,subprocess
r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-305';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();git=lambda *v:subprocess.check_output(['git',*v],cwd=r)
parent='51994bed995f9dbf31b8b88b3381cad2642012e0';previous303='82ef740f951e6076f095c62b4efd62507d74adc5';assert git('rev-parse','HEAD').decode().strip()==parent and not o.exists()
freeze=a/'root-part305-final-source-freeze3.json';assert h(freeze.read_bytes())=='0B595447CCC12C1735B069F987728D434DFBF1709E04931A23049F70C3115E26';f=j(freeze)
assert f['ParentHead']==parent and f['SourcePaths']==len(f['Files'])==32 and f['CSharpPaths']==26 and f['TokensExactForCSharp'] and f['ReviewedSourceInputs']==1009 and not f['PublicMfaAdded'] and not f['PublicLoadNifActivated'] and not f['NewOriginalExecuted'] and not f['PendingOnLoadPurgeImplemented'] and not f['ZeroExportNifOwnerAssociationComplete']
paths={x['Path'] for x in f['Files']};assert len(paths)==32 and sum(p.endswith('.cs') for p in paths)==26
for row in f['Files']:
 data=(r/row['Path']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256']
assert set(git('diff','HEAD','--name-only','-z','--','src','tests','tools','Erlang.Net.slnx','.github/workflows').decode().split('\0'))-{''}<=paths
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';assert h(plan.read_bytes())==f['ReviewedSourcePlanSha256']=='64267EDED370D35DEA5AB176E39A02370F461072784678C0A922C507484F5F56' and len(j(plan)['Files'])==1009
assert j(a/'root-part305-full3-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part305-full3-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-304/reused303/tests.json');names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']];added=f['AddedHostNames']
assert added==['HOST exact-image process-code requests and purge drain','HOST raw on_load deferred exports against six captured OTP finals']
assert (t['Passed'],t['Failed'])==(6624,0) and len(names)==len(set(names))==6624 and len(oldnames)==6622 and [n for n in names if n in set(oldnames)]==oldnames and set(names)-set(oldnames)==set(added) and all(x['Status']=='Passed' for x in t['Tests'])
assert j(a/'integration-results.json')==j(r/'docs/validation/part-304/reused303/integration-results.json') and len(j(a/'integration-results.json'))==11
raw_path=a/'root-part305-raw-supported-mfas.json';raw=j(raw_path);derived=j(r/'docs/supported-mfas.json');published=json.loads(git('cat-file','blob',parent+':docs/supported-mfas.json'));key=lambda q:(q['Module'],q['Function'],q['Arity']);subset=('erlang','process_info',1)
assert len(raw)==len(derived)==503 and derived==published and [key(x) for x in raw]==[key(x) for x in derived]
for before,after in zip(raw,derived):assert (before==after if key(before)!=subset else {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'})
meta=j(a/'root-part305-registry-metadata-guard.json');assert meta['Status']=='ACTUAL_RAW503_CAPTURED_ONLY_EXISTING_PROCESS_INFO_DISCLOSURE_RESTORED' and meta['RawSha256']==h(raw_path.read_bytes()) and meta['RestoredSha256']==h((r/'docs/supported-mfas.json').read_bytes()) and meta['Other502RowsFieldsAndOrderingExact'] and not meta['SourceExporterChanged']
full_guard=j(a/'root-part305-full-guard.json');assert full_guard['Status']=='ACTUAL_MAIN_6624_FULL_ALL6622_ORDERED_NAMES_32_SOURCE_26_CSHARP_AND_11_INTEGRATION_PASS' and full_guard['SourceHashesExact'] and full_guard['SourceFreezeSha256']==h(freeze.read_bytes()) and full_guard['Passed']==6624 and full_guard['Failed']==0 and full_guard['ActualSixOriginalBeamFixtureCasesPassed']

ready=j(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6624 and ready['RegisteredMfas']==503 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-304/reused303/tests.json'
native=j(a/'root-part305-main-native-guard.json');assert native['Status']=='PASS_ACTUAL_MAIN_NATIVE_TRUSTED_WINDOWS_AND_COMPILED_SOURCE' and native['ExitCode']==0 and native['SourcePaths']==32 and native['CSharpPaths']==26 and native['SourceHashesExact'] and native['SourceFreezeSha256']==h(freeze.read_bytes()) and native['ReviewedSourcePlanSha256']==h(plan.read_bytes())
assert native['NativeTrustedWindowsProfilePassed'] and native['ActualGeneratedCSharpRoutePassed'] and native['ActualNativeFenceConcurrencyPassed'] and native['NonDefaultNativeProviderRefusalPassed'] and not native['GeneralNifAbiComplete'] and native['GeneratedCSharpSha256']=='6E28E8B093F042C22ACAFD2C0213A55C7DCEFC2D162B486AC8443FC7C4E5F77A'
assert (a/'root-part305-main-native.log').read_text(encoding='utf-8-sig').splitlines()==(a/'root-part303-main-native.log').read_text(encoding='utf-8-sig').splitlines()
assert not native['PublicLoadNifActivated'] and not native['NewRawOriginalReexecuted'] and not native['PendingOnLoadPurgeImplemented']
image=a/'raw-onload-exact-image305-candidate1';raw_candidate=a/'raw-onload-deferred-exports306-candidate1'
for directory,expected,count in [(image,'1E0BFF84372B17A5EFEC0259A875E193C48EF5FCCB01E2309456BF9E16807B62',52),(raw_candidate,'652DAD5872282FE5B1E54635308FA2358E5F89F41B9C3ED118A2FFE958662342',97)]:
 sf=directory/'source-freeze.json';assert h(sf.read_bytes())==expected and len(j(sf)['Files'])==count
 for row in j(sf)['Files']:assert len((directory/row['Path']).read_bytes())==row['Bytes'] and h((directory/row['Path']).read_bytes())==row['Sha256']
assert j(image/'tokens8-folder2-before.json')==j(image/'tokens8-folder2-after.json') and j(image/'format8-folder2-guard.json')['TokensExact'] and j(image/'focus14-resource-repaired-exit.json')['ExitCode']==0
assert j(raw_candidate/'tokens17-folder2-before.json')==j(raw_candidate/'tokens17-folder2-after.json') and j(raw_candidate/'format17-folder2-guard.json')['TokensExact'] and j(raw_candidate/'focus19-final-exit.json')['ExitCode']==0
rv=j(raw_candidate/'validation-result.json');assert rv['Status']=='ISOLATED_SIX_CAPTURED_ORIGINAL_FINALS_AND_19_MANAGED_FOCUS_PASS' and len(rv['FocusGroups'])==19 and len(rv['ActualOriginalBinaryCasesPassed'])==6 and rv['MalformedAllocationControlsPassed']==5 and rv['DuplicateAllocationKindPositivePassed'] and not rv['OriginalRerun'] and not rv['NativeAbiExecuted']
assert j(raw_candidate/'red-prerequisite-classification.json')['ExpectedSemanticFailure'] is False and j(raw_candidate/'red-external-import-classification.json')['ExpectedSemanticFailure'] is False
assert j(raw_candidate/'red-semantic-before-latch-exit.json')['ExitCode']==1 and j(raw_candidate/'red-semantic-before-latch-exit.json')['ExpectedSemanticFailure'] and (raw_candidate/'red-semantic-before-latch.log').read_text().startswith('PASS raw on_load captured final fresh_direct_true') and 'fresh_false_false_true' in (raw_candidate/'red-semantic-before-latch.log').read_text()
assert j(raw_candidate/'green-initial-exit.json')['ExitCode']==0 and len(re.findall(r'^PASS raw on_load captured final ',(raw_candidate/'green-initial.log').read_text(),re.M))==6
assert h((a/'lifecycle305-independent-source-review.json').read_bytes())=='FFC392AADAEC00545B53722D8A39212A236E847C8B8289E5089029027FA47B1D'
assert h((a/'lifecycle306-final-independent-source-review.json').read_bytes())=='39F149E0C1929FC8007E5685F4AB27C14704B14A2FA536A6626C14BB27EEDF80'
assert h((a/'lifecycle306-six-projection-resource-review.json').read_bytes())=='6089F3B6A046F9B615F863D0811012C205963B3A7A88875E768822835F8AC79A'
for folder in ['root-part305-full-failure1','root-part305-full-failure2']:
 directory=a/folder;receipt=j(directory/'failure-receipt.json')
 for row in receipt['Files']:assert len((directory/row['Path']).read_bytes())==row['Bytes'] and h((directory/row['Path']).read_bytes())==row['Sha256']
first=j(a/'root-part305-full-failure1/artifacts/tests.json');assert (first['Passed'],first['Failed'])==(6622,2)
assert {row['Name'] for row in first['Tests'] if row['Status']!='Passed'}=={'BEAM prepared publication, native metadata and actual generation leases','host/loader-typed-admission-and-error-metadata'}
assert not j(a/'root-part305-full-failure1/failure-receipt.json')['IntegrationExecutedAfterTests']
second=j(a/'root-part305-full-failure2/failure-receipt.json');assert not second['TestsExecuted'] and not second['IntegrationExecuted'] and not second['RuntimeAlgorithmChanged'] and 'CS0117' in (a/'root-part305-full-failure2/artifacts/root-part305-full2-validation.log').read_text(encoding='utf-8-sig')
legacy=a/'root-part305-legacy-expectations3';assert j(legacy/'tokens2-folder2-before.json')==j(legacy/'tokens2-folder2-after.json') and j(legacy/'format2-folder2-guard.json')['TokensExact']
assert h((a/'lifecycle305-root-two-host-migrations-freeze3-peer-review.json').read_bytes())=='2CFE178478C8C442EFB05E3746CF27B3F6CD6D42814D273631042C09FA4A1F7D'
closed45='9EB82D1D39B01ABB618DFD3698D656E0ADE6F3511506604F33038C9A3B381CB6';assert h((r/'docs/changelog/0045.md').read_bytes())==closed45
volume=r/'docs/changelog/0046.md';entry=(Path(__file__).resolve().parent/'part305-changelog-draft.md').read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip('\r\n')+'\n';combined=volume.read_bytes()+b'\r\n'+entry.replace('\n','\r\n').encode();assert b'## Part 305' not in volume.read_bytes() and len(combined)<=32768 and len(combined.splitlines())<=400 and not combined.endswith(b'\r\n\r\n')
index=r/'CHANGELOG.md';ib=index.read_bytes();index_label='parts302–304 (active)'.encode();assert ib.count(index_label)==1;updated_index=ib.replace(index_label,'parts302–305 (active)'.encode());assert all(isinstance(x,bytes) for x in [ib,combined,updated_index])
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md']
for name in metadata:assert b'Part305:' not in (r/name).read_bytes() and re.search(br'\r?\n\r?\n',(r/name).read_bytes())
attrs=r/'.gitattributes';assert b'docs/validation/part-305/** binary' not in attrs.read_bytes() and b'tests/Erlang.Tests/Resources/RawOnLoadDeferredExports/** binary' not in attrs.read_bytes()
copies={}
def select(src,dst,expected=None,size=None):
 src=src.resolve();dst=dst.resolve();assert src.is_file() and dst.is_relative_to(o) and not {'bin','obj','__pycache__','.git'}.intersection(src.parts) and 'LICENSE.local.txt' not in src.parts
 data=src.read_bytes()
 if expected:assert h(data)==expected
 if size is not None:assert len(data)==size
 if dst in copies:assert copies[dst][1]==h(data)
 copies[dst]=(src,h(data))
def manifest(mp):
 for row in j(mp):select(r/row['SourcePath'],r/row['Destination'],row['Sha256'],row['Bytes'])
 select(mp,o/'staging-manifests'/mp.name)
ordinary=j(a/'pack303-original-38736737943-independent-guard.json');assert ordinary['Head']==previous303 and ordinary['Status']=='PASS_EXACT_HEAD303_ORIGINAL5854_AND_FD8' and ordinary['Passed']==5854 and ordinary['Failed']==0 and ordinary['FdPassed']==8 and ordinary['Historical4911Module778Expression165DiagnosticRowsExact'];manifest(a/'pack303-original-detached-staging-manifest-for305.json')
ci=j(a/'pack304-ci-38739476269-independent-guard.json');assert ci['Head']==parent and ci['Status']=='PASS_EXACT_HEAD304_CI6622_FULL' and ci['Passed']==6622 and ci['Failed']==0 and ci['OrderedNamesExact'] and ci['RegisteredMfas']==503 and len(ci['Integration'])==11;manifest(a/'pack304-ci-detached-staging-manifest-for305.json')
for pattern in ['root-part305-*','root-*305*.py','root-*305*.ps1','part305-*.py','part305-*.json','part305-changelog-draft.md']:
 for src in sorted(a.glob(pattern)):
  if src.is_file():select(src,o/'root-artifacts'/src.name)
for directory in [image,raw_candidate]:
 for row in j(directory/'source-freeze.json')['Files']:select(directory/row['Path'],o/'candidate-provenance'/directory.name/row['Path'],row['Sha256'],row['Bytes'])
 select(directory/'source-freeze.json',o/'candidate-provenance'/directory.name/'source-freeze.json')
for name in ['lifecycle305-independent-source-review.json','lifecycle306-final-independent-source-review.json','lifecycle306-six-projection-resource-review.json']:select(a/name,o/'independent-reviews'/name)
for directory in [a/'root-part305-full-failure1',a/'root-part305-full-failure2',a/'root-part305-legacy-expectations2',a/'root-part305-legacy-expectations3',a/'part305-packaging-initial-source30-history',a/'part305-packaging-revision3']:
 for src in sorted(directory.rglob('*')):
  if src.is_file() and not {'bin','obj','__pycache__','.git'}.intersection(src.parts):select(src,o/'revisions-and-failures'/directory.name/src.relative_to(directory))
select(a/'lifecycle305-root-two-host-migrations-freeze3-peer-review.json',o/'independent-reviews/lifecycle305-root-two-host-migrations-freeze3-peer-review.json')
for src in sorted(Path(__file__).resolve().parent.iterdir()):
 if src.is_file() and src.suffix in ['.py','.md','.json']:select(src,o/'packaging-revision4'/src.name)
for src,dst in [(a/'tests.json',o/'tests.json'),(a/'integration-results.json',o/'integration-results.json'),(r/'docs/supported-mfas.json',o/'supported-mfas.json'),(raw_path,o/'supported-mfas-raw-export.json')]:select(src,dst)
for name in ['module-readiness.json','MODULE_READINESS.md']:select(r/'docs'/name,o/name)
for dst,(src,digest) in copies.items():
 data=src.read_bytes();assert h(data)==digest;dst.parent.mkdir(parents=True,exist_ok=True);dst.write_bytes(data)
volume.write_bytes(combined);index.write_bytes(updated_index)
note='Part305: actual6624/full preserves all6622 immutable304/reused303 ordered names plus two internal HOST rows. Exact-image requests/drain helpers preserve public module queries; raw false permanently clears deferred exports while later true retains zero-export code and local functions, matching six original typed finals through genuine BEAM. Preserve unsupported-opcode/import prerequisites separately from genuine export semantic red/green, five malformed controls and native repeated-kind positive. Two obsolete unsupported-put admission expectations positively migrated; firstfull6622/2 and secondcompile-only lease-property failure retained, currentlease.Value correction changes no runtime algorithm. No new public MFA/default loader;503/345 of351 unchanged. Stage/fun fence, saved-old purger and zero-export NIF source-owner association remain incomplete. Actual503 raw metadata, native1009 postfull checks, candidate14/19 focus and peers preserved; no fresh original rerun. Prior303strict5854/fd8 andCI3046622 verified. Whole compatibility/performance incomplete.'
for name in metadata:
 q=r/name;data=q.read_bytes();cut=re.search(br'\r?\n\r?\n',data).end();nl=b'\r\n' if b'\r\n' in data else b'\n';q.write_bytes(data[:cut]+note.encode()+nl+nl+data[cut:])
attrs.write_bytes(attrs.read_bytes()+b'docs/validation/part-305/** binary\ntests/Erlang.Tests/Resources/RawOnLoadDeferredExports/** binary\n');assert h((r/'docs/changelog/0045.md').read_bytes())==closed45
receipt={'Status':'ACTUAL_MAIN6624_FULL_EXACT_IMAGE_AND_RAW_DEFERRED_EXPORT_PREREQUISITES','ParentHead':parent,'SourcePaths':32,'CSharpPaths':26,'Passed':6624,'Failed':0,'Historical6622OrderedNamesExact':True,'Added':added,'Registry':503,'RegistryDelta':0,'ErlangPresence':345,'ErlangReference':351,'IntegrationAndPackagesPassed':True,'SeparateMainFocusRunClaimed':False,'ImageCandidateFocusGroups':14,'RawCandidateFocusGroups':19,'InitialActualFull6622PassTwoObsoleteAdmissionFailuresPreserved':True,'SecondCompileOnlyCS0117FailurePreserved':True,'LegacyPrepareAdmissionFixturesPositivelyMigrated':2,'RuntimeChangedForLegacyFixtureMigration':False,'SixCapturedOriginalFinalsMatched':True,'OriginalCompiledBeamBytesUnmodified':True,'MalformedAllocationControlsPassed':5,'DuplicateAllocationKindPositivePassed':True,'PrerequisiteFailuresSeparateFromSemanticRed':True,'ActualPostFullMainNativeWindowsPassed':True,'PublicLoadNifActivated':False,'GeneralNifAbiComplete':False,'ReviewedSourceInputs':1009,'ReviewedSourcePlanSha256':h(plan.read_bytes()),'Actual503RawExporterOutputPreserved':True,'OnlyExistingProcessInfo1LimitsRestored':True,'Previous303StrictOriginal5854Fd8Verified':True,'Previous304CI6622Verified':True,'OldArchived304CIAndNativeProofDuplicated':False,'FreshOriginalRerun':False,'PendingOnLoadPurgeImplemented':False,'StageFunFenceImplemented':False,'ZeroExportNifOwnerAssociationComplete':False,'SameHeadCI305Verified':False,'ChangedSourceOriginalVerified':False,'WholeCompatibility':False,'Closed0045Sha256':closed45}
receipt['Files']=[{'Path':q.relative_to(o).as_posix(),'Bytes':q.stat().st_size,'Sha256':h(q.read_bytes())} for q in sorted(o.rglob('*')) if q.is_file()]
(o/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({'Passed':6624,'SourcePaths':32,'ProofFiles':len(receipt['Files']),'VolumeBytes':len(combined)}))
