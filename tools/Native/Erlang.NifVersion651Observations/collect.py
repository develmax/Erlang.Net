"""42 fresh original workers plus independent reviewed/ordinary original-BEAM replay. No builds."""
import argparse,json,os,shutil,sys
from pathlib import Path
from common import *
def assert_expected(term,case):
    A=lambda x:('atom',x)
    T=lambda *x:('tuple',tuple(x))
    value=lambda x:T(A('value'),x)
    need(term[0]=='tuple' and len(term[1])==3,'Observer requires complete three outcomes')
    load,first,second=term[1]
    if case['Expected'].startswith('success'):
        payload=T(A('payload'),('list',(('integer',17),('binary',bytes([1,2,3])))))
        need(term==T(value(A('ok')),value(('integer',42)),value(payload)),'Original success behavior differs from pinned fixture source')
        return
    native_text=lambda key,default: bytes(case[key]).decode('latin1') if key in case else default
    if case['Major']>2 or case['Minor']>18:
        reason="That 'nif_version651' NIF library needs "+native_text('MinimumErtsTextBytes','erts-version651')+" or newer. Either try to recompile the NIF lib or use a newer erts runtime."
    elif case['Major']<2 or case['Minor']==5:
        reason='That old NIF library ('+str(case['Major'])+'.'+str(case['Minor'])+') is not compatible with this erts runtime (2.18). Try recompile the NIF lib.'
    else:
        reason='Library ('+native_text('VariantTextBytes',case['Variant'])+') not compiled for this vm variant (beam.vanilla).'
    text=('list',tuple(('integer',ord(c)) for c in reason))
    need(load==value(T(A('error'),T(A('bad_lib'),text))),'Returned bad_lib text/precedence differs')
    for failed in [first,second]:
        need(failed[0]=='tuple' and len(failed[1])==4 and failed[1][0]==A('raised') and failed[1][1]==A('error') and failed[1][2]==A('not_loaded') and failed[1][3][0]=='list' and bool(failed[1][3][1]),'Failure replaced stub or lost complete stack')


def project_headers552(root, hash_file):
    paths=[root/'tools/Native/Erlang.NifNativeBackend/native'/name for name in ('nif_bridge_api.h','nif_snprintf791.h')]
    return [dict(Path=str(path.resolve()),Bytes=path.stat().st_size,Sha256=hash_file(path)) for path in paths]

def require_project_headers552(proof, root, hash_file):
    expected=project_headers552(root,hash_file);actual=proof['ProjectHeaderInputs']
    assert len(actual)==len(expected) and {row['Path']:(row['Bytes'],row['Sha256']) for row in actual}=={row['Path']:(row['Bytes'],row['Sha256']) for row in expected},'Project bridge headers changed or unbound'
    return expected

def main():
    p=argparse.ArgumentParser()
    for n in ['repository','native-build-receipt','managed-host','source-freeze','output']:p.add_argument('--'+n,type=Path,required=True)
    for n in ['plan-sha256','admission-sha256','approved-native-build-sha256','source-freeze-sha256']:p.add_argument('--'+n,required=True)
    for n in ['official-admission','build-receipt','otp-source']:p.add_argument('--'+n,type=Path)
    p.add_argument('--expected-head')
    p.add_argument('--framework-version',default='10.0.11');a=p.parse_args();repo=a.repository.resolve();tool=Path(__file__).resolve().parent
    linux=sys.platform.startswith('linux');need(os.name=='nt' or linux and os.uname().machine=='x86_64','Windows or Linux x64 only')
    input_name='linux-inputs.json' if linux else 'inputs.json';plan=guard(repo,tool,a.plan_sha256,input_name)
    need((a.official_admission is None and a.build_receipt is not None and a.otp_source is not None and a.expected_head) if linux else a.official_admission is not None,'Explicit original provider profile required')
    out=fresh(repo,a.output);errors=[];observations=[];before=None;after=None;sealed=None;compile_ready=False
    module690=dict(Passed=False,Count=4,ExpectedComparisons=8,Rows=[],Errors=[dict(Phase="not-started",Error="Module690 has not completed")])
    owned=load('v651_owned',repo/OWNED) if not linux else load('v651_linux_owned',repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/owned_process.py');decoder=load('v651_decoder',repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py')
    env=environment();provider=load('v651_provider',repo/PROVIDER)
    if linux:
        entry_stage=load('v651_linux_stage',repo/'tools/Native/Erlang.NifEntryOriginalObservations/common.py');identity=load('v651_linux_identity',repo/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py')
        env['ERLC_USE_SERVER']='false'
        for n in ['LD_PRELOAD','LD_AUDIT','LD_LIBRARY_PATH','DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS']:env.pop(n,None)
    cases=json.loads((tool/'cases.json').read_bytes());need(len(cases)==42,'42 cases required')
    def phase(argv,d,name,timeout=120):
        try:
            receipt=entry_stage.stage(owned,identity,argv,d,env,d/name,timeout) if linux else stage(owned,argv,d,name,env,timeout)
            return dict(Passed=True,Receipt=receipt)
        except Exception as error:
            item=dict(Passed=False,Error=repr(error));receipt=d/(name+('.process.json' if linux else '.exit.json'))
            if receipt.is_file():item['Receipt']=json.loads(receipt.read_bytes())
            errors.append(dict(Directory=str(d),Phase=name,Error=repr(error)));return item
    host=None;beams=[];images={};native=None;installed=None;erl=None;bridge_inputs=[];bridge_admission={}
    try:
        native=checked(a.native_build_receipt,a.approved_native_build_sha256);build=json.loads(native.read_bytes())
        need(build['Status']==('VERSION651_LINUX_BUILD' if linux else 'VERSION651_WINDOWS_BUILD') and build['Pin']==PIN and build['PlanSha256']==plan['BuildPlanSha256'],'Wrong native receipt')
        # Partial build failure remains failure, but successfully prepared independent cases are retained.
        if not build['Passed']:errors.append(dict(Phase='native-build',Error='Native build has incomplete cases or failures',Details=build['Errors']))
        for image in build['Images']:
            need(image['Id'] not in images,'Duplicate native image');r=image['Library'];checked(r['Path'],r['Sha256']);images[image['Id']]=image
        if linux:
            bridge=build['Bridge'];need(bridge is not None,'No successful admitted bridge build')
            for name in ['Library','BuildReceipt']:bridge_inputs.append(checked(bridge[name]['Path'],bridge[name]['Sha256']))
            proof=json.loads(bridge_inputs[1].read_bytes());need(proof['Status']=='VERSION651_LINUX_BRIDGE_BUILD' and proof['Pin']==PIN and proof['SubjectHead']==a.expected_head,'Bridge producer differs')
            need(proof['BridgePath']==str(bridge_inputs[0]) and proof['BridgeSha256']==sha(bridge_inputs[0]),'Bridge proof/library differs')
            project_header_rows=require_project_headers552(proof,repo,sha)
            bridge_inputs.extend(Path(row_['Path']) for row_ in project_header_rows)
            bridge_admission=dict(BridgePath=str(bridge_inputs[0]),BridgeSha256=sha(bridge_inputs[0]),BridgeBuildReceiptPath=str(bridge_inputs[1]),BridgeBuildReceiptSha256=sha(bridge_inputs[1]))
        checked(a.source_freeze,a.source_freeze_sha256);frozen=json.loads(a.source_freeze.read_bytes());need(bool(frozen['Rows']),'Empty source freeze')
        for row_ in frozen['Rows']:checked(repo/row_['Path'],row_['Sha256'])
        host=a.managed_host.resolve();need(host.is_file() and host.suffix=='.dll','Managed host absent')
        binary_inputs=[q for q in host.parent.iterdir() if q.is_file() and (q.suffix.lower()=='.dll' or q.name.endswith('.deps.json') or q.name.endswith('.runtimeconfig.json'))]
        sealed=snapshot([native,a.source_freeze,a.build_receipt if linux else a.official_admission,tool/input_name,tool/'cases.json',*bridge_inputs,*binary_inputs,*[Path(i['Library']['Path']) for i in images.values()]])
        save(out/'sealed-inputs.json',sealed)
    except Exception as error:errors.append(dict(Phase='shared-admission',Error=repr(error)));host=None
    if linux and host is not None:
        # Ordinary loading discovers the deployed bridge beside the application.
        # The VM namespace binds its real reference/environment callbacks; the
        # process-global registration table has no such per-VM authority.
        deployment=dict(Passed=False,ProcessGlobalRegistration=False)
        try:
            target=host.parent/'Erlang.NifBridge.so'
            need(not target.is_symlink(),'Ordinary bridge deployment cannot follow a symbolic link')
            if not target.exists():
                with target.open('xb') as destination:destination.write(bridge_inputs[0].read_bytes())
            checked(target,bridge_admission['BridgeSha256'])
            sealed.append(row(target))
            deployment.update(Passed=True,Source=row(bridge_inputs[0]),Deployed=row(target))
        except Exception as error:
            # Reviewed execution uses its explicitly admitted bridge path and
            # remains independent of this ordinary-only deployment preparation.
            deployment['Error']=repr(error)
            errors.append(dict(Phase='ordinary-bridge-deployment',Error=repr(error)))
        save(out/'ordinary-bridge-deployment.json',deployment)
    # Original provider/compilation is a real dependency of the identical-BEAM replay, not of native image admission.
    try:
        if not linux:a.build_receipt=a.otp_source=None
        installed,erl,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
        prep=out/'beam-preparation';prep.mkdir();need(all(ord(c)<128 for c in str(prep)+str(installed)),'Explicit nonASCII path protocol is not admitted')
        for name in ['nif_version651.erl','version_observer651.erl']:
            shutil.copyfile(tool/'fixtures'/name,prep/name);need(sha(prep/name)==sha(tool/'fixtures'/name),'Copied Erlang source differs')
        env['ERL_CRASH_DUMP']=str(prep/'erl_crash.dump')
        compiled=phase([installed/('bin/erlc' if linux else 'bin/erlc.exe'),'+debug_info','-o',str(prep),'nif_version651.erl','version_observer651.erl'],prep,'compile')
        if compiled['Passed']:
            beams=[row(prep/(name+'.beam')) for name in ['nif_version651','version_observer651']];compile_ready=True;save(prep/'compiled-images.json',beams)
        save(prep/'completion.json',dict(Passed=compile_ready,Compilation=compiled,Beams=beams,**bridge_admission))
    except Exception as error:errors.append(dict(Phase='provider-compile',Error=repr(error)))
    try:
        for case in cases:
            d=out/case['Id'];d.mkdir();entry=dict(Id=case['Id'],Expected=case['Expected'],Routes={},Comparisons={});image=images.get(case['Id'])
            if image is None or not compile_ready:
                entry['SkippedDependent']='Missing admitted image or original compiled BEAM preparation';observations.append(entry);save(d/'completion.json',entry);continue
            lib=Path(image['Library']['Path']);stem=lib.with_suffix('').as_posix();need(all(ord(c)<128 for c in stem),'Explicit nonASCII native path protocol is not admitted');original=d/'original';original.mkdir();env['ERL_CRASH_DUMP']=str(original/'erl_crash.dump')
            expression=('W=fun(N,V)->ok=file:write_file(N,term_to_binary(V,[{minor_version,2}])) end,'
                'Expected=filename:absname('+estr(installed)+'),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
                'W("profile.etf",{erlang:system_info(otp_release),erlang:system_info(version),erlang:system_info(system_architecture)}),'
                '[begin {module,M}=code:load_abs(filename:join('+estr(prep)+',atom_to_list(M))) end||M<-[nif_version651,version_observer651]],'
                'I=fun(M)->P=filename:absname(filename:join('+estr(prep)+',atom_to_list(M)++".beam")),P=filename:absname(code:which(M)),{ok,{M,D}}=beam_lib:md5(P),D=M:module_info(md5),{M,P,D,M:module_info(compile)} end,W("executing-owners.etf",[I(M)||M<-[nif_version651,version_observer651]]),'
                'Parent=self(),{Pid,Monitor}=spawn_monitor(version_observer651,entry,['+estr(stem)+',Parent]),'
                'receive {version651,T}->W("observed.etf",T) after 30000->erlang:error(version651_observation_timeout) end,'
                "receive {'DOWN',Monitor,process,Pid,normal}->ok;{'DOWN',Monitor,process,Pid,Reason}->erlang:error({version651_child,Reason}) after 30000->erlang:error(version651_down_timeout) end,"
                'W("cleanup.etf",{child_normal,not is_process_alive(Pid)}),halt(0).')
            (original/'invocation.eval').write_text(expression)
            entry['Routes']['original']=phase([erl,'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang','-pa',str(prep),*(['-start_epmd','false'] if linux else []),'-eval',expression],original,'observe',120)
            if (original/'observed.etf').is_file():
                try:assert_expected(decoder.decode_exact((original/'observed.etf').read_bytes()),case)
                except Exception as error:
                    entry['Routes']['original']['Passed']=False;errors.append(dict(Case=case['Id'],Route='original',Error=repr(error)))
            # Both managed routes run even if the original observation failed; compilation is the shared real dependency.
            for route in ['reviewed','ordinary']:
                rd=d/route;rd.mkdir()
                if host is None:
                    entry['Routes'][route]=dict(Passed=False,SkippedDependent='Managed host/source admission failed');continue
                admission=rd/'execution-admission.json';save(admission,dict(Status='APPROVED_VERSION651_EXECUTION',Pin=PIN,Id=case['Id'],Route=route,LibraryPath=str(lib),LibrarySha256=image['Library']['Sha256'],BuildReceiptPath=str(native),BuildReceiptSha256=sha(native),Beams=beams,**bridge_admission))
                env['ERL_CRASH_DUMP']=str(rd/'unused-managed.crash.dump')
                result=phase(['dotnet','exec','--fx-version',a.framework_version,host,'--version651-case',admission,sha(admission),rd/'capture'],rd,'managed',180)
                if (rd/'capture/completion.json').is_file():
                    receipt=json.loads((rd/'capture/completion.json').read_bytes());result['CaptureReceipt']=receipt
                    if not receipt.get('Passed') or not receipt.get('RuntimeClosed') or not receipt.get('ImageRetired') or not receipt.get('OriginalBeamExecution'):
                        result['Passed']=False;errors.append(dict(Case=case['Id'],Route=route,Error='Managed capture/cleanup did not complete'))
                else:result['Passed']=False;errors.append(dict(Case=case['Id'],Route=route,Error='Missing managed capture receipt'))
                entry['Routes'][route]=result
                left=original/'observed.etf';right=rd/'capture/observed.etf'
                if left.is_file() and right.is_file():
                    try:
                        aterm=decoder.decode_exact(left.read_bytes());bterm=decoder.decode_exact(right.read_bytes());assert_expected(aterm,case);assert_expected(bterm,case)
                        equal=dict(WholeTypedEquality=aterm==bterm,RawByteEquality=left.read_bytes()==right.read_bytes(),OriginalSha256=sha(left),ManagedSha256=sha(right))
                        entry['Comparisons'][route]=equal
                        if not all([equal['WholeTypedEquality'],equal['RawByteEquality']]):errors.append(dict(Case=case['Id'],Route=route,Error='Complete outcome/stack differs; no normalization performed'))
                    except Exception as error:errors.append(dict(Case=case['Id'],Route=route,Error=repr(error)))
            observations.append(entry);save(d/'completion.json',entry)
    except Exception as error:
        errors.append(dict(Phase="collection-unexpected",Error=repr(error)))
    finally:
        # Independent module scenarios reuse admitted provider/native/host; even a baseline failure must not skip them.
        try:
            module=load('v651_module690',tool/'module_text690.py')
            module690=module.collect(tool,out,images.get('v2_18_opts1'),native,host,installed,erl,
                bridge_admission,phase,decoder,env,a.framework_version,linux)
        except Exception as error:
            errors.append(dict(Phase='module690-collection',Error=repr(error)))
        try:
            if sealed is not None:verify_rows(sealed)
            for row_ in beams:checked(row_['Path'],row_['Sha256'])
            if compile_ready:
                for name in ['nif_version651.erl','version_observer651.erl']:need(sha(prep/name)==sha(tool/'fixtures'/name),'Copied Erlang source changed')
            for image_ in images.values():checked(image_['Library']['Path'],image_['Library']['Sha256'])
            guard(repo,tool,a.plan_sha256,input_name)
        except Exception as error:errors.append(dict(Phase='inputs-after',Error=repr(error)))
        if after is not None:
            try:
                current=after();save(out/'provider-after.json',current);need(current==before['Inventory'],'Provider changed')
            except Exception as error:errors.append(dict(Phase='provider-after',Error=repr(error)))
    complete=len(observations)==42 and all(len(x['Routes'])==3 and all(r.get('Passed') for r in x['Routes'].values()) and len(x['Comparisons'])==2 for x in observations)
    result=dict(Passed=complete and module690['Passed'] and not errors,Count=42,Rows=observations,ModuleText690=module690,Errors=errors,FreshOriginalCompilation=compile_ready,OriginalBeamReplay=True,RawOutcomesRewritten=False,FramesProjected=False,ApiDelta=0,FullContractDelta=0,OutputFiles=[row(q) for q in sorted(out.rglob('*')) if q.is_file()])
    save(out/'completion.json',result);return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
