"""Passive fresh ordinary5870/fd8 audit against unchanged published308 raw rows/rules."""
from pathlib import Path
import base64,hashlib,json,re,sys,importlib.util
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack317-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mapping,d,run,job,items,rawrows=m.load('ordinary');head=mapping['Head'];assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{items[0]["id"]}';v=m.read(ex/'differential.json');fd=m.read(ex/'fd-differential.json')
oldpath='docs/validation/part-317/previous-head316/pack316-watch-38816836998-terminal/artifact-12011849126/differential.json'
paths=[oldpath,oldpath.replace('differential.json','fd-differential.json'),'tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs']
blobs=m.blobs(head,paths);old=json.loads(blobs[oldpath]);oldfd=json.loads(blobs[paths[1]])
assert m.sha(blobs[oldpath])=='6902C83DA85D67BFF8C85486FF2793A728BE3AA842A995E10BDC8CBC28813AD1' and m.sha(blobs[paths[1]])=='04A222821AA5280A87A025B270F926F738B696D7EF8182AA604CCBA5AD3B63A7'
assert v['Planned']==v['Executed']==v['Passed']==5880 and old['Passed']==5870 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
manifest=m.read(m.A/'clr-memory-public317-oracle1/case-manifest.json');new=manifest['ComparableCases'];assert len(new)==10
newnames={x['Name'] for x in new};assert len(newnames)==10
currentmodules=v['ModuleResults'];assert len(currentmodules)==4937
added=[x for x in currentmodules if x['Name'] in newnames];assert len(added)==10 and [x['Name'] for x in added]==[x['Name'] for x in new]
for row,case in zip(added,new):
 assert row['Passed'] and row['Source']==case['Source'] and row['SourceSha256']==case['SourceUtf8Sha256'] and row['Expected']==row['FixtureExpected']==row['Actual']==case['Expected']
oldmodules=[x for x in currentmodules if x['Name'] not in newnames]
compared=dict(v,ModuleResults=oldmodules)
differences=[]
for key,count in [('Results',778),('ModuleResults',4927),('DiagnosticResults',165)]:
 assert len(compared[key])==len(old[key])==count and all(x['Passed'] for x in compared[key])
 assert [x.get('Name',x.get('Source')) for x in compared[key]]==[x.get('Name',x.get('Source')) for x in old[key]]
 differences.extend({'Track':key,'Index':i,'Previous316':before,'Current317':after} for i,(before,after) in enumerate(zip(old[key],compared[key])) if before!=after)
if differences:
 path=m.A/f'pack317-ordinary-{mode}-historical-differences.json';assert not path.exists();path.write_text(json.dumps(differences,indent=2)+'\n')
if mode=='strict':assert not differences,'Strict raw variance retained; only separate semantic gate can apply existing published allowance'
assert len(differences)<=1
canonical='237B613D3E7B307D2C623D3E2241227D0D0A';alternative='237B623D3E2241222C613D3E7B307D7D0D0A'
for change in differences:
 assert change['Track']=='ModuleResults' and change['Index']==3605
 before,after=change['Previous316'],change['Current317'];assert before['Name']==after['Name']=='erlang-term-display-map'
 assert {k for k in set(before)|set(after) if before.get(k)!=after.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for row in [before,after]:
  assert row['ExpectedStandardOutputHex']==row['SdkStandardOutputHex']==canonical and row['ExpectedStandardOutputAlternatives']==[alternative] and row['NativeStandardOutputHex'] in [canonical,alternative] and row['NativeAndSdkStandardOutputExactlyMatch']==(row['NativeStandardOutputHex']==canonical) and row['StandardStreamsMatch']
  assert row['Expected']==row['FixtureExpected']==row['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in blobs[paths[2]] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in blobs[paths[3]]
# Every ordinary corpus source/expectation/policy and stdout allowance outside the two explicit append/observer paths is unchanged since published316.
source_paths=[x['Path'] for x in m.read(m.A/'root-part316-main2/source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
current_sources=m.blobs(head,source_paths);previous_sources=m.blobs('6d7a430f55a43e488257f654e72a53b3b7f96ada',source_paths)
unchanged={p for p in source_paths if not p.endswith(('/AllocatorProfileObservation.cs','/CompiledModuleCases.cs'))};assert len(unchanged)==222
assert all(current_sources[p]==previous_sources[p] for p in unchanged)
assert b'ClrMemoryCases.Modules' in current_sources['tools/Erlang.Differential/CompiledModuleCases.cs']
assert b'ClrMemoryObservationContracts.Validate' in current_sources['tools/Erlang.Differential/AllocatorProfileObservation.cs']
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack']:assert v[key]==old[key]
assert {k:x for k,x in v['CompiledModuleTrack'].items() if k not in ['Planned','Executed','Passed']}=={k:x for k,x in old['CompiledModuleTrack'].items() if k not in ['Planned','Executed','Passed']}
for k in ['Planned','Executed','Passed']:assert v['CompiledModuleTrack'][k]==old['CompiledModuleTrack'][k]+10
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
for row in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(row['Source'].encode())==row['SourceSha256']
assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
raw=(d/f'job-{job["id"]}.log').read_bytes();log=raw.decode('utf-8-sig');assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
result={'Status':'PASS_EXACT_HEAD317_FRESH_ORIGINAL5880_FD8_PRESERVED316_CORPUS_PLUS_TEN_MEMORY_CASES','Mode':mode,'Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':5880,'Failed':0,'Expressions':778,'CompiledModules':4937,'Diagnostics':165,'FdPassed':8,'Historical5870OrderedRowsVerified':True,'NewComparableMemoryCases':10,'HistoricalRawRowsExact':not differences,'ObservedPredeclaredNativeMapStdoutVarianceRows':len(differences),'RawNormalization':False,'HistoricalDifferencesPreserved':True,'SixExclusionsContextPolicyExact':True,'DifferentialSourceFilesUnchangedSince316':len(unchanged),'NewOriginalStageInterleavingsExecuted':False,'GeneralNifAbiComplete':False,'ReferenceCoverageComplete':False,'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'RawJobSha256':m.sha(raw),'Previous316DifferentialSha256':m.sha(blobs[oldpath]),'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=m.A/f'pack317-ordinary-{mode}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
