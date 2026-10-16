import ast
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def write(name, value):
    path = ROOT / 'artifacts' / name
    assert not path.exists()
    path.write_bytes((json.dumps(value, indent=2) + '\n').encode())
    print(str(path), sha(path))

new = ROOT / 'artifacts/non-tail-stack329-root-candidate7'
old = ROOT / 'artifacts/non-tail-stack329-root-candidate6'
assert sha(new/'source-freeze.json') == '72C8D5115B63E9C421F35ED34858F8093B466960056D586265FFF66D0EF58D70'
freeze = load(new/'source-freeze.json')
assert sha(new/'source-resource-closure.json') == freeze['SourceClosureSha256']
rows = load(new/'source-resource-closure.json')['Files']
prior = {r['Path']: r for r in load(old/'source-resource-closure.json')['Files']}
owned = {r['Path'] for r in freeze['Files']}
normalized = set(freeze['ExplicitOwnedCrLfToLfPaths'])
assert len(rows) == 2397 and len(owned) == 7 and len(normalized) == 2
changed = []
for row in rows:
    path = new/'candidate'/row['Path']
    data = path.read_bytes()
    assert len(data) == row['Bytes'] and sha(path) == row['Sha256']
    before = (old/'candidate'/row['Path']).read_bytes()
    if before != data:
        changed.append(row['Path'])
        assert row['Path'] in normalized and before.replace(b'\r\n', b'\n') == data and b'\r' not in data
    else:
        assert row == prior[row['Path']]
assert set(changed) == normalized
checks = (new/'candidate/tests/Erlang.Tests/NonTailStackLifecycleHostChecks.cs').read_text()
assert 'unwind.Task.WaitAsync' in checks and 'Names.Unwound' in checks
write('non-tail-stack329-candidate7-putdata-source-peer1.json', {
    'Status':'PASS_SOURCE_ONLY_CANDIDATE7_EXPLICIT_TWO_TEST_FILE_LF_NORMALIZATION', 'Passed':True,
    'CandidateFreezeSha256':sha(new/'source-freeze.json'), 'SourceClosureSha256':freeze['SourceClosureSha256'],
    'PreviousFreezeSha256':sha(old/'source-freeze.json'), 'SourceRowsRehashed':len(rows),
    'OnlyChangedPaths':changed, 'AllOther2395FilesRawExactCandidate6':True,
    'ProductionRawExactCandidate6AndActualFocus3':True, 'UnwoundBarrierAndAllAssertionsUnchanged':True,
    'Qualification':'Candidate6 formatter lexical precondition refused before build because raw literal line endings changed; candidate7 intentionally normalizes only the two owned test files before the unchanged strict lexical guard. This is source-only clearance, not actual execution or generic stack safety proof.',
    'ReviewerExecutedSdkBuildVmNetworkMainGitMutation':False})

g1=ROOT/'artifacts/part329-main-gates-template1'
g2=ROOT/'artifacts/part329-main-gates-template2'
native=ROOT/'artifacts/part329-main-native-template1'
assert sha(g2/'proposal-freeze.json') == '9C3DCDE030984D8F9B6D9A6443C7CF014058830B9DA12EA2CBD2171E5F86A613'
assert sha(native/'proposal-freeze.json') == '12C901E7406F7B484B92E2E6CD5BF47145000684F6E76D60F038E90295CDDDBE'
for directory in (g2,native):
    for row in load(directory/'proposal-freeze.json')['Files']:
        path=directory/row['Path']
        assert path.stat().st_size==row['Bytes'] and sha(path)==row['Sha256']
        if path.suffix=='.py': ast.parse(path.read_text())
text=(g2/'main329.py').read_text()
additions=["assert managed['ExpectedCases']==managed['Executed']==managed['FixturePassed']==len(managed['Cases'])==8 and managed['FixtureFailed']==managed['Unexecuted']==0\n", "assert len(ledger['NewHostRows'])==1 and len(ledger['HostActuals'])==10\n"]
for addition in additions:
    assert text.count(addition)==1
    text=text.replace(addition,'')
assert text==(g1/'main329.py').read_text()
assert (g2/'run329-main-full.ps1').read_bytes()==(g1/'run329-main-full.ps1').read_bytes()
write('part329-main-native-gates-putdata-source-peer1.json', {
    'Status':'PASS_SOURCE_ONLY_MAIN_GATES_REVISION2_AND_NATIVE_TEMPLATE1', 'Passed':True,
    'MainProposalFreezeSha256':sha(g2/'proposal-freeze.json'), 'NativeProposalFreezeSha256':sha(native/'proposal-freeze.json'),
    'MainSourceSha256':sha(g2/'main329.py'), 'NativeRunnerSha256':sha(native/'run329-main-native.py'),
    'NativeGuardSha256':sha(native/'guard329-main-native.py'),
    'Revision1CardinalityFindingResolved':True,
    'Findings':[
        'Before promotion exact eight successful managed cases, one new HOST row and ten successful HOST actual selectors are required; revision2 changes only these two assertions and retains the runner bytes.',
        'Final source, peer, ledger and actual reports are mandatory hash-bound inputs. Twelve owned/seven C#/2397 sources and published328 parent/raw and canonical Git models are checked before promotion.',
        'Full guard requires 6747 ordered contracts and eleven integrations, preserves all6738 old ordered names, restores thirty prior custom Limits and requires all registry rows exact parent; no MFA scope suffix is appended.',
        'Native template is a narrow part/count adaptation of the successful328 runner/guard, with exact unchanged configuration8742307. Trusted binaries/layout/readers and source/full/final-peer identities remain mandatory; three fresh bounded processes retain raw outcomes and before/after bindings.',
        'Source-only permission for Root execution after actual final composition/ledger inputs exist; no full/native/readiness result is asserted and no original or physical-memory parity is inferred.'
    ],
    'ReviewerExecutedEntrypointSdkVmNativeMainGitMutation':False})
