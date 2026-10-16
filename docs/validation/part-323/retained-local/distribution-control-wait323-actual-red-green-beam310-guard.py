from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';red=a/'distribution-control-wait323-historical-red1';green=a/'distribution-control-wait323-focus1';base=a/'connected-guest-exit322-focus2';h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());bind=lambda p:{'Path':p.relative_to(r).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p)}
assert h(red/'source-freeze.json')=='1510A40806DACDCE7675FA74BFAAC8F048DD056BD70608C5CFD8AFCD8BD23EAB';assert h(red/'source-resource-closure.json')=='4C342B7A502C734A549BA447FAD5DA281E75263AB8E8B4C401F2E42C80221250';rows=j(red/'source-resource-closure.json')['Files'];assert len(rows)==2317
for x in rows:
 p=red/'candidate'/x['Path'];assert p.stat().st_size==x['Bytes'] and h(p)==x['Sha256']
for x in j(base/'formatted-source-resource-closure.json')['Files']:assert (red/'candidate'/x['Path']).read_bytes()==(base/'candidate'/x['Path']).read_bytes()
shared=[x['Path'] for x in j(red/'source-freeze.json')['Files']];assert len(shared)==3
for n in shared:assert (red/'candidate'/n).read_bytes()==(green/'candidate'/n).read_bytes()
assert j(red/'target-build-exit.json')['ExitCode']==0;log=(red/'target-build.log').read_text();assert 'Предупреждений: 0' in log and 'Ошибок: 0' in log
reports=[];identities=None
for stem,exitstem,method,kind in [('actual-query-red','actual-query-exit','RunQueries','query-capacity'),('actual-query-turn-red','actual-query-turn-exit','QueryDuringTurn','query-turn'),('actual-kill-capacity-red','actual-kill-capacity-exit','KillDuringCapacity','kill-capacity'),('actual-kill-turn-red','actual-kill-turn-exit','KillDuringTurn','kill-turn')]:
 p=red/(stem+'.json');report=j(p);e=j(red/(exitstem+'.json'));assert e['ExitCode']==e['ExpectedExitCode']==1;assert report['Status']=='FAIL' and len(report['Cases'])==1;case=report['Cases'][0];trace=case['Exception'];assert case['Name']=='DistributionControlWaitPublicRouteHostChecks:'+method and case['Status']=='FAIL' and 4000<=case['ElapsedMilliseconds']<30000
 assert not any(s in trace for s in ['AggregateException','TypeLoadException','NullReferenceException','MissingMethodException','TargetInvocationException'])
 assert 'Finish(' in trace and 'DistributionControlWaitPublicRouteHostChecks.cs:line 281' in trace
 if kind.startswith('query'):assert trace.startswith('System.TimeoutException:') and 'DistributionControlWaitPublicRouteHostChecks.cs:line 51' in trace
 else:assert trace.startswith('System.InvalidOperationException: Guest distribution control wait assertion failed') and 'DistributionControlWaitPublicRouteHostChecks.cs:line 90' in trace
 if identities is None:identities=report['Identities']
 else:assert identities==report['Identities']
 for x in report['Identities']:assert h(Path(x['Path']))==x['Sha256']
 reports.append({'Role':kind,'Report':bind(p),'Exit':bind(red/(exitstem+'.json')),'Failure':'public_process_info_wait_timeout_line51' if kind.startswith('query') else 'public_monitor_DOWN_missing_after_bounded_receive_line90','Cleanup':'Finish rethrows sole primary at line281 after awaited unwound/process/blocker/query settlement; no aggregate cleanup failure'})
source=(red/'candidate/tests/Erlang.Tests/DistributionControlWaitPublicRouteHostChecks.cs').read_text().splitlines();assert 'await query.WaitAsync' in source[50] and 'Check(down?.Equals' in source[89] and 'ExceptionDispatchInfo.Capture(failures[0]).Throw()' in source[280]
assert h(green/'formatted-source-freeze.json')=='5DC06B0D895EEC67562DE36D36CEA0C700D95297769BEC7454FCB9BCA21AAF06';assert h(a/'distribution-control-wait323-actual-green-beam310-peer.json')=='197A145452AF7C6D39666F49A4C2E9F45B8CDD951FAF39D88BCDD7878BF42891';assert (green/'tokens-before.json').read_bytes()==(green/'tokens-after.json').read_bytes() and len(j(green/'tokens-before.json'))==11
for x in j(green/'formatted-source-resource-closure.json')['Files']:
 p=green/'candidate'/x['Path'];assert p.stat().st_size==x['Bytes'] and h(p)==x['Sha256']
g=j(green/'actual-green-focus.json');assert g['Status']=='PASS' and len(g['Cases'])==16 and all(x['Status']=='PASS' for x in g['Cases'])
for x in g['Identities']:assert h(Path(x['Path']))==x['Sha256']
out=a/'distribution-control-wait323-actual-red-green-beam310-guard.json';assert not out.exists();out.write_text(json.dumps({'Status':'PASS_PASSIVE_ACTUAL323_FOUR_HISTORICAL_PUBLIC_WAIT_REDS_AND_SIXTEEN_GREEN','RedSourceFreeze':bind(red/'source-freeze.json'),'RedSourceClosure':bind(red/'source-resource-closure.json'),'RedSourceRowsRehashed':2317,'AllFormatted322ProductionAndProgram2314RawExact':True,'ThreeSharedFormattedHostSourcesExactGreenRed':shared,'ActualRedTargetWarningsErrors':[0,0],'ActualRedReports':reports,'ActualRedDllIdentities':identities,'GreenSourceFreeze':bind(green/'formatted-source-freeze.json'),'GreenSourceClosure':bind(green/'formatted-source-resource-closure.json'),'GreenReport':bind(green/'actual-green-focus.json'),'IndependentGreenPeer':bind(a/'distribution-control-wait323-actual-green-beam310-peer.json'),'ActualGreenGroups':16,'GreenSourceRowsRehashed':2318,'ElevenFormattingTokenStreamsExact':True,'ActualGreenDllIdentities':g['Identities'],'NoHarnessTypeSelectorOrCleanupAggregateFailure':True,'KillFailureIsMissingDownAssertionNotTimeoutException':True,'NoMainFullOriginalNativeWireOrSchedulerParityClaim':True,'ReviewerExecutedSdkBuildNativeVmMainGitMutation':False},indent=2)+'\n');print(h(out))
