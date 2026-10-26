"""Rebuild the existing ordinary Windows driver services from their unchanged C sources."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--headers', required=True)
    parser.add_argument('--output', required=True)
    args = parser.parse_args()
    source = Path(__file__).resolve().parent / 'native'
    output = Path(args.output).resolve(strict=True)
    manifest = json.loads((source / 'manifest.json').read_text(encoding='utf-8'))
    for name, expected in manifest['Files'].items():
        assert hashlib.sha256((source / name).read_bytes()).hexdigest().upper() == expected, name
    vswhere = Path(os.environ['ProgramFiles(x86)']) / 'Microsoft Visual Studio/Installer/vswhere.exe'
    install = subprocess.check_output([str(vswhere), '-latest', '-products', '*', '-requires',
                                      'Microsoft.VisualStudio.Component.VC.Tools.x86.x64',
                                      '-property', 'installationPath'], text=True).strip()
    vcvars = Path(install) / 'VC/Auxiliary/Build/vcvars64.bat'
    assert vcvars.is_file()
    # Capture the compiler environment privately; never print inherited variables.
    command = f'cmd.exe /d /s /c ""{vcvars}" >nul && set"'
    raw = subprocess.check_output(command, text=True)
    environment = os.environ.copy()
    for line in raw.splitlines():
        if '=' in line and not line.startswith('='):
            key, value = line.split('=', 1)
            environment[key] = value
    compiler = next(Path(directory) / 'cl.exe' for directory in environment['Path'].split(';')
                    if (Path(directory) / 'cl.exe').is_file())
    sources = [source / name for name in manifest['Files'] if name.endswith('.c')]
    arguments = [str(compiler), '/nologo', '/O2', '/MD', '/LD', '/I' + str(Path(args.headers).resolve(strict=True)),
                 '/Fo' + str(output) + os.sep] + [str(path) for path in sources]
    arguments += ['/link', '/OUT:' + str(output / 'erldrv-host-native.dll')]
    result = subprocess.run(arguments, cwd=output, env=environment, capture_output=True, text=True, timeout=60)
    (output / 'compile.stdout.txt').write_text(result.stdout, encoding='utf-8')
    (output / 'compile.stderr.txt').write_text(result.stderr, encoding='utf-8')
    receipt = {'ExitCode': result.returncode, 'Sources': manifest['Files'], 'Arguments': arguments}
    if result.returncode == 0:
        receipt['NativeSha256'] = hashlib.sha256((output / 'erldrv-host-native.dll').read_bytes()).hexdigest().upper()
    (output / 'compile.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    if result.returncode:
        raise RuntimeError('Ordinary driver bridge compilation failed; output retained')


if __name__ == '__main__':
    main()
