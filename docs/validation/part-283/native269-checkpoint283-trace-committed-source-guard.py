import pathlib,hashlib,json,subprocess,argparse
p=argparse.ArgumentParser();p.add_argument('--head',required=True);args=p.parse_args();head=args.head;sha=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p]);s=json.loads(pathlib.Path('artifacts/native269-trace283-dispatch-source19.json').read_bytes());assert len(s['Files'])==19
promotion=json.loads(blob('docs/validation/part-283/main-promotion.json'));assert len(promotion['Files'])==11
for actual in promotion['Files']:
 expected=next(x for x in s['Files'] if x['Path']==actual['Path']);assert actual['Sha256']==expected['RawSha256']
for r in s['Files']:
 b=blob(r['Path'])
 if r['Path']=='tools/Erlang.DistributionTraceObservations/README.md':
  actual=next(x for x in promotion['Files'] if x['Path']==r['Path']);assert sha(b)==actual['Sha256'];continue
 assert sha(b if r['StrictRaw'] else b.replace(b'\r\n',b'\n'))==(r['RawSha256'] if r['StrictRaw'] else r['LfSha256'])
plan=json.loads(blob('tools/Erlang.DistributionTraceObservations/source-plan.json'));assert plan['OtpCommit']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and plan['OtpTag']=='OTP-29.1.1' and plan['FreshVmRequiredPerCase'] and len(plan['Cases'])==8 and not plan['SemanticAssertions'] and plan['NoGuessedExpectedResults'];assert sha(blob('tools/Erlang.DistributionTraceObservations/distribution_trace_probe.erl'))==plan['ProbeSha256']
t=json.loads(blob('docs/validation/part-283/historical-tests-282.json'));assert t['Passed']==len(t['Tests'])==6562 and t['Failed']==0 and blob('docs/validation/part-283/historical-tests-282.json')==blob('docs/validation/part-282/tests.json');receipt=json.loads(blob('docs/validation/part-283/evidence-receipt.json'));assert receipt['HistoricalMainPassed282']==6562 and not receipt['FullMainRerun'] and receipt['SourcePaths']==11 and receipt['RuntimeChanges']==0 and receipt['TargetErrors']==receipt['TargetWarnings']==receipt['StyleChangedFiles']==0 and receipt['CliRefusals']==14 and receipt['RawControls']==2 and not receipt['NativeTTExecuted']
promotion=json.loads(blob('docs/validation/part-283/main-promotion.json'));assert len(promotion['Files'])==11
parent='fdaf994c21984b41c471670a2d89787b307425de';changes=subprocess.check_output(['git','diff','--name-only',parent,head,'--','src','tests','tools/Erlang.DistributionSignalObservations','.github/workflows/distribution-signal-observations.yml'],text=True).splitlines();assert not changes,changes
print('PASS committed newtrace11/shared8/rawprobe+plan/historical6562/actualMainlocalgates; eight original pending')
