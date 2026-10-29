"""Pinned Linux x64 N1 fixture+bridge build; no target NIF initialization."""
import argparse,json,os,re,shutil,sys
from pathlib import Path
from common import *
def main():
    p=argparse.ArgumentParser()
    for n in ['repository','otp-source','build-receipt','output']:p.add_argument('--'+n,type=Path,required=True)
    for n in ['plan-sha256','admission-sha256','expected-head']:p.add_argument('--'+n,required=True)
    a=p.parse_args();repo=a.repository.resolve();tool=Path(__file__).resolve().parent
    need(sys.platform.startswith('linux') and os.uname().machine=='x86_64','Linux x64 required');plan=guard(repo,tool,a.plan_sha256,'linux-build-inputs.json')
    a.official_admission=None;provider=load('n1linux_provider',repo/PROVIDER);identity=load('n1linux_identity',repo/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py');owned=load('n1linux_owned',repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py');entry=load('n1linux_stages',repo/'tools/Native/Erlang.NifEntryOriginalObservations/common.py')
    out=fresh(repo,a.output);errors=[];images=[];bridge=None;before=None;after=None;sealed=None
    try:
        installed,_,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
        headers=out/'headers';headers.mkdir();recipe=json.loads((repo/'tools/Native/Erlang.NifFixtureHost/native/source-plan.json').read_bytes())
        for h in recipe['Headers']:
            if not h['OutputName'].endswith('.in'):shutil.copyfile(checked(installed/'usr/include'/h['OutputName'],h['Sha256']),headers/h['OutputName'])
        shutil.copyfile(installed/'usr/include/erl_int_sizes_config.h',headers/'erl_int_sizes_config.h')
        sizes=(headers/'erl_int_sizes_config.h').read_text()
        for name,value in dict(SIZEOF_CHAR=1,SIZEOF_SHORT=2,SIZEOF_INT=4,SIZEOF_LONG=8,SIZEOF_LONG_LONG=8,SIZEOF_VOID_P=8).items():need(re.search(r'^#define\s+'+name+r'\s+'+str(value)+r'\s*$',sizes,re.M),'Wrong LP64 '+name)
        tools={n:shutil.which(n) for n in ['gcc','nm','readelf']};need(all(tools.values()),'Existing Linux compiler/symbol tools required')
        env=environment()
        for name in ['LD_PRELOAD','LD_AUDIT','LD_LIBRARY_PATH','GITHUB_TOKEN','GH_TOKEN']:env.pop(name,None)
        sources=[repo/'tools/Native/Erlang.NifNativeBackend/native'/n for n in ['nif_bridge_exports.c','nif_bridge_varargs.c','nif_bridge_pthread_control.c']]
        sealed=snapshot([tool/'linux-build-inputs.json',tool/'cases.json',tool/'fixtures/version_entry651.c',a.build_receipt,*headers.iterdir(),*sources,*[Path(v) for v in tools.values()]])
        save(out/'sealed-inputs.json',sealed);save(out/'tools.json',[row(Path(v)) for v in tools.values()])
        def stage_(argv,d,name,timeout=90):return entry.stage(owned,identity,argv,d,env,d/name,timeout)
        stage_([tools['gcc'],'--version'],out,'compiler-version')
        bd=out/'bridge';bd.mkdir()
        try:
            library=bd/'Erlang.NifBridge.so'
            stage_([tools['gcc'],'-shared','-fPIC','-std=c11','-O2','-Wall','-Wextra','-Werror','-I',headers,*sources,'-o',library],bd,'compile')
            stage_([tools['nm'],'-D','--defined-only','--format=posix',library],bd,'exports')
            stage_([tools['readelf'],'-h','-Ws','-d',library],bd,'elf')
            exports={x.split()[0].split('@')[0] for x in (bd/'exports.stdout').read_text().splitlines() if x.split()}
            required={'nif_bridge_register','enif_release_resource','enif_keep_resource','enif_make_ref','enif_get_map_size','pthread_create','pthread_join','pthread_key_create','pthread_key_delete','pthread_getspecific','pthread_setspecific','__cxa_thread_atexit_impl','pthread_exit','nif_bridge_pthread_register','nif_bridge_pthread_provider','nif_bridge_pthread_calls'}
            need(required<=exports and not {'voidenif_release_resource','voidenif_keep_resource'}&exports,'Bridge exports differ')
            save(bd/'build-receipt.json',dict(Status='VERSION651_LINUX_BRIDGE_BUILD',Pin=PIN,SubjectHead=a.expected_head,BridgePath=str(library),BridgeSha256=sha(library),Sources=snapshot(sources),HeaderInputs=snapshot(headers.iterdir()),Exports=sorted(exports),NativeLoaded=False))
            bridge=dict(Library=row(library),BuildReceipt=row(bd/'build-receipt.json'))
        except Exception as error:errors.append(dict(Phase='bridge',Error=repr(error)))
        # Fixture builds remain independent of a failed bridge build; no NIF is executed.
        cases=json.loads((tool/'cases.json').read_bytes());need(len(cases)==42,'42 cases required')
        for case in cases:
            d=out/case['Id'];d.mkdir();failure=None
            try:
                shutil.copyfile(tool/'fixtures/version_entry651.c',d/'fixture.c');defs=['-DCASE_MAJOR='+str(case['Major']),'-DCASE_MINOR='+str(case['Minor']),'-DCASE_OPTIONS='+str(case.get('Options',0))]
                for key,macro in [('Variant','CASE_VARIANT'),('Module','CASE_MODULE'),('MinimumErts','CASE_MINIMUM_ERTS')]:
                    if key in case:defs.append('-D'+macro+'="'+case[key]+'"')
                cc=[tools['gcc'],'-std=c11','-O2','-Wall','-Wextra','-Werror','-I',headers,*defs];library=d/(case['Id']+'.so')
                stage_([*cc,'-shared','-fPIC','fixture.c','-o',library],d,'compile')
                stage_([tools['nm'],'-D','--defined-only','--format=posix',library],d,'defined')
                stage_([tools['nm'],'-D','--undefined-only','--format=posix',library],d,'undefined')
                stage_([tools['readelf'],'-h','-Ws','-d',library],d,'elf')
                defined={x.split()[0].split('@')[0] for x in (d/'defined.stdout').read_text().splitlines() if x.split()};need(defined=={'nif_init'},'Unexpected fixture exports')
                undefined={x.split()[0].split('@')[0] for x in (d/'undefined.stdout').read_text().splitlines() if x.split()};need(undefined<={'enif_make_int','__cxa_finalize','_ITM_registerTMCloneTable','_ITM_deregisterTMCloneTable','__gmon_start__'},'Unexpected fixture imports')
                raw=library.read_bytes();need(raw[:6]==b'\x7fELF\x02\x01' and int.from_bytes(raw[18:20],'little')==62,'Wrong ELF machine/class')
                (d/'layout.c').write_text('#include "fixture.c"\n#include <stdio.h>\nint main(void){printf("%zu %zu %zu\\n",sizeof(Entry651),sizeof(Function651),offsetof(Entry651,functions));return 0;}\n')
                # Layout executable uses only sizeof/offsetof; the linked NIF callbacks are never invoked.
                stage_([*cc,'-ffunction-sections','-fdata-sections','layout.c','-Wl,--gc-sections','-o',d/'layout'],d,'layout-build')
                stage_([d/'layout'],d,'layout-run');layout=[int(x) for x in (d/'layout-run.stdout').read_text().split()]
                minor=case['Minor'];major=case['Major'];size=96 if major>2 or minor>=14 else 88 if minor>=12 else 80 if minor>=7 else 72 if minor>=1 else 64
                need(layout==[size,32 if major>2 or minor>=7 else 24,24],'Physical short layout differs');need(sha(d/'fixture.c')==sha(tool/'fixtures/version_entry651.c'),'Copied fixture changed')
                images.append(dict(Id=case['Id'],Library=row(library),Layout=layout,Definitions=defs,Expected=case['Expected'],NativeInitializationExecuted=False))
            except Exception as error:failure=repr(error);errors.append(dict(Case=case['Id'],Error=failure))
            save(d/'completion.json',dict(Passed=failure is None,Error=failure))
    except Exception as error:errors.append(dict(Phase='prepare',Error=repr(error)))
    finally:
        if sealed is not None:
            try:verify_rows(sealed)
            except Exception as error:errors.append(dict(Phase='inputs-after',Error=repr(error)))
        if after is not None:
            try:
                current=after();save(out/'provider-after.json',current);need(current==before['Inventory'],'Provider changed')
            except Exception as error:errors.append(dict(Phase='provider-after',Error=repr(error)))
        try:guard(repo,tool,a.plan_sha256,'linux-build-inputs.json')
        except Exception as error:errors.append(dict(Phase='source-after',Error=repr(error)))
    result=dict(Status='VERSION651_LINUX_BUILD',Pin=PIN,SubjectHead=a.expected_head,Passed=len(images)==42 and bridge is not None and not errors,ExpectedCases=42,Images=images,Bridge=bridge,Errors=errors,PlanSha256=a.plan_sha256.upper(),ExecutionApproved=False,NativeInitializationExecuted=False,OriginalExecuted=False)
    save(out/'build-receipt.json',result);return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
