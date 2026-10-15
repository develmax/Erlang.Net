from pathlib import Path
import hashlib, json, subprocess, zipfile, runpy
A=Path('artifacts'); head='d45e62732d1cd101fc2ec7dfc1c5014942a6e2d0'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'))
blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p])
area=A/'part-284-trace-pinned-distribution-trace-observations'; root=area/'distribution-trace-observations'
m=j(A/'part-284-trace-pinned-distribution-trace-observations-metadata.json'); pr=j(A/'part-284-native269-trace-latest-provenance.json')
assert m['Run']['id']==pr['Run']['id']==38669679953 and pr['Job']['id']==116209593073
assert all(x['head_sha']==head and x['conclusion']=='success' for x in [m['Run'],pr['Run'],pr['Job']])
assert pr['Run']['run_attempt']==1 and len(pr['Job']['steps'])==10 and all(x['conclusion']=='success' for x in pr['Job']['steps'])
ar=m['Artifact']; z=A/'part-284-trace-pinned-distribution-trace-observations.zip'
assert ar['id']==11943731517 and ar['workflow_run']['head_sha']==head and not ar['expired']
assert z.stat().st_size==ar['size_in_bytes'] and sha(z.read_bytes())==m['ZipSHA256'].upper()==ar['digest'][7:].upper()
with zipfile.ZipFile(z) as f:
 for n in f.namelist():
  if not n.endswith('/'):assert f.read(n)==(area/n).read_bytes()
plan=j(root/'source-plan.json'); report=j(root/'distribution-trace-observations.json')
for n in ['source-plan.json','distribution_trace_probe.erl']:assert (root/n).read_bytes()==blob('tools/Erlang.DistributionTraceObservations/'+n)
assert sha((root/plan['Probe']).read_bytes())==plan['ProbeSha256']==report['SourceSha256']
assert plan['OtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and plan['FreshVmRequiredPerCase'] and not plan['SemanticAssertions']
assert report['Pin']==plan['OtpCommit'] and report['Captured']==report['Planned']==8 and not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim']
assert [x['Name'] for x in report['Rows']]==plan['Cases']
for x in report['Files']:
 b=(root/x['Path']).read_bytes(); assert len(b)==x['Length'] and sha(b)==x['Sha256']
e=j(root/'source-build-evidence.json'); assert e==j(area/'beam-source-build-evidence.json')
assert e['SourceBuildCompleted'] and e['SourceCommit']==plan['OtpCommit'] and e['HarnessCommit']==head and e['RunId']=='38669679953'
assert (root/'source-build.log').read_bytes()==(area/'beam-otp-build.log').read_bytes() and sha((root/'source-build.log').read_bytes())==e['SourceBuildLogSha256']
etf=runpy.run_path(str(A/'part276-faithful-etf-parser.py')); decode=etf['decode']; project=etf['project']
read=lambda d,n:decode((d/(n+'.etf')).read_bytes())
runtime=read(root,'runtime'); rp=project(runtime)
assert rp[:3]==([50,57],[49,55,46,49],'jit') and bytes(rp[3]).decode()==e['InstalledOtpRoot'] and rp[4:]==(8,[120,56,54,95,54,52,45,112,99,45,108,105,110,117,120,45,103,110,117],'opt')
for n in ['compile','runtime','disassemble']:
 assert (root/(n+'.exit')).read_bytes()==b'0\n' and (root/(n+'.raw-complete')).read_bytes()==b'true\n'
typed=[]
for row in report['Rows']:
 c=row['Name']; d=root/c
 assert row['Captured'] and row['Error'] is None and row['Process']=={'ExitCode':0,'TimedOut':False,'RawComplete':True}
 terms={n:read(d,n[:-4]) for n in plan['RequiredOutputs'] if n.endswith('.etf')}; p={n:project(v) for n,v in terms.items()}
 assert p['stage.etf']==('complete',c) and p['outer.etf']==('result','ok')
 assert bytes.fromhex(row['Stage'])==(d/'stage.etf').read_bytes()
 assert p['input-result.etf']=='ok'
 assert p['receiver-cleanup.etf']=='normal' and p['controller-cleanup.etf']=='normal' and p['tracer-cleanup.etf']=='killed'
 assert p['queued.etf'][0]==p['token-before.etf'] and p['queued.etf'][2]==('status','suspended')
 selected=p['selected.etf']; assert selected[0]=='selected' and len(selected)==6
 if c=='nil':assert selected[3:]==([],[],[])
 elif c=='undefined':assert selected[5]==p['token-before.etf'][1]
 elif c=='exit2_normal':assert p['queued.etf'][1]==('messages',[]) and selected[5]==[]
 else:
  assert selected[3]==('label','incoming_label') and selected[4]==('serial',(3,9)) and selected[5][0]==(1<<40 if c=='wide_flags' else 2)
 traces=p['tracer-mailbox.etf']; assert traces[0]=='messages' and len(traces[1])==(1 if c in ['send','registered','exit_link','exit2_trap'] else 0)
 typed.append({'Case':c,'Terms':terms,'RawFiles':[{'Path':q.name,'Bytes':q.stat().st_size,'Sha256':sha(q.read_bytes())} for q in sorted(d.iterdir()) if q.is_file()]})
log=A/'part-284-native269-trace-job.log'; raw=log.read_bytes();assert sha(raw)==j(Path(str(log)+'.digest.json'))['SHA256']
assert head.encode() in raw and plan['OtpCommit'].encode() in raw and b'PINNED_BEAM_INSTALL_VERIFIED 29.1.1' in raw
out=A/'native-audit-trace284-independent-guard.json'
out.write_text(json.dumps({'Status':'VERIFIED_ORIGINAL_TYPED8_CAPTURE_NOT_SDK_PARITY','Head':head,'Run':38669679953,'Job':116209593073,'Artifact':11943731517,'Pin':plan['OtpCommit'],'RuntimeTyped':runtime,'ObservedScenarioVMs':8,'AllOuterComplete':True,'AllOwnedCleanupObserved':True,'SdkParityClaim':False,'RawZipSha256':sha(z.read_bytes()),'RawJobSha256':sha(raw),'TypedScenarios':typed,'NoDispatchRepeated':True,'MainEdited':False,'GitEdited':False},indent=2)+'\n',encoding='utf-8')
sources=[p for p in sorted(area.rglob('*')) if p.is_file()]+[z,A/'part-284-trace-pinned-distribution-trace-observations-metadata.json',A/'part-284-native269-trace-latest-provenance.json',log,Path(str(log)+'.digest.json'),out,A/Path(__file__).name,A/'native-audit-trace284-initial-input-result-assumption.py',A/'native-audit-provenance284.ps1',A/'part276-faithful-etf-parser.py']
dest=A/'native-audit-part285-original-trace284-evidence-staging'; records=[]
for q in sources:
 raw=q.read_bytes(); target='docs/validation/part-285/original-trace-284/'+q.relative_to(A).as_posix(); t=dest/target;t.parent.mkdir(parents=True,exist_ok=True);t.write_bytes(raw);records.append({'SourcePath':q.as_posix(),'Destination':target,'Bytes':len(raw),'Sha256':sha(raw)})
manifest=dest/'manifest.json';manifest.write_text(json.dumps({'Stage':'DETACHED_ORIGINAL_TRACE284_FULL_TYPED8_CAPTURE','Files':records,'FileCount':len(records),'Bytes':sum(x['Bytes'] for x in records)},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Guard':str(out),'GuardSha256':sha(out.read_bytes()),'Manifest':str(manifest),'ManifestSha256':sha(manifest.read_bytes()),'Files':len(records),'Bytes':sum(x['Bytes'] for x in records)}))
