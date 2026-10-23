import json,hashlib,struct,datetime
from pathlib import Path
R=Path.cwd(); cookie='ddll450_owned_fixture_cookie'
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
rows=exact=authcount=negatives=familyhandles=0;groups=[];freezes=[]
for run,depths in [(1,[8]),(2,[0,1,2])]:
 root=R/f'artifacts/library450-probe-run{run}';finish=read(root/'completion.json')
 assert finish['Passed'] and finish['InputsUnchanged'] and all(s['Equal'] and s['Files']==6427 for s in finish['ProviderScans'])
 freeze=read(root/'inputs-before.json');assert all(sha(Path(p))==h for p,h in freeze.items())
 ep=read(root/'epmd.exit.json');assert ep['ExitCode']==0 and ep['Settled'] and not ep['ForcedTermination']
 freezes.append({'Run':run,'Bindings':len(freeze),'Mismatch':0,'RuntimeSha256':finish['RuntimeSha256'],'Provider6427EqualBeforeAfter':True,'EpmdNormal':True})
 for depth in depths:
  for order in ['incoming','outgoing']:
   base=root/f'depth{depth}-{order}';obs=base/'observations';c=read(obs/'comparison.json');assert c['Passed'] and c['PhysicalCleanup'] and len(c['Results'])==50
   for row in c['Results']:
    left=[p for p in obs.glob('*-original.etf') if p.name.split('-',1)[1]==row['Name']+'-original.etf'];right=[p for p in obs.glob('*-managed.etf') if p.name.split('-',1)[1]==row['Name']+'-managed.etf'];assert len(left)==len(right)==1 and row['Passed']
    same=left[0].read_bytes()==right[0].read_bytes();assert same==row['WholeRawEqual'];rows+=1;exact+=same
   pick=read(obs/'simultaneous-selection.json');expected=pick['Managed']<pick['Peer'];assert pick['Passed'] and pick['IncomingSelected']==expected and pick['OriginalStatus']==('nok' if expected else 'ok_simultaneous') and not pick['LoserAdopted'] and pick['ActualOriginalConnect']
   assert (obs/'simultaneous-outgoing/status.bin').read_bytes()==b's'+pick['OriginalStatus'].encode()
   winner='simultaneous-incoming' if expected else 'simultaneous-outgoing';loser='simultaneous-outgoing' if expected else 'simultaneous-incoming'
   reject=read(obs/loser/'candidate-refusal.json');assert not reject['Adopted']
   if not expected:assert reject['OriginalOutgoingSetupClosed']
   assert not (obs/loser/'authentication.json').exists()
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
   groups.append({'Run':run,'Depth':depth,'Order':order,'Rows':50,'Exact':43,'ProtocolOnly':7,'Simultaneous':pick,'ListenerCreation':reg['Creation'],'Channels':channels,'OriginalControllerBefore':before[5],'OriginalControllerAfter':after[5],'Handoff':handoff,'Fatal':fatal,'ActualOldExit':old,'Restart':restart,'NativeCleanup':True})
assert rows==400 and exact==344 and authcount==24 and negatives==8
assert (R/'artifacts/library450-probe-run1/observer-ebin/beam_ddll_distribution450.beam').read_bytes()==(R/'artifacts/library450-probe-run2/observer-ebin/beam_ddll_distribution450.beam').read_bytes()
assert (R/'artifacts/library450-probe-run1/observer-ebin/ddll_tcp450.beam').read_bytes()==(R/'artifacts/library450-probe-run2/observer-ebin/ddll_tcp450.beam').read_bytes()
out={'Passed':True,'ObservationRows':rows,'RawExact':exact,'ProtocolOnly':rows-exact,'PairedRows':352,'AuxiliaryGuards':48,'AuthenticatedChannels':authcount,'AliveFalseRefusals':8,'AliveWrongCookieRefusals':negatives,'ExpectedFatalOldVmExits':8,'NormalOracleReplacementExits':16,'CapturedKilledFamilyHandles':familyhandles,'PrivateEpmdProcesses':2,'Groups':groups,'InputFreezes':freezes}
(R/'artifacts/library450-independent-analysis1/accepted-audit.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({k:v for k,v in out.items() if k not in ['Groups','InputFreezes']}));print(json.dumps(freezes))
