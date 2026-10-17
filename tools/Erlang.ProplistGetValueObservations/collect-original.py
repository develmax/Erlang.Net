"""One bounded pinned-OTP proplists59 capture; no SDK/native NIF dependencies."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, os, re, sys, threading, time
R=Path(__file__).resolve().parents[2]
T=Path(__file__).resolve().parent
sys.path.insert(0,str(R/'tools/Erlang.SchedulerAffinityOriginalObservations'))
from source_inputs import PIN,digest,verify,source_identity,sanitized_environment,require_normal_receipt

def load(path,name):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module

def write(path,value):
    with path.open('x',encoding='utf-8') as stream:json.dump(value,stream,indent=2);stream.write('\n')

def main():
    parser=argparse.ArgumentParser()
    for key in ['expected-head','source-inputs-sha256']:parser.add_argument('--'+key,required=True)
    for key in ['otp-source','runtime-root','build-receipt','approved-build-receipt-sha256','output']:parser.add_argument('--'+key)
    parser.add_argument('--verify-only',action='store_true')
    a=parser.parse_args();sourceplan=T/'source-inputs.json';assert digest(sourceplan)==a.source_inputs_sha256.upper()
    plan=json.loads(sourceplan.read_bytes());assert plan['PinnedOtpCommit']==PIN and plan['Family']=='proplists59'
    for row in plan['BoundSourceInputs']:
        p=R/row['Path'];assert p.resolve().is_relative_to(R) and p.stat().st_size==row['Bytes'] and digest(p)==row['Sha256']
    for row in plan['Files']:
        p=R/row['Path'];assert p.stat().st_size==row['Bytes'] and digest(p)==row['Sha256']
    build_manifest=R/'tools/Erlang.SchedulerAffinityOriginalObservations/manifest.json'
    actual_build_manifest_sha=digest(build_manifest);assert actual_build_manifest_sha==plan['ExistingBuildManifestSha256']
    _,_,build_plan=verify(R,actual_build_manifest_sha,a.expected_head)
    fixture=T/'original/proplist_get_value_contract.erl'
    source=fixture.read_text();names=re.findall(r"'([^']+)'",source.split('names()->[',1)[1].split('].',1)[0])
    assert names==plan['Names'] and len(names)==len(set(names))==59
    assert 'get_value,FrameArgs' in source and 'true=FrameArgs=:=[key,#{a=>1}' in source
    if a.verify_only:print('PASS_SOURCE_BOUND_CORRECTED_PROPLISTS59_ONLY');return
    assert all(getattr(a,key.replace('-','_')) for key in ['otp-source','runtime-root','build-receipt','approved-build-receipt-sha256','output'])
    output=Path(a.output).resolve();output.mkdir(parents=True,exist_ok=False)
    stages=[];phase='build-admission';vm_started=False
    try:
        receipt_path=Path(a.build_receipt).resolve();assert digest(receipt_path)==a.approved_build_receipt_sha256.upper()
        build=json.loads(receipt_path.read_bytes());runtime=Path(a.runtime_root).resolve();otp=Path(a.otp_source).resolve()
        assert build['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['Commit']==PIN and build['SubjectHead']==a.expected_head
        assert build['SourceManifestSha256']==actual_build_manifest_sha and Path(build['RuntimeRoot']).resolve()==runtime
        assert Path(build['Prefix']).resolve()/'lib/erlang'==runtime and Path(build['SourceRoot']).resolve()==otp
        assert digest(receipt_path.parent/'source-before.json')==build['SourceBeforeSha256'] and digest(receipt_path.parent/'source-after.json')==build['SourceAfterSha256']
        before=source_identity(otp);assert before==json.loads((receipt_path.parent/'source-before.json').read_bytes())==json.loads((receipt_path.parent/'source-after.json').read_bytes())
        assert [row['Name'] for row in build['Stages']]==['configure','make','install']
        for row in build['Stages']:
            p=Path(row['ReceiptPath']);assert p.resolve().is_relative_to(receipt_path.parent) and digest(p)==row['ReceiptSha256']
            require_normal_receipt(json.loads(p.read_bytes()))
            for key in ['Stdout','Stderr']:assert digest(p.parent/(row['Name']+'.'+key.lower()))==row[key+'Sha256']
        for row in build['InstalledFiles']:assert digest(runtime/row['Path'])==row['Sha256']
        versions=list((runtime/'releases').glob('*/OTP_VERSION'));assert any(p.read_text().strip()=='29.1.1' for p in versions)
        proplists=list(runtime.glob('lib/stdlib-*/ebin/proplists.beam'));assert len(proplists)==1
        installed=proplists[0].resolve();installed_sha=digest(installed)
        pinned_proplists=next(row for row in before['TrackedFiles'] if row['Path']=='lib/stdlib/src/proplists.erl')
        installed_copy=output/'proplists-installed.beam';installed_copy.write_bytes(installed.read_bytes())
        owned=load(R/'tools/Erlang.GuestConnectedSendObservations/owned_process.py','pure_proplist_owned')
        decoder=load(R/'tools/Erlang.GuestConnectedSendObservations/passive_etf.py','pure_proplist_decoder')
        environment=sanitized_environment(os.environ)
        for key in ['GH_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_LIBRARY_PATH']:environment.pop(key,None)
        environment['ERLC_USE_SERVER']='false';environment['PURE_API_OUTPUT']=str(output);environment['PURE_API_EBIN']=str(output/'ebin')
        write(output/'inputs.json',dict(SubjectHead=a.expected_head,PinnedOtpCommit=PIN,SourceInputsSha256=digest(sourceplan),ExistingBuildManifestSha256=actual_build_manifest_sha,BuildReceiptSha256=digest(receipt_path),FixtureSha256=digest(fixture),InstalledProplistsPath=str(installed),InstalledProplistsSha256=installed_sha,PinnedProplistsSourceSha256=pinned_proplists['Sha256'],ErlPath=str(runtime/'bin/erl'),ErlSha256=digest(runtime/'bin/erl'),ErlcPath=str(runtime/'bin/erlc'),ErlcSha256=digest(runtime/'bin/erlc'),Names=names,Flags=['+S','2:2'],WorkflowRun=os.environ.get('GITHUB_RUN_ID'),WorkflowAttempt=os.environ.get('GITHUB_RUN_ATTEMPT')))
        def stage(name,arguments,timeout):
            process=None;failure=None
            try:
                process=owned.OwnedProcess([str(x) for x in arguments],R,environment,output/name)
                process.wait(time.monotonic()+timeout,threading.Event().is_set)
            except BaseException as error:failure=repr(error)
            finally:
                if process is not None:
                    try:process.close()
                    except BaseException as error:failure=(failure or '')+';cleanup:'+repr(error)
                write(output/(name+'.stage.json'),dict(Name=name,Failure=failure,Started=process is not None,Arguments=[str(x) for x in arguments]))
            if process is not None:
                proof=output/(name+'.process.json');stages.append(dict(Name=name,ReceiptSha256=digest(proof),StdoutSha256=digest(output/(name+'.stdout')),StderrSha256=digest(output/(name+'.stderr'))))
            if failure:raise RuntimeError(name+':'+failure)
            require_normal_receipt(json.loads((output/(name+'.process.json')).read_bytes()))
        phase='compile';(output/'ebin').mkdir()
        stage('compile',[runtime/'bin/erlc','-Werror','-o',output/'ebin',fixture],90)
        compiled=output/'ebin/proplist_get_value_contract.beam';assert compiled.is_file()
        phase='observe';vm_started=True
        expression='O=os:getenv("PURE_API_OUTPUT"),E=os:getenv("PURE_API_EBIN"),{module,proplist_get_value_contract}=code:load_abs(filename:join(E,"proplist_get_value_contract")),{module,proplists}=code:ensure_loaded(proplists),{proplists,B,F}=code:get_object_code(proplists),ok=file:write_file(filename:join(O,"proplists-loaded.beam"),B),Meta={pure_api_provenance,code:root_dir(),code:which(proplists),F,erlang:md5(B),proplists:module_info(md5),code:which(proplist_get_value_contract),erlang:system_info(otp_release),erlang:system_info(schedulers),erlang:system_info(schedulers_online),proplist_get_value_contract:names()},ok=file:write_file(filename:join(O,"metadata.etf"),term_to_binary(Meta)),Rows=proplist_get_value_contract:run(),ok=file:write_file(filename:join(O,"returned.etf"),term_to_binary(Rows)),halt().'
        stage('observe',[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-eval',expression],120)
        metadata=decoder.decode_exact((output/'metadata.etf').read_bytes());rows=decoder.decode_exact((output/'returned.etf').read_bytes())
        def text(value):
            assert value[0]=='list' and all(x[0]=='integer' for x in value[1]);return ''.join(chr(x[1]) for x in value[1])
        assert metadata[0]=='tuple' and len(metadata[1])==11 and metadata[1][0]==('atom','pure_api_provenance')
        fields=metadata[1];assert Path(text(fields[1])).resolve()==runtime
        assert Path(text(fields[2])).resolve()==Path(text(fields[3])).resolve()==installed
        assert Path(text(fields[6])).resolve()==compiled.resolve() and text(fields[7])=='29'
        assert fields[8:10]==(('integer',2),('integer',2))
        assert fields[10]==('list',tuple(('atom',n) for n in names))
        assert digest(output/'proplists-loaded.beam')==installed_sha==digest(installed)
        assert fields[4]==('binary',hashlib.md5(installed.read_bytes()).digest())
        assert rows[0]=='list' and len(rows[1])==59
        results=[]
        for name,row in zip(names,rows[1]):
            assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name)
            result=row[1][1];results.append(dict(Name=name,Passed=result==('tuple',(('atom','ok'),('atom','done'))),ActualDecoded=repr(result)))
        write(output/'case-results.json',results)
        assert source_identity(otp)==before and digest(fixture)==plan['FixtureSha256']
        failed=[row for row in results if not row['Passed']]
        write(output/'completion.json',dict(Status='PASS_ORIGINAL_PROPLISTS59' if not failed else 'FAIL_ORIGINAL_PROPLISTS59_ROWS_RETAINED',SubjectHead=a.expected_head,PinnedOtpCommit=PIN,Names=names,Total=59,Passed=59-len(failed),Failed=len(failed),SourceInputsSha256=digest(sourceplan),BuildReceiptSha256=digest(receipt_path),InstalledProplistsSha256=installed_sha,FixtureSha256=digest(fixture),CompiledFixtureSha256=digest(compiled),ReturnedSha256=digest(output/'returned.etf'),MetadataSha256=digest(output/'metadata.etf'),CaseResultsSha256=digest(output/'case-results.json'),OwnedStages=stages,AllRowsAcquiredBeforeVerdict=True,OriginalOnly=True,SdkOrNativeNifExecution=False))
        if failed:raise RuntimeError('Original proplists contract rows failed; all59 retained.')
    except BaseException as error:
        write(output/'failure.json',dict(Status='FAILED_ORIGINAL_PROPLISTS59_RAW_RETAINED',Phase=phase,Error=repr(error),VmStarted=vm_started,DependentVmSkipped=not vm_started,OwnedStages=stages));raise
if __name__=='__main__':main()
