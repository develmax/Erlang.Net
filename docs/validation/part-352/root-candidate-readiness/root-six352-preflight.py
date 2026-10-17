from pathlib import Path
import collections, hashlib, json, subprocess

r = Path.cwd()
p = r / 'artifacts/six-predicate-published351-contract-proposal'
o = r / 'artifacts/root-six352-preflight.json'
assert not o.exists()
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
load = lambda path: json.loads(path.read_bytes())
head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=r, text=True).strip()
assert head == '547a2dd266586589a90c7883dac88d09a5a1de20'
proposal = load(p / 'proposal.json')
assert sha((p / 'proposal.json').read_bytes()) == 'C4A338D5E454999D399F4A3EB9D5379AD3F5EF3EAAD33CC5FBEA2F9537237D0E'
before = load(r / 'docs/mfa-compatibility.json')
after = load(p / 'ledger-candidate.json')
assert sha((r / 'docs/mfa-compatibility.json').read_bytes()) == proposal['BaseLedgerSha256']
assert sha((p / 'ledger-candidate.json').read_bytes()) == proposal['CandidateLedgerSha256']
key = lambda x: (x['Module'], x['Function'], x['Arity'])
changed = [(a, b) for a, b in zip(before['Entries'], after['Entries']) if a != b]
expected = {('erlang', name, 1) for name in ('is_boolean', 'is_function', 'is_list', 'is_pid', 'is_port', 'is_reference')}
assert len(before['Entries']) == len(after['Entries']) == 587
assert [key(x) for x in before['Entries']] == [key(x) for x in after['Entries']]
assert {key(b) for a, b in changed} == expected and len(changed) == 6
for a, b in changed:
    assert a['Status'] == 'Needs verification' and b['Status'] == 'Verified compatible'
    assert not b['Limits'] and b['Scope']
    assert {'OriginalOtpComparison', 'CompatibilityAudit'} <= {x['Kind'] for x in b['Evidence']}
assert after['VerifiedSourceFiles'][:74] == before['VerifiedSourceFiles']
assert len(after['VerifiedSourceFiles']) == 77
for row in after['VerifiedSourceFiles']:
    actual = (r / row['Path']).read_bytes()
    assert sha(actual) == row['Sha256']
    assert actual == subprocess.check_output(['git', 'show', head + ':' + row['Path']], cwd=r)
proof = load(p / 'proof-copy-manifest.json')['Files']
assert len(proof) == 98
targets = {}
for row in proof:
    data = (r / row['SourcePath']).read_bytes()
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
    assert row['TargetPath'].startswith('docs/validation/complete-six-predicate-audit/')
    assert row['TargetPath'] not in targets
    targets[row['TargetPath']] = data
for entry in after['Entries']:
    for ev in entry['Evidence']:
        target = 'docs/' + ev['Path']
        data = targets[target] if target in targets else (r / target).read_bytes()
        assert sha(data) == ev['Sha256']
anchors = json.loads(targets['docs/validation/complete-six-predicate-audit/pinned-source-anchors.json'])
for row in anchors:
    actual = subprocess.check_output(['git', '-C', str(r.parent / 'otp'), 'show', row['Commit'] + ':' + row['Path']])
    assert sha(actual) == row['Sha256'] and len(actual) == row['Bytes']
    lines = actual.decode('utf-8').splitlines()
    for anchor in row['Anchors']:
        assert anchor['Needle'] in lines[anchor['Line'] - 1]
report = {'Status': 'ROOT_PREFLIGHT_EXACT_SIX_TRANSITIONS_AND_PINNED_BRANCH_BINDINGS_PASS', 'Head': head,
    'Counts': dict(collections.Counter(x['Status'] for x in after['Entries'])),
    'Other581EntriesExact': True, 'PriorTenVerifiedExact': True,
    'CurrentPublishedSourceGuardsExact': 77, 'ProofFilesRehashed': 98,
    'AllLedgerEvidenceRehashed': True, 'PinnedSourceFilesRehashed': len(anchors),
    'MainChanged': False, 'NewRuntimeOrNativeExecution': False,
    'Scope': 'Root read branch and routing audit. Mechanical preflight complements independent full-contract review; does not itself certify complete contracts.'}
o.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
print(json.dumps(report))
