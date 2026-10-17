"""OS-loader regression only: own lazy-dlopen C baseline versus ordinary SDK loading."""
from pathlib import Path
import argparse, hashlib, json, os, shutil, subprocess, sys, threading, time
R = Path(__file__).resolve().parents[2]
T = Path(__file__).resolve().parent
H = lambda p: hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
J = lambda p: json.loads(Path(p).read_bytes())
def write(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, indent=2); stream.write('\n')
def main():
    parser = argparse.ArgumentParser()
    for key in ['expected-head','header-receipt','approved-header-receipt-sha256','headers','focus','bridge-sha256','output']:
        parser.add_argument('--'+key, required=True)
    parser.add_argument('--thread-destructors', action='store_true')
    a = parser.parse_args()
    head = subprocess.check_output(['git','--no-optional-locks','-C',str(R),'rev-parse','HEAD'], text=True, timeout=20).strip()
    assert head == a.expected_head
    receipt = Path(a.header_receipt).resolve()
    assert H(receipt) == a.approved_header_receipt_sha256.upper()
    headerproof = J(receipt)
    assert headerproof['Status'] == 'PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD'
    assert headerproof['PinnedOtpCommit'] == 'ad05823719d77c8faee87348ea39513d4e2f99c5'
    headers = Path(a.headers).resolve()
    assert len(headerproof['HeaderInputs']) == 4
    for row in headerproof['HeaderInputs']:
        assert H(headers/row['Name']) == row['Sha256']
    focus = Path(a.focus).resolve()
    assert focus.name == 'Erlang.NifOrdinaryLoaderFocus.dll' and focus.is_file()
    assert H(focus.parent/'Erlang.NifBridge.so') == a.bridge_sha256.upper()
    output = Path(a.output).resolve(); output.mkdir(parents=True, exist_ok=False)
    for tool in ['Erlang.NifEntryOriginalObservations','Erlang.GuestConnectedSendObservations','Erlang.SchedulerAffinityOriginalObservations']:
        sys.path.insert(0,str(R/'tools'/tool))
    import common, owned_process, source_inputs, passive_etf
    env = source_inputs.sanitized_environment(os.environ)
    for key in ['GH_TOKEN','GITHUB_TOKEN','LD_PRELOAD','LD_LIBRARY_PATH','DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS','DOTNET_SHARED_STORE']:
        env.pop(key,None)
    env.update(DOTNET_PROCESSOR_COUNT='2',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    gcc = shutil.which('gcc'); dotnet = shutil.which('dotnet'); readelf = shutil.which('readelf')
    assert gcc and dotnet and readelf
    dlls = [dict(Path=str(path),Sha256=H(path)) for path in sorted(focus.parent.glob('*.dll'))]
    sourcefiles = sorted((T/'native').glob('*.c')) + sorted((T/'native').glob('*.cpp')) + sorted((T/'original').glob('*.erl'))
    inputs = dict(ExecutingHead=head,HeaderProducerHead=headerproof['SubjectHead'],HeaderReceiptSha256=H(receipt),HeaderInputs=headerproof['HeaderInputs'],Sources=[dict(Path=str(path),Sha256=H(path)) for path in sourcefiles],AdjacentDlls=dlls,BridgeSha256=a.bridge_sha256.upper(),OriginalOtpComparisonExecuted=False)
    write(output/'inputs.json', inputs)
    stages=[]
    def stage(argv,prefix,environment=env,expected=0):
        process=None; failure=None
        try:
            process=owned_process.OwnedProcess([str(x) for x in argv],R,environment,prefix)
            process.wait(time.monotonic()+180,threading.Event().is_set)
        except BaseException as error:
            failure=repr(error)
        finally:
            if process is not None: process.close()
        proof=J(prefix.with_suffix('.process.json'))
        assert proof['ExitCode']==expected and proof['ParentReaped'] and proof['StreamsClosed']
        assert not proof['TerminationRequested'] and not proof['RemainingGroupKillRequested'] and proof['CleanupFailure'] is None
        if expected==0: assert failure is None
        else: assert failure is not None
        row=dict(Arguments=[str(x) for x in argv],ExpectedExit=expected,ObservedFailure=failure,ReceiptSha256=H(prefix.with_suffix('.process.json')),StdoutSha256=H(prefix.with_suffix('.stdout')),StderrSha256=H(prefix.with_suffix('.stderr')))
        stages.append(row); return proof
    baseline=output/'plain-baseline'
    stage([gcc,'-O2','-std=c11','-Wall','-Wextra','-Werror',T/'native/baseline.c','-ldl','-o',baseline],output/'compile-baseline')
    absolute=output/'absolute path with spaces'; bare=output/'bare'; missing=output/'missing'
    for index,directory in enumerate([absolute,bare]):
        (directory/'deps').mkdir(parents=True)
        helper_compiler = shutil.which('g++') if a.thread_destructors else gcc
        assert helper_compiler
        helper_source = T/'native/thread-destructors.cpp' if a.thread_destructors else T/'native/dependency.c'
        helper_flags = ['-std=c++17','-pthread'] if a.thread_destructors else []
        stage([helper_compiler,'-shared','-fPIC','-O2','-Wall','-Wextra','-Werror',*helper_flags,helper_source,'-Wl,-soname,libordinary_path_dependency.so','-o',directory/'deps/libordinary_path_dependency.so'],output/('compile-helper-'+str(index)))
        stage([gcc,'-shared','-fPIC','-O2','-Wall','-Wextra','-Werror','-I',headers,T/'native/entry.c','-L'+str(directory/'deps'),'-lordinary_path_dependency','-Wl,-rpath,$ORIGIN/deps','-Wl,-z,lazy','-o',directory/'libordinary_path_probe.so'],output/('compile-image-'+str(index)))
        stage([readelf,'-d','-Ws',directory/'libordinary_path_probe.so'],output/('image-metadata-'+str(index)))
    missing.mkdir(); shutil.copyfile(absolute/'libordinary_path_probe.so',missing/'libordinary_path_probe.so')
    images=[dict(Path=str(path),Sha256=H(path)) for directory in [absolute,bare,missing] for path in sorted(directory.rglob('*.so'))]
    for path in absolute.rglob('*.so'): path.chmod(0o444)
    (absolute/'deps').chmod(0o555); absolute.chmod(0o555)
    results=[]
    for name,directory,isbare,success in [('origin-spaces-readonly',absolute,False,True),('bare-ld-library-path',bare,True,True),('missing-dependency',missing,False,False)]:
        caseenv=env.copy()
        if isbare: caseenv['LD_LIBRARY_PATH']=str(directory)
        library='libordinary_path_probe.so' if isbare else str(directory/'libordinary_path_probe.so')
        prefix=output/(name+'-baseline')
        stage([baseline,library],prefix,caseenv,0 if success else 20)
        baselinevalue=int(prefix.with_suffix('.stdout').read_text().strip()) if success else None
        if a.thread_destructors and success: assert baselinevalue == 1111
        if not success: assert 'libordinary_path_dependency.so' in prefix.with_suffix('.stderr').read_text()
        sdkout=output/(name+'-sdk')
        stage([dotnet,focus,T/'original/ordinary_loader_probe.erl',H(T/'original/ordinary_loader_probe.erl'),library[:-3],sdkout,a.bridge_sha256.upper()],output/(name+'-managed'),caseenv)
        actual=J(sdkout/'completion.json')
        assert actual['Status']=='ACQUIRED_ORDINARY_OS_LOADER_CONTROL' and actual['RuntimeDisposalsCompleted'] and actual['Loaded']==success
        if success:
            assert actual['NativeImageRetirementCompleted'] and int(actual['ActualValue'])==baselinevalue
            assert passive_etf.decode_exact((sdkout/'returned.etf').read_bytes())==('integer',baselinevalue)
        else:
            raw=passive_etf.decode_exact((sdkout/'load.etf').read_bytes())
            assert raw[0]=='tuple' and raw[1][0]==('atom','error') and 'load_failed' in repr(raw)
        results.append(dict(Name=name,BaselineValue=baselinevalue,ManagedValue=actual['ActualValue'],Loaded=actual['Loaded'],LoadTermSha256=H(sdkout/'load.etf')))
    for row in images+dlls: assert H(row['Path'])==row['Sha256']
    for row in inputs['Sources']: assert H(row['Path'])==row['Sha256']
    write(output/'completion.json',dict(Status='PASS_THREE_ORDINARY_OS_LOADER_REGRESSIONS',ExecutingHead=head,InputsSha256=H(output/'inputs.json'),Images=images,OwnedStages=stages,Cases=results,ThreadDestructorControl=a.thread_destructors,OriginalOtpComparisonExecuted=False,WholeOrdinaryLoaderCompatibilityProved=False))
if __name__=='__main__': main()
