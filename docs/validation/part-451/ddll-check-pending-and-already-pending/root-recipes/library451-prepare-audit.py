from pathlib import Path
r=Path.cwd();s=(r/'artifacts/library450-independent-analysis1/audit-passive.py').read_text().replace('library450','library451').replace('ddll450','ddll451').replace('distribution450','distribution451').replace('[(1,[8]),(2,[0,1,2])]','[(2,[8]),(3,[0,1,2])]').replace("len(c['Results'])==50","len(c['Results'])==53")
start=s.index("   pick=read(obs/'simultaneous-selection.json')");end=s.index("   reg=read(obs/'listener/registration.json')",start)
s=s[:start]+'''   pick=read(obs/'check-pending-proof.json');assert pick['Passed'] and pick['OriginalStatus']=='ok_simultaneous' and pick['ActualDiscoveryBarrier'] and pick['ActualCheckPendingRow'] and pick['ActualCheckerExit']=='remarked' and pick['OriginalConnectWaiter'] and pick['SameRuntimePendingHandle'] and not pick['ManagedCheckPendingStateClaim']
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
'''+s[end:]
s=s.replace("'Rows':50,'Exact':43","'Rows':53,'Exact':46").replace('rows==400 and exact==344','rows==424 and exact==368').replace("'PairedRows':352","'PairedRows':376")
s=s.replace("library451-probe-run1/observer-ebin","library451-probe-run3/observer-ebin").replace('ddll_tcp450.beam','ddll_tcp451.beam')
s=s.replace("'AuthenticatedChannels':authcount,","'CheckPendingTakeovers':8,'AlreadyPendingSilentRefusals':16,'FirstPendingFalseSettled':8,'FirstPendingTrueAuthenticated':8,'ActualKernelReplyTracesVerified':24,'AuthenticatedChannels':authcount,")
(r/'artifacts/library451-audit-passive.py').write_text(s)
