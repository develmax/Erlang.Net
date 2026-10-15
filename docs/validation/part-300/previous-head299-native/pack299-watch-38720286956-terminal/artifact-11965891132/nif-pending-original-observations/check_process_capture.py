import importlib.util
import json
import os
import pathlib
import sys
import time


def main():
    if not sys.platform.startswith('linux') or len(sys.argv) != 2:
        raise RuntimeError('Pass one fresh output directory on Linux.')
    output = pathlib.Path(sys.argv[1]).resolve()
    output.mkdir(parents=True, exist_ok=False)
    source = pathlib.Path(__file__).resolve().with_name('collect.py')
    specification = importlib.util.spec_from_file_location('nif_observer_process_checks', source)
    collector = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(collector)
    cases = []
    try:
        prefix = output / 'raw-exit'
        command = [sys.executable, '-c', 'import sys;print("raw-out",flush=True);print("raw-err",file=sys.stderr,flush=True);sys.exit(3)']
        try:
            collector.execute(command, output, os.environ.copy(), prefix)
        except RuntimeError:
            pass
        else:
            raise AssertionError('Exit 3 was accepted.')
        raw = json.loads(prefix.with_suffix('.process.json').read_text())
        assert raw['ExitCode'] == 3 and not raw['TimedOut'] and raw['PipesClosed']
        assert prefix.with_suffix('.stdout').read_bytes() == b'raw-out\n'
        assert prefix.with_suffix('.stderr').read_bytes() == b'raw-err\n'
        cases.append('raw-exit-3-preserved')
        prefix = output / 'inherited-pipe'
        command = [sys.executable, '-c', 'import subprocess,sys;subprocess.Popen([sys.executable,"-c","import time;time.sleep(30)"]);print("parent-exited",flush=True)']
        started = time.monotonic()
        try:
            collector.execute(command, output, os.environ.copy(), prefix, timeout=0.5)
        except RuntimeError:
            pass
        else:
            raise AssertionError('Inherited pipe timeout was accepted.')
        elapsed = time.monotonic() - started
        raw = json.loads(prefix.with_suffix('.process.json').read_text())
        assert raw['TimedOut'] and raw['PipesClosed'] and elapsed < 15
        assert prefix.with_suffix('.stdout').read_bytes() == b'parent-exited\n'
        cases.append('owned-group-inherited-pipe-bounded')
        (output / 'receipt.json').write_text(json.dumps({'Passed': cases, 'InheritedPipeElapsedSeconds': elapsed, 'OriginalNifObserved': False}, indent=2) + '\n', encoding='utf-8')
    except BaseException as error:
        (output / 'failure-receipt.json').write_text(json.dumps({'Passed': cases, 'Complete': False, 'Error': str(error), 'OriginalNifObserved': False}, indent=2) + '\n', encoding='utf-8')
        raise


if __name__ == '__main__':
    main()
