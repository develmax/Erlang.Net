import argparse
import hashlib
import json
import os
import pathlib
import re
import shutil
import subprocess
import sys

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
ROOT = pathlib.Path(__file__).resolve().parents[4]


def sha(data):
    return hashlib.sha256(data).hexdigest().upper()


def run(arguments, cwd, environment, output):
    result = subprocess.run(arguments, cwd=cwd, env=environment, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, check=False)
    output.write_bytes(result.stdout)
    if result.returncode != 0:
        raise RuntimeError(f'Command failed with exit {result.returncode}; inspect {output}')
    return result.stdout


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--plan', required=True, type=pathlib.Path)
    parser.add_argument('--approved-plan-sha256', required=True)
    parser.add_argument('--output', required=True, type=pathlib.Path)
    args = parser.parse_args()
    if os.name != 'nt' or sys.maxsize <= 2**32:
        raise RuntimeError('This reviewed recipe is Windows x64 only.')
    plan_bytes = args.plan.read_bytes()
    if sha(plan_bytes) != args.approved_plan_sha256.upper():
        raise RuntimeError('Reviewed plan SHA256 mismatch; no build was started.')
    plan = json.loads(plan_bytes)
    if plan['PinnedCommit'] != PIN or plan['Profile'] != 'Windows x64 LLP64':
        raise RuntimeError('Unreviewed pin or ABI profile.')
    if sha(pathlib.Path(__file__).read_bytes()) != plan['BuildScriptSha256']:
        raise RuntimeError('Build recipe changed after plan review.')
    approved_sources = {}
    for source in plan['Sources']:
        path = (ROOT / source['Path']).resolve()
        if not path.is_relative_to(ROOT / 'tools/Native/Erlang.NifFixtureHost/native'):
            raise RuntimeError('Fixture source is outside the reviewed native source directory.')
        data = path.read_bytes()
        if sha(data) != source['Sha256']:
            raise RuntimeError('Reviewed source SHA256 mismatch: ' + source['Path'])
        approved_sources[source['OutputName']] = data
    otp = ROOT.parent / 'otp'
    actual_pin = subprocess.check_output(['git', '-C', str(otp), 'rev-parse', 'OTP-29.1.1^{commit}'], text=True).strip()
    if actual_pin != PIN:
        raise RuntimeError('Pinned OTP tag commit mismatch.')
    headers = {}
    for header in plan['Headers']:
        data = subprocess.check_output(['git', '-C', str(otp), 'show', PIN + ':' + header['GitPath']])
        if sha(data) != header['Sha256']:
            raise RuntimeError('Pinned header SHA256 mismatch: ' + header['GitPath'])
        headers[header['OutputName']] = data
    template = headers['erl_int_sizes_config.h.in'].decode('utf-8')
    for name, value in plan['GeneratedSizes'].items():
        pattern = r'^#undef ' + re.escape(name) + r'$'
        template, count = re.subn(pattern, '#define ' + name + ' ' + str(value), template, flags=re.M)
        if count != 1:
            raise RuntimeError('Unexpected pinned integer size template: ' + name)
    if re.search(r'^#undef SIZEOF_', template, re.M):
        raise RuntimeError('Unsubstituted platform integer size.')
    headers['erl_int_sizes_config.h'] = template.encode('utf-8')
    output = args.output.resolve()
    if not output.is_relative_to(ROOT / 'artifacts') or output.exists():
        raise RuntimeError('Output must be a new directory below repository artifacts; existing paths are never overwritten.')
    vswhere = pathlib.Path(os.environ.get('ProgramFiles(x86)', 'C:/Program Files (x86)')) / 'Microsoft Visual Studio/Installer/vswhere.exe'
    installation = subprocess.check_output([str(vswhere), '-latest', '-products', '*', '-requires', 'Microsoft.VisualStudio.Component.VC.Tools.x86.x64', '-property', 'installationPath'], text=True).strip()
    vcvars = pathlib.Path(installation) / 'VC/Auxiliary/Build/vcvars64.bat'
    if not vcvars.is_file() or any(character in str(vcvars) for character in ['"', '\n', '\r', '%']):
        raise RuntimeError('No usable VC x64 environment script.')
    setup = subprocess.run('cmd.exe /d /s /c ""' + str(vcvars) + '" >nul && set"', stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
    environment = os.environ.copy()
    for line in setup.stdout.decode('mbcs', errors='strict').splitlines():
        if '=' in line and not line.startswith('='):
            key, value = line.split('=', 1)
            environment[key.upper()] = value
    tools = {name: shutil.which(name, path=environment.get('PATH')) for name in ['cl.exe', 'link.exe', 'dumpbin.exe']}
    if not all(tools.values()):
        raise RuntimeError('VC compiler, linker or dumpbin not found in configured environment.')
    output.mkdir(parents=True, exist_ok=False)
    (output / 'reviewed-plan.json').write_bytes(plan_bytes)
    (output / 'vcvars.stderr.log').write_bytes(setup.stderr)
    for name, data in (headers | approved_sources).items():
        (output / name).write_bytes(data)
    cl = tools['cl.exe']
    common = [cl, '/nologo', '/std:c11', '/W4', '/WX', '/O2', '/MD', '/utf-8', '/I' + str(output)]
    run(common + ['/c', 'fixture.c', '/Fofixture.obj'], output, environment, output / 'fixture-compile.log')
    run([tools['link.exe'], '/NOLOGO', '/DLL', '/MACHINE:X64', '/Brepro', '/OUT:fixture.dll', 'fixture.obj'], output, environment, output / 'fixture-link.log')
    run(common + ['layout.c', '/Folayout.obj', '/Felayout.exe', '/link', '/MACHINE:X64', '/Brepro'], output, environment, output / 'layout-build.log')
    observed = json.loads(run([str(output / 'layout.exe')], output, environment, output / 'layout.json'))
    expected = plan['ExpectedLayout']
    for key, value in expected.items():
        if observed[key] != value:
            raise RuntimeError('Compiled layout mismatch: ' + key)
    decl = (output / 'erl_nif_api_funcs.h').read_text(encoding='utf-8')
    names = re.findall(r'^ERL_NIF_API_FUNC_DECL\(\s*[^,]+,\s*(enif_\w+)\s*,', decl, re.M)
    if len(names) != 180 or len(set(names)) != 180:
        raise RuntimeError('Pinned Windows declaration table changed.')
    offsets = {name: index * 8 for index, name in enumerate(names)} | {'erts_alc_test': 1440, 'erts_internal_test_ptr': 1448}
    if observed['CallbackOffsets'] != offsets:
        raise RuntimeError('Compiled native callback offsets differ from all 180 pinned declarations.')
    exports = run([tools['dumpbin.exe'], '/nologo', '/exports', 'fixture.dll'], output, environment, output / 'fixture-exports.log').decode('mbcs')
    export_names = re.findall(r'^\s+\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)\s*$', exports, re.M)
    if sorted(export_names) != sorted(plan['ExpectedExports']):
        raise RuntimeError('Native fixture export set differs from reviewed plan: ' + repr(export_names))
    run([tools['dumpbin.exe'], '/nologo', '/dependents', 'fixture.dll'], output, environment, output / 'fixture-dependents.log')
    run([tools['dumpbin.exe'], '/nologo', '/headers', 'fixture.dll'], output, environment, output / 'fixture-pe-headers.log')
    for name, data in (headers | approved_sources).items():
        if (output / name).read_bytes() != data:
            raise RuntimeError('Hydrated source changed during compilation: ' + name)
    receipt = {
        'Status': 'Build/layout/export verified; independent review required before DLL is allowlisted',
        'PinnedCommit': PIN, 'ReviewedPlanSha256': sha(plan_bytes),
        'FixtureDllSha256': sha((output / 'fixture.dll').read_bytes()),
        'NativeFixtureBehaviorExecuted': False, 'LayoutExecutableExecuted': True,
        'NativeLayout': observed,
        'Tools': [{'Path': str(path), 'Sha256': sha(pathlib.Path(path).read_bytes())} for path in tools.values()],
        'VcvarsPath': str(vcvars), 'VcvarsSha256': sha(vcvars.read_bytes()),
        'Files': [{'Path': file.name, 'Bytes': file.stat().st_size, 'Sha256': sha(file.read_bytes())} for file in sorted(output.iterdir()) if file.is_file()],
        'TrustBoundary': 'DLL hash alone is not approval. Review this receipt, pinned source/header/probe provenance, native layout and exact exports independently first.',
    }
    (output / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({'Receipt': str(output / 'build-receipt.json'), 'FixtureDllSha256': receipt['FixtureDllSha256'], 'NativeFixtureBehaviorExecuted': False}))


if __name__ == '__main__':
    main()
