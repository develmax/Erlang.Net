"""Build a target-native CLR launcher exporting the existing NIF bridge ABI."""
import argparse
import hashlib
import json
import platform
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--repository', type=Path, required=True)
parser.add_argument('--otp-include', type=Path, required=True)
parser.add_argument('--dotnet-root', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
root = args.repository.resolve()
output = args.output.resolve()
if platform.system() != 'Darwin' or output.exists() or not output.is_relative_to(root / 'artifacts'):
    parser.error('Run on macOS with a new output directory under repository/artifacts')
rid = 'osx-arm64' if platform.machine() == 'arm64' else 'osx-x64'
headers = list((args.dotnet_root / 'packs' / ('Microsoft.NETCore.App.Host.' + rid)).glob('*/runtimes/' + rid + '/native/hostfxr.h'))
if not headers:
    parser.error('Installed SDK hostfxr.h not found')
header = max(headers, key=lambda path: tuple(map(int, path.parents[3].name.split('.'))))
native = root / 'tools/Native/Erlang.NifNativeBackend/native'
sources = [native / name for name in ('nif_process_host.c', 'nif_bridge_exports.c', 'nif_bridge_varargs.c', 'nif_bridge_platform.c', 'nif_bridge_layout.c')]
output.mkdir(parents=True)
command = ['cc', '-std=c11', '-O2', '-fvisibility=hidden', '-Wall', '-Wextra', '-Werror', '-Wl,-export_dynamic', '-I' + str(args.otp_include.resolve()), '-I' + str(header.parent), *map(str, sources), '-o', str(output / 'erlang-nif-host')]
subprocess.run(command, check=True)
receipt = {'Host': platform.platform(), 'Command': command, 'Sources': {str(path.relative_to(root)): hashlib.sha256(path.read_bytes()).hexdigest() for path in sources}, 'HostfxrHeaderSha256': hashlib.sha256(header.read_bytes()).hexdigest(), 'Executed': False}
(output / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
