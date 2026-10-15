import argparse, hashlib, json, pathlib, re, subprocess

parser = argparse.ArgumentParser()
parser.add_argument('--head', required=True)
args = parser.parse_args()
head = args.head
assert re.fullmatch('[0-9a-f]{40}', head)
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
blob = lambda path: subprocess.check_output(['git', 'cat-file', 'blob', head + ':' + path])
binding_path = pathlib.Path('artifacts/pid-order-sender284-dispatch-source22-helper8.json')
binding_bytes = binding_path.read_bytes()
assert sha(binding_bytes) == '441680DA5B6B003DE4E3774DB6858EC563D8F82A0C0305E58ECF927101955FA3'
bindings = json.loads(binding_bytes)
promotion = json.loads(blob('docs/validation/part-284/main-promotion-revision3.json'))
assert len(bindings['Files']) == len(promotion['Files']) == 22
assert len(bindings['Helper8']) == 8
for row in bindings['Files']:
    actual = next(item for item in promotion['Files'] if item['Path'] == row['Path'])
    assert actual['Sha256'] == row['RawSha256'], row['Path']
for row in bindings['Files'] + bindings['Helper8']:
    data = blob(row['Path'])
    assert sha(data if row['StrictRaw'] else data.replace(b'\r\n', b'\n')) == (
        row['RawSha256'] if row['StrictRaw'] else row['LfSha256']), row['Path']
plan_bytes = blob('tools/Erlang.DistributionSenderObservations/source-plan.json')
plan = json.loads(plan_bytes)
probe = blob('tools/Erlang.DistributionSenderObservations/distribution_sender_observer.erl')
assert plan['Pin'] == 'ad05823719d77c8faee87348ea39513d4e2f99c5'
assert plan['SourceSha256'] == sha(probe)
assert plan['FreshObservationVMs'] == 2 and plan['Scenarios'] == ['sender_plain', 'sender_trace']
assert plan['ObservedSemanticExpectations'] is None and not plan['NativeExecuted'] and not plan['SdkExpectedUsed']
assert plan['RequiredArtifacts'] == ['identities.etf', 'operations.etf', 'outputs.etf', 'cleanup.etf', 'stage']
assert 'Mandatory only' in plan['AbsentFlagCase']
names = blob('tools/Erlang.DistributionSenderObservations/SenderNames.cs').decode()
assert 'SourceSha256 = "' + sha(probe) + '"' in names
assert 'PlanSha256 = "' + sha(plan_bytes) + '"' in names
assert 'NativeExecutions = 2;' in names
project = blob('tools/Erlang.DistributionSenderObservations/Erlang.DistributionSenderObservations.csproj').decode()
for helper in bindings['Helper8']:
    assert '../Erlang.DistributionSignalObservations/framework/' + pathlib.PurePosixPath(helper['Path']).name in project
parent = '6b562cb30540e2fd1d5606215ec087e0dce96d68'
oldblob = lambda path: subprocess.check_output(['git', 'cat-file', 'blob', parent + ':' + path])
# Keep original independently frozen signal5 and trace8 plans separate.
for path in ['tools/Erlang.DistributionSignalObservations/source-plan.json',
             'tools/Erlang.DistributionSignalObservations/distribution_signal_observer.erl',
             '.github/workflows/distribution-signal-observations.yml',
             'tools/Erlang.DistributionTraceObservations/source-plan.json']:
    assert blob(path) == oldblob(path), path
tests = json.loads(blob('docs/validation/part-284/tests.json'))
old_tests = json.loads(oldblob('docs/validation/part-283/historical-tests-282.json'))
names = [row['Name'] for row in tests['Tests']]
old_names = [row['Name'] for row in old_tests['Tests']]
assert tests['Passed'] == len(names) == len(set(names)) == 6563 and tests['Failed'] == 0
assert all(row['Status'] == 'Passed' for row in tests['Tests'])
added = set(names) - set(old_names)
assert added == {'runtime/distribution-sender-payload-selection-and-owned-ingress'}
assert [name for name in names if name not in added] == old_names
assert len([name for name in names if name.startswith('mfa/')]) == 501
receipt = json.loads(blob('docs/validation/part-284/evidence-receipt.json'))
assert receipt['Passed'] == 6563 and receipt['Failed'] == 0 and receipt['SourcePaths'] == 22
assert 'SenderAssertions = session.checks' in blob('tests/Erlang.Tests/DistributionSenderHostChecks.cs').decode()
proof_manifest = json.loads(blob('docs/validation/part-284/sender-proof/manifest.json'))
record = next(row for row in proof_manifest['Records'] if row['InputPath'] ==
              'artifacts/pid-order-sender-revision5-executed/actor-focus8.stdout.raw')
actor_proof = blob(record['RepositoryBlob'])
assert sha(actor_proof) == record['Sha256'] and len(actor_proof) == record['Bytes']
observations = [json.loads(line) for line in actor_proof.decode().splitlines() if line.startswith('{')]
assert next(row for row in observations if 'SenderAssertions' in row)['SenderAssertions'] == 49
assert sum(line.startswith('PASS ') for line in actor_proof.decode().splitlines()) == 8
workflow = blob('.github/workflows/distribution-sender-observations.yml').decode()
assert 'tools/Erlang.DistributionSenderObservations' in workflow
assert 'name: pinned-distribution-sender-observations' in workflow
assert 'if: always()' in workflow and 'continue-on-error' not in workflow
print('PASS committed284/source22/helper8/full6563/oldnames+sender1/unchanged501MFA; native sender2 pending with no SDK oracle')
