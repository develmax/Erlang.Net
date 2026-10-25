"""Observe one owned OTP VM's configured atom-table termination boundary."""
import argparse
import importlib.util
import json
import os
import re
from pathlib import Path

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
CAPACITY = 16384


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--admission', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root, out = args.repository.resolve(), args.output.resolve()
    assert not out.exists() and out.is_relative_to(root / 'artifacts')
    helpers = root / 'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts'
    inventory = load('atom_inventory492', helpers / 'bounded_inventory.py')
    owned = load('atom_owned492', helpers / 'owned.py')
    admission = json.loads(args.admission.read_bytes())
    assert admission['SourceCommit'] == PIN and admission['ReleaseTag'] == 'OTP-29.1.1'
    installed = Path(admission['InstalledOtpRoot']).resolve()
    manifest = Path(admission['ExecutionManifestPath'])
    assert inventory.digest(manifest) == admission['ExecutionManifestSha256']
    expected = {r['Path'].replace('\\', '/'): r['Sha256'] for r in json.loads(manifest.read_bytes())}
    assert len(expected) == 6427 and inventory.inventory(installed) == expected
    out.mkdir(parents=True)
    expression = ('io:format("before=~p limit=~p~n",[erlang:system_info(atom_count),erlang:system_info(atom_limit)]),'
                  'try lists:foreach(fun(N)->list_to_atom("otp492cap_"++integer_to_list(N)) end,'
                  'lists:seq(1,' + str(CAPACITY) + ')) of _->io:format("completed~n") '
                  'catch C:R->io:format("caught=~p:~p~n",[C,R]) end,halt(0).')
    (out / 'expression.txt').write_text(expression + '\n', encoding='utf-8')
    environment = dict(os.environ)
    for name in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS', 'ERLC_EMULATOR']:
        environment.pop(name, None)
    environment['ERL_CRASH_DUMP'] = os.devnull
    command = [str(installed / 'bin/erl.exe'), '+S', '1:1', '+A', '1', '+t', str(CAPACITY),
               '-noshell', '-noinput', '-boot', 'no_dot_erlang', '-eval', expression]
    helper_error = None
    try:
        owned.owned(command, out, 'original', 40, environment, str(out))
    except RuntimeError as error:
        helper_error = str(error)
    after = inventory.inventory(installed) == expected
    receipt_path = out / 'original.exit.json'
    receipt = json.loads(receipt_path.read_bytes()) if receipt_path.exists() else None
    stdout = (out / 'original.stdout.log').read_text(encoding='utf-8', errors='replace')
    stderr = (out / 'original.stderr.log').read_text(encoding='utf-8', errors='replace')
    header = re.fullmatch(r'before=(\d+) limit=(\d+)\s*', stdout)
    observed = bool(receipt and helper_error and header and int(header[2]) == CAPACITY
                    and 0 < int(header[1]) < CAPACITY and receipt['Settled']
                    and receipt['ExitCode'] == 1 and not receipt['TimedOut'] and receipt['Failures'] == []
                    and ('no more index entries in atom_tab (max=' + str(CAPACITY) + ')') in stderr
                    and 'caught=' not in stdout and 'completed' not in stdout and after)
    result = {'ExpectedNativeFatalTerminationObserved': observed, 'OrdinarySuccessClaimed': False,
              'OwnedHelperCorrectlyRejectedNonzero': helper_error is not None, 'OwnedHelperError': helper_error,
              'OriginalReceipt': receipt, 'ProviderBeforeEqual': True, 'ProviderAfterEqual': after,
              'ProviderFiles': 6427, 'SourceCommit': PIN, 'ConfiguredOriginalCapacity': CAPACITY,
              'ManagedCapacityExecutionClaimed': False, 'CrashDumpArtifactCollected': False}
    (out / 'completion.json').write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    if not observed:
        raise RuntimeError('Expected owned OTP termination was not established; raw evidence retained')


if __name__ == '__main__':
    main()
