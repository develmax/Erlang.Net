import argparse
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True
from process_capture import execute

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
OBSERVER = 'memory_allocator_probe'
MAX_ETF_BYTES = 8 * 1024 * 1024
OUTPUT = None
COMPLETED = []


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def main():
    global OUTPUT
    parser = argparse.ArgumentParser()
    parser.add_argument('--otp-source', type=pathlib.Path, required=True)
    parser.add_argument('--otp-bin', type=pathlib.Path, required=True)
    parser.add_argument('--approved-plan-sha256', required=True)
    parser.add_argument('--output', type=pathlib.Path, required=True)
    args = parser.parse_args()
    if sys.platform != 'linux' or os.uname().machine != 'x86_64':
        raise RuntimeError('Reviewed original recipe requires Linux x86_64.')
    source = pathlib.Path(__file__).resolve().parent
    plan_bytes = (source / 'source-plan.json').read_bytes()
    if sha(plan_bytes) != args.approved_plan_sha256.upper():
        raise RuntimeError('Reviewed source plan hash differs.')
    plan = json.loads(plan_bytes)
    if plan['PinnedCommit'] != PIN or plan['ExpectedFreshVmCases'] != 19:
        raise RuntimeError('Original pin or case count differs.')
    for item in plan['Sources']:
        path = (source / item['Path']).resolve()
        if not path.is_relative_to(source) or sha(path.read_bytes()) != item['Sha256']:
            raise RuntimeError('Reviewed source changed: ' + item['Path'])
    otp, binary = args.otp_source.resolve(), args.otp_bin.resolve()
    if not binary.is_relative_to(otp):
        raise RuntimeError('Built emulator/compiler must belong to the pinned source tree.')
    head = subprocess.check_output(['git', '-C', str(otp), 'rev-parse', 'HEAD'], text=True).strip()
    if head != PIN or subprocess.check_output(['git', '-C', str(otp), 'status', '--porcelain', '--untracked-files=no'], text=True).strip():
        raise RuntimeError('Original source must have the exact clean tracked pin.')
    for item in plan['PinnedSources']:
        raw = subprocess.check_output(['git', '-C', str(otp), 'show', PIN + ':' + item['GitPath']])
        if sha(raw) != item['Sha256']:
            raise RuntimeError('Pinned original reference source differs.')
    erl, erlc = binary / 'erl', binary / 'erlc'
    if not erl.is_file() or not erlc.is_file():
        raise RuntimeError('Build exact pinned OTP first; collector downloads/installs nothing.')
    output = args.output.resolve()
    if output.exists():
        raise RuntimeError('Fresh evidence directory required.')
    output.mkdir(parents=True)
    OUTPUT = output
    for item in plan['Sources']:
        destination = output / 'sources' / item['Path']
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source / item['Path'], destination)
    (output / 'source-plan.json').write_bytes(plan_bytes)
    environment = dict(os.environ)
    for name in ('ERL_AFLAGS', 'ERL_FLAGS', 'ERL_ZFLAGS'):
        environment.pop(name, None)
    environment['ERLC_USE_SERVER'] = 'false'
    environment['PYTHONDONTWRITEBYTECODE'] = '1'
    environment.pop('PYTHONOPTIMIZE', None)
    (output / 'observer-ebin').mkdir()
    execute([str(erlc), '-Werror', '+debug_info', '-o', str(output / 'observer-ebin'), str(output / 'sources' / (OBSERVER + '.erl'))], output, environment, output / 'compile-observer')
    base = [str(erl), '+S', '1:1', '+SDcpu', '1', '+SDio', '1', '+A', '1', '-noshell', '-noinput', '-pa', str(output / 'observer-ebin')]
    environment['RAW_ETF'] = str(output / 'metadata.etf')
    evaluation = 'Result=' + OBSERVER + ':metadata(),ok=file:write_file(os:getenv("RAW_ETF"),term_to_binary(Result)),io:format("metadata_saved.~n"),halt().'
    execute(base + ['-eval', evaluation], output, environment, output / 'metadata')
    evaluation = 'Result=beam_disasm:file("observer-ebin/' + OBSERVER + '.beam"),ok=file:write_file("disassembly.etf",term_to_binary(Result)),io:format("disassembly_saved.~n"),halt().'
    execute(base + ['-eval', evaluation], output, environment, output / 'disassembly')
    for case in plan['Cases']:
        directory = output / case['Name']
        directory.mkdir()
        environment['RAW_ETF'] = str(directory / 'result.etf')
        evaluation = 'Result=' + OBSERVER + ':run(' + case['Name'] + '),ok=file:write_file(os:getenv("RAW_ETF"),term_to_binary(Result)),io:format("observation_saved.~n"),halt().'
        execute(base + ['-eval', evaluation], output, environment, directory / 'observation')
        raw_path = directory / 'result.etf'
        if not 2 <= raw_path.stat().st_size <= MAX_ETF_BYTES:
            raise RuntimeError('Original raw ETF file budget: ' + case['Name'])
        raw = raw_path.read_bytes()
        if raw[0] != 131:
            raise RuntimeError('Missing raw original ETF case: ' + case['Name'])
        COMPLETED.append(case['Name'])
    receipt = {'Status': 'ORIGINAL_RAW_BASE18_PLUS_FULLINFO_COLLECTED_INDEPENDENT_TYPED_REVIEW_REQUIRED',
               'Complete': True, 'PinnedCommit': PIN, 'SourcePlanSha256': sha(plan_bytes),
               'OrderedFreshVmCases': COMPLETED, 'ExpectedFreshVmCases': 19,
               'SemanticAssertionsExecuted': False, 'SdkExecuted': False, 'SdkCompared': False,
               'NativeFixtureUsed': False, 'Exclusions': [],
               'Tools': [{'Path': str(path), 'Sha256': sha(path.read_bytes())} for path in (erl, erlc)],
               'Files': members(output)}
    (output / 'receipt.json').write_bytes((json.dumps(receipt, indent=2) + '\n').encode('utf-8'))
    print(json.dumps({'Complete': True, 'FreshVmCases': len(COMPLETED), 'SemanticAssertionsExecuted': False, 'SdkCompared': False}))


def members(root):
    return [{'Path': path.relative_to(root).as_posix(), 'Bytes': path.stat().st_size, 'Sha256': sha(path.read_bytes())}
            for path in sorted(root.rglob('*')) if path.is_file()]


if __name__ == '__main__':
    try:
        main()
    except BaseException as error:
        failure = {'Complete': False, 'ErrorType': type(error).__name__, 'Error': str(error),
                   'PinnedCommit': PIN, 'OrderedFreshVmCases': COMPLETED, 'ExpectedFreshVmCases': 19,
                   'SemanticAssertionsExecuted': False, 'SdkCompared': False, 'NativeFixtureUsed': False}
        if OUTPUT is not None:
            failure['Files'] = members(OUTPUT)
            (OUTPUT / 'failure-receipt.json').write_bytes((json.dumps(failure, indent=2) + '\n').encode('utf-8'))
        print(json.dumps(failure), file=sys.stderr)
        raise
