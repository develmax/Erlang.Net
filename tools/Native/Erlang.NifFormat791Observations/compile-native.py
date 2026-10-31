"""Genuine existing MSVC recipe, with owned compile/link/inspection stages."""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
from common import PACKAGE, sha, save, require, load, check, HELPER


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--plan', type=Path, required=True)
    parser.add_argument('--plan-sha256', required=True)
    parser.add_argument('--subject', required=True)
    parser.add_argument('--provider-root', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    require(os.name == 'nt' and sha(args.plan) == args.plan_sha256.upper(), 'Windows approved plan required')
    plan = json.loads(args.plan.read_bytes()); check(plan)
    repo = Path(plan['Repository']); helper = load(repo / HELPER, 'functional_build_owned')
    subject = next(row for row in plan['Subjects'] if row['Name'] == args.subject)
    require(re.fullmatch('[a-z][a-z0-9_]*', subject['Module']), 'Unsafe module stem')
    out = args.output.resolve()
    require(out.is_relative_to(repo / 'artifacts') and not out.exists(), 'Fresh contained native build required')
    out.mkdir(parents=True)
    for row in plan['Headers']:
        header = args.provider_root / 'usr/include' / row['OutputName']
        require(sha(header) == row['Sha256'], 'Admitted header bytes differ')
        shutil.copyfile(header, out / row['OutputName'])
    source = PACKAGE / subject['Native']; shutil.copyfile(source, out / 'fixture.c')
    vswhere = Path(os.environ.get('ProgramFiles(x86)', 'C:/Program Files (x86)')) / 'Microsoft Visual Studio/Installer/vswhere.exe'
    installation = subprocess.check_output([str(vswhere), '-latest', '-products', '*', '-requires',
        'Microsoft.VisualStudio.Component.VC.Tools.x86.x64', '-property', 'installationPath'], text=True, timeout=30).strip()
    setup = Path(installation) / 'VC/Auxiliary/Build/vcvars64.bat'
    require(setup.is_file() and not any(c in str(setup) for c in ['"', '%', '\r', '\n']), 'Invalid existing VS setup path')
    # Capture setup environment only in memory; do not persist potentially
    # sensitive inherited environment values into observation artifacts.
    configured = subprocess.run('cmd.exe /d /s /c ""' + str(setup) + '" >nul && set"', capture_output=True, check=True, timeout=60)
    env = dict(os.environ)
    for line in configured.stdout.decode('mbcs').splitlines():
        if '=' in line and not line.startswith('='):
            key, value = line.split('=', 1); env[key.upper()] = value
    env.pop('CL', None); env.pop('_CL_', None)
    tools = {name: shutil.which(name, path=env.get('PATH')) for name in ('cl.exe', 'link.exe', 'dumpbin.exe')}
    require(all(tools.values()), 'Existing MSVC compiler tools unavailable')
    stages = []
    def stage(name, command):
        directory = out / name; directory.mkdir()
        stages.append({'Name': name, 'Receipt': helper.execute(repo, command, directory, env)})
    object_file = out / 'fixture.obj'; library = out / (subject['Module'] + '.dll')
    stage('compile', [tools['cl.exe'], '/nologo', '/std:c11', '/W4', '/WX', '/O2', '/MD', '/utf-8',
                     '/I' + str(out), '/c', str(out / 'fixture.c'), '/Fo' + str(object_file)])
    stage('link', [tools['link.exe'], '/NOLOGO', '/DLL', '/MACHINE:X64', '/Brepro', '/OUT:' + str(library), str(object_file)])
    stage('exports', [tools['dumpbin.exe'], '/nologo', '/exports', str(library)])
    exports = (out / 'exports/original.stdout.log').read_bytes().decode('mbcs')
    names = re.findall(r'^\s+\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)\s*$', exports, re.M)
    require(names == ['nif_init'], 'Unexpected native fixture exports')
    stage('dependencies', [tools['dumpbin.exe'], '/nologo', '/dependents', str(library)])
    require(sha(source) == sha(out / 'fixture.c'), 'Compiled source copy differs')
    check(plan)
    receipt = {'Status': 'SOURCE_BOUND_FUNCTIONAL_NIF_WINDOWS_BUILD', 'Pin': plan['Pin'], 'Subject': subject['Name'],
               'PlanSha256': sha(args.plan), 'LibraryPath': str(library), 'LibrarySha256': sha(library),
               'NativeSourceSha256': sha(source), 'CompiledSourceSha256': sha(out / 'fixture.c'), 'Exports': names,
               'Tools': [{'Path': path, 'Sha256': sha(path)} for path in tools.values()], 'Stages': stages,
               'Headers': [{'Name': row['OutputName'], 'Sha256': sha(out / row['OutputName'])} for row in plan['Headers']],
               'NativeExecuted': False}
    save(out / 'build-receipt.json', receipt)
    print(json.dumps({'Receipt': str(out / 'build-receipt.json'), 'Sha256': sha(out / 'build-receipt.json')}))


if __name__ == '__main__':
    main()
