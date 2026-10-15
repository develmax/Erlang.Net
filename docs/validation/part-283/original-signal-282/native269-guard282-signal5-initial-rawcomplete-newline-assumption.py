from pathlib import Path
import json,hashlib,subprocess,zipfile,importlib.util
A=Path('artifacts');head='fdaf994c21984b41c471670a2d89787b307425de';pin='ad05823719d77c8faee87348ea39513d4e2f99c5';sha=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'));blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p]);area=A/'part-282-signal-pinned-distribution-signal-observations';root=area/'distribution-signal-observations';m=j(A/'part-282-signal-pinned-distribution-signal-observations-metadata.json');pr=j(A/'part-282-native269-signal-latest-provenance.json');ar=m['Artifact'];job=pr['Job'];assert ar['id']==11940373669 and ar['workflow_run']['head_sha']==head and not ar['expired'];assert job['id']==116193426467 and job['run_id']==38664575229 and job['head_sha']==head and len(job['steps'])==10 and all(r['conclusion']=='success' for r in job['steps'])
for r in [m['Run'],pr['Run']]:assert r['id']==38664575229 and r['head_sha']==head and r['status']=='completed' and r['conclusion']=='success' and r['run_attempt']==1
z=A/'part-282-signal-pinned-distribution-signal-observations.zip';assert z.stat().st_size==ar['size_in_bytes'] and sha(z.read_bytes())==m['ZipSHA256'].upper()==ar['digest'][7:].upper()
with zipfile.ZipFile(z) as f:
 for n in f.namelist():
  if not n.endswith('/'):assert f.read(n)==(area/n).read_bytes()
report=j(root/'distribution-signal-observations.json');plan=j(root/'source-plan.json');assert report['Captured']==report['Planned']==5 and report['Pin']==pin and not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim'];assert plan['Scenarios']==[r['Name'] for r in report['Rows']] and plan['FreshObservationVMs']==5 and plan['ObservedSemanticExpectations'] is None
for n in ['source-plan.json','distribution_signal_observer.erl']:assert (root/n).read_bytes()==blob('tools/Erlang.DistributionSignalObservations/'+n)
assert sha((root/'distribution_signal_observer.erl').read_bytes())==plan['SourceSha256']==report['SourceSha256']
for r in report['Files']:
 raw=(root/r['Path']).read_bytes();assert len(raw)==r['Length'] and sha(raw)==r['Sha256']
evidence=j(root/'source-build-evidence.json');assert evidence==j(area/'beam-source-build-evidence.json') and evidence['SourceCommit']==pin and evidence['SourceTag']=='OTP-29.1.1' and evidence['SourceBuildCompleted'] and evidence['HarnessCommit']==head and evidence['RunId']=='38664575229' and evidence['RunAttempt']=='1';assert sha((root/'source-build.log').read_bytes())==evidence['SourceBuildLogSha256'] and (root/'source-build.log').read_bytes()==(area/'beam-otp-build.log').read_bytes()
spec=importlib.util.spec_from_file_location('etf',A/'part276-faithful-etf-parser.py');etf=importlib.util.module_from_spec(spec);spec.loader.exec_module(etf);runtime=etf.decode((root/'runtime.etf').read_bytes());rv=etf.project(runtime);assert rv[:3]==([50,57],[49,55,46,49],'jit') and bytes(rv[3]).decode()==evidence['InstalledOtpRoot']
for prefix in ['runtime','compile','disassemble']:
 assert (root/(prefix+'.exit')).read_text().strip()=='0' and (root/(prefix+'.raw-complete')).read_text()=='true'
typed=[];outbound=[];calls=0
for row in report['Rows']:
 case=row['Name'];p=root/case;assert row['Captured'] and row['Stage']=='complete' and row['Error'] is None and row['Process']=={'ExitCode':0,'TimedOut':False,'RawComplete':True};assert (p/'stage').read_bytes()==b'complete'
 for n in plan['RequiredArtifacts']:assert (p/n).is_file()
 facts={n:etf.decode((p/n).read_bytes()) for n in plan['RequiredArtifacts'] if n.endswith('.etf')};ops=etf.project(facts['operations.etf']);inputs=[v for k,v in ops if k=='input'];assert len(inputs)==5 and all(v[2]==('ok','ok') for v in inputs);calls+=len(inputs)
 for control,packet,result in inputs:
  data=bytes.fromhex(packet['BytesHex']);assert data[0]==112 and etf.project(etf.decode(data[1:]))==control
 outputs=etf.project(facts['outputs.etf']);decoded=[]
 for k,v in outputs:
  assert v[0]=='ok'
  if v[1]=='none':decoded.append({'Label':k,'Kind':'none'});continue
  assert isinstance(v[1],list) and all(x['Type']=='Binary' for x in v[1]);data=b''.join(bytes.fromhex(x['BytesHex']) for x in v[1]);assert data[0]==112;control=etf.decode(data[1:]);decoded.append({'Label':k,'Kind':'iodata','RawFrameHex':data.hex(),'TypedControl':control})
 typed.append({'Case':case,'FullTypedArtifacts':facts,'ProjectedOperations':ops});outbound.append({'Case':case,'Outputs':decoded})
log=A/'part-282-native269-signal-job.log';raw=log.read_bytes();assert sha(raw)==j(Path(str(log)+'.digest.json'))['SHA256'];txt=raw.decode('utf-8-sig');assert pin in txt and head in txt and 'PINNED_BEAM_INSTALL_VERIFIED 29.1.1' in txt and 'Capture five original signal scenarios' in txt
bindings=[]
for q in sorted(Path('tools/Erlang.DistributionSignalObservations').rglob('*')):
 if q.is_file() and q.suffix in ['.cs','.csproj','.erl','.json'] and not any(x in q.parts for x in ['bin','obj']):
  p=q.as_posix();b=blob(p);bindings.append({'Path':p,'Bytes':len(b),'Sha256':sha(b)})
assert len(bindings)==17
out={'Status':'PASS_SOURCE_BOUND_GENUINE_SIGNAL5_CAPTURE_TYPED_AUDIT','Head':head,'Run':38664575229,'Job':116193426467,'Artifact':11940373669,'Captured':5,'IndependentScenarioVMs':5,'MetadataVMs':1,'PinnedBuilds':1,'NativeInputCallsAccepted':calls,'RuntimeTyped':runtime,'FullTypedCases':typed,'DecodedOutgoingFrames':outbound,'FilesInventoryExact':len(report['Files']),'ZipSha256':sha(z.read_bytes()),'RawJobSha256':sha(raw),'CommittedTool17Bindings':bindings,'ObservedFacts':['Outbound monitoring frames require both flags0x08 and0x20; all25 incoming calls accepted, individual/no flags suppress all outgoing drains.','Origin remote DOWN actual observer_remote_reason and fallback actual noconnection remain across four flag variants.','Named demonitor suppresses later named death output.','GROUP_LEADER accepts local/dead/external leaders, ordered self assignment, then third-node external leader survives observed controller normal teardown.'],'Bounds':['Capture+typed native facts do not prove corresponding SDK graph byte/identity parity without exact paired execution','Original metadata attests release/version/JIT/root only, no raw wordsize/build_type or optimized/LP64 attestation','No original Erlang API completeness claim/no retry/no normalization'],'MainEdited':False,'GitEdited':False,'HeavyExecuted':False};f=A/'native269-part282-signal5-independent-typed-guard.json';f.write_bytes((json.dumps(out,indent=2)+'\n').encode());print(f,sha(f.read_bytes()),len(report['Files']))
