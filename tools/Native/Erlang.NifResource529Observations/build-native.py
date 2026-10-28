"""Windows x64 fixture recipe; a successful receipt is not execution permission."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys

PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
ROOT = Path(__file__).resolve().parents[3]

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--plan', required=True)
    parser.add_argument('--approved-plan-sha256', required=True)
    parser.add_argument('--output', required=True)
    parser.add_argument('--provider-admission', required=True)
    parser.add_argument('--variant', required=True)
    args = parser.parse_args()
    assert os.name == 'nt' and sys.maxsize > 2**32
    plan_path = Path(args.plan).resolve()
    assert sha(plan_path) == args.approved_plan_sha256.upper()
    plan = json.loads(plan_path.read_bytes())
    assert plan['PinnedCommit'] == PIN and plan['BuildScriptSha256'] == sha(__file__)
    assert args.variant in plan['Variants']
    variant = plan['Variants'][args.variant]
    flag = variant['Define']
    native, erlang = [(ROOT / variant[key]).resolve() for key in ['NativeSourcePath', 'ErlangSourcePath']]
    assert native.is_relative_to(Path(__file__).parent) and erlang.is_relative_to(Path(__file__).parent)
    assert sha(native) == variant['NativeSourceSha256'] and sha(erlang) == variant['ErlangSourceSha256']
    provider = json.loads(Path(args.provider_admission).read_bytes())
    assert provider['SourceCommit'] == PIN and provider['ReleaseTag'] == 'OTP-29.1.1'
    installed = Path(provider['InstalledOtpRoot']).resolve()
    headers = {}
    for row in plan['Headers']:
        data = (installed/'usr/include'/row['OutputName']).read_bytes()
        assert hashlib.sha256(data).hexdigest().upper() == row['Sha256']
        headers[row['OutputName']] = data
    out = Path(args.output).resolve()
    assert out.is_relative_to(ROOT/'artifacts/nif-resource529') and not out.exists()
    vswhere = Path(os.environ.get('ProgramFiles(x86)','C:/Program Files (x86)'))/'Microsoft Visual Studio/Installer/vswhere.exe'
    installation = subprocess.check_output([str(vswhere),'-latest','-products','*','-requires','Microsoft.VisualStudio.Component.VC.Tools.x86.x64','-property','installationPath'],text=True,timeout=30).strip()
    setup = Path(installation)/'VC/Auxiliary/Build/vcvars64.bat'
    assert setup.is_file() and not any(c in str(setup) for c in ['"','%','\r','\n'])
    configured = subprocess.run('cmd.exe /d /s /c ""'+str(setup)+'" >nul && set"',capture_output=True,check=True,timeout=60)
    env = os.environ.copy()
    for line in configured.stdout.decode('mbcs').splitlines():
        if '=' in line and not line.startswith('='):
            key,value = line.split('=',1); env[key.upper()] = value
    binaries = {key:shutil.which(key,path=env.get('PATH')) for key in ['cl.exe','link.exe','dumpbin.exe']}
    assert all(binaries.values())
    out.mkdir(parents=True)
    for name,data in headers.items(): (out/name).write_bytes(data)
    shutil.copyfile(native,out/'fixture.c')
    shutil.copyfile(erlang,out/'fixture.erl')
    shutil.copyfile(plan_path,out/'source-plan.json')
    assert sha(out/'fixture.c') == variant['NativeSourceSha256']
    assert sha(out/'fixture.erl') == variant['ErlangSourceSha256']
    commands = []
    def run(command, leaf):
        commands.append({"Phase": leaf, "Command": command})
        result = subprocess.run(command,cwd=out,env=env,capture_output=True,timeout=120)
        (out/(leaf+'.stdout')).write_bytes(result.stdout)
        (out/(leaf+'.stderr')).write_bytes(result.stderr)
        assert result.returncode == 0, (leaf,result.returncode)
        return result.stdout
    run([binaries['cl.exe'],'/nologo','/std:c11','/W4','/WX','/O2','/MD','/utf-8','/I'+str(out),'/DRESOURCE_VERSION'+variant['VersionSuffix']+'='+flag,'/D_CRT_SECURE_NO_WARNINGS','/c','fixture.c','/Fofixture.obj'],'compile')
    run([binaries['link.exe'],'/NOLOGO','/DLL','/MACHINE:X64','/Brepro','/OUT:resource529.dll','fixture.obj','kernel32.lib'],'link')
    exports = run([binaries['dumpbin.exe'],'/nologo','/exports','resource529.dll'],'exports').decode('mbcs')
    names = re.findall(r'^\s+\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)\s*$',exports,re.M)
    assert names == ['nif_init'], names
    run([binaries['dumpbin.exe'],'/nologo','/dependents','resource529.dll'],'dependencies')
    assert sha(native) == variant['NativeSourceSha256'] and sha(erlang) == variant['ErlangSourceSha256']
    assert sha(out/'fixture.c') == variant['NativeSourceSha256']
    assert sha(out/'fixture.erl') == variant['ErlangSourceSha256']
    receipt = {'Commands':commands,'Variant':args.variant,'VersionDefine':flag,'VariantValue':plan['Variants'][args.variant]['Value'],'Status':'RESOURCE529_WINDOWS_BUILD_COMPLETE','PinnedCommit':PIN,'PlanSha256':sha(plan_path),'LibrarySha256':sha(out/'resource529.dll'),'NativeSourceSha256':sha(native),'ErlangSourceSha256':sha(erlang),'CompileExitCode':0,'LinkExitCode':0,'ExpectedExports':names,'Tools':[{ 'Path':path,'Sha256':sha(path)} for path in binaries.values()],'Files':[{ 'Path':p.name,'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted(out.iterdir()) if p.is_file()],'ExecutionApproved':False,'NativeFixtureExecuted':False,'CompiledSourceSha256':sha(out/'fixture.c'),'CopiedErlangSourceSha256':sha(out/'fixture.erl')}
    (out/'build-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
    print(json.dumps({'Receipt':str(out/'build-receipt.json'),'Sha256':sha(out/'build-receipt.json'),'ExecutionApproved':False}))

if __name__ == '__main__': main()
