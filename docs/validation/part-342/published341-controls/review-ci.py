from pathlib import Path
import importlib.util, json, sys
sys.dont_write_bytecode = True
folder = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('passive341', folder/'passive-common.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
mapping, raw, run, job, artifacts, rows = m.load('ci')
head = '7b43e00608c41303c1b03a25ebd5385d12517ee5'
assert mapping['Head'] == run['head_sha'] == head and run['id'] == 38963689078
assert run['path'] == '.github/workflows/validate.yml' and run['event'] == 'push'
assert run['conclusion'] == job['conclusion'] == 'success'
assert all(step['conclusion'] in ('success','skipped') for step in job['steps'])
assert len(artifacts) == 1
extracted = raw / ('artifact-' + str(artifacts[0]['id']))
test_paths = list(extracted.rglob('tests.json'))
integration_paths = list(extracted.rglob('integration-results.json'))
assert len(test_paths) == len(integration_paths) == 1
tests = m.read(test_paths[0])
names = [row['Name'] for row in tests['Tests']]
assert tests['Passed'] == len(names) == len(set(names)) == 7194 and tests['Failed'] == 0
assert all(row['Status'] == 'Passed' for row in tests['Tests'])
selected = ['docs/validation/part-340/final-candidate/tests.json', 'docs/validation/part-340/final-candidate/integration-results.json', 'docs/validation/part-341/stage-plan.json', 'docs/supported-mfas.json', 'docs/module-readiness.json']
blobs = m.blobs(head, selected)
previous, previous_integration, stage, registry, readiness = [json.loads(blobs[path]) for path in selected]
assert previous['Passed'] == 7193 and previous['Failed'] == 0
assert names == ['tool/readiness-explicit-command-compatibility-admission'] + [row['Name'] for row in previous['Tests']]
assert sum(name.startswith('mfa/') for name in names) == len(registry) == 587
assert sum(name.startswith('mfa/erlang:') for name in names) == 351
assert sum(name.startswith('mfa/proc_lib:') for name in names) == 39
assert m.read(integration_paths[0]) == previous_integration and len(previous_integration) == 11
assert (readiness['VerifiedCompatibleMfas'], readiness['KnownDifferenceMfas'], readiness['NeedsVerificationMfas']) == (5,18,564)
assert readiness['LocalPassedTests'] == 7193
published = m.blobs(head, [row['Path'] for row in stage['Records']])
for row in stage['Records']:
    data = published[row['Path']]
    assert len(data) == row['Bytes'] and m.sha(data) == row['Sha256'], row['Path']
assert len(stage['Records']) == 155 and stage['TestedCSharpPaths'] == 14
assert head.encode() in (raw/('job-'+str(job['id'])+'.log')).read_bytes()
result = dict(Status='PASS_PUBLISHED341_ACTUAL_CI7194_ELEVEN_INTEGRATIONS', Head=head, Run=run['id'], Job=job['id'], Artifact=artifacts[0]['id'], Passed=7194, Failed=0, AllPrevious7193OrderedNamesPreserved=True, NewlyExecutedReportingAdmissionGroup=True, Registry=587, ImplementedModules=14, Verified=5, KnownDifferences=18, NeedsVerification=564, Integrations=11, PublishedStageRowsRehashed=155, RawArtifactApiDigestAndMembersVerified=True, TestReportSha256=m.sha(test_paths[0].read_bytes()), FreshOriginalExecuted=False, SdkOrDownloadedCodeExecutedByReader=False)
target = folder/'ci-independent-guard.json'
assert not target.exists()
target.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result))
