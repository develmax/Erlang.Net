"""Root only. Exact authority and terminal gates precede all mutations."""
import importlib.util,sys,json,re
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common',Path(__file__).with_name('part308-common.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1]);assert not m.OUT.exists()
ready=m.read(m.pinned(c['ReadinessResult']));assert ready['LocalPassedTests']==c['ExpectedPassed'] and ready['RegisteredMfas']==c['ExpectedRegistry'] and ready['RegisteredMfaDelta']==c['ExpectedRegistry']-504 and ready['ComparisonTestReport']==c['ComparisonTestReport']
volume=m.pinned(c['ActiveChangelogBefore']);entry=m.render(m.pinned(c['ChangelogDraft']).read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip()+'\n',c)
assert entry.startswith('## Part 308') and b'## Part 308' not in volume.read_bytes()
combined=volume.read_bytes()+b'\r\n'+entry.replace('\n','\r\n').encode();assert len(combined)<=32768 and len(combined.splitlines())<=400
note=m.render(m.pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip(),c);assert note.startswith('Part308:')
changes={}
for name in c['MetadataPaths']:
 p=m.safe(name);data=p.read_bytes();assert b'Part308:' not in data;match=re.search(br'\r?\n\r?\n',data);assert match
 nl=b'\r\n' if b'\r\n' in data else b'\n';changes[p]=data[:match.end()]+note.encode()+nl+nl+data[match.end():]
idx=m.pinned(c['ChangelogIndexBefore']);before=c['ChangelogIndexOldLabel'].encode();assert idx.read_bytes().count(before)==1;changes[idx]=idx.read_bytes().replace(before,c['ChangelogIndexNewLabel'].encode())
copies=[]
for row in index:
 data=m.safe(row['SourcePath']).read_bytes();assert m.sha(data)==row['Sha256'];copies.append((m.safe(row['Destination']),data))
config_bytes=Path(sys.argv[1]).read_bytes();index_bytes=m.pinned(c['EvidenceIndex']).read_bytes()
authority_bytes=m.pinned(c['CompositeAuthority']).read_bytes()
for name,data in [('final-actual-config.json',config_bytes),('evidence-index.json',index_bytes),('composite-authority.json',authority_bytes)]:
 dest=m.OUT/'packaging-proposal2'/name;assert all(p!=dest for p,_ in copies);copies.append((dest,data))
for dest,data in copies:dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
for dest,data in changes.items():dest.write_bytes(data)
volume.write_bytes(combined)
receipt={'Status':'ACTUAL_MAIN308_FULL_NATIVE_EXACT_COMPOSITE_AUTHORITY','ParentHead':c['ParentHead'],'Passed':c['ExpectedPassed'],'Failed':0,'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'SourceFreezeSha256':c['SourceFreeze']['Sha256'],'ReviewedSourcePlanSha256':c['NativeSourcePlan']['Sha256'],'NativeInputs':c['ExpectedNativeInputs'],'Registry':c['ExpectedRegistry'],'RegistryDelta':c['ExpectedRegistry']-504,'OptionalIoExports':c['ExpectedOptionalIo'],'ErlangPresence':c['ExpectedErlang'],'AddedNames':c['AddedNames'],'AddedHostNames':c['AddedHostNames'],'Historical6627OrderedNamesExact':True,'IntegrationAndPackagesPassed':True,'NativeTrustedWindowsProfilePassed':True,'ActualGeneratedCSharpRoutePassed':True,'Previous307CIAndNativeAndOrdinaryVerified':True,'GeneralNifAbiComplete':False,'DefaultNativeHandlerInstalled':False,'FreshOriginal308Verified':False,'WholeCompatibilityComplete':False,'Closed0045Sha256':m.CLOSED45,'ExactEvidenceIndexSha256':m.sha(index_bytes),'FinalActualConfigSha256':m.sha(config_bytes),'Files':[{'Path':p.relative_to(m.OUT).as_posix(),'Bytes':len(data),'Sha256':m.sha(data)} for p,data in copies]}
receipt.update({'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'ActualRawExporterPreserved':True,'ExactTwoLimitsDisclosures':True,'Composite2189ArchiveSha256':c['CompositeArchive']['Sha256'],'Composite2189AndAll1032CandidateFilesReplayed':True,'Previous307OrdinaryRawRowsExactAgainstRetained303Baseline':True,'Previous307ObservedMapStdoutVariances':0,'Previous307PermittedMapStdoutAlternatives':1,'ComparisonTestReportMeaning':'Previous baseline for registered MFA deltas; current actual tests are separately archived.'})
receipt['CompositeAuthoritySha256']=m.sha(authority_bytes)
(m.OUT/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');assert m.sha((m.ROOT/'docs/changelog/0045.md').read_bytes())==m.CLOSED45
print(json.dumps({'Passed':c['ExpectedPassed'],'ProofFiles':len(copies),'VolumeBytes':len(combined)}))
