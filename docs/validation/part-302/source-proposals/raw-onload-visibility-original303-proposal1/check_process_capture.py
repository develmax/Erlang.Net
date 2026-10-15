import json
import pathlib
import sys

sys.dont_write_bytecode = True
from process_capture import execute


root = pathlib.Path(sys.argv[1]).resolve()
if root.exists():
    raise RuntimeError('Capture controls require a fresh output directory.')
root.mkdir(parents=True)
environment = dict(__import__('os').environ)
environment['PYTHONDONTWRITEBYTECODE'] = '1'


def refused(program, prefix, timeout):
    try:
        execute([sys.executable, '-c', program], root, environment, root / prefix, timeout)
    except RuntimeError:
        return json.loads((root / (prefix + '.process.json')).read_bytes())
    raise RuntimeError('Intentional raw capture failure unexpectedly passed.')


failed = refused('import os;os.write(1,b"\\x00\\xffraw");os.write(2,b"\\xfeerr");raise SystemExit(3)', 'exit-three', 5)
if failed['ExitCode'] != 3 or failed['TimedOut'] or (root / 'exit-three.stdout').read_bytes() != b'\x00\xffraw' or (root / 'exit-three.stderr').read_bytes() != b'\xfeerr':
    raise RuntimeError('Non-UTF8 bytes or nonzero exit were lost.')
program = 'import os,subprocess,sys;subprocess.Popen([sys.executable,"-c","import time;time.sleep(50)"],close_fds=False);os.write(1,b"owned-child-pipes");raise SystemExit(0)'
inherited = refused(program, 'inherited-pipes', 1)
if inherited['TimedOut'] is not True or inherited['PipesClosed'] is not True or inherited['ParentReaped'] is not True or (root / 'inherited-pipes.stdout').read_bytes() != b'owned-child-pipes':
    raise RuntimeError('Owned inherited-pipe timeout was not bounded and recorded.')
(root / 'controls.json').write_bytes((json.dumps({'RawByteExitControlPassed': True, 'OwnedInheritedPipeControlPassed': True,
                                                'OriginalEmulatorExecuted': False, 'NativeFixtureUsed': False}, indent=2) + '\n').encode('utf-8'))
