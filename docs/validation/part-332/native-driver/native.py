"""Three bounded trusted native captures; passive review is a separate mode."""
from pathlib import Path
import argparse
import hashlib
import importlib.util
import json
import os
import subprocess
import sys
import time

sys.dont_write_bytecode = True
sha = lambda data: hashlib.sha256(data).hexdigest().upper()
read = lambda path: json.loads(path.read_bytes())
parser = argparse.ArgumentParser()
parser.add_argument('mode', choices=['run', 'review'])
parser.add_argument('--full-guard-sha256', required=True)
parser.add_argument('--tool-sha256', required=True)
parser.add_argument('--runtime-sha256', required=True)
parser.add_argument('--output', required=True, type=Path)
args = parser.parse_args()
root = Path.cwd().resolve()
config_path = Path(__file__).with_name('configuration.json')
config = read(config_path)
candidate = root / config['Candidate']
proof = root / config['Proof']
output = args.output.resolve()
assert output.is_relative_to(root / 'artifacts') and output not in (candidate, proof)
assert (root / '.git').exists()
freeze_path = proof / 'source-freeze.json'
closure_path = proof / 'source-resource-closure.json'
full_path = proof / 'full-guard.json'
exit_path = proof / 'full-exit.json'
peer_path = root / config['CompositionPeer']['Path']
tool = candidate / config['Tool']
runtime = tool.with_name('Erlang.Runtime.dll')

def binding(path):
    data = path.read_bytes()
    return {'Path': path.relative_to(root).as_posix(), 'Bytes': len(data), 'Sha256': sha(data)}

def authorities():
    assert sha(freeze_path.read_bytes()) == config['SourceFreezeSha256']
    assert sha(closure_path.read_bytes()) == config['SourceClosureSha256']
    assert sha(peer_path.read_bytes()) == config['CompositionPeer']['Sha256']
    peer = read(peer_path)
    assert peer['Passed'] is True and peer['Role'] == 'FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW'
    assert peer['SourceFreezeSha256'] == config['SourceFreezeSha256']
    assert peer['SourceClosureSha256'] == config['SourceClosureSha256']
    freeze = read(freeze_path)
    assert freeze['OwnedPaths'] == len(freeze['Files']) == config['OwnedPaths']
    assert freeze['OwnedCSharpFiles'] == config['OwnedCSharpFiles']
    closure = read(closure_path)['Files']
    assert len(closure) == freeze['SourceClosurePaths'] == config['ClosureRows']
    for row in closure:
        raw = (candidate / row['Path']).read_bytes()
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
    assert sha(full_path.read_bytes()) == args.full_guard_sha256.upper() == config['CompositeGuardSha256']
    full = read(full_path)
    for key, value in config['ExpectedFull'].items():
        assert full[key] == value, key
    assert full['SourceFreezeSha256'] == config['SourceFreezeSha256']
    assert full['SourceClosureSha256'] == config['SourceClosureSha256']
    assert full['SourceClosurePaths'] == len(closure)
    assert full['TestReportSha256'] == sha((proof / 'tests.json').read_bytes())
    assert full['FullExitSha256'] == sha(exit_path.read_bytes())
    completed = read(exit_path)
    assert completed['ExitCode'] == 1 and completed['TimedOut'] is False and completed['Settled'] is True
    for item in config['CompositeProofs']:
        raw = (proof / item['File']).read_bytes()
        assert sha(raw) == item['Sha256'] == full[item['Field']]
    suffix = read(proof / 'package-suffix-exit.json')
    assert suffix['ExitCode'] == 0 and suffix['TimedOut'] is False and suffix['Settled'] is True
    assert full['TestReportSha256'] == config['InitialTestReportSha256']
    suffix_freeze = read(proof / 'package-suffix-source-freeze.json')
    for item in suffix_freeze['Files']:
        raw = (proof / item['Path']).read_bytes()
        assert len(raw) == item['Bytes'] and sha(raw) == item['Sha256']
    supplemental = read(proof / 'packaging-resource-supplement.json')['Files']
    assert len(supplemental) == config['SupplementalResources']
    for item in supplemental:
        raw = (candidate / item['Path']).read_bytes()
        assert len(raw) == item['Bytes'] and sha(raw) == item['Sha256']
    assert sha(tool.read_bytes()) == args.tool_sha256.upper()
    assert sha(runtime.read_bytes()) == args.runtime_sha256.upper()
    assert runtime.read_bytes() == (candidate / 'src/Erlang.Runtime/bin/Debug/net10.0/Erlang.Runtime.dll').read_bytes()
    for row in config['References']:
        raw = (root / row['Path']).read_bytes()
        assert len(raw) == row['Bytes'] and sha(raw) == row['Sha256'], row['Path']
    return closure

rows = authorities()
pins = [binding(path) for path in sorted(tool.parent.rglob('*')) if path.is_file() and path.suffix in {'.dll', '.json'}]
assert any(row['Path'] == tool.relative_to(root).as_posix() for row in pins)
assert any(row['Path'] == runtime.relative_to(root).as_posix() for row in pins)
pins += [binding(path) for path in [config_path, Path(__file__).resolve(), freeze_path, closure_path, full_path, exit_path, peer_path]]
pins += config['References']
pins += [binding(proof / item['File']) for item in config['CompositeProofs']]
assert len({row['Path'] for row in pins}) == len(pins)

def pinned():
    authorities()
    for row in pins:
        assert binding(root / row['Path']) == row, row['Path']

def command(role):
    return ['dotnet', str(tool), *[str(root / value['Path']) if 'Path' in value else value['Literal'] for value in role['Arguments']]]

if args.mode == 'run':
    assert os.name == 'nt' and not output.exists()
    output.mkdir()
    env = os.environ.copy()
    env['DOTNET_PROCESSOR_COUNT'] = '2'
    for key in ['DOTNET_STARTUP_HOOKS', 'DOTNET_ADDITIONAL_DEPS', 'DOTNET_SHARED_STORE']:
        env.pop(key, None)
    for role in config['Roles']:
        pinned()
        name = role['Name']
        attempt = {'Role': name, 'Command': command(role), 'StartedUnixSeconds': time.time(), 'Bindings': pins, 'CandidateExecution': True, 'OriginalExecuted': False, 'AutomaticRetryAllowed': False}
        with (output / (name + '.attempt.json')).open('x', encoding='utf-8') as stream:
            json.dump(attempt, stream, indent=2)
            stream.write('\n')
        start = time.monotonic()
        try:
            process = subprocess.run(command(role), cwd=candidate, env=env, capture_output=True, timeout=config['TimeoutSeconds'])
            stdout, stderr, code, timed_out = process.stdout, process.stderr, process.returncode, False
        except subprocess.TimeoutExpired as failure:
            stdout, stderr, code, timed_out = failure.stdout or b'', failure.stderr or b'', None, True
        (output / (name + '.stdout')).write_bytes(stdout)
        (output / (name + '.stderr')).write_bytes(stderr)
        receipt = {'Role': name, 'ExitCode': code, 'TimedOut': timed_out, 'Settled': True, 'ElapsedSeconds': time.monotonic() - start, 'StdoutSha256': sha(stdout), 'StderrSha256': sha(stderr), 'CandidateExecution': True, 'NativeProbeInvoked': True, 'NativeExecutionValidated': False, 'OriginalExecuted': False}
        (output / (name + '.process.json')).write_text(json.dumps(receipt, indent=2) + '\n')
        pinned()
        print(json.dumps(receipt), flush=True)
        assert code == 0 and not timed_out and not stderr, 'Preserve first capture; no retry'
else:
    assert output.is_dir()
    captures = []
    for role in config['Roles']:
        name = role['Name']
        attempt = read(output / (name + '.attempt.json'))
        process = read(output / (name + '.process.json'))
        stdout = (output / (name + '.stdout')).read_bytes()
        stderr = (output / (name + '.stderr')).read_bytes()
        assert attempt['Role'] == process['Role'] == name and attempt['Command'] == command(role)
        assert attempt['Bindings'] == pins and attempt['AutomaticRetryAllowed'] is False
        assert attempt['CandidateExecution'] is True and attempt['OriginalExecuted'] is False
        assert type(process['ExitCode']) is int and process['ExitCode'] == 0 and process['TimedOut'] is False and process['Settled'] is True
        assert process['CandidateExecution'] is True and process['NativeProbeInvoked'] is True and process['NativeExecutionValidated'] is False and process['OriginalExecuted'] is False
        assert process['StdoutSha256'] == sha(stdout) and process['StderrSha256'] == sha(stderr) and not stderr
        assert 0 <= process['ElapsedSeconds'] < config['TimeoutSeconds']
        captures.append({'Role': name, 'Attempt': binding(output / (name + '.attempt.json')), 'Process': binding(output / (name + '.process.json')), 'Stdout': binding(output / (name + '.stdout')), 'Stderr': binding(output / (name + '.stderr'))})
    dependency_assets = []
    for target in read(tool.with_suffix('.deps.json'))['targets'].values():
        for library in target.values():
            for kind in ['runtime', 'resources', 'native']:
                for name in library.get(kind, {}):
                    asset = tool.parent / name
                    assert binding(asset) in pins
                    dependency_assets.append(name)
    assert dependency_assets
    for role in ['legacy', 'reallocation']:
        assert (output / (role + '.stdout')).read_bytes() == (root / config['RawBaseline'] / (role + '.stdout')).read_bytes()
    reader = root / config['PassiveReader']
    spec = importlib.util.spec_from_file_location('reviewed_passive315', reader)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    legacy = module.legacy_reader(candidate / config['LegacyReader'])
    fixture_source = candidate / config['FixtureSource']
    assert sha(fixture_source.read_bytes()) == module.SOURCE_SHA
    assert module.SOURCE_SHA in (candidate / 'src/Erlang.Runtime/NifNativeNames.cs').read_text(encoding='utf-8-sig')
    typed = module.parse_capture((output / 'provider-query.stdout').read_bytes(), (output / 'provider-query.stderr').read_bytes(), legacy, module.SOURCE_SHA, config['FixtureSha256'], None)
    baseline = read(root / config['TypedBaseline'])
    for key, value in typed.items():
        assert baseline[key] == value, key
    assert typed['LoadedRequestedBytes'] == typed['LoadedBackingRequestBytes'] > 0
    pinned()
    report = {'Status': 'PASS_ACTUAL_ISOLATED332_POST_FULL_WINDOWS_NATIVE_THREE_ROLES', 'SourceFreezeSha256': config['SourceFreezeSha256'], 'SourceClosureSha256': config['SourceClosureSha256'], 'ActualFullGuard': binding(full_path), 'SourceRowsRehashed': len(rows), 'CompositionPeer': binding(peer_path), 'ActualTool': binding(tool), 'ActualRuntime': binding(runtime), 'Roles': captures, 'FullPassed': config['ExpectedFull']['Passed'], 'IntegrationResults': config['ExpectedFull']['IntegrationResults'], 'RuntimeDependencyAssetsVerified': len(dependency_assets), 'RecursiveDllJsonPins': sum(row['Path'].startswith(tool.parent.relative_to(root).as_posix() + '/') for row in pins), 'IndependentTypedReplayMatchesBaseline': True, 'LegacyAndReallocationRawBytesMatchPreviousActual317': True, 'QueryPhases': typed['Completion']['Phases'], 'RuntimeOrdinals': [query['RuntimeOrdinal'] for query in typed['QueryObservations']], 'JoinedOwners': [query['JoinedOwners'] for query in typed['QueryObservations']], 'RequestedAndBackingPairs': [query['QueryResult'] for query in typed['QueryObservations']], 'Scope': typed['Scope'], 'CandidateExecution': True, 'MainExecution': False, 'OriginalExecuted': False, 'InitialPipelineCompleted': False, 'PackagingResumed': True, 'CompletedTestsRerun': False, 'InitialPipelineExit': binding(exit_path), 'PackageSuffixExit': binding(proof / 'package-suffix-exit.json'), 'PackageResourceSupplement': binding(proof / 'packaging-resource-supplement.json'), 'NewCCompiled': False, 'PhysicalMemoryCompared': False}
    with (output / 'guard.json').open('x', encoding='utf-8') as stream:
        json.dump(report, stream, indent=2)
        stream.write('\n')
    print(json.dumps({'GuardSha256': sha((output / 'guard.json').read_bytes()), 'Status': report['Status']}))
