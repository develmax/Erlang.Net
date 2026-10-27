"""Four independent existing driver routes, gated by complete original V2 evidence."""
import argparse,json,os,sys
from pathlib import Path
from evidence import *
ap=argparse.ArgumentParser()
for n in ['repository','output','original-completion','managed-host','managed-closure']:ap.add_argument('--'+n,type=Path,required=True)
for n in ['source-plan-sha256','approved-original-completion-sha256','approved-managed-closure-sha256']:ap.add_argument('--'+n,required=True)
a=ap.parse_args();repo=a.repository.resolve();here=Path(__file__).resolve().parent
require(not a.output.resolve().is_relative_to(a.original_completion.resolve().parent) and not a.original_completion.resolve().parent.is_relative_to(a.output.resolve()),'Managed output cannot overlap original acquisition')
out=fresh_output(repo,a.output)
errors=[];outcomes=[];sealed=None;host=None;closure=None;host_rows=None
def host_inventory():
    rows=[]
    for p in sorted(host.parent.rglob('*')):
        require(not p.is_symlink(),'Host symlink not admitted')
        if p.is_file() and p.suffix in {'.dll','.json'}:rows.append(row(p))
    return rows
try:
    require(os.name=='nt','The admitted genuine DLL profile is Windows-only')
    package_paths=package(repo,here,a.source_plan_sha256)
    c,original_root,original_inputs=original_admission(a.original_completion,a.approved_original_completion_sha256)
    require(not out.is_relative_to(original_root) and not original_root.is_relative_to(out),'Managed outputs cannot overlap original acquisition')
    host=a.managed_host.resolve();closure=verified(a.managed_closure,a.approved_managed_closure_sha256);host_rows=json.loads(closure.read_bytes());verify_rows(host_rows)
    require(any(Path(r['Path']).resolve()==host for r in host_rows) and host.name=='Erlang.NifNativeBackend.dll','Actual approved native backend required')
    require(host_inventory()==host_rows,'Host closure has missing/additional/changed files')
    library=Path(c['NativeLibrary']['Path']);build_receipt=Path(c['BuildReceipt']['Path']);original=original_root/'original'
    images=[original/(n+'.beam') for n in ['load_private557','load_private_observer557']]
    sealed=state_inputs([*package_paths,*[Path(r['Path']) for r in original_inputs],closure,*[Path(r['Path']) for r in host_rows]])
    save(out/'sealed-inputs.json',sealed);save(out/'managed-before.json',host_rows)
    admission={'Status':'SOURCE_BOUND_ORIGINAL_DIRTY521_JOB','Pin':PIN,'Mode':'probe','SubjectKind':'GENUINE_LOAD_PRIVATE557','ObserverModule':'load_private_observer557','NifModule':'load_private557','NativeLibraryPath':str(library),'NativeLibrarySha256':sha(library),'BuildReceiptPath':str(build_receipt),'BuildReceiptSha256':sha(build_receipt),'Images':[{'Module':p.stem,'Path':str(p),'Sha256':sha(p)} for p in images],'Dependencies':['erlang','load_private557','load_private_observer557'],'ControlDependencies':[],'OriginalRawPath':str(original/'observed.etf'),'OriginalDeterministicPath':str(original/'observed-deterministic.etf'),'OriginalCleanupPath':str(original/'cleanup.etf'),'Inputs':[{'Path':r['Path'],'Sha256':r['Sha256']} for r in sealed]}
    adpath=out/'admission.json';save(adpath,admission);adsha=sha(adpath)
    owned=load('owned_managed557v2',repo/OWNED)
    for profile in ['ordinary','private-owned']:
        for route in ['ordinary','reviewed']:
            name=profile+'-'+route;directory=out/name;directory.mkdir();process=None;result=None;route_errors=[]
            command=['dotnet','exec','--fx-version','10.0.11',str(host),'--nif-dirty521-child',str(adpath),adsha,route,profile,str(directory/'actual')]
            try:
                verify_rows(sealed);verified(adpath,adsha)
                process=owned.owned(command,directory,name,120,dict(os.environ),str(repo))
            except Exception as exc:route_errors.append(error('execution',exc))
            if process is None and (directory/(name+'.exit.json')).is_file():
                try:process=json.loads((directory/(name+'.exit.json')).read_bytes())
                except Exception as exc:route_errors.append(error('exit-receipt',exc))
            try:
                actual=directory/'actual';result=json.loads((actual/'completion.json').read_bytes())
                require(process_ok(process),'Managed owned process failed')
                for key in ['Captured','ChildPassedFiniteRawComparison','RawWholeEqual','DeterministicWholeEqual','ExecutionAndCleanupPassed','NativeEnvironmentAndBinaryBaselinesRestored','NativeModuleAbsentBeforeAndAfter']:require(result.get(key) is True,'Managed finite gate failed: '+key)
                require(result.get('NormalizationPerformed') is False and result.get('Error') is None and result.get('AdmissionSha256')==adsha and result.get('Route')==route and result.get('OwnedProfile')==(profile=='private-owned'),'Managed receipt context differs')
                require((actual/'observed.etf').read_bytes()==(original/'observed.etf').read_bytes(),'Default whole ETF differs')
                require((actual/'observed-deterministic.etf').read_bytes()==(original/'observed-deterministic.etf').read_bytes(),'Deterministic whole ETF differs')
            except Exception as exc:route_errors.append(error('comparison-and-cleanup',exc))
            try:verify_rows(sealed);verified(adpath,adsha)
            except Exception as exc:route_errors.append(error('inputs-after-route',exc))
            outcomes.append({'Profile':profile,'Route':route,'Passed':not route_errors,'Process':process,'Actual':result,'Errors':route_errors})
except Exception as exc:errors.append(error('managed-preparation',exc))
finally:
    if sealed is not None:
        try:verify_rows(sealed)
        except Exception as exc:errors.append(error('sealed-inputs-after',exc))
    if host is not None and host_rows is not None:
        try:
            after=host_inventory();save(out/'managed-after.json',after);require(after==host_rows,'Actual managed closure changed')
        except Exception as exc:errors.append(error('managed-closure-after',exc))
    # Revalidate approved original reference and entire artifact set again;
    # no comparison run may mutate or silently remint original evidence.
    try:original_admission(a.original_completion,a.approved_original_completion_sha256)
    except Exception as exc:errors.append(error('original-admission-after',exc))
passed=len(outcomes)==4 and all(r['Passed'] for r in outcomes) and not errors
try:save(out/'output-inventory.json',files(out,('completion.json','output-inventory.json')))
except Exception as exc:errors.append(error('output-inventory',exc));passed=False
completion={'Status':'LOAD_PRIVATE557_MANAGED_V2','Pin':PIN,'Passed':passed,'PreparationAdmitted':len(outcomes)>0,'AllIndependentRoutesAttempted':len(outcomes)==4,'AllIndependentRoutesExecuted':len(outcomes)==4 and all(isinstance(r['Process'],dict) and type(r['Process'].get('Pid')) is int and r['Process']['Pid']>0 for r in outcomes),'OriginalCompletionSha256':a.approved_original_completion_sha256.upper(),'OriginalRewritten':False,'NormalizationPerformed':False,'WholeContractsClosed':0,'Errors':errors,'Outcomes':outcomes}
if (out/'output-inventory.json').is_file():completion['OutputInventory']={'Path':'output-inventory.json','Sha256':sha(out/'output-inventory.json')}
if (out/'sealed-inputs.json').is_file():completion['InputInventory']={'Path':'sealed-inputs.json','Sha256':sha(out/'sealed-inputs.json')}
save(out/'completion.json',completion);print(json.dumps({'Passed':passed,'Routes':len(outcomes),'CompletionSha256':sha(out/'completion.json')}));sys.exit(0 if passed else 1)
