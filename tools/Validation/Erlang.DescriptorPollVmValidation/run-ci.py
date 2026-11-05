"""One source-admitted Linux collector:11engine+7VM+8OTP-FD+1idle, both profiles.

The production shutdown semantics changed since the52-case observation. This is
a focused54-case check, not a blind rerun or whole API/statistics acceptance.
"""
import argparse
import datetime
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import sys
import time
from types import SimpleNamespace
import xml.etree.ElementTree as ET

TOOL = Path(__file__).resolve().parent
ROOT = TOOL.parents[2]
RUNTIME = '10.0.11'
FOUR = ('Erlang.Runtime.dll', 'Erlang.Terms.dll', 'Erlang.Serialization.dll', 'Erlang.Otp.Stdlib.dll')
ENGINE = ('poll-thread-identity', 'read-write-ready', 'cancel-native-snapshot', 'retire-reuse-fd', 'same-fd-two-owners', 'invalid-fd', 'hangup-eof', 'stop-active-snapshot', 'stop-before-start', 'lost-wake', 'concurrent-dispose-held-poll-entry')
VM = ('eager-two-vms', 'dispose-isolation', 'halt-no-flush', 'initializer-no-host-dispose', 'fail-before-start', 'fail-before-accounting', 'fail-entered-before-ready')
FD = ('stream', 'packet-one', 'packet-two', 'packet-four', 'line', 'list', 'reject-exit-status', 'eof-closes')
IDLE = 'runtime/port-fd-io/cancel-idle-borrowed-reader'


def need(value, reason):
    if not value:
        raise ValueError(reason)


def sha(path):
    result = hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            result.update(block)
    return result.hexdigest().upper()


def save(path, value):
    Path(path).write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')


def load_module(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def event(kind, **fields):
    print(json.dumps({'Event': kind, 'Utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), **fields}), flush=True)


def binaries(root):
    result = []
    need(root.is_dir(), 'Produced binary directory absent')
    for path in sorted(root.rglob('*')):
        need(not path.is_symlink(), 'Unbound binary symlink refused')
        if path.is_file():
            result.append({'Path': path.relative_to(root).as_posix(), 'Bytes': path.stat().st_size, 'Sha256': sha(path)})
    need(result, 'Empty produced binary closure')
    return result


def four(rows):
    by_name = {row['Path']: row for row in rows}
    need(all(name in by_name for name in FOUR), 'Required four actual runtime DLLs absent')
    return [{key: by_name[name][key] for key in ('Path', 'Bytes', 'Sha256')} for name in FOUR]


def require_cases(report, names, kind):
    need(report['Passed'] is True and report['Runtime'] == RUNTIME, 'Actual observer failed or CLR differs')
    need([case['Name'] for case in report['Cases']] == list(names), 'Exact actual observer roster differs')
    need(all(case['Passed'] is True for case in report['Cases']), 'Actual case failed')
    for case in report['Cases']:
        result = case['Result']
        if kind == 'engine':
            need(report['Architecture'] == 'X64', 'Engine architecture differs')
            need(result['Mode'] == case['Name'] and result['PhysicalLifetimeEnded'] is True and result['ActualThreadJoined'] == (case['Name'] != 'stop-before-start'), 'Physical engine ownership not closed')
            if case['Name'] == ENGINE[-1]:
                witness = result['Evidence']
                need(all(witness[key] is True for key in ('ActualPollEntryHeld', 'FirstInvocationClaimedAndBlocked', 'SecondSharedTaskPendingBeforeRelease', 'SameTaskAllThreeDisposals', 'ActualThreadJoined', 'BothOwnedWakeFieldsRetired')), 'Concurrent actual shared disposal/physical ownership proof absent')
        elif case['Name'].startswith('fail-'):
            need(result['ExactSentinelRetained'] is True and result['PhysicalWakeResourcesClosed'] is True and result['ProofAtConstructorThrowBeforeHostDispose'] is True and result['HookCalls'] == 1 and result['ActualPollThreadJoined'] == (case['Name'] != 'fail-before-start'), 'Constructor failure/physical ownership proof absent')
        else:
            need(result['AllOwnedPollersJoined'] is True, 'Owned VM pollers not physically joined')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--expected-head', required=True)
    parser.add_argument('--expected-src-tree', required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--output', default='artifacts/descriptor-poll-validation')
    parser.add_argument('--provider-prefix')
    parser.add_argument('--provider-receipt')
    parser.add_argument('--provider-receipt-sha256')
    args = parser.parse_args()
    need(os.name == 'posix' and os.uname().sysname == 'Linux' and os.uname().machine == 'x86_64', 'Actual Linux x64 required')
    need(sys.dont_write_bytecode, 'Use Python -B')
    need(re.fullmatch('[0-9a-f]{40}', args.expected_head) and re.fullmatch('[0-9a-f]{40}', args.expected_src_tree), 'Explicit actual source head/src tree required')
    out = (ROOT / args.output).resolve() if not Path(args.output).is_absolute() else Path(args.output).resolve()
    need(out.is_relative_to(ROOT / 'artifacts') and not out.exists(), 'Fresh repository artifact output required')
    out.mkdir(parents=True)
    fd_path = ROOT / 'tools/Validation/Erlang.DescriptorPollFdValidation/run-ci.py'
    verify_path = TOOL / 'verify-ci.py'
    source_before = source_after = None
    rows, errors, outcomes, produced, runtime_bindings = [], [], [], [], []
    provider_before, provider_after = {}, None
    try:
        inputs = {str(path): sha(path) for path in (Path(__file__).resolve(), fd_path, verify_path)}
        fd = load_module('fd_existing1576', fd_path)
        fd.ROOT = ROOT
        verify = load_module('whole_source_admission1589', verify_path)
    except Exception as error:
        failure = {'Passed': False, 'ExpectedHead': args.expected_head, 'ExpectedSrcTree': args.expected_src_tree, 'ExpectedCases': 54, 'ObservedCases': 0,
                   'Errors': [{'Stage': 'bootstrap', 'Error': repr(error)}], 'Rows': [], 'Outcomes': [], 'ApiOrFullContractCredits': 0,
                   'Scope': 'Collector/helper/source verifier unavailable; no process/build/native/semantic execution. No stale fallback.'}
        save(out / 'summary.json', failure)
        event('CollectorEnd', Passed=False, ExpectedCases=54, ObservedCases=0, Errors=failure['Errors'])
        return 1
    original_run = fd.run

    def instrumented(out_arg, rows_arg, label, command, seconds=300, cwd=ROOT):
        event('StageStart', Name=label, DeadlineSeconds=seconds)
        result = original_run(out_arg, rows_arg, label, command, seconds, cwd)
        event('StageEnd', Name=label, Passed=fd.clean(result), ExitCode=result['ExitCode'], TimedOut=result['TimedOut'], ForcedCleanup=result['ForcedCleanup'], ProcessGroupAliveAfterNaturalExit=result['ProcessGroupAliveAfterNaturalExit'], ElapsedSeconds=result['ElapsedSeconds'])
        return result

    fd.run = instrumented

    def stage(label, command, seconds=300):
        row = fd.run(out, rows, label, command, seconds, ROOT)
        if not fd.clean(row):
            errors.append({'Stage': label, 'Error': 'Actual stage/outer watchdog/natural process-group cleanup failed', 'Receipt': label + '.exit.json'})
        return row

    def skipped(profile, family, names, reason):
        for name in names:
            outcomes.append({'Profile': profile, 'Family': family, 'Name': name, 'State': 'SkippedDependency', 'Passed': False, 'Reason': reason})
        errors.append({'Stage': profile + '-' + family, 'Error': reason})

    def retain_binary(profile, kind, directory):
        inventory = binaries(directory)
        save(out / (profile + '-' + kind + '-binary-before.json'), inventory)
        produced.append({'Profile': profile, 'Kind': kind, 'Root': str(directory), 'Before': inventory})
        return inventory

    try:
        source_before = verify.verify(args.expected_head, args.expected_src_tree, args.plan_sha256)
        need(source_before['Head'] == args.expected_head and fd.git('rev-parse', 'HEAD:src') == args.expected_src_tree, 'Source head/src tree admission differs')
        save(out / 'source-before.json', source_before)
        dotnet = shutil.which('dotnet')
        need(dotnet is not None, 'Actual dotnet absent')
        dotnet = str(Path(dotnet).resolve(strict=True))
        inputs[dotnet] = sha(dotnet)
        oracle = None
        event('StageStart', Name='provider-prepare-once')
        try:
            provider_args = SimpleNamespace(provider_prefix=args.provider_prefix, provider_receipt=args.provider_receipt, provider_receipt_sha256=args.provider_receipt_sha256, expected_head=args.expected_head)
            oracle = fd.prepare_provider(provider_args, out, rows, provider_before)
            event('StageEnd', Name='provider-prepare-once', Passed=True)
        except Exception as error:
            errors.append({'Stage': 'provider-prepare-once', 'Error': repr(error)})
            event('StageEnd', Name='provider-prepare-once', Passed=False, Error=repr(error))
        for profile in ('Debug', 'Release'):
            fdbin = out / 'build' / profile / 'fd'
            fdproj = fd_path.with_name('Erlang.DescriptorPollFdValidation.csproj')
            built = stage(profile + '-fd-build-once', [dotnet, 'build', fdproj, '-c', profile, '--output', fdbin, '-m:1', '--disable-build-servers', '--nologo'], 900)
            ready = fd.clean(built)
            expected_runtime = None
            if ready:
                try:
                    expected_runtime = four(retain_binary(profile, 'fd', fdbin))
                except Exception as error:
                    ready = False
                    errors.append({'Stage': profile + '-fd-binary-admission', 'Error': repr(error)})
            for kind, folder, names in (('engine', 'Erlang.DescriptorPollIntegratedValidation', ENGINE), ('vm', 'Erlang.DescriptorPollVmValidation', VM)):
                if not ready:
                    skipped(profile, kind, names, 'Shared FD/runtime preparation failed; dependent observer not built or launched')
                    continue
                label = profile + '-' + kind
                binary, intermediate = out / 'build' / profile / kind, out / 'obj' / profile / kind
                project = ROOT / 'tools/Validation' / folder / (folder + '.csproj')
                try:
                    tree = ET.parse(project)
                    need(not tree.findall('.//ProjectReference'), 'Engine/VM must use direct reviewed FD runtime references; no repeated runtime build')
                    row = stage(label + '-build', [dotnet, 'build', project, '-c', profile, '-p:RuntimeReferenceDirectory=' + str(fdbin), '-p:BaseIntermediateOutputPath=' + str(intermediate) + '/', '--output', binary, '-m:1', '--disable-build-servers', '--nologo'], 420)
                    need(fd.clean(row), 'Dependent observer build failed')
                    copied_runtime = four(retain_binary(profile, kind, binary))
                    same_runtime = copied_runtime == expected_runtime
                    runtime_bindings.append({'Profile': profile, 'Kind': kind, 'Matched': same_runtime, 'FdRuntime': expected_runtime, 'ObserverRuntime': copied_runtime})
                    need(same_runtime, 'Copied four runtime DLL hashes/bytes differ from this profile FD build')
                except Exception as error:
                    skipped(profile, kind, names, 'Observer preparation rejected: ' + repr(error))
                    continue
                for name in names:
                    case_label = label + '-' + name
                    report = out / (case_label + '.json')
                    # Mandatory OUTER process-group fence: service synchronous
                    # Dispose may block before the observer can call WaitAsync.
                    row = stage(case_label, [dotnet, 'exec', '--fx-version', RUNTIME, '--roll-forward', 'Disable', binary / 'Erlang.AsyncMicrostateValidation.dll', name, report], 120)
                    try:
                        actual = json.loads(report.read_bytes())
                        require_cases(actual, (name,), kind)
                        need(fd.clean(row), 'Actual observer did not settle naturally')
                        outcomes.append({'Profile': profile, 'Family': kind, 'Name': name, 'State': 'Passed', 'Passed': True, 'Report': report.name})
                    except Exception as error:
                        errors.append({'Stage': case_label, 'Error': repr(error)})
                        outcomes.append({'Profile': profile, 'Family': kind, 'Name': name, 'State': 'Failed', 'Passed': False, 'Report': report.name, 'Error': repr(error)})
            if not ready:
                skipped(profile, 'fd', FD, 'FD build/runtime closure not admitted')
                skipped(profile, 'idle', (IDLE,), 'FD build/runtime closure not admitted')
                continue
            for mode in ('compare', 'cancel'):
                label = profile + '-fd-' + mode
                names = FD if mode == 'compare' else (IDLE,)
                family = 'fd' if mode == 'compare' else 'idle'
                if mode == 'compare' and oracle is None:
                    skipped(profile, family, names, 'Original provider failed; independent engine/VM/idle retained')
                    continue
                report = out / (label + '.json')
                row = stage(label, [dotnet, 'exec', '--fx-version', RUNTIME, '--roll-forward', 'Disable', fdbin / 'Erlang.DescriptorPollFdValidation.dll', mode, oracle or '-', report], 300)
                try:
                    actual = json.loads(report.read_bytes())
                    need(fd.clean(row) and actual['Passed'] is True and actual['Mode'] == mode and actual['Runtime'] == RUNTIME and actual['Architecture'] == 'X64', 'Actual FD/idle outcome, runtime or physical cleanup rejected')
                    if mode == 'compare':
                        detail_path = Path(actual['DetailsPath']).resolve(strict=True)
                        need(detail_path.is_relative_to(out), 'FD detail output escapes retained artifact root')
                        detail = json.loads(detail_path.read_bytes())
                        need(actual['OriginalComparisonCount'] == 8 and detail['Passed'] == 8 and detail['Failed'] == 0 and [case['Name'] for case in detail['Results']] == list(FD), 'Original eight-case roster/count differs')
                        need(all(case['Passed'] is True and case['ExitCode'] == 0 and case['ActualOutputHex'] == case['OutputHex'] for case in detail['Results']), 'Actual complete original FD bytes differ')
                    else:
                        need(actual['ExistingCancellationCase'] == IDLE, 'Existing idle-borrowed-reader case identity differs')
                    outcomes.extend({'Profile': profile, 'Family': family, 'Name': name, 'State': 'Passed', 'Passed': True, 'Report': report.name} for name in names)
                except Exception as error:
                    errors.append({'Stage': label, 'Error': repr(error)})
                    outcomes.extend({'Profile': profile, 'Family': family, 'Name': name, 'State': 'Failed', 'Passed': False, 'Report': report.name, 'Error': repr(error)} for name in names)
    except Exception as error:
        errors.append({'Stage': 'collector', 'Error': repr(error)})
    finally:
        for binary in produced:
            try:
                after = binaries(Path(binary['Root']))
                save(out / (binary['Profile'] + '-' + binary['Kind'] + '-binary-after.json'), after)
                need(after == binary['Before'], 'Produced binary closure changed during execution')
                binary['Stable'] = True
            except Exception as error:
                binary['Stable'] = False
                errors.append({'Stage': 'binary-final', 'Profile': binary['Profile'], 'Kind': binary['Kind'], 'Error': repr(error)})
        if provider_before:
            try:
                receipt_path = Path(provider_before['ReceiptPath'])
                need(sha(receipt_path) == provider_before['ReceiptSha256'], 'Exact admitted original provider receipt changed')
                receipt = json.loads(receipt_path.read_bytes())
                fd.admit_provider(Path(provider_before['Prefix']), receipt)
                provider_after = dict(provider_before)
                need(provider_after['SourceCommit'] == receipt['SourceCommit'] == fd.PIN, 'Provider pin changed')
            except Exception as error:
                provider_after = {'Failed': repr(error)}
                errors.append({'Stage': 'provider-final', 'Error': repr(error)})
        for path, expected in inputs.items():
            try:
                need(sha(path) == expected, 'Actual collector/helper/executable input changed')
            except Exception as error:
                errors.append({'Stage': 'execution-input-final', 'Path': path, 'Error': repr(error)})
        try:
            source_after = verify.verify(args.expected_head, args.expected_src_tree, args.plan_sha256)
            need(source_after['Head'] == args.expected_head and fd.git('rev-parse', 'HEAD:src') == args.expected_src_tree and source_before == source_after, 'Final whole source admission differs')
            save(out / 'source-after.json', source_after)
        except Exception as error:
            errors.append({'Stage': 'source-final', 'Error': repr(error)})
        roster = [(profile, family, name) for profile in ('Debug', 'Release') for family, names in (('engine', ENGINE), ('vm', VM), ('fd', FD), ('idle', (IDLE,))) for name in names]
        actual_roster = [(row['Profile'], row['Family'], row['Name']) for row in outcomes]
        if actual_roster != roster or len(set(actual_roster)) != 54:
            errors.append({'Stage': 'final-roster', 'Error': 'Exact independent54 outcomes missing, duplicated or reordered'})
        passed = not errors and len(outcomes) == 54 and all(row['Passed'] is True for row in outcomes) and bool(provider_before) and provider_before == provider_after and len(produced) == 6 and all(row.get('Stable') is True for row in produced)
        summary = {'Passed': passed, 'ExpectedHead': args.expected_head, 'ExpectedSrcTree': args.expected_src_tree, 'SourceBefore': source_before, 'SourceAfter': source_after,
                   'ExpectedCases': 54, 'ObservedCases': len(outcomes), 'PassedCases': sum(row['Passed'] is True for row in outcomes), 'Outcomes': outcomes, 'Rows': rows, 'Errors': errors,
                   'ProviderBefore': provider_before or None, 'ProviderAfter': provider_after, 'ProviderStable': bool(provider_before) and provider_before == provider_after,
                   'ProducedBinaryClosures': produced, 'ExecutionInputs': inputs, 'RuntimeBindings': runtime_bindings,
                   'SameFourRuntimeDllsPerProfile': len(runtime_bindings) == 4 and all(row['Matched'] is True for row in runtime_bindings) and len(produced) == 6 and all(row.get('Stable') is True for row in produced),
                   'Scope': 'Changed production service/VM shutdown:11 engine+7 VM+8 original FD whole-byte outcomes+1 existing idle reader per Debug/Release. Original provider prepared once; FD helper/runtime built once per profile, engine/VM direct-reference identical DLLs. No API/full-contract/statistics/all-cancellation/external-FD-reuse or performance credits.',
                   'ProductionChangedSince52CaseObservation': True, 'ApiOrFullContractCredits': 0}
        save(out / 'summary.json', summary)
        event('CollectorEnd', Passed=passed, ExpectedCases=54, ObservedCases=len(outcomes), PassedCases=summary['PassedCases'], Errors=errors)
    return 0 if summary['Passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
