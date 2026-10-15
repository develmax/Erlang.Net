import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
PLAN_SHA = 'EAC7F22640E3233268FD81EC22568A08666BAD5A190A81748DC1A49A1D5B20C0'
POLICY_SHA = 'B1CA9E182911DE41ECF74214B889E51FFF6CE7E77F244A77DA49EFCB0DFA2F15'
DECODER_SHA = 'F09799AD51AB4A49C271977E987F1C186C76FF4A87C943D19E8D6AA709F1DB4C'
MODULE = 'nif_pending_varargs_fixture'
CASES = ['tuple_empty', 'tuple_single', 'tuple_spill', 'list_empty', 'list_single', 'list_spill',
         'pending_none', 'pending_raise_twice', 'pending_badarg_ignored', 'pending_detail',
         'pending_badarg_then_raise', 'pending_raise_then_badarg', 'load_veto']


def require(value, message):
    if not value:
        raise ValueError(message)


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def atom(name):
    return ('atom', name)


def integer(number):
    return ('integer', number)


def sequence(value, kind, size=None):
    require(type(value) is tuple and len(value) == 2 and value[0] == kind, 'Expected ETF ' + kind)
    values = value[1]
    require(type(values) is tuple and (size is None or len(values) == size), 'ETF sequence shape/count')
    return values


def pair(value, key):
    fields = sequence(value, 'tuple', 2)
    require(fields[0] == atom(key), 'Missing typed observation key: ' + key)
    return fields[1]


def character_list(value):
    values = sequence(value, 'list')
    require(all(type(v) is tuple and v[0] == 'integer' and 0 <= v[1] <= 255 for v in values), 'Expected byte character list')
    return bytes(v[1] for v in values).decode('latin1')


def capture_return(value):
    fields = sequence(value, 'tuple', 2)
    require(fields[0] == atom('return'), 'Expected raw return capture')
    return fields[1]


def exception(value, reason, function=None, before=False):
    fields = sequence(value, 'tuple', 4)
    require(fields[:3] == (atom('exception'), atom('error'), reason), 'Exception class/reason mismatch')
    stack = sequence(fields[3], 'list')
    require(len(stack) > 0, 'Empty raw exception stack')
    first = sequence(stack[0], 'tuple')
    require(len(first) in (3, 4), 'First frame shape')
    require(first[:2] == (atom('erlang'), atom('nif_error')) if before else first[:2] == (atom(MODULE), atom(function)), 'First NIF/error frame mismatch')
    if not before:
        require(first[2] in (integer(0), ('list', ())), 'First native frame must have arity zero')
    if len(first) == 4:
        sequence(first[3], 'list')
    # Raw full stack stays in result.etf; no rewriting, truncation or source-line equality.
    return sha(repr(fields[3]).encode('utf-8'))


def child(root, relative):
    require(type(relative) is str, 'Manifest path must be text')
    normalized = Path(relative)
    require(not normalized.is_absolute() and '..' not in normalized.parts and '\\' not in relative, 'Unsafe manifest path')
    candidate = (root / normalized).resolve()
    require(candidate.is_relative_to(root.resolve()) and candidate.is_file(), 'Missing or escaping evidence file')
    require(candidate.stat().st_size <= 64 * 1024 * 1024, 'Evidence file exceeds guard bound')
    return candidate


def verify_members(root, records):
    require(type(records) is list and records, 'Empty receipt manifest')
    paths = []
    for record in records:
        relative = record['Path']
        require(relative not in paths, 'Duplicate manifest member')
        paths.append(relative)
        data = child(root, relative).read_bytes()
        require(type(record['Bytes']) is int and len(data) == record['Bytes'] and sha(data) == record['Sha256'], 'Receipt hash/length drift: ' + relative)
    return set(paths)


def process(root, prefix, expected_erl=None, case=None):
    record = load(child(root, prefix + '.process.json'))
    require(record['ExitCode'] == 0 and record['TimedOut'] is False and record['PipesClosed'] is True and record['ParentReaped'] is True, 'Process infrastructure failure: ' + prefix)
    require(type(record['OwnedPid']) is int and record['OwnedPid'] > 0, 'Missing owned process identity')
    arguments = record['Arguments']
    require(type(arguments) is list and arguments and all(type(a) is str for a in arguments), 'Missing actual argv')
    if expected_erl:
        require(arguments[0] == expected_erl and arguments[-2] == '-eval', 'Original emulator command drift')
    if case:
        require('nif_pending_varargs_observer:run(' + case + ')' in arguments[-1], 'Case invocation drift')
    child(root, prefix + '.stdout')
    child(root, prefix + '.stderr')
    return record['OwnedPid']


def run(args):
    root, proposal = args.observations.resolve(), args.proposal.resolve()
    require(not args.report.exists(), 'Fresh report path required')
    require(sha((proposal / 'source-plan.json').read_bytes()) == PLAN_SHA, 'Frozen source plan drift')
    require(sha((proposal / 'expected-policy.json').read_bytes()) == POLICY_SHA, 'Pre-execution policy drift')
    require(sha(args.decoder_source.read_bytes()) == DECODER_SHA, 'Trusted ETF decoder source drift')
    spec = importlib.util.spec_from_file_location('nif_pending_trusted_etf_decoder', args.decoder_source)
    decoder = importlib.util.module_from_spec(spec)
    previous_bytecode = sys.dont_write_bytecode
    try:
        sys.dont_write_bytecode = True
        spec.loader.exec_module(decoder)
    finally:
        sys.dont_write_bytecode = previous_bytecode
    decode = decoder.decode_exact
    plan, policy = load(proposal / 'source-plan.json'), load(proposal / 'expected-policy.json')
    require(plan['PinnedCommit'] == PIN and plan['Cases'] == CASES, 'Pin/case list drift')
    require([r['Name'] for r in policy['Cases']] == CASES, 'Predeclared policy case count/order drift')
    require(len(set(CASES)) == 13 and len(policy['Cases']) == 13, 'Thirteen distinct cases required')
    receipt = load(root / 'receipt.json')
    require(receipt['Complete'] is True and receipt['SemanticAssertionsExecuted'] is False, 'Collection is not completed raw evidence')
    require(receipt['PinnedCommit'] == PIN and receipt['PlanSha256'] == PLAN_SHA, 'Receipt provenance drift')
    require(receipt['OrderedFreshVmCases'] == CASES and type(receipt['ExpectedCaseCount']) is int and receipt['ExpectedCaseCount'] == 13, 'Missing/reordered/extra original case')
    for key in ('ExcludedCases', 'SkippedCases', 'Exclusions'):
        require(receipt.get(key, []) == [], 'Exclusions are forbidden')
    members = verify_members(root, receipt['Files'])
    actual = {f.relative_to(root).as_posix() for f in root.rglob('*') if f.is_file()}
    require(actual == members | {'receipt.json'}, 'Unmanifested/missing raw evidence; failure receipts are not success')
    require(sha((root / 'source-plan.json').read_bytes()) == PLAN_SHA, 'Collected source plan drift')
    for source in plan['Sources']:
        require((root / source['Name']).read_bytes() == (proposal / source['Name']).read_bytes(), 'Collected source differs from reviewed proposal')
        require(sha((root / source['Name']).read_bytes()) == source['Sha256'], 'Reviewed source hash drift')
    require(sha((root / 'fixture.c').read_bytes()) == receipt['FixtureSourceSha256'] == plan['FixtureSourceSha256'], 'Native fixture source drift')
    require((root / 'fixture.c').read_bytes() == (proposal / 'fixture.c').read_bytes(), 'Unreviewed native fixture')
    for header in plan['Headers']:
        require(sha((root / 'headers' / header['Name']).read_bytes()) == header['Sha256'], 'Pinned public header drift')
    require(sha((root / 'headers/erl_int_sizes_config.h').read_bytes()) == receipt['ActualConfiguredHeaderSha256'], 'Configured ABI sizes drift')
    require(sha((root / 'fixture.so').read_bytes()) == receipt['FixtureSharedLibrarySha256'], 'Original native object drift')
    provenance = load(args.build_provenance)
    require(provenance['SourceHead'] == PIN and provenance['NativeNifCasesExecuted'] is False and len(provenance['Files']) == 4, 'Original build provenance mismatch')
    tools = receipt['Tools']
    require(len(tools) == 4, 'Expected erl/erlc/gcc/readelf provenance')
    for tool in tools[:2]:
        require(any(tool['Path'].endswith(r['Path']) and tool['Sha256'] == r['Sha256'] for r in provenance['Files']), 'Emulator tool not bound to source build')
    erl = tools[0]['Path']
    for prefix in ('gcc-version', 'fixture-build', 'fixture-symbols', 'fixture-dynamic', 'erlc', 'metadata', 'disassembly'):
        process(root, prefix, erl if prefix in ('metadata', 'disassembly') else None)
    metadata = sequence(decode((root / 'metadata.etf').read_bytes()), 'tuple', 9)
    require(metadata[0] == atom('nif_pending_varargs_metadata'), 'Metadata envelope')
    meta = {}
    for item in metadata[1:]:
        row = sequence(item, 'tuple', 2)
        require(row[0] not in meta, 'Duplicate runtime metadata key')
        meta[row[0]] = row[1]
    require(character_list(meta[atom('otp_release')]) == '29' and character_list(meta[atom('erts_version')]) == '17.1', 'Wrong original release')
    require(meta[atom('wordsize')] == integer(8) and character_list(meta[atom('system_architecture')]) == 'x86_64-pc-linux-gnu', 'Wrong native ABI platform')
    results, pids = {}, []
    for case, declared in zip(CASES, policy['Cases']):
        pids.append(process(root, case + '/observation', erl, case))
        raw = (root / case / 'result.etf').read_bytes()
        fields = sequence(decode(raw), 'tuple', 8)
        require(fields[:2] == (atom('nif_pending_varargs_observation'), atom(case)), 'Typed case envelope/order')
        require(pair(fields[2], 'load_info') == integer(17 if case == 'load_veto' else 42), 'Load marker drift')
        exception(pair(fields[3], 'before_load'), atom('not_loaded'), before=True)
        load_result, call = pair(fields[4], 'load_result'), pair(fields[5], 'call_result')
        before, after = sequence(pair(fields[6], 'taints_before'), 'list'), sequence(pair(fields[7], 'taints_after'), 'list')
        require(all(v[0] == 'atom' for v in before + after), 'Taint entries must be atoms')
        require(atom(MODULE) not in before and after == before + (atom(MODULE),), 'Persistent pre-init taint admission/order mismatch')
        require(len(set(after)) == len(after), 'Duplicate taint entry')
        if case == 'load_veto':
            error = sequence(capture_return(load_result), 'tuple', 2)
            require(error[0] == atom('error'), 'Veto did not return error')
            detail = sequence(error[1], 'tuple', 2)
            require(detail[0] == atom('load') and character_list(detail[1]).startswith('Library load-call unsuccessful (1)'), 'Veto typed load reason drift')
            exception(call, atom('not_loaded'), before=True)
        else:
            require(capture_return(load_result) == atom('ok'), 'Native original load failed')
            wanted = declared['ExpectedCall']
            if wanted['Kind'] == 'return':
                expected = ('tuple' if wanted['TermType'] == 'tuple' else 'list', tuple(integer(v) for v in wanted['Items']))
                require(capture_return(call) == expected, 'Typed constructor/no-pending observation mismatch')
            else:
                reason = atom(wanted['ReasonAtom']) if 'ReasonAtom' in wanted else integer(wanted['ReasonInteger']) if 'ReasonInteger' in wanted else ('tuple', tuple(integer(v) for v in wanted['ReasonTuple']))
                exception(call, reason, case)
        results[case] = {'Passed': True, 'RawEtfSha256': sha(raw), 'FullStackAndTaintsRetained': True}
    require(len(set(pids)) == len(CASES), 'Fresh VM process identities duplicated')
    identity = None
    if args.ci_envelope:
        identity = load(args.ci_envelope)
        require(identity['RunConclusion'] == 'success' and identity['JobConclusion'] == 'success', 'CI infrastructure did not succeed')
        require(args.expected_head is not None and args.expected_run is not None and args.expected_job is not None, 'Explicit expected CI identity required')
        require(identity['Head'] == args.expected_head and identity['Run'] == args.expected_run and identity['Job'] == args.expected_job, 'External exact-head CI identity drift')
        require(identity['SourcePlanSha256'] == PLAN_SHA, 'CI plan identity drift')
    report = {'Status': 'ORIGINAL_ONLY_THIRTEEN_TYPED_SEMANTIC_PASS', 'PinnedCommit': PIN,
              'SourcePlanSha256': PLAN_SHA, 'PredeclaredPolicySha256': POLICY_SHA,
              'DecoderSha256': DECODER_SHA, 'Cases': results, 'ReceiptFilesVerified': len(members),
              'RawEvidenceDirectory': str(root), 'CiIdentity': identity, 'CiEnvelopeMatchedExpectedFields': identity is not None, 'CiIdentityAuthenticatedByNetwork': False,
              'SdkCompared': False, 'SdkPass': False, 'GenericNifSupport': False,
              'NativeUnloadCounterCompared': False, 'Exclusions': [],
              'Scope': 'Independent original-only semantics. Does not establish SDK ABI/lifecycle/route compatibility.'}
    args.report.write_bytes((json.dumps(report, indent=2) + '\n').encode('utf-8'))
    print(json.dumps({'Status': report['Status'], 'Cases': len(results), 'SdkCompared': False}))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--observations', type=Path, required=True)
    parser.add_argument('--proposal', type=Path, required=True)
    parser.add_argument('--build-provenance', type=Path, required=True)
    parser.add_argument('--decoder-source', type=Path, required=True)
    parser.add_argument('--ci-envelope', type=Path)
    parser.add_argument('--expected-head')
    parser.add_argument('--expected-run', type=int)
    parser.add_argument('--expected-job', type=int)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    require(not args.report.exists(), 'Refuse to overwrite a prior report')
    require(not args.report.resolve().is_relative_to(args.observations.resolve()), 'Report must stay outside immutable raw evidence')
    try:
        run(args)
    except Exception as failure:
        args.report.write_bytes((json.dumps({'Status': 'ORIGINAL_GUARD_REFUSED_OR_FAILED', 'ErrorType': type(failure).__name__, 'Error': str(failure), 'SdkCompared': False, 'SdkPass': False}, indent=2) + '\n').encode('utf-8'))
        raise


if __name__ == '__main__':
    main()
