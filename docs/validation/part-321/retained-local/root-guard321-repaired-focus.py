from pathlib import Path
import hashlib, json

r = Path.cwd()
a = r / 'artifacts'
g = a / 'root-part321-composed5'
old = a / 'root-part321-composed3'
d = a / 'root-part321-historical-red2'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_text(encoding='utf-8-sig'))
bind = lambda p: {'Path': p.relative_to(r).as_posix(), 'Bytes': p.stat().st_size, 'Sha256': h(p.read_bytes())}
target = g / 'actual-core-focus-guard.json'
assert not target.exists()
fp = g / 'focus-source-freeze.json'
cl = g / 'focus-source-resource-closure.json'
assert h(fp.read_bytes()) == '47C76299A42E01356C7BADD65E845ED93AF53AAE931F054EB581ECA1866A21F5'
assert h(cl.read_bytes()) == read(fp)['SourceClosureSha256'] == 'DAFC935D62C41457400553BE6AD2608F47952E5BB265A2BB2577531C82091450'
rows = read(cl)['Files']
assert len(rows) == 2307 and len(read(fp)['Files']) == 9
for e in rows:
    raw = (g / 'candidate' / e['Path']).read_bytes()
    assert len(raw) == e['Bytes'] and h(raw) == e['Sha256']
assert read(g / 'target-build-exit.json')['ExitCode'] == 0
assert (g / 'tokens-before.json').read_bytes() == (g / 'tokens-after.json').read_bytes()
assert len(read(g / 'tokens-before.json')) == 9
def assemblies(report):
    for e in report['Identities']:
        assert h(Path(e['Path']).read_bytes()) == e['Sha256']
green = read(g / 'actual-green-focus.json')
assert green['Status'] == 'PASS' and len(green['Cases']) == 10 and all(e['Status'] == 'PASS' for e in green['Cases'])
assert any(e['Name'] == 'DistributionRawSignalHostChecks' for e in green['Cases'])
assemblies(green)
previous = read(old / 'focus-source-resource-closure.json')['Files']
gm = {e['Path']: e for e in rows}
changed = {e['Path'] for e in previous if e != gm[e['Path']]}
assert changed == {'src/Erlang.Runtime/ProcessRuntime.DistributionLinks.cs', 'tests/Erlang.Tests/DistributionGuestLinkHostChecks.cs'}
assert h((old / 'actual-core-focus-guard.json').read_bytes()) == 'A96F90EB4D73F5BB01E1E32059C071F43410601C2A90570EF90D1A79898DA2BB'
redreports = []
for n in ['actual-link-red-focus.json', 'actual-unlink-red-focus.json']:
    report = read(d / n)
    assert report['Status'] == 'FAIL' and len(report['Cases']) == 1
    assert 'TakeNow(' in report['Cases'][0]['Exception'] and 'TimeoutException' not in report['Cases'][0]['Exception']
    assemblies(report)
    redreports.append(bind(d / n))
for n in ['DistributionGuestLinkHostNames.cs', 'DistributionGuestLinkPublicRouteHostChecks.cs', 'DistributionRawSignalHostChecks.cs']:
    name = 'tests/Erlang.Tests/' + n
    assert (g / 'candidate' / name).read_bytes() == (d / 'candidate' / name).read_bytes()
regression = read(old / 'actual-raw-link-retirement-regression-focus.json')
baseline = read(old / 'actual-published320-raw-link-retirement-focus.json')
assert regression['Status'] == 'FAIL' and 'DistributionRawSignalHostChecks.cs:line 182' in regression['Cases'][0]['Exception']
assert baseline['Status'] == 'PASS' and len(baseline['Cases']) == 1
assemblies(regression)
assemblies(baseline)
first = a / 'root-part321-main1/first-full-failure-receipt.json'
assert h(first.read_bytes()) == 'DB948C4DC131A6AE35B542FAB720A5D67E770DDFCD1B853C256C53BBE714DFE4'
assert (read(first)['Passed'], read(first)['Failed']) == (6677, 1)
for key in ['FullLog', 'FullExit', 'ImmutableTests', 'RawRegeneratedMfas']:
    e = read(first)[key]
    assert h((r / e['Path']).read_bytes()) == e['Sha256']
peer = a / 'connected-guest-links321-revision5-allocator309-source-peer1/review.json'
assert h(peer.read_bytes()) == '7C62AB39FCED4C3D10D617CE8C3FBAA39574525AC33C8B9E70B12E5A750AA966'
report = {'Status': 'PASS_ACTUAL_REPAIRED321_TEN_GREEN_TWO_PUBLISHED320_MISSING_OUTPUT_RED_AND_FULL_REGRESSION_REPAIR', 'Passed': True, 'SourceFreeze': bind(fp), 'SourceClosure': bind(cl), 'SourceFiles': 2307, 'CSharpPaths': 9, 'GreenHostGroups': 10, 'LifecycleSubgroups': 12, 'GreenReport': bind(g / 'actual-green-focus.json'), 'ActualAssemblies': green['Identities'], 'NineTokenStreamsExact': True, 'SharedPublished320PublicHostSourcesRawExact': True, 'HistoricalMissingOutputReports': redreports, 'PreservedFirstFullFailure': bind(first), 'Previous321RawSignalFailure': bind(old / 'actual-raw-link-retirement-regression-focus.json'), 'Published320SameRawSignalPass': bind(old / 'actual-published320-raw-link-retirement-focus.json'), 'OnlyTwoChangedSourcesFromFirstFocusedCandidate': sorted(changed), 'Revision5SourcePeer': bind(peer), 'NoAssertionsWeakenedOrHistoricalFailuresOverwritten': True, 'NewOriginalCollectorExecuted': False, 'MainFullRerunSucceeded': False, 'GuardSource': bind(Path(__file__))}
target.write_bytes((json.dumps(report, indent=2) + '\n').encode())
print(json.dumps({'Status': report['Status'], 'GuardSha256': h(target.read_bytes())}))
