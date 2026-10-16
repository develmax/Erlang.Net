from pathlib import Path
import hashlib
import importlib.util
import json
import sys

sys.dont_write_bytecode = True
r = Path.cwd()
root = r / 'artifacts/part337-remote-new-review'
spec = importlib.util.spec_from_file_location('passive337', root / 'passive-common.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)
mp, d, run, job, items, rows = m.load('ci')
head = 'add206c03c801cbd68c51d23dbbdb8e36f2d581f'
assert mp['Head'] == run['head_sha'] == head and run['id'] == 38944298251
assert run['path'] == '.github/workflows/validate.yml' and run['event'] == 'push' and run['head_branch'] == 'main'
assert run['conclusion'] == job['conclusion'] == 'success'
assert all(step['conclusion'] in ['success', 'skipped'] for step in job['steps'])
assert len(items) == 1
ex = d / f'artifact-{items[0]["id"]}'
tp = list(ex.rglob('tests.json'))
ip = list(ex.rglob('integration-results.json'))
assert len(tp) == len(ip) == 1
j = lambda p: json.loads(p.read_bytes())
h = lambda b: hashlib.sha256(b).hexdigest().upper()
t = j(tp[0])
paths = ['docs/validation/part-337/final-candidate/tests.json',
         'docs/validation/part-335/final-candidate/tests.json',
         'docs/validation/part-337/final-candidate/integration-results.json',
         'docs/validation/part-337/final-candidate/source-freeze.json']
b = m.blobs(head, paths)
expected, prior, integrations, freeze = [json.loads(b[p]) for p in paths]
names = [x['Name'] for x in t['Tests']]
assert t['Passed'] == len(names) == len(set(names)) == 7017 and t['Failed'] == 0
assert all(x['Status'] == 'Passed' for x in t['Tests'])
assert names == [x['Name'] for x in expected['Tests']]
old = [x['Name'] for x in prior['Tests']]
assert len(old) == 6952 and [n for n in names if n in set(old)] == old
assert len(set(names) - set(old)) == 65
assert sum(n.startswith('mfa/') for n in names) == 540
assert sum(n.startswith('mfa/erlang:') for n in names) == 351
assert sum(n.startswith('mfa/proc_lib:') for n in names) == 29
assert j(ip[0]) == integrations and len(integrations) == 11
stage = j(r / 'artifacts/root-part337-stage-guard.json')
assert stage['Status'] == 'EXACT337_CACHED_STAGE_PASS' and stage['Paths'] == 1783 and stage['Source'] == 54
published = m.blobs(head, [x['Path'] for x in stage['Rows']])
for x in stage['Rows']:
    raw = published[x['Path']]
    assert len(raw) == x['Bytes'] and h(raw) == x['Sha256'], x['Path']
source = m.blobs(head, [x['Path'] for x in freeze['Files']])
for x in freeze['Files']:
    assert h(source[x['Path']]) == x['Sha256'], x['Path']
guard = dict(Status='PASS_ACTUAL_PUBLISHED337_CI7017_ELEVEN_INTEGRATIONS',
             Head=head, Run=run['id'], Job=job['id'], Artifact=items[0]['id'], Attempt=1,
             Passed=7017, Failed=0, Prior6952OrderedNamesExact=True, AddedNames=65,
             RegisteredMfas=540, ErlangMfas=351, ProcLibMfas=29, IntegrationResults=11,
             PublishedBlobsRehashed=1783, PublishedSourcePaths=54,
             OriginalComparisonInferred=False, SdkOrDownloadedCodeExecuted=False)
target = d / 'independent-ci-guard.json'
assert not target.exists()
target.write_text(json.dumps(guard, indent=2) + '\n', encoding='utf-8')
print(json.dumps(dict(Path=target.relative_to(r).as_posix(), Sha256=h(target.read_bytes()), **guard)))
