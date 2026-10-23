import json,hashlib,struct,datetime
from pathlib import Path
R=Path.cwd(); cookie='ddll452_owned_fixture_cookie'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def u32(b,o):return struct.unpack_from('>I',b,o)[0]
def digest(c,n):return hashlib.md5((c+str(n)).encode('latin1')).digest()
def term(obs,name,side='original'):
 p=[p for p in obs.glob('*-'+side+'.json') if p.name.split('-',1)[1]==name+'-'+side+'.json'];assert len(p)==1;return read(p[0])
def refs(t):
 if isinstance(t,dict):
  if t.get('Kind')=='ReferenceTerm':yield t
  for v in t.values():yield from refs(v)
 elif isinstance(t,list):
  for v in t:yield from refs(v)
rows=exact=authcount=negatives=familyhandles=0;groups=[];freezes=[];seen=set()
for run in [1,2,3,4,5]:
 root=R/f'artifacts/library452-probe-run{run}';finish=read(root/'completion.json')
 assert (finish['Passed'] if run!=3 else not finish['Passed']) and finish['InputsUnchanged'] and all(s['Equal'] and s['Files']==6427 for s in finish['ProviderScans'])
 freeze=read(root/'inputs-before.json');assert all(sha(Path(p))==h for p,h in freeze.items())
 ep=read(root/'epmd.exit.json');assert ep['ExitCode']==0 and ep['Settled'] and not ep['ForcedTermination']
 freezes.append({'Run':run,'Bindings':len(freeze),'Mismatch':0,'RuntimeSha256':finish['RuntimeSha256'],'Provider6427EqualBeforeAfter':True,'EpmdNormal':True})
 for group in finish['Groups']:
  if not group['Passed']:
   assert run==3 and (group['Depth'],group['Order'],group['Mode'])==(8,'outgoing','old_timer');continue
  depth,order,mode=group['Depth'],group['Order'],group['Mode']
  assert (depth,order,mode) not in seen;seen.add((depth,order,mode))
  base=root/f'depth{depth}-{order}-{mode}';obs=base/'observations';c=read(obs/'comparison.json');assert c['Passed'] and c['PhysicalCleanup'] and len(c['Results'])==53
  for row in c['Results']:
   left=[p for p in obs.glob('*-original.etf') if p.name.split('-',1)[1]==row['Name']+'-original.etf'];right=[p for p in obs.glob('*-managed.etf') if p.name.split('-',1)[1]==row['Name']+'-managed.etf'];assert len(left)==len(right)==1 and row['Passed']
   same=left[0].read_bytes()==right[0].read_bytes();assert same==row['WholeRawEqual'];rows+=1;exact+=same
  pick=read(obs/'check-pending-proof.json');assert pick['Passed'] and pick['OriginalStatus']=='ok_simultaneous' and pick['ActualDiscoveryBarrier'] and pick['ActualCheckPendingRow'] and pick['ActualCheckerExit']=='remarked' and pick['OriginalConnectWaiter'] and pick['SameRuntimePendingHandle'] and not pick['ManagedCheckPendingStateClaim']
  winner='simultaneous-outgoing';assert (obs/winner/'status.bin').read_bytes()==b'sok_simultaneous'
  checking=term(obs,'original-check-pending-row')['Items'];checkrow=checking[0]['Items'];checker=checking[1];connector=checking[2]
  assert checkrow[3]=={'Kind':'Atom','Name':'check_pending'} and checkrow[4]==checker and checkrow[5]==checkrow[6]=={'Kind':'Atom','Name':'undefined'}
  assert connector in [item['Items'][0] for item in checkrow[8]['Items']]
  finished=term(obs,'original-check-pending-finish')['Items'];up=finished[2]['Items'];assert finished[:2]==[{'Kind':'Atom','Name':'remarked'},{'Kind':'Atom','Name':'true'}] and up[3]=={'Kind':'Atom','Name':'up'} and up[2]==checkrow[2] and up[4]!=checker
  traces=term(obs,'original-check-pending-traces')['Items'];assert len(traces)==1
  pair=traces[0]['Items'];assert pair[0]['Items'][1]['Items']==[{'Kind':'Atom','Name':'accept_pending'},{'Kind':'Atom','Name':'ok_pending'}] and pair[1]==up[4]
  for phase in ['false','true']:
   name='already-pending-'+phase;proof=read(obs/(name+'-proof.json'));assert all(proof[k] for k in ['Passed','OriginalRowUnchanged','ManagedSnapshotUnchanged','NoStatus','NoChallenge','NoAdoption','FirstCandidateStillPending'])
   beforefull=term(obs,'original-'+name+'-before');afterfull=term(obs,'original-'+name+'-after');assert beforefull==afterfull
   brow=beforefull['Items'][0]['Items'];assert brow[3]=={'Kind':'Atom','Name':'up_pending'} and brow[4]==up[4] and brow[2]==up[2]
   ev=term(obs,'original-'+name+'-traces')['Items'];assert len(ev)==2
   assert [item['Items'][0]['Items'][1]['Items'][1]['Name'] for item in ev]==['up_pending','already_pending']
   assert ev[0]['Items'][1]==brow[6] and ev[1]['Items'][1]!=brow[6] and ev[1]['Items'][1]!=brow[4]
   p=obs/(name+'-extra');refusal=read(p/'candidate-refusal.json');assert refusal['OriginalClosedWithoutStatus'] and refusal['ReceivedStatusBytes']==0 and (refusal['PhysicalEof'] or refusal['PhysicalReset']) and not refusal['Adopted'] and not refusal['ChallengeReceived']
   assert not any((p/n).exists() for n in ['status.bin','challenge.bin','reply.bin','ack.bin','authentication.json'])
  first=obs/'already-pending-first-false';refusal=read(first/'candidate-refusal.json');assert refusal['OriginalClosed'] and not refusal['Adopted'] and (first/'status.bin').read_bytes()==b'salive' and (first/'alive-response.bin').read_bytes()==b'sfalse'
  for name in ['current-data-while-false-candidate-held','current-data-after-extra-refused-first-false','current-data-while-true-candidate-held']:
   for side in ['original','managed']:assert term(obs,name,side)=={'Kind':'Atom','Name':'true'}
  proof=read(obs/'late-discovery-owner-proof.json');assert proof['Passed'] and proof['Mode']==mode and proof['ActualSourceMessages'] and not proof['InjectedSetupMessage'] and not proof['InjectedTimeoutMessage']
  events=term(obs,'original-discovery-receive-events')['Items']
  received=[e['Items'][1] for e in events if e['Items'][0]=={'Kind':'Atom','Name':'received'}]
  if mode!='old_timer':
   ordered=term(obs,'original-actual-mailbox-order')['Items'];messages=ordered[0]['Items'];ai=int(ordered[1]['Decimal']);si=int(ordered[2]['Decimal']);am=ordered[3];sm=ordered[4]
   assert 0<ai<si<=len(messages) and messages[ai-1]==am and messages[si-1]==sm
   assert am['Items'][0]==up[4] and am['Items'][1]['Items'][0]=={'Kind':'Atom','Name':'accept_pending'} and am['Items'][1]['Items'][2]==checkrow[1]
   late=sm['Items'];assert late[0]=={'Kind':'Atom','Name':'setup_check'} and late[1]==checkrow[1] and late[2]==checker and late[3]['Kind']=='ReferenceTerm'
   assert received==[sm],(mode,received)
   assert not any(m['Kind']=='TupleTerm' and m['Items'][0]=={'Kind':'Atom','Name':'setup_check_timeout'} for m in messages)
   acceptor=term(obs,'original-real-acceptor-created')['Items'];assert acceptor[0]=={'Kind':'Atom','Name':'controller_sent'} and acceptor[1]['Items'][2]==up[4]
   result=late[4]['Items']
   if mode=='late_success':assert result[0]=={'Kind':'Atom','Name':'ok'} and result[1]['Kind']=='TupleTerm'
   else:assert result==[{'Kind':'Atom','Name':'error'},{'Kind':'TupleTerm','Items':[{'Kind':'Atom','Name':'unsupported_address_type'},checkrow[1]]}]
  else:
   assert received==[] and proof['NaturalTimerAfterActualHandoff']
   before_timer=term(obs,'original-row-before-natural-old-timer');after_timer=term(obs,'original-row-after-natural-old-timer');assert before_timer==after_timer
   before_path=list(obs.glob('*-original-row-before-natural-old-timer-original.etf'));after_path=list(obs.glob('*-original-row-after-natural-old-timer-original.etf'));assert len(before_path)==len(after_path)==1 and before_path[0].read_bytes()==after_path[0].read_bytes()
   now=before_timer['Items'][0]['Items'];assert now[3]=={'Kind':'Atom','Name':'up'} and now[4]!=up[4] and now[5]!=up[5] and now[2]!=up[2]
   timer=term(obs,'original-natural-old-timer')['Items'];msg=timer[0];assert msg=={'Kind':'TupleTerm','Items':[{'Kind':'Atom','Name':'setup_check_timeout'},checkrow[1],checker]}
   assert timer[1]==before_timer['Items'][0] and timer[2:]==[{'Kind':'Atom','Name':'false'},{'Kind':'Integer','Decimal':'5000'}]
   timer_events=term(obs,'original-natural-timer-receive-events')['Items'];assert [e['Items'][1] for e in timer_events if e['Items'][0]=={'Kind':'Atom','Name':'received'}]==[msg]
   for name in ['actual-data-before-old-timer','actual-data-after-old-timer']:assert term(obs,name)=={'Kind':'Atom','Name':'true'}
  reg=read(obs/'listener/registration.json');resp=(obs/'listener/alive2-response.bin').read_bytes();assert resp[:2]==bytes([118,0]) and u32(resp,2)==reg['Creation']
  channels=[]
  for conn in [winner,'alive-handoff','connection2']:
   p=obs/conn;ch=(p/'challenge.bin').read_bytes();reply=(p/'reply.bin').read_bytes();ack=(p/'ack.bin').read_bytes();hello=(p/'hello.bin').read_bytes();a=read(p/'authentication.json')
   assert len(reply)==21 and reply[:1]==b'r' and reply[5:]==digest(cookie,u32(ch,9));assert len(ack)==17 and ack[:1]==b'a' and ack[1:]==digest(cookie,u32(reply,1))
   local=u32(ch,13) if a['Incoming'] else u32(hello,9);remote=u32(hello,9) if a['Incoming'] else u32(ch,13)
   assert local==reg['Creation']==a['LocalCreation'] and remote==a['RemoteCreation'] and a['Authenticated']
   e=read(p/'cleanup.json');s=e['FinalSnapshot'];assert e['Passed'] and e['ControllerJoined'] and e['SocketDisposed'] and all(s[k]==0 for k in ['QueuedPacketBytes','QueuedPackets','QueuedSegments'])
   channels.append({'Directory':conn,'Authentication':a,'Incoming':e['Incoming'],'Outgoing':e['Outgoing'],'ConnectionId':s['ConnectionId']});authcount+=1
  p=obs/'alive-false';r=read(p/'candidate-refusal.json');assert r['OriginalStatus']=='alive' and r['AliveReply'] is False and r['OriginalClosed'] and not r['Adopted'];assert (p/'status.bin').read_bytes()==b'salive' and (p/'alive-response.bin').read_bytes()==b'sfalse' and not (p/'challenge.bin').exists()
  p=obs/'alive-wrong-cookie';r=read(p/'candidate-refusal.json');ch=(p/'challenge.bin').read_bytes();reply=(p/'reply.bin').read_bytes();assert r['WrongCookieRejected'] and not r['Adopted'] and r['AliveReply'] is True;assert reply[5:]!=digest(cookie,u32(ch,9)) and reply[5:]==digest(cookie+'_wrong',u32(ch,9)) and (p/'status.bin').read_bytes()==b'salive' and (p/'alive-response.bin').read_bytes()==b'strue' and not (p/'ack.bin').exists();negatives+=1
  for name in ['current-data-after-alive-false','current-data-after-alive-bad-cookie']:
   for side in ['original','managed']:assert term(obs,name,side)=={'Kind':'Atom','Name':'true'}
  handoff=read(obs/'alive-handoff-proof.json');assert handoff['Passed'] and handoff['OldControllerJoined'] and handoff['RetiredBeforeHostAckRead'] and handoff['SamePeerVmCreation'];assert handoff['Old']['ConnectionId']!=handoff['Current']['ConnectionId'] and handoff['Old']['RemoteCreation']==handoff['Current']['RemoteCreation']
  before=term(obs,'original-controller-before-alive')['Items'];after=term(obs,'original-controller-after-alive')['Items'];assert before[0]==after[0]=={'Kind':'Atom','Name':'connection'} and before[3]==after[3]=={'Kind':'Atom','Name':'up'} and before[2]!=after[2] and before[5]!=after[5]
  fatal=read(obs/'fatal-peer-stop.json');old=read(base/'peer.exit.json');assert fatal['Passed'] and fatal['KillRequested'] and fatal['EntireOwnedTree'] and fatal['OldPid']==old['Pid'] and old['ExpectedScenarioTermination'] and not old['ForcedCleanup'] and old['ExitCode']!=0 and old['Settled'];assert len(fatal['Family'])>0 and old['Pid'] in [f['Pid'] for f in fatal['Family']]
  assert len(set(f['Pid'] for f in fatal['Family']))==len(fatal['Family']) and all(f['Settled'] and datetime.datetime.fromisoformat(f['StartedUtc'].replace('Z','+00:00')) for f in fatal['Family']);familyhandles+=len(fatal['Family'])
  restart=read(obs/'actual-peer-restart.json');assert restart['Passed'] and restart['OldPid']==fatal['OldPid'] and restart['NewPid']!=restart['OldPid'] and restart['OldCreation']!=restart['NewCreation'] and restart['OldConnection']!=restart['NewConnection'] and restart['OldReferenceWireUnchanged'] and restart['OldReferenceCreation']==restart['OldCreation']
  assert channels[0]['Authentication']['RemoteCreation']==channels[1]['Authentication']['RemoteCreation']==restart['OldCreation'] and channels[2]['Authentication']['RemoteCreation']==restart['NewCreation']
  for file in ['primary.exit.json','restarted-peer/exit.json']:
   e=read(base/file);assert e['ExitCode']==0 and e['Settled'] and not e['ForcedTermination']
   if file.startswith('restarted'):assert e['PumpsJoined'] and e['Pid']==restart['NewPid']
  for p in [obs/'alive-handoff/stale-input-attempt.json',obs/'connection2/stale-input-attempt.json']:
   s=read(p);assert s['Passed'] and s['OneControllerTurn'] and s['CurrentActualInputHandler'] and all(s['Before'][k]==s['After'][k] for k in ['AcceptedInputPackets','QueuedPacketBytes','QueuedPackets','QueuedSegments'])
  for side in ['original','managed']:
   oldref=term(obs,'genuine-peer-reference-via-distribution',side);fresh=term(obs,'new-peer-reference',side);assert oldref['Kind']==fresh['Kind']=='ReferenceTerm' and oldref['Node']==fresh['Node'] and oldref['Creation']!=fresh['Creation']
   for name in ['foreign-reject-before-disconnect','foreign-reject-after-reconnect','old-peer-ref-rejected-by-new-peer']:
    e=term(obs,name,side);it=e['Items'];assert [v['Name'] for v in it[:3]]==['exception','error','badarg'] and len(it[3]['Items'])==max(1,min(depth,3)) and oldref in list(refs(e))
   assert term(obs,'new-peer-queue-survives-old-ref',side)=={'Kind':'Integer','Decimal':'1'} and term(obs,'new-peer-queue-zero-after-new-ref',side)=={'Kind':'Integer','Decimal':'0'} and term(obs,'peer-memory-lost-after-actual-restart',side)=={'Kind':'Atom','Name':'undefined'}
  groups.append({'Run':run,'Depth':depth,'Order':order,'Mode':mode,'Rows':53,'Exact':46,'ProtocolOnly':7,'Simultaneous':pick,'ListenerCreation':reg['Creation'],'Channels':channels,'OriginalControllerBefore':before[5],'OriginalControllerAfter':after[5],'Handoff':handoff,'Fatal':fatal,'ActualOldExit':old,'Restart':restart,'NativeCleanup':True})
assert rows==1272 and exact==1104 and authcount==72 and negatives==24
assert seen=={(d,o,m) for d in [0,1,2,8] for o in ['incoming','outgoing'] for m in ['late_success','late_error','old_timer']}
for name in ['beam_ddll_distribution452.beam','ddll452_epmd.beam']:
 expected=(R/'artifacts/library452-probe-run1/observer-ebin'/name).read_bytes()
 assert all((R/f'artifacts/library452-probe-run{run}/observer-ebin'/name).read_bytes()==expected for run in [2,3,4,5])
for name in ['Program.cs','DistributionTcp452.cs','DistributionTcp452.Initiator.cs','beam_ddll_distribution452.erl','ddll452_epmd.erl']:assert (R/'artifacts/library452-probe1'/name).read_bytes()==(R/'artifacts/library452-probe2'/name).read_bytes()
failure=read(R/'artifacts/library452-probe-run3/depth8-outgoing-old_timer/observations/comparison.json');assert not failure['Passed'] and failure['PhysicalCleanup'] and any('IOException' in x and 'ready' in x and 'being used by another process' in x for x in failure['Failures'])
bad_exit=read(R/'artifacts/library452-probe-run3/depth8-outgoing-old_timer/restarted-peer/construction-failure-exit.json');assert bad_exit['Settled'] and bad_exit['ForcedTermination']
out={'Passed':True,'ObservationRows':rows,'RawExact':exact,'ProtocolOnly':rows-exact,'PairedRows':1128,'AuxiliaryGuards':144,'CheckPendingTakeovers':24,'AlreadyPendingSilentRefusals':48,'FirstPendingFalseSettled':24,'FirstPendingTrueAuthenticated':24,'ActualKernelReplyTracesVerified':72,'LateSuccessResults':8,'LateErrorResults':8,'NaturalOldTimersAfterHandoff':8,'ActualLateReceiveMessages':24,'OrderedMailboxes':16,'AuthenticatedChannels':authcount,'AliveFalseRefusals':24,'AliveWrongCookieRefusals':negatives,'ExpectedFatalOldVmExits':24,'NormalOracleReplacementExits':48,'CapturedKilledFamilyHandles':familyhandles,'PrivateEpmdProcesses':5,'Groups':groups,'InputFreezes':freezes}
(R/'artifacts/library452-independent-analysis1/accepted-audit.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({k:v for k,v in out.items() if k not in ['Groups','InputFreezes']}));print(json.dumps(freezes))
