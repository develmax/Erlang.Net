from pathlib import Path
import base64, hashlib, json, re, subprocess, sys, zipfile
r=Path.cwd();a=r/'artifacts';head='ca33514ba537688aaa8a265200b8d270a01f98fb';rid=int(sys.argv[1]);kind=sys.argv[2];d=a/f'pack301-watch-{rid}-terminal'
j=lambda q:json.loads(q.read_text(encoding='utf-8-sig'));h=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda q:subprocess.check_output(['git','cat-file','blob',head+':'+q])
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert len(jobs)==len(items)==1
job=jobs[0];item=items[0];assert run['head_sha']==head and run['run_attempt']==1 and run['status']=='completed' and run['conclusion']=='success'
assert run['id']==rid and run['name']=={'ci':'Validate CLR implementation','original':'Pinned OTP differential oracle'}[kind]
assert job['head_sha']==head and job['status']=='completed' and job['conclusion']=='success' and all(s['conclusion'] in ['success','skipped'] for s in job['steps'])
assert item['workflow_run']['head_sha']==head and not item['expired']
z=d/f"artifact-{item['id']}.zip";ex=d/f"artifact-{item['id']}";assert z.stat().st_size==item['size_in_bytes'] and h(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as archive:
 for name in archive.namelist():
  assert not Path(name).is_absolute() and '..' not in Path(name).parts
  if not name.endswith('/'):assert archive.read(name)==(ex/name).read_bytes()
manifest=j(d/'download-manifest.json');assert {x.relative_to(d).as_posix() for x in d.rglob('*') if x.is_file() and x.name!='download-manifest.json'}=={x['Path'].replace('\\','/') for x in manifest}
for row in manifest:
 q=d/row['Path'];assert q.stat().st_size==row['Bytes'] and h(q.read_bytes())==row['Sha256']
raw=(d/f"job-{job['id']}.log").read_bytes();log=raw.decode('utf-8-sig')
result={'Head':head,'Run':rid,'Job':job['id'],'Artifact':item['id'],'RawZipSha256':h(z.read_bytes()),'RawJobSha256':h(raw),'Attempt':1,'MainEdited':False,'DispatchOrRerun':False}

assert kind=='original' and rid==38732672128 and job['id']==116414052901
v=j(ex/'differential.json');oldpath='docs/validation/part-301/previous-head300-original/pack300-watch-38727291159-terminal/artifact-11969214226/differential.json';old=json.loads(blob(oldpath))
assert v['Planned']==v['Executed']==v['Passed']==5854 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
for key,count in [('Results',778),('DiagnosticResults',165)]:assert len(v[key])==count and v[key]==old[key] and all(x['Passed'] for x in v[key])
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack']:assert v[key]==old[key]
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
track=dict(v['CompiledModuleTrack']);assert all(track.pop(k)==4911 for k in ['Planned','Executed','Passed']) and track.pop('Failed')==0
oldtrack={k:q for k,q in old['CompiledModuleTrack'].items() if k not in ['Planned','Executed','Passed','Failed']};assert track==oldtrack
report_path=Path(sys.argv[3]);report=j(report_path);assert report['Head']==report['HeadAfter']==head and report['SourceFreezeSha256']=='A763E55596D771736233F06A197ED46485013D8EFD0D01D13B854BE44AFAED90' and report['SourceFreezePathsVerifiedBefore']==report['SourceFreezePathsVerifiedAfter']==14
local={row['Name']:row for row in report['OrderedCases']};assert len(local)==9 and len(report['ActualInputDlls'])==3
const=dict(re.findall(r'public const string (\w+) = "(process-info-default-subset-[^"]+)";',blob('tools/Erlang.Differential/ProcessInfoDefaultSubsetCaseNames.cs').decode()))
catalog=blob('tools/Erlang.Differential/ProcessInfoDefaultSubsetCases.cs').decode();matches=re.findall(r'ProcessInfoDefaultSubsetCaseNames\.(\w+),\s*("(?:[^"\]|\.)*"),',catalog)
newcases={const[name]:json.loads(source) for name,source in matches};assert len(newcases)==8 and set(local)==set(newcases)|{'erlang-system-profile-dead-receiver'}
oldnames=[x['Name'] for x in old['ModuleResults']];newnames=[x['Name'] for x in v['ModuleResults']];assert len(v['ModuleResults'])==len(set(newnames))==4911 and [name for name in newnames if name in set(oldnames)]==oldnames and [name for name in newnames if name not in set(oldnames)]==list(newcases)
actual={row['Name']:row for row in v['ModuleResults']};differences=[];migration_count=0;map_variances=0
ledger=json.loads(blob('docs/validation/part-301/system-profile-fixture-provenance/fixture-migration-ledger.json'))
for before in old['ModuleResults']:
 after=actual[before['Name']]
 if before==after:continue
 changed={k for k in set(before)|set(after) if before.get(k)!=after.get(k)};differences.append({'Name':before['Name'],'ChangedFields':sorted(changed),'Historical':before,'Actual':after})
 if before['Name']=='erlang-system-profile-dead-receiver':
  assert changed=={'Source','SourceSha256','CSharpSha256'} and before['Source'].count(ledger['OldBody'])==1 and after['Source']==before['Source'].replace(ledger['OldBody'],ledger['NewBody']) and after['SourceSha256']==h(after['Source'].encode())
  assert after['Source']==local[after['Name']]['Source'] and after['SourceSha256']==local[after['Name']]['SourceSha256'] and after['CSharpSha256']==local[after['Name']]['CSharpSha256'];migration_count+=1
 else:
  assert before['Name']=='erlang-term-display-map' and changed=={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
  canonical='237B613D3E7B307D2C623D3E2241227D0D0A';alternative='237B623D3E2241222C613D3E7B307D7D0D0A'
  assert before['ExpectedStandardOutputHex']==after['ExpectedStandardOutputHex']==after['SdkStandardOutputHex']==canonical and after['ExpectedStandardOutputAlternatives']==[alternative]
  assert before['NativeStandardOutputHex'] in [canonical,alternative] and after['NativeStandardOutputHex'] in [canonical,alternative] and after['NativeAndSdkStandardOutputExactlyMatch']==(after['NativeStandardOutputHex']==canonical)
  assert b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in blob('tools/Erlang.Differential/CompiledStandardOutputContract.cs');map_variances+=1
assert migration_count==1 and map_variances in [0,1]
for name,source in newcases.items():
 row=actual[name];own=local[name];assert row['Source']==source==own['Source'] and row['SourceSha256']==h(source.encode())==own['SourceSha256'] and row['CSharpSha256']==own['CSharpSha256']
 assert row['Expected']==row['FixtureExpected']==row['Actual']=='{ok,done}' and row['CallerContext']=='UntrappedFresh' and row['CompilerPolicy']=='Default' and row['NativeAndSdkStandardOutputExactlyMatch'] and row['StandardStreamsMatch'] and row['Passed']
 assert all(row[k] is None for k in ['ExpectedStandardOutputHex','ExpectedStandardOutputAlternatives','ExpectedStandardErrorHex','NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
for row in v['ModuleResults']+v['DiagnosticResults']:assert row['Passed'] and h(row['Source'].encode())==row['SourceSha256']
fd=j(ex/'fd-differential.json');oldfd=json.loads(blob(oldpath.replace('differential.json','fd-differential.json')));assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
for row in fd['Results']:
 assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
 assert base64.b64decode(re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1)).hex()=='83680277026f6b7704646f6e65'
assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
delta=a/'pack301-original-approved-historical-differences.json';assert not delta.exists();delta.write_text(json.dumps(differences,indent=2)+'\n')
result.update(Status='PASS_EXACT_HEAD301_ORIGINAL5854_FD8_EIGHT_NEW_AND_SINGLE_FIXTURE_MIGRATION',Passed=5854,Failed=0,Expressions=778,CompiledModules=4911,Diagnostics=165,FdPassed=8,Historical4902SourceRowsExact=True,HistoricalOtherRowsExactExceptPredeclaredMapStdout=True,ApprovedHistoricalSourceGeneratedMigrations=1,NewCompiledCases=8,NineActualLocalGeneratedHashesMatch=True,ActualLocalGeneratedReportSha256=h(report_path.read_bytes()),PredeclaredNativeMapStdoutVarianceRows=map_variances,HistoricalRawDifferencesPreserved=True,RawNormalization=False,SixExclusionsContextPolicyExact=True,ReferenceCoverageComplete=False)
out=a/f'pack301-original-{rid}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
