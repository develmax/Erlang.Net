from pathlib import Path
import hashlib, json, subprocess

root = Path.cwd()
audit_path = root / 'artifacts/bounded-mfa-compatibility-audit/classification.json'
audit = json.loads(audit_path.read_bytes())
sha = lambda raw: hashlib.sha256(raw).hexdigest().upper()
assert sha(audit_path.read_bytes()) == 'B424B90ADA84FB2271BE803A5525A00835EAFC80B3FFB5967E12588678E14A7A'
expected = {'erlang:self/0', 'erlang:node/0', 'erlang:is_map/1', 'erlang:is_binary/1', 'erlang:is_bitstring/1'}
assert set(audit['VerifiedMfas']) == expected
rows = [row for row in audit['ReviewedErlangMfas'] if row['Classification'] == 'verified']
assert len(rows) == 5 and {row['Mfa'] for row in rows} == expected
reports = {}
witnesses = 0
for row in rows:
    assert row['RemainingGaps'] == [] and row['ExhaustivePartitionArgument']
    assert not row['PresenceOrDirectTestAloneUsedForVerification']
    for kind in ('SdkSource', 'OtpSource'):
        source = row[kind]
        repository = root if kind == 'SdkSource' else root.parent / 'otp'
        raw = subprocess.check_output(['git', '-C', str(repository), 'show', source['Commit'] + ':' + source['Path']])
        assert sha(raw) == source['FileSha256']
        assert source['Anchor'] in raw.decode('utf-8').splitlines()[source['Line'] - 1]
    for evidence in row['OriginalComparedEvidence']:
        path = root / evidence['Report']
        if path not in reports:
            raw = path.read_bytes()
            assert sha(raw) == audit['OriginalEvidence']['ReportSha256']
            reports[path] = json.loads(raw)
        actual = reports[path][evidence['Track']][evidence['Index']]
        assert actual['Passed'] is True and actual['Expected'] == actual['Actual']
        assert actual['Source'] == evidence['Source']
        assert sha(actual['Source'].encode()) == evidence['SourceSha256']
        assert actual['Expected'] == evidence['NativeExpected'] == evidence['SdkActual']
        witnesses += 1
for source in audit['SdkSourceUnchangedBetweenActual337AndPublished340']:
    older = subprocess.check_output(['git', 'show', audit['OriginalEvidence']['Head'] + ':' + source['Path']])
    current = subprocess.check_output(['git', 'show', audit['PublishedHead'] + ':' + source['Path']])
    assert older == current and sha(current) == source['Sha256']
    assert (root / source['Path']).read_bytes() == current
result = dict(Status='PASS_INDEPENDENT_FIVE_COMPLETE_LOGICAL_CONTRACT_AUDITS', VerifiedMfas=sorted(expected), SelectedOriginalWitnesses=witnesses, AuditSha256=sha(audit_path.read_bytes()), SourceUnchangedRows=len(audit['SdkSourceUnchangedBetweenActual337AndPublished340']), FreshOriginalExecution=False, RuntimeModified=False, Limit='Logical return/error/side-effect contracts only; no VM internals, scheduling, capacity or performance equivalence claim.')
out = root / 'artifacts/root-bounded-compatibility-review.json'
assert not out.exists()
out.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
print(json.dumps(result))
