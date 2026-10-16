from pathlib import Path
import hashlib, json

r = Path.cwd()
a = r / 'artifacts'
g = a / 'root-part321-composed3'
d = a / 'root-part321-historical-red2'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
bind = lambda p: {'Path': p.relative_to(r).as_posix(), 'Bytes': p.stat().st_size, 'Sha256': h(p.read_bytes())}
target = g / 'actual-core-focus-guard.json'
assert not target.exists()
def source(folder, freeze, closure, count):
    f = read(folder / freeze)
    cl = folder / closure
    assert h(cl.read_bytes()) == f['SourceClosureSha256']
    rows = read(cl)['Files']
    assert len(rows) == count
    for e in rows:
        raw = (folder / 'candidate' / e['Path']).read_bytes()
        assert len(raw) == e['Bytes'] and h(raw) == e['Sha256'], e['Path']
    return f, {e['Path']: e for e in rows}
gf, gm = source(g, 'focus-source-freeze.json', 'focus-source-resource-closure.json', 2307)
rf, rm = source(d, 'source-freeze.json', 'source-resource-closure.json', 2304)
assert h((g / 'focus-source-freeze.json').read_bytes()) == 'D9370F456E3313C27A6E9917B4F5BC5EFDBA1C07DE6C80E572727365F74356C8'
assert read(g / 'target-build-exit.json')['ExitCode'] == read(d / 'target-build-exit.json')['ExitCode'] == 0
before = (g / 'tokens-before.json').read_bytes()
assert before == (g / 'tokens-after.json').read_bytes() and len(json.loads(before)) == 9
parent = a / 'root-part320-composed3'
for e in read(parent / 'source-resource-closure.json')['Files']:
    assert rm[e['Path']] == e
shared = ['tests/Erlang.Tests/DistributionGuestLinkHostNames.cs', 'tests/Erlang.Tests/DistributionGuestLinkPublicRouteHostChecks.cs']
assert set(rm) - {e['Path'] for e in read(parent / 'source-resource-closure.json')['Files']} == set(shared)
for n in shared:
    assert rm[n] == gm[n]
def assemblies(report):
    for e in report['Identities']:
        assert h(Path(e['Path']).read_bytes()) == e['Sha256']
green = read(g / 'actual-green-focus.json')
assert green['Status'] == 'PASS' and len(green['Cases']) == 9 and all(e['Status'] == 'PASS' for e in green['Cases'])
assemblies(green)
red = []
for name, method in [('actual-link-red-focus.json', 'LinkPublishes'), ('actual-unlink-red-focus.json', 'InboundUnlinkPublishes')]:
    report = read(d / name)
    assert report['Status'] == 'FAIL' and len(report['Cases']) == 1
    case = report['Cases'][0]
    assert case['Name'] == 'DistributionGuestLinkPublicRouteHostChecks:' + method and case['Status'] == 'FAIL'
    assert 'System.InvalidOperationException: Connected public guest link assertion failed' in case['Exception']
    assert 'TakeNow(' in case['Exception'] and 'TimeoutException' not in case['Exception'] and 'TypeLoadException' not in case['Exception']
    assemblies(report)
    red.append(bind(d / name))
previous = a / 'root-part321-composed2'
old = read(previous / 'focus-source-resource-closure.json')['Files']
changed = {e['Path'] for e in old if e != gm[e['Path']]}
assert changed == {'tests/Erlang.Tests/DistributionGuestLinkHostChecks.cs', *shared}
assert read(a / 'root-part321-composed1/target-build-exit.json')['ExitCode'] == 1
assert read(previous / 'target-build-exit.json')['ExitCode'] == 0
for name in ['actual-green-focus.json', 'actual-inbound-regressions-focus.json']:
    report = read(previous / name)
    assert report['Status'] == 'FAIL' and 'TimeoutException' in report['Cases'][0]['Exception']
diagnostic = a / 'guest-links321-query-diagnostic1/actual-report.json'
diag = read(diagnostic)
assert diag['Passed'] is True and diag['CandidateModified'] is False and diag['PrivateFieldsWritten'] is False
assert diag['Steps'][1]['QueryCompleted'] is False and diag['Steps'][1]['PendingOwnerInfoRequests'] == 1
assert diag['Steps'][2]['QueryCompleted'] is True and diag['Steps'][2]['PendingOwnerInfoRequests'] == 0
report = {'Status': 'PASS_ACTUAL321_NINE_GREEN_AND_TWO_EXACT_PUBLISHED320_MISSING_OUTPUT_RED', 'Passed': True, 'SourceFreeze': bind(g / 'focus-source-freeze.json'), 'SourceClosure': bind(g / 'focus-source-resource-closure.json'), 'GreenSourceFiles': 2307, 'OwnedCSharp': 9, 'GreenHostGroups': 9, 'LifecycleSubgroups': 9, 'GreenReport': bind(g / 'actual-green-focus.json'), 'HistoricalSourceFreeze': bind(d / 'source-freeze.json'), 'HistoricalSourceClosure': bind(d / 'source-resource-closure.json'), 'HistoricalProduction320RawExact': True, 'SharedHostSourcesExact': shared, 'HistoricalMissingLinkAndUnlinkReports': red, 'NineTokenStreamsExact': True, 'ThreeTestsOnlyOwnerObservationCorrection': sorted(changed), 'FiveRuntimeAndProgramExactPreviouslyCompiledRevision2': True, 'FirstCompileCheckBindingFailurePreserved': bind(a / 'root-part321-composed1/target-build.log'), 'FirstTwoCrossActorQueryTimeoutReportsPreserved': [bind(previous / n) for n in ['actual-green-focus.json', 'actual-inbound-regressions-focus.json']], 'ActualOwnerPumpDiagnostic': bind(diagnostic), 'NoPrivateStateWritesOrFakeRepliesInHostCorrection': True, 'NewOriginalSevenContractsExecuted': False, 'LiveSdkPeerOrNativeWireEqualityClaimed': False, 'GuardSource': bind(Path(__file__))}
target.write_bytes((json.dumps(report, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'GuardSha256': h(target.read_bytes())}))
