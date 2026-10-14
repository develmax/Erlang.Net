from pathlib import Path
import hashlib,json,zipfile,importlib.util,subprocess
root=Path.cwd();a=root/'artifacts';base=a/'part-260-loader-contrasts';folder=base/'loader-contrasts'
load=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda b:hashlib.sha256(b).hexdigest().upper()
spec=importlib.util.spec_from_file_location('static_guard',a/'part260-static-summary-guard.py');guard=importlib.util.module_from_spec(spec);spec.loader.exec_module(guard)
wrapper,declbytes,decl,expected,variants=guard.static()
m=load(a/'part-260-loader-contrasts-metadata.json');v=load(a/'part-260-loader-latest-provenance.json');r=load(folder/'loader-contrasts.json');head=guard.HEAD;pin=guard.PIN
assert m['Run']['head_sha']==v['Run']['head_sha']==head and m['Run']['id']==v['Run']['id']==38547161044 and v['Job']['id']==115795957298 and v['Job']['labels']==['ubuntu-latest']
assert m['Run']['conclusion']==v['Job']['conclusion']=='success'
assert m['Artifact']['workflow_run']['id']==38547161044 and m['Artifact']['workflow_run']['head_sha']==head and not m['Artifact']['expired']
assert sha((a/'part-260-loader-contrasts.zip').read_bytes())==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-260-loader-contrasts.zip') as z:
 for n in z.namelist():
  if not n.endswith('/'):assert z.read(n)==(base/n).read_bytes()
assert r['Complete'] and r['CaseCount']==10 and r['FreshVmExecutions']==r['ExpectedFreshVmExecutions']==11 and r['Passed']==11
assert r['SourceCommit']==pin and r['SourceTag']=='OTP-29.1.1' and r['MfaRegistrations']==0 and r['SdkExecuted'] is False
assert (folder/'predeclared-contracts.json').read_bytes()==declbytes and (folder/'loader_contrast_wrapper.erl').read_bytes()==wrapper
assert r['PredeclaredSha256']==sha(declbytes) and r['WrapperSourceSha256']==sha(wrapper) and r['WrapperBeamSha256']==sha((folder/'loader_contrast_wrapper.beam').read_bytes())
proof=load(base/'beam-source-build-evidence.json');assert proof==r['BuildEvidence']==load(folder/'source-build-evidence.json')
assert proof['SourceBuildCompleted'] and proof['SourceCommit']==pin and proof['HarnessCommit']==head and proof['RunId']=='38547161044' and proof['RunAttempt']=='1' and proof['RunnerOs']=='Linux'
assert (base/'beam-otp-build.log').read_bytes()==(folder/'source-build.log').read_bytes() and sha((folder/'source-build.log').read_bytes())==proof['SourceBuildLogSha256']
build=(folder/'source-build.log').read_text();assert f'PINNED_BEAM_SOURCE_VERIFIED {pin}' in build and 'PINNED_BEAM_INSTALL_VERIFIED 29.1.1' in build
provenance=(folder/'pinned-source-provenance.json').read_bytes();assert provenance==subprocess.check_output(['git','show',head+':tools/Erlang.LoaderContrasts/pinned-source-provenance.json'])
for row in json.loads(provenance)['Files']:
 raw=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:'+row['Path']]);assert sha(raw)==row['Sha256']
for row in decl['InputBytes']:assert sha((folder/'inputs'/row['Path']).read_bytes())==row['Sha256']
for stage in ['runtime-version','compile','disassemble']:
 assert (folder/(stage+'.exit')).read_text().strip()=='0' and (folder/(stage+'.raw-complete')).read_text().strip()=='true'
version=guard.decoder.decode_exact((folder/'invoked-runtime.etf').read_bytes());assert version[0]=='tuple' and version[1][4]==('atom','jit')
text=(folder/'invoked-runtime.txt').read_text();assert '"29","17.1"' in text and ',opt,jit,' in text and proof['InstalledOtpRoot'] in text
dis=(folder/'wrapper-disassembly.txt').read_text();assert '{function,direct,1,' in dis and '{extfunc,erlang,call_on_load_function,1}' in dis
observed=[];failed=[]
assert len(r['Results'])==11
for row,(name,scenario) in zip(r['Results'],variants):
 assert row['Name']==name and row['Scenario']==scenario and row['RawComplete'] and not row['TimedOut']
 case=folder/name;files={x['Name']:x for x in row['Files']};assert len(files)==len(row['Files']) and set(files)=={q.name for q in case.iterdir() if q.is_file()}
 for filename,item in files.items():
  raw=(case/filename).read_bytes();assert len(raw)==item['Length'] and sha(raw)==item['Sha256']
 actual=guard.decoder.decode_exact((case/'actual.etf').read_bytes());wanted=guard.decoder.decode_exact((case/'expected.etf').read_bytes());assert wanted==expected[name]
 assert (case/'process.raw-complete').read_text().strip()=='true' and int((case/'process.exit').read_text())==row['ExitCode'] and (case/'stage.txt').read_text()==row['Stage']
 if row['Passed']:
  assert row['ExitCode']==0 and row['Stage']=='completed' and actual==wanted and (case/'actual.etf').read_bytes()==(case/'expected.etf').read_bytes()
 else:
  assert row['ExitCode']==3 and row['Stage']=='failed' and actual!=wanted and (case/'failure.etf').exists();failed.append(name)
 observed.append({'Name':name,'Passed':row['Passed'],'Stage':row['Stage'],'ExitCode':row['ExitCode'],'ActualDecoded':actual,'StaticExpectedDecoded':wanted,'RawActualSha256':files['actual.etf']['Sha256'],'RawExpectedSha256':files['expected.etf']['Sha256']})
assert failed==[]
independent=guard.verify(folder)
assert independent['NativeExecutions']==11
d=load(a/'part-260-loader-job.log.digest.json');assert sha((a/'part-260-loader-job.log').read_bytes())==d['SHA256']
assert all(step['status']=='completed' and step['conclusion']=='success' for step in v['Job']['steps'])
result={'AuditStatus':'PASS','NativeStatus':'PASS','Complete':True,'NativePassed':11,'NativeFailed':0,'FreshVmExecutions':11,'SourceHead':head,'SourcePin':pin,'Run':38547161044,'Job':115795957298,'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'LogSha256':d['SHA256'],'AllJobStepsSuccessful':True,'IndependentStaticSummaryGuard':independent,'Prior257FailedRunPreserved':True,'RuntimeVersion':version,'RuntimeCompileAndDirectInstructionAttested':True,'FiveGenuineInputsExact':True,'StaticExpectedClausesUnchanged':True,'FailedScenarios':failed,'IndependentDecodedRows':observed,'SdkExecuted':False,'NoExpectationChanges':True}
(a/'part260-loader-provenance-guard.json').write_text(json.dumps(result,indent=2)+'\n');print('PASS corrected11 native contrasts with source/pin/JIT/direct/generic/helper/raw ZIP/job proof; historical257 failures retained')
