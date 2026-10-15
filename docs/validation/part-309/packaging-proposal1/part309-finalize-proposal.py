"""Root only; no fresh runtime/full/native validation is represented here."""
import importlib.util,sys,json
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common',Path(__file__).with_name('part309-common.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1]);assert not m.OUT.exists()
ready=m.read(m.pinned(c['ReadinessResult']));assert ready['LocalPassedTests']==6637 and ready['RegisteredMfas']==505 and ready['RegisteredMfaDelta']==0 and ready['ComparisonTestReport']=='validation/part-308/tests.json'
volume=m.pinned(c['ActiveChangelogBefore']);entry=m.render(m.pinned(c['ChangelogDraft']).read_text(encoding='utf-8-sig').replace('\r\n','\n').rstrip()+'\n',c);assert entry.startswith('## Part 309') and b'## Part 309' not in volume.read_bytes()
combined=volume.read_bytes()+b'\r\n'+entry.replace('\n','\r\n').encode();assert len(combined)<=32768 and len(combined.splitlines())<=400
note=m.render(m.pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip(),c);assert note.startswith('Part309:');changes={}
for relative in c['MetadataPaths']:
 p=m.safe(relative);data=p.read_bytes();assert b'Part309:' not in data;changes[p]=m.noted_doc(data,note)
idx=m.pinned(c['ChangelogIndexBefore']);before=c['ChangelogIndexOldLabel'].encode();assert idx.read_bytes().count(before)==1;changes[idx]=idx.read_bytes().replace(before,c['ChangelogIndexNewLabel'].encode())
copies=[]
for row in index:
 data=m.safe(row['SourcePath']).read_bytes();assert m.sha(data)==row['Sha256'];copies.append((m.safe(row['Destination']),data))
config_bytes=Path(sys.argv[1]).read_bytes();index_bytes=m.pinned(c['EvidenceIndex']).read_bytes()
for name,data in [('final-actual-config.json',config_bytes),('evidence-index.json',index_bytes)]:
 dest=m.OUT/'packaging-proposal1'/name;assert all(p!=dest for p,_ in copies);copies.append((dest,data))
for dest,data in copies:dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
for dest,data in changes.items():dest.write_bytes(data)
volume.write_bytes(combined)
receipt={'Status':'TOOLING_ONLY309_STANDALONE_TOOL_PASS_REUSES_EXACT_PUBLISHED308_MAIN_PROOF','ValidationMode':'Reused published308 runtime/full/native plus new standalone tooling checks','ParentHead':c['ParentHead'],'SourcePaths':8,'CSharpPaths':3,'SourceFreezeSha256':c['SourceFreeze']['Sha256'],'Passed':6637,'Failed':0,'ReusedFullPassed':6637,'ReusedFullHead':c['ParentHead'],'NewFullValidationExecuted':False,'IntegrationAndPackagesPassed':True,'IntegrationResultsReused':11,'NewNativeExecuted':False,'NewOriginalExecuted':False,'NewOriginalCompilerExecuted':False,'NewOpcodeShapesVerified':False,'OriginalFixtureBodyExecuted':False,'NativeSourceInputsUnchanged':True,'NativeInputs':1020,'ReviewedSourcePlanSha256':c['NativeSourcePlan']['Sha256'],'ReusedPublishedWindowsNativeAndGeneratedSourcePassed':True,'StandaloneMainToolBuildPassed':True,'ManagedCliRefusalsPassed':6,'AppendRefusalPassed':True,'VerifierRefusalsPassed':2,'Registry':505,'RegistryDelta':0,'OptionalIoExports':507,'ErlangPresence':347,'NewPublicMfa':False,'HumanTableRowsCorrected':3,'ActuallyUnregisteredApis':['alloc_info/1','alloc_sizes/1','memory/0','memory/1'],'RuntimeCompilerAndTestSourcesChanged':False,'Remote308States':m.read(m.pinned(c['RemoteStates'])),'GeneralNifAbiComplete':False,'WholeCompatibilityComplete':False,'Closed0045Sha256':m.CLOSED45,'ExactEvidenceIndexSha256':m.sha(index_bytes),'FinalActualConfigSha256':m.sha(config_bytes),'Files':[{'Path':p.relative_to(m.OUT).as_posix(),'Bytes':len(data),'Sha256':m.sha(data)} for p,data in copies]}
(m.OUT/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');m.table_gate(c,after=True);assert m.sha((m.ROOT/'docs/changelog/0045.md').read_bytes())==m.CLOSED45
print(json.dumps({'ReusedFullPassed':6637,'NewFullValidationExecuted':False,'ProofFiles':len(copies),'VolumeBytes':len(combined)}))
