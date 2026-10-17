from pathlib import Path
import json,hashlib,re
r=Path(__file__).resolve().parents[2];o=Path(__file__).parent;a=r/'artifacts/root-cleanup354-redgreen-actual1';src=r/'artifacts/root-cleanup354-redgreen3'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
c=j(a/'completion.json');assert c['Status']=='PASS_DETERMINISTIC_REAL_ACTOR_RED_GREEN_CLEANUP_STEALING'
b=r/'artifacts/size-bif354-full-candidate2';patch=r/'artifacts/halt-cleanup354-stealing-proposal/source/src/Erlang.Runtime/RuntimeActorDispatcher.cs';plan={x['Path']:x for x in j(r/'artifacts/size-bif354-full2-preparation/composition-plan.json')['Files']};v=j(a/'inputs.json');assert len(v['SourceInputs'])==len({x['Path'] for x in v['SourceInputs']})==969
changes=[]
for x in v['SourceInputs']:
 assert h(b/x['Path'])==x['Sha256']==plan[x['Path']]['Sha256']
 current=h(a/'green-source'/x['Path']);expected=h(patch) if x['Path']=='src/Erlang.Runtime/RuntimeActorDispatcher.cs' else x['Sha256'];assert current==expected
 if current!=x['Sha256']:changes.append(x['Path'])
assert changes==['src/Erlang.Runtime/RuntimeActorDispatcher.cs']
actual={p.relative_to(a/'green-source').as_posix() for p in (a/'green-source').rglob('*') if p.is_file() and not any(x in ('bin','obj') for x in p.relative_to(a/'green-source').parts)};assert actual=={x['Path'] for x in v['SourceInputs']}
for name in ['Main.cs','Diagnostic.csproj']:assert (a/'harness'/name).read_bytes()==(src/name).read_bytes()
for name in ['CleanupStealingHostChecks.cs','CleanupStealingHostNames.cs']:assert (a/'harness'/name).read_bytes()==(r/'artifacts/cleanup-stealing354-regression-proposal2/source/tests/Erlang.Tests'/name).read_bytes()
assert {p.name for p in (a/'harness').glob('*.cs')}=={'Main.cs','CleanupStealingHostChecks.cs','CleanupStealingHostNames.cs'}
assert h(a/'build/Cleanup354.dll')==c['HarnessSha256'];builds=[]
for name in ['runtime-build','harness-build']:
 x=j(a/(name+'.exit.json'));assert x['Settled'] and x['ExitCode']==0 and not x['TimedOut'] and not x['Failures']
 for stream in ['stdout','stderr']:assert h(a/(name+'.'+stream+'.log'))==x[stream.capitalize()+'Sha256']
 text=(a/(name+'.stdout.log')).read_text();assert re.search(r'(?:0 Warning\(s\)|Предупреждений:\s+0)',text) and re.search(r'(?:0 Error\(s\)|Ошибок:\s+0)',text)
 builds.append({'Name':name,'ReceiptSha256':h(a/(name+'.exit.json')),'ElapsedSeconds':x['ElapsedSeconds']})
attempts=[];assert len(c['Attempts'])==2
for label,item in zip(['red','green'],c['Attempts']):
 assert item['Label']==label;d=a/label;x=j(d/'process.exit.json');out=j(d/'outcome.json');assert item['Outcome']==out and item['Process']==x
 assert x['Settled'] and not x['TimedOut'] and not x['Failures'] and x['KillOnCloseJobAssigned'] and not x['JobTerminationRequested'] and x['RemainingJobProcesses']==0
 for stream in ['stdout','stderr']:assert h(d/('process.'+stream+'.log'))==x[stream.capitalize()+'Sha256']
 bound={(str(Path(z['Path']).resolve()).casefold(),z['Sha256']) for z in item['Inputs']};assert len(bound)==3
 for row in item['Inputs']:assert h(Path(row['Path']))==row['Sha256']
 assert {Path(z['Path']).name for z in out['LoadedRuntime']}=={'Erlang.Runtime.dll','Erlang.Terms.dll'}
 assert all((str(Path(z['Path']).resolve()).casefold(),z['Sha256']) in bound for z in out['LoadedRuntime'])
 live=j(d/'process.live.json');admit=j(d/'process.admitted.json');assert live['Pid']==admit['Pid']==x['Pid'] and not live['InvocationReleased'] and admit['InvocationReleased'] and admit['KillOnCloseJobAssigned']
 text=(d/'process.stdout.log').read_text();assert 'CLEANUP_BLOCKER_ENTERED' in text
 if label=='red':
  assert x['ExitCode']==1 and not out['Passed'] and out['InnerCount']==1 and out['InnerType']=='System.TimeoutException' and 'CleanupStealingHostChecks.cs:line 71' in out['Failure'] and 'CLEANUP_OBSERVER_ENTERED_BEFORE_RELEASE' not in text
 else:
  assert x['ExitCode']==0 and out['Passed'] and out['Failure'] is None
  assert text.index('CLEANUP_BLOCKER_ENTERED')<text.index('CLEANUP_OBSERVER_ENTERED_BEFORE_RELEASE')<text.index('CONTROL_RETURNED_AFTER_CLEANUP')
 attempts.append({'Label':label,'OutcomeSha256':h(d/'outcome.json'),'ReceiptSha256':h(d/'process.exit.json'),'ElapsedSeconds':x['ElapsedSeconds'],'LoadedRuntime':out['LoadedRuntime'],'ExitCode':x['ExitCode'],'JobProcessesRemaining':0,'ForcedCleanup':False})
old=j(r/'artifacts/halt-port354-gates-probe2/targets.json')['ActualFullDlls'];assert len(old)==19
for row in old:assert h(b/row['Path'])==row['Sha256']
report={'Status':'PASS_ACTUAL_DETERMINISTIC_REAL_ACTOR_CLEANUP_STEALING_RED_GREEN','DriverSourceFreezeSha256':h(src/'source-freeze.json'),'SourceAdmissionSha256':h(o/'source3-report.json'),'CompletionSha256':h(a/'completion.json'),'InputsSha256':h(a/'inputs.json'),'SourceInputsRehashed':969,'GreenSourceDeltaExactly':changes,'DispatcherSha256':h(patch),'OldFullDllRowsUnchanged':19,'HarnessSha256':c['HarnessSha256'],'ControlSourceExactlyProposal2':True,'ControlAssertionsBudgetsUnchanged':True,'ActualBuilds':builds,'ActualAttempts':attempts,'Proven':'Old dispatcher fails specific observer wait behind bound held lane1; same control DLL with sole Dispatcher source correction allows canceled unbound observer before release, then real HaltQuiescence/Dispose joins. Actual Runtime/Terms path/hash checked in each process.','HistoricalCauseLimit':'This proves reachable liveness defect and repair, not the unobserved exact interleaving of previous HaltPort historical failure. Uninstrumented failure and instrumented bothPASS remain separate.','BuildRepresentation':'All three green projects rebuilt; Terms/Serialization rawDLL hashes differ but their sources exactbaseline. Serialization not actually loaded either child, no fabricated loaded identity.','ReaderHistory':'Initial inline reader rejected English-only build summary assertion; actual Russian logs show zero warnings/errors. Reader now accepts exact anchored English or Russian labels, raw receipts/hashes unchanged. No source or guest failure change.','SdkNativeNetworkMainMutationByReviewer':False,'NoFullOriginalOrCompatibilityPromotionClaim':True}
p=o/'actual1-report.json';p.write_bytes((json.dumps(report,indent=2)+'\n').encode());print(h(p))
