"""Build only the Windows runtime C varargs adapter; no VM or target NIF is run."""
import argparse
import hashlib
import json
import os
import platform
import re
import shutil
import subprocess
from pathlib import Path

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--source-sha256', required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--architecture', choices=('x64', 'arm64', 'x86'), default='x64')
    parser.add_argument('--run-smoke', action='store_true', help='Execute native C varargs tests on the matching host')
    parser.add_argument('--compile-smoke', action='store_true', help='Compile native tests without target execution')
    args = parser.parse_args()
    root = args.repository.resolve()
    source = root / 'src/Runtime/Erlang.Runtime.Core/Native/Nif/native/windows_varargs526.c'
    assert os.name == 'nt' and sha(source) == args.source_sha256.upper()
    format_header = root / 'tools/Native/Erlang.NifNativeBackend/native/nif_snprintf791.h'
    assert format_header.is_file()
    format_header_sha = sha(format_header)
    out = args.output.resolve()
    assert out.is_relative_to(root / 'artifacts') and (not out.exists())
    vswhere = Path(os.environ.get('ProgramFiles(x86)', 'C:/Program Files (x86)')) / 'Microsoft Visual Studio/Installer/vswhere.exe'
    installation = subprocess.check_output([str(vswhere), '-latest', '-products', '*', '-requires', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64', '-property', 'installationPath'], text=True, timeout=30).strip()
    setup = Path(installation) / 'VC/Auxiliary/Build/vcvarsall.bat'
    assert setup.is_file() and (not any((c in str(setup) for c in ['"', '%', '\r', '\n'])))
    target = {'x64': 'x64', 'arm64': 'x64_arm64', 'x86': 'x64_x86'}[args.architecture]
    configured = subprocess.run('cmd.exe /d /s /c ""' + str(setup) + '" ' + target + ' >nul && set"', capture_output=True, check=True, timeout=60)
    env = dict(os.environ)
    for line in configured.stdout.decode('mbcs').splitlines():
        if '=' in line and (not line.startswith('=')):
            key, value = line.split('=', 1)
            env[key.upper()] = value
    tools = {name: shutil.which(name, path=env.get('PATH')) for name in ('cl.exe', 'link.exe', 'dumpbin.exe')}
    assert all(tools.values())
    out.mkdir(parents=True)
    shutil.copyfile(source, out / source.name)
    commands = []

    def run(command, name):
        result = subprocess.run(command, cwd=out, env=env, capture_output=True, timeout=120)
        (out / (name + '.stdout')).write_bytes(result.stdout)
        (out / (name + '.stderr')).write_bytes(result.stderr)
        commands.append({'Name': name, 'Command': command, 'ExitCode': result.returncode})
        assert result.returncode == 0, (name, result.returncode)
        return result.stdout
    run([tools['cl.exe'], '/nologo', '/std:c11', '/W4', '/WX', '/O2', '/MT', '/utf-8', '/I' + str(format_header.parent), '/c', source.name, '/Foadapter.obj'], 'compile')
    run([tools['link.exe'], '/NOLOGO', '/DLL', '/MACHINE:' + args.architecture.upper(), '/Brepro', '/OUT:Erlang.Nif.WindowsVariadic.dll', 'adapter.obj', 'kernel32.lib'], 'link')
    exports = run([tools['dumpbin.exe'], '/nologo', '/exports', 'Erlang.Nif.WindowsVariadic.dll'], 'exports').decode('mbcs')
    names = re.findall('^\\s+\\d+\\s+[0-9A-Fa-f]+\\s+[0-9A-Fa-f]+\\s+(\\S+)\\s*$', exports, re.M)
    assert sorted(names) == sorted(['nif_windows_varargs_register526', 'nif_windows_make_tuple526', 'nif_windows_make_list526', 'nif_windows_snprintf791', 'nif_windows_set_option'])
    run([tools['dumpbin.exe'], '/nologo', '/dependents', 'Erlang.Nif.WindowsVariadic.dll'], 'dependencies')
    run([tools['dumpbin.exe'], '/nologo', '/headers', 'Erlang.Nif.WindowsVariadic.dll'], 'headers')
    assert sha(source) == args.source_sha256.upper()
    assert sha(format_header) == format_header_sha
    assert sha(out / source.name) == args.source_sha256.upper()
    receipt = {'Status': 'WINDOWS_VARARGS526_BUILD_COMPLETE', 'BuildScriptSha256': sha(__file__), 'FormatHeaderPath': str(format_header), 'FormatHeaderSha256': format_header_sha, 'SourceSha256': sha(source), 'CompiledSourceSha256': sha(out / source.name), 'AdapterSha256': sha(out / 'Erlang.Nif.WindowsVariadic.dll'), 'Commands': commands, 'Tools': [{'Path': path, 'Sha256': sha(path)} for path in tools.values()], 'VMExecuted': False, 'Files': [{'Path': p.name, 'Sha256': sha(p), 'Bytes': p.stat().st_size} for p in sorted(out.iterdir()) if p.is_file()]}
    receipt['TargetArchitecture'] = args.architecture
    receipt['ExecutedOnTarget'] = False
    if args.run_smoke or args.compile_smoke:
        smoke = root / 'tools/Native/Erlang.NifPlatformValidation/windows_variadic_smoke.c'
        receipt['SmokeSourceSha256'] = sha(smoke)
        run([tools['cl.exe'], '/nologo', '/W4', '/WX', '/O2', '/MT', str(smoke), '/Fe:smoke.exe', '/link', '/LIBPATH:' + str(out), 'Erlang.Nif.WindowsVariadic.lib'], 'smoke-compile')
        probe = root / 'tools/Native/Erlang.NifPlatformValidation/windows_variadic_probe.c'
        run([tools['cl.exe'], '/nologo', '/std:c11', '/W4', '/WX', '/O2', '/MT', '/LD', str(probe), '/Fe:Erlang.Nif.VariadicProbe.dll', '/link', '/MACHINE:' + args.architecture.upper()], 'probe-compile')
        if args.run_smoke:
            host = platform.machine().lower()
            assert (args.architecture == 'x64' and host in ('amd64', 'x86_64')) or (args.architecture == 'arm64' and host in ('arm64', 'aarch64')) or (args.architecture == 'x86' and host in ('amd64', 'x86_64', 'x86', 'i386'))
            run([str(out / 'smoke.exe')], 'smoke-run')
            receipt['ExecutedOnTarget'] = True
    (out / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'Receipt': str(out / 'build-receipt.json'), 'Sha256': sha(out / 'build-receipt.json')}))
if __name__ == '__main__':
    main()
