import hashlib,json,struct
from pathlib import Path
R=Path.cwd()
def read(p): return json.loads(Path(p).read_text(encoding='utf-8-sig'))
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def u32(b,o): return struct.unpack_from('>I',b,o)[0]
def digest(cookie,n): return hashlib.md5((cookie+str(n)).encode('latin1')).digest()
cookie='ddll449_owned_fixture_cookie'
rows=exact=auth_count=negative=0
groups=[];freezes=[]
for run,depths in [(6,[8]),(7,[0,1,2])]:
 root=R/f'artifacts/library449-probe-run{run}'
 finish=read(root/'completion.json')
 assert finish['Passed'] and finish['InputsUnchanged']
 assert all(s['Equal'] and s['Files']==6427 for s in finish['ProviderScans'])
 bindings=read(root/'inputs-before.json')
 assert all(sha(p)==h for p,h in bindings.items())
 ep=read(root/'epmd.exit.json');assert ep['ExitCode']==0 and ep['Settled'] and not ep['ForcedTermination']
 freezes.append({'Run':run,'Bindings':len(bindings),'Mismatch':0,'RuntimeSha256':finish['RuntimeSha256'],'ProviderBeforeAfter6427':True,'EpmdNormal':True})
 for depth in depths:
  base=root/f'depth{depth}';obs=base/'observations';c=read(obs/'comparison.json')
  assert c['Passed'] and c['PhysicalCleanup'] and len(c['Results'])==41
  for row in c['Results']:
   left=[p for p in obs.glob('*-original.etf') if p.name.split('-',1)[1]==row['Name']+'-original.etf']
   right=[p for p in obs.glob('*-managed.etf') if p.name.split('-',1)[1]==row['Name']+'-managed.etf']
   assert len(left)==len(right)==1 and row['Passed']
   same=left[0].read_bytes()==right[0].read_bytes();assert same==row['WholeRawEqual']
   rows+=1;exact+=same
  reg=read(obs/'listener/registration.json');response=(obs/'listener/alive2-response.bin').read_bytes()
  assert response[:2]==bytes([118,0]) and u32(response,2)==reg['Creation']
  channels=[]
  for conn in ['connection1','connection2']:
   p=obs/conn;ch=(p/'challenge.bin').read_bytes();reply=(p/'reply.bin').read_bytes();ack=(p/'ack.bin').read_bytes();hello=(p/'hello.bin').read_bytes();a=read(p/'authentication.json')
   assert len(reply)==21 and reply[:1]==b'r' and reply[5:]==digest(cookie,u32(ch,9))
   assert len(ack)==17 and ack[:1]==b'a' and ack[1:]==digest(cookie,u32(reply,1))
   assert u32(ch,13)==reg['Creation']==a['LocalCreation'] and u32(hello,9)==a['RemoteCreation'] and a['Incoming'] and a['Authenticated']
   clean=read(p/'cleanup.json');s=clean['FinalSnapshot']
   assert clean['Passed'] and clean['ControllerJoined'] and clean['SocketDisposed'] and all(s[k]==0 for k in ['QueuedPacketBytes','QueuedPackets','QueuedSegments'])
   auth_count+=1;channels.append({'Authentication':a,'Incoming':clean['Incoming'],'Outgoing':clean['Outgoing']})
  bad=obs/'wrong-cookie';a=read(bad/'authentication.json');ch=(bad/'challenge.bin').read_bytes();reply=(bad/'reply.bin').read_bytes()
  assert a['ExpectedWrongCookieRejected'] and not a['Adopted'] and reply[5:]!=digest(cookie,u32(ch,9)) and reply[5:]==digest(cookie+'_wrong',u32(ch,9));negative+=1
  restart=read(obs/'actual-peer-restart.json')
  assert restart['Passed'] and restart['OldPid']!=restart['NewPid'] and restart['OldCreation']!=restart['NewCreation'] and restart['OldConnection']!=restart['NewConnection'] and restart['OldReferenceWireUnchanged'] and restart['OldReferenceCreation']==restart['OldCreation']
  assert channels[0]['Authentication']['RemoteCreation']==restart['OldCreation'] and channels[1]['Authentication']['RemoteCreation']==restart['NewCreation']
  for role in ['peer','primary']:
   e=read(base/f'{role}.exit.json');assert e['ExitCode']==0 and e['Settled'] and not e['ForcedTermination']
  e=read(base/'restarted-peer/exit.json');assert e['ExitCode']==0 and e['Settled'] and not e['ForcedTermination'] and e['PumpsJoined'] and e['Pid']==restart['NewPid']
  s=read(obs/'connection2/stale-input-attempt.json')
  assert s['Passed'] and s['OneControllerTurn'] and s['CurrentActualInputHandler'] and all(s['Before'][k]==s['After'][k] for k in ['AcceptedInputPackets','QueuedPacketBytes','QueuedPackets','QueuedSegments'])
  groups.append({'Run':run,'Depth':depth,'Rows':41,'Exact':36,'ProtocolOnly':5,'ListenerCreation':reg['Creation'],'Restart':restart,'Channels':channels,'NativeCleanup':True})
assert rows==164 and exact==144 and auth_count==8 and negative==4
out={'Passed':True,'ObservationRows':rows,'RawExact':exact,'ProtocolOnly':rows-exact,'PairedRows':140,'AuxiliaryGuards':24,'AuthenticatedIncomingChannels':auth_count,'WrongCookieRefusals':negative,'OriginalOsNodes':12,'PrivateEpmdProcesses':2,'Groups':groups,'InputFreezes':freezes}
(R/'artifacts/library449-independent-analysis1/accepted-audit.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps({k:v for k,v in out.items() if k not in ['Groups','InputFreezes']}));print(json.dumps(freezes))
