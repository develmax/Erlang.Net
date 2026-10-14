from pathlib import Path
import json,hashlib,subprocess,zipfile
A=Path('artifacts');B=A/'part-278-native269-queue-pinned-distribution-queue-contrasts';R=B/'distribution-queue-contrasts';HEAD='d7f10238d7f35146268617128729052e1af53c44';PIN='ad05823719d77c8faee87348ea39513d4e2f99c5';H=lambda b:hashlib.sha256(b).hexdigest().upper();J=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'));blob=lambda p:subprocess.check_output(['git','cat-file','blob',HEAD+':'+p])
ns={};exec((A/'part276-faithful-etf-parser.py').read_bytes(),ns);decode=ns['decode'];project=ns['project'];m=J(A/'part-278-native269-queue-pinned-distribution-queue-contrasts-metadata.json');pr=J(A/'part-278-native269-queue-latest-provenance.json');ar=m['Artifact'];job=pr['Job']
for run in [m['Run'],pr['Run']]:assert run['id']==38654983784 and run['head_sha']==HEAD and run['run_attempt']==1 and run['status']=='completed' and run['conclusion']=='success'
assert job['id']==116162502397 and job['run_id']==38654983784 and job['head_sha']==HEAD and job['labels']==['ubuntu-latest'] and job['conclusion']=='success' and len(job['steps'])==10 and all(x['status']=='completed' and x['conclusion']=='success' for x in job['steps'])
z=A/'part-278-native269-queue-pinned-distribution-queue-contrasts.zip';assert H(z.read_bytes())==m['ZipSHA256'].upper()==ar['digest'][7:].upper() and z.stat().st_size==ar['size_in_bytes'] and ar['id']==11937165627 and ar['workflow_run']['head_sha']==HEAD and not ar['expired']
with zipfile.ZipFile(z) as zip:
 files=[x for x in zip.namelist() if not x.endswith('/')];assert len(files)==len(set(files))
 for n in files:assert zip.read(n)==(B/n).read_bytes()
report=J(R/'distribution-queue.json');plan=J(R/'source-plan.json');ev=J(R/'source-build-evidence.json');assert ev==J(B/'beam-source-build-evidence.json') and ev['SourceCommit']==PIN and ev['SourceTag']=='OTP-29.1.1' and ev['HarnessCommit']==HEAD and ev['SourceBuildCompleted'] and ev['RunId']=='38654983784' and ev['RunAttempt']=='1'
assert (R/'source-build.log').read_bytes()==(B/'beam-otp-build.log').read_bytes() and H((R/'source-build.log').read_bytes())==ev['SourceBuildLogSha256'];assert (R/'source-plan.json').read_bytes()==blob('tools/Erlang.DistributionQueueContrasts/source-plan.json') and H((R/'source-plan.json').read_bytes())=='7330C72086B9E1522438BECED9E9A4FB17AAA172F1DBF0184CCCC8954241F292D22D' if False else True
assert H((R/'source-plan.json').read_bytes())=='7330C72086B9E1522438BECED9E9A4FB17AAA172F1DBBE4727C9C7910E443726' and (R/'source-plan.json').read_bytes()==blob('tools/Erlang.DistributionQueueContrasts/source-plan.json')
for src in plan['Sources']:
 b=(R/src['File']).read_bytes();assert b==blob('tools/Erlang.DistributionQueueContrasts/'+src['File']) and H(b)==src['Sha256']==report['Sources'][src['File']]
assert report['Pin']==plan['Pin']==PIN and report['Planned']==report['Captured']==report['FreshScenarioVMs']==7 and plan['FreshVmPerScenario'] and plan['FreshScenarioVMs']==7 and plan['AdditionalRuntimeAttestationVM']==1 and report['SdkExpectedUsed']==report['SemanticCompatibilityClaim']==report['NativeNetworkingUsed']==False
assert [r['Scenario'] for r in report['Rows']]==plan['Scenarios'] and sum(map(len,plan['RequiredArtifacts'].values()))==132
assert project(decode((R/'runtime.etf').read_bytes()))==(list(b'29'),list(b'17.1'),'jit',list(ev['InstalledOtpRoot'].encode()))
for k in ['runtime','compile']:assert (R/(k+'.exit')).read_bytes().strip()==b'0' and (R/(k+'.raw-complete')).read_bytes().strip()==b'true'
for f in report['Files']:
 b=(R/f['Path']).read_bytes();assert len(b)==f['Bytes'] and H(b)==f['Sha256']
facts=J(A/'native269-part278-queue-faithful-typed-facts.json')['Facts'];get=lambda c,n:project(facts[c][n+'.etf'])
for row in report['Rows']:
 c=row['Scenario'];d=R/c;assert row['ExitCode']==0 and not row['TimedOut'] and row['RawComplete'] and row['Stage']=='complete' and row['Captured'] and row['CollectorException'] is None
 assert all((d/n).is_file() for n in plan['RequiredArtifacts'][c]) and (d/'stage').read_bytes()==b'complete' and not (d/'exception.etf').exists()
 assert (d/'process.exit').read_bytes().strip()==b'0' and (d/'process.raw-complete').read_bytes().strip()==b'true'
 if c in ['small','large_binary','two_binaries']:
  assert get(c,'case')==c and get(c,c+'-empty-stat')==('ok',0,0,0) and get(c,c+'-after-stat')==('ok',0,1,0) and get(c,c+'-previous-option')=='false' and get(c,'cleanup')=='normal'
  data=facts[c][c+'-raw-data.etf'];segments=data['Values'][1]['Values'];wire=b''.join(bytes.fromhex(x['BytesHex']) for x in segments);assert wire==(d/(c+'.packet')).read_bytes();assert wire==b'\x70'+(d/(c+'-control.etf')).read_bytes()+(d/(c+'-payload.etf')).read_bytes()
  shape=get(c,c+'-wire-shape');assert shape[0]==shape[1]==len(wire) and shape[2]==[len(bytes.fromhex(x['BytesHex'])) for x in segments]
 else:
  profile=get(c,'profile');assert profile[:3]==(8,'opt',list(b'x86_64-pc-linux-gnu')) and profile[3:]==('queue_additive_local@localhost','true')
  assert get(c,'pending-stat')==('result',('ok',0,0,0))
observations={c:{n:project(v) for n,v in f.items() if any(k in n for k in ['stat','option','wire-shape','down','cleanup'])} for c,f in facts.items()}
for c,q,w in [('small',238,78),('large_binary',65808,65624),('two_binaries',16709,16477)]:assert get(c,c+'-queued-stat')==('ok',0,0,q) and get(c,c+'-wire-shape')[0]==w
assert get('two_buffers','both-queued-stat')==('result',('ok',0,0,66056)) and get('two_buffers','after-first-owner-stat')==('result',('ok',0,1,65810)) and get('two_buffers','after-second-owner-stat')==('result',('ok',0,2,0))
for n in ['nil-stat','atom-stat','pid-stat','old-after-reopen-stat']:
 x=get('handle_domain',n);assert x[:3]==('exception','error','badarg') and len(x[3])>0
for n in ['pending-stat','owner-stat','controller-stat','third-stat','old-after-close-stat','new-after-reopen-stat']:assert get('handle_domain',n)==('result',('ok',0,0,0))
assert get('input_counter','input-put-outcome')==('result','ok') and get('input_counter','delivery')==get('input_counter','input-payload') and get('input_counter','after-input-owner-stat')==('result',('ok',1,0,0))
assert get('input_malformed','input-put-outcome')==('result','ok') and get('input_malformed','input-controller-after-stat')==('result',('ok',1,0,0)) and get('input_malformed','after-input-owner-stat')==('result',('ok',0,0,0)) and get('input_malformed','malformed-controller-down')==('down','killed')
log=(A/'part-278-native269-queue-job.log').read_bytes();assert H(log)==J(A/'part-278-native269-queue-job.log.digest.json')['SHA256'];txt=log.decode('utf-8-sig');assert HEAD in txt and 'PINNED_BEAM_INSTALL_VERIFIED 29.1.1' in txt and txt.count('dotnet run --project tools/Erlang.DistributionQueueContrasts --no-build --')==1
paths=subprocess.check_output(['git','ls-tree','-r','--name-only',HEAD,'tools/Erlang.DistributionQueueContrasts'],text=True).splitlines()+['.github/workflows/distribution-queue-contrasts.yml']+['tools/Erlang.NodeIdentityContrasts/'+n+'.cs' for n in ['NodeProcess','NodeProcessResult','NodeCaptureProtocol','NodeDiagnostics','NodeNames','NodeErlangString','NodeStringSyntax','NodeFields','NodeModuleSource']];sources=[{'Path':p,'Bytes':len(blob(p)),'Sha256':H(blob(p))} for p in paths]
out={'Status':'PASS_INDEPENDENT_NATIVE_QUEUE278_CAPTURE_AND_TYPED_AUDIT','Head':HEAD,'Run':38654983784,'Job':116162502397,'Artifact':11937165627,'Pin':PIN,'ZipPhysicalFiles':len(files),'ReportedFileRecords':len(report['Files']),'RequiredRawArtifacts':132,'FreshScenarioVMs':7,'AdditionalRuntimeMetadataVM':1,'OnePinnedBuild':True,'AllSevenComplete':True,'SourceBindings':sources,'Observations':observations,'FaithfulTypedFactsSha256':H((A/'native269-part278-queue-faithful-typed-facts.json').read_bytes()),'Bounds':['Captured native evidence only; no SDK expected/physical queue allocator parity','Original all-three sequential33 plan preserved, not claimed executed; fresh derived sample3 separately','LP64 raw profile8 opt x86_64 independently observed in four additive cases; original three lack per-scenario profile and share same installed binary','Old handle after close succeeds zero; after reopen generation advances and old handle badarg','Malformed put returns ok and immediate controller in1; after killed teardown owner zero observed; full raw counts retained'],'MainWritten':False,'GitWritten':False,'RuntimeRerun':False};p=A/'native269-part278-queue-native-independent-guard.json';p.write_bytes((json.dumps(out,indent=2)+'\n').encode());print('PASS',H(p.read_bytes()),'ZIP',len(files),'Reported',len(report['Files']))
