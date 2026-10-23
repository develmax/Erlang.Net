from pathlib import Path
import textwrap
r=Path.cwd();s=(r/'artifacts/library451-audit-passive.py').read_text().replace('library451','library452').replace('ddll451','ddll452').replace('distribution451','distribution452').replace('ddll_tcp451','ddll_tcp452')
s=s.replace('for run,depths in [(2,[8]),(3,[0,1,2])]:','for run in [1,2,3,4,5]:')
s=s.replace(" assert finish['Passed'] and finish['InputsUnchanged']", " assert (finish['Passed'] if run!=3 else not finish['Passed']) and finish['InputsUnchanged']")
start=s.index(' for depth in depths:');body_start=s.index('   base=',start);end=s.index('assert rows==',body_start)
body=textwrap.indent(textwrap.dedent(s[body_start:end]),'  ')
s=s[:start]+" for group in finish['Groups']:\n  if not group['Passed']:\n   assert run==3 and (group['Depth'],group['Order'],group['Mode'])==(8,'outgoing','old_timer');continue\n  depth,order,mode=group['Depth'],group['Order'],group['Mode']\n  assert (depth,order,mode) not in seen;seen.add((depth,order,mode))\n"+body+s[end:]
s=s.replace("base=root/f'depth{depth}-{order}'","base=root/f'depth{depth}-{order}-{mode}'")
s=s.replace('rows=exact=authcount=negatives=familyhandles=0;groups=[];freezes=[]','rows=exact=authcount=negatives=familyhandles=0;groups=[];freezes=[];seen=set()')
needle="  reg=read(obs/'listener/registration.json')";assert needle in s
extra='''  proof=read(obs/'late-discovery-owner-proof.json');assert proof['Passed'] and proof['Mode']==mode and proof['ActualSourceMessages'] and not proof['InjectedSetupMessage'] and not proof['InjectedTimeoutMessage']
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
'''
s=s.replace(needle,extra+needle)
s=s.replace('rows==424 and exact==368 and authcount==24 and negatives==8','rows==1272 and exact==1104 and authcount==72 and negatives==24')
s=s.replace("'Run':run,'Depth':depth,'Order':order,","'Run':run,'Depth':depth,'Order':order,'Mode':mode,")
s=s.replace("'PairedRows':376,'AuxiliaryGuards':48,'CheckPendingTakeovers':8,'AlreadyPendingSilentRefusals':16,'FirstPendingFalseSettled':8,'FirstPendingTrueAuthenticated':8,'ActualKernelReplyTracesVerified':24", "'PairedRows':1128,'AuxiliaryGuards':144,'CheckPendingTakeovers':24,'AlreadyPendingSilentRefusals':48,'FirstPendingFalseSettled':24,'FirstPendingTrueAuthenticated':24,'ActualKernelReplyTracesVerified':72,'LateSuccessResults':8,'LateErrorResults':8,'NaturalOldTimersAfterHandoff':8,'ActualLateReceiveMessages':24,'OrderedMailboxes':16")
s=s.replace("'AliveFalseRefusals':8","'AliveFalseRefusals':24").replace("'ExpectedFatalOldVmExits':8,'NormalOracleReplacementExits':16","'ExpectedFatalOldVmExits':24,'NormalOracleReplacementExits':48").replace("'PrivateEpmdProcesses':2","'PrivateEpmdProcesses':5")
start=s.index("assert (R/'artifacts/library452-probe-run3/observer-ebin");end=s.index("out={'Passed':",start)
s=s[:start]+'''assert seen=={(d,o,m) for d in [0,1,2,8] for o in ['incoming','outgoing'] for m in ['late_success','late_error','old_timer']}
for name in ['beam_ddll_distribution452.beam','ddll452_epmd.beam']:
 expected=(R/'artifacts/library452-probe-run1/observer-ebin'/name).read_bytes()
 assert all((R/f'artifacts/library452-probe-run{run}/observer-ebin'/name).read_bytes()==expected for run in [2,3,4,5])
for name in ['Program.cs','DistributionTcp452.cs','DistributionTcp452.Initiator.cs','beam_ddll_distribution452.erl','ddll452_epmd.erl']:assert (R/'artifacts/library452-probe1'/name).read_bytes()==(R/'artifacts/library452-probe2'/name).read_bytes()
failure=read(R/'artifacts/library452-probe-run3/depth8-outgoing-old_timer/observations/comparison.json');assert not failure['Passed'] and failure['PhysicalCleanup'] and any('IOException' in x and 'ready' in x and 'being used by another process' in x for x in failure['Failures'])
bad_exit=read(R/'artifacts/library452-probe-run3/depth8-outgoing-old_timer/restarted-peer/construction-failure-exit.json');assert bad_exit['Settled'] and bad_exit['ForcedTermination']
'''+s[end:]
(r/'artifacts/library452-audit-passive.py').write_bytes(s.encode())
