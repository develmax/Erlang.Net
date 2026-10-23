from pathlib import Path
import hashlib,json,struct
R=Path.cwd(); cookie='ddll451_owned_fixture_cookie'
def j(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def digest(c,n):return hashlib.md5((c+str(n)).encode('latin1')).digest()
def u32(b,n):return struct.unpack_from('>I',b,n)[0]
def term(obs,name,side='original'):
 f=[p for p in obs.glob('*-'+side+'.json') if p.name.split('-',1)[1]==name+'-'+side+'.json'];assert len(f)==1;return j(f[0])
def atom(n):return {'Kind':'Atom','Name':n}
rows=exact=tracegroups=tracemessages=auth=extras=families=0;groups=[];freezes=[]
for run,depths in [(2,[8]),(3,[0,1,2])]:
 root=R/f'artifacts/library451-probe-run{run}';finish=j(root/'completion.json');assert finish['Passed'] and finish['InputsUnchanged'] and all(s['Equal'] and s['Files']==6427 for s in finish['ProviderScans'])
 bindings=j(root/'inputs-before.json');assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()==h for p,h in bindings.items())
 ep=j(root/'epmd.exit.json');assert ep['ExitCode']==0 and ep['Settled'] and not ep['ForcedTermination'];freezes.append({'Run':run,'Bindings':len(bindings),'Mismatch':0,'Runtime':finish['RuntimeSha256'],'EpmdNormal':True})
 for depth in depths:
  for order in ['incoming','outgoing']:
   base=root/f'depth{depth}-{order}';obs=base/'observations';comp=j(obs/'comparison.json');assert comp['Passed'] and comp['PhysicalCleanup'] and len(comp['Results'])==53
   for r in comp['Results']:
    a=[p for p in obs.glob('*-original.etf') if p.name.split('-',1)[1]==r['Name']+'-original.etf'];b=[p for p in obs.glob('*-managed.etf') if p.name.split('-',1)[1]==r['Name']+'-managed.etf'];assert len(a)==len(b)==1 and r['Passed'];eq=a[0].read_bytes()==b[0].read_bytes();assert eq==r['WholeRawEqual'];rows+=1;exact+=eq
   initial=term(obs,'original-check-pending-row')['Items'];old=initial[0]['Items'];checker=initial[1];connector=initial[2];assert old[3]==atom('check_pending') and old[4]==checker and old[5]==old[6]==atom('undefined') and connector in [v['Items'][0] for v in old[8]['Items']]
   end=term(obs,'original-check-pending-finish')['Items'];up=end[2]['Items'];assert end[:2]==[atom('remarked'),atom('true')] and up[3]==atom('up') and up[2]==old[2] and up[4]!=checker
   trace=term(obs,'original-check-pending-traces')['Items'];assert len(trace)==1;ev=trace[0]['Items'];assert ev[0]['Items'][1]['Items']==[atom('accept_pending'),atom('ok_pending')] and ev[1]==up[4];tracegroups+=1;tracemessages+=len(trace)
   assert (obs/'simultaneous-outgoing/status.bin').read_bytes()==b'sok_simultaneous';proof=j(obs/'check-pending-proof.json');assert proof['SameRuntimePendingHandle'] and not proof['ManagedCheckPendingStateClaim']
   for phase in ['false','true']:
    name='already-pending-'+phase;before=term(obs,'original-'+name+'-before');after=term(obs,'original-'+name+'-after');assert before==after;row=before['Items'][0]['Items'];assert row[3]==atom('up_pending') and row[4]==up[4] and row[2]==up[2]
    ev=term(obs,'original-'+name+'-traces')['Items'];assert len(ev)==2;events=[r['Items'] for r in ev];assert [r[0]['Items'][1]['Items'] for r in events]==[[atom('accept_pending'),atom('up_pending')],[atom('accept_pending'),atom('already_pending')]] and events[0][1]==row[6] and events[1][1]!=row[6] and events[1][1]!=row[4];tracegroups+=1;tracemessages+=2
    p=obs/(name+'-extra');q=j(p/'candidate-refusal.json');assert q['ReceivedStatusBytes']==0 and(q['PhysicalEof'] or q['PhysicalReset']) and q['OriginalClosedWithoutStatus'] and not q['Adopted'] and not q['ChallengeReceived'];assert not any((p/n).exists() for n in ['status.bin','challenge.bin','reply.bin','ack.bin','authentication.json']);extras+=1
   first=obs/'already-pending-first-false';assert (first/'status.bin').read_bytes()==b'salive' and(first/'alive-response.bin').read_bytes()==b'sfalse';assert j(first/'candidate-refusal.json')['OriginalClosed']
   for name in ['current-data-while-false-candidate-held','current-data-after-extra-refused-first-false','current-data-while-true-candidate-held','current-data-after-alive-false','current-data-after-alive-bad-cookie']:
    assert all(term(obs,name,s)==atom('true') for s in ['original','managed'])
   reg=j(obs/'listener/registration.json');rb=(obs/'listener/alive2-response.bin').read_bytes();assert rb[:2]==bytes([118,0]) and u32(rb,2)==reg['Creation']
   for name in ['simultaneous-outgoing','alive-handoff','connection2']:
    p=obs/name;a=j(p/'authentication.json');ch=(p/'challenge.bin').read_bytes();rep=(p/'reply.bin').read_bytes();ack=(p/'ack.bin').read_bytes();hello=(p/'hello.bin').read_bytes();assert rep[5:]==digest(cookie,u32(ch,9)) and len(rep)==21 and ack[1:]==digest(cookie,u32(rep,1)) and len(ack)==17
    assert (u32(ch,13) if a['Incoming'] else u32(hello,9))==reg['Creation']==a['LocalCreation'];clean=j(p/'cleanup.json');assert clean['Passed'] and clean['ControllerJoined'] and all(clean['FinalSnapshot'][k]==0 for k in ['QueuedPacketBytes','QueuedPackets','QueuedSegments']);auth+=1
   p=obs/'alive-wrong-cookie';ch=(p/'challenge.bin').read_bytes();rep=(p/'reply.bin').read_bytes();assert rep[5:]!=digest(cookie,u32(ch,9)) and rep[5:]==digest(cookie+'_wrong',u32(ch,9));assert not (p/'ack.bin').exists() and j(p/'candidate-refusal.json')['WrongCookieRejected']
   h=j(obs/'alive-handoff-proof.json');assert h['Passed'] and h['OldControllerJoined'] and h['RetiredBeforeHostAckRead'] and h['Old']['ConnectionId']!=h['Current']['ConnectionId'] and h['Old']['RemoteCreation']==h['Current']['RemoteCreation']
   f=j(obs/'fatal-peer-stop.json');oldexit=j(base/'peer.exit.json');assert f['Passed'] and f['KillRequested'] and f['OldPid']==oldexit['Pid'] and oldexit['ExpectedScenarioTermination'] and not oldexit['ForcedCleanup'] and oldexit['ExitCode']!=0 and oldexit['Settled'];assert len(f['Family'])>0 and all(v['Settled'] and v['StartedUtc'] for v in f['Family']);families+=len(f['Family'])
   for file in ['primary.exit.json','restarted-peer/exit.json']:
    e=j(base/file);assert e['ExitCode']==0 and e['Settled'] and not e['ForcedTermination']
    if file.startswith('restarted'):assert e['PumpsJoined']
   restart=j(obs/'actual-peer-restart.json');assert restart['OldPid']!=restart['NewPid'] and restart['OldCreation']!=restart['NewCreation'] and restart['OldReferenceWireUnchanged']
   groups.append({'Run':run,'Depth':depth,'Order':order,'Rows':53,'Checker':checker,'Connector':connector,'ActualCheckerExit':'remarked','OriginalWaiter':True,'RawRowsPreserved':True,'TraceGroups':3,'TraceMessages':5,'SilentExtras':2,'NativeCleanup':True,'CapturedKillHandles':len(f['Family'])})
for beam in ['beam_ddll_distribution451.beam','ddll_tcp451.beam','ddll451_epmd.beam']:
 assert (R/f'artifacts/library451-probe-run2/observer-ebin/{beam}').read_bytes()==(R/f'artifacts/library451-probe-run3/observer-ebin/{beam}').read_bytes()
assert (rows,exact,tracegroups,tracemessages,auth,extras,families)==(424,368,24,40,24,16,8)
result={'Passed':True,'Rows':rows,'RawExact':exact,'OwnReferenceProtocol':rows-exact,'Paired':376,'Auxiliary':48,'TraceGroups':tracegroups,'TraceMessages':tracemessages,'CheckPendingReplies':8,'UpPendingReplies':16,'AlreadyPendingReplies':16,'SilentExtras':extras,'AuthenticatedChannels':auth,'CapturedKillHandles':families,'NormalOracleReplacementExits':16,'ExpectedFatalExits':8,'PrivateEpmdNormalExits':2,'Groups':groups,'Freezes':freezes}
(R/'artifacts/library451-independent-analysis1/independent-audit.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:v for k,v in result.items() if k not in ['Groups','Freezes']}));print(json.dumps(freezes))
