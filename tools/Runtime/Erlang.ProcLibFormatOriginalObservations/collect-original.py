"""Capture raw original OTP outcomes. Never runs SDK code or derives expected values."""
import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import signal
import subprocess
import sys
import time

sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
VERSION = '29.1.1'
TOOL = 'tools/Runtime/Erlang.ProcLibFormatOriginalObservations'
MAX_ETF_BYTES = 16 * 1024 * 1024
MODULES = ['proc_lib', 'io_lib', 'io_lib_pretty', 'io_lib_format', 'erl_error', 'string', 'unicode_util', 'io', 'file']


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def binding(path, root=None):
    return dict(Path=path.relative_to(root).as_posix() if root else str(path), Bytes=path.stat().st_size, Sha256=sha(path))


def save(path, value):
    with path.open('xb') as stream:
        stream.write((json.dumps(value, indent=2) + '\n').encode())


def safe_file(root, name):
    require(type(name) is str and name and not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and '\\' not in name and ':' not in name, 'Unsafe relative source path')
    path = (root / name).resolve()
    require(path.is_relative_to(root) and path.is_file(), 'Missing or escaping source: ' + name)
    return path


def git(root, *arguments):
    return subprocess.check_output(['git', '-C', str(root), *arguments], timeout=30)


def verify(repository, manifest_sha, expected_head=None):
    root = repository.resolve(strict=True)
    tool = root / TOOL
    require(re.fullmatch('[0-9a-fA-F]{64}', manifest_sha or ''), 'Explicit source manifest SHA256 required')
    manifest_path = safe_file(tool, 'source-manifest.json')
    require(sha(manifest_path) == manifest_sha.upper(), 'Source manifest hash mismatch')
    manifest = json.loads(manifest_path.read_bytes())
    require(manifest['SchemaVersion'] == 1 and manifest['PinnedOtpCommit'] == PIN and manifest['SdkExecuted'] is False and manifest['OriginalExecuted'] is False, 'Source manifest scope mismatch')
    require(binding(safe_file(root, manifest['Workflow']['Path']), root) == manifest['Workflow'], 'Workflow source hash mismatch')
    rows = manifest['Files']
    require(len(rows) == len({row['Path'] for row in rows}), 'Duplicate source manifest entry')
    actual = {path.relative_to(tool).as_posix() for path in tool.rglob('*') if path.is_file() and path != manifest_path}
    require(actual == {row['Path'] for row in rows}, 'Exact collector source closure mismatch')
    for row in rows:
        require(binding(safe_file(tool, row['Path']), tool) == row, 'Source hash/size mismatch: ' + row['Path'])
    catalog = json.loads((tool / 'cases.json').read_bytes())
    require(catalog['PinnedOtpCommit'] == PIN and catalog['ExpectedValuesPresent'] is False and catalog['ReportCases'] == 57 and catalog['PrimitiveOriginalOnlyCases'] == 19, 'Case catalog scope mismatch')
    cases = catalog['Cases']
    require(len(cases) == 76 and len({(row['Track'], row['Name']) for row in cases}) == 76, 'Expected unique57+19 cases')
    require(sum(row['Track'] == 'report' for row in cases) == 57 and sum(row['Track'] == 'primitive-original-only' for row in cases) == 19, 'Case tracks mismatch')
    for row in cases:
        require(re.fullmatch('[a-z0-9][a-z0-9-]*', row['Name']), 'Unsafe case name')
        source = safe_file(tool, row['Path'])
        require(binding(source, tool) == {key: row[key] for key in ['Path', 'Bytes', 'Sha256']}, 'Case source drift')
        require(module_name(source.read_bytes()) in ['oracle_proc_format', 'oracle_proc_format_primitive'], 'Unexpected fixture module')
    require(sha(tool / 'combined4-source-freeze.json') == catalog['CombinedSourceFreezeSha256'], 'Combined source snapshot mismatch')
    if expected_head is not None:
        require(re.fullmatch('[0-9a-f]{40}', expected_head), 'Exact checkout commit required')
        require(git(root, 'rev-parse', 'HEAD').decode().strip() == expected_head, 'Checkout head mismatch')
    return root, tool, catalog, manifest_path


def module_name(source):
    matches = re.findall(rb'(?m)^-module\(([a-z][a-z0-9_]*)\)\.', source)
    require(len(matches) == 1, 'Exactly one reviewed module declaration required')
    return matches[0].decode('ascii')


def clean_environment(output):
    environment = os.environ.copy()
    for key in ['ERL_FLAGS', 'ERL_AFLAGS', 'ERL_ZFLAGS', 'ERL_LIBS', 'ERL_COMPILER_OPTIONS']:
        environment.pop(key, None)
    environment.update(ERLC_USE_SERVER='false', ERL_CRASH_DUMP=str(output / 'erl_crash.dump'), ERLANG_NET_FORMAT_CAPTURE=str(output))
    return environment


def run_owned(directory, label, arguments, timeout, environment, cwd):
    started = time.monotonic()
    save(directory / (label + '.attempt.json'), dict(Arguments=arguments,WorkingDirectory=str(cwd),TimeoutSeconds=timeout,StartedUnix=time.time(),NewOwnedSession=True,SelectedEnvironment={key:environment[key] for key in ['ERLC_USE_SERVER','ERL_CRASH_DUMP','ERLANG_NET_FORMAT_CAPTURE']}))
    process = subprocess.Popen(arguments, cwd=cwd, env=environment, stdin=subprocess.DEVNULL, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
    timed_out = False
    try:
        output, error = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired:
        timed_out = True
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        output, error = process.communicate(timeout=10)
    (directory / (label + '.stdout')).write_bytes(output)
    (directory / (label + '.stderr')).write_bytes(error)
    receipt = dict(ExitCode=process.returncode,TimedOut=timed_out,Settled=process.poll() is not None,OwnedPid=process.pid,ElapsedSeconds=time.monotonic()-started,Stdout=binding(directory/(label+'.stdout'),directory),Stderr=binding(directory/(label+'.stderr'),directory))
    save(directory / (label + '.process.json'), receipt)
    return receipt


def validate_etf(path):
    data = path.read_bytes()
    require(1 < len(data) <= MAX_ETF_BYTES and data[0] == 131, 'Missing/bad ETF version or capture size')
    return binding(path)


def provenance(args, root, tool, out):
    require(sys.platform == 'linux', 'Original collection uses reviewed Linux process-group ownership')
    otp = args.otp_source.resolve(strict=True)
    require(git(otp, 'rev-parse', 'HEAD').decode().strip() == PIN, 'Pinned original commit mismatch')
    require(not git(otp, 'status', '--porcelain', '--untracked-files=no').strip(), 'Tracked OTP source modified')
    erl = args.erl.resolve(strict=True)
    installed = erl.parent.parent
    version_files = sorted((installed / 'releases').glob('*/OTP_VERSION'))
    require(len([path for path in version_files if path.read_text().strip() == VERSION]) == 1, 'Exact installed OTP_VERSION missing')
    erlc = args.erl.parent / 'erlc'
    require(erlc.is_file() and erlc.resolve().is_relative_to(installed), 'Compiler must belong to same original installation')
    erts = list(installed.glob('erts-*/bin/beam.smp'))
    require(len(erts) == 1, 'Exactly one installed BEAM runtime required')
    erlexec = erts[0].with_name('erlexec')
    require(erlexec.is_file(), 'Installed erlexec missing')
    env = clean_environment(out)
    expression = 'lists:foreach(fun(M)->io:format("MODULE ~s ~s~n",[atom_to_list(M),code:which(M)]) end,[' + ','.join(MODULES) + ']),io:format("OTP ~s~nERTS ~s~nPRINTABLE ~p~n",[erlang:system_info(otp_release),erlang:system_info(version),io:printable_range()]),halt(0).'
    receipt = run_owned(out,'runtime-control',[str(args.erl),'+S','2:2','+pc','latin1','-noshell','-noinput','-boot','start_clean','-eval',expression],args.timeout_seconds,env,out)
    require(receipt['ExitCode'] == 0 and not receipt['TimedOut'] and receipt['Settled'], 'Original runtime control failed')
    text = (out/'runtime-control.stdout').read_text()
    require(re.search(r'(?m)^OTP 29$',text) and re.search(r'(?m)^PRINTABLE latin1$',text), 'Runtime control version/printable range mismatch')
    modules = []
    for module in MODULES:
        matches = re.findall(r'(?m)^MODULE '+module+r' (.+)$',text)
        require(len(matches) == 1, 'Module provenance missing: '+module)
        path = Path(matches[0]).resolve(strict=True)
        require(path.is_relative_to(installed) and path.suffix == '.beam', 'Module outside pinned installation: '+module)
        modules.append(dict(Module=module,**binding(path)))
    combined = json.loads((tool/'combined4-source-freeze.json').read_bytes())
    references = []
    for row in combined['PinnedSources']:
        path = safe_file(otp,row['Path'])
        require(binding(path,otp) == row, 'Pinned source reference mismatch: '+row['Path'])
        references.append(row)
    report = dict(PinnedCommit=PIN,InstalledOtpVersion=VERSION,ActualCheckoutCommit=git(root,'rev-parse','HEAD').decode().strip(),Workflow={key:os.environ.get(key) for key in ['GITHUB_SHA','GITHUB_RUN_ID','GITHUB_RUN_ATTEMPT','GITHUB_JOB']},OriginalOnly=True,SdkExecuted=False,DefaultPrintableRangeExplicit='latin1',SourceReferences=references,OracleLauncher=binding(args.erl),ResolvedOracle=binding(erl),Compiler=binding(erlc),ResolvedCompiler=binding(erlc.resolve()),Runtime=binding(erts[0]),Erlexec=binding(erlexec),InstalledOtpVersionFiles=[binding(path) for path in version_files],LoadedModules=modules,SourceManifest=binding(tool/'source-manifest.json'))
    save(out/'original-provenance.json',report)
    return erlc


def capture(args, root, tool, catalog, manifest_path):
    require(args.output is not None and args.otp_source is not None and args.erl is not None and args.expected_head is not None, 'Collect requires output, original source, original erl and exact checkout head')
    require(1 <= args.timeout_seconds <= 120, 'Per-process timeout must be1..120seconds')
    args.otp_source = args.otp_source.resolve(strict=True)
    args.erl = args.erl.absolute()
    out = args.output.resolve()
    out.mkdir(parents=True,exist_ok=False)
    erlc = provenance(args,root,tool,out)
    rows = []
    for case in catalog['Cases']:
        name = case['Track'] + '--' + case['Name']
        directory = out/name
        directory.mkdir()
        source_directory = directory/'source'
        source_directory.mkdir()
        beam_directory = directory/'beam'
        beam_directory.mkdir()
        source = safe_file(tool,case['Path'])
        module = module_name(source.read_bytes())
        copied = source_directory/(module+'.erl')
        copied.write_bytes(source.read_bytes())
        environment = clean_environment(directory)
        compile_receipt = run_owned(directory,'compile',[str(erlc),'-o',str(beam_directory),str(copied)],args.timeout_seconds,environment,directory)
        row = dict(Track=case['Track'],Name=case['Name'],Source=binding(source,tool),SourceReturnProfile=case['SourceReturnProfile'],CopiedSource=binding(copied,directory),Module=module,Scope='Original-only primitive observation for future HOST backend' if case['Track']=='primitive-original-only' else 'Original report fixture observation; source wrapper determines flattened versus raw return',Compile=compile_receipt,OriginalVmLaunched=False,OriginalExecuted=False,ExpectedValue=None,SdkCompared=False,RawNormalization=False)
        if compile_receipt['ExitCode'] != 0 or compile_receipt['TimedOut'] or not compile_receipt['Settled']:
            row['Status']='COMPILE_FAILED'
        else:
            beam=beam_directory/(module+'.beam')
            if not beam.is_file():
                row['Status']='COMPILE_OUTPUT_MISSING'
                save(directory/'observation.json',row)
                rows.append(row)
                save(out/('progress-'+str(len(rows)).zfill(3)+'.json'),dict(Completed=len(rows),Planned=len(catalog['Cases']),Cases=rows))
                continue
            row['Beam']=binding(beam,directory)
            expression = 'D=os:getenv("ERLANG_NET_FORMAT_CAPTURE"),R=try '+module+':run() of V->{returned,V} catch C:E:S->{raised,C,E,S} end,case R of {returned,Returned}->ok=file:write_file(filename:join(D,"returned.etf"),term_to_binary(Returned));{raised,Class,Reason,Stack}->ok=file:write_file(filename:join(D,"raised.etf"),term_to_binary({Class,Reason,Stack})) end,ok=file:write_file(filename:join(D,"outcome.etf"),term_to_binary(R)),halt(0).'
            run_receipt=run_owned(directory,'run',[str(args.erl),'+S','2:2','+pc','latin1','-noshell','-noinput','-boot','start_clean','-pa',str(beam_directory),'-eval',expression],args.timeout_seconds,environment,directory)
            row['Run']=run_receipt
            row['OriginalVmLaunched']=True
            if run_receipt['ExitCode'] != 0 or run_receipt['TimedOut'] or not run_receipt['Settled']:
                row['Status']='PROCESS_FAILED'
            else:
                outcomes=[directory/name for name in ['returned.etf','raised.etf'] if (directory/name).is_file()]
                try:
                    require(len(outcomes)==1 and (directory/'outcome.etf').is_file(),'Expected one raw returned or raised observation')
                    row['RawValueEtf']=validate_etf(outcomes[0])
                    row['RawOutcomeEtf']=validate_etf(directory/'outcome.etf')
                    row['Status']='OBSERVED_RETURNED' if outcomes[0].name=='returned.etf' else 'OBSERVED_RAISED'
                    row['OriginalExecuted']=True
                except (ValueError,OSError) as error:
                    row['Status']='CAPTURE_INVALID'
                    row['CaptureError']=str(error)
        save(directory/'observation.json',row)
        rows.append(row)
        save(out/('progress-'+str(len(rows)).zfill(3)+'.json'),dict(Completed=len(rows),Planned=len(catalog['Cases']),Cases=rows))
        print(json.dumps(dict(Track=row['Track'],Name=row['Name'],Status=row['Status'])),flush=True)
    verify(root,args.manifest_sha256,args.expected_head)
    require(git(args.otp_source,'rev-parse','HEAD').decode().strip()==PIN and not git(args.otp_source,'status','--porcelain','--untracked-files=no').strip(),'Pinned source changed after observations')
    failures=[row for row in rows if row['Status'] not in ['OBSERVED_RETURNED','OBSERVED_RAISED']]
    report=dict(SchemaVersion=1,Status='ORIGINAL_OBSERVATIONS_COMPLETE' if not failures else 'ORIGINAL_OBSERVATIONS_INCOMPLETE',ActualCheckoutCommit=args.expected_head,PinnedOtpCommit=PIN,SourceManifest=binding(manifest_path),Planned=76,ReportPlanned=57,PrimitiveOriginalOnlyPlanned=19,Executed=sum(row['OriginalExecuted'] for row in rows),VmLaunched=sum(row['OriginalVmLaunched'] for row in rows),ObservedReturned=sum(row['Status']=='OBSERVED_RETURNED' for row in rows),ObservedRaised=sum(row['Status']=='OBSERVED_RAISED' for row in rows),InfrastructureFailures=len(failures),Cases=rows,OriginalObservationOnly=True,IndependentExpectedValuesEstablished=False,SdkExecuted=False,SdkCompared=False,RawReturnedIolistEtfUnmodified=True,RawNormalization=False,RuntimeBeamDependencyAdded=False)
    save(out/'observations.json',report)
    return 0 if not failures else 1


def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode',choices=['verify','collect'])
    parser.add_argument('--repository',type=Path,required=True)
    parser.add_argument('--manifest-sha256',required=True)
    parser.add_argument('--expected-head')
    parser.add_argument('--otp-source',type=Path)
    parser.add_argument('--erl',type=Path)
    parser.add_argument('--output',type=Path)
    parser.add_argument('--timeout-seconds',type=int,default=30)
    args=parser.parse_args(argv)
    try:
        root,tool,catalog,manifest=verify(args.repository,args.manifest_sha256,args.expected_head)
        if args.mode=='verify':
            print(json.dumps(dict(Status='SOURCE_CLOSURE_VERIFIED',Cases=len(catalog['Cases']),OriginalExecuted=False,SdkExecuted=False,ManifestSha256=sha(manifest))))
            return 0
        return capture(args,root,tool,catalog,manifest)
    except (ValueError,OSError,KeyError,subprocess.SubprocessError) as error:
        print('COLLECTOR_REFUSED: '+str(error),file=sys.stderr)
        return 2


if __name__=='__main__':
    raise SystemExit(main())
