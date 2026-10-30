"""Fresh genuine same-DLL original, matched compiled MFA and full raw stacks."""
import argparse,json,os,shutil,sys
from pathlib import Path
from common import *

def main():
    p=argparse.ArgumentParser()
    for key in ['repository','output','native-build-receipt']:p.add_argument('--'+key,type=Path,required=True)
    for key in ['expected-head','plan-sha256','admission-sha256','approved-native-build-sha256']:p.add_argument('--'+key,required=True)
    for key in ['official-admission','build-receipt','otp-source']:p.add_argument('--'+key,type=Path)
    a=p.parse_args();repo=a.repository.resolve();head=select_head(repo,a.expected_head);packet=Path(__file__).resolve().parent
    linux=sys.platform.startswith('linux');need(os.name=='nt' or linux and os.uname().machine=='x86_64','Windows/Linux x64')
    plan=guard(repo,packet,a.plan_sha256,'original-inputs.json');guard(repo,packet,plan['NativeBuildPlanSha256'],'build-inputs.json')
    receipt=checked(a.native_build_receipt,a.approved_native_build_sha256);build=json.loads(receipt.read_bytes())
    need(build['Status']=='ENV709_NATIVE_BUILD' and build['Passed'] and build['Pin']==PIN and build['SubjectHead']==head and build['PlanSha256']==plan['NativeBuildPlanSha256'],'Exact admitted fresh native builder')
    need(len(build['Images'])==1 and build['Images'][0]['Id']=='same_dll709','ONE physical library')
    library=checked(build['Images'][0]['Library']['Path'],build['Images'][0]['Library']['Sha256'])
    need(library.is_relative_to(receipt.parent) and library.suffix==('.so' if linux else '.dll'),'Native output containment/platform')
    out=fresh(repo,a.output);errors=[];results=[];before=None;after=None;beams={};env=environment();stage=stages(repo,linux,env)
    sealed=rows([receipt,library]);save(out/'sealed-inputs.json',sealed)
    prep=out/'preparation';prep.mkdir();prepared=False
    try:
        provider=load('env709_original_provider',repo/PROVIDER)
        installed,erl,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
        for name in ['nif_env709.erl','env_observer709.erl']:shutil.copyfile(packet/'fixtures'/name,prep/name)
        expression=('W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
            'Expected=filename:absname('+estr(installed)+'),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
            'Compile=fun({N,S,D})->{ok,F}=epp:parse_file(S,[],D),R=compile:forms(F,[binary,debug_info,return_errors,return_warnings]),'
            'W(N++"-compile.etf",R),{M,B}=case R of {ok,M0,B0}->{M0,B0};{ok,M0,B0,_}->{M0,B0} end,'
            'ok=file:write_file(N++".beam",B),{N,M,beam_lib:md5(B)} end,'
            'R=[Compile(X)||X<-[{"subject-v1","nif_env709.erl",[{\'CODE_VERSION709\',1}]},'
            '{"subject-v2","nif_env709.erl",[{\'CODE_VERSION709\',2}]},{"observer","env_observer709.erl",[]}]],W("compile-receipt.etf",R),halt(0).')
        (prep/'compile.eval').write_bytes(expression.encode());env['ERL_CRASH_DUMP']=str(prep/'erl_crash.dump')
        stage([erl,'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang','-eval',expression],prep,'compile',180)
        beams={name:row(prep/(name+'.beam')) for name in ['subject-v1','subject-v2','observer']}
        need(beams['subject-v1']['Sha256']!=beams['subject-v2']['Sha256'],'Changed code checksum');save(prep/'beams.json',beams);prepared=True
        for mode in [0,1]:
            d=out/('accept' if mode==0 else 'veto');d.mkdir();record=dict(Mode=mode,Passed=False)
            try:
                log=d/'events.log';log.write_bytes(b'');env['ERL_CRASH_DUMP']=str(d/'erl_crash.dump')
                native=library.with_suffix('').as_posix()
                source=('W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
                    'Expected=filename:absname('+estr(installed)+'),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
                    '{ok,O}=file:read_file('+estr(prep/'observer.beam')+'),{module,env_observer709}=code:load_binary(env_observer709,"observer.beam",O),'
                    '{ok,A}=file:read_file('+estr(prep/'subject-v1.beam')+'),{ok,B}=file:read_file('+estr(prep/'subject-v2.beam')+'),'
                    'T=env_observer709:capture(A,B,'+estr(native)+','+estr(log)+','+str(mode)+'),W("observed.etf",T),'
                    '{{returned,Body},normal}=T,{First,Before,Upgrade,OldAfter,CurrentAfter,Counts,Stable,Threads}=Body,'
                    '{value,ok}=First,{{value,true},_}=Stable,{{value,709},_}=Threads,'
                    + ('{{value,true},{value,true}}=Stable,{{value,709},{value,709}}=Threads,{value,ok}=Upgrade,' if mode==0 else '{value,{error,{upgrade,_}}}=Upgrade,')+
                    'W("witness.etf",{First,Before,Upgrade,OldAfter,CurrentAfter,Counts,Stable,Threads}),'
                    'P=code:purge(nif_env709),W("old-purge.etf",P),'
                    'OldCount=fun()->{ok,D}=file:read_file('+estr(log)+'),length([ok||<<"unload 1">><-binary:split(D,<<"\n">>,[global])]) end,'
                    'OldWait=fun F(0)->erlang:error({env709_old_unload_timeout,OldCount()});F(N)->case OldCount() of 1->ok;_->receive after 10->F(N-1) end end end,'
                    'OldWait(1500),W("old-unload-phase.etf",{old_a_unload_phase_complete,OldCount()}),'
                    'Cleanup={code:delete(nif_env709),code:purge(nif_env709)},W("code-cleanup.etf",Cleanup),'
                    'Count=fun()->{ok,D}=file:read_file('+estr(log)+'),Lines=binary:split(D,<<"\n">>,[global]),'
                    '{length([ok||<<"load ",_/binary>><-Lines])+length([ok||<<"accept ",_/binary>><-Lines]),length([ok||<<"unload ",_/binary>><-Lines])} end,'
                    'Wait=fun F(0)->erlang:error({env709_unload_timeout,Count()});F(N)->case Count() of {X,X} when X>0->ok;_->receive after 10->F(N-1) end end end,'
                    'Wait(1500),W("physical-cleanup.etf",{accepted_owners_unloaded,Count()}),halt(0).')
                (d/'invocation.eval').write_bytes(source.encode())
                record['Original']=stage([erl,'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang','-eval',source],d,'original',90)
                record['Observed']=row(d/'observed.etf');record['Witness']=row(d/'witness.etf');record['PhysicalCleanup']=row(d/'physical-cleanup.etf');record['Events']=row(log)
                events=[line.decode('ascii').split() for line in log.read_bytes().splitlines()]
                need(events and events[0]==['load','1'],'Genuine first load');need(all(len(e)==2 and e[0] in ['load','upgrade','accept','veto','unload'] for e in events),'Unmodified semantic event protocol')
                need(sum(e[0] in ['load','accept'] for e in events)==sum(e[0]=='unload' for e in events),'Actual owner unload balance')
                record.update(Passed=True,EventValues=events,RawOutcomesRewritten=False,PointerAddressesNormalized=False,ForeignOsThreadJoined=True)
            except Exception as error:record['Error']=repr(error);errors.append(dict(Mode=mode,Error=repr(error)))
            results.append(record);save(d/'completion.json',record)
    except Exception as error:errors.append(dict(Phase='preparation',Error=repr(error)))
    finally:
        try:
            verify(sealed)
            for beam in beams.values():checked(beam['Path'],beam['Sha256'])
            guard(repo,packet,a.plan_sha256,'original-inputs.json')
            guard(repo,packet,plan['NativeBuildPlanSha256'],'build-inputs.json')
        except Exception as error:errors.append(dict(Phase='source-after',Error=repr(error)))
        if after is not None:
            try:
                current=after();save(out/'provider-after.json',current);need(current==before['Inventory'],'Provider unchanged')
            except Exception as error:errors.append(dict(Phase='provider-after',Error=repr(error)))
    passed=prepared and len(results)==2 and all(r['Passed'] for r in results) and not errors
    result=dict(Status='ENV709_ORIGINAL',Pin=PIN,SubjectHead=head,Passed=passed,OriginalPlanSha256=a.plan_sha256.upper(),NativeBuild=row(receipt),Beams=beams,Rows=results,Errors=errors,OutputFiles=rows(p for p in out.rglob('*') if p.is_file()),FullRawOutcomesAndStacks=True,CompiledWorker=True,WholeApiCredits=0)
    save(out/'completion.json',result);return 0 if passed else 1
if __name__=='__main__':raise SystemExit(main())
