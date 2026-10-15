from pathlib import Path
import json,re,hashlib,subprocess,zipfile
A=Path('artifacts');head='a10fc16c6e5392d69b6f84d17c396ac525a34104';sha=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'));blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p])
m=j(A/'part-280-differential-metadata.json');pr=j(A/'part-280-native269-ordinary-latest-provenance.json');d=j(A/'part-280-differential/differential.json');old=j(A/'part-275-differential/differential.json');fd=j(A/'part-280-differential/fd-differential.json');ofd=j(A/'part-275-differential/fd-differential.json')
for r in [m['Run'],pr['Run']]: assert r['id']==38659091374 and r['head_sha']==head and r['conclusion']=='success' and r['status']=='completed' and r['run_attempt']==1
job=pr['Job'];assert job['id']==116175807652 and job['head_sha']==head and job['run_id']==38659091374 and len(job['steps'])==10 and all(x['conclusion']=='success' for x in job['steps'])
z=A/'part-280-differential.zip';ar=m['Artifact'];assert ar['id']==11938668658 and ar['workflow_run']['head_sha']==head and not ar['expired'] and z.stat().st_size==ar['size_in_bytes'] and sha(z.read_bytes())==m['ZipSHA256'].upper()==ar['digest'][7:].upper()
with zipfile.ZipFile(z) as f:
 assert set(f.namelist())=={'differential.json','differential.progress.json','fd-differential.json'}
 for n in f.namelist():assert f.read(n)==(A/'part-280-differential'/n).read_bytes()
assert d['Planned']==d['Executed']==d['Passed']==5820 and d['Failed']==0 and d['Complete'] and d['VersionVerified'] and d['Baseline']=='OTP-29.1.1' and not d['InvestigationOnly'] and not d['ReferenceCoverageComplete']
for key in ['Results','DiagnosticResults','OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation']:assert d[key]==old[key]
assert len(d['Results'])==778 and len(d['DiagnosticResults'])==163 and len(d['OriginalOtpDefectExclusions'])==6
om={r['Name']:r for r in old['ModuleResults']};nm={r['Name']:r for r in d['ModuleResults']};names=[r['Name'] for r in d['ModuleResults']];oldnames=list(om);assert len(nm)==len(names)==4879 and len(om)==4871
source=blob('tools/Erlang.Differential/GarbageCollectionResultCases.cs').decode();params=blob('tools/Erlang.Differential/GarbageCollectionResultCaseNames.cs').decode();prefix=json.loads(re.search(r'Prefix\s*=\s*("[^"\n]+")',params).group(1));mp=json.loads(re.search(r'ModulePrefix\s*=\s*("[^"\n]+")',params).group(1));decl=re.findall(r'Add\(\s*("(?:[^"\\]|\\.)*")\s*,\s*("(?:[^"\\]|\\.)*")',source);assert len(decl)==8
expectednames=[prefix+json.loads(n) for n,b in decl];assert set(nm)-set(om)==set(expectednames) and [n for n in names if n not in expectednames]==oldnames
for n,b in decl:
 row=nm[prefix+json.loads(n)];assert row['Source']==mp+json.loads(b)+',done.' and row['Actual']==row['Expected']==row['FixtureExpected']=='{ok,done}' and row['Passed'] and row['CallerContext']=='UntrappedFresh' and row['CompilerPolicy']=='Default'
deltas=[]
for n in oldnames:
 if om[n]==nm[n]:continue
 delta={k:{'Historical':om[n].get(k),'Actual':nm[n].get(k)} for k in om[n].keys()|nm[n].keys() if om[n].get(k)!=nm[n].get(k)}
 assert n=='erlang-term-display-map' and set(delta)<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'} and nm[n]['NativeStandardOutputHex'] in [om[n]['ExpectedStandardOutputHex']]+om[n]['ExpectedStandardOutputAlternatives'] and nm[n]['Passed'] and nm[n]['StandardStreamsMatch'];deltas.append({'Name':n,'Delta':delta,'HistoricalFullRow':om[n],'ActualFullRow':nm[n]})
for row in d['ModuleResults']+d['DiagnosticResults']:assert row['Passed'] and sha(row['Source'].encode())==row['SourceSha256']
assert fd['Passed']==8 and fd['Failed']==0 and fd['Results']==ofd['Results']
log=A/'part-280-native269-ordinary-job.log';raw=log.read_bytes();assert sha(raw)==j(Path(str(log)+'.digest.json'))['SHA256'];txt=raw.decode('utf-8-sig');assert head in txt and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in txt and 'checking OTP version... 29.1.1' in txt
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',txt,re.M);assert passes==[x['Source'] for x in d['Results']]+['module '+n for n in names]+['diagnostic '+r['Name'] for r in d['DiagnosticResults']]
progress=j(A/'part-280-differential/differential.progress.json');assert progress['CompletedComparisons']==5656 and progress['Source']==d['ModuleResults'][-1]['Source'] and progress['NativeEvidence']['ExitCode']==0
paths=['tools/Erlang.Differential/GarbageCollectionResultCases.cs','tools/Erlang.Differential/GarbageCollectionResultCaseNames.cs','tools/Erlang.Differential/CompiledModuleCases.cs','src/Erlang.Runtime/GarbageCollectionResultRegistrations.cs','.github/workflows/differential.yml'];bindings=[{'Path':p,'Sha256':sha(blob(p)),'Bytes':len(blob(p))} for p in paths]
r={'Status':'PASS_BOUNDED_NATIVE_5820_FD8','Head':head,'Run':38659091374,'Job':116175807652,'Artifact':11938668658,'Passed':5820,'FdPassed':8,'AllOld5812ContractsAndOrderExactExceptPredeclaredRawMapVariance':True,'Old4871ModuleRowsExact':len(deltas)==0,'Old778Expressions163DiagnosesSixExclusionsExact':True,'NewEightNames':expectednames,'NewEightSourcesAndNativeSdkOutcomesExactPublished':True,'PredeclaredRawMapVariance':deltas,'NoNormalization':True,'ZipSha256':sha(z.read_bytes()),'RawJobSha256':sha(raw),'CommittedBindings':bindings,'ProgressScope':'5656 is final compiled precomparison snapshot, terminal report/full5820 rawpass establish complete count.','ReferenceCoverageComplete':False,'MainEdited':False,'GitEdited':False,'NativeRerun':False}
out=A/'native269-part280-original-independent-guard.json';out.write_bytes((json.dumps(r,indent=2)+'\n').encode());strict={'Status':'FAIL' if deltas else 'PASS','Scope':'Strict old4871 row equality','Differences':deltas};(A/'native269-part280-original-strict-raw-history-guard.json').write_bytes((json.dumps(strict,indent=2)+'\n').encode());print(out);print(sha(out.read_bytes()))
