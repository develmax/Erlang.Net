"""Build only the Windows runtime C varargs adapter; no VM or target NIF is run."""
import argparse
import hashlib
import json
import os
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
    args = parser.parse_args()
    root = args.repository.resolve()
    source = root / 'src/Runtime/Erlang.Runtime.Core/Native/Nif/native/windows_varargs526.c'
    assert os.name == 'nt' and sha(source) == args.source_sha256.upper()
    out = args.output.resolve()
    assert out.is_relative_to(root / 'artifacts') and (not out.exists())
    vswhere = Path(os.environ.get('ProgramFiles(x86)', 'C:/Program Files (x86)')) / 'Microsoft Visual Studio/Installer/vswhere.exe'
    installation = subprocess.check_output([str(vswhere), '-latest', '-products', '*', '-requires', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64', '-property', 'installationPath'], text=True, timeout=30).strip()
    setup = Path(installation) / 'VC/Auxiliary/Build/vcvars64.bat'
    assert setup.is_file() and (not any((c in str(setup) for c in ['"', '%', '\r', '\n'])))
    configured = subprocess.run('cmd.exe /d /s /c ""' + str(setup) + '" >nul && set"', capture_output=True, check=True, timeout=60)
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
    run([tools['cl.exe'], '/nologo', '/std:c11', '/W4', '/WX', '/O2', '/MT', '/utf-8', '/c', source.name, '/Foadapter.obj'], 'compile')
    run([tools['link.exe'], '/NOLOGO', '/DLL', '/MACHINE:X64', '/Brepro', '/OUT:Erlang.Nif.WindowsVariadic.dll', 'adapter.obj', 'kernel32.lib'], 'link')
    exports = run([tools['dumpbin.exe'], '/nologo', '/exports', 'Erlang.Nif.WindowsVariadic.dll'], 'exports').decode('mbcs')
    names = re.findall('^\\s+\\d+\\s+[0-9A-Fa-f]+\\s+[0-9A-Fa-f]+\\s+(\\S+)\\s*$', exports, re.M)
    assert sorted(names) == sorted(['nif_windows_varargs_register526', 'nif_windows_make_tuple526', 'nif_windows_make_list526'])
    run([tools['dumpbin.exe'], '/nologo', '/dependents', 'Erlang.Nif.WindowsVariadic.dll'], 'dependencies')
    assert sha(source) == args.source_sha256.upper()
    assert sha(out / source.name) == args.source_sha256.upper()
    receipt = {'Status': 'WINDOWS_VARARGS526_BUILD_COMPLETE', 'BuildScriptSha256': sha(__file__), 'SourceSha256': sha(source), 'CompiledSourceSha256': sha(out / source.name), 'AdapterSha256': sha(out / 'Erlang.Nif.WindowsVariadic.dll'), 'Commands': commands, 'Tools': [{'Path': path, 'Sha256': sha(path)} for path in tools.values()], 'VMExecuted': False, 'Files': [{'Path': p.name, 'Sha256': sha(p), 'Bytes': p.stat().st_size} for p in sorted(out.iterdir()) if p.is_file()]}
    (out / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'Receipt': str(out / 'build-receipt.json'), 'Sha256': sha(out / 'build-receipt.json')}))
if __name__ == '__main__':
    main()
