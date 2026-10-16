"""Passive replay only: never loads or executes managed/native binaries."""
from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
G = ROOT / 'artifacts/connected-guest-exit322-focus2'
R = ROOT / 'artifacts/connected-guest-exit322-historical-red1'
P = ROOT / 'artifacts/root-part321-composed5'
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
bindings = []

def bind(p, expected=None):
    b = p.read_bytes()
    digest = sha(b)
    assert expected is None or digest == expected, str(p)
    row = dict(Path=p.relative_to(ROOT).as_posix(), Bytes=len(b), Sha256=digest)
    bindings.append(row)
    return row

def closure(folder, filename, expected, count):
    p = folder / filename
    bind(p, expected)
    rows = read(p)['Files']
    assert len(rows) == count and len({x['Path'] for x in rows}) == count
    for x in rows:
        b = (folder / 'candidate' / x['Path']).read_bytes()
        assert len(b) == x['Bytes'] and sha(b) == x['Sha256'], x['Path']
    return {x['Path']: x for x in rows}

bind(G/'formatted-source-freeze.json', '4370C19D8D454424956D3EFEA314269624B976C557352554C1AFAFA2E332629C')
bind(R/'source-freeze.json', 'F78327392B885E2D79B9A098C008E38CE85F49C3D44A107504BC47FDAC2A5096')
green = closure(G, 'formatted-source-resource-closure.json', '0AAC713323A387F424034A35CE78665161F13BCA5DC2B5C9BF9009EA6412C566', 2314)
red = closure(R, 'source-resource-closure.json', 'ECA8E10BA55B66FB93BC0E6FB67BCA9A952F08A19921FAFDD9D14060F063EDB5', 2309)
parent = closure(P, 'focus-source-resource-closure.json', 'DAFC935D62C41457400553BE6AD2608F47952E5BB265A2BB2577531C82091450', 2307)
assert all(red[k] == v for k,v in parent.items())
helpers = {x['Path'] for x in read(R/'source-freeze.json')['Files']}
assert set(red)-set(parent) == helpers and len(helpers) == 2
assert all(red[k] == green[k] for k in helpers)
assert all((R/'candidate'/k).read_bytes() == (G/'candidate'/k).read_bytes() for k in helpers)
for n in ('tokens-before.json', 'tokens-after.json'): bind(G/n)
assert (G/'tokens-before.json').read_bytes() == (G/'tokens-after.json').read_bytes()
assert len(read(G/'tokens-before.json')) == 10
bind(G/'actual-green-guard.json', 'B726EBA81DF1965B450E225D9BB222CC832EEB452DA19ABBB913FC0C93A8FB2F')
bind(ROOT/'artifacts/connected-guest-exit322-actual-green-beam310-peer.json', 'D93E3BC553F9BCE7642505D7C77AA797F589D62728A0DA01026BE7F312485A75')
bind(G/'actual-green-focus2.json', '4FBDED73EC6D5CD82D34983A9F88CDB35BE33A076967B576F7E55EC436818780')
gr = read(G/'actual-green-focus2.json')
assert gr['Status']=='PASS' and len(gr['Cases'])==13 and all(x['Status']=='PASS' for x in gr['Cases'])
def identities(report):
    result = {}
    for x in report['Identities']:
        p = Path(x['Path'])
        assert p.is_relative_to(ROOT/'artifacts') and sha(p.read_bytes()) == x['Sha256']
        result[x['Name']] = dict(Path=str(p), Sha256=x['Sha256'])
    assert 'Erlang.Tests' in result and 'Erlang.Runtime' in result
    return result
gi = identities(gr)
for folder in (G,R):
    bind(folder/'target-build-exit.json'); bind(folder/'target-build.log')
    assert read(folder/'target-build-exit.json')['ExitCode'] == 0
    log = (folder/'target-build.log').read_text(encoding='utf-8-sig')
    assert ('Предупреждений: 0' in log and 'Ошибок: 0' in log) or ('0 Warning(s)' in log and '0 Error(s)' in log)
rf = read(R/'source-freeze.json')
ri = []
for prefix,method in [('exit-two','ExitTwoPublishes'),('exit-three','ExitThreePublishes')]:
    for tail in ('-actual-red.json','-actual-red.log','-exit.json'): bind(R/(prefix+tail))
    report = read(R/(prefix+'-actual-red.json'))
    assert report['Status']=='FAIL' and len(report['Cases'])==1
    case = report['Cases'][0]
    assert case['Name']=='DistributionGuestExitPublicRouteHostChecks:'+method and case['Status']=='FAIL'
    error=case['Exception']
    assert rf['ExpectedExceptionType']+': '+rf['ExpectedExceptionMessage'] in error
    assert ':line '+str(rf['ExpectedMissingPacketAssertionLine']) in error
    assert all(t not in error for t in ('TimeoutException','TypeLoadException','MissingMethodException','MissingMemberException','NullReferenceException'))
    assert read(R/(prefix+'-exit.json'))['ExitCode']==1
    assert any(x['Name']==case['Name'] and x['Status']=='PASS' for x in gr['Cases'])
    ri.append(identities(report))
assert ri[0] == ri[1] and gi['Erlang.Runtime']['Sha256'] != ri[0]['Erlang.Runtime']['Sha256']
source = (R/'candidate/tests/Erlang.Tests/DistributionGuestExitPublicRouteHostChecks.cs').read_text().splitlines()
assert 'Check(buffer is { SegmentCount: 1 });' in source[25]
for name in ('RUN-RED-PROPOSAL.ps1','review-red.py','verify-source.py','runner-binding.json'): bind(R/name)
rb = read(R/'runner-binding.json'); bind(ROOT/rb['Path'], rb['Sha256'])
result = dict(Status='PASS_ACTUAL322_GREEN13_AND_GENUINE_HISTORICAL321_MISSING_PACKET_RED2', Passed=True,
    GreenGroups=13, HistoricalRedCases=2, GreenSourceRows=2314, HistoricalRedSourceRows=2309,
    Historical321ParentRowsRawExact=2307, SameTwoFormattedHostFilesRawExact=True,
    RedClassification='Actual exit/2 and exit/3 return true, then immediate real output packet assertion fails at line 26; no timeout or missing API/member.',
    SourceBeforeAuthority='Frozen source closures created before actual builds; recipe checks before and after each complete RED run.',
    SourceAfterReplay='All current 2309 RED and 2314 GREEN authored rows independently rehashed after execution.',
    GreenAssemblies=gi, HistoricalRedAssemblies=ri[0], Bindings=bindings,
    OriginalExecuted=False, MainFullPassed=False, ReviewerExecutedSdkNativeVmOrBuild=False)
destination = OUT/'guard.json'
assert not destination.exists(), 'Fresh report required; preserve prior result'
destination.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(Path=str(destination),Sha256=sha(destination.read_bytes()),Bindings=len(bindings))))
