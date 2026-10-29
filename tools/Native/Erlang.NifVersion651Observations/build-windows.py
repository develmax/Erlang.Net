"""Reviewed Windows x64 version651 fixture build; no NIF initialization."""
import argparse,json,os,re,shutil,subprocess,sys
from pathlib import Path
from common import *
def main():
    p=argparse.ArgumentParser()
    for n in ['repository','official-admission','output']:p.add_argument('--'+n,type=Path,required=True)
    for n in ['plan-sha256','admission-sha256']:p.add_argument('--'+n,required=True)
    a=p.parse_args();repo=a.repository.resolve();tool=Path(__file__).resolve().parent
    need(os.name=='nt' and sys.maxsize>2**32,'Windows x64 only');plan=guard(repo,tool,a.plan_sha256,"build-inputs.json")
    a.build_receipt=a.otp_source=None;provider=load('v651_provider',repo/PROVIDER);owned=load('v651_owned',repo/OWNED)
    out=fresh(repo,a.output);errors=[];images=[];before=None;after=None;sealed=None
    try:
        installed,_,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
        headers=out/'headers';headers.mkdir()
        recipe=json.loads((repo/'tools/Native/Erlang.NifFixtureHost/native/source-plan.json').read_bytes())
        for h in recipe['Headers']:
            if h['OutputName'].endswith('.in'):continue
            shutil.copyfile(checked(installed/'usr/include'/h['OutputName'],h['Sha256']),headers/h['OutputName'])
        shutil.copyfile(installed/'usr/include/erl_int_sizes_config.h',headers/'erl_int_sizes_config.h')
        sizes=(headers/'erl_int_sizes_config.h').read_text()
        for name,value in recipe['GeneratedSizes'].items():need(re.search(r'^#define\s+'+name+r'\s+'+str(value)+r'\s*$',sizes,re.M),'Wrong LLP64 size '+name)
        vs=Path(os.environ.get('ProgramFiles(x86)','C:/Program Files (x86)'))/'Microsoft Visual Studio/Installer/vswhere.exe'
        installation=subprocess.check_output([str(vs),'-latest','-products','*','-requires','Microsoft.VisualStudio.Component.VC.Tools.x86.x64','-property','installationPath'],text=True,timeout=30).strip()
        setup=Path(installation)/'VC/Auxiliary/Build/vcvars64.bat';need(setup.is_file() and not any(c in str(setup) for c in ['"','%','\r','\n']),'Invalid vcvars')
        env=environment();command='cmd.exe /d /s /c ""'+str(setup)+'" >nul && set"'
        configured=subprocess.run(command,capture_output=True,timeout=60);(out/'vcvars.stdout').write_bytes(configured.stdout);(out/'vcvars.stderr').write_bytes(configured.stderr);need(configured.returncode==0,'vcvars failed')
        for line in configured.stdout.decode('mbcs').splitlines():
            if '=' in line and not line.startswith('='):
                k,v=line.split('=',1);env[k.upper()]=v
        tools={n:shutil.which(n,path=env.get('PATH')) for n in ['cl.exe','link.exe','dumpbin.exe']};need(all(tools.values()),'Missing VC tools')
        sealed=snapshot([tool/'build-inputs.json',tool/'cases.json',tool/'fixtures/version_entry651.c',a.official_admission,setup,*[Path(v) for v in tools.values()],*[q for q in headers.iterdir()]])
        save(out/'sealed-inputs.json',sealed);save(out/'tools.json',dict(Tools=[row(Path(v)) for v in tools.values()],Vcvars=row(setup),Vswhere=row(vs)))
        cases=json.loads((tool/'cases.json').read_bytes());need(len(cases)==42,'42 cases required')
        for case in cases:
            d=out/case['Id'];d.mkdir();failures=[]
            try:
                shutil.copyfile(tool/'fixtures/version_entry651.c',d/'fixture.c')
                defs=['/DCASE_MAJOR='+str(case['Major']),'/DCASE_MINOR='+str(case['Minor']),'/DCASE_OPTIONS='+str(case.get('Options',0))]
                for key,macro in [('Variant','CASE_VARIANT'),('Module','CASE_MODULE'),('MinimumErts','CASE_MINIMUM_ERTS')]:
                    if key in case:defs.append('/D'+macro+'="'+case[key]+'"')
                common=[tools['cl.exe'],'/nologo','/std:c11','/W4','/WX','/O2','/MD','/utf-8','/I'+str(headers),*defs]
                stage(owned,[*common,'/c','fixture.c','/Fofixture.obj'],d,'compile',env)
                stage(owned,[tools['link.exe'],'/NOLOGO','/DLL','/MACHINE:X64','/Brepro','/OUT:'+case['Id']+'.dll','fixture.obj'],d,'link',env)
                stage(owned,[tools['dumpbin.exe'],'/nologo','/exports',case['Id']+'.dll'],d,'exports',env)
                text=(d/'exports.stdout.log').read_text(encoding='mbcs');exports=re.findall(r'^\s+\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)\s*$',text,re.M);need(exports==['nif_init'],'Unexpected exports')
                stage(owned,[tools['dumpbin.exe'],'/nologo','/headers',case['Id']+'.dll'],d,'pe-headers',env)
                pe=(d/'pe-headers.stdout.log').read_text(encoding='mbcs');need('8664 machine' in pe,'PE machine differs')
                stage(owned,[tools['dumpbin.exe'],'/nologo','/dependents',case['Id']+'.dll'],d,'dependencies',env)
                layout='#include "fixture.c"\n#include <stdio.h>\nint main(void){printf("{\\"EntryBytes\\":%zu,\\"FunctionBytes\\":%zu,\\"FunctionOffset\\":%zu}\\n",sizeof(Entry651),sizeof(Function651),offsetof(Entry651,functions));return 0;}\n'
                # String escapes above generate an ordinary C printf JSON string.
                (d/'layout.c').write_text(layout)
                stage(owned,[*common,'layout.c','/Folayout.obj','/Felayout.exe','/link','/MACHINE:X64','/Brepro'],d,'layout-build',env)
                stage(owned,[d/'layout.exe'],d,'layout-run',env)
                observed=json.loads((d/'layout-run.stdout.log').read_text());minor=case['Minor'];major=case['Major']
                expected=96 if major>2 or minor>=14 else 88 if minor>=12 else 80 if minor>=7 else 72 if minor>=1 else 64
                need(observed==dict(EntryBytes=expected,FunctionBytes=32 if major>2 or minor>=7 else 24,FunctionOffset=24),'Physical version layout differs')
                need(sha(d/'fixture.c')==sha(tool/'fixtures/version_entry651.c'),'Copied C source changed')
                images.append(dict(Id=case['Id'],Library=row(d/(case['Id']+'.dll')),Layout=observed,Definitions=defs,Expected=case['Expected'],NativeInitializationExecuted=False))
            except Exception as error:failures.append(repr(error));errors.append(dict(Case=case['Id'],Error=repr(error)))
            save(d/'completion.json',dict(Passed=not failures,Errors=failures))
    except Exception as error:errors.append(dict(Phase='prepare',Error=repr(error)))
    finally:
        if sealed is not None:
            try:verify_rows(sealed)
            except Exception as error:errors.append(dict(Phase='sealed-inputs-after',Error=repr(error)))
        if after is not None:
            try:
                current=after();save(out/'provider-after.json',current);need(current==before['Inventory'],'Provider changed')
            except Exception as error:errors.append(dict(Phase='provider-after',Error=repr(error)))
        try:guard(repo,tool,a.plan_sha256,"build-inputs.json")
        except Exception as error:errors.append(dict(Phase='source-after',Error=repr(error)))
    result=dict(Status='VERSION651_WINDOWS_BUILD',Pin=PIN,Passed=len(images)==42 and not errors,ExpectedCases=42,Images=images,Errors=errors,PlanSha256=a.plan_sha256.upper(),NativeInitializationExecuted=False,ExecutionApproved=False,OriginalExecuted=False)
    save(out/'build-receipt.json',result);return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
