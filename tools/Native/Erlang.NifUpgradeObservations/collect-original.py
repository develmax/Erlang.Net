"""Fresh original N2 functional baseline; no managed/runtime implementation or build."""
import argparse,json,os,shutil,sys
from pathlib import Path
from common import *

def main():
    parser=argparse.ArgumentParser()
    for name in ['repository','output','native-build-receipt','wrong-entry-build-receipt']:parser.add_argument('--'+name,type=Path,required=True)
    for name in ['plan-sha256','admission-sha256','expected-head','approved-native-build-sha256','approved-wrong-entry-build-sha256']:parser.add_argument('--'+name,required=True)
    for name in ['official-admission','build-receipt','otp-source']:parser.add_argument('--'+name,type=Path)
    a=parser.parse_args();repo=a.repository.resolve();HEAD=select_head(repo,a.expected_head);packet=Path(__file__).resolve().parent;linux=sys.platform.startswith('linux')
    need((os.name=='nt' or linux and os.uname().machine=='x86_64') and a.expected_head==HEAD,'Exact subject/platform required')
    plan=guard(repo,packet,a.plan_sha256,'original-inputs.json');guard(repo,packet,plan['NativeBuildPlanSha256'],'build-inputs.json');out=fresh(repo,a.output);env=environment();stage=stages(repo,linux,env)
    native=checked(a.native_build_receipt,a.approved_native_build_sha256);build=json.loads(native.read_bytes())
    need(build['Status']=='UPGRADE693_NATIVE_BUILD' and build['Pin']==PIN and build['SubjectHead']==HEAD and build['Passed'] and build['PlanSha256']==plan['NativeBuildPlanSha256'],'Unadmitted genuine native3 build')
    images={item['Id']:item['Library'] for item in build['Images']};need(set(images)=={'a42','b43','b43_no_upgrade'},'Exact native3 image IDs')
    wrong=checked(a.wrong_entry_build_receipt,a.approved_wrong_entry_build_sha256);wrong_build=json.loads(wrong.read_bytes())
    need(wrong_build['Passed'] and wrong_build['Pin']==PIN and wrong_build['Status']==('VERSION651_LINUX_BUILD' if linux else 'VERSION651_WINDOWS_BUILD')
         and wrong_build['PlanSha256']==plan['WrongEntryNativeBuildPlans']['Linux' if linux else 'Windows'] and wrong_build['SubjectHead']==HEAD,'Wrong-entry N1 source producer differs')
    images['n1_wrong_name']=next(item['Library'] for item in wrong_build['Images'] if item['Id']=='v2_18_opts1')
    for key,image in images.items():
        checked(image['Path'],image['Sha256']);need(image['Path'].endswith('.so' if linux else '.dll'),'Wrong native platform')
        need(Path(image['Path']).resolve().is_relative_to(wrong.parent if key=='n1_wrong_name' else native.parent),'Escaping native producer output')
    sealed=rows([native,wrong,*[Path(item['Path']) for item in images.values()]])
    save(out/'native-admissions.json',dict(GenuineBuild=row(native),WrongEntryBuild=row(wrong),Images=images));save(out/'sealed-inputs.json',sealed)
    provider=load('upgrade693_original_provider',repo/PROVIDER);decoder=load('upgrade693_original_etf',repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py')
    errors=[];results=[];before=None;after=None;beams={};prep=out/'preparation';prep.mkdir();compile_pass=False
    def phase(argv,d,name,timeout=120):
        try:return dict(Passed=True,Receipt=stage(argv,d,name,timeout))
        except Exception as error:
            errors.append(dict(Phase=name,Directory=str(d),Error=repr(error)));return dict(Passed=False,Error=repr(error))
    try:
        installed,erl,before,after=provider.provider(a,repo);save(out/'provider-before.json',before)
        need(all(ord(c)<128 for c in str(installed)+str(out)), 'Native/provider/output paths remain ASCII')
        for source in ['nif_upgrade693.erl','upgrade_observer693.erl']:shutil.copyfile(packet/'fixtures'/source,prep/source)
        expression=('W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
            'Expected=filename:absname('+estr(installed)+'),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
            'Compile=fun({Name,Source,Defines})->Result=try {ok,F}=epp:parse_file(Source,[],Defines),'
            'C=compile:forms(F,[binary,debug_info,return_errors,return_warnings]),W(Name++"-compile.etf",C),'
            '{M,B}=case C of {ok,M0,B0}->{M0,B0};{ok,M0,B0,_}->{M0,B0} end,ok=file:write_file(Name++".beam",B),'
            '{ok,{M,Md5}}=beam_lib:md5(B),{ok,M,Md5} catch Class:Reason:Stack->{failed,Class,Reason,Stack} end,'
            '{Name,Result} end,Results=[Compile(S)||S<-[{"subject-v1","nif_upgrade693.erl",[{\'CODE_VERSION693\',1}]},'
            '{"subject-v2","nif_upgrade693.erl",[{\'CODE_VERSION693\',2}]},{"observer","upgrade_observer693.erl",[]}]],'
            'W("compile-receipt.etf",Results),case lists:all(fun({_,{ok,_,_}})->true;(_)->false end,Results) of true->halt(0);false->halt(79) end.')
        (prep/'compile.eval').write_bytes(expression.encode());env['ERL_CRASH_DUMP']=str(prep/'erl_crash.dump')
        compilation=phase([erl,'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang',*(['-start_epmd','false'] if linux else []),'-eval',expression],prep,'compile',180)
        for name in ['subject-v1','subject-v2','observer']:
            path=prep/(name+'.beam')
            if path.is_file():beams[name]=row(path)
        compile_pass=compilation['Passed'] and len(beams)==3
        if compile_pass:need(beams['subject-v1']['Sha256']!=beams['subject-v2']['Sha256'],'Changed marker must change BEAM checksum')
        save(prep/'completion.json',dict(Passed=compile_pass,Compilation=compilation,Beams=beams))
        cases=json.loads((packet/'cases.json').read_bytes());need(len(cases)==12,'Twelve initial functional cases required')
        for case in cases:
            d=out/case['Id'];d.mkdir();record=dict(Id=case['Id'],Case=case,Passed=False)
            if not compile_pass:
                record['SkippedDependent']='Original genuine BEAM preparation failed';results.append(record);save(d/'completion.json',record);continue
            try:
                log=d/'events.log';log.write_bytes(b'');env['ERL_CRASH_DUMP']=str(d/'erl_crash.dump')
                a_path=Path(images['a42']['Path']).with_suffix('').as_posix();b_path=Path(images[case['NativeB']]['Path']).with_suffix('').as_posix()
                beam2=prep/('subject-v1.beam' if case['BeamChecksum']=='same' else 'subject-v2.beam')
                source=('W=fun(N,T)->ok=file:write_file(N,term_to_binary(T,[{minor_version,2}])) end,'
                    'Expected=filename:absname('+estr(installed)+'),Expected=filename:absname(code:root_dir()),8=erlang:system_info(wordsize),'
                    '{ok,Observer}=file:read_file('+estr(prep/'observer.beam')+'),{module,upgrade_observer693}=code:load_binary(upgrade_observer693,"observer.beam",Observer),'
                    '{ok,B1}=file:read_file('+estr(prep/'subject-v1.beam')+'),{ok,B2}=file:read_file('+estr(beam2)+'),'
                    'T=upgrade_observer693:capture(B1,B2,'+estr(a_path)+','+estr(b_path)+','+estr(log)+','+str(case['Mode'])+','+str(case['ReloadOnly']).lower()+'),W("observed.etf",T),'
                    'OldEvents=fun()->case file:read_file('+estr(log)+') of {ok,D}->[Parts||L<-lists:droplast(binary:split(D,<<"\n">>,[global])),Parts<-[binary:split(L,<<" ">>,[global])],length(Parts)=:=4];{error,_}->[] end end,'
                    'OldCount=fun()->length([ok||[<<"5">>,<<"42">>,_,_]<-OldEvents()]) end,'
                    'WaitOld=fun F(0)->erlang:error({upgrade709_old_unload_timeout,OldEvents()});F(N)->case OldCount() of 1->ok;0->receive after 10->F(N-1) end;Other->erlang:error({upgrade709_old_unload_count,Other}) end end,'
                    'OldPurge=code:purge(nif_upgrade693),case '+str(case['ReloadOnly']).lower()+' of false->WaitOld(1500);true->ok end,'
                    'W("old-unload-phase.etf",{old_a_unload_phase_complete,OldCount()}),'
                    'Cleanup={OldPurge,code:delete(nif_upgrade693),code:purge(nif_upgrade693)},W("code-cleanup.etf",Cleanup),'
                    'Counts=fun()->{ok,Data}=file:read_file('+estr(log)+'),Lines=[binary:split(L,<<" ">>,[global])||L<-binary:split(Data,<<"\n">>,[global]),L=/=<<>>],'
                    '{length([ok||[E,_,_,_]<-Lines,E=:=<<"1">> orelse E=:=<<"3">>]),length([ok||[<<"5">>,_,_,_]<-Lines])} end,'
                    'Wait=fun F(0)->erlang:error({upgrade693_unload_timeout,Counts()});F(N)->case Counts() of {ExpectedCount,ActualCount} when ExpectedCount>0,ExpectedCount=:=ActualCount->ok;_->receive after 10->F(N-1) end end end,'
                    'Wait(1500),W("physical-cleanup.etf",{all_accepted_owners_unloaded,Counts()}),halt(0).')
                (d/'invocation.eval').write_bytes(source.encode());record['Original']=phase([erl,'+S','1:1','+A','1','-noshell','-noinput','-boot','no_dot_erlang',*(['-start_epmd','false'] if linux else []),'-eval',source],d,'original',90)
                observed=d/'observed.etf'
                if observed.is_file():
                    term=decoder.decode_exact(observed.read_bytes());need(term[0]=='tuple' and len(term[1])==2 and term[1][1]==('atom','normal'),'Actual monitored compiled worker envelope')
                    result=term[1][0];need(result[0]=='tuple' and len(result[1])==2 and result[1][0]==('atom','returned'),'Compiled worker must return actual observation')
                    body=result[1][1];need(body[0]=='tuple' and len(body[1])==6,'Complete six-part functional observation')
                    need(body[1][0]==('tuple',(('atom','value'),('atom','ok'))),'Initial genuine A NIF load failed')
                    atom=lambda value:('atom',value)
                    tuple_=lambda *values:('tuple',tuple(values))
                    value=lambda item:tuple_(atom('value'),item)
                    upgrade=body[1][2]
                    if case['ReloadOnly']:category='reload';text='NIF library already loaded (reload disallowed since OTP 20).'
                    elif case['NativeB']=='n1_wrong_name':category='bad_lib';text="Library module name 'nif_version651' does not match calling module 'nif_upgrade693'"
                    elif case['NativeB']=='b43_no_upgrade':category='upgrade';text='Upgrade not supported by this NIF library.'
                    elif case['Mode'] in [2,3]:category='upgrade';text='Library upgrade-call unsuccessful ('+('7' if case['Mode']==2 else '-13')+').'
                    else:category=None;text=None
                    wanted=value(atom('ok')) if category is None else value(tuple_(atom('error'),tuple_(atom(category),('list',tuple(('integer',ord(c)) for c in text)))))
                    need(upgrade==wanted,'Original upgrade result/source precedence differs; raw observation retained')
                    record['ExpectedUpgradeKind']=category or 'success'

                    record['Observed']=row(observed);record['FullRawOutcomeAndStacksRetained']=True
                else:raise RuntimeError('Missing complete original outcome')
                lines=log.read_bytes().splitlines();events=[tuple(int(word) for word in line.split()) for line in lines];need(all(len(event)==4 for event in events),'Fixed semantic native event protocol')
                need(events and events[0]==(1,42,17,0) and not any(event[0]==7 for event in events),'Initial load or forbidden reload callback differs')
                need(sum(event[0] in [1,3] for event in events)==sum(event[0]==5 for event in events),'Every accepted native owner must unload')
                record['OldUnloadPhase']=row(d/'old-unload-phase.etf');record['Events']=row(log);record['EventValues']=events;record['PhysicalCleanup']=row(d/'physical-cleanup.etf');record['Passed']=record['Original']['Passed']
            except Exception as error:record['Error']=repr(error);errors.append(dict(Case=case['Id'],Error=repr(error)))
            results.append(record);save(d/'completion.json',record)
    except Exception as error:errors.append(dict(Phase='provider-preparation',Error=repr(error)))
    finally:
        try:
            verify(sealed)
            for beam in beams.values():checked(beam['Path'],beam['Sha256'])
            for name in ['nif_upgrade693.erl','upgrade_observer693.erl']:
                if (prep/name).is_file():need(sha(prep/name)==sha(packet/'fixtures'/name),'Copied Erlang source changed')
            guard(repo,packet,a.plan_sha256,'original-inputs.json')
            guard(repo,packet,plan['NativeBuildPlanSha256'],'build-inputs.json')
        except Exception as error:errors.append(dict(Phase='inputs-after',Error=repr(error)))
        if after is not None:
            try:current=after();save(out/'provider-after.json',current);need(current==before['Inventory'],'Provider changed')
            except Exception as error:errors.append(dict(Phase='provider-after',Error=repr(error)))
    passed=len(results)==12 and all(record['Passed'] for record in results) and not errors
    result=dict(Status='UPGRADE693_ORIGINAL_BASELINE',OriginalProducerPlanSha256=a.plan_sha256.upper(),Platform='linux-x64-lp64' if linux else 'windows-x64-llp64',OutputFiles=rows(p for p in out.rglob('*') if p.is_file()),CleanupProtocol='OLD_A_UNLOAD_BEFORE_CURRENT_PURGE_709',Passed=passed,Pin=PIN,SubjectHead=HEAD,Count=12,Rows=results,Errors=errors,FreshOriginalCompilation=compile_pass,CompiledWorker=True,RawOutcomesRewritten=False,PointerAddressesNormalized=False,OpaqueFunEtfCompared=False,WholeN2Ready=False,WholeApiCredits=0)
    save(out/'completion.json',result);return 0 if passed else 1
if __name__=='__main__':raise SystemExit(main())
