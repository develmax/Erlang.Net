"""Passive typed review of downloaded result bytes; never runs collector or VM."""
from pathlib import Path, PurePosixPath
import base64
import hashlib
import importlib.util
import json
import subprocess
import sys

sys.dont_write_bytecode = True
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('cleanup_common', HERE / 'passive-common.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
mapping, folder, run, job, items, raw_rows = m.load('cleanup')
head = 'b7484cb0dacc13c04ac818a3c58e4ff4f6afe344'
subject = 'd83ed875d0c77939e20049afcb46fa297fee82dd'
pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
assert mapping['Head'] == head and run['id'] == 38901757920
assert run['path'] == '.github/workflows/recursive-cleanup.yml'
assert run['event'] == 'workflow_dispatch' and run['head_branch'] == 'main'
assert run['conclusion'] == job['conclusion'] == 'success'
assert all(step['conclusion'] in ('success', 'skipped') for step in job['steps'])
assert len(items) == 1
base = folder / f'artifact-{items[0]["id"]}'
comparisons = list(base.rglob('comparison.json'))
assert len(comparisons) == 1
root = comparisons[0].parent
j = m.read
sha = m.sha
comparison = j(comparisons[0])
assert comparison['Status'] == 'PASS_BOTH_GUEST_OBSERVABLE_RECURSIVE_CLEANUP_COMPARISONS'
assert comparison['SubjectCommit'] == subject and comparison['OriginalSourceCommit'] == pin
tool_base = 'tools/Erlang.RecursiveCleanupObservations/'
paths = [tool_base + name for name in ('source-freeze.json', 'named.erl', 'anonymous.erl', 'run.py', 'Program.cs')]
paths += ['.github/workflows/recursive-cleanup.yml', 'tools/Erlang.GroupLeaderOutboundObservations/passive_etf.py']
published = m.blobs(head, paths)
assert sha(published[paths[0]]) == comparison['SourceFreeze']['Sha256'] == 'F57391AB9AE06404FB2ACFA6AD7CC71C8CE33472AAA7BB06BCFCBEE8632FB911'
assert sha(published[tool_base + 'run.py']) == '957538189CB9409C6A196E19655711B28AE8F8C8F3F8503D0DCBA8AF7EAB7412'
freeze = json.loads(published[paths[0]])
all_source = m.blobs(head, [tool_base + row['Path'] for row in freeze['Files']])
for row in freeze['Files']:
    data = all_source[tool_base + row['Path']]
    assert len(data) == row['Bytes'] and sha(data) == row['Sha256']
assert sha(published['.github/workflows/recursive-cleanup.yml']) == freeze['WorkflowSha256']
decoder_path = m.A / 'pack327-group-leader-published-passive-contracts/passive_etf.py'
assert decoder_path.read_bytes() == published['tools/Erlang.GroupLeaderOutboundObservations/passive_etf.py']
decoder_spec = importlib.util.spec_from_file_location('trusted_local_cleanup_etf', decoder_path)
decoder = importlib.util.module_from_spec(decoder_spec)
decoder_spec.loader.exec_module(decoder)
expected_term = ('tuple', (('atom', 'ok'), ('tuple', (('atom', 'killed'), ('atom', 'no_cleanup')))))
expected_text = '{ok,{killed,no_cleanup}}'
provenance = j(root / 'original-provenance.json')
assert provenance['SourceCommit'] == pin and provenance['SubjectCommit'] == subject
installed_root = PurePosixPath(provenance['InstalledOtpRoot'])
assert installed_root.parts[-2:] == ('lib', 'erlang')
assert PurePosixPath(provenance['ResolvedOracle']['Path']) == installed_root / 'bin/erl'
assert provenance['Oracle']['Sha256'] == provenance['ResolvedOracle']['Sha256']
otp_version = subprocess.check_output(['git', '-C', str(m.ROOT.parent / 'otp'), 'show', pin + ':OTP_VERSION'])
assert otp_version.strip() == b'29.1.1'
assert any(row['Sha256'] == sha(otp_version) and row['Bytes'] == len(otp_version) and PurePosixPath(row['Path']).parts[-3:] == ('releases', '29', 'OTP_VERSION') for row in provenance['OtpVersionFiles'])
cases = []
all_pins = None
for name in ('named', 'anonymous'):
    source_sha = sha(published[tool_base + name + '.erl'])
    matched = [row for row in comparison['Cases'] if row['Case'] == name]
    assert len(matched) == 1 and matched[0]['SourceSha256'] == source_sha
    for mode in ('sdk', 'original'):
        prefix = root / f'{mode}-{name}'
        report_path = prefix.with_suffix('.json')
        report = j(report_path)
        attempt = j(prefix.with_suffix('.attempt.json'))
        process = j(prefix.with_suffix('.process.json'))
        assert report['Status'] == 'PASS' and report['Mode'] == mode
        assert report['SourceSha256'] == source_sha and report['SourceUnchanged']
        assert report['Actual'] == report['Expected'] == expected_text
        assert report['Depth'] == 256 and report['NoCleanupObservationMilliseconds'] == 100
        assert report['ReadyHandshakeRequired'] and report['MonitoredExitReasonRequired'] == 'killed'
        assert report['OriginalExecuted'] == (mode == 'original')
        assert report['OriginalExitCode'] == (0 if mode == 'original' else None)
        assert process['ExitCode'] == 0 and process['Settled'] and not process['TimedOut']
        assert 0 <= process['ElapsedSeconds'] < 60 and attempt['DeadlineSeconds'] == 60
        assert process['Report']['Sha256'] == sha(report_path.read_bytes())
        assert attempt['SubjectCommit'] == subject
        assert attempt['Source']['Sha256'] == source_sha
        assert attempt['SourceFreeze']['Sha256'] == comparison['SourceFreeze']['Sha256']
        counterpart = {PurePosixPath(row['Path']).name: row for row in attempt['SubjectBinaries']}
        binaries = {PurePosixPath(row['Path']).name: row for row in attempt['Binaries']}
        assert len(counterpart) == 8 and len(binaries) == 9
        for filename, row in counterpart.items():
            assert binaries[filename]['Sha256'] == row['Sha256']
            assert '/artifacts/subject/tools/Erlang.Differential/bin/' in row['Path']
        pins = {name: row['Sha256'] for name, row in counterpart.items()}
        if all_pins is None:
            all_pins = pins
        assert all_pins == pins
        for assembly in report['Assemblies']:
            assert binaries[assembly['Name'] + '.dll']['Sha256'] == assembly['Sha256']
        assert matched[0]['Sdk' if mode == 'sdk' else 'Original']['Sha256'] == sha(report_path.read_bytes())
        if mode == 'sdk':
            assert report['StandardOutput'] == report['StandardError'] == ''
        else:
            assert report['StandardError'] == ''
            output = report['StandardOutput']
            begin, end = 'ERLANG_NET_ETF_BEGIN:', ':ERLANG_NET_ETF_END'
            assert output.count(begin) == output.count(end) == 1
            before, framed = output.split(begin)
            encoded, after = framed.split(end)
            assert not before.strip() and not after.strip()
            raw = base64.b64decode(encoded, validate=True)
            decoded, consumed = decoder.decode_one(raw)
            assert consumed == len(raw) and decoded == expected_term
    cases.append(dict(Case=name, SourceSha256=source_sha, Sdk=expected_text, Original=expected_text, OriginalRawEtfIndependentlyDecoded=True))
value = dict(Status='PASS_ACTUAL_CORRECTED330_ORIGINAL_AND_SUBJECT329_SDK_RECURSIVE_CLEANUP_TWO_CASES', Passed=True, Head=head, SubjectCommit=subject, Run=run['id'], Job=job['id'], Artifact=items[0]['id'], Attempt=1, PinnedOtpCommit=pin, OriginalOtpVersion='29.1.1', CorrectInstalledRoot=str(installed_root), Cases=cases, OriginalCasesPassed=2, SdkCasesPassed=2, EightSubjectDllCounterpartsMatchCopiedAndLoadedIdentities=True, ActualRemoteDllBytesExportedOrLocallyRehashed=False, PublishedFrozenSourceFilesVerified=len(freeze['Files']), RawRowsRehashed=len(raw_rows), OriginalEtfIndependentlyDecodedUsingPreviouslyReviewedLocalHelper=True, RawNormalization=False, Scope='Finite guest-observable named and anonymous recursive try-after external-kill behavior at depth256 and quiet100ms. Host leases/Unwound, physical stack/memory and arbitrary scheduler interleavings remain separate.', ReviewerExecutedBuildVmSdkNetworkDownloadedCodeOrMainMutation=False, RawZipSha256=sha((folder / f'artifact-{items[0]["id"]}.zip').read_bytes()), ComparisonSha256=sha(comparisons[0].read_bytes()), ProvenanceSha256=sha((root / 'original-provenance.json').read_bytes()))
target = HERE / 'independent-cleanup-guard.json'
assert not target.exists()
target.write_bytes((json.dumps(value, indent=2) + '\n').encode())
print(json.dumps(dict(Guard=str(target), Sha256=sha(target.read_bytes()))))
