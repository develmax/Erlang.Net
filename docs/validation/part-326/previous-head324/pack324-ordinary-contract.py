"""Passive fresh ordinary5880/fd8 audit against unchanged published308 raw rows/rules."""
from pathlib import Path
import base64,hashlib,json,re,sys,importlib.util
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('audit',Path(__file__).with_name('pack324-ordinary-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mapping,d,run,job,items,rawrows=m.load('ordinary');head=mapping['Head'];assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{items[0]["id"]}';v=m.read(ex/'differential.json');fd=m.read(ex/'fd-differential.json')
oldpath='artifacts/pack323-watch-38843439575-terminal/artifact-12023315909/differential.json'
paths=[oldpath,oldpath.replace('differential.json','fd-differential.json'),'tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs']
blobs=m.blobs(head,paths[2:]);blobs[oldpath]=(m.ROOT/oldpath).read_bytes();blobs[paths[1]]=(m.ROOT/paths[1]).read_bytes();old=json.loads(blobs[oldpath]);oldfd=json.loads(blobs[paths[1]])
assert m.sha(blobs[oldpath])=='BB3FF9030824F131B590724A00D3AF659D6A75FC196CC4FBBCB3A38950C46D6F' and m.sha(blobs[paths[1]])=='04A222821AA5280A87A025B270F926F738B696D7EF8182AA604CCBA5AD3B63A7'
assert v['Planned']==v['Executed']==v['Passed']==5891 and old['Passed']==5885 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
differences=[]
for key,count in [('Results',778),('ModuleResults',4942),('DiagnosticResults',165)]:
 assert len(old[key])==count and len(v[key])==count+(6 if key=='ModuleResults' else 0) and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in v[key][:count]]==[x.get('Name',x.get('Source')) for x in old[key]]
 differences.extend({'Track':key,'Index':i,'Previous323':before,'Current324':after} for i,(before,after) in enumerate(zip(old[key],v[key])) if before!=after)
if differences:
 path=m.A/f'pack324-ordinary-{mode}-historical-differences.json';assert not path.exists();path.write_text(json.dumps(differences,indent=2)+'\n')
# Strict refuses a raw difference after complete passive validation; it never rewrites a row.
assert len(differences)<=1
canonical='237B613D3E7B307D2C623D3E2241227D0D0A';alternative='237B623D3E2241222C613D3E7B307D7D0D0A'
for change in differences:
 assert change['Track']=='ModuleResults' and change['Index']==3605
 before,after=change['Previous323'],change['Current324'];assert before['Name']==after['Name']=='erlang-term-display-map'
 assert {k for k in set(before)|set(after) if before.get(k)!=after.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for row in [before,after]:
  assert row['ExpectedStandardOutputHex']==row['SdkStandardOutputHex']==canonical and row['ExpectedStandardOutputAlternatives']==[alternative] and row['NativeStandardOutputHex'] in [canonical,alternative] and row['NativeAndSdkStandardOutputExactlyMatch']==(row['NativeStandardOutputHex']==canonical) and row['StandardStreamsMatch']
  assert row['Expected']==row['FixtureExpected']==row['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in blobs[paths[2]] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in blobs[paths[3]]
# All229 historical Differential sources remain exact except one additive case registration.
source_paths=[x['Path'] for x in m.read(m.ROOT/'docs/validation/part-323/final-main/source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
current_paths=[x['Path'] for x in m.read(m.ROOT/'docs/validation/part-324/final-main/source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
current_sources=m.blobs(head,current_paths);previous_sources=m.blobs('3785a8916b6469c7e7ada4838786b2526dad7f9e',source_paths)
changed=[name for name in source_paths if current_sources[name]!=previous_sources[name]]
assert len(source_paths)==229 and len(current_paths)==231 and changed==['tools/Erlang.Differential/CompiledModuleCases.cs']
registration=changed[0];oldregistration=previous_sources[registration]
assert current_sources[registration]==oldregistration.replace(b'.. AsyncDistributionCases.Modules\n',b'.. AsyncDistributionCases.Modules,\n        .. ProcessInfoAsyncDistributionCases.Modules\n')
additions=set(current_paths)-set(source_paths)
assert additions=={'tools/Erlang.Differential/ProcessInfoAsyncDistributionCaseNames.cs','tools/Erlang.Differential/ProcessInfoAsyncDistributionCases.cs'}
managed=m.read(m.A/'root-part320-composed2/managed-actual-results.json');assert managed['Passed'] and len(managed['Cases'])==5
newrows=v['ModuleResults'][-11:-6];assert [x['Name'] for x in newrows]==[x['Case'] for x in managed['Cases']]
for x,expected in zip(newrows,managed['Cases']):
 assert x['SourceSha256']==expected['SourceSha256'] and m.sha(x['Source'].encode())==x['SourceSha256'] and x['Expected']==x['FixtureExpected']==x['Actual']==expected['Expected'] and x['CallerContext']==expected['CallerContext'] and x['CompilerPolicy']=='Default' and x['Passed'] and x['StandardStreamsMatch']
 assert x['NativeStandardOutputHex'] in [None,''] and x['NativeStandardErrorHex'] in [None,''] and x['SdkStandardOutputHex'] in [None,''] and x['SdkStandardErrorHex'] in [None,'']
# New six native fixtures bind byte-exact to actual compiled managed report6A2C and published case sources.
managed_path=m.A/'process-info-async-dist324-focus2/root-green-run-execution/actual.json'
managed_six=m.read(managed_path)
assert m.sha(managed_path.read_bytes())=='6A2C2C548B9F5444827CA7F517CFB3C690012D7874BA8EF41C137FDD9210A7F8'
assert managed_six['RecipePassed'] and managed_six['Executed']==managed_six['FixturePassed']==6 and managed_six['FixtureFailed']==0
for path in additions|{registration}:
 assert current_sources[path]==(m.A/'process-info-async-dist324-focus2/candidate'/path).read_bytes()
actual_six=v['ModuleResults'][-6:]
assert [x['Name'] for x in actual_six]==[x['Case'] for x in managed_six['Cases']]
for native,sdk in zip(actual_six,managed_six['Cases']):
 assert native['Source']==sdk['Source'] and native['SourceSha256']==sdk['SourceSha256']==m.sha(native['Source'].encode())
 assert native['Expected']==native['FixtureExpected']==native['Actual']==sdk['Actual']==sdk['Expected']=='{ok,done}'
 assert native['CallerContext']==sdk['CallerContext']=='UntrappedFresh' and native['CompilerPolicy']=='Default' and native['Passed'] and native['StandardStreamsMatch']
 assert all(native.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
 assert sdk['FixturePassed'] and sdk['Exception'] is None
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack']:assert v[key]==old[key]
assert {k:value for k,value in v['CompiledModuleTrack'].items() if k not in ['Planned','Executed','Passed']}=={k:value for k,value in old['CompiledModuleTrack'].items() if k not in ['Planned','Executed','Passed']} and all(v['CompiledModuleTrack'][k]==4948 for k in ['Planned','Executed','Passed'])
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
for row in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(row['Source'].encode())==row['SourceSha256']
assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
raw=(d/f'job-{job["id"]}.log').read_bytes();log=raw.decode('utf-8-sig');assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
result={'Status':'PASS_EXACT_HEAD324_FRESH_ORIGINAL5891_FD8_OLD5885_AND_SIX_PUBLIC_ASYNC_DIST_QUERIES','Mode':mode,'Head':head,'Run':mapping['Run'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':5891,'Failed':0,'Expressions':778,'CompiledModules':4948,'Diagnostics':165,'FdPassed':8,'AllHistorical5885OrderedRowsVerified':True,'FiveExistingOriginalAndSdkFlagCasesRetained':True,'NewOrdinary324Cases':6,'HistoricalRawRowsExact':not differences,'ObservedPredeclaredNativeMapStdoutVarianceRows':len(differences),'RawNormalization':False,'HistoricalDifferencesPreserved':True,'SixExclusionsContextPolicyExact':True,'Historical229SourcesExceptExactAdditiveRegistrationPreserved':True,'PublishedDifferentialSourceFiles':len(current_paths),'SixActualNativeCasesMatchActualManaged6A2C':True,'NewOriginalStageInterleavingsExecuted':False,'GeneralNifAbiComplete':False,'ReferenceCoverageComplete':False,'RawZipSha256':m.sha((d/f'artifact-{items[0]["id"]}.zip').read_bytes()),'RawJobSha256':m.sha(raw),'Previous323DifferentialSha256':m.sha(blobs[oldpath]),'NativeExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
result.update(StrictRawEqualityPassed=not differences,AllElevenRawFilesVerified=True,UnpackedZipMembersVerifiedAgainstApiDigest=True,Current323OriginalBusyCollectorExecuted=False,Current324OrdinaryExecuted=True,GuardSourceSha256=m.sha(Path(__file__).read_bytes()))
if mode=='strict' and differences:result.update(Status='REFUSED_STRICT_RAW_EQUALITY_ONE_EXISTING_PREDECLARED_MAP_STDOUT_VARIANCE',Passed=False,SemanticComparisonRequiredSeparately=True)
out=m.A/f'pack324-ordinary-{mode}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))

if mode=='strict' and differences:raise SystemExit(1)
