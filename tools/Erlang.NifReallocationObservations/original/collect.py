import argparse
import json
import os
import pathlib
import shutil
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent.parent))
from reviewed_common import CASES, PIN, bound_helpers, capture, clean_environment, exact_head, files_receipt, sha, verified_plan, verify_otp, write_fresh


def main():
    parser = argparse.ArgumentParser()
    for name in ('repository', 'otp-source', 'otp-include', 'otp-bin', 'plan', 'output', 'source-build-receipt'):
        parser.add_argument('--' + name, type=pathlib.Path, required=True)
    for name in ('expected-head', 'approved-plan-sha256', 'approved-source-build-receipt-sha256'):
        parser.add_argument('--' + name, required=True)
    args = parser.parse_args()
    if sys.platform != 'linux' or os.uname().machine != 'x86_64':
        raise RuntimeError('Original reallocation collector is Linux x64 only')
    root, otp, include, binaries, output = [path.resolve() for path in (args.repository, args.otp_source, args.otp_include, args.otp_bin, args.output)]
    plan = verified_plan(root, args.plan.resolve(), args.approved_plan_sha256)
    head = exact_head(root, args.expected_head)
    verify_otp(otp, include, plan)
    if sha(args.source_build_receipt.read_bytes()) != args.approved_source_build_receipt_sha256.upper():
        raise RuntimeError('Independent source-build receipt digest required')
    build_proof = json.loads(args.source_build_receipt.read_bytes())
    if build_proof['PinnedOtpCommit'] != PIN or build_proof['BuiltFromPinnedSource'] is not True:
        raise RuntimeError('Independently approved pinned source-build proof required')
    runtime_files = build_proof['RuntimeFiles']
    if not runtime_files or len(runtime_files) != len({item['Path'] for item in runtime_files}):
        raise RuntimeError('Exact built runtime file closure required')
    required_binaries = {str((binaries / name).resolve()) for name in ('erl', 'erlc')}
    actual_binaries = set()
    for item in runtime_files:
        path = pathlib.Path(item['Path']).resolve()
        if not path.is_relative_to(otp) or not path.is_file() or path.stat().st_size != item['Bytes'] or sha(path.read_bytes()) != item['Sha256']:
            raise RuntimeError('Pinned built runtime file differs from approved source-build receipt')
        actual_binaries.add(str(path))
    if not required_binaries.issubset(actual_binaries) or not any(pathlib.Path(path).name == 'beam.smp' for path in actual_binaries):
        raise RuntimeError('Approved source-build closure must include actual erl/erlc/beam.smp')
    if not binaries.is_relative_to(otp) or not (binaries / 'erl').is_file() or not (binaries / 'erlc').is_file():
        raise RuntimeError('Actual built OTP binaries must belong to pinned tree')
    helpers = bound_helpers(root, plan)
    if output.exists():
        raise RuntimeError('Fresh output required')
    output.mkdir(parents=True)
    try:
        shutil.copyfile(args.source_build_receipt, output / 'approved-source-build-receipt.json')
        shutil.copyfile(args.plan, output / 'reviewed-source-plan.json')
        env = clean_environment()
        native = root / 'tools/Erlang.NifReallocationObservations/native'
        observer = root / 'tools/Erlang.NifReallocationObservations/original'
        for item in ('reallocation_fixture.c', 'reallocation_probe_names.h'):
            shutil.copyfile(native / item, output / item)
        for item in ('memory_allocator_nif_fixture.erl', 'reallocation_observer.erl'):
            shutil.copyfile(observer / item, output / item)
        headers = output / 'headers'
        headers.mkdir()
        for item in [entry['Name'] for entry in plan['Headers']] + ['erl_int_sizes_config.h']:
            shutil.copyfile(include / item, headers / item)
        ebin = output / 'ebin'
        ebin.mkdir()
        gcc, readelf, nm = [shutil.which(name) for name in ('gcc', 'readelf', 'nm')]
        if not all((gcc, readelf, nm)):
            raise RuntimeError('Existing gcc/readelf/nm required; nothing installed')
        def run(command, name, timeout=90):
            return capture(helpers['bounded_process'], command, output, env, output / name, timeout)
        run([gcc, '--version'], 'compiler-version')
        run([gcc, '-shared', '-fPIC', '-O2', '-std=c11', '-Wall', '-Wextra', '-Werror', '-I', headers,
             'reallocation_fixture.c', '-o', 'reallocation_fixture.so'], 'compile-fixture')
        run([readelf, '-h', '-Ws', '-d', 'reallocation_fixture.so'], 'fixture-readelf')
        symbols = run([nm, '-D', '--defined-only', 'reallocation_fixture.so'], 'fixture-defined')
        if {line.split()[-1] for line in symbols.stdout.decode().splitlines() if line.split()} != {'nif_init'}:
            raise RuntimeError('Unexpected probe dynamic exports')
        run([nm, '-D', '--undefined-only', 'reallocation_fixture.so'], 'fixture-undefined')
        run([binaries / 'erlc', '-Werror', '+debug_info', '-o', ebin, 'memory_allocator_nif_fixture.erl', 'reallocation_observer.erl'], 'compile-observers')
        base = [binaries / 'erl', '+S', '1:1', '+SDcpu', '1', '+SDio', '1', '+A', '1', '-noshell', '-noinput', '-pa', ebin]
        env['REALLOCATION_FIXTURE_PATH'] = str(output / 'reallocation_fixture')
        bindings = []
        def evaluate(expression, target, prefix):
            env['REALLOCATION_ETF'] = str(target)
            code = 'R=' + expression + ',ok=file:write_file(os:getenv("REALLOCATION_ETF"),term_to_binary(R)),io:format("~tp.~n",[R]),halt().'
            run([*base, '-eval', code], prefix)
            helpers['passive_etf'].decode_exact(target.read_bytes())
            bindings.append({'Prefix': prefix, 'Target': target.name, 'EtfSha256': sha(target.read_bytes()),
                'StdoutSha256': sha((output / (prefix + '.stdout')).read_bytes()),
                'StderrSha256': sha((output / (prefix + '.stderr')).read_bytes()),
                'ProcessSha256': sha((output / (prefix + '.process.json')).read_bytes()),
                'Evaluation': code, 'EnvironmentBindings': {'REALLOCATION_ETF': str(target), 'REALLOCATION_FIXTURE_PATH': env['REALLOCATION_FIXTURE_PATH']}})
        evaluate('reallocation_observer:metadata()', output / 'metadata.etf', 'metadata')
        # Each case has a fresh VM and verifies real enabled driver before any fixture call.
        for case in CASES:
            evaluate('reallocation_observer:run(' + case + ')', output / (case + '.etf'), case)
        write_fresh(output / 'result-bindings.json', {'Bindings': bindings})
        write_fresh(output / 'receipt.json', {'Complete': True, 'Head': head, 'PinnedOtpCommit': PIN,
            'PlanSha256': sha(args.plan.read_bytes()), 'IndependentlyApprovedSourceBuildReceiptSha256': sha(args.source_build_receipt.read_bytes()),
            'Cases': CASES, 'SemanticCompared': False, 'Files': files_receipt(output),
            'Tools': [{'Path': str(path), 'Sha256': sha(pathlib.Path(path).read_bytes())} for path in (binaries / 'erl', binaries / 'erlc', gcc, readelf, nm)]})
    except BaseException as failure:
        write_fresh(output / 'failure-receipt.json', {'Complete': False, 'ErrorType': type(failure).__name__, 'Error': str(failure), 'Files': files_receipt(output)})
        raise


if __name__ == '__main__':
    main()
