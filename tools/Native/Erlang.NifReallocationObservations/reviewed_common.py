import argparse
import hashlib
import importlib.util
import json
import os
import pathlib
import shutil
import subprocess
import sys

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
CASES = ['null_positive', 'null_zero', 'resize_sequence']


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def load_bound(path, expected, name):
    if sha(path.read_bytes()) != expected:
        raise RuntimeError('Bound helper changed: ' + str(path))
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def source_record(path, root, mode):
    data = path.read_bytes()
    if mode == 'TextLF':
        data = data.replace(b'\r\n', b'\n')
    elif mode != 'BinaryExact':
        raise RuntimeError('Unknown source hash mode')
    return {'Path': path.relative_to(root).as_posix(), 'Bytes': len(data), 'Sha256': sha(data), 'HashMode': mode}


def verified_plan(root, plan_path, approved):
    if sha(plan_path.read_bytes()) != approved.upper():
        raise RuntimeError('Reviewed source plan changed')
    plan = json.loads(plan_path.read_bytes())
    if plan['PinnedOtpCommit'] != PIN or plan['NativeExecuted'] is not False:
        raise RuntimeError('Source plan pin/scope changed')
    if len(plan['Files']) != len({item['Path'] for item in plan['Files']}):
        raise RuntimeError('Duplicate source plan members')
    for item in plan['Files']:
        path = (root / item['Path']).resolve()
        if not path.is_relative_to(root) or source_record(path, root, item['HashMode']) != item:
            raise RuntimeError('Reviewed source changed: ' + item['Path'])
    return plan


def bound_helpers(root, plan):
    return {item['Name']: load_bound(root / item['Path'], item['Sha256'], item['Name']) for item in plan['BoundHelpers']}


def clean_environment():
    env = dict(os.environ)
    for key in ('ERL_AFLAGS', 'ERL_FLAGS', 'ERL_ZFLAGS'):
        env.pop(key, None)
    env['ERLC_USE_SERVER'] = 'false'
    return env


def write_fresh(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)
        stream.write('\n')


def capture(executor, command, cwd, env, prefix, timeout=90):
    result = executor.execute([str(arg) for arg in command], cwd, env, prefix, timeout)
    if result.stderr:
        raise RuntimeError('Unexpected nonempty stderr: ' + str(prefix))
    return result


def verify_otp(root, include, plan):
    if subprocess.check_output(['git', '--no-optional-locks', '-C', str(root), 'rev-parse', 'HEAD'], text=True).strip() != PIN:
        raise RuntimeError('Actual OTP head differs')
    if subprocess.check_output(['git', '--no-optional-locks', '-C', str(root), 'status', '--porcelain', '--untracked-files=no'], text=True).strip():
        raise RuntimeError('OTP tracked source differs')
    if not include.is_relative_to(root):
        raise RuntimeError('Include must belong to pinned built source tree')
    for item in plan['Headers']:
        actual = (include / item['Name']).read_bytes()
        pinned = subprocess.check_output(['git', '--no-optional-locks', '-C', str(root), 'show', PIN + ':' + item['GitPath']])
        if actual != pinned or sha(actual) != item['Sha256']:
            raise RuntimeError('Installed pinned header mismatch')
    if not (include / 'erl_int_sizes_config.h').is_file():
        raise RuntimeError('Actual configured integer header required')


def exact_head(root, expected):
    actual = subprocess.check_output(['git', '--no-optional-locks', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
    if actual != expected or (os.environ.get('GITHUB_SHA') and actual != os.environ['GITHUB_SHA']):
        raise RuntimeError('Exact source head binding failed')
    return actual


def files_receipt(output):
    return [{'Path': path.relative_to(output).as_posix(), 'Bytes': path.stat().st_size, 'Sha256': sha(path.read_bytes())}
            for path in sorted(output.rglob('*')) if path.is_file()]
