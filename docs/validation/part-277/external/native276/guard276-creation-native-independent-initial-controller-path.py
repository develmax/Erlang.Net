from pathlib import Path
import json,hashlib,subprocess,zipfile,importlib.util
A=Path('artifacts');B=A/'part-276-pinned-node-creation-boundaries';R=B/'node-creation-boundaries';HEAD='54c9ff90ee2a924792467289a28629e5e02be661';PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
j=lambda p:json.loads(p.read_bytes().decode('utf-8-sig'));sha=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda p:subprocess.check_output(['git','cat-file','blob',HEAD+':'+p])
ns={};exec((A/'part276-faithful-etf-parser.py').read_bytes(),ns);dec=ns['decode'];project=ns['project']
m=j(A/'part-276-pinned-node-creation-boundaries-metadata.json');pr=j(A/'part-276-creation-native-latest-provenance.json');job=pr['Job'];ar=m['Artifact']
for run in (m['Run'],pr['Run']):assert run['id']==38647831713 and run['head_sha']==HEAD and run['status']=='completed' and run['conclusion']=='success' and run['run_attempt']==1
assert job['id']==116139005760 and job['run_id']==38647831713 and job['head_sha']==HEAD and job['labels']==['ubuntu-latest'] and job['status']=='completed' and job['conclusion']=='success' and len(job['steps'])==10 and all(s['status']=='completed' and s['conclusion']=='success' for s in job['steps'])
z=A/'part-276-pinned-node-creation-boundaries.zip';assert sha(z.read_bytes())==m['ZipSHA256'].upper()==ar['digest'][7:].upper() and ar['id']==11933910357 and ar['workflow_run']['head_sha']==HEAD and not ar['expired'] and z.stat().st_size==ar['size_in_bytes']
with zipfile.ZipFile(z) as archive:
 files=[x for x in archive.namelist() if not x.endswith('/')]
 for n in files:assert archive.read(n)==(B/n).read_bytes()
report=j(R/'creation-boundary.json');plan=j(R/'source-plan.json');ev=j(R/'source-build-evidence.json');assert ev==j(B/'beam-source-build-evidence.json')
assert report['Pin']==plan['Pin']==ev['SourceCommit']==PIN and plan['SourceTag']==ev['SourceTag']=='OTP-29.1.1' and ev['SourceBuildCompleted'] and ev['HarnessCommit']==HEAD and ev['RunId']=='38647831713' and ev['RunAttempt']=='1'
assert sha((R/'source-build.log').read_bytes())==ev['SourceBuildLogSha256'] and (R/'source-build.log').read_bytes()==(B/'beam-otp-build.log').read_bytes()
assert blob('tools/Erlang.NodeCreationContrasts/source-plan.json')==(R/'source-plan.json').read_bytes();assert sha((R/'source-plan.json').read_bytes())=='BF8AF703AE81CD829FFFF55A910DBF51B70F594EFF70445EBE5DCDE8B6ECA481'
for source in plan['Sources']:
 raw=(R/source['File']).read_bytes();assert raw==blob('tools/Erlang.NodeCreationContrasts/'+source['File']) and sha(raw)==source['Sha256']==report['Sources'][source['File']]
assert project(dec((R/'runtime.etf').read_bytes()))==(list(b'29'),list(b'17.1'),'jit',list(ev['InstalledOtpRoot'].encode()))
for kind in ['runtime','compile']:
 assert (R/(kind+'.exit')).read_bytes().strip()==b'0' and (R/(kind+'.raw-complete')).read_bytes().strip()==b'true'
for f in report['Files']:
 raw=(R/f['Path']).read_bytes();assert len(raw)==f['Bytes'] and sha(raw)==f['Sha256']
assert report['Planned']==report['Captured']==report['FreshScenarioVMs']==6 and plan['FreshVmPerScenario'] and plan['FreshScenarioVMs']==6 and plan['AdditionalRuntimeAttestationVM']==1 and not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim'] and not report['NativeNetworkingUsed']
cases=plan['Scenarios'];assert [r['Scenario'] for r in report['Rows']]==cases
facts=j(A/'part276-creation-native-typed-facts.json')['Facts'];get=lambda c,n:project(facts[c][n+'.etf'])
expected={'truncated_zero':(1<<32,'none',4),'truncated_one':((1<<32)+1,'none',4),'maximum_small':((1<<59)-1,'none',(1<<32)-1),'first_big':(1<<59,'none',None),'retained_four':(1<<32,4,5),'retained_max_wrap':((1<<59)-1,(1<<32)-1,4)}
for row in report['Rows']:
 c=row['Scenario'];folder=R/c;requested,retained,selected=expected[c]
 assert row['ExitCode']==0 and row['RawComplete'] and not row['TimedOut'] and row['Captured'] and row['Stage']=='complete' and row['CollectorException'] is None
 assert len(plan['RequiredArtifacts'][c])==11 and all((folder/n).is_file() for n in plan['RequiredArtifacts'][c]) and (folder/'stage').read_bytes()==b'complete' and not (folder/'exception.etf').exists()
 assert get(c,'inputs')==(c,'creation_boundary@localhost',requested,retained) and get(c,'before')==get(c,'inactive')==('nonode@nohost',0,'false') and get(c,'kernel-down')=='killed'
 profile=get(c,'runtime-profile');assert profile==(8,'opt',list(b'x86_64-pc-linux-gnu'))
 if selected is None:
  out=get(c,'call-outcome');assert out[:3]==('exception','error','badarg') and len(out)==4 and len(out[3])>0 and get(c,'active')==('nonode@nohost',0,'false')
 else:assert get(c,'call-outcome')==('result','true') and get(c,'active')==('creation_boundary@localhost',selected,'true')
 owner=facts[c]['owner-after.etf'];assert owner['Type']=='Pid' and owner['Creation']==(selected or 0) and owner['Node']['Value']==('creation_boundary@localhost' if selected is not None else 'nonode@nohost')
 assert (folder/'retained-before.etf').read_bytes()==(folder/'retained-after.etf').read_bytes()
 if retained!='none':
  ref=facts[c]['retained-after.etf'];assert ref['Type']=='Reference' and ref['Creation']==retained and ref['Ids']==[173,29,53] and ref['Node']['Value']=='creation_boundary@localhost'
 else:assert get(c,'retained-before')==get(c,'retained-after')=='none'
log=(A/'part-276-creation-native-job.log').read_bytes();assert sha(log)==j(A/'part-276-creation-native-job.log.digest.json')['SHA256'];txt=log.decode('utf-8-sig');assert HEAD in txt and 'PINNED_BEAM_INSTALL_VERIFIED 29.1.1' in txt and txt.count('dotnet run --project tools/Erlang.NodeCreationContrasts --no-build --')==1
sourcepaths=['.github/workflows/node-creation-contrasts.yml','tools/Erlang.NodeCreationContrasts/CreationExporter.cs','tools/Erlang.NodeCreationContrasts/CreationEvaluation.cs','tools/Erlang.NodeCreationContrasts/CreationNames.cs','tools/Erlang.NodeCreationContrasts/source-plan.json','tools/Erlang.NodeCreationContrasts/creation_boundary_probe.erl','src/Erlang.Runtime/RuntimeNodeCreation.cs','src/Erlang.Runtime/RuntimeNodeCreationParameters.cs','src/Erlang.Runtime/ProcessRuntime.NodeAdmission.cs','src/Erlang.Runtime/ProcessRuntime.NodeController.cs','tests/Erlang.Tests/RuntimeNodeCreationHostChecks.cs','tests/Erlang.Tests/RuntimeNodeCreationHostParameters.cs','tests/Erlang.Tests/RuntimeNodeCreationHostNames.cs']
records=[{'Path':p,'Size':len(blob(p)),'Sha256':sha(blob(p))} for p in sourcepaths]
source=blob('tools/Erlang.NodeCreationContrasts/CreationExporter.cs').decode();assert 'foreach' in source and 'CreationEvaluation.Run' in source
ci=j(A/'part-276-validation/tests.json');host='runtime/node-creation-small-domain-low32-conversion';assert ci['Passed']==6544 and ci['Failed']==0 and sum(x['Name']==host and x['Status']=='Passed' for x in ci['Tests'])==1
params=blob('tests/Erlang.Tests/RuntimeNodeCreationHostParameters.cs').decode();assert 'FirstNonSmallShift = 59' in params and 'WireCreationShift = 32' in params and 'CollisionSuccessor = 5' in params and 'MinimumCreation = 4' in params
assert '((1L << WireCreationShift) + MinimumCreation, MinimumCreation)' in params
result={'Status':'PASS_NATIVE_CAPTURE_PROVENANCE_AND_TYPED_FACTS','Head':HEAD,'Run':38647831713,'Job':job['id'],'Artifact':ar['id'],'Steps':10,'SourcePin':PIN,'SourceTag':'OTP-29.1.1','OnePinnedSourceBuild':True,'FreshScenarioVMs':6,'AdditionalMetadataVM':1,'ZipFiles':len(files),'ReportedFiles':len(report['Files']),'RequiredRawArtifacts':66,'Runtime':['29','17.1','jit'],'ActualProfiles':{'Wordsize':8,'BuildType':'opt','Architecture':'x86_64-pc-linux-gnu'},'ActualSelectedCreation':{c:e[2] for c,e in expected.items()},'FirstBigActualException':'error/badarg with complete typed stack, inactive before and after','RetainedReferenceRawBeforeAfterExact':True,'AllSixKernelDownKilledAndInactive':True,'CommittedSourceBindings':records,'SdkComparison':{'ActualSameHeadCiHostPassed':host,'DirectPermanentCasesCover':'2^32, maximum SMALL, retained4 collision, uintMax collision wrap, BIG rejection; additional high-small matrices','Exact2Pow32Plus1HostCaseExecuted':False,'Exact2Pow32Plus1SourceAlignment':'native low1 floors4; shared production low32+minimum helper agrees, but current local HOST case uses2^32+4','PhysicalNativeGcOrInstructionCountClaim':False},'RawJobSha256':sha(log),'OutcomeNormalization':False,'Retries':0,'Historical274ProofRewritten':False,'EntireSdkCompatibilityClaim':False,'MainWritten':False,'RuntimeTestsRerun':False}
(A/'part276-creation-native-independent-guard.json').write_bytes((json.dumps(result,indent=2)+'\n').encode());print('PASS six native creation captures guardSHA '+sha((A/'part276-creation-native-independent-guard.json').read_bytes()))
