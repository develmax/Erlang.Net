"""Fresh original BEAM function admission and unmodified full-stack comparison."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import sys
import time

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def need(value, message):
    if not value:
        raise RuntimeError(message)


def save(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--official-admission', type=Path, required=True)
    parser.add_argument('--admission-sha256', required=True)
    parser.add_argument('--configuration', choices=['Debug', 'Release'], default='Debug')
    args = parser.parse_args()
    repo, tool, out = args.repository.resolve(), Path(__file__).resolve().parent, args.output.resolve()
    need(out.is_relative_to(repo / 'artifacts') and not out.exists(), 'Fresh confined output required')
    plan_bytes = (tool / 'inputs.json').read_bytes()
    plan = json.loads(plan_bytes)
    need(plan['Pin'] == PIN, 'Wrong original source pin')

    def guard():
        need((tool / 'inputs.json').read_bytes() == plan_bytes, 'Input plan changed')
        for row in plan['Inputs']:
            need(sha(repo / row['Path']) == row['Sha256'], 'Input changed: ' + row['Path'])

    guard()
    out.mkdir(parents=True)
    spec = importlib.util.spec_from_file_location('owned542', repo / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py')
    owned = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(owned)
    errors = []
    original_ok = False
    try:
        owned.owned([sys.executable, str(tool / 'capture-original.py'), '--repository', str(repo),
            '--output', str(out / 'original'), '--official-admission', str(args.official_admission),
            '--admission-sha256', args.admission_sha256], out, 'original-collector', 300,
            dict(os.environ, PYTHONDONTWRITEBYTECODE='1'), str(repo))
        original = json.loads((out / 'original/completion.json').read_bytes())
        need(original['CompilerPassed'] and not original['Errors']
            and len(original['Modes']) == 3 and all(x['PassedExecution'] for x in original['Modes']), 'Original failed')
        decoder_spec = importlib.util.spec_from_file_location('passive542', repo / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/passive_etf.py')
        decoder = importlib.util.module_from_spec(decoder_spec)
        decoder_spec.loader.exec_module(decoder)
        value_one = ('tuple', (('atom', 'value'), ('integer', 1)))
        for mode in ['complete', 'abort', 'reload']:
            envelope = decoder.decode_exact((out / 'original' / mode / 'observed.etf').read_bytes())
            need(envelope[0] == 'tuple' and len(envelope[1]) == 4, 'Invalid original envelope')
            row = envelope[1][1]
            need(row[0] == 'tuple' and len(row[1]) == 5, 'Invalid original row')
            items = row[1]
            need(items[0] == value_one and items[1] == ('atom', 'false')
                and items[2] == ('atom', 'false' if mode == 'abort' else 'true')
                and items[4] == ('integer', 1 if mode == 'reload' else 2), 'Original semantic result differs')
            if mode == 'complete':
                failure = items[3]
                need(failure[0] == 'tuple' and len(failure[1]) == 5
                    and failure[1][:2] == (('atom', 'exception'), ('atom', 'error'))
                    and failure[1][2][0] == 'tuple' and failure[1][2][1][0] == ('atom', 'badfun')
                    and failure[1][4] == ('atom', 'true'), 'Original badfun identity differs')
            else:
                need(items[3] == value_one, 'Original function result differs')
        original_ok = True
    except Exception as error:
        errors.append('original: ' + repr(error))

    host = repo / f'tests/Erlang.Tests/bin/{args.configuration}/net10.0/Erlang.Tests.dll'
    root = repo / 'artifacts/passive-fun647'
    previous = set(root.iterdir()) if root.exists() else set()
    binary = host.parent
    def inventory():
        return {p.name: sha(p) for p in binary.iterdir() if p.is_file()
            and (p.suffix == '.dll' or p.name.endswith(('.deps.json', '.runtimeconfig.json')))}
    before = inventory()
    managed_ok = False
    started = time.time_ns()
    try:
        command = ['dotnet', 'exec', '--fx-version', '10.0.11', str(host)]
        for name in plan['Cases']:
            command += ['--case', name]
        command += ['--report', str(out / 'managed-report.json')]
        owned.owned(command, out, 'managed', 120, dict(os.environ, DOTNET_PROCESSOR_COUNT='2'), str(repo))
        report = json.loads((out / 'managed-report.json').read_bytes())
        need(report['Passed'] == 3 and report['Failed'] == 0
            and {x['Name'] for x in report['Tests']} == set(plan['Cases']), 'Managed cases differ')
        managed_ok = True
    except Exception as error:
        errors.append('managed: ' + repr(error))
    ended = time.time_ns()
    captures = {}
    try:
        added = (set(root.iterdir()) if root.exists() else set()) - previous
        need(len(added) == 3, 'Expected exactly three fresh captures')
        destination = out / 'managed'
        destination.mkdir()
        for folder in added:
            need(re.fullmatch('[a-fA-F0-9]{32}', folder.name), 'Unexpected capture directory')
            receipt = json.loads((folder / 'receipt.json').read_bytes())
            mode = receipt['Mode']
            need(mode in ['complete', 'abort', 'reload'] and mode not in captures, 'Invalid/duplicate mode')
            cleanup = json.loads((folder / 'cleanup.json').read_bytes())
            need(cleanup['Completed'] and not cleanup['Errors'], 'Actor/VM cleanup failed')
            target = destination / mode
            target.mkdir()
            for path in folder.iterdir():
                if path.is_file():
                    need(started <= path.stat().st_mtime_ns <= ended, 'Capture outside owned window')
                    shutil.copyfile(path, target / path.name)
            captures[mode] = receipt
        need(inventory() == before, 'Managed DLL/config inventory changed')
    except Exception as error:
        errors.append('capture/cleanup: ' + repr(error))
    equal = {}
    if original_ok and managed_ok and len(captures) == 3:
        for mode in ['complete', 'abort', 'reload']:
            a, b = out / 'original' / mode, out / 'managed' / mode
            equal[mode + '/whole-stack'] = (a / 'stack.etf').read_bytes() == (b / 'stack.etf').read_bytes()
            if mode != 'complete':
                equal[mode + '/whole-outcome'] = (a / 'observed.etf').read_bytes() == (b / 'actual.etf').read_bytes()
        if not all(equal.values()):
            errors.append('Fresh complete raw comparison differs')
    try:
        guard()
    except Exception as error:
        errors.append('postflight: ' + repr(error))
    result = dict(Passed=original_ok and managed_ok and len(equal) == 5 and all(equal.values()) and not errors,
        Errors=errors, Equal=equal, Configuration=args.configuration, InputPlanSha256=hashlib.sha256(plan_bytes).hexdigest().upper(),
        ProtectedDllAndConfig=before, CleanupCompletedModes=sorted(captures),
        OpaqueFunctionIdsCompared=False, FunctionReasonIdentityCheckedInsideOriginalAndManagedWorker=True,
        StackFramesProjected=False, RawEtfRewritten=False, ApiDelta=0, FullContractDelta=0)
    save(out / 'completion.json', result)
    print(json.dumps({k: v for k, v in result.items() if k != 'ProtectedDllAndConfig'}))
    return 0 if result['Passed'] else 1


if __name__ == '__main__':
    raise SystemExit(main())
