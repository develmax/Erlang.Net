from pathlib import Path
import base64, hashlib, json, re, subprocess, sys, zipfile
r=Path.cwd();a=r/'artifacts';head='9c5b8a809b2a6bcd76965cdeae2d9dbd2ec873e2';rid=int(sys.argv[1]);kind=sys.argv[2];d=a/f'pack291-watch-{rid}-terminal'
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
if kind=='ci':
 tp=list(ex.rglob('tests.json'));ip=list(ex.rglob('integration-results.json'));assert len(tp)==len(ip)==1
 t=j(tp[0]);expected=json.loads(blob('docs/validation/part-291/tests.json'));assert t['Passed']==6599 and t['Failed']==0 and all(x['Status']=='Passed' for x in t['Tests'])
 names=[x['Name'] for x in t['Tests']];assert len(names)==len(set(names))==6599 and names==[x['Name'] for x in expected['Tests']]
 old=json.loads(blob('docs/validation/part-289/tests.json'));oldnames=[x['Name'] for x in old['Tests']];assert [n for n in names if n in set(oldnames)]==oldnames
 assert len(oldnames)==6588 and len([n for n in names if n.startswith('mfa/')])==502
 integration=j(ip[0]);assert integration==json.loads(blob('docs/validation/part-291/integration-results.json')) and len(integration)==11
 assert 'Validation passed' in log
 result.update(Status='PASS_EXACT_HEAD291_CI6599_FULL',Passed=6599,Failed=0,OrderedNamesExact=True,Historical6588OrderedNamesExact=True,Integration=integration,RegisteredMfas=502)
else:
 v=j(ex/'differential.json');old=json.loads(blob('docs/validation/part-290/original-289-38685834317/root-ordinary289-38685834317-terminal/artifact-11949943361/differential.json'))
 assert v['Planned']==v['Executed']==v['Passed']==5846 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
 assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
 for key,count in [('Results',778),('ModuleResults',4903),('DiagnosticResults',165)]:assert len(v[key])==count and all(x['Passed'] for x in v[key])
 for key in ['Results','OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation']:assert v[key]==old[key]
 assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
 for key,count in [('ExpressionTrack',778),('CompiledModuleTrack',4903),('CompilerDiagnosticTrack',165)]:
  track=v[key];assert track['Planned']==track['Executed']==track['Passed']==count and track['Failed']==0
 for key in ['ExpressionTrack','CompiledModuleTrack','CompilerDiagnosticTrack']:
  for field in ['Mode','ReferenceMode','ImplementationMode']:
   if field in old[key]:assert v[key][field]==old[key][field]
 for key,oldcount in [('ModuleResults',4895),('DiagnosticResults',163)]:
  historical={x['Name']:x for x in old[key]};current={x['Name']:x for x in v[key]};assert len(historical)==oldcount and len(current)==len(v[key])
  differences=[{'Name':n,'Historical':row,'Actual':current.get(n)} for n,row in historical.items() if current.get(n)!=row]
  if differences:(a/f'pack291-{key}-historical-differences.json').write_text(json.dumps(differences,indent=2)+'\n')
  assert not differences,'Historical rows changed; preserve differences'
  assert [x['Name'] for x in v[key] if x['Name'] in historical]==list(historical)
 for row in v['ModuleResults']+v['DiagnosticResults']:assert h(row['Source'].encode())==row['SourceSha256']
 def constants(path):return {n:json.loads(s) for n,s in re.findall(r'const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")',blob(path).decode())}
 c=constants('tools/Erlang.Differential/NifDeclarationCaseNames.cs')
 declarations=[(json.loads(n),json.loads(b)) for n,b in re.findall(r'Add\(\s*("(?:[^"\\]|\\.)*")\s*,\s*("(?:[^"\\]|\\.)*")',blob('tools/Erlang.Differential/NifDeclarationCases.cs').decode())]
 expected_declarations=[('absent-fallback',''),('empty-fallback','-nifs([]). '),('private-fallback','-nifs([fallback/0]). '),('tuple-form-fallback','-nifs([{fallback,0}]). '),('duplicate-function-fallback','-nifs([fallback/0,fallback/0]). '),('repeated-attribute-fallback','-nifs([fallback/0]). -nifs([run/0,fallback/0]). '),('empty-then-nonempty-fallback','-nifs([]). -nifs([fallback/0]). '),('nonempty-then-empty-fallback','-nifs([fallback/0]). -nifs([]). ')]
 assert declarations==expected_declarations
 previous={x['Name'] for x in old['ModuleResults']};new=[x for x in v['ModuleResults'] if x['Name'] not in previous];assert len(new)==8
 for row,(suffix,attribute) in zip(new,declarations):
  assert row['Name']==c['Prefix']+suffix and row['Source']==c['ModulePrefix']+attribute+c['Body']
  assert row['Expected']==row['FixtureExpected']==row['Actual']=='{ok,{kept,[],false}}' and row['Passed']
  assert row['CallerContext']=='UntrappedFresh' and row['CompilerPolicy']=='Default' and row['StandardStreamsMatch'] and row['NativeAndSdkStandardOutputExactlyMatch']
  assert all(row[k] is None for k in ['ExpectedStandardOutputHex','ExpectedStandardOutputAlternatives','ExpectedStandardErrorHex','NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
  assert re.fullmatch('[0-9A-F]{64}',row['CSharpSha256'])
 expected_diagnostics=[('nif-declaration-undefined-local','-module(nif_declaration_rejection). -export([run/0]). -nifs([missing/7]). run()->ok.','{error,[{undefined_nif,{missing,7}}]}','Undefined nif missing/7','NifUndefined'),('nif-declaration-invalid-integer-entry','-module(nif_declaration_rejection). -export([run/0]). -nifs([17]). run()->ok.','{error,[{invalid_fa_attribute,17}]}','Badly formed nifs function attribute','NifInvalidEntry')]
 previous={x['Name'] for x in old['DiagnosticResults']};diagnostics=[x for x in v['DiagnosticResults'] if x['Name'] not in previous];assert len(diagnostics)==2
 for row,(name,source,expected,message,kind_value) in zip(diagnostics,expected_diagnostics):
  assert row['Name']==name and row['Source']==source and row['Expected']==row['Reference']==expected
  assert not row['ImplementationAccepted'] and row['ImplementationCode']=='ERL005' and row['ImplementationMessage']==message and row['DiagnosticKind']==kind_value and row['Passed']
 fd=j(ex/'fd-differential.json');oldfd=json.loads(blob('docs/validation/part-290/original-289-38685834317/root-ordinary289-38685834317-terminal/artifact-11949943361/fd-differential.json'))
 assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
 for row in fd['Results']:
  assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
  payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
 assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
 passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M)
 assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
 result.update(Status='PASS_EXACT_HEAD291_ORIGINAL5846_AND_FD8',Passed=5846,Failed=0,Expressions=778,CompiledModules=4903,Diagnostics=165,FdPassed=8,Historical4895ModuleRowsExact=True,Historical778Expressions163DiagnosticsSixExclusionsAndContextExact=True,NewEightCompiledAndTwoTypedDiagnosticsExact=True,NativeUnloadCounterCompared=False,ReferenceCoverageComplete=False)
out=a/f'pack291-{kind}-{rid}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
