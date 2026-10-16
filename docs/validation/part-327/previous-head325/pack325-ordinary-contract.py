"""Passive exact325 ordinary contracts; strict raw comparison is independently retained."""
from pathlib import Path
import base64,importlib.util,json,re,sys
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('audit325',Path(__file__).with_name('pack325-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mapping,d,run,job,items,rawrows=m.load('ordinary');head=mapping['Head']
assert head=='2bd148150ddf2cca49dd2c797d05de3260bf663c' and run['id']==38848192670 and job['id']==116803361433 and items[0]['id']==12023784821
assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/'artifact-12023784821';oldroot=m.A/'pack324-watch-38845658227-terminal/artifact-12024030806'
v=m.read(ex/'differential.json');fd=m.read(ex/'fd-differential.json');old=m.read(oldroot/'differential.json');oldfd=m.read(oldroot/'fd-differential.json')
assert m.sha((oldroot/'differential.json').read_bytes())=='BBA717509423F8E5C555B5F7E9BDDEC1C067F7BFF6173DF4BC4E1F89D90E1886'
assert m.sha((oldroot/'fd-differential.json').read_bytes())=='04A222821AA5280A87A025B270F926F738B696D7EF8182AA604CCBA5AD3B63A7'
for report in [v,old]:
 assert report['Planned']==report['Executed']==report['Passed']==5891 and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
differences=[]
for key,count in [('Results',778),('ModuleResults',4948),('DiagnosticResults',165)]:
 assert len(v[key])==len(old[key])==count and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in v[key]]==[x.get('Name',x.get('Source')) for x in old[key]]
 differences.extend({'Track':key,'Index':i,'Previous324':before,'Current325':after} for i,(before,after) in enumerate(zip(old[key],v[key])) if before!=after)
assert len(differences)<=1
paths=['tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs'];policy=m.blobs(head,paths)
canonical='237B613D3E7B307D2C623D3E2241227D0D0A';alternative='237B623D3E2241222C613D3E7B307D7D0D0A'
for change in differences:
 assert change['Track']=='ModuleResults' and change['Index']==3605
 before,after=change['Previous324'],change['Current325'];assert before['Name']==after['Name']=='erlang-term-display-map'
 assert {k for k in set(before)|set(after) if before.get(k)!=after.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for row in [before,after]:
  assert row['ExpectedStandardOutputHex']==row['SdkStandardOutputHex']==canonical and row['ExpectedStandardOutputAlternatives']==[alternative]
  assert row['NativeStandardOutputHex'] in [canonical,alternative] and row['NativeAndSdkStandardOutputExactlyMatch']==(row['NativeStandardOutputHex']==canonical) and row['StandardStreamsMatch']
  assert row['Expected']==row['FixtureExpected']==row['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in policy[paths[0]] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in policy[paths[1]]
sourcepaths=lambda part:[x['Path'] for x in m.read(m.ROOT/f'docs/validation/part-{part}/final-main/source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
current_paths=sourcepaths(325);previous_paths=sourcepaths(324);assert current_paths==previous_paths and len(current_paths)==231
current_sources=m.blobs(head,current_paths);previous_sources=m.blobs('56fa1196fb5507e1dce28e73d530685e7dcbc4d8',previous_paths)
assert current_sources==previous_sources
managed=m.read(m.A/'root-part320-composed2/managed-actual-results.json');assert managed['Passed'] and len(managed['Cases'])==5
for native,sdk in zip(v['ModuleResults'][-11:-6],managed['Cases']):
 assert native['Name']==sdk['Case'] and native['SourceSha256']==sdk['SourceSha256']==m.sha(native['Source'].encode())
 assert native['Expected']==native['FixtureExpected']==native['Actual']==sdk['Expected'] and native['CallerContext']==sdk['CallerContext'] and native['Passed'] and native['StandardStreamsMatch']
sixpath=m.A/'process-info-async-dist324-focus2/root-green-run-execution/actual.json';assert m.sha(sixpath.read_bytes())=='6A2C2C548B9F5444827CA7F517CFB3C690012D7874BA8EF41C137FDD9210A7F8'
six=m.read(sixpath);assert six['RecipePassed'] and six['Executed']==six['FixturePassed']==6 and six['FixtureFailed']==0
for native,sdk in zip(v['ModuleResults'][-6:],six['Cases']):
 assert native['Name']==sdk['Case'] and native['Source']==sdk['Source'] and native['SourceSha256']==sdk['SourceSha256']==m.sha(native['Source'].encode())
 assert native['Expected']==native['FixtureExpected']==native['Actual']==sdk['Actual']==sdk['Expected']=='{ok,done}' and native['CallerContext']==sdk['CallerContext']=='UntrappedFresh'
 assert native['CompilerPolicy']=='Default' and native['Passed'] and native['StandardStreamsMatch'] and sdk['FixturePassed'] and sdk['Exception'] is None
 assert all(native.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack','CompiledModuleTrack']:assert v[key]==old[key]
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
for row in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(row['Source'].encode())==row['SourceSha256']
assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
raw=(d/f'job-{job["id"]}.log').read_bytes();log=raw.decode('utf-8-sig');assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
if differences:
 out=m.A/f'pack325-ordinary-{mode}-historical-differences.json';assert not out.exists();out.write_text(json.dumps(differences,indent=2)+'\n')
result={'Status':'PASS_EXACT_HEAD325_FRESH_ORIGINAL5891_FD8_ALL_324_ORDERED_CONTRACTS_AND_231_SOURCES','Mode':mode,'Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':5891,'Failed':0,'FdPassed':8,'Expressions':778,'CompiledModules':4948,'Diagnostics':165,'AllHistorical5891OrderedRowsVerified':True,'Published231DifferentialSourcesByteExact324':True,'FiveExistingOriginalAndSdkFlagCasesRetained':True,'SixActualOriginalAndManaged6A2CCasesExact':True,'SixExclusionsAndCallerPoliciesExact':True,'StrictRawEqualityPassed':not differences,'ObservedPredeclaredNativeMapStdoutVarianceRows':len(differences),'RawNormalization':False,'AllRawFilesAndZipMembersVerifiedAgainstApiDigest':True,'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'RawJobSha256':m.sha(raw),'Previous324DifferentialSha256':m.sha((oldroot/'differential.json').read_bytes()),'GuardSourceSha256':m.sha(Path(__file__).read_bytes()),'ReviewerExecutedVmSdkNativeNetworkOrMainMutation':False,'Current326OriginalExecuted':False}
if mode=='strict' and differences:result.update(Status='REFUSED_STRICT_RAW_EQUALITY_ONE_EXISTING_PREDECLARED_MAP_STDOUT_VARIANCE',Passed=False,SemanticComparisonRequiredSeparately=True)
out=m.A/f'pack325-ordinary-{mode}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if mode=='strict' and differences:raise SystemExit(1)
