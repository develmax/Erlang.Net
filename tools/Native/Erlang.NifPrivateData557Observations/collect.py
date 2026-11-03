"""One genuine load callback acquisition, then four independent managed routes."""
import argparse,json,os,subprocess,sys
from pathlib import Path
from evidence import *
sys.dont_write_bytecode=True
ROOT=Path(__file__).resolve().parents[3]
TOOL=Path(__file__).resolve().parent

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    for n in ['official-admission','output']:parser.add_argument('--'+n,type=Path,required=True)
    for n in ['admission-sha256','plan-sha256','expected-head']:parser.add_argument('--'+n,required=True)
    parser.add_argument('--execute-original',action='store_true')
    parser.add_argument('--with-managed',action='store_true')
    a=parser.parse_args()
    require(not a.with_managed or a.execute_original,'Managed execution requires genuine original acquisition')
    require(os.name=='nt','Pinned MSVC original profile requires Windows')
    require(subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD'],timeout=30).decode().strip()==a.expected_head,'Exact source head differs')
    paths=package(ROOT,TOOL,a.plan_sha256)
    out=fresh_output(ROOT,a.output)
    save(out/'launch-plan.json',{'SourcePlanSha256':a.plan_sha256.upper(),'ExpectedHead':a.expected_head,'ExecuteOriginal':a.execute_original,'ManagedRequested':a.with_managed,'WholeContractsClosed':0})
    if not a.execute_original:return 0
    errors=[];phases={};matched=[];before=None;provider_after=None;sealed=None
    owned=load('private557_owned',ROOT/OWNED)
    env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',PYTHONOPTIMIZE='0')
    for key in ['ERL_FLAGS','ERL_AFLAGS','ERL_ZFLAGS','ERL_LIBS','ERL_COMPILER_OPTIONS','ERLC_EMULATOR','ERLC_USE_SERVER','ERLC_SERVER_ID','ERLC_CONFIGURATION','ERL_ROOTDIR']:
        env.pop(key,None)
    def phase(name,command):
        directory=out/(name+'-process');directory.mkdir()
        result=None;failures=[]
        save(directory/'command.json',{'Command':command,'WorkingDirectory':str(ROOT)})
        try:
            result=owned.owned(command,directory,name,600,env,str(ROOT))
            require(process_ok(result),'Owned phase did not settle successfully')
        except Exception as exc:failures.append(error(name,exc))
        if result is None and (directory/(name+'.exit.json')).is_file():
            try:result=json.loads((directory/(name+'.exit.json')).read_bytes())
            except Exception as exc:failures.append(error(name+'-receipt',exc))
        phases[name]={'Passed':not failures,'Process':result,'Errors':failures}
        errors.extend(failures)
        return not failures
    original_passed=False
    try:
        helper=load('private557_provider',ROOT/'tools/Runtime/Erlang.OwnedGuest506Observations/collect.py')
        a.build_receipt=a.otp_source=None
        installed,erl,before,provider_after=helper.provider(a,ROOT)
        save(out/'provider-before.json',before)
        paths.extend([a.official_admission.resolve(),*[Path(r['Path']) for r in before['RelatedAdmissions']]])
        sealed=state_inputs(paths);save(out/'source-inputs.json',sealed)
        plan=TOOL/'source-plan.json'
        built=phase('build',[sys.executable,'-B',str(TOOL/'build-native.py'),'--plan',str(plan),'--approved-plan-sha256',a.plan_sha256,'--output',str(out/'build'),'--provider-admission',str(a.official_admission.resolve())])
        if built:
            library=out/'build/load_private557.dll';build=out/'build/build-receipt.json'
            original_passed=phase('original',[sys.executable,'-B',str(TOOL/'collect-original.py'),'--repository',str(ROOT),'--output',str(out/'original-acquisition'),'--official-admission',str(a.official_admission.resolve()),'--library',str(library),'--build-receipt',str(build),'--source-plan-sha256',a.plan_sha256,'--approved-official-admission-sha256',a.admission_sha256,'--approved-library-sha256',sha(library),'--approved-build-receipt-sha256',sha(build)])
            if original_passed:
                completion=out/'original-acquisition/completion.json'
                original_sha=sha(completion)
                original_admission(completion,original_sha)
                if a.with_managed:
                    host=ROOT/'tools/Native/Erlang.NifNativeBackend/bin/Debug/net10.0/Erlang.NifNativeBackend.dll'
                    snapshot=phase('host-snapshot',[sys.executable,'-B',str(TOOL/'snapshot-managed.py'),'--managed-host',str(host),'--output',str(out/'host-closure.json')])
                    if snapshot:
                        phase('managed',[sys.executable,'-B',str(TOOL/'run-managed.py'),'--repository',str(ROOT),'--output',str(out/'managed'),'--original-completion',str(completion),'--managed-host',str(host),'--managed-closure',str(out/'host-closure.json'),'--source-plan-sha256',a.plan_sha256,'--approved-original-completion-sha256',original_sha,'--approved-managed-closure-sha256',sha(out/'host-closure.json')])
                        managed=out/'managed/completion.json'
                        if managed.is_file():
                            receipt=json.loads(managed.read_bytes())
                            matched=[row['Profile']+'-'+row['Route'] for row in receipt['Outcomes'] if row['Passed']]
                            require(receipt['Passed'] and receipt['AllIndependentRoutesAttempted'] and receipt['AllIndependentRoutesExecuted'] and len(matched)==4,'Four real independent routes and cleanup required')
                original_admission(completion,original_sha)
    except Exception as exc:errors.append(error('batch',exc))
    finally:
        if sealed is not None:
            try:verify_rows(sealed)
            except Exception as exc:errors.append(error('sealed-inputs-after',exc))
        try:package(ROOT,TOOL,a.plan_sha256)
        except Exception as exc:errors.append(error('source-plan-after',exc))
        try:require(subprocess.check_output(['git','-C',str(ROOT),'rev-parse','HEAD'],timeout=30).decode().strip()==a.expected_head,'Source head changed')
        except Exception as exc:errors.append(error('source-head-after',exc))
        if provider_after is not None:
            try:
                after=provider_after();save(out/'provider-after.json',after)
                require(after==before['Inventory'],'Provider changed after complete batch')
            except Exception as exc:errors.append(error('provider-after',exc))
    original_executed=False
    original_receipt=out/'original-acquisition/completion.json'
    if original_receipt.is_file():
        try:
            original_result=json.loads(original_receipt.read_bytes())
            original_process=original_result.get('Process')
            original_executed=(original_result.get('OriginalExecuted') is True
                and isinstance(original_process,dict)
                and type(original_process.get('Pid')) is int and original_process['Pid']>0)
        except Exception as exc:errors.append(error('original-execution-receipt',exc))
    passed=not errors and original_passed and (not a.with_managed or len(matched)==4)
    try:save(out/'output-inventory.json',files(out,('completion.json','output-inventory.json')))
    except Exception as exc:errors.append(error('output-inventory',exc));passed=False
    save(out/'completion.json',{'Passed':passed,'SourcePlanSha256':a.plan_sha256.upper(),'ExpectedHead':a.expected_head,'OriginalExecuted':original_executed,'OriginalPassed':original_passed,'ManagedRequested':a.with_managed,'ExpectedManagedRoutes':4 if a.with_managed else 0,'MatchedManagedRoutes':matched,'Phases':phases,'Errors':errors,'ProviderFiles':len(before['Inventory']) if before else None,'NormalizationPerformed':False,'WholeContractsClosed':0,'OutputInventorySha256':sha(out/'output-inventory.json') if (out/'output-inventory.json').is_file() else None})
    print(json.dumps({'Passed':passed,'MatchedManagedRoutes':matched,'CompletionSha256':sha(out/'completion.json')}))
    return 0 if passed else 1

if __name__=='__main__':sys.exit(main())
