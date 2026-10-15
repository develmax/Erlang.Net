"""Passive ordinary5861 gate: exact historical5854 plus seven frozen compiled cases."""
from pathlib import Path
import base64,hashlib,io,json,re,subprocess,sys,zipfile
r=Path.cwd();a=r/'artifacts';head='e1d8a999faac113d98ebf2d4471f80ef30daf84a';rid=38763659694;mode=sys.argv[1];assert mode in ['strict','semantic']
d=a/f'pack308-watch-{rid}-terminal';j=lambda p:json.loads(p.read_bytes());h=lambda data:hashlib.sha256(data).hexdigest().upper();blob=lambda path:subprocess.check_output(['git','cat-file','blob',head+':'+path],cwd=r)
envelope_path=a/f'pack308-terminal-{rid}-envelope-guard.json';envelope=j(envelope_path);assert envelope['Head']==head and envelope['Conclusion']=='success'
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert run['id']==rid and run['run_attempt']==1 and run['name']=='Pinned OTP differential oracle' and run['conclusion']=='success' and len(jobs)==len(items)==1
job,item=jobs[0],items[0];assert job['id']==116521174272 and job['head_sha']==head and job['conclusion']=='success' and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{item["id"]}';v=j(ex/'differential.json');oldpath='docs/validation/part-308/previous-head307-original/pack307-watch-38757595924-terminal/artifact-11984834385/differential.json';old=json.loads(blob(oldpath))
assert v['Planned']==v['Executed']==v['Passed']==5861 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
differences=[]
for key,prior_count,actual_count in [('Results',778,778),('ModuleResults',4911,4918),('DiagnosticResults',165,165)]:
 assert len(old[key])==prior_count and len(v[key])==actual_count and all(x['Passed'] for x in v[key])
 differences += [{'Track':key,'Index':i,'Historical':p,'Actual':q} for i,(p,q) in enumerate(zip(old[key],v[key][:prior_count])) if p!=q]
if differences:
 out=a/f'pack308-ordinary-{mode}-historical-differences.json';assert not out.exists();out.write_text(json.dumps(differences,indent=2)+'\n')
if mode=='strict':assert not differences,'Preserve strict historical differences; do not normalize'
assert len(differences)<=1
for change in differences:
 assert change['Track']=='ModuleResults' and change['Index']==3605
 before,after=change['Historical'],change['Actual'];assert before['Name']==after['Name']=='erlang-term-display-map'
 assert {k for k in set(before)|set(after) if before.get(k)!=after.get(k)}=={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 canonical='237B613D3E7B307D2C623D3E2241227D0D0A';alternative='237B623D3E2241222C613D3E7B307D7D0D0A'
 assert before['ExpectedStandardOutputHex']==after['ExpectedStandardOutputHex']==canonical and before['ExpectedStandardOutputAlternatives']==after['ExpectedStandardOutputAlternatives']==[alternative]
 assert before['NativeStandardOutputHex']==canonical and after['NativeStandardOutputHex']==alternative and before['SdkStandardOutputHex']==after['SdkStandardOutputHex']==canonical
 assert before['NativeAndSdkStandardOutputExactlyMatch'] and not after['NativeAndSdkStandardOutputExactlyMatch'] and before['StandardStreamsMatch'] and after['StandardStreamsMatch']
 assert before['Expected']==before['FixtureExpected']==before['Actual']==after['Expected']==after['FixtureExpected']==after['Actual']=='{ok,done}'
 assert blob('tools/Erlang.Differential/TermDisplayCases.cs')==subprocess.check_output(['git','cat-file','blob','8b83560372b63b4760604ac088802fb74ac1c112:tools/Erlang.Differential/TermDisplayCases.cs'],cwd=r)
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in blob('tools/Erlang.Differential/TermDisplayCases.cs')
 assert b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in blob('tools/Erlang.Differential/CompiledStandardOutputContract.cs')
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack']:assert v[key]==old[key]
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
before_track,track=old['CompiledModuleTrack'],v['CompiledModuleTrack'];assert {k:v for k,v in track.items() if k not in ['Planned','Executed','Passed']}=={k:v for k,v in before_track.items() if k not in ['Planned','Executed','Passed']}
for key in ['Planned','Executed','Passed']:assert track[key]==4918 and before_track[key]==4911
# Frozen managed inventory from the published, fully validated composite independently
# pins all seven new fixture names, source bytes and explicit expected values.
zip_path='docs/validation/part-308/composite-portable/composite2189.zip'
zip_bytes=blob(zip_path);assert h(zip_bytes)=='C346EA265DFDFDD8EA2708F54218A917F79ABBEF1ECA16AFD3CA55E3DD22611C'
with zipfile.ZipFile(io.BytesIO(zip_bytes)) as z:
 inventory_bytes=z.read('evidence/compiled-cases-inventory.json');inventory=json.loads(inventory_bytes)
assert h(inventory_bytes)=='588B6660A62618CF7C091C9FB0537F4F3EA0669135985F0A11E806B3113B8612'
expected=[x for x in inventory if x['Name'].startswith('erlang-load-nif-indirect-')];added=v['ModuleResults'][4911:];assert len(expected)==len(added)==7
assert [x['Name'] for x in added]==[x['Name'] for x in expected]
for row,spec in zip(added,expected):
 assert row['SourceSha256']==spec['SourceSha256']==h(row['Source'].encode()) and row['FixtureExpected']==row['Expected']==row['Actual']==spec['Expected']
 assert row['CallerContext']=='UntrappedFresh' and row['CompilerPolicy']=='Default' and row['Passed'] and row['StandardStreamsMatch'] and re.fullmatch('[0-9A-F]{64}',row['CSharpSha256'])
 assert all(row[key] is None for key in ['ExpectedStandardOutputHex','ExpectedStandardOutputAlternatives','ExpectedStandardErrorHex','NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
for row in v['ModuleResults']+v['DiagnosticResults']:assert h(row['Source'].encode())==row['SourceSha256']
fd=j(ex/'fd-differential.json');oldfd=json.loads(blob(oldpath.replace('differential.json','fd-differential.json')));assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
rawlog=(d/f'job-{job["id"]}.log').read_bytes();log=rawlog.decode('utf-8-sig');assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
changed=subprocess.check_output(['git','diff','--name-only','8b83560372b63b4760604ac088802fb74ac1c112',head,'--','src/Erlang.Compiler/CodeGeneration.cs','tools/Erlang.Differential'],cwd=r).decode().splitlines()
assert set(changed)=={'tools/Erlang.Differential/CompiledModuleCases.cs','tools/Erlang.Differential/IndirectLoadNifCaseNames.cs','tools/Erlang.Differential/IndirectLoadNifCases.cs'}
result={'Status':'PASS_EXACT_HEAD308_ORIGINAL5861_FD8_SEVEN_INDIRECT_LOAD_NIF_CASES','Mode':mode,'Head':head,'Run':rid,'Job':job['id'],'Artifact':item['id'],'Attempt':1,'Passed':5861,'Failed':0,'Expressions':778,'HistoricalCompiledModules':4911,'CompiledModules':4918,'Diagnostics':165,'AddedCompiledCases':7,'AddedNames':[x['Name'] for x in added],'NewFixtureSourceAndExpectedExactToFrozenInventory':True,'Historical5854SourceExpectedSdkGeneratedAndPoliciesExact':True,'HistoricalRawRowsExact':not differences,'ObservedNativeMapStdoutVarianceRows':len(differences),'PredeclaredAllowedNativeMapStdoutVarianceRows':1,'HistoricalDifferencesPreserved':True,'RawNormalization':False,'FdPassed':8,'SixExclusionsContextPolicyExact':True,'ChangedHistoricalCompilerGenerationSource':False,'NewNativeAbiActivated':False,'DefaultNativeLoadInstructionHandlerInstalled':False,'PublicLoadNifRegistered':True,'WholeCompatibility':False,'ReferenceCoverageComplete':False,'RawZipSha256':h((d/f'artifact-{item["id"]}.zip').read_bytes()),'RawJobSha256':h(rawlog),'EnvelopeGuardSha256':h(envelope_path.read_bytes()),'FrozenInventorySha256':h(inventory_bytes),'MainEdited':False,'NativeExecutedByVerifier':False,'PostExecuted':False}
out=a/f'pack308-original-{rid}-{mode}-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
