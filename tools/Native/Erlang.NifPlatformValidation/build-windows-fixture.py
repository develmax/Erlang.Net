"""Build the NIF image with actual target-measured Windows C scalar sizes."""
import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--repository', type=Path, required=True)
parser.add_argument('--otp-source', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--architecture', choices=('x64', 'arm64'), required=True)
args = parser.parse_args()
root = args.repository.resolve()
out = args.output.resolve()
assert os.name == 'nt' and out.is_relative_to(root / 'artifacts') and not out.exists()
pin = json.loads((root / 'reference-baseline.json').read_text())['commit']
otp = args.otp_source.resolve()
assert subprocess.check_output(['git', '-C', str(otp), 'rev-parse', 'HEAD'], text=True).strip() == pin
out.mkdir(parents=True)
for name in ('erl_nif.h', 'erl_nif_api_funcs.h', 'erl_drv_nif.h'):
    data = subprocess.check_output(['git', '-C', str(otp), 'show', pin + ':erts/emulator/beam/' + name])
    (out / name).write_bytes(data)
template = subprocess.check_output(['git', '-C', str(otp), 'show', pin + ':erts/include/erl_int_sizes_config.h.in']).decode()
vswhere = Path(os.environ['ProgramFiles(x86)']) / 'Microsoft Visual Studio/Installer/vswhere.exe'
installation = subprocess.check_output([str(vswhere), '-latest', '-products', '*', '-property', 'installationPath'], text=True).strip()
setup = Path(installation) / 'VC/Auxiliary/Build/vcvarsall.bat'
assert setup.is_file() and not any(v in str(setup) for v in ('"', '%', '\n', '\r'))
target = 'x64' if args.architecture == 'x64' else 'x64_arm64'
configured = subprocess.check_output('cmd.exe /d /s /c ""' + str(setup) + '" ' + target + ' >nul && set"')
env = dict(os.environ)
for line in configured.decode('mbcs').splitlines():
    if '=' in line and not line.startswith('='):
        key, value = line.split('=', 1)
        env[key.upper()] = value
compiler = shutil.which('cl.exe', path=env['PATH'])
linker = shutil.which('link.exe', path=env['PATH'])
assert compiler and linker
commands = []


def run(command, label):
    result = subprocess.run(command, cwd=out, env=env, capture_output=True, timeout=120)
    (out / (label + '.stdout')).write_bytes(result.stdout)
    (out / (label + '.stderr')).write_bytes(result.stderr)
    commands.append(command)
    if result.returncode:
        raise RuntimeError('Native fixture stage failed: ' + label)
    return result.stdout


probe = '#include <stdio.h>\nint main(void) {\n'
for name, native in [('CHAR', 'char'), ('SHORT', 'short'), ('INT', 'int'), ('LONG', 'long'), ('LONG_LONG', 'long long'), ('VOID_P', 'void *')]:
    probe += 'printf("SIZEOF_' + name + '=%zu\\n", sizeof(' + native + '));\n'
probe += 'return 0; }\n'
(out / 'sizes.c').write_bytes(probe.encode())
run([compiler, '/nologo', '/W4', '/WX', '/O2', '/MT', 'sizes.c', '/Fe:sizes.exe'], 'sizes-compile')
sizes = dict(line.split('=', 1) for line in run([str(out / 'sizes.exe')], 'sizes-execute').decode().splitlines())
assert sizes['SIZEOF_VOID_P'] == '8' and sizes['SIZEOF_LONG'] == '4'
for name, value in sizes.items():
    template, count = re.subn('^#undef ' + name + '$', '#define ' + name + ' ' + value, template, flags=re.M)
    assert count == 1
assert not re.search('^#undef SIZEOF_', template, re.M)
(out / 'erl_int_sizes_config.h').write_bytes(template.encode())
source = root / 'tools/Native/Erlang.NifPlatformValidation/nif_platform_fixture.c'
run([compiler, '/nologo', '/std:c11', '/W4', '/WX', '/O2', '/MT', '/utf-8', '/I' + str(out), '/c', str(source), '/Fofixture.obj'], 'fixture-compile')
run([linker, '/NOLOGO', '/DLL', '/MACHINE:' + args.architecture.upper(), '/OUT:nif_platform_fixture.dll', 'fixture.obj'], 'fixture-link')
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
receipt = {'TargetArchitecture': args.architecture, 'NativeSizesExecuted': True, 'Sizes': sizes, 'Commands': commands,
           'OtpPin': pin, 'SourceSha256': sha(source), 'LibrarySha256': sha(out / 'nif_platform_fixture.dll'),
           'Headers': {p.name: sha(p) for p in out.glob('*.h')}, 'NifImageExecuted': False}
(out / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
