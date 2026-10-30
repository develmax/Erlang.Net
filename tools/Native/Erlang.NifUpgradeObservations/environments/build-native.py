"""One fresh same-physical-DLL fixture. Builder NEVER executes target nif_init."""
import argparse,json,os,re,shutil,sys
from pathlib import Path
from common import *

def main():
    p=argparse.ArgumentParser()
    for key in ['repository','output']:p.add_argument('--'+key,type=Path,required=True)
    for key in ['expected-head','plan-sha256','admission-sha256']:p.add_argument('--'+key,required=True)
    for key in ['official-admission','build-receipt','otp-source']:p.add_argument('--'+key,type=Path)
    a=p.parse_args();repo=a.repository.resolve();head=select_head(repo,a.expected_head);packet=Path(__file__).resolve().parent
    linux=sys.platform.startswith('linux');need(os.name=='nt' or linux and os.uname().machine=='x86_64','Windows/Linux x64')
    need(sys.maxsize>2**32,'64-bit builder');guard(repo,packet,a.plan_sha256,'build-inputs.json')
    need((a.official_admission is not None and a.build_receipt is None) if not linux else (a.official_admission is None and a.build_receipt is not None and a.otp_source is not None),'Explicit genuine original provider')
    out=fresh(repo,a.output);errors=[];image=None;before=None;after=None;sealed=[]
    try:
        provider=load('env709_provider',repo/PROVIDER)
        installed,_,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
        headers=out/'headers';headers.mkdir()
        recipe=json.loads((repo/'tools/Native/Erlang.NifFixtureHost/native/source-plan.json').read_bytes())
        for item in recipe['Headers']:
            if not item['OutputName'].endswith('.in'):shutil.copyfile(checked(installed/'usr/include'/item['OutputName'],item['Sha256']),headers/item['OutputName'])
        shutil.copyfile(installed/'usr/include/erl_int_sizes_config.h',headers/'erl_int_sizes_config.h')
        sizes=(headers/'erl_int_sizes_config.h').read_text()
        for key,value in dict(SIZEOF_CHAR=1,SIZEOF_SHORT=2,SIZEOF_INT=4,SIZEOF_LONG=8 if linux else 4,SIZEOF_LONG_LONG=8,SIZEOF_VOID_P=8).items():
            need(re.search(r'^#define\s+'+key+r'\s+'+str(value)+r'\s*$',sizes,re.M),'Generated ABI '+key)
        env=environment();extras=[]
        if linux:
            tools={name:shutil.which(name) for name in ['gcc','nm','readelf']};need(all(tools.values()),'Existing Linux build tools')
        else:
            vs=Path(os.environ.get('ProgramFiles(x86)','C:/Program Files (x86)'))/'Microsoft Visual Studio/Installer/vswhere.exe'
            import subprocess
            installation=subprocess.check_output([str(vs),'-latest','-products','*','-requires','Microsoft.VisualStudio.Component.VC.Tools.x86.x64','-property','installationPath'],text=True,timeout=30).strip()
            setup=Path(installation)/'VC/Auxiliary/Build/vcvars64.bat'
            need(setup.is_file() and not any(c in str(setup) for c in ['"','%','\r','\n']),'Reviewed vcvars path')
            configured=subprocess.run('cmd.exe /d /s /c ""'+str(setup)+'" >nul && set"',capture_output=True,timeout=60)
            (out/'vcvars.stdout').write_bytes(configured.stdout);(out/'vcvars.stderr').write_bytes(configured.stderr);need(configured.returncode==0,'vcvars preparation')
            for line in configured.stdout.decode('mbcs').splitlines():
                if '=' in line and not line.startswith('='):
                    key,value=line.split('=',1);env[key.upper()]=value
            tools={name:shutil.which(name,path=env.get('PATH')) for name in ['cl.exe','link.exe','dumpbin.exe']};need(all(tools.values()),'Existing Windows build tools');extras=[vs,setup]
        stage=stages(repo,linux,env)
        sealed=rows([packet/'build-inputs.json',packet/'fixtures/nif_env709.c',a.build_receipt if linux else a.official_admission,*headers.iterdir(),*[Path(v) for v in tools.values()],*extras]);save(out/'sealed-inputs.json',sealed)
        d=out/'single';d.mkdir();shutil.copyfile(packet/'fixtures/nif_env709.c',d/'fixture.c')
        library=d/('nif_env709.so' if linux else 'nif_env709.dll')
        if linux:
            cc=[tools['gcc'],'-std=c11','-D_GNU_SOURCE','-pthread','-O2','-Wall','-Wextra','-Werror','-I',str(headers)]
            stage([*cc,'-shared','-fPIC','fixture.c','-o',library],d,'compile')
            stage([tools['nm'],'-D','--defined-only','--format=posix',library],d,'exports')
            stage([tools['nm'],'-D','--undefined-only','--format=posix',library],d,'imports')
            stage([tools['readelf'],'-h','-Ws','-d',library],d,'elf')
            need({line.split()[0].split('@')[0] for line in (d/'exports.stdout').read_text().splitlines() if line.split()}=={'nif_init'},'Exact entry export')
            imports={line.split()[0].split('@')[0] for line in (d/'imports.stdout').read_text().splitlines() if line.split()}
            allowed={'enif_alloc','enif_free','enif_alloc_env','enif_free_env','enif_make_copy','enif_get_tuple','enif_get_string','enif_get_int','enif_priv_data','enif_make_int','enif_make_uint','enif_make_atom','enif_make_tuple','enif_make_tuple_from_array'}
            need({name for name in imports if name.startswith('enif_')}<=allowed,'Unexpected NIF import')
            raw=library.read_bytes();need(raw[:6]==b'\x7fELF\x02\x01' and int.from_bytes(raw[18:20],'little')==62,'ELF x64')
        else:
            cc=[tools['cl.exe'],'/nologo','/std:c11','/W4','/WX','/O2','/MD','/utf-8','/D_CRT_SECURE_NO_WARNINGS','/I'+str(headers)]
            stage([*cc,'/c','fixture.c','/Fofixture.obj'],d,'compile')
            stage([tools['link.exe'],'/NOLOGO','/DLL','/MACHINE:X64','/Brepro','/OUT:'+library.name,'fixture.obj','kernel32.lib'],d,'link')
            stage([tools['dumpbin.exe'],'/nologo','/exports',library.name],d,'exports')
            stage([tools['dumpbin.exe'],'/nologo','/headers',library.name],d,'pe-headers')
            stage([tools['dumpbin.exe'],'/nologo','/dependents',library.name],d,'dependencies')
            exports=re.findall(r'^\s+\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)\s*$',(d/'exports.stdout.log').read_text(encoding='mbcs'),re.M)
            need(exports==['nif_init'],'Exact entry export');need('8664 machine' in (d/'pe-headers.stdout.log').read_text(encoding='mbcs'),'PE x64')
        layout='#include "fixture.c"\n#include <stddef.h>\nint main(void){printf("%zu %zu %zu %zu %zu\\n",sizeof(ErlNifEntry),sizeof(ErlNifFunc),offsetof(ErlNifEntry,funcs),sizeof(void*),sizeof(ERL_NIF_TERM));return 0;}\n'
        (d/'layout.c').write_bytes(layout.encode())
        if linux:
            stage([*cc,'-ffunction-sections','-fdata-sections','layout.c','-Wl,--gc-sections','-o',d/'layout'],d,'layout-build');stage([d/'layout'],d,'layout-run');log=d/'layout-run.stdout'
        else:
            stage([*cc,'layout.c','/Folayout.obj','/Felayout.exe','/link','/MACHINE:X64','/Brepro','kernel32.lib'],d,'layout-build');stage([d/'layout.exe'],d,'layout-run');log=d/'layout-run.stdout.log'
        measured=[int(v) for v in log.read_text().split()];need(measured==[96,32,24,8,8],'Pinned physical ABI')
        need(sha(d/'fixture.c')==sha(packet/'fixtures/nif_env709.c'),'Fixture copy bytes')
        image=dict(Id='same_dll709',Library=row(library),Layout=measured,TargetNifInitializationExecuted=False)
    except Exception as error:errors.append(repr(error))
    finally:
        try:verify(sealed);guard(repo,packet,a.plan_sha256,'build-inputs.json')
        except Exception as error:errors.append('seal-after: '+repr(error))
        if after is not None:
            try:
                current=after();save(out/'provider-after.json',current);need(current==before['Inventory'],'Provider unchanged')
            except Exception as error:errors.append('provider-after: '+repr(error))
    result=dict(Status='ENV709_NATIVE_BUILD',Pin=PIN,SubjectHead=head,Platform='linux-x64-lp64' if linux else 'windows-x64-llp64',Passed=image is not None and not errors,Images=[] if image is None else [image],PlanSha256=a.plan_sha256.upper(),Errors=errors,TargetNifInitializationExecuted=False)
    save(out/'build-receipt.json',result);return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
