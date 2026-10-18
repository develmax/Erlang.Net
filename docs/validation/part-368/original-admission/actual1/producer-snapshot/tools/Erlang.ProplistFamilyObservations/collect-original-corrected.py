"""Source-only family adapter. No build/clone/network action; admitted same-job runtime only."""
from pathlib import Path
import argparse, hashlib, importlib.util, json, os, sys, threading, time

def load(path,name):
    spec=importlib.util.spec_from_file_location(name,path);m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m);return m

def digest(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def write(path,value):
    with Path(path).open('x',encoding='utf-8') as f:json.dump(value,f,indent=2);f.write('\n')
def resolved(root,name):
    path=(root/name).resolve();assert path.is_relative_to(root.resolve());return path

def require_parse_gate(gate_data,all_sources):
    assert gate_data['Status']=='PASS_FIXTURE_PARSE_GATE'
    rows=gate_data['Fixtures']
    assert len(rows)==len(all_sources) and len(all_sources)>0
    assert all(x.get('Passed') is True and 'Error' not in x for x in rows)
    actual=[(str(Path(x['Path']).resolve()),x['Sha256']) for x in rows]
    assert len({x[0] for x in actual})==len(all_sources)
    assert sorted(actual)==sorted((str(x.resolve()),digest(x)) for x in all_sources)

def require_old59_stack_rows(stack_rows):
    assert stack_rows[0]=='list' and len(stack_rows[1])==12
    expected=[(('integer',arity),('atom',mode),('integer',depth)) for arity in [2,3] for mode in ['tail','non_tail'] for depth in [0,1,2]]
    for labels,row in zip(expected,stack_rows[1]):
        assert row[0]=='tuple' and len(row[1])==4 and row[1][:3]==labels
        captured=row[1][3]
        assert captured[0]=='tuple' and len(captured[1])==3
        assert captured[1][0]==('atom','error') and captured[1][1]==('atom','function_clause')
        assert captured[1][2][0]=='list'

def main():
    p=argparse.ArgumentParser()
    for key in ['repository','expected-head','source-inputs-sha256','parse-gate','parse-gate-sha256']:p.add_argument('--'+key,required=True)
    for key in ['otp-source','runtime-root','build-receipt','approved-build-receipt-sha256','output']:p.add_argument('--'+key)
    p.add_argument('--verify-only',action='store_true');a=p.parse_args()
    r=Path(a.repository).resolve();t=Path(__file__).resolve().parent;manifest=t/'source-inputs.corrected.json'
    assert digest(manifest)==a.source_inputs_sha256.upper();plan=json.loads(manifest.read_bytes())
    for row in plan['RepositoryInputs']:
        f=resolved(r,row['Path']);assert f.stat().st_size==row['Bytes'] and digest(f)==row['Sha256']
    for row in plan['ToolInputs']:
        f=resolved(t,row['Path']);assert f.stat().st_size==row['Bytes'] and digest(f)==row['Sha256']
    shared=load(r/'tools/Erlang.SchedulerAffinityOriginalObservations/source_inputs.py','family_source_inputs')
    shared.verify(r,plan['ExistingBuildManifestSha256'],a.expected_head)
    profile=json.loads((t/'profile.json').read_bytes());assert profile['PinnedOtpCommit']==shared.PIN;assert profile.get('SubjectModule')=='queue'
    def path_for(f):return resolved(t if f['Location']=='tool' else r,f['Path'])
    all_sources=[path_for(f) for f in profile['Fixtures']]
    all_sources += [resolved(t,x) for f in profile['Fixtures'] for x in f['Companions']]
    all_sources=list(dict.fromkeys(all_sources))
    declared=[resolved(t if x['Location']=='tool' else r,x['Path']) for x in profile['FixtureSources']]
    assert len(declared)==len(set(declared))==profile['ExpectedUniqueSourceCount']==len(all_sources)
    assert set(declared)==set(all_sources)
    for row,path in zip(profile['FixtureSources'],declared):assert digest(path)==row['Sha256']
    gate=Path(a.parse_gate);assert digest(gate)==a.parse_gate_sha256.upper();gate_data=json.loads(gate.read_bytes())
    require_parse_gate(gate_data,all_sources)
    assert len({x['Id'] for x in profile['Fixtures']})==len(profile['Fixtures'])
    assert set(profile['Mfas'])=={m for f in profile['Fixtures'] for c in f['Cases'] for m in c['Mfas']}
    for f in profile['Fixtures']:assert len(f['Names'])==len(set(f['Names']))==len(f['Cases']) and f['Names']==[c['Name'] for c in f['Cases']]
    if a.verify_only:print('PASS_SOURCE_BOUND_FAMILY_ADAPTER_AND_FIXTURE_PARSE_GATE');return
    assert sys.platform.startswith('linux'),'No pinned OTP runtime is installed in the current Windows environment.'
    assert all(getattr(a,k) for k in ['otp_source','runtime_root','build_receipt','approved_build_receipt_sha256','output'])
    out=Path(a.output).resolve();out.mkdir(parents=True,exist_ok=False)
    stages=[];groups=[];before=None;phase='build-admission'
    try:
        receipt=Path(a.build_receipt).resolve();assert digest(receipt)==a.approved_build_receipt_sha256.upper()
        build=json.loads(receipt.read_bytes());runtime=Path(a.runtime_root).resolve();otp=Path(a.otp_source).resolve()
        assert build['Status']=='PASS_OWNED_PINNED_ORIGINAL_BUILD' and build['Commit']==shared.PIN and build['SubjectHead']==a.expected_head
        assert build['SourceManifestSha256']==plan['ExistingBuildManifestSha256'] and Path(build['RuntimeRoot']).resolve()==runtime
        assert Path(build['Prefix']).resolve()/'lib/erlang'==runtime and Path(build['SourceRoot']).resolve()==otp
        assert digest(receipt.parent/'source-before.json')==build['SourceBeforeSha256'] and digest(receipt.parent/'source-after.json')==build['SourceAfterSha256']
        before=shared.source_identity(otp)
        assert before==json.loads((receipt.parent/'source-before.json').read_bytes())==json.loads((receipt.parent/'source-after.json').read_bytes())
        assert [x['Name'] for x in build['Stages']]==['configure','make','install']
        for stage in build['Stages']:
            f=Path(stage['ReceiptPath']);assert f.resolve().is_relative_to(receipt.parent) and digest(f)==stage['ReceiptSha256']
            shared.require_normal_receipt(json.loads(f.read_bytes()))
            for stream in ['Stdout','Stderr']:assert digest(f.parent/(stage['Name']+'.'+stream.lower()))==stage[stream+'Sha256']
        for row in build['InstalledFiles']:assert digest(resolved(runtime,row['Path']))==row['Sha256']
        installed=list(runtime.glob('lib/stdlib-*/ebin/queue.beam'));assert len(installed)==1;installed=installed[0].resolve()
        versions=list((runtime/'releases').glob('*/OTP_VERSION'));assert any(x.read_text().strip()=='29.1.1' for x in versions)
        installed_sha=digest(installed);(out/'queue-installed.beam').write_bytes(installed.read_bytes())
        write(out/'inputs.json',dict(SubjectHead=a.expected_head,PinnedOtpCommit=shared.PIN,SourceInputsSha256=digest(manifest),ProfileSha256=digest(t/'profile.json'),ParseGateSha256=digest(gate),BuildReceiptSha256=digest(receipt),InstalledQueuePath=str(installed),InstalledQueueSha256=installed_sha,ErlPath=str(runtime/'bin/erl'),ErlSha256=digest(runtime/'bin/erl'),ErlcPath=str(runtime/'bin/erlc'),ErlcSha256=digest(runtime/'bin/erlc'),PinnedQueueSource=next(x for x in before['TrackedFiles'] if x['Path']=='lib/stdlib/src/queue.erl')))
        owned=load(r/'tools/Erlang.GuestConnectedSendObservations/owned_process.py','family_owned')
        decoder=load(t/'passive_etf_family.py','family_decoder')
        env=shared.sanitized_environment(os.environ)
        for key in ['GH_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_LIBRARY_PATH']:env.pop(key,None)
        env['ERLC_USE_SERVER']='false'
        def stage(name,command,directory,timeout):
            process=None;failure=None
            try:
                process=owned.OwnedProcess([str(x) for x in command],r,env,directory/name)
                process.wait(time.monotonic()+timeout,threading.Event().is_set)
            except BaseException as e:failure=repr(e)
            finally:
                if process is not None:
                    try:process.close()
                    except BaseException as e:failure=(failure or '')+';cleanup:'+repr(e)
                write(directory/(name+'.stage.json'),dict(Name=name,Failure=failure,Started=process is not None,Arguments=[str(x) for x in command]))
            proof=directory/(name+'.process.json')
            if proof.exists():
                stages.append(dict(Group=directory.name,Name=name,ReceiptSha256=digest(proof),StdoutSha256=digest(directory/(name+'.stdout')),StderrSha256=digest(directory/(name+'.stderr'))))
                if failure is None:
                    try:shared.require_normal_receipt(json.loads(proof.read_bytes()))
                    except BaseException as e:failure=repr(e)
            return failure
        def text(node):
            assert node[0]=='list' and all(x[0]=='integer' for x in node[1]);return ''.join(chr(x[1]) for x in node[1])
        for f in profile['Fixtures']:
            directory=out/f['Id'];directory.mkdir();ebin=directory/'ebin';ebin.mkdir();phase=f['Id']
            group=dict(Id=f['Id'],ExpectedRows=len(f['Names']),Acquired=False,VmSkipped=False)
            try:
                sources=[path_for(f)]+[resolved(t,x) for x in f['Companions']]
                options=f.get('OriginalCompilerOptions',[])
                assert isinstance(options,list) and len(options)==len(set(options)) and all(x=='nowarn_nomatch' for x in options)
                failure=stage('compile',[runtime/'bin/erlc','-Werror','+deterministic',*['+'+x for x in options],'-o',ebin,*sources],directory,90)
                if failure:group.update(Failure=failure,VmSkipped=True);continue
                env['FAMILY_OUTPUT']=str(directory);env['FAMILY_EBIN']=str(ebin)
                modules=[f['Module']]+[x.stem for x in sources[1:]]
                loads=','.join('{module,'+m+'}=code:load_abs(filename:join(E,"'+m+'"))' for m in modules)
                module=f['Module'];stack=f['StackModule'];entry=f['StackEntry'];names=f['NamesEntry'];worker=f['WorkerModule'];worker_entry=f['WorkerEntry']
                expression='O=os:getenv("FAMILY_OUTPUT"),E=os:getenv("FAMILY_EBIN"),'+loads+',{module,queue}=code:ensure_loaded(queue),{queue,B,F}=code:get_object_code(queue),ok=file:write_file(filename:join(O,"queue-loaded.beam"),B),M={family_provenance,code:root_dir(),code:which(queue),F,erlang:md5(B),queue:module_info(md5),code:which('+module+'),'+module+':'+names+'(),erlang:system_info(otp_release),erlang:system_info(schedulers),erlang:system_info(schedulers_online)},ok=file:write_file(filename:join(O,"metadata.etf"),term_to_binary(M)),Rows='+module+':'+f['Run']+'(),ok=file:write_file(filename:join(O,"returned.etf"),term_to_binary(Rows)),Stacks='+worker+':'+worker_entry+'('+stack+','+entry+'),ok=file:write_file(filename:join(O,"stack-observations.etf"),term_to_binary(Stacks)),halt().'
                failure=stage('observe',[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-eval',expression],directory,120)
                if failure:group['Failure']=failure;continue
                metadata=decoder.decode_exact((directory/'metadata.etf').read_bytes());assert metadata[0]=='tuple' and len(metadata[1])==11
                fields=metadata[1];assert fields[0]==('atom','family_provenance') and Path(text(fields[1])).resolve()==runtime
                assert Path(text(fields[2])).resolve()==Path(text(fields[3])).resolve()==installed
                assert fields[4]==('binary',hashlib.md5(installed.read_bytes()).digest()) and digest(directory/'queue-loaded.beam')==installed_sha==digest(installed)
                assert Path(text(fields[6])).resolve()==(ebin/(module+'.beam')).resolve()
                assert fields[7]==('list',tuple(('atom',x) for x in f['Names'])) and text(fields[8])=='29'
                assert fields[9:11]==(('integer',2),('integer',2))
                rows=decoder.decode_exact((directory/'returned.etf').read_bytes());assert rows[0]=='list' and len(rows[1])==len(f['Names'])
                for expected,row in zip(f['Names'],rows[1]):assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',expected)
                stack_rows=decoder.decode_exact((directory/'stack-observations.etf').read_bytes())
                assert stack_rows[0]=='list'
                if f['StackShape']=='depth_cases':
                    assert len(stack_rows[1])==5
                    for expected_depth,entry in zip([('atom','default'),('integer',0),('integer',1),('integer',2),('integer',8)],stack_rows[1]):
                        assert entry[0]=='tuple' and len(entry[1])==2 and entry[1][0]==expected_depth
                        nested=entry[1][1];assert nested[0]=='list' and len(nested[1])==len(f['Names'])
                        for name,row in zip(f['Names'],nested[1]):assert row[0]=='tuple' and len(row[1])==2 and row[1][0]==('atom',name)
                elif f['StackShape']=='old59_twelve':
                    require_old59_stack_rows(stack_rows)
                else:raise AssertionError('Unknown full-stack fixture shape')
                group.update(Acquired=True,ReturnedSha256=digest(directory/'returned.etf'),RawStacksSha256=digest(directory/'stack-observations.etf'),MetadataSha256=digest(directory/'metadata.etf'),CompiledFiles=[dict(Module=m,Sha256=digest(ebin/(m+'.beam'))) for m in modules])
            except BaseException as e:group['Failure']=repr(e)
            finally:groups.append(group)
        assert shared.source_identity(otp)==before
        for row in plan['ToolInputs']:assert digest(resolved(t,row['Path']))==row['Sha256']
        for row in plan['RepositoryInputs']:assert digest(resolved(r,row['Path']))==row['Sha256']
        failed=[g for g in groups if not g['Acquired']]
        write(out/'completion.json',dict(Status='PASS_ORIGINAL_CAPTURE_PENDING_COMPARISON' if not failed else 'FAIL_ORIGINAL_CAPTURE_GROUPS_RETAINED',SubjectHead=a.expected_head,PinnedOtpCommit=shared.PIN,SourceInputsSha256=digest(manifest),ProfileSha256=digest(t/'profile.json'),BuildReceiptSha256=digest(receipt),Groups=groups,OwnedStages=stages,FailedGroups=len(failed),AllGroupsAttempted=len(groups)==len(profile['Fixtures']),CompatibilityPromoted=False,OriginalBuildsByCollector=0))
        if failed:raise RuntimeError('Independent original groups failed; raw outputs retained.')
    except BaseException as e:
        write(out/'failure.json',dict(Status='FAILED_RAW_RETAINED',Phase=phase,Error=repr(e),Groups=groups,OwnedStages=stages));raise
if __name__=='__main__':main()
