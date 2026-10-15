import argparse
import importlib.util
import json
from pathlib import Path
import sys

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
PLAN_SHA = 'B565EA20C36B326D98CE9F854556C432142F0E074D33077679F6BF482CDBCC54'
POLICY_SHA = '8C205BF5171D233DEAD25A12C8F36E1CA90F2866C531BC2D29A0295997515C4B'
BASE_SHA = '46F8837A41981C15DD6D49E3F16CFE06ABB2A8DE2B404BBCF84D9B93A248150F'
STACK_SHA = '5BC59B5CE577DC0F1D099A86978E4051D6400C56D13F9554D9CA05BD81E82E59'
DECODER_SHA = 'A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713'
MEMORY_KEYS = ['total', 'processes', 'processes_used', 'system', 'atom', 'atom_used', 'binary', 'code', 'ets']
CASE_NAMES = ['alloc_info_empty', 'alloc_sizes_empty', 'alloc_nonlist', 'alloc_unknown', 'alloc_duplicate', 'alloc_improper', 'alloc_atom', 'alloc_order', 'alloc_raw_ref', 'memory_all', 'memory_empty', 'memory_order_duplicates', 'memory_invalid', 'memory_maximum', 'memory_improper', 'process_default', 'nif_apply_invalid_path', 'nif_apply_missing_path', 'alloc_info_binary_full']
MAX_ETF_BYTES = 8 * 1024 * 1024
MAX_TOTAL_BYTES = 32 * 1024 * 1024


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(path):
    return __import__('hashlib').sha256(path.read_bytes()).hexdigest().upper()


def load(path):
    return json.loads(path.read_bytes())


def helper(path, name, expected):
    require(sha(path) == expected, 'Frozen helper hash drift: ' + name)
    spec = importlib.util.spec_from_file_location(name, path)
    result = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(result)
    return result


def atom(name):
    return ('atom', name)


def seq(term, kind='tuple', count=None):
    require(type(term) is tuple and len(term) == 2 and term[0] == kind, 'Typed sequence')
    require(count is None or len(term[1]) == count, 'Sequence arity')
    return term[1]


def nonnegative(term):
    require(type(term) is tuple and len(term) == 2 and term[0] == 'integer' and type(term[1]) is int and term[1] >= 0, 'Nonnegative byte count')
    return term[1]


def allocator_result(term):
    if term == atom('false'):
        return
    previous = -1
    for instance in seq(term, 'list'):
        fields = seq(instance)
        if len(fields) == 3:
            require(fields[0] == atom('instance'), 'Numeric instance marker')
            number = nonnegative(fields[1])
            require(number > previous, 'Distinct ascending numeric instances')
            previous = number
            seq(fields[2], 'list')
        else:
            require(len(fields) == 2 and fields[0][0] == 'atom', 'Named instance pair')
            seq(fields[1], 'list')


def allocator_full_result(term):
    allocator_result(term)
    instances = seq(term, 'list')
    require(bool(instances), 'Positive full-info instance list required')
    for instance in instances:
        marker, number, data = seq(instance, count=3)
        require(marker == atom('instance'), 'Positive full-info numeric instance')
        rows = [seq(row) for row in seq(data, 'list')]
        require(all(len(row) in (2, 3) and row[0][0] == 'atom' for row in rows), 'Full-info property shape')
        keys = [row[0][1] for row in rows]
        require(len(keys) == len(set(keys)) and {'versions', 'options', 'mbcs', 'sbcs', 'calls'}.issubset(keys), 'Positive full-info property inventory')
        fields = {row[0][1]: row for row in rows}
        require(len(fields['versions']) == 3, 'Two original allocator version strings')
        for text in fields['versions'][1:]:
            require(bool(seq(text, 'list')) and all(0 <= nonnegative(c) <= 255 for c in seq(text, 'list')), 'Allocator version character list')
        for key in ['options', 'mbcs', 'sbcs', 'calls']:
            require(len(fields[key]) == 2, 'Full-info property arity')
            seq(fields[key][1], 'list')


def projection(value, expected, stack):
    fields = stack.captured(value)  # Retains/validates complete error stack, including fun frames.
    if expected in ('badarg', 'function_clause'):
        require(fields[:3] == (atom('exception'), atom('error'), atom(expected)), 'Expected guest error reason')
        require(len(seq(fields[3], 'list')) > 0, 'Full nonempty original error stack')
        return
    require(fields[0] == atom('return'), 'Expected returned observation')
    value = fields[1]
    if expected == 'allocator_full':
        allocator_full_result(value)
    elif expected == 'allocator_atom':
        allocator_result(value)
    elif expected == 'allocator_order':
        pairs = seq(value, 'list', 2)
        for pair, name in zip(pairs, ['binary_alloc', 'ets_alloc']):
            key, data = seq(pair, count=2)
            require(key == atom(name), 'Allocator request order')
            allocator_result(data)
    elif expected == 'memory_all':
        pairs = seq(value, 'list', len(MEMORY_KEYS))
        values = {}
        for pair, name in zip(pairs, MEMORY_KEYS):
            key, size = seq(pair, count=2)
            require(key == atom(name), 'Nine ordered memory keys')
            values[name] = nonnegative(size)
        require(values['total'] == values['system'] + values['processes'], 'Memory category partition')
        require(values['processes_used'] <= values['processes'] and values['atom_used'] <= values['atom'], 'Used storage bounds')
    elif expected == 'empty':
        seq(value, 'list', 0)
    elif expected == 'memory_order':
        pairs = seq(value, 'list', 3)
        sizes = []
        for pair, name in zip(pairs, ['ets', 'total', 'ets']):
            key, size = seq(pair, count=2)
            require(key == atom(name), 'Memory requested order and duplicates')
            sizes.append(nonnegative(size))
        require(sizes[0] == sizes[2], 'Duplicate requests share one snapshot')
    elif expected == 'process_default':
        keys = ['current_function', 'initial_call', 'status', 'message_queue_len', 'links', 'dictionary', 'trap_exit', 'error_handler', 'priority', 'group_leader', 'total_heap_size', 'heap_size', 'stack_size', 'reductions', 'garbage_collection', 'suspending']
        pairs = list(seq(value, 'list'))
        if pairs and seq(pairs[0], count=2)[0] == atom('registered_name'):
            pairs.pop(0)
        require([seq(p, count=2)[0] for p in pairs] == [atom(k) for k in keys], 'Native default process key order')
    elif expected == 'dynamic_nif':
        tag, reason = seq(value, count=2)
        code, message = seq(reason, count=2)
        require(tag == atom('error') and code == atom('bad_lib'), 'Dynamic load_nif special rejection')
        text = ''.join(chr(nonnegative(c)) for c in seq(message, 'list'))
        require(text == 'load_nif/2 must be explicitly called from the NIF module. It cannot be called through apply/3.', 'Pinned dynamic NIF rejection text')
    else:
        raise ValueError('Unknown authored projection')


def validate_contract(plan, policy, receipt):
    require(plan['PinnedCommit'] == policy['PinnedCommit'] == receipt['PinnedCommit'] == PIN, 'Exact original pin')
    require([c['Name'] for c in plan['Cases']] == CASE_NAMES and plan['Cases'] == policy['Cases'], 'Independent base18+extra cases/order/policy')
    require(receipt['Complete'] is True and receipt['SemanticAssertionsExecuted'] is False and receipt['SdkExecuted'] is False and receipt['SdkCompared'] is False and receipt['NativeFixtureUsed'] is False, 'Raw collector scope')
    require(receipt['OrderedFreshVmCases'] == CASE_NAMES and receipt['ExpectedFreshVmCases'] == 19 and receipt['SourcePlanSha256'] == PLAN_SHA, 'Complete collection binding')
    require(all(receipt.get(k, []) == [] for k in ['Exclusions', 'SkippedCases', 'ExcludedCases']), 'No exclusions')


def run(args, report):
    root, proposal = args.observations.resolve(), args.proposal.resolve()
    base = helper(args.base_guard, 'allocator_bound_base', BASE_SHA)
    stack = helper(args.stack_guard, 'allocator_bound_stack', STACK_SHA)
    decoder = helper(args.decoder, 'allocator_bound_decoder', DECODER_SHA)
    require(sha(proposal / 'source-plan.json') == PLAN_SHA and sha(proposal / 'expected-policy.json') == POLICY_SHA, 'Frozen observation policy/plan drift')
    plan, policy, receipt = load(proposal / 'source-plan.json'), load(proposal / 'expected-policy.json'), load(root / 'receipt.json')
    validate_contract(plan, policy, receipt)
    require(sum(r['Bytes'] for r in receipt['Files']) <= 128 * 1024 * 1024, 'Total raw manifest byte budget')
    members = base.verify_members(root, receipt['Files'])
    require({p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()} == members | {'receipt.json'}, 'Exact raw file closure')
    require(sha(root / 'source-plan.json') == PLAN_SHA, 'Copied raw source plan')
    for row in plan['Sources']:
        copied, source = base.child(root, 'sources/' + row['Path']), base.child(proposal, row['Path'])
        require(sha(copied) == sha(source) == row['Sha256'] and copied.stat().st_size == row['Bytes'], 'Observer input source drift')
    provenance = load(args.build_provenance)
    require(provenance['SourceHead'] == PIN and provenance['OriginalCasesExecuted'] is False and provenance['NativeFixtureUsed'] is False, 'Actual source-built original pin')
    require(provenance['HarnessCommit'] == args.expected_head and str(provenance['RunId']) == args.expected_run and str(provenance['RunAttempt']) == args.expected_attempt and provenance['Job'] == args.expected_job, 'Explicit CI identity')
    paths = ['observer-install/bin/erl', 'observer-install/bin/erlc', 'observer-install/lib/erlang/erts-17.1/bin/beam.smp', 'observer-install/lib/erlang/erts-17.1/bin/erlexec']
    require(sorted(r['Path'] for r in provenance['Files']) == sorted(paths), 'Four actual source-built tools')
    require(len(receipt['Tools']) == 2, 'Two collector tools')
    for actual, path in zip(receipt['Tools'], paths[:2]):
        bound = next(r for r in provenance['Files'] if r['Path'] == path)
        require(actual['Path'].endswith('/' + path) and actual['Sha256'] == bound['Sha256'], 'Actual compiler/emulator hash binding')
    require(provenance['PinnedReferenceSources'] == plan['PinnedSources'], 'Exact original source reference inventory')
    erl, erlc = [r['Path'] for r in receipt['Tools']]
    base.process(root, 'compile-observer')
    command = load(root / 'compile-observer.process.json')['Arguments']
    require(command[0] == erlc and '-Werror' in command and '+debug_info' in command and command[-1].endswith('/sources/memory_allocator_probe.erl'), 'Exact pinned observer compilation')
    for prefix in ['metadata', 'disassembly']:
        base.process(root, prefix, erl)
    metadata = seq(decoder.decode_exact((root / 'metadata.etf').read_bytes()), count=2)
    require(metadata[0] == atom('allocator_memory_metadata'), 'Metadata envelope')
    values = dict(seq(p, count=2) for p in seq(metadata[1], 'list', 7))
    require(base.character_list(values[atom('otp_release')]) == '29' and base.character_list(values[atom('erts_version')]) == '17.1' and values[atom('wordsize')] == ('integer', 8), 'Pinned runtime version and word size')
    require(base.character_list(values[atom('system_architecture')]) == 'x86_64-pc-linux-gnu', 'Linux x64 source build')
    total = 0
    results = []
    for case in policy['Cases']:
        name = case['Name']
        pid = base.process(root, name + '/observation', erl)
        command = load(root / name / 'observation.process.json')['Arguments']
        require('memory_allocator_probe:run(' + name + ')' in command[-1] and command[command.index('-pa') + 1].endswith('/observer-ebin'), 'Actual fresh case invocation')
        require(command[command.index('+S') + 1] == '1:1' and command[command.index('+A') + 1] == '1', 'Owned bounded scheduler profile')
        for suffix in ['stdout', 'stderr']:
            require((root / name / ('observation.' + suffix)).stat().st_size <= 65536, 'Bounded textual case output')
        raw = root / name / 'result.etf'
        require(2 <= raw.stat().st_size <= MAX_ETF_BYTES, 'Per-case raw ETF budget')
        total += raw.stat().st_size
        require(total <= MAX_TOTAL_BYTES, 'Aggregate case ETF budget')
        value = decoder.decode_exact(raw.read_bytes())
        projection(value, case['Projection'], stack)
        results.append(dict(Name=name, Scope=case['Scope'], TypedProjectionPassed=True, RawFile=name + '/result.etf', RawSha256=sha(raw), OwnedPid=pid, FullTypedObservation=stack.passive_json(value)))
    controls = load(args.controls / 'controls.json')
    require(controls['RawByteExitControlPassed'] is True and controls['OwnedInheritedPipeControlPassed'] is True and controls['OriginalEmulatorExecuted'] is False, 'Actual raw capture controls')
    failed, inherited = [load(args.controls / (p + '.process.json')) for p in ['exit-three', 'inherited-pipes']]
    require(failed['ExitCode'] == 3 and failed['TimedOut'] is False and failed['ParentReaped'] is True, 'Nonzero raw exit control')
    require((args.controls / 'exit-three.stdout').read_bytes() == b'\x00\xffraw' and (args.controls / 'exit-three.stderr').read_bytes() == b'\xfeerr', 'Unchanged nonUTF8 raw streams')
    require(inherited['ExitCode'] == 0 and inherited['TimedOut'] is True and inherited['PipesClosed'] is True and inherited['ParentReaped'] is True and (args.controls / 'inherited-pipes.stdout').read_bytes() == b'owned-child-pipes', 'Exited parent inherited pipe control')
    require(type(failed['OwnedPid']) is int and failed['OwnedPid'] > 0 and type(inherited['OwnedPid']) is int and inherited['OwnedPid'] > 0 and failed['OwnedPid'] != inherited['OwnedPid'], 'Owned distinct capture control identities')
    control_files = [dict(Path=p.relative_to(args.controls).as_posix(), Bytes=p.stat().st_size, Sha256=sha(p)) for p in sorted(args.controls.rglob('*')) if p.is_file()]
    report.update(CaptureControlFiles=control_files, Status='ORIGINAL_BASE18_PLUS_FULLINFO_TYPED_PROJECTIONS_PASS', PinnedCommit=PIN, PlanSha256=PLAN_SHA, PolicySha256=POLICY_SHA, Cases=results, BaseAllocatorCases=9, ExtraFullAllocatorCases=1, MemoryCases=6, AdjacentCases=3, DisabledAllocatorProfileCovered=False, RawReceiptSha256=sha(root / 'receipt.json'), BuildProvenanceSha256=sha(args.build_provenance), CiIdentityNetworkAuthenticated=False, ExplicitExpectedCiIdentity=dict(Head=args.expected_head, Run=args.expected_run, Attempt=args.expected_attempt, Job=args.expected_job), PhysicalValues='Preserved original observations; no SDK profile or numeric parity claim')
    return stack


def main():
    parser = argparse.ArgumentParser()
    for name in ['observations', 'proposal', 'build-provenance', 'base-guard', 'stack-guard', 'decoder', 'controls', 'report']:
        parser.add_argument('--' + name, type=Path, required=True)
    for name in ['expected-head', 'expected-run', 'expected-attempt', 'expected-job']:
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    require(not args.report.exists() and not args.report.resolve().is_relative_to(args.observations.resolve()) and not args.report.resolve().is_relative_to(args.controls.resolve()), 'Fresh report outside raw evidence required')
    report = dict(Status='ORIGINAL_BASE18_PLUS_FULLINFO_GUARD_REFUSED_OR_FAILED', SdkCompared=False, SdkPass=False, GenericRuntimeParity=False, OriginalExecutedByGuard=False)
    try:
        stack = run(args, report)
        stack.write_report(args.report, report, 32 * 1024 * 1024)
    except Exception as error:
        args.report.write_bytes((json.dumps(dict(Status='ORIGINAL_BASE18_PLUS_FULLINFO_GUARD_REFUSED_OR_FAILED', ErrorType=type(error).__name__, Error=str(error), SdkCompared=False, SdkPass=False, OriginalExecutedByGuard=False, PartialReportRetainedIfPresent=str(args.report) + '.partial'), indent=2) + '\n').encode())
        raise
    print(json.dumps(dict(Status=report['Status'], Cases=len(report['Cases']), SdkCompared=False)))


if __name__ == '__main__':
    main()
