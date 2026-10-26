import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import sys

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
OBSERVED_PLAN = 'B565EA20C36B326D98CE9F854556C432142F0E074D33077679F6BF482CDBCC54'
TOOL = 'tools/Runtime/Erlang.AllocatorMemoryObservations'
WORKFLOW = '.github/workflows/allocator-memory-original-observations.yml'
BASE_HELPER = 'tools/Native/Erlang.NifPendingVarargsObservations/verify-original13.py'
BASE_SHA = '46F8837A41981C15DD6D49E3F16CFE06ABB2A8DE2B404BBCF84D9B93A248150F'
PLAN_NAME = 'reviewed-execution-plan.json'
STACK_HELPER = 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/verify-original9.py'
DECODER_HELPER = 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/passive_etf.py'
HEADERS = [
    ('erl_nif.h', 'erts/emulator/beam/erl_nif.h', '6CB96C37D306A173EF71946E823829FC21E47CB4A19288B221129EAC2F13B689'),
    ('erl_drv_nif.h', 'erts/emulator/beam/erl_drv_nif.h', 'E9D4280779E8FD4AC463144F310B4DA2346DD3A18E73D445BF1F88AE3C1F1312'),
    ('erl_nif_api_funcs.h', 'erts/emulator/beam/erl_nif_api_funcs.h', 'CF34ADA521AEB0EE00C9936320CD694F9816EDBAB960500453051CEA443DDA11'),
]


def require(value, message):
    if not value:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def child(root, name):
    relative = PurePosixPath(name)
    require(type(name) is str and not relative.is_absolute() and '..' not in relative.parts and '\\' not in name, 'Unsafe source path')
    path = (root / name).resolve()
    require(path.is_relative_to(root) and path.is_file(), 'Missing or escaping source: ' + name)
    return path


def record(root, name):
    path = child(root, name)
    return {'Path': name, 'Bytes': path.stat().st_size, 'Sha256': sha(path), 'HashMode': 'BinaryExact'}


def reviewed_paths(root):
    tool = root / TOOL
    paths = {path.relative_to(root).as_posix() for path in tool.rglob('*') if path.is_file() and path != tool / PLAN_NAME}
    require(not any({'__pycache__', 'bin', 'obj'}.intersection(PurePosixPath(path).parts) for path in paths), 'Unexpected generated source-directory content')
    return paths | {WORKFLOW, BASE_HELPER, STACK_HELPER, DECODER_HELPER, 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/check-passive-decoder.py', 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/check-stack-frames.py', 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/check-report-budget.py'}


def write_fresh(path, value):
    with path.open('xb') as output:
        output.write((json.dumps(value, indent=2) + '\n').encode())


def git(otp, *args):
    return subprocess.check_output(['git', '-C', str(otp), *args], timeout=30)


def verify_inputs(args):
    require(sys.flags.optimize == 0, 'Synthetic assertions require Python optimization disabled')
    root = args.repository.resolve()
    approved = args.approved_execution_plan_sha256.upper()
    require(re.fullmatch('[0-9A-F]{64}', approved), 'Explicit reviewed execution SHA required')
    plan_path = child(root, TOOL + '/' + PLAN_NAME)
    require(sha(plan_path) == approved, 'Reviewed execution plan hash drift')
    plan = json.loads(plan_path.read_bytes())
    require(plan['PinnedCommit'] == PIN and plan['ObservedPlanSha256'] == OBSERVED_PLAN and plan['OriginalExecuted'] is False and plan['SdkCompared'] is False, 'Execution plan scope drift')
    records = plan['Files']
    require(len(records) == len({row['Path'] for row in records}) and {row['Path'] for row in records} == reviewed_paths(root), 'Exact execution source closure drift')
    for row in records:
        require(record(root, row['Path']) == row, 'Execution source bytes drift: ' + row['Path'])
    require(sha(child(root, BASE_HELPER)) == BASE_SHA and sha(child(root, TOOL + '/original/source-plan.json')) == OBSERVED_PLAN, 'Frozen helper/observation plan drift')
    expected = os.environ.get('GITHUB_SHA', '')
    require(re.fullmatch('[0-9a-fA-F]{40}', expected) and git(root, 'rev-parse', 'HEAD').decode().strip() == expected, 'Actual CI checkout identity required')
    for key in ('GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT', 'GITHUB_JOB'):
        require(bool(os.environ.get(key)), 'Actual CI identity missing: ' + key)
    return root, plan_path


def provenance(root, otp, execution_plan):
    require(sys.platform == 'linux' and os.uname().machine == 'x86_64', 'Reviewed original requires Linux x86_64')
    require(git(otp, 'rev-parse', 'HEAD').decode().strip() == PIN and not git(otp, 'status', '--porcelain', '--untracked-files=no').strip(), 'Exact clean tracked OTP source required')
    source_plan = json.loads((root / TOOL / 'original/source-plan.json').read_bytes())
    references = []
    for row in source_plan['PinnedSources']:
        data = git(otp, 'show', PIN + ':' + row['GitPath'])
        require(len(data) == row['Bytes'] and hashlib.sha256(data).hexdigest().upper() == row['Sha256'], 'Pinned source blob drift')
        require((otp / row['GitPath']).read_bytes() == data, 'Actual original source differs')
        references.append(row)
    include = otp / 'observer-install/lib/erlang/usr/include'
    headers = []
    for name, git_path, expected in HEADERS:
        raw = git(otp, 'show', PIN + ':' + git_path)
        path = include / name
        require(path.is_file() and path.read_bytes() == raw and sha(path) == expected, 'Installed pinned public header drift: ' + name)
        headers.append({'Name': name, 'GitPath': git_path, 'Bytes': len(raw), 'Sha256': expected})
    configured = include / 'erl_int_sizes_config.h'
    require(configured.is_file() and configured.stat().st_size > 0, 'Actual configured size header required')
    paths = ['observer-install/bin/erl', 'observer-install/bin/erlc', 'observer-install/lib/erlang/erts-17.1/bin/beam.smp', 'observer-install/lib/erlang/erts-17.1/bin/erlexec']
    files = [record(otp, name) for name in paths]
    return {'SourceHead': PIN, 'ConfiguredPrefix': str(otp / 'observer-install'), 'OriginalCasesExecuted': False, 'NativeFixtureUsed': False,
            'HarnessCommit': os.environ['GITHUB_SHA'], 'RunId': os.environ['GITHUB_RUN_ID'], 'RunAttempt': os.environ['GITHUB_RUN_ATTEMPT'], 'Job': os.environ['GITHUB_JOB'],
            'ExecutionPlanSha256': sha(execution_plan), 'Files': files, 'PinnedReferenceSources': references, 'InstalledPinnedPublicHeaders': headers,
            'ConfiguredSizeHeader': record(otp, configured.relative_to(otp).as_posix()), 'CiIdentityNetworkAuthenticated': False}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('phase', choices=['verify', 'provenance', 'synthetic', 'collect', 'guard'])
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--approved-execution-plan-sha256', required=True)
    parser.add_argument('--otp-source', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root, execution_plan = verify_inputs(args)
    output = args.output.resolve()
    require(not output.is_relative_to(root / TOOL), 'Evidence must remain outside reviewed source closure')
    tool = root / TOOL
    environment = dict(os.environ)
    environment.pop('PYTHONOPTIMIZE', None)
    environment['PYTHONDONTWRITEBYTECODE'] = '1'
    for name in ('ERL_AFLAGS', 'ERL_FLAGS', 'ERL_ZFLAGS'):
        environment.pop(name, None)
    def execute(script, *argv, timeout=120):
        subprocess.run([sys.executable, str(script), *(str(arg) for arg in argv)], cwd=root, env=environment, check=True, timeout=timeout)
    if args.phase == 'verify':
        require(not output.exists(), 'Fresh execution evidence required')
        output.mkdir(parents=True)
        write_fresh(output / 'reviewed-inputs.json', {'ExecutionPlanSha256': sha(execution_plan), 'ObservedPlanSha256': OBSERVED_PLAN, 'HarnessCommit': os.environ['GITHUB_SHA'], 'OriginalExecuted': False, 'SdkCompared': False})
        return
    require(output.is_dir(), 'Preflight evidence directory required')
    preflight = json.loads((output / 'reviewed-inputs.json').read_bytes())
    require(preflight['ExecutionPlanSha256'] == sha(execution_plan) and preflight['HarnessCommit'] == os.environ['GITHUB_SHA'], 'Preflight source binding drift')
    if args.phase == 'synthetic':
        execute(tool / 'guard/check-projections.py', '--repository', root, '--report', output / 'synthetic-projections.json')
        execute(root / 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/check-passive-decoder.py', output / 'synthetic-decoder35.json')
        execute(root / 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/check-stack-frames.py', output / 'synthetic-stack16.json')
        execute(root / 'tools/Native/Erlang.RawOnLoadVisibilityObservations/guard/check-report-budget.py', output / 'synthetic-report-budget')
        result = json.loads((output / 'synthetic-projections.json').read_bytes())
        require(result['Passed'] is True and result['Count'] == 52 and result['OriginalExecuted'] is False and result['SdkCompared'] is False, 'Synthetic projection controls required')
        return
    require(args.otp_source is not None, 'Exact built OTP source required')
    otp = args.otp_source.resolve()
    actual = provenance(root, otp, execution_plan)
    receipt = output / 'build-provenance.json'
    if args.phase == 'provenance':
        write_fresh(receipt, actual)
        return
    require(json.loads(receipt.read_bytes()) == actual, 'Built tools/headers/source or CI identity changed after provenance')
    if args.phase == 'collect':
        execute(tool / 'original/check_process_capture.py', output / 'capture-controls')
        execute(tool / 'original/collect.py', '--otp-source', otp, '--otp-bin', otp / 'observer-install/bin', '--approved-plan-sha256', OBSERVED_PLAN, '--output', output / 'observations', timeout=2400)
        return
    execute(tool / 'guard/verify-original19.py', '--observations', output / 'observations', '--proposal', tool / 'original', '--build-provenance', receipt,
            '--base-guard', root / BASE_HELPER, '--stack-guard', root / STACK_HELPER, '--decoder', root / DECODER_HELPER,
            '--controls', output / 'capture-controls', '--report', output / 'original-nineteen-report.json',
            '--expected-head', os.environ['GITHUB_SHA'], '--expected-run', os.environ['GITHUB_RUN_ID'], '--expected-attempt', os.environ['GITHUB_RUN_ATTEMPT'], '--expected-job', os.environ['GITHUB_JOB'])


if __name__ == '__main__':
    main()
