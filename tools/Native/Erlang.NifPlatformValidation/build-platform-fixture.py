"""Compile one C NIF for OTP and .NET, retain the original ETF observation."""
import argparse
import hashlib
import json
import platform
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--repository', type=Path, required=True)
parser.add_argument('--otp-root', type=Path, required=True)
parser.add_argument('--bridge', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.repository.resolve()
otp = args.otp_root.resolve()
out = args.output.resolve()
assert out.is_relative_to(root / 'artifacts') and not out.exists()
out.mkdir(parents=True)
source = root / 'tools/Native/Erlang.NifPlatformValidation'
include = otp / 'usr/include'
ebin = out / 'ebin'
ebin.mkdir()
subprocess.run([str(otp / 'bin/erlc'), '+debug_info', '-o', str(ebin), str(source / 'nif_platform_fixture.erl')], check=True)
commands = []
for profile in ('managed', 'original'):
    folder = out / profile
    folder.mkdir()
    library = folder / 'nif_platform_fixture.so'
    flags = ['-std=c11', '-fPIC', '-O2', '-Wall', '-Wextra', '-Werror', '-I' + str(include)]
    if platform.system() == 'Darwin':
        flags += ['-bundle']
        if profile == 'original':
            emulator = next(otp.glob('erts-*/bin/beam.smp'))
            flags += ['-bundle_loader', str(emulator)]
    else:
        flags += ['-shared']
    command = ['cc', *flags, str(source / 'nif_platform_fixture.c')]
    if profile == 'managed':
        command += [str(args.bridge.resolve()), '-Wl,-rpath,' + str(args.bridge.resolve().parent)]
    command += ['-o', str(library)]
    subprocess.run(command, check=True)
    commands.append(command)
original_stem = str(out / 'original/nif_platform_fixture')
etf = str(out / 'original.etf')
evaluation = ('ok=nif_platform_fixture:load(' + json.dumps(original_stem) + '),'
              'V={nif_platform_fixture:sum(19,23),nif_platform_fixture:tuple(),nif_platform_fixture:generation()},'
              'ok=file:write_file(' + json.dumps(etf) + ',term_to_binary(V)),halt().')
subprocess.run([str(otp / 'bin/erl'), '-noshell', '-pa', str(ebin), '-eval', evaluation], check=True, timeout=60)
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
receipt = {'Host': platform.platform(), 'Commands': commands, 'SourceSha256': sha(source / 'nif_platform_fixture.c'),
           'BeamSha256': sha(ebin / 'nif_platform_fixture.beam'), 'OriginalEtfSha256': sha(out / 'original.etf'),
           'OriginalExecuted': True, 'ManagedExecuted': False}
(out / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
