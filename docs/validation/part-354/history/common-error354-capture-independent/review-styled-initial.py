import hashlib
import json
import pathlib
import re

root = pathlib.Path(__file__).resolve().parents[2]
out = pathlib.Path(__file__).resolve().parent
proposal = root / 'artifacts/size-bif354-permanent-test-proposal2'
view = root / 'artifacts/size-bif354-owned-style-view'


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


inputs = json.loads((proposal / 'style-inputs.json').read_bytes())
token_raw = (root / 'artifacts/root-size354-style-token-identity.json').read_bytes()
token = json.loads(token_raw)
rows = {row['Path']: row for row in token['Files']}
assert len(rows) == len(inputs['Files']) == 50
current = []
for row in inputs['Files']:
    source = (root / row['SourcePath']).read_bytes()
    after = (view / row['Path']).read_bytes()
    assert sha(source) == row['Sha256'] == rows[row['Path']]['BeforeSha256']
    assert sha(after) == rows[row['Path']]['AfterSha256']
    current.append({'Path': row['Path'], 'Bytes': len(after), 'Sha256': sha(after)})
assert len([row for row in token['Files'] if row['BeforeSha256'] != row['AfterSha256']]) == 10
fixture_raw = (proposal / 'fixtures.json').read_bytes()
fixture = json.loads(fixture_raw)
sources = (view / 'tools/Erlang.Differential/SizeErrorCaseSources.cs').read_bytes().decode()
seen = {}
for match in re.finditer(r'public const string (\w+) = """\r?\n(.*?)(^[ \t]*)""";', sources, re.M | re.S):
    name, body, indent = match.groups()
    assert body.endswith('\n')
    body = body[:-2] if body.endswith('\r\n') else body[:-1]
    seen[name] = ''.join(line[len(indent):] if line.startswith(indent) else line for line in body.splitlines(True)).encode()
assert len(seen) == len(fixture['Cases']) == 23
for row in fixture['Cases']:
    key = ''.join(word.title() for word in row['Module'].split('_'))
    assert sha(seen[key]) == row['Sha256']
ledger_raw = (root / 'docs/mfa-compatibility.json').read_bytes()
ledger = json.loads(ledger_raw)
guards = {row['Path']: row for row in ledger['VerifiedSourceFiles']}
assert len(guards) == 89
for row in guards.values():
    assert sha((root / row['Path']).read_bytes()) == row['Sha256']
runtime = {row['Path']: row for row in current if row['Path'].startswith('src/')}
assert len(runtime) == 32
union = []
for path in sorted(set(guards) | set(runtime)):
    data = (view / path).read_bytes() if path in runtime else (root / path).read_bytes()
    union.append({'Path': path, 'Sha256': sha(data), 'Origin': 'StyledRuntime32' if path in runtime else 'UnchangedPrior89'})
assert len(union) == 114
program = (view / 'tests/Erlang.Tests/Program.cs').read_bytes().decode()
early = program[program.index('foreach (var fixture in Erlang.Differential.CompiledModuleCases.All.Where'):]
early = early[:early.index('\n{')]
assert 'SizeErrorCases' not in early
report = {
    'Status': 'REFUSED_INITIAL_STYLED50_COMPOSITION_TWO_CORRECTIONS_REQUIRED',
    'PositiveChecks': {
        'OwnedRows': current,
        'RoslynTokenProofSha256': sha(token_raw),
        'RoslynToolSourceSha256': sha((root / 'artifacts/Style354TokenAudit/Program.cs').read_bytes()),
        'Method': 'Independently bound all50 before/current hashes and reviewed actual Roslyn token+directive comparison source; raw literal tokens included and syntax errors refused. Reviewer did not rerun SDK.',
        'ChangedLayoutFiles': 10,
        'FixtureSourceBytesExact': 23,
        'FixtureManifestSha256': sha(fixture_raw),
    },
    'BlockingFindings': [
        'Default filename validation branch lost when source13 complete SourceLoadNif file replaced actualfocus6 corrected branch; validator is unreachable in initial styled view.',
        'Program early CompiledModuleCases loop excludes only SchedulerAffinityCases; new23 SizeErrorCases execute before old HOST suffix, breaking old7313 ordered prefix. Need early exclusion plus tail append of23 using unchanged compiled policy.',
    ],
    'PriorLedgerSha256': sha(ledger_raw),
    'ExistingVerifiedMfas': [{'Module': row['Module'], 'Function': row['Function'], 'Arity': row['Arity']} for row in ledger['Entries'] if row['Status'] == 'Verified compatible'],
    'ProvisionalGuardUnion': union,
    'GuardUnionCount': 114,
    'GuardOriginCounts': {'Prior': 89, 'Runtime': 32, 'Overlap': 7, 'New': 25},
    'GuardScope': 'Conservative prior89 union all32 owned runtime. Provisional hashes only: SourceLoadNif correction must be rebound, not blindly adopted into final ledger.',
    'ExistingSixteenScopeReview': 'CoreModules diff changes four invalid size branches only; prior16 total predicates/self/node branches and term-admission helpers unchanged. ProcessRuntime delta only synchronized BacktraceDepth; sequence registration adds distinct flag branch; common capture successful native body/returned terms unchanged. Narrow source13 preservation review retained. Final exact composed source and metadata guard proof pending.',
    'ConventionRemainder': 'Reinstall HOST reflection literals Entries and Item2 remain inline; extract domain constants during narrow successor.',
    'SdkExecutedByReviewer': False,
    'NetworkExecuted': False,
    'MainMutation': False,
    'StatusPromotion': False,
}
assert len(report['ExistingVerifiedMfas']) == 16
target = out / 'styled-initial-report.json'
target.write_bytes((json.dumps(report, ensure_ascii=False, indent=2) + '\n').encode())
print(sha(target.read_bytes()))
