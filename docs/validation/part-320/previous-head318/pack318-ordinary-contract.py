"""Passive fresh ordinary5880/fd8 audit against unchanged published308 raw rows/rules."""
from pathlib import Path
import base64,hashlib,json,re,sys,importlib.util
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack318-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mapping,d,run,job,items,rawrows=m.load('ordinary');head=mapping['Head'];assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{items[0]["id"]}';v=m.read(ex/'differential.json');fd=m.read(ex/'fd-differential.json')
oldpath='artifacts/pack317-watch-38821055667-terminal/artifact-12014251011/differential.json'
paths=[oldpath,oldpath.replace('differential.json','fd-differential.json'),'tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs']
blobs=m.blobs(head,paths[2:]);blobs[oldpath]=(m.ROOT/oldpath).read_bytes();blobs[paths[1]]=(m.ROOT/paths[1]).read_bytes();old=json.loads(blobs[oldpath]);oldfd=json.loads(blobs[paths[1]])
assert m.sha(blobs[oldpath])=='81D3887BA86614D29A14538015932AC2680A41773D67D52B09C9C2E7D875C083' and m.sha(blobs[paths[1]])=='04A222821AA5280A87A025B270F926F738B696D7EF8182AA604CCBA5AD3B63A7'
assert v['Planned']==v['Executed']==v['Passed']==old['Passed']==5880 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
differences=[]
for key,count in [('Results',778),('ModuleResults',4937),('DiagnosticResults',165)]:
 assert len(v[key])==len(old[key])==count and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in v[key]]==[x.get('Name',x.get('Source')) for x in old[key]]
 differences.extend({'Track':key,'Index':i,'Previous317':before,'Current318':after} for i,(before,after) in enumerate(zip(old[key],v[key])) if before!=after)
if differences:
 path=m.A/f'pack318-ordinary-{mode}-historical-differences.json';assert not path.exists();path.write_text(json.dumps(differences,indent=2)+'\n')
if mode=='strict':assert not differences,'Strict raw variance retained; only separate semantic gate can apply existing published allowance'
assert len(differences)<=1
canonical='237B613D3E7B307D2C623D3E2241227D0D0A';alternative='237B623D3E2241222C613D3E7B307D7D0D0A'
for change in differences:
 assert change['Track']=='ModuleResults' and change['Index']==3605
 before,after=change['Previous317'],change['Current318'];assert before['Name']==after['Name']=='erlang-term-display-map'
 assert {k for k in set(before)|set(after) if before.get(k)!=after.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for row in [before,after]:
  assert row['ExpectedStandardOutputHex']==row['SdkStandardOutputHex']==canonical and row['ExpectedStandardOutputAlternatives']==[alternative] and row['NativeStandardOutputHex'] in [canonical,alternative] and row['NativeAndSdkStandardOutputExactlyMatch']==(row['NativeStandardOutputHex']==canonical) and row['StandardStreamsMatch']
  assert row['Expected']==row['FixtureExpected']==row['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in blobs[paths[2]] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in blobs[paths[3]]
# Every ordinary corpus source/expectation/policy and stdout allowance is unchanged since published308.
source_paths=[x['Path'] for x in m.read(m.A/'root-part317-main1/source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
current_sources=m.blobs(head,source_paths);previous_sources=m.blobs('50cee883796a95c11fcde318098ce8a1156685ca',source_paths)
assert current_sources==previous_sources
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack','CompiledModuleTrack']:assert v[key]==old[key]
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
for row in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(row['Source'].encode())==row['SourceSha256']
assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
raw=(d/f'job-{job["id"]}.log').read_bytes();log=raw.decode('utf-8-sig');assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
result={'Status':'PASS_EXACT_HEAD318_FRESH_ORIGINAL5880_FD8_UNCHANGED317_CORPUS_AND_RULES','Mode':mode,'Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':5880,'Failed':0,'Expressions':778,'CompiledModules':4937,'Diagnostics':165,'FdPassed':8,'All5880OrderedRowsVerified':True,'HistoricalRawRowsExact':not differences,'ObservedPredeclaredNativeMapStdoutVarianceRows':len(differences),'RawNormalization':False,'HistoricalDifferencesPreserved':True,'SixExclusionsContextPolicyExact':True,'DifferentialSourceFilesUnchangedSince317':len(source_paths),'NewOriginalStageInterleavingsExecuted':False,'GeneralNifAbiComplete':False,'ReferenceCoverageComplete':False,'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'RawJobSha256':m.sha(raw),'Previous317DifferentialSha256':m.sha(blobs[oldpath]),'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=m.A/f'pack318-ordinary-{mode}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
