from pathlib import Path
import hashlib
import json
import subprocess

r = Path.cwd()
d = r / 'artifacts/proc-lib332-full-source'
h = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=r).decode().strip() == '6e4019aacb479ee407a91c92b6b7d1b1fcd9bf6d'
guard = read(d / 'full-guard.json')
assert h((d / 'full-guard.json').read_bytes()) == 'FBB9A95750DD49DA34C8FF76B4CE10D2D1EC640C98A32555D9D4667BFDC899E7'
exit_result = read(d / 'full-exit.json')
assert exit_result['ExitCode'] == 1 and not exit_result['TimedOut'] and exit_result['Settled']
suffix_exit = read(d / 'package-suffix-exit.json')
assert suffix_exit['ExitCode'] == 0 and not suffix_exit['TimedOut'] and suffix_exit['Settled']
assert b'NU5019' in (d / 'full-validation.log').read_bytes()
tests = read(d / 'tests.json')
assert tests['Failed'] == 0 and tests['Passed'] == len(tests['Tests']) == 6822
assert all(x['Status'] == 'Passed' for x in tests['Tests'])
names = [x['Name'] for x in tests['Tests']]
previous_names = [x['Name'] for x in read(r / 'docs/validation/part-331/final-main/tests.json')['Tests']]
assert len(names) == len(set(names)) and len(previous_names) == 6775
assert [x for x in names if x in set(previous_names)] == previous_names
expected_integrations = {
    'Readiness': 'Passed', 'PackagedReadiness': 'Passed', 'Build': 'Passed',
    'Tests': 'Passed', 'OracleUnavailableReport': 'Expected failure recorded',
    'HybridOutput': 'Hello World', 'Incremental': 'Passed',
    'DisabledPreprocessing': 'Expected failure', 'CleanRebuild': 'Passed',
    'PackageConsumer': 'Passed', 'LocalTool': 'Passed',
}
assert read(d / 'integration-results.json') == expected_integrations
assert guard['Status'] == 'PASS_ACTUAL_ISOLATED332_COMPOSITE_VALIDATION'
for key, value in {
    'Passed': 6822, 'Failed': 0, 'Registry': 521, 'PreviousRegistry': 515,
    'ErlangMfas': 351, 'ProcLibMfas': 12, 'PreviousOrderedNames': 6775,
    'PreviousOrderedNamesUnchanged': True, 'All515PreviousRegistryRowsRestoredExact': True,
    'SourceClosurePaths': 2431, 'IntegrationResults': expected_integrations,
    'InitialPipelineCompleted': False, 'PackagingResumed': True, 'CompletedTestsRerun': False,
}.items():
    assert guard[key] == value, key
for key, file in {
    'TestReportSha256': 'tests.json', 'FullExitSha256': 'full-exit.json',
    'SourceFreezeSha256': 'source-freeze.json', 'SourceClosureSha256': 'source-resource-closure.json',
    'PackageSuffixExitSha256': 'package-suffix-exit.json',
    'PackageSuffixSourceFreezeSha256': 'package-suffix-source-freeze.json',
    'PackageResourceSupplementSha256': 'packaging-resource-supplement.json',
    'InitialPipelineLogSha256': 'full-validation.log',
    'PackageSuffixLogSha256': 'package-suffix-validation.log',
}.items():
    assert guard[key] == h((d / file).read_bytes()), key
assert guard['SourceFreezeSha256'] == '8CA193FB17ED3418F6FD79127FE3CBB64AAFCC423AE32EB564A1B8E82D417E71'
assert guard['SourceClosureSha256'] == '56908A656DEA730DE49C960EB82B4C5656A46E5B94B0E77E75F98BAA07A12F95'
resources = read(d / 'packaging-resource-supplement.json')['Files']
assert len(resources) == 6 and all(x['Path'] != 'LICENSE.local.txt' for x in resources)
for row in resources:
    data = (r / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']
freeze = read(d / 'source-freeze.json')
rows = freeze['Files']
assert len(rows) == freeze['OwnedPaths'] == 25
old_path = r / 'artifacts/root-part331-main/source-resource-closure.json'
assert h(old_path.read_bytes()) == '9E6119DDB243964E57FB9AC2A35B550B03B4B4495DE54AD6505BE6CB5A76FF34'
old = read(old_path)['Files']
closure = read(d / 'source-resource-closure.json')['Files']
assert len(old) == 2417 and len(closure) == 2431
for row in old:
    data = (r / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']
for row in rows:
    data = (d / 'source' / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']
planned = {x['Path']: x for x in rows}
for row in closure:
    p = d / 'source' / row['Path'] if row['Path'] in planned else r / row['Path']
    data = p.read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']
for row in rows:
    p = r / row['Path']
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_bytes((d / 'source' / row['Path']).read_bytes())
for row in closure:
    data = (r / row['Path']).read_bytes()
    assert len(data) == row['Bytes'] and h(data) == row['Sha256'], row['Path']
receipt = {
    'Status': 'EXACT_TESTED_CANDIDATE_SOURCE_PROMOTED',
    'OwnedPaths': len(rows),
    'SourceClosurePaths': len(closure),
    'FullGuardSha256': h((d / 'full-guard.json').read_bytes()),
    'SourceFreezeSha256': h((d / 'source-freeze.json').read_bytes()),
    'Passed': tests['Passed'],
    'Failed': tests['Failed'],
    'RepeatedLocalFull': False,
    'ValidationMode': 'Initial tests and integration prefix plus exact resumed package suffix',
    'InitialPipelineExitCode': 1,
    'PackageSuffixExitCode': 0,
    'Reason': 'All 2431 tested source/resource bytes match after exact promotion; current-head CI follows publication.',
    'Files': rows,
}
p = r / 'artifacts/root-part332-promotion.json'
with p.open('xb') as stream:
    stream.write((json.dumps(receipt, indent=2) + '\n').encode())
print(json.dumps({k: v for k, v in receipt.items() if k != 'Files'}))
