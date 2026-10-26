import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
PLAN_SHA = '36F3A580DAAB83675014C835F5537861C894440D761479B839384B722EDD5319'
POLICY_SHA = '5FBDB2F091A0A1D3F55C6673B7A6B140D23A1CA2823BDE3F6865AAC8028ED957'
BASE_SHA = '46F8837A41981C15DD6D49E3F16CFE06ABB2A8DE2B404BBCF84D9B93A248150F'
DECODER_SHA = 'A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
MODULE = 'raw_onload_visibility_fixture'
MAX_REPORT_BYTES = 32 * 1024 * 1024
MAX_TOTAL_OBSERVATION_BYTES = 32 * 1024 * 1024
EXPECTED_EVENTS = {'direct_true': ['keep_true'], 'false_false_true': ['false_first', 'false_second', 'keep_true'], 'wrapper_false': ['wrapper_false']}
FIELDS = {'ModuleLoaded': 'module_loaded', 'OldCode': 'old_code', 'BodyExported': 'body_exported', 'CaptureExported': 'capture_exported', 'BodyCall': 'body', 'PendingFunCall': 'pending_fun', 'OldFunCall': 'old_fun'}


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def module(path, name, expected):
    require(sha(path) == expected, 'Bound helper hash drift: ' + name)
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    sys.dont_write_bytecode = True
    spec.loader.exec_module(result)
    return result


def atom(value):
    return ('atom', value)


def seq(value, size=None, kind='tuple'):
    require(type(value) is tuple and len(value) == 2 and value[0] == kind, 'Typed sequence shape')
    require(size is None or len(value[1]) == size, 'Typed sequence size')
    return value[1]


def pair(value, key):
    fields = seq(value, 2)
    require(fields[0] == atom(key), 'Expected observation key: ' + key)
    return fields[1]


def declare(value):
    if type(value) is bool:
        return atom(str(value).lower())
    require(type(value) is dict and len(value) == 1, 'Declared term shape')
    if 'Atom' in value:
        return atom(value['Atom'])
    if 'Integer' in value:
        return ('integer', value['Integer'])
    key = 'Tuple' if 'Tuple' in value else 'List'
    return (key.lower(), tuple(declare(v) for v in value[key]))


def captured(value):
    fields = seq(value)
    require(fields[0] in (atom('return'), atom('exception')), 'Capture tag')
    if fields[0] == atom('return'):
        require(len(fields) == 2, 'Return capture shape')
    else:
        require(len(fields) == 4 and fields[1] in (atom('error'), atom('exit'), atom('throw')), 'Exception capture shape/class')
        for frame in seq(fields[3], kind='list'):
            row = seq(frame)
            if len(row) == 3 and row[0][0] in ('new_fun', 'export_fun'):
                require(row[1][0] == 'list' or row[1][0] == 'integer' and 0 <= row[1][1] <= 255, 'Fun stack arity/arguments')
                seq(row[2], kind='list')
                continue
            require(len(row) in (3, 4) and row[0][0] == 'atom' and row[1][0] == 'atom', 'Stack frame module/function')
            require(row[2][0] == 'list' or row[2][0] == 'integer' and row[2][1] >= 0, 'Stack arity/arguments')
            if len(row) == 4:
                seq(row[3], kind='list')
    return fields


def snapshot(value, name):
    fields = seq(value, 10)
    require(fields[:2] == (atom('snapshot'), atom(name)), 'Snapshot phase')
    keys = ['module_loaded', 'old_code', 'body_exported', 'capture_exported', 'exports', 'body', 'pending_fun', 'old_fun']
    values = {key: pair(field, key) for key, field in zip(keys, fields[2:])}
    require(all(values[key] in (atom('true'), atom('false')) for key in keys[:4]), 'Boolean visibility fields')
    exports = captured(values['exports'])
    if exports[0] == atom('return'):
        for item in seq(exports[1], kind='list'):
            function, arity = seq(item, 2)
            require(function[0] == 'atom' and arity[0] == 'integer' and arity[1] >= 0, 'Export tuple types')
    captured(values['body'])
    for key in ('pending_fun', 'old_fun'):
        held = values[key]
        if held == atom('no_holder'):
            continue
        shape = seq(held)
        if shape[0] == atom('holder_down'):
            require(len(shape) == 2, 'Holder DOWN shape')
        else:
            captured(held)
    return values


def projections(actual, declared):
    return {key: actual[FIELDS[key]] == declare(wanted) for key, wanted in declared.items()}


def passive_json(value):
    if type(value) is bytes:
        return {'BytesHex': value.hex()}
    if isinstance(value, (tuple, list)):
        return [passive_json(item) for item in value]
    if isinstance(value, frozenset):
        return {'UnorderedMapEntries': [passive_json(item) for item in sorted(value, key=repr)]}
    return value


def run(args, report):
    root, proposal = args.observations.resolve(), args.proposal.resolve()
    base = module(args.base_guard, 'bound_original13_helpers', BASE_SHA)
    decoder = module(args.decoder, 'bound_passive_etf', DECODER_SHA)
    require(sha(proposal / 'source-plan.json') == PLAN_SHA and sha(proposal / 'expected-policy.json') == POLICY_SHA, 'Frozen plan/policy drift')
    plan, policy, receipt = load(proposal / 'source-plan.json'), load(proposal / 'expected-policy.json'), load(root / 'receipt.json')
    cases = plan['Cases']
    require(plan['PinnedCommit'] == PIN and len(cases) == 9 and len({row['Name'] for row in cases}) == 9, 'Pin/distinct nine cases')
    require([(x['Baseline'], x['Operation']) for x in cases] == [(baseline, operation) for baseline in ('fresh', 'current', 'deleted_old') for operation in EXPECTED_EVENTS], 'Independent nine combinations/order')
    require([row['Name'] for row in cases] == [row['Name'] for row in policy['Cases']], 'Policy order')
    require(receipt['Complete'] is True and receipt['SemanticAssertionsExecuted'] is False and receipt['SdkCompared'] is False and receipt['SdkExecuted'] is False and receipt['NativeFixtureUsed'] is False, 'Raw-only collection scope')
    require(receipt['PinnedCommit'] == PIN and receipt['SourcePlanSha256'] == PLAN_SHA and receipt['ExpectedFreshVmCases'] == 9 and receipt['OrderedFreshVmCases'] == [row['Name'] for row in cases], 'Receipt case/pin/plan drift')
    require(all(receipt.get(key, []) == [] for key in ('Exclusions', 'SkippedCases', 'ExcludedCases')), 'No exclusions allowed')
    members = base.verify_members(root, receipt['Files'])
    require({p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()} == members | {'receipt.json'}, 'Exact raw file closure')
    require(sha(root / 'source-plan.json') == PLAN_SHA, 'Copied plan drift')
    for row in plan['Sources']:
        actual = base.child(root, 'sources/' + row['Path'])
        reviewed = base.child(proposal, row['Path'])
        require(sha(actual) == sha(reviewed) == row['Sha256'] and actual.stat().st_size == row['Bytes'], 'Reviewed input bytes drift')
    provenance = load(args.build_provenance)
    require(provenance['SourceHead'] == PIN and provenance['OriginalCasesExecuted'] is False and provenance['NativeFixtureUsed'] is False, 'Build provenance scope/pin')
    require(provenance['HarnessCommit'] == args.expected_head and str(provenance['RunId']) == args.expected_run and str(provenance['RunAttempt']) == args.expected_attempt and provenance['Job'] == args.expected_job, 'Explicit expected CI build identity')
    expected_paths = ['observer-install/bin/erl', 'observer-install/bin/erlc', 'observer-install/lib/erlang/erts-17.1/bin/beam.smp', 'observer-install/lib/erlang/erts-17.1/bin/erlexec']
    require(sorted(row['Path'] for row in provenance['Files']) == sorted(expected_paths), 'All four source-built tools')
    require(len(receipt['Tools']) == 2, 'Collector two tool hashes')
    for tool, rel in zip(receipt['Tools'], expected_paths[:2]):
        original = next(row for row in provenance['Files'] if row['Path'] == rel)
        require(tool['Path'].endswith('/' + rel) and tool['Sha256'] == original['Sha256'], 'Tool bound to source build')
    erl = receipt['Tools'][0]['Path']
    for prefix in ('compile-old', 'compile-new', 'compile-observer', 'metadata', 'disassembly'):
        base.process(root, prefix, erl if prefix in ('metadata', 'disassembly') else None)
        if prefix.startswith('compile-'):
            compiler = load(base.child(root, prefix + '.process.json'))['Arguments']
            require(compiler[0] == receipt['Tools'][1]['Path'] and '-Werror' in compiler and '+debug_info' in compiler, 'Actual source compiler invocation')
    metadata = seq(decoder.decode_exact(base.child(root, 'metadata.etf').read_bytes()), 2)
    require(metadata[0] == atom('raw_onload_visibility_metadata'), 'Metadata tag')
    rows = seq(metadata[1], 7, 'list')
    keys = ['otp_release', 'erts_version', 'system_version', 'system_architecture', 'wordsize', 'build_type', 'emulator_type']
    meta = {key: pair(value, key) for key, value in zip(keys, rows)}
    require(base.character_list(meta['otp_release']) == '29' and base.character_list(meta['erts_version']) == '17.1' and meta['wordsize'] == ('integer', 8) and base.character_list(meta['system_architecture']) == 'x86_64-pc-linux-gnu', 'Original platform/release metadata')
    require(meta['build_type'][0] == 'atom' and meta['emulator_type'][0] == 'atom', 'Build/emulator metadata types')
    pids, results = [], []
    total_observation_bytes = 0
    for case, authored in zip(cases, policy['Cases']):
        name, baseline, operation = case['Name'], case['Baseline'], case['Operation']
        require(authored['Baseline'] == baseline and authored['Operation'] == operation, 'Case policy baseline/operation')
        pids.append(base.process(root, name + '/observation', erl))
        process = load(base.child(root, name + '/observation.process.json'))
        evaluation = process['Arguments'][-1]
        require('raw_onload_visibility_observer:run(' + baseline + ',' + operation + ')' in evaluation, 'Actual case invocation')
        require(process['Arguments'][process['Arguments'].index('-pa') + 1].endswith('/observer-ebin') and not any('/old-ebin' in arg or '/new-ebin' in arg for arg in process['Arguments']), 'Fixture directories must stay off path')
        raw_path = base.child(root, name + '/result.etf')
        total_observation_bytes += raw_path.stat().st_size
        require(total_observation_bytes <= MAX_TOTAL_OBSERVATION_BYTES, 'Aggregate observation byte budget')
        value = decoder.decode_exact(raw_path.read_bytes())
        fields = seq(value, 9)
        require(fields[:3] == (atom('raw_onload_visibility_observation'), atom(baseline), atom(operation)), 'Observation typed envelope')
        before = snapshot(pair(fields[3], 'before_stage'), 'before_stage')
        stage = pair(fields[4], 'stage_result')
        snapshot(pair(fields[5], 'staged'), 'staged')
        callback = pair(fields[6], 'callback')
        captured(callback)
        after = snapshot(pair(fields[7], 'after_callback'), 'after_callback')
        events = seq(pair(fields[8], 'events'), len(EXPECTED_EVENTS[operation]), 'list')
        returns, final = [], None
        for event, event_name in zip(events, EXPECTED_EVENTS[operation]):
            row = seq(event, 4)
            require(row[:2] == (atom('event'), atom(event_name)), 'Finalization event order')
            returned = captured(pair(row[2], 'result'))
            returns.append(returned[1] if returned[0] == atom('return') else ('captured_exception', returned))
            final = snapshot(pair(row[3], 'snapshot'), event_name)
        certain = authored['ExpectedSourceDerivedCertain']
        checks = {'Callback': callback == declare(certain['Callback']), 'StageResult': stage == declare(certain['StageResult']), 'OperationReturns': returns == [declare(x) for x in certain['OperationReturns']]}
        for phase, actual in [('BeforeStage', before), ('AfterCallback', after), ('Final', final)]:
            checks.update({phase + '.' + key: passed for key, passed in projections(actual, certain[phase]).items()})
        hypothesis = authored['SourceDerivedHypothesisNotYetObserved']
        hypothesis_results = {}
        if hypothesis:
            for key, wanted in hypothesis.items():
                actual = final[FIELDS[key]]
                if isinstance(wanted, dict) and 'ExceptionClassAtom' in wanted:
                    captured_value = captured(actual)
                    passed = len(captured_value) == 4 and captured_value[:3] == (atom('exception'), atom(wanted['ExceptionClassAtom']), atom(wanted['ReasonAtom']))
                else:
                    passed = actual == declare(wanted)
                hypothesis_results[key] = 'MATCH' if passed else 'CONTRADICTED'
        results.append({'Name': name, 'CertainAssertions': checks, 'CertainPassed': all(checks.values()), 'Hypothesis': hypothesis_results, 'InvestigativeDimensions': authored['InvestigativeDimensions'], 'RawEtfSha256': sha(raw_path), 'FullPassiveTypedObservation': passive_json(value), 'RawStackAndFunIdentityRetained': True, 'FunRawSpansReference': str(raw_path)})
    require(len(set(pids)) == 9, 'Nine distinct fresh VM process identities')
    # Controls remain a separate synthetic capture gate, never original semantics.
    controls = load(args.controls / 'controls.json')
    require(controls['RawByteExitControlPassed'] is True and controls['OwnedInheritedPipeControlPassed'] is True and controls['OriginalEmulatorExecuted'] is False and controls['NativeFixtureUsed'] is False, 'Capture control scope/status')
    exit_control = load(args.controls / 'exit-three.process.json')
    inherited = load(args.controls / 'inherited-pipes.process.json')
    require(exit_control['ExitCode'] == 3 and exit_control['TimedOut'] is False and exit_control['PipesClosed'] is True and exit_control['ParentReaped'] is True, 'Exit control completion')
    require((args.controls / 'exit-three.stdout').read_bytes() == bytes([0, 255]) + b'raw' and (args.controls / 'exit-three.stderr').read_bytes() == bytes([254]) + b'err', 'Capture bytes')
    require(inherited['ExitCode'] == 0 and inherited['TimedOut'] is True and inherited['PipesClosed'] is True and inherited['ParentReaped'] is True and (args.controls / 'inherited-pipes.stdout').read_bytes() == b'owned-child-pipes', 'Owned timeout control completion')
    control_files = [{'Path': str(path.relative_to(args.controls)), 'Bytes': path.stat().st_size, 'Sha256': sha(path)} for path in sorted(args.controls.rglob('*')) if path.is_file()]
    require(type(exit_control['OwnedPid']) is int and exit_control['OwnedPid'] > 0 and type(inherited['OwnedPid']) is int and inherited['OwnedPid'] > 0 and inherited['OwnedPid'] != exit_control['OwnedPid'], 'Distinct owned control PIDs')
    certain_ok = all(row['CertainPassed'] for row in results)
    contradicted = any('CONTRADICTED' in row['Hypothesis'].values() for row in results)
    report.update({'Status': 'ORIGINAL_NINE_CERTAIN_MISMATCH' if not certain_ok else 'ORIGINAL_NINE_CERTAIN_PASS_HYPOTHESIS_CONTRADICTED' if contradicted else 'ORIGINAL_NINE_CERTAIN_PASS_HYPOTHESIS_MATCHED', 'CertainAssertionsPassed': certain_ok, 'HypothesisContradicted': contradicted, 'Cases': results, 'ReceiptFilesVerified': len(members), 'PinnedCommit': PIN, 'PlanSha256': PLAN_SHA, 'PolicySha256': POLICY_SHA, 'RawReceiptSha256': sha(root / 'receipt.json'), 'BuildProvenanceSha256': sha(args.build_provenance), 'ExplicitExpectedCiIdentity': {'Head': args.expected_head, 'Run': args.expected_run, 'Attempt': args.expected_attempt, 'Job': args.expected_job}, 'CiIdentityNetworkAuthenticated': False, 'CaptureControlsPassed': True, 'CaptureControlFiles': control_files})
    return 0 if certain_ok else 1



def write_report(path, report, limit=MAX_REPORT_BYTES):
    partial = Path(str(path) + '.partial')
    require(not partial.exists(), 'Refuse prior partial report')
    written = 0
    with partial.open('xb') as output:
        for text in json.JSONEncoder(indent=2).iterencode(report):
            data = text.encode('utf-8')
            require(len(data) <= limit - written, 'Aggregate serialized report byte budget')
            output.write(data)
            written += len(data)
        require(written < limit, 'Report final newline budget')
        output.write(b'\n')
    partial.replace(path)


def main():
    parser = argparse.ArgumentParser()
    for name in ('observations', 'proposal', 'build-provenance', 'base-guard', 'decoder', 'controls', 'report'):
        parser.add_argument('--' + name, type=Path, required=True)
    for name in ('expected-head', 'expected-run', 'expected-attempt', 'expected-job'):
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    require(not args.report.exists() and not args.report.resolve().is_relative_to(args.observations.resolve()) and not args.report.resolve().is_relative_to(args.controls.resolve()), 'Fresh report outside raw/control evidence required')
    report = {'Status': 'ORIGINAL_NINE_GUARD_REFUSED_OR_FAILED', 'SdkCompared': False, 'SdkPass': False, 'GenericRuntimeParity': False, 'OriginalExecutedByGuard': False}
    try:
        exit_code = run(args, report)
        write_report(args.report, report)
    except Exception as error:
        failure = {'Status': 'ORIGINAL_NINE_GUARD_REFUSED_OR_FAILED', 'ErrorType': type(error).__name__, 'Error': str(error), 'SdkCompared': False, 'SdkPass': False, 'GenericRuntimeParity': False, 'OriginalExecutedByGuard': False, 'PartialReportRetainedIfPresent': str(args.report) + '.partial'}
        args.report.write_bytes((json.dumps(failure, indent=2) + '\n').encode())
        raise
    print(json.dumps({key: report[key] for key in ('Status', 'SdkCompared', 'CertainAssertionsPassed', 'HypothesisContradicted')}))
    raise SystemExit(exit_code)


if __name__ == '__main__':
    main()
