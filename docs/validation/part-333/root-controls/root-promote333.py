from pathlib import Path
import hashlib
import json
import subprocess

r = Path.cwd()
d = r / 'artifacts/proc-lib333-full-source'
j = lambda p: json.loads(p.read_bytes())
h = lambda b: hashlib.sha256(b).hexdigest().upper()
assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=r).decode().strip() == 'aaf48552bee429b0447d3e28e8274d34ad590d73'
f = j(d / 'source-freeze.json')
g = j(d / 'full-guard.json')
assert h((d / 'source-freeze.json').read_bytes()) == 'AA5249A3F81F74F825626EAAF1DACB1E44564105B6BD0589DB5A0DD9CA4046C4'
assert g['SourceClosureSha256'] == f['SourceClosureSha256']
assert g['PackageResourceManifestSha256'] == f['PackageResourceManifestSha256']
expected = {'Readiness':'Passed','PackagedReadiness':'Passed','Build':'Passed','Tests':'Passed','OracleUnavailableReport':'Expected failure recorded','HybridOutput':'Hello World','Incremental':'Passed','DisabledPreprocessing':'Expected failure','CleanRebuild':'Passed','PackageConsumer':'Passed','LocalTool':'Passed'}
for key, value in {'Status':'PASS_ACTUAL_ISOLATED333_FULL','Passed':6871,'Failed':0,'PreviousOrderedNames':6822,'PreviousOrderedNamesUnchanged':True,'Registry':528,'PreviousRegistry':521,'All521PreviousRegistryRowsRestoredExact':True,'ErlangMfas':351,'ProcLibMfas':19,'IntegrationResults':expected,'SourceClosurePaths':2439,'PackageResourceRows':6}.items():
    assert g[key] == value, key
for key, name in {'SourceFreezeSha256':'source-freeze.json','SourceClosureSha256':'source-resource-closure.json','TestReportSha256':'tests.json','FullExitSha256':'full-exit.json','PackageResourceManifestSha256':'package-resources.json'}.items():
    assert g[key] == h((d / name).read_bytes()), key
exit_result = j(d / 'full-exit.json')
assert exit_result['ExitCode'] == 0 and exit_result['Settled'] and not exit_result['TimedOut']
assert j(d / 'integration-results.json') == expected
tests = j(d / 'tests.json')
assert tests['Passed'] == len(tests['Tests']) == 6871 and tests['Failed'] == 0
assert all(x['Status'] == 'Passed' for x in tests['Tests'])
names = [x['Name'] for x in tests['Tests']]
old_names = [x['Name'] for x in j(r / 'docs/validation/part-332/final-candidate/tests.json')['Tests']]
assert len(names) == len(set(names)) and len(old_names) == 6822
assert [x for x in names if x in set(old_names)] == old_names
previous = j(d / 'registry-before.json')
registry = j(d / 'supported-mfas.json')
key = lambda x: (x['Module'], x['Function'], x['Arity'])
actual = {key(x):x for x in registry}
assert len(previous) == 521 and len(registry) == len(actual) == 528
assert all(actual[key(x)] == x for x in previous)
old_path = r / 'artifacts/proc-lib332-full-source/source-resource-closure.json'
assert h(old_path.read_bytes()) == '56908A656DEA730DE49C960EB82B4C5656A46E5B94B0E77E75F98BAA07A12F95'
old = j(old_path)['Files']
assert len(old) == 2431 and len(f['Files']) == f['OwnedPaths'] == 18
for row in old + j(d / 'package-resources.json')['Files']:
    data = (r / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']
owned = {x['Path']:x for x in f['Files']}
closure = j(d / 'source-resource-closure.json')['Files']
assert len(closure) == 2439
for row in closure:
    p = d / 'source' / row['Path'] if row['Path'] in owned else r / row['Path']
    data = p.read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']
for row in f['Files']:
    data = (d / 'source' / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256']
for row in f['Files']:
    p = r / row['Path']
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes((d / 'source' / row['Path']).read_bytes())
for row in closure:
    assert h((r / row['Path']).read_bytes()) == row['Sha256'], row['Path']
receipt = {'Status':'EXACT_TESTED_CANDIDATE_SOURCE_PROMOTED','Part':333,'OwnedPaths':18,'SourceClosurePaths':2439,'PackageResourceRows':6,'Passed':6871,'Failed':0,'Registry':528,'ProcLibMfas':19,'FullGuardSha256':h((d/'full-guard.json').read_bytes()),'SourceFreezeSha256':h((d/'source-freeze.json').read_bytes()),'RepeatedLocalFull':False,'ValidationMode':'Successful uninterrupted isolated full; exact source promotion; current-head CI follows publication','Files':f['Files']}
p = r / 'artifacts/root-part333-promotion.json'
with p.open('xb') as stream:
    stream.write((json.dumps(receipt, indent=2) + '\n').encode())
print(json.dumps({k:v for k,v in receipt.items() if k!='Files'}))
