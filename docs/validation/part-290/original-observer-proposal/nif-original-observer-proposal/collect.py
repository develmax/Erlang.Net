import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
FIXTURE_SHA = '7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074'
CASES = ['increment_zero', 'increment_negative', 'increment_maximum_success', 'increment_maximum_badarg', 'increment_atom_badarg', 'greeting', 'load_veto']
OUTPUT = None
COMPLETED = []


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def execute(arguments, cwd, environment, prefix, timeout=90):
    try:
        result = subprocess.run(arguments, cwd=cwd, env=environment, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=timeout, check=False)
    except subprocess.TimeoutExpired as error:
        prefix.with_suffix('.stdout').write_bytes(error.stdout or b'')
        prefix.with_suffix('.stderr').write_bytes(error.stderr or b'')
        prefix.with_suffix('.process.json').write_text(json.dumps({'Arguments': arguments, 'ExitCode': None, 'TimedOut': True}, indent=2) + '\n', encoding='utf-8')
        raise
    prefix.with_suffix('.stdout').write_bytes(result.stdout)
    prefix.with_suffix('.stderr').write_bytes(result.stderr)
    prefix.with_suffix('.process.json').write_text(json.dumps({'Arguments': arguments, 'ExitCode': result.returncode}, indent=2) + '\n', encoding='utf-8')
    if result.returncode != 0:
        raise RuntimeError('Original process failed: ' + str(prefix))
    return result


def main():
    global OUTPUT
    parser = argparse.ArgumentParser()
    parser.add_argument('--otp-source', type=pathlib.Path, required=True)
    parser.add_argument('--otp-include', type=pathlib.Path, required=True)
    parser.add_argument('--otp-bin', type=pathlib.Path, required=True)
    parser.add_argument('--fixture-source', type=pathlib.Path, required=True)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    parser.add_argument('--approved-plan-sha256', required=True)
    args = parser.parse_args()
    if not sys.platform.startswith('linux'):
        raise RuntimeError('Prepared observer recipe is Linux only.')
    proposal = pathlib.Path(__file__).resolve().parent
    plan_bytes = (proposal / 'source-plan.json').read_bytes()
    if sha(plan_bytes) != args.approved_plan_sha256.upper():
        raise RuntimeError('Independently reviewed observer plan hash mismatch.')
    plan = json.loads(plan_bytes)
    if plan['PinnedCommit'] != PIN or plan['Cases'] != CASES:
        raise RuntimeError('Observer pin or ordered cases changed.')
    for record in plan['Sources']:
        if sha((proposal / record['Name']).read_bytes()) != record['Sha256']:
            raise RuntimeError('Reviewed source changed: ' + record['Name'])
    if sha((proposal / 'fixture.c').read_bytes()) != FIXTURE_SHA:
        raise RuntimeError('Fixture differs from the actual SDK native fixture C source.')
    fixture_source = args.fixture_source.resolve()
    if sha(fixture_source.read_bytes()) != FIXTURE_SHA:
        raise RuntimeError('Explicit fixture source differs from the reviewed unchanged native fixture.')
    otp = args.otp_source.resolve()
    include = args.otp_include.resolve()
    if not include.is_relative_to(otp):
        raise RuntimeError('Built include directory must belong to the same pinned OTP source tree.')
    actual = subprocess.check_output(['git', '-C', str(otp), 'rev-parse', 'HEAD'], text=True).strip()
    if actual != PIN:
        raise RuntimeError('OTP source HEAD differs from exact pinned commit.')
    dirty = subprocess.check_output(['git', '-C', str(otp), 'status', '--porcelain', '--untracked-files=no'], text=True)
    if dirty.strip():
        raise RuntimeError('Pinned OTP tracked source is dirty; retain/audit it before original observation.')
    for header in plan['Headers']:
        native = (include / header['Name']).read_bytes()
        original = subprocess.check_output(['git', '-C', str(otp), 'show', PIN + ':' + header['GitPath']])
        if native != original or sha(native) != header['Sha256']:
            raise RuntimeError('Built public header does not match pinned source: ' + header['Name'])
    generated_sizes = include / 'erl_int_sizes_config.h'
    if not generated_sizes.is_file():
        raise RuntimeError('Actual configured OTP integer-size header is required.')
    otp_bin = args.otp_bin.resolve()
    if not otp_bin.is_relative_to(otp):
        raise RuntimeError('Explicit built OTP bin directory must belong to the same pinned source tree.')
    erl = otp_bin / 'erl'
    erlc = otp_bin / 'erlc'
    if not erl.is_file() or not erlc.is_file():
        raise RuntimeError('Build pinned OTP first; this observer does not build/download an emulator.')
    gcc = shutil.which('gcc')
    readelf = shutil.which('readelf')
    if not gcc or not readelf:
        raise RuntimeError('Existing gcc and readelf are required; nothing is installed.')
    output = args.output.resolve()
    if output.exists():
        raise RuntimeError('Fresh output directory required; no previous evidence is overwritten.')
    output.mkdir(parents=True)
    OUTPUT = output
    ebin = output / 'ebin'
    ebin.mkdir()
    headers_out = output / 'headers'
    headers_out.mkdir()
    for name in [record['Name'] for record in plan['Headers']] + ['erl_int_sizes_config.h']:
        shutil.copyfile(include / name, headers_out / name)
    for record in plan['Sources']:
        shutil.copyfile(proposal / record['Name'], output / record['Name'])
    shutil.copyfile(fixture_source, output / 'fixture.c')
    (output / 'source-plan.json').write_bytes(plan_bytes)
    environment = os.environ.copy()
    for name in ['ERL_AFLAGS', 'ERL_FLAGS', 'ERL_ZFLAGS']:
        environment.pop(name, None)
    execute([gcc, '--version'], output, environment, output / 'gcc-version')
    execute([gcc, '-shared', '-fPIC', '-O2', '-std=c11', '-Wall', '-Wextra', '-Werror', '-I', str(headers_out), 'fixture.c', '-o', 'fixture.so'], output, environment, output / 'fixture-build')
    execute([readelf, '-Ws', 'fixture.so'], output, environment, output / 'fixture-symbols')
    execute([readelf, '-d', 'fixture.so'], output, environment, output / 'fixture-dynamic')
    execute([str(erlc), '-Werror', '+debug_info', '-o', str(ebin), 'memory_allocator_nif_fixture.erl', 'nif_original_observer.erl'], output, environment, output / 'erlc')
    environment['NIF_FIXTURE_PATH'] = str(output / 'fixture')
    base = [str(erl), '+S', '1:1', '+SDcpu', '1', '+SDio', '1', '+A', '1', '-noshell', '-noinput', '-pa', str(ebin)]
    evaluation = 'Result=nif_original_observer:metadata(),ok=file:write_file(os:getenv("NIF_OBSERVER_ETF"),term_to_binary(Result)),io:format("~tp.~n",[Result]),halt().'
    environment['NIF_OBSERVER_ETF'] = str(output / 'metadata.etf')
    execute(base + ['-eval', evaluation], output, environment, output / 'metadata')
    disassembly = 'Results=[beam_disasm:file(P)||P<-filelib:wildcard("ebin/*.beam")],ok=file:write_file("disassembly.etf",term_to_binary(Results)),io:format("~tp.~n",[Results]),halt().'
    execute(base + ['-eval', disassembly], output, environment, output / 'disassembly')
    for case in CASES:
        case_directory = output / case
        case_directory.mkdir()
        environment['NIF_OBSERVER_ETF'] = str(case_directory / 'result.etf')
        evaluation = 'Result=nif_original_observer:run(' + case + '),ok=file:write_file(os:getenv("NIF_OBSERVER_ETF"),term_to_binary(Result)),io:format("~tp.~n",[Result]),halt().'
        execute(base + ['-eval', evaluation], output, environment, case_directory / 'observation')
        raw = (case_directory / 'result.etf').read_bytes()
        if not raw or raw[0] != 131:
            raise RuntimeError('Missing original ETF output: ' + case)
        COMPLETED.append(case)
    receipt = {
        'Status': 'Original raw observations collected; independent typed semantic review required',
        'Complete': True,
        'PinnedCommit': PIN, 'PlanSha256': sha(plan_bytes), 'FixtureSourceSha256': FIXTURE_SHA,
        'FixtureSharedLibrarySha256': sha((output / 'fixture.so').read_bytes()),
        'ActualConfiguredHeaderSha256': sha(generated_sizes.read_bytes()),
        'OrderedFreshVmCases': COMPLETED, 'ExpectedCaseCount': len(CASES),
        'SemanticAssertionsExecuted': False, 'NativeUnloadCounterCompared': False,
        'Tools': [{'Path': str(path), 'Sha256': sha(pathlib.Path(path).read_bytes())} for path in [erl, erlc, gcc, readelf]],
        'Files': [{'Path': file.relative_to(output).as_posix(), 'Bytes': file.stat().st_size, 'Sha256': sha(file.read_bytes())} for file in sorted(output.rglob('*')) if file.is_file()],
    }
    (output / 'receipt.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'Receipt': str(output / 'receipt.json'), 'Complete': True, 'FreshVms': len(COMPLETED), 'SemanticAssertionsExecuted': False}))


if __name__ == '__main__':
    try:
        main()
    except BaseException as error:
        failure = {
            'Complete': False, 'PinnedCommit': PIN, 'Status': 'Refused or interrupted; no complete native observation claim',
            'ErrorType': type(error).__name__, 'Error': str(error),
            'OrderedFreshVmCases': COMPLETED, 'ExpectedCaseCount': len(CASES),
            'SemanticAssertionsExecuted': False, 'NativeUnloadCounterCompared': False,
        }
        if OUTPUT is not None:
            failure['Files'] = [{'Path': file.relative_to(OUTPUT).as_posix(), 'Bytes': file.stat().st_size, 'Sha256': sha(file.read_bytes())} for file in sorted(OUTPUT.rglob('*')) if file.is_file()]
            (OUTPUT / 'failure-receipt.json').write_text(json.dumps(failure, indent=2) + '\n', encoding='utf-8')
        print(json.dumps(failure), file=sys.stderr)
        raise
