from pathlib import Path
import hashlib
import json
import sys

root = Path(sys.argv[1]).resolve()
sha = lambda path: hashlib.sha256(path.read_bytes()).hexdigest().upper()
old = json.loads((root / 'manifest.json').read_text(encoding='utf-8-sig'))
assert old['SourcePin'] == 'ad05823719d77c8faee87348ea39513d4e2f99c5'
assert old['InstalledVersion'] == '29.1.1'
assert len(old['Fixtures']) == 6 and old['OriginalFixturesExecuted'] == 5
names = ['constant', 'add', 'local', 'receive', 'on_load', 'closure']
expected = ['{ok,42,[alpha,beta]}', '42', '{ok,[1,2,3]}', '{ok,ping}', None, '{ok,12,true}']
for fixture, name, result in zip(old['Fixtures'], names, expected):
    assert fixture['Module'] == 'beam_fixture_' + name
    assert fixture['Metadata']['FileOnlyQuery'] and not fixture['Metadata']['ModuleLoadPerformed']
    assert sha(root / fixture['SourceFile']) == fixture['SourceSha256']
    assert sha(root / fixture['BeamFile']) == fixture['BeamSha256']
    execution = fixture['Execution']
    assert execution['Executed'] == (result is not None)
    assert execution['IndependentExpectedExpression'] == result
    if result is not None:
        assert execution['ExitCode'] == 0
        assert execution['ActualEtfSha256'] == execution['ExpectedEtfSha256']
        assert sha(root / execution['ActualEtfFile']) == execution['ActualEtfSha256']
        assert sha(root / execution['ExpectedEtfFile']) == execution['ExpectedEtfSha256']

observer_root = root / 'on-load-observers'
manifest = json.loads((observer_root / 'on-load-observer-manifest.json').read_text(encoding='utf-8-sig'))
assert manifest['SourceCommit'] == old['SourcePin'] and manifest['SourceTag'] == old['SourceTag']
assert manifest['InstalledOtpRoot'] == old['InstalledOtpRoot']
assert manifest['ScenarioCount'] == 4 and len(manifest['Scenarios']) == 4
contracts = [
    ('fresh-success', 'beam_on_load_fresh_success', True, False),
    ('fresh-failure', 'beam_on_load_fresh_failure', False, False),
    ('replacement-success', 'beam_on_load_replace_success', True, True),
    ('replacement-failure', 'beam_on_load_replace_failure', False, True),
]
beam_count = 0
for observed, (name, module, success, has_old) in zip(manifest['Scenarios'], contracts):
    assert observed['Name'] == name and observed['Module'] == module
    assert observed['Succeeds'] == success and observed['HasOldImage'] == has_old
    reason = 'ok' if success else 'not_ok'
    load = '{module,' + module + '}' if success else '{error,on_load_failure}'
    before = '{value,old_image}' if has_old else '{error,undef}'
    during = '{value,old_image}' if has_old else 'not_invoked'
    after = '{value,new_image}' if success else before
    summary = '{observed,' + ','.join([load, reason, before, during, after]) + '}'
    assert observed['IndependentExpectedExpression'] == summary
    assert observed['ExitCode'] == 0 and observed['OriginalObserverExecutionPerformed']
    assert observed['CallbackMonitorDownReasonRetainedRaw']
    assert observed['CallbackMonitorEstablishedBeforeAcknowledgement']
    assert observed['WorkerMonitorDownRetainedRaw']
    assert not observed['FreshPendingExportInvocationPerformed']
    assert observed['ReplacementPendingOldExportInvocationPerformed'] == has_old
    assert observed['ActualSummarySha256'] == observed['ExpectedSummarySha256']
    files = {entry['File']: entry for entry in observed['Files']}
    for name_on_disk, entry in files.items():
        path = (observer_root / name_on_disk).resolve()
        assert path.is_relative_to(observer_root.resolve())
        assert path.stat().st_size == entry['Bytes'] and sha(path) == entry['Sha256']
        beam_count += name_on_disk.endswith('.beam')
    for phase in ['notice', 'callback-down', 'load-result', 'worker-down', 'before', 'during', 'after', 'actual', 'expected']:
        assert name + '/' + phase + '.etf' in files
    assert files[name + '/actual.etf']['Sha256'] == observed['ActualSummarySha256']
    assert files[name + '/expected.etf']['Sha256'] == observed['ExpectedSummarySha256']
    predeclared = json.loads((observer_root / name / 'predeclared-contract.json').read_text(encoding='utf-8-sig'))
    assert predeclared['IndependentExpectedExpression'] == summary and not predeclared['OriginalExecutionPerformed']
    assert sha(observer_root / name / 'new' / (module + '.erl')) == predeclared['NewSourceSha256']
    if has_old:
        assert sha(observer_root / name / 'old' / (module + '.erl')) == predeclared['OldSourceSha256']
assert beam_count == 6
print('PINNED_BEAM_OBSERVER_VERIFIED 4 6; retained compiler fixtures6/native executions5')
