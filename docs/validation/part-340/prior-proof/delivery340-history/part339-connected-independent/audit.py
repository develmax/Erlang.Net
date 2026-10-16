import sys,importlib.util,json,hashlib,subprocess,zipfile
from pathlib import Path
sys.dont_write_bytecode=True
r=Path.cwd();o=r/'artifacts/part339-connected-independent'
sys.path.insert(0,str(o));from typed_etf import decode,packet
spec=importlib.util.spec_from_file_location('bindings',r/'artifacts/part339-remote-new-review/passive-common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();a=lambda s:('atom',s);nil=('nil',)
def ls(v):
 assert v==nil or v[0]=='list' and v[2]==nil
 return () if v==nil else v[1]
def text(v):return ''.join(map(chr,ls(v)))
def kv(v):return dict(ls(v))
controls=[]
def control(name,raw,expected):assert decode(bytes.fromhex(raw))==expected;controls.append(name)
control('small-int','83612A',42);control('signed-int','8362FFFFFFFF',-1);control('utf8-atom','8377026F6B',a('ok'));control('latin1-atom','836400026F6B',a('ok'));control('tuple','8368026101770161',(1,a('a')));control('nil','836A',nil);control('string-byte-list','836B00024142',('list',(65,66),nil));control('improper-list','836C000000016101770161',('list',(1,),a('a')));control('binary','836D000000024142',('binary','4142'))
for name,raw in [('truncated','8368026101'),('trailing','83610100'),('bad-version','006101'),('unsupported','8300')]:
 try:decode(bytes.fromhex(raw))
 except (ValueError,UnicodeError):controls.append(name)
 else:raise AssertionError(name)
assert decode(bytes.fromhex('836B000141'))!=decode(bytes.fromhex('83770141'));controls.append('atom-list-type-distinction')
mapping,d,run,job,items,rows=m.load('connected');assert run['conclusion']==job['conclusion']=='success';assert len(items)==1 and items[0]['id']==12068192649
cap=d/'artifact-12068192649/capture';obs=j(cap/'observations.json');prov=j(cap/'original-provenance.json');head=mapping['Head'];pin='ad05823719d77c8faee87348ea39513d4e2f99c5';assert obs['CollectorHead']==prov['CollectorHead']==head=='36e6da6dfc2696adce6bff8808ca96bbd46596b6';assert obs['PinnedOtpCommit']==prov['PinnedOtpCommit']==pin
stage=j(r/'artifacts/root-part339-stage-guard.json');pub=m.blobs(head,[x['Path'] for x in stage['Rows']]);assert len(pub)==31
for row in stage['Rows']:assert len(pub[row['Path']])==row['Bytes'] and h(pub[row['Path']])==row['Sha256']
tool='tools/Erlang.ConnectedSpawnOriginalObservations/';manifestRaw=pub[tool+'source-manifest.json'];manifest=json.loads(manifestRaw);assert h(manifestRaw)==obs['SourceManifestSha256']=='C942C5C8CD4150A72F353F255D035A11CB74703F51B9314AE3C770FA381A43BE'
for row in manifest['Files']:
 raw=pub[tool+row['Path']];assert len(raw)==row['Bytes'] and h(raw)==row['Sha256']
for row in manifest['ExternalDependencies']+[manifest['Workflow']]:
 raw=m.blobs(head,[row['Path']])[row['Path']];assert len(raw)==row['Bytes'] and h(raw)==row['Sha256']
assert len(prov['Source'])==5
for row in prov['Source']:
 raw=subprocess.check_output(['git','-C',str(r.parent/'otp'),'show',pin+':'+row['Path']]);assert len(raw)==row['Bytes'] and h(raw)==row['Sha256']
assert prov['OtpVersion']['Sha256']==h(b'29.1.1\n')
rt=(cap/'runtime-control.stdout').read_text();assert 'ROOT '+prov['ResolvedRoot'] in rt and 'OTP 29' in rt and 'ERTS 17.1' in rt and 'PRINTABLE latin1' in rt
for row in prov['Modules']:assert str(Path(row['Path']).as_posix()) in rt
allDecoded={};caseSummaries=[];rawCount=0
for case in obs['Cases']:
 name=case['Name'];folder=cap/name;assert j(folder/'observation.json')==case and case['Status']=='OBSERVED_AND_CLEANED' and not case['SdkExecuted'] and not case['ExpectedValuesPresent'] and not case['RawNormalization']
 for row in [case['Source'],case['CompileReceipt'],case['Beam'],case['RunReceipt']]+case['RawFiles']:
  raw=(folder/row['Path']).read_bytes();assert len(raw)==row['Bytes'] and h(raw)==row['Sha256']
 assert (folder/case['Source']['Path']).read_bytes()==pub[tool+'connected_spawn_probe.erl']
 assert {x['Path'] for x in case['RawFiles']}==set(manifest['RequiredRawFiles'][name]);rawCount+=len(case['RawFiles'])
 for role in ['compile','run']:
  receipt=j(folder/(role+'.process.json'));attempt=j(folder/(role+'.attempt.json'));assert receipt['Arguments']==attempt['Arguments'] and receipt['ExitCode']==0 and receipt['ParentReaped'] and receipt['StreamsClosed'] and receipt['CleanupFailure'] is None and not receipt['TerminationRequested'] and not receipt['RemainingGroupKillRequested'] and receipt['OwnedPid']==receipt['OwnedProcessGroup'] and receipt['ElapsedSeconds']<=attempt['TimeoutSeconds']+1
  assert attempt['NewOwnedSession'] and receipt['StdoutBytes']==(folder/(role+'.stdout')).stat().st_size and receipt['StderrBytes']==(folder/(role+'.stderr')).stat().st_size
 v={f.name:decode(f.read_bytes()) for f in folder.glob('*.etf')};allDecoded[name]=v
 assert v['outer.etf']==(a('result'),a('ok')) and v['stage.etf']==(a('complete'),a(name))
 assert v['controller-cleanup.etf']==v['tracer-cleanup.etf']==(a('observed'),a('normal'))
 if 'child-cleanup.etf' in v:assert v['child-cleanup.etf']==(a('observed'),a('normal'))
 runtime=v['runtime.etf'];assert runtime[0]==a('runtime') and text(runtime[1])=='29' and text(runtime[2])==prov['ResolvedRoot'] and runtime[3]==a('latin1') and runtime[4]==('binary',hashlib.md5((folder/case['Beam']['Path']).read_bytes()).hexdigest()) and text(runtime[5]).endswith('/'+name+'/beam/connected_spawn_probe.beam')
 setup=v['setup.etf'];assert setup[0]==a(name) and setup[3]==setup[4] and setup[5]==(a('dist_ctrlr'),setup[2],a('spawn_remote@localhost'),setup[2]);parent=setup[1]
 events=ls(v['trace-events.etf']);notes=[]
 if name.startswith('inbound') or name.startswith('outbound'):
  ctrl,payload=packet((folder/'request.packet').read_bytes());assert ctrl==v['request-control.etf'] and payload==v['request-arguments.etf'];ref=ctrl[1];assert ref[0]=='ref' and ctrl[2][0]=='pid';trace='trace' in name
  assert ctrl[0]==(30 if trace else 29)
  reply,none=packet((folder/'reply.packet').read_bytes());assert none==a('no_payload') and reply[0]==(32 if trace else 31) and reply[1]==ref and reply[2]==ctrl[2]
  if name.startswith('inbound'):
   assert v['input-result.etf']==a('ok') and v['reply-decoded.etf']==(reply,none) and payload==('list',(parent,),nil)
   bad='badopt' in name
   if bad:assert reply[3:5]==(0,a('badopt')) and v['child-state.etf']==a('no_child_expected')
   else:
    child=v['child-state.etf'];assert reply[3]==2 and reply[4]==child[0] and child[1]==ctrl[3];info=kv(child[2]);assert info[a('initial_call')]==ctrl[4] and info[a('links')]==nil;assert info[a('sequential_trace_token')]==(ctrl[6] if trace else nil)
    death,deathpayload=packet((folder/'child-monitor-exit.packet').read_bytes());assert death==(21,child[0],ctrl[2],ref,a('normal')) and deathpayload==a('no_payload')
   if trace:
    expected=[(a('receive'),(a('spawn_request'),)+ctrl[1:6]),(a('send'),(a('spawn_reply'),ref,a('error' if bad else 'ok'),reply[4]))]
    if not bad:expected.append((a('receive'),payload))
  else:
   assert v['reply-control.etf']==reply and v['reply-input-result.etf']==a('ok') and v['spawn-reply.etf']==(a('spawn_reply'),ref,a('ok'),reply[4]) and ctrl[2]==parent and payload==nil
   monitor='monitor' in name;info=kv(v['parent-state.etf']);assert info[a('priority_messages')]==a('true' if monitor else 'false')
   if monitor:
    assert reply[3]==2 and (a('process'),reply[4]) in ls(info[a('monitors')]);assert v['down.etf']==(a('custom_down'),ref,a('process'),reply[4],a('remote_done')) and v['alias-removal.etf']==a('true') and v['down-input-result.etf']==a('ok')
    down,no=packet((folder/'down.packet').read_bytes());assert down==(21,reply[4],parent,ref,a('remote_done')) and no==a('no_payload')
   else:assert reply[3]==0 and (a('process'),reply[4]) not in ls(info[a('monitors')])
   if trace:expected=[(a('send'),(a('spawn_request'),)+ctrl[1:6]),(a('send'),payload),(a('receive'),v['spawn-reply.etf'])]
  if trace:
   assert ctrl[6][0:2]==(3,77) and reply[5][0:2]==(3,77)
   for kind,message in expected:assert any(e[0]==a('seq_trace') and e[1]==77 and e[2][0]==kind and e[2][4]==message for e in events)
  else:assert not events
  notes=['request/reply reference and actor identity exact','monitor flag and child/parent state exact','TT core messages found among unnormalized real trace events' if trace else 'no trace events']
 elif name.startswith('alias_input'):
  mode,trace,child,alias,cleanup,sender=v['alias-identities.etf'];assert alias[0]=='ref' and child[0]=='pid' and cleanup!=alias;explicit=mode==a('explicit_unalias');replyMode=mode==a('reply_demonitor')
  for ix,label in enumerate(['first','second','third']):
   ctrl,payload=packet((folder/('alias-'+label+'.packet')).read_bytes());assert ctrl[:3]==(34 if trace==a('true') else 33,sender,alias) and payload==(a(['alias_first','alias_after_demonitor','alias_after_unalias'][ix]),mode,trace) and v['alias-'+label+'-input.etf']==a('ok')
  assert v['alias-first-selected.etf']==(a('alias_first'),mode,trace);assert v['alias-second-selected.etf']==((a('alias_after_demonitor'),mode,trace) if explicit else a('no_message'));assert v['alias-third-selected.etf']==a('no_message') and v['alias-demonitor.etf']==a('false' if replyMode else 'true') and v['alias-unalias.etf']==a('true' if explicit else 'false') and v['alias-extra-down.etf']==a('no_message')
  before=ls(v['alias-state-before.etf'][1]);after=ls(v['alias-state-selected.etf'][1]);assert before.count((a('process'),child))==2 and after.count((a('process'),child))==(1 if replyMode else 2)
  if trace==a('true'):assert any(e[0]==a('seq_trace') and e[1]==77 and e[2][0]==a('receive') and e[2][4]==v['alias-first-selected.etf'] for e in events)
  else:assert not events
  notes=['explicit alias survives demonitor only' if explicit else 'alias drops after '+mode[1],'reply_demonitor removes exactly one monitor before explicit demonitor' if replyMode else 'two child monitors before explicit demonitor','unalias blocks later delivery; no extra DOWN']
 else:
  ctrl,payload=packet((folder/'alias-output.packet').read_bytes());trace='trace' in name;assert (ctrl,payload)==v['alias-output-decoded.etf'] and ctrl[:3]==(34 if trace else 33,parent,v['alias-target.etf']) and payload==(a('outgoing_alias_payload'),a('true' if trace else 'false')) and v['alias-send-result.etf']==(a('ok') if trace else payload)
  if trace:assert ctrl[3]==(3,77,11,parent,0) and any(e[0]==a('seq_trace') and e[1]==77 and e[2][0]==a('send') and e[2][4]==payload for e in events)
  else:assert not events
  notes=['alias target, sender and payload exact','send/3 noconnect returns ok with TT34' if trace else 'send/2 returns payload with33']
 caseSummaries.append({'Name':name,'Status':'PASS_TYPED_ORIGINAL_OBSERVATION','RawFiles':len(case['RawFiles']),'TraceEventCount':len(events),'Assertions':notes})
assert len(caseSummaries)==obs['OriginalCases']==12 and [c['Name'] for c in obs['Cases']]==manifest['Cases']
with zipfile.ZipFile(d/'artifact-12068192649.zip') as z:members=len([n for n in z.namelist() if not n.endswith('/')])
(o/'typed-decoded.json').write_text(json.dumps(allDecoded,indent=2)+'\n')
local=j(r/'artifacts/connected-remote-spawn-alias-actual2/outcomes.json');assert len(local)==21 and all(x['Passed'] and x['Error'] is None for x in local)
report={'Status':'PASS_INDEPENDENT_PUBLISHED339_ORIGINAL12_TYPED_OBSERVATIONS','Head':head,'Run':mapping['Run'],'Job':mapping['Job'],'Artifact':items[0]['id'],'ArtifactApiDigest':items[0]['digest'],'ZipMembers':members,'DownloadManifestRows':len(rows),'PublishedBlobRows':31,'SourceManifestSha256':h(manifestRaw),'OriginalPinnedSourceRowsRehashed':5,'InstalledRuntimeBinaries':'Receipt hashes only: installed binaries not archived and not available for independent raw rehash; runtime/root/version/module paths are independently crosslinked to captured stdout and percase ETF.','Cases':caseSummaries,'RequiredRawFiles':rawCount,'IndependentDecoderControls':controls,'IndependentDecoderSha256':h((o/'typed_etf.py').read_bytes()),'TypedDecodedSha256':h((o/'typed-decoded.json').read_bytes()),'LocalCandidateCorrelation':{'Path':'artifacts/connected-remote-spawn-alias-actual2/outcomes.json','Sha256':h((r/'artifacts/connected-remote-spawn-alias-actual2/outcomes.json').read_bytes()),'Passed':21,'Scope':'Related source-reviewed local HOST contracts, not identical native fixtures or cross-runtime identity equality; no complete SDK/native comparison claimed.'},'SourceExecutionByAudit':False,'SdkExecutedByAudit':False,'NetworkUsedByAudit':False,'TcpInteroperabilityProven':False,'FullSdkCompatibilityProven':False}
(o/'guard.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'Status':report['Status'],'GuardSha256':h((o/'guard.json').read_bytes()),'Members':members,'Cases':12,'RawFiles':rawCount}))
