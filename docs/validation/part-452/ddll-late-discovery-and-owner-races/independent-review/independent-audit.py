from pathlib import Path
import json, hashlib, struct, collections, difflib
R=Path.cwd(); O=R/'artifacts/library452-independent-analysis1'
def j(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def a(s):return {'Kind':'Atom','Name':s}
def files(obs,n,s,ext='json'):
 p=[p for p in obs.glob('*-'+s+'.'+ext) if p.name.split('-',1)[1]==n+'-'+s+'.'+ext];assert len(p)==1;return p[0]
def t(obs,n,s='original'):return j(files(obs,n,s))
def rf(b):
 b=bytes.fromhex(b['BytesHex']);assert b[:2]==bytes([131,90]);n=int.from_bytes(b[2:4],'big');tag=b[4];assert tag in (119,118)
 z=1 if tag==119 else 2;ln=int.from_bytes(b[5:5+z],'big');k=5+z;node=b[k:k+ln].decode('utf-8');k+=ln;creation=int.from_bytes(b[k:k+4],'big');k+=4;assert len(b)==k+4*n
 return {'Kind':'ReferenceTerm','Node':node,'Creation':creation,'Words':[int.from_bytes(b[k+4*i:k+4*i+4],'big') for i in range(n)]}
def eq(x,y,bindings):
 if x==y:return True
 if isinstance(x,dict) and isinstance(y,dict):
  if x.get('Kind')==y.get('Kind')=='ReferenceTerm':return any(x==l and y==r for l,r in bindings)
  return x.keys()==y.keys() and all(eq(x[k],y[k],bindings) for k in x)
 if isinstance(x,list) and isinstance(y,list):return len(x)==len(y) and all(eq(l,r,bindings) for l,r in zip(x,y))
 return False
stats=collections.Counter(); groups=[]; freezes=[]; seen=set(); failures=[]
for run in range(1,6):
 root=R/f'artifacts/library452-probe-run{run}';finish=j(root/'completion.json');assert finish['Passed']==(run!=3) and finish['InputsUnchanged'];assert all(s['Equal'] and s['Files']==6427 for s in finish['ProviderScans'])
 frozen=j(root/'inputs-before.json');assert len(frozen)==2698 and all(sha(Path(p))==h for p,h in frozen.items());freezes.append({'Run':run,'Bindings':len(frozen),'Drift':0,'Runtime':finish['RuntimeSha256']})
 ep=j(root/'epmd.exit.json');assert ep['ExitCode']==0 and ep['Settled'] and not ep['ForcedTermination'];stats['NormalEPMD']+=1
 for g in finish['Groups']:
  d,order,mode=g['Depth'],g['Order'],g['Mode'];base=root/f'depth{d}-{order}-{mode}';obs=base/'observations'
  if not g['Passed']:assert (run,d,order,mode)==(3,8,'outgoing','old_timer');failures.append(str(base));continue
  key=d,order,mode;assert key not in seen;seen.add(key);c=j(obs/'comparison.json');assert c['Passed'] and c['PhysicalCleanup'] and len(c['Results'])==53
  bindings=[(rf(t(obs,'wire-'+k)),rf(t(obs,'wire-'+k,'managed'))) for k in ['roundtrip','handoff','held']]
  for row in c['Results']:
   n=row['Name'];x=t(obs,n);y=t(obs,n,'managed');assert 0<=row['OwnReferenceBindings']<=3;assert eq(x,y,bindings[:row['OwnReferenceBindings']]) and row['Passed'];raw=files(obs,n,'original','etf').read_bytes()==files(obs,n,'managed','etf').read_bytes();assert raw==row['WholeRawEqual'] and (x==y)==row['WholeTypedEqual'];stats['Rows']+=1;stats['RawExact']+=raw;stats['OwnRefProtocol']+=not raw
  checking=t(obs,'original-check-pending-row')['Items'];checker=checking[1];old=checking[0]['Items'];fin=t(obs,'original-check-pending-finish')['Items'];up=fin[2]['Items'];assert fin[:2]==[a('remarked'),a('true')] and old[3]==a('check_pending') and old[4]==checker and up[4]!=checker and up[2]==old[2]
  initial=t(obs,'original-discovery-receive-events')['Items'];received=[v['Items'][1] for v in initial if v['Items'][0]==a('received')]
  if mode!='old_timer':
   q=t(obs,'original-actual-mailbox-order')['Items'];ms=q[0]['Items'];ai=int(q[1]['Decimal']);si=int(q[2]['Decimal']);am,sm=q[3:];assert 0<ai<si<=len(ms) and ms[ai-1]==am and ms[si-1]==sm
   assert am['Items'][0]==up[4] and am['Items'][1]['Items'][2]==old[1];assert sm['Items'][0:3]==[a('setup_check'),old[1],checker] and sm['Items'][3]['Kind']=='ReferenceTerm' and received==[sm]
   assert not any(v.get('Kind')=='TupleTerm' and v['Items'][0]==a('setup_check_timeout') for v in ms)
   res=sm['Items'][4]['Items'];assert res[0]==a('ok' if mode=='late_success' else 'error')
   if mode=='late_error':assert res[1]=={'Kind':'TupleTerm','Items':[a('unsupported_address_type'),old[1]]}
   acceptor=t(obs,'original-real-acceptor-created')['Items'];assert acceptor[1]['Items'][2]==up[4];stats[mode]+=1;stats['OrderedMailboxes']+=1
  else:
   assert not received;timer=t(obs,'original-natural-old-timer')['Items'];message={'Kind':'TupleTerm','Items':[a('setup_check_timeout'),old[1],checker]};assert timer[0]==message and timer[2:]==[a('false'),{'Kind':'Integer','Decimal':'5000'}]
   before=t(obs,'original-row-before-natural-old-timer');assert before==t(obs,'original-row-after-natural-old-timer') and timer[1]==before['Items'][0];now=timer[1]['Items'];assert now[4]!=up[4] and now[5]!=up[5] and now[2]!=up[2]
   assert files(obs,'original-row-before-natural-old-timer','original','etf').read_bytes()==files(obs,'original-row-after-natural-old-timer','original','etf').read_bytes()
   ev=t(obs,'original-natural-timer-receive-events')['Items'];assert [v['Items'][1] for v in ev if v['Items'][0]==a('received')]==[message];assert all(t(obs,n)==a('true') for n in ['actual-data-before-old-timer','actual-data-after-old-timer']);stats['NaturalOldTimers']+=1
  assert (obs/'simultaneous-outgoing/status.bin').read_bytes()==b'sok_simultaneous'
  for phase in ['false','true']:
   n='already-pending-'+phase;before=t(obs,'original-'+n+'-before');assert before==t(obs,'original-'+n+'-after');assert before['Items'][0]['Items'][3]==a('up_pending');assert files(obs,'original-'+n+'-before','original','etf').read_bytes()==files(obs,'original-'+n+'-after','original','etf').read_bytes()
   ev=t(obs,'original-'+n+'-traces')['Items'];assert len(ev)==2;assert [v['Items'][0]['Items'][1]['Items'][1]['Name'] for v in ev]==['up_pending','already_pending'];assert ev[0]['Items'][1]==before['Items'][0]['Items'][6] and ev[1]['Items'][1]!=ev[0]['Items'][1]
   p=obs/(n+'-extra');r=j(p/'candidate-refusal.json');assert r['ReceivedStatusBytes']==0 and (r['PhysicalEof'] or r['PhysicalReset']) and not r['Adopted'];assert not any((p/f).exists() for f in ['status.bin','challenge.bin','reply.bin','ack.bin','authentication.json']);stats['SilentExtras']+=1
  for name in ['simultaneous-outgoing','alive-handoff','connection2']:
   p=obs/name;ch=(p/'challenge.bin').read_bytes();rep=(p/'reply.bin').read_bytes();ack=(p/'ack.bin').read_bytes();digest=lambda n:hashlib.md5(('ddll452_owned_fixture_cookie'+str(int.from_bytes(n,'big'))).encode('latin1')).digest();assert rep[5:]==digest(ch[9:13]) and ack[1:]==digest(rep[1:5])
   cl=j(p/'cleanup.json');assert cl['Passed'] and cl['ControllerJoined'] and cl['SocketDisposed'] and all(cl['FinalSnapshot'][k]==0 for k in ['QueuedPackets','QueuedSegments','QueuedPacketBytes']);stats['AuthenticatedChannels']+=1
  fatal=j(obs/'fatal-peer-stop.json');ex=j(base/'peer.exit.json');assert fatal['Passed'] and fatal['KillRequested'] and fatal['OldPid']==ex['Pid'] and ex['ExpectedScenarioTermination'] and ex['Settled'] and ex['ExitCode']!=0 and not ex['ForcedCleanup'];assert all(x['Settled'] and x['StartedUtc'] for x in fatal['Family']);stats['KilledFamilyHandles']+=len(fatal['Family']);stats['ExpectedFatalVMs']+=1
  for f in ['primary.exit.json','restarted-peer/exit.json']:
   e=j(base/f);assert e['Settled'] and e['ExitCode']==0 and not e['ForcedTermination'];stats['NormalVMs']+=1
   if f.startswith('restarted'):assert e['PumpsJoined']
  for side in ['original','managed']:
   oldref=t(obs,'genuine-peer-reference-via-distribution',side);new=t(obs,'new-peer-reference',side);assert oldref['Node']==new['Node'] and oldref['Creation']!=new['Creation']
   for n in ['foreign-reject-before-disconnect','foreign-reject-after-reconnect','old-peer-ref-rejected-by-new-peer']:
    v=t(obs,n,side)['Items'];assert v[:3]==[a('exception'),a('error'),a('badarg')] and len(v[3]['Items'])==max(1,min(d,3));assert json.dumps(oldref,sort_keys=True) in json.dumps(t(obs,n,side),sort_keys=True)
   assert t(obs,'peer-memory-lost-after-actual-restart',side)==a('undefined');assert t(obs,'new-peer-queue-survives-old-ref',side)=={'Kind':'Integer','Decimal':'1'} and t(obs,'new-peer-queue-zero-after-new-ref',side)=={'Kind':'Integer','Decimal':'0'};stats['ReferenceErrorSides']+=1
  groups.append({'Run':run,'Depth':d,'Order':order,'Mode':mode,'Rows':53,'Checker':checker,'LateTimerOrResultVerified':True,'ExplicitWireReferenceBindings':3,'PhysicalCleanup':True})
assert seen=={(d,o,m) for d in [0,1,2,8] for o in ['incoming','outgoing'] for m in ['late_success','late_error','old_timer']}
assert (stats['Rows'],stats['RawExact'],stats['OwnRefProtocol'],stats['late_success'],stats['late_error'],stats['NaturalOldTimers'],stats['AuthenticatedChannels'],stats['KilledFamilyHandles'])==(1272,1104,168,8,8,8,72,32)
for f in ['Program.cs','DistributionTcp452.cs','DistributionTcp452.Initiator.cs','beam_ddll_distribution452.erl','ddll452_epmd.erl']:assert (R/'artifacts/library452-probe1'/f).read_bytes()==(R/'artifacts/library452-probe2'/f).read_bytes()
control1=(R/'artifacts/library452-probe1/ddll_tcp452.erl').read_bytes();control2=(R/'artifacts/library452-probe2/ddll_tcp452.erl').read_bytes();cut=b'    {ok,S}=gen_tcp:accept(L,60000)';assert control1.split(cut,1)[1]==control2.split(cut,1)[1]
for f in ['beam_ddll_distribution452.beam','ddll452_epmd.beam']:
 assert len({sha(R/f'artifacts/library452-probe-run{i}/observer-ebin'/f) for i in range(1,6)})==1
failed=R/'artifacts/library452-probe-run3/depth8-outgoing-old_timer';bad=j(failed/'observations/comparison.json');assert not bad['Passed'] and bad['PhysicalCleanup'] and any('IOException' in v and 'ready' in v and 'being used by another process' in v for v in bad['Failures']);ex=j(failed/'restarted-peer/construction-failure-exit.json');assert ex['Settled'] and ex['ForcedTermination']
out={'Passed':True,'Counts':dict(stats),'PairedRows':1128,'AuxiliaryRows':144,'Groups':groups,'Freezes':freezes,'FailedPreparationPreserved':failures,'SuccessorControlScenarioSuffixEqual':True,'SuccessorHostAndWorkerUnchanged':True}
(O/'independent-audit.json').write_text(json.dumps(out,indent=2)+'\n');print(json.dumps({'Passed':True,'Counts':dict(stats),'Groups':len(groups),'Freezes':freezes}))
