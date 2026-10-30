"""Build three genuine N2 images; target nif_init is never called by this builder."""
import argparse,json,os,re,shutil,subprocess,sys
from pathlib import Path
from common import *

def main():
    parser=argparse.ArgumentParser()
    for name in ['repository','output']:parser.add_argument('--'+name,type=Path,required=True)
    for name in ['plan-sha256','admission-sha256','expected-head']:parser.add_argument('--'+name,required=True)
    for name in ['official-admission','build-receipt','otp-source']:parser.add_argument('--'+name,type=Path)
    a=parser.parse_args();repo=a.repository.resolve();HEAD=select_head(repo,a.expected_head);packet=Path(__file__).resolve().parent
    linux=sys.platform.startswith('linux');need((os.name=='nt' or linux and os.uname().machine=='x86_64') and sys.maxsize>2**32,'Windows or Linux x64 profile')
    need(a.expected_head==HEAD,'Explicit reviewed subject required');plan=guard(repo,packet,a.plan_sha256,'build-inputs.json')
    need((a.official_admission is not None and a.build_receipt is None) if not linux else (a.official_admission is None and a.build_receipt is not None and a.otp_source is not None),'Explicit original provider profile')
    provider=load('upgrade693_provider',repo/PROVIDER);out=fresh(repo,a.output);errors=[];images=[];before=None;after=None;sealed=None
    try:
        installed,_,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
        headers=out/'headers';headers.mkdir();recipe=json.loads((repo/'tools/Native/Erlang.NifFixtureHost/native/source-plan.json').read_bytes())
        for item in recipe['Headers']:
            if not item['OutputName'].endswith('.in'):shutil.copyfile(checked(installed/'usr/include'/item['OutputName'],item['Sha256']),headers/item['OutputName'])
        shutil.copyfile(installed/'usr/include/erl_int_sizes_config.h',headers/'erl_int_sizes_config.h')
        sizes=(headers/'erl_int_sizes_config.h').read_text();expected=dict(SIZEOF_CHAR=1,SIZEOF_SHORT=2,SIZEOF_INT=4,SIZEOF_LONG=8 if linux else 4,SIZEOF_LONG_LONG=8,SIZEOF_VOID_P=8)
        for name,value in expected.items():need(re.search(r'^#define\s+'+name+r'\s+'+str(value)+r'\s*$',sizes,re.M),'Generated ABI size '+name)
        env=environment();tools={};setup=None
        if linux:
            tools={name:shutil.which(name) for name in ['gcc','nm','readelf']};need(all(tools.values()),'Existing gcc/nm/readelf required')
        else:
            vs=Path(os.environ.get('ProgramFiles(x86)','C:/Program Files (x86)'))/'Microsoft Visual Studio/Installer/vswhere.exe'
            installation=subprocess.check_output([str(vs),'-latest','-products','*','-requires','Microsoft.VisualStudio.Component.VC.Tools.x86.x64','-property','installationPath'],text=True,timeout=30).strip()
            setup=Path(installation)/'VC/Auxiliary/Build/vcvars64.bat';need(setup.is_file() and not any(c in str(setup) for c in ['"','%','\r','\n']),'Reviewed vcvars path')
            configured=subprocess.run('cmd.exe /d /s /c ""'+str(setup)+'" >nul && set"',capture_output=True,timeout=60)
            (out/'vcvars.stdout').write_bytes(configured.stdout);(out/'vcvars.stderr').write_bytes(configured.stderr);need(configured.returncode==0,'vcvars setup failed')
            for line in configured.stdout.decode('mbcs').splitlines():
                if '=' in line and not line.startswith('='):
                    key,value=line.split('=',1);env[key.upper()]=value
            tools={name:shutil.which(name,path=env.get('PATH')) for name in ['cl.exe','link.exe','dumpbin.exe']};need(all(tools.values()),'Existing cl/link/dumpbin required')
        stage=stages(repo,linux,env)
        sealed=rows([packet/'build-inputs.json',packet/'native-images.json',packet/'fixtures/nif_upgrade693.c',a.build_receipt if linux else a.official_admission,*headers.iterdir(),*[Path(v) for v in tools.values()],*([setup,vs] if setup else [])])
        save(out/'sealed-inputs.json',sealed);save(out/'tools.json',rows(Path(v) for v in tools.values()))
        specs=json.loads((packet/'native-images.json').read_bytes());need([item['Id'] for item in specs]==['a42','b43','b43_no_upgrade'],'Exact three-image baseline')
        for spec in specs:
            directory=out/spec['Id'];directory.mkdir();failures=[]
            try:
                shutil.copyfile(packet/'fixtures/nif_upgrade693.c',directory/'fixture.c')
                definitions=[('CASE_VERSION',str(spec['Version'])),('WITH_RESOURCE','0')]+([('NO_UPGRADE','1')] if spec['NoUpgrade'] else [])
                suffix='.so' if linux else '.dll';library=directory/(spec['Id']+suffix)
                if linux:
                    cc=[tools['gcc'],'-std=c11','-O2','-Wall','-Wextra','-Werror','-I',str(headers),*['-D'+key+'='+value for key,value in definitions]]
                    stage([*cc,'-shared','-fPIC','fixture.c','-o',library],directory,'compile')
                    stage([tools['nm'],'-D','--defined-only','--format=posix',library],directory,'exports')
                    stage([tools['nm'],'-D','--undefined-only','--format=posix',library],directory,'imports')
                    stage([tools['readelf'],'-h','-Ws','-d',library],directory,'elf')
                    exported={line.split()[0].split('@')[0] for line in (directory/'exports.stdout').read_text().splitlines() if line.split()};need(exported=={'nif_init'},'Unexpected exported entry')
                    imports=sorted({line.split()[0].split('@')[0] for line in (directory/'imports.stdout').read_text().splitlines() if line.split()})
                    allowed={'enif_alloc','enif_free','enif_get_tuple','enif_get_string','enif_get_int','enif_priv_data','enif_make_int','enif_make_atom','enif_make_tuple','enif_make_tuple_from_array'}
                    need({name for name in imports if name.startswith('enif_')}<=allowed,'Unreviewed NIF callback import')
                    raw=library.read_bytes();need(raw[:6]==b'\x7fELF\x02\x01' and int.from_bytes(raw[18:20],'little')==62,'Wrong ELF machine')
                else:
                    cc=[tools['cl.exe'],'/nologo','/std:c11','/W4','/WX','/O2','/MD','/utf-8','/D_CRT_SECURE_NO_WARNINGS','/I'+str(headers),*['/D'+key+'='+value for key,value in definitions]]
                    stage([*cc,'/c','fixture.c','/Fofixture.obj'],directory,'compile')
                    stage([tools['link.exe'],'/NOLOGO','/DLL','/MACHINE:X64','/Brepro','/OUT:'+library.name,'fixture.obj'],directory,'link')
                    stage([tools['dumpbin.exe'],'/nologo','/exports',library.name],directory,'exports')
                    stage([tools['dumpbin.exe'],'/nologo','/headers',library.name],directory,'pe-headers')
                    stage([tools['dumpbin.exe'],'/nologo','/dependents',library.name],directory,'dependencies')
                    exports=re.findall(r'^\s+\d+\s+[0-9A-Fa-f]+\s+[0-9A-Fa-f]+\s+(\S+)\s*$',(directory/'exports.stdout.log').read_text(encoding='mbcs'),re.M)
                    need(exports==['nif_init'],'Unexpected exported entry');need('8664 machine' in (directory/'pe-headers.stdout.log').read_text(encoding='mbcs'),'Wrong PE machine');imports=[]
                layout='#include "fixture.c"\n#include <stddef.h>\nint main(void){printf("%zu %zu %zu %zu %zu\\n",sizeof(ErlNifEntry),sizeof(ErlNifFunc),offsetof(ErlNifEntry,funcs),sizeof(void*),sizeof(ERL_NIF_TERM));return 0;}\n'
                (directory/'layout.c').write_bytes(layout.encode())
                if linux:stage([*cc,'-ffunction-sections','-fdata-sections','layout.c','-Wl,--gc-sections','-o',directory/'layout'],directory,'layout-build');stage([directory/'layout'],directory,'layout-run');layout_log=directory/'layout-run.stdout'
                else:stage([*cc,'layout.c','/Folayout.obj','/Felayout.exe','/link','/MACHINE:X64','/Brepro'],directory,'layout-build');stage([directory/'layout.exe'],directory,'layout-run');layout_log=directory/'layout-run.stdout.log'
                measured=[int(v) for v in layout_log.read_text().split()];need(measured==[96,32,24,8,8],'Pinned current NIF header physical layout')
                need(sha(directory/'fixture.c')==sha(packet/'fixtures/nif_upgrade693.c'),'Copied fixture changed')
                images.append(dict(Id=spec['Id'],Library=row(library),Definitions=definitions,Layout=measured,Imports=imports,TargetNifInitializationExecuted=False))
            except Exception as error:failures.append(repr(error));errors.append(dict(Image=spec['Id'],Error=repr(error)))
            save(directory/'completion.json',dict(Passed=not failures,Errors=failures))
    except Exception as error:errors.append(dict(Phase='preparation',Error=repr(error)))
    finally:
        if sealed is not None:
            try:verify(sealed)
            except Exception as error:errors.append(dict(Phase='sealed-inputs-after',Error=repr(error)))
        if after is not None:
            try:current=after();save(out/'provider-after.json',current);need(current==before['Inventory'],'Provider changed')
            except Exception as error:errors.append(dict(Phase='provider-after',Error=repr(error)))
        try:guard(repo,packet,a.plan_sha256,'build-inputs.json')
        except Exception as error:errors.append(dict(Phase='source-after',Error=repr(error)))
    result=dict(Status='UPGRADE693_NATIVE_BUILD',Pin=PIN,SubjectHead=HEAD,Platform='linux-x64-lp64' if linux else 'windows-x64-llp64',Passed=len(images)==3 and not errors,ExpectedImages=3,Images=images,Errors=errors,PlanSha256=a.plan_sha256.upper(),TargetNifInitializationExecuted=False,ExecutionApproved=False)
    save(out/'build-receipt.json',result);return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
