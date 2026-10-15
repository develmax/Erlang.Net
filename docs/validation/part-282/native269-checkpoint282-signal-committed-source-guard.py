import pathlib,hashlib,json,subprocess,argparse
p=argparse.ArgumentParser();p.add_argument('--head',required=True);args=p.parse_args();head=args.head;sha=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p]);s=json.loads(pathlib.Path('artifacts/native269-signal282-dispatch-source18.json').read_bytes());assert len(s['Files'])==18
for r in s['Files']:
 b=blob(r['Path']);assert sha(b if r['StrictRaw'] else b.replace(b'\r\n',b'\n'))==(r['RawSha256'] if r['StrictRaw'] else r['LfSha256'])
plan=json.loads(blob('tools/Erlang.DistributionSignalObservations/source-plan.json'));assert plan['Pin']==s['Pin'] and plan['FreshObservationVMs']==5 and len(plan['Scenarios'])==5 and plan['ObservedSemanticExpectations'] is None and not plan['SdkExpectedUsed'] and not plan['NativeExecuted'];assert sha(blob('tools/Erlang.DistributionSignalObservations/distribution_signal_observer.erl'))==plan['SourceSha256']
t=json.loads(blob('docs/validation/part-282/tests.json'));assert t['Passed']==len(t['Tests'])==6562 and t['Failed']==0 and all(r['Status']=='Passed' for r in t['Tests']);receipt=json.loads(blob('docs/validation/part-282/evidence-receipt.json'));assert receipt['Passed']==6562 and receipt['Failed']==0
promotion=json.loads(blob('docs/validation/part-282/main-promotion.json'));assert len(promotion['Files'])==40
print('PASS committed signal18/rawprobe+plan/full6562/source40, native five pending')
