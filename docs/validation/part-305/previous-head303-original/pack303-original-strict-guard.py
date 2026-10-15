from pathlib import Path
import base64, hashlib, json, re, subprocess, sys, zipfile
r=Path.cwd();a=r/'artifacts';head='82ef740f951e6076f095c62b4efd62507d74adc5';rid=int(sys.argv[1]);kind=sys.argv[2];d=a/f'pack303-watch-{rid}-terminal'
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
assert kind=='original' and rid==38736737943 and job['id']==116427844737
if False:raise AssertionError('CI handled by dedicated CI303 guard')
else:
 v=j(ex/'differential.json');oldpath='docs/validation/part-303/previous-head301-original/pack301-watch-38732672128-terminal/artifact-11972366494/differential.json';old=json.loads(blob(oldpath))
 assert v['Planned']==v['Executed']==v['Passed']==5854 and v['Failed']==0 and v['Complete'] and v['VersionVerified'] and v['Baseline']=='OTP-29.1.1' and not v['InvestigationOnly'] and not v['ReferenceCoverageComplete']
 assert all(v[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
 for key,count in [('Results',778),('ModuleResults',4911),('DiagnosticResults',165)]:
  assert len(v[key])==len(old[key])==count and all(x['Passed'] for x in v[key])
  differences=[{'Index':i,'Historical':p,'Actual':q} for i,(p,q) in enumerate(zip(old[key],v[key])) if p!=q]
  if differences:(a/f'pack303-{key}-historical-differences.json').write_text(json.dumps(differences,indent=2)+'\n')
  assert not differences,'Preserve strict historical differences; do not normalize'
 for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompiledModuleTrack','CompilerDiagnosticTrack']:assert v[key]==old[key]
 assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
 for row in v['ModuleResults']+v['DiagnosticResults']:assert h(row['Source'].encode())==row['SourceSha256']
 fd=j(ex/'fd-differential.json');oldfd=json.loads(blob(oldpath.replace('differential.json','fd-differential.json')))
 assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
 for row in fd['Results']:
  assert row['Passed'] and row['Native']==row['Sdk']=='{ok,done}' and row['ExitCode']==0 and row['StandardError']=='' and row['ActualOutputHex']==row['OutputHex']
  payload=re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',row['StandardOutput']).group(1);assert base64.b64decode(payload).hex()=='83680277026f6b7704646f6e65'
 assert head in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
 passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M)
 assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
 assert not subprocess.check_output(['git','diff','--name-only','ca33514ba537688aaa8a265200b8d270a01f98fb',head,'--','src/Erlang.Compiler/CodeGeneration.cs','tools/Erlang.Differential']).strip()
 result.update(Status='PASS_EXACT_HEAD303_ORIGINAL5854_AND_FD8',Passed=5854,Failed=0,Expressions=778,CompiledModules=4911,Diagnostics=165,FdPassed=8,Historical4911Module778Expression165DiagnosticRowsExact=True,SixExclusionsContextPolicyExact=True,CodeGenerationAndCorpusSourceUnchangedFrom301=True,NewPressureCorpus=False,ReferenceCoverageComplete=False)

out=a/f'pack303-{kind}-{rid}-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
