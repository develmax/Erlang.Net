from pathlib import Path
import hashlib,importlib.util,json,subprocess,zipfile
r=Path.cwd();a=r/'artifacts';head='200ef8550d0b533f2a59eb872ac3949179994d7c';rid=38705231791;pin='ad05823719d77c8faee87348ea39513d4e2f99c5';d=a/f'pack294-watch-{rid}-terminal'
j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));h=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p])
spec=importlib.util.spec_from_file_location('pressure_etf',a/'pack294-etf-decoder.py');etf=importlib.util.module_from_spec(spec);spec.loader.exec_module(etf)
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert len(jobs)==len(items)==1;job=jobs[0];item=items[0]
assert run['name']=='Pinned OTP distribution pressure observations' and run['head_sha']==head and run['id']==rid and run['run_attempt']==1 and run['status']=='completed' and run['conclusion']=='success'
assert job['id']==116322900527 and job['head_sha']==head and job['status']=='completed' and job['conclusion']=='success' and all(s['conclusion']=='success' for s in job['steps'])
assert item['id']==11957883840 and item['workflow_run']['head_sha']==head and not item['expired']
z=d/f"artifact-{item['id']}.zip";ex=d/f"artifact-{item['id']}";assert z.stat().st_size==item['size_in_bytes'] and h(z.read_bytes())==item['digest'][7:].upper()
with zipfile.ZipFile(z) as archive:
 for name in archive.namelist():
  assert not Path(name).is_absolute() and '..' not in Path(name).parts
  if not name.endswith('/'):assert archive.read(name)==(ex/name).read_bytes()
download=j(d/'download-manifest.json');assert {p.relative_to(d).as_posix() for p in d.rglob('*') if p.is_file() and p.name!='download-manifest.json'}=={x['Path'].replace('\\','/') for x in download}
for row in download:
 b=(d/row['Path']).read_bytes();assert len(b)==row['Bytes'] and h(b)==row['Sha256']
o=ex/'distribution-pressure-observations';report=j(o/'distribution-pressure.json');plan=json.loads(blob('tools/Erlang.DistributionPressureObservations/source-plan.json'));policy=json.loads(blob('tools/Erlang.DistributionPressureObservations/expected-policy.json'))
assert report['Pin']==pin and report['Planned']==report['FreshScenarioVMs']==report['Captured']==3 and not report['SdkExpectedUsed'] and not report['SemanticCompatibilityClaim'] and not report['NativeNetworkingUsed']
assert [x['Scenario'] for x in report['Rows']]==plan['Scenarios'] and all(x['ExitCode']==0 and not x['TimedOut'] and x['RawComplete'] and x['Stage']=='complete' and x['Captured'] and x['CollectorException'] is None for x in report['Rows'])
assert {p.relative_to(o).as_posix() for p in o.rglob('*') if p.is_file() and p.name!='distribution-pressure.json'}=={x['Path'].replace('\\','/') for x in report['Files']}
for row in report['Files']:
 b=(o/row['Path']).read_bytes();assert len(b)==row['Bytes'] and h(b)==row['Sha256']
for name in ['distribution_pressure_probe.erl','expected-policy.json','source-plan.json']:
 assert (o/name).read_bytes()==blob('tools/Erlang.DistributionPressureObservations/'+name)
assert policy['Status']=='SOURCE_AUTHORED_BEFORE_ORIGINAL_EXECUTION' and policy['FreshVms']==3 and policy['IndependentTypedReviewRequired']
build=j(ex/'beam-source-build-evidence.json');assert build['SourceCommit']==pin and build['SourceTag']=='OTP-29.1.1' and build['SourceBuildCompleted'] and build['HarnessCommit']==head and build['RunId']==str(rid) and build['RunAttempt']=='1'
buildlog=(ex/'beam-otp-build.log').read_bytes();assert h(buildlog)==build['SourceBuildLogSha256'] and buildlog==(o/'source-build.log').read_bytes() and (ex/'beam-source-build-evidence.json').read_bytes()==(o/'source-build-evidence.json').read_bytes()
assert b'checking OTP version... 29.1.1' in buildlog
joblog=(d/f"job-{job['id']}.log").read_bytes();assert head.encode() in joblog and pin.encode() in joblog
def value(scenario,name):return etf.decode((o/scenario/name).read_bytes())
def process(prefix,directory):
 assert (directory/(prefix+'.exit')).read_text().strip()=='0'
 assert json.loads((directory/(prefix+'.raw-complete')).read_bytes()) is True
 assert (directory/(prefix+'.stderr')).read_bytes()==b''
for prefix in ['runtime','compile']:process(prefix,o)
assert (o/'distribution_pressure_probe.beam').is_file() and (o/'distribution_pressure_probe.beam').stat().st_size>0
runtime=etf.decode((o/'runtime.etf').read_bytes());assert runtime[0]==[50,57] and ''.join(map(chr,runtime[3]))==build['InstalledOtpRoot']
def stats(v):
 assert len(v)==2 and v[0]=='result' and len(v[1])==4 and v[1][0]=='ok'
 assert all(isinstance(x,int) and x>=0 for x in v[1][1:]);return v[1][1:]
def pid(v,node):assert isinstance(v,tuple) and len(v)==5 and v[0]=='PID' and v[1]==node;return v
def packets(scenario,name):
 rows=value(scenario,name);assert isinstance(rows,list);result=[]
 for index,row in enumerate(rows):
  assert len(row)==8 and row[0]==index and row[1][0]==row[2]==row[3]==len(row[4])
  assert etf.flat_iolist(row[1][1])==row[4] and row[4][0]==112
  control,used=etf.decode_prefix(row[4][1:]);assert control==row[5] and row[4][1+used:]==row[6]
  payload=None if row[6]==b'' else etf.decode(row[6]);stats(row[7]);result.append((control,payload))
 return result
cases={};setups={};fillers={}
for scenario in plan['Scenarios']:
 directory=o/scenario;process('process',directory);assert (directory/'stage').read_bytes()==b'complete' and value(scenario,'outer.etf')==('result','ok')
 assert all((directory/name).is_file() for name in plan['RequiredArtifacts'][scenario])
 launch=j(directory/'native-launch.json');assert launch['Executable']==build['InstalledOtpRoot']+'/bin/erl' and launch['Arguments'][:4]==['+zdbbl','1','+S','2:2']
 setup=value(scenario,'setup.etf');assert len(setup)==14 and setup[0]==scenario;owner=pid(setup[1],'pressure_local@localhost');controller=pid(setup[2],'pressure_local@localhost')
 assert setup[3]==setup[4] and setup[5]==('dist_ctrlr',controller,'pressure_remote@localhost',controller) and setup[7]=='pressure_local@localhost' and setup[8]==owner[4]==controller[4] and setup[9]==[50,57] and setup[12]==8
 assert setup[13]==1024 # Actual native +zdbbl1 setting; not an SDK capacity equality.
 barrier=value(scenario,'busy-barrier.etf');assert barrier[0]=='send_nosuspend_refused' and isinstance(barrier[1],int) and 0<barrier[1]<256 and stats(barrier[2])[2]>0
 filler=value(scenario,'fill-payload.etf');assert filler==('pressure_fill',bytes([0,128,255,1])*16384);fillers[scenario]=filler
 assert stats(value(scenario,'before-fill.etf'))[2]==0 and stats(value(scenario,'after-drain.etf'))[2]==0
 cleanup=value(scenario,'cleanup.etf');assert all(row[1]=='already_not_alive' or row[1:]==('killed_cleanup','killed') for row in cleanup)
 assert sum(row[0]==controller and row[1:]==('killed_cleanup','killed') for row in cleanup)==1
 setups[scenario]=setup;cases[scenario]={'ObservedBusyLimitBytes':setup[13],'SuccessfulFillSends':barrier[1],'BusyPendingBytes':stats(barrier[2])[2],'CleanupChildren':len(cleanup),'TypedOuterAndCleanupPassed':True}
def filler_control(owner):return (6,owner,etf.Atom(''),'pressure_sink')
s='same_controller_replies';setup=setups[s];remote,dead=value(s,'identities.etf');pid(dead,'pressure_local@localhost');assert remote==('PID','pressure_remote@localhost',11,0,1)
expected=[(3,dead,remote,'noproc'),(36,173,dead,remote)];assert value(s,'expected-controls.etf')==expected
for name,control,reads in [('missing_link',(1,remote,dead),1),('unlink_id',(35,173,remote,dead),2)]:
 assert value(s,name+'-control.etf')==control;packet=(o/s/(name+'.packet')).read_bytes();assert packet[0]==112 and etf.decode(packet[1:])==control
 put=value(s,name+'-put.etf');assert put[0]==('result','ok') and stats(put[1])[1]==stats(put[2])[1]==0 and stats(put[2])[0]==reads
before=value(s,'before-any-drain.etf');assert before[0]=='both_put_results_observed' and stats(before[1])[:2]==(2,0)
actual=packets(s,'required_replies-drain.etf');assert actual==[(filler_control(setup[1]),fillers[s])]*cases[s]['SuccessfulFillSends']+[(x,None) for x in expected]
cases[s].update(PutResultsBeforeAnyGetData=True,ExactRepliesInOrder=[3,36],ReplyDuplicateCount=0,Packets=len(actual))
s='producer_death_then_writer';setup=setups[s];producer,info,_=value(s,'producer-suspended.etf');assert info[0]==('status','suspended')
down=value(s,'producer-down-before-drain.etf');assert down[0][0]=='DOWN' and down[0][2:]==('process',producer,'killed') and down[1:3]==('false','undefined') and stats(down[3])[1]==0
writer,info,_=value(s,'next-writer-suspended.etf');assert writer!=producer and info[0]==('status','suspended')
assert value(s,'next-writer-sent.etf')==('producer_sent',writer,'surviving_writer',('pressure_marker','surviving_writer'))
released=packets(s,'release_capacity-drain.etf');after=packets(s,'after_writer_return-drain.etf');allpackets=released+after
expected=[(filler_control(setup[1]),fillers[s])]*cases[s]['SuccessfulFillSends']+[(filler_control(producer),('pressure_marker','cancelled_producer')),(filler_control(writer),('pressure_marker','surviving_writer'))]
assert allpackets==expected
cases[s].update(BothSendersObservedSuspended=True,KilledSenderDownBeforeDrain=True,KilledSenderMarkerPresentAfterDown=True,SurvivingWriterReturnedAndMarkerPresent=True,PacketInventoryOrder=['filler','cancelled_producer','surviving_writer'],FirstDrainPackets=len(released),LaterDrainPackets=len(after),ExactAdmissionInstantObserved=False)
s='terminal_public_free';setup=setups[s];target,remote,links=value(s,'established-link.etf');assert remote==('PID','pressure_remote@localhost',19,0,1) and remote in links[1]
control=(1,remote,target);assert value(s,'live_link-control.etf')==control and etf.decode((o/s/'live_link.packet').read_bytes()[1:])==control and value(s,'live_link-put.etf')[0]==('result','ok')
free=value(s,'public-free-before-drain.etf');assert free[:3]==(target,'false','undefined') and stats(free[3])[1]==0
down=value(s,'local-down-before-drain.etf');assert down[0][0]=='DOWN' and down[0][2:]==('process',target,'pressure_terminal_reason') and stats(down[1])[1]==0
expected=[(3,target,remote,'pressure_terminal_reason')];assert value(s,'expected-controls.etf')==expected
actual=packets(s,'terminal_exit-drain.etf');assert actual==[(filler_control(setup[1]),fillers[s])]*cases[s]['SuccessfulFillSends']+[(expected[0],None)]
cases[s].update(PublicLookupGoneBeforeDrain=True,LocalDownBeforeDrain=True,ExactTerminalExitAfterDrain=True,InternalDoneOrUnwoundCompared=False,Packets=len(actual))
result={'Status':'PASS_EXACT_HEAD294_ORIGINAL_THREE_TYPED_PRESSURE_OBSERVATIONS','Head':head,'Run':rid,'Job':job['id'],'Artifact':item['id'],'PinnedCommit':pin,'CasesPassed':3,'PinnedOriginalProbeCompiled':True,'FreshScenarioVms':3,'ReportFilesVerified':len(report['Files']),'DownloadFilesVerified':len(download),'RawZipSha256':h(z.read_bytes()),'RawJobSha256':h(joblog),'Cases':cases,'SdkHardBudgetEqualityClaimed':False,'ExactNativeAdmissionInstantClaimed':False,'GeneralDistributionCompatibility':False,'MainEdited':False,'NativeRerun':False}
out=a/'pack294-pressure-38705231791-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
