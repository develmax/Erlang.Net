"""Root only, once after actual final full/native/readiness and Root source approval."""
import sys,json,re,importlib.util
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common312',Path(__file__).with_name('common312.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1]);assert not m.OUT.exists();volume=m.safe(c['RotationTarget']);assert not volume.exists()
ready=m.read(m.pinned(c['ReadinessResult']));assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['RegisteredMfaDelta'],ready['ComparisonTestReport'])==(c['ExpectedPassed'],c['ExpectedRegistry'],0,c['ComparisonTestReport'])
scope=m.read(m.pinned(c['ReadinessScopeCurrent']));runtime=next(x for x in scope['Components'] if x['Id']=='Runtime');note=m.render(m.pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip(),c)
assert note.startswith('Part312:') and runtime.get('Implemented',[]).count(note)==1 and all(x in runtime['TestPrefixes'] for x in c['AddedHostNames'])
seed=m.pinned(c['RotationSeed']).read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip()
entry=m.render(m.pinned(c['ChangelogDraft']).read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip(),c);assert entry.startswith('## Part 312')
combined=(seed+'\n\n'+entry+'\n').replace('\n','\r\n').encode();assert len(combined)<=32768 and len(combined.splitlines())<=400
changes={volume:combined}
for name in c['MetadataPaths']:
 p=m.safe(name);data=p.read_bytes();assert b'Part312:' not in data;match=re.search(br'\r?\n\r?\n',data);assert match
 nl=b'\r\n' if b'\r\n' in data else b'\n';changes[p]=data[:match.end()]+note.encode()+nl+nl+data[match.end():]
idx=m.pinned(c['ChangelogIndexBefore']);data=idx.read_bytes();before=c['ChangelogIndexOldLabel'].encode();assert data.count(before)==1 and b'0047.md' not in data
nl=b'\r\n' if b'\r\n' in data else b'\n';changes[idx]=data.replace(before,c['ChangelogIndexNewLabel'].encode()).rstrip(b'\r\n')+nl+c['ChangelogIndexAppendLabel'].encode()+nl
copies=[]
for row in index:
 data=m.safe(row['SourcePath']).read_bytes();assert (len(data),m.sha(data))==(row['Bytes'],row['Sha256']);copies.append((m.safe(row['Destination']),data))
config_bytes=Path(sys.argv[1]).read_bytes();index_bytes=m.pinned(c['EvidenceIndex']).read_bytes();authority_bytes=m.pinned(c['CompositeAuthority']).read_bytes()
for name,data in [('final-actual-config.json',config_bytes),('evidence-index.json',index_bytes),('composite-authority.json',authority_bytes)]:
 dest=m.OUT/'packaging-proposal1'/name;assert all(p!=dest for p,_ in copies);copies.append((dest,data))
assert len({p for p,_ in copies})==len(copies) and not set(changes)&{p for p,_ in copies}
# Every source/input/target checked before the first Main write. Closed0046 is never a target.
assert m.ROOT/'docs/changelog/0046.md' not in changes
for dest,data in copies:dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
for dest,data in changes.items():dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
receipt={'Status':'ACTUAL_MAIN312_FULL_NATIVE_EXACT_REVISED_COMPOSITE_AUTHORITY','ParentHead':c['ParentHead'],'Passed':c['ExpectedPassed'],'Failed':0,'FreshMainFullValidationExecuted':True,'ReusedFullValidation':False,'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'SourceFreezeSha256':c['SourceFreeze']['Sha256'],'ReviewedSourcePlanSha256':c['NativeSourcePlan']['Sha256'],'NativeInputs':c['ExpectedNativeInputs'],'Registry':c['ExpectedRegistry'],'RegistryDelta':0,'OptionalIoExports':c['ExpectedOptionalIo'],'ErlangPresence':c['ExpectedErlang'],'AddedNames':c['AddedNames'],'AddedHostNames':c['AddedHostNames'],'Historical6639OrderedNamesExact':True,'IntegrationAndPackagesPassed':True,'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedCSharpRoutePassed':True,'NewStageInterleavingsHostOnly':True,'FreshOriginal312Verified':False,'NewNativeStageExecution':False,'Ordinary5861Fd8ReusedPublishedPart308':True,'Previous311TerminalEvidenceSeparate':True,'FirstFailedFull6639Of6640Retained':True,'CorrectedFocusFourPassSameDlls':True,'IsolatedStageTokenPaths':34,'RootFinalTokenPaths':c['CSharpPaths'],'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'WholeCompatibilityComplete':False,'Closed0045Sha256':m.CLOSED45,'Closed0046RawSha256':m.CLOSED46,'Closed0046CanonicalSha256':m.CANON46,'RotationTarget':c['RotationTarget'],'ExactEvidenceIndexSha256':m.sha(index_bytes),'FinalActualConfigSha256':m.sha(config_bytes),'CompositeAuthoritySha256':m.sha(authority_bytes),'CompositeArchiveSha256':c['CompositeArchive']['Sha256'],'ArchiveOnlyEveryFrozenMemberReplayed':True,'ComparisonTestReportMeaning':'Previous part311 baseline; current Main full is fresh and actual.','Files':[{'Path':p.relative_to(m.OUT).as_posix(),'Bytes':len(data),'Sha256':m.sha(data)} for p,data in copies]}
(m.OUT/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
assert m.sha((m.ROOT/'docs/changelog/0046.md').read_bytes())==m.CLOSED46
print(json.dumps({'Passed':c['ExpectedPassed'],'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'ProofFiles':len(copies),'VolumeBytes':len(combined),'Rotated0047':True}))
