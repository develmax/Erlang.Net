from pathlib import Path
import hashlib
import json
import subprocess
import zipfile
import sys

HEAD = 'a9a616ef9142cb87d564fdd10a6c39f1d182c8f1'
RUN = 38537455237
JOB = 115763706547
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
repo = Path.cwd()
proof_root = repo / 'artifacts/part254-metadata-observation'
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
metadata = json.loads((repo / 'artifacts/part-254-beam-fixtures-metadata.json').read_text(encoding='utf-8-sig'))
assert metadata['Run']['id'] == RUN and metadata['Run']['head_sha'] == HEAD
assert metadata['Run']['status'] == 'completed'
jobs = metadata['Jobs']
assert len(jobs) == 1 and jobs[0]['id'] == JOB and jobs[0]['status'] == 'completed'
archive = repo / 'artifacts/part-254-beam-fixtures.zip'
assert sha(archive) == metadata['ZipSHA256']
assert metadata['Artifact']['name'] == 'beam-fixtures' and metadata['Artifact']['expired'] is False
with zipfile.ZipFile(archive) as zipped:
    hashes = []
    for name in zipped.namelist():
        assert '..' not in Path(name).parts and not Path(name).is_absolute()
        if not name.endswith('/'):
            hashes.append({'Path': name, 'Sha256': hashlib.sha256(zipped.read(name)).hexdigest().upper()})
extracted = repo / 'artifacts/part-254-beam-fixtures'
roots = list(extracted.rglob('metadata-mutations/metadata-mutation-results.json'))
# Failed export may not have results yet: preserve provenance/raw file hashes first.
checkpoint = {'Head': HEAD, 'Run': RUN, 'Job': JOB, 'Conclusion': metadata['Run']['conclusion'], 'ArchiveSha256': sha(archive), 'ArtifactId': metadata['Artifact']['id'], 'ArchiveFiles': hashes, 'Original24Claim': False}
(proof_root / 'raw-artifact-checkpoint.json').write_text(json.dumps(checkpoint, indent=2), encoding='utf-8')
if metadata['Run']['conclusion'] != 'success' or jobs[0]['conclusion'] != 'success':
    print('FAILED original metadata job; raw artifact checkpoint retained before any outcome/fixture changes')
    raise SystemExit(2)
assert len(roots) == 1
output = roots[0].parent.parent
build = json.loads((output / 'source-build-evidence.json').read_text())
assert build['SourceCommit'] == PIN and build['SourceTag'] == 'OTP-29.1.1'
assert build['HarnessCommit'] == HEAD and int(build['RunId']) == RUN
assert build['Job'] == 'fixtures' and build['SourceBuildCompleted'] is True
# Bind verifier and static authoring to publication Git blobs; runtime worktree changes are irrelevant.
for path in ['tools/Erlang.BeamFixtures/verify-metadata-mutations.py', 'tools/Erlang.BeamFixtures/metadata-mutation-declarations.json', 'tools/Erlang.BeamFixtures/verify-on-load-observers.py']:
    expected = subprocess.check_output(['git', 'show', HEAD + ':' + path])
    actual = (repo / path).read_bytes()
    assert actual.replace(b'\r\n', b'\n') == expected.replace(b'\r\n', b'\n'), path
observer = subprocess.run([sys.executable, 'tools/Erlang.BeamFixtures/verify-on-load-observers.py', str(output)], capture_output=True)
(proof_root / 'retained-six-five-onload-four-verifier.log').write_bytes(observer.stdout + observer.stderr)
assert observer.returncode == 0
verify = subprocess.run([sys.executable, 'tools/Erlang.BeamFixtures/verify-metadata-mutations.py', str(output), '--expected-head', HEAD, '--expected-run', str(RUN), '--expected-attempt', str(build['RunAttempt'])], capture_output=True)
(proof_root / 'raw24-verifier.log').write_bytes(verify.stdout + verify.stderr)
assert verify.returncode == 0
actual = json.loads((output / 'metadata-mutations/metadata-mutation-verifier-results.json').read_text())
assert actual['Status'] == 'RAW24_ORIGINAL_EVIDENCE_PASS' and actual['ActualCases'] == 24 and actual['OriginalObserved'] is True
checkpoint.update({'Status': 'ORIGINAL24_RAW_PROOF_PASS', 'Original24Claim': True, 'NativeCases': 24, 'RetainedCompilerFixtures': 6, 'RetainedBodyExecutions': 5, 'OnLoadObserverScenarios': 4, 'SourceBuildSha256': sha(output / 'source-build-evidence.json'), 'BuildLogSha256': sha(output / 'source-build.log'), 'Raw24VerifierSha256': sha(output / 'metadata-mutations/metadata-mutation-verifier-results.json'), 'RuntimeParityClaim': False})
(proof_root / 'final-guard.json').write_text(json.dumps(checkpoint, indent=2), encoding='utf-8')
print('PASS exact published-head/run/pin/artifact/raw24 +retained6/5/onload4; no whole runtime parity claim')
