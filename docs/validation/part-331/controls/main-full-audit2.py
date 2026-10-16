from pathlib import Path
import hashlib
import json
import os
import subprocess
import time

r = Path.cwd()
d = r / 'artifacts/root-part331-main'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
j = lambda p: json.loads(p.read_bytes())
freeze = j(d / 'source-freeze.json')
closure = j(d / 'source-resource-closure.json')['Files']

def exact():
    for row in closure:
        data = (r / row['Path']).read_bytes()
        assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']

def save(name, value):
    with (d / name).open('xb') as stream:
        stream.write((json.dumps(value, indent=2) + '\n').encode())

exact()
assert len(closure)==freeze['SourceClosurePaths']==2417
assert h((d/'source-resource-closure.json').read_bytes())==freeze['SourceClosureSha256']
exit_receipt=j(d/'full-exit.json')
assert exit_receipt['ExitCode']==0 and exit_receipt['Settled'] and not exit_receipt['TimedOut']
tests = j(r / 'artifacts/tests.json')
assert (tests['Passed'], tests['Failed']) == (6775, 0)
old = j(r / 'docs/validation/part-329/final-main/tests.json')
names = [x['Name'] for x in tests['Tests']]
previous_names = [x['Name'] for x in old['Tests']]
assert len(names)==6775 and len(previous_names)==6747
assert all(x['Status']=='Passed' for x in tests['Tests'])
added = [x for x in names if x not in set(previous_names)]
assert len(added) == 28 and len(names) == len(set(names))
assert [x for x in names if x not in set(added)] == previous_names
integrations = j(r / 'artifacts/integration-results.json')
expected_integration = {'Incremental':'Passed','CleanRebuild':'Passed','LocalTool':'Passed','HybridOutput':'Hello World','Readiness':'Passed','PackageConsumer':'Passed','Build':'Passed','Tests':'Passed','OracleUnavailableReport':'Expected failure recorded','PackagedReadiness':'Passed','DisabledPreprocessing':'Expected failure'}
assert integrations == expected_integration
raw = (r / 'docs/supported-mfas.json').read_bytes()
(d / 'raw-supported-mfas.json').write_bytes(raw)
registry = json.loads(raw)
before = j(d / 'registry-before.json')
key = lambda x: (x['Module'], x['Function'], x['Arity'])
old_rows = {key(x): x for x in before}
assert len(before) == 509 and len(registry) == 515 and len({key(x) for x in registry}) == 515
new_rows = [x for x in registry if key(x) not in old_rows]
assert {key(x) for x in new_rows} == {('proc_lib', n, a) for n, a in [('spawn',1),('spawn',3),('spawn_link',1),('spawn_link',3),('init_ack',1),('init_ack',2)]}
for row in registry:
    if key(row) in old_rows:
        assert {k:v for k,v in row.items() if k != 'Limits'} == {k:v for k,v in old_rows[key(row)].items() if k != 'Limits'}
restored = [old_rows.get(key(x), x) for x in registry]
(r / 'docs/supported-mfas.json').write_bytes((json.dumps(restored, indent=2) + '\n').encode())
assert all(next(x for x in restored if key(x) == k) == v for k,v in old_rows.items())
exact()
guard = dict(Status='PASS_ACTUAL_MAIN331_6775_FULL_ELEVEN_INTEGRATIONS', Passed=6775, Failed=0, IntegrationResults=11, Registry=515, PreviousRegistry=509, RegisteredDelta=6, ErlangMfas=sum(x['Module']=='erlang' for x in registry), ProcLibMfas=6, All6747PreviousOrderedNamesExact=True, All509PreviousRegistryRowsRestoredExact=True, AddedTestNames=added, SourceFreezeSha256=h((d/'source-freeze.json').read_bytes()), SourceClosureSha256=freeze['SourceClosureSha256'], SourceClosurePaths=2417, OriginalExecuted=False, FullExitSha256=h((d/'full-exit.json').read_bytes()), TestReportSha256=h((r/'artifacts/tests.json').read_bytes()), IntegrationReportSha256=h((r/'artifacts/integration-results.json').read_bytes()), RawRegistrySha256=h(raw))
save('full-guard.json', guard)
print(json.dumps(guard))
