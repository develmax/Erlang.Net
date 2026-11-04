import hashlib,json,os,subprocess,sys
from pathlib import Path
repo=Path.cwd();tool=repo/'tools/Native/Erlang.NifVersion651Observations'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
# These four real Linux verifiers are independent source-only checks.
source_out=repo/'artifacts/nif-version651/ci/reused-source-preflight'
source_out.mkdir(parents=True,exist_ok=True)
source_results=[]
for label,name in [('entry','Erlang.NifEntryOriginalObservations'),('term','Erlang.NifTermCallbackOriginalObservations'),('text','Erlang.NifTextBinaryOriginalObservations'),('unix','Erlang.NifUnixFilenameObservations')]:
    status=dict(Name=label,Tool=name,Passed=False,OriginalExecuted=False,ManagedExecuted=False)
    stdout=b'';stderr=b''
    try:
        folder=repo/'tools/Native'/name;manifest=folder/'manifest.json'
        argv=[sys.executable,'-B',str(folder/'verify-source.py'),'--expected-head',os.environ['EXPECTED_HEAD']]
        if label=='unix':argv+=['--source-manifest-sha256',sha(manifest)]
        else:argv+=['--repository',str(repo),'--manifest-sha256',sha(manifest)]
        environment=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',GITHUB_SHA=os.environ['EXPECTED_HEAD'])
        result=subprocess.run(argv,cwd=repo,env=environment,capture_output=True,timeout=45)
        stdout=result.stdout;stderr=result.stderr
        status.update(Passed=result.returncode==0,ExitCode=result.returncode,ManifestSha256=sha(manifest))
    except Exception as error:status['Error']=repr(error)
    (source_out/(label+'.stdout')).write_bytes(stdout)
    (source_out/(label+'.stderr')).write_bytes(stderr)
    status.update(StdoutSha256=hashlib.sha256(stdout).hexdigest().upper(),StderrSha256=hashlib.sha256(stderr).hexdigest().upper())
    source_results.append(status)
aggregate=dict(Passed=all(x['Passed'] for x in source_results),SubjectHead=os.environ['EXPECTED_HEAD'],SourceOnly=True,OriginalExecuted=False,ManagedExecuted=False,Results=source_results)
(source_out/'completion.json').write_text(json.dumps(aggregate,indent=2))
assert aggregate['Passed'],aggregate
for name,pin in [('linux-build-inputs.json',os.environ['NATIVE_PLAN']),('linux-inputs.json',os.environ['CAPTURE_PLAN'])]:
    path=tool/name;assert sha(path)==pin
    plan=json.loads(path.read_bytes());assert plan['Pin']==os.environ['OTP_PIN']
    for row in plan['Inputs']:
        root=tool if row['Scope']=='Tool' else repo
        p=(root/row['Path']).resolve();assert p.is_relative_to(root) and sha(p)==row['Sha256'],row['Path']
acceptance=repo/'tools/Native/Erlang.NifLinuxCallbackObservations/acceptance-inputs.json'
assert sha(acceptance)==os.environ['ACCEPTANCE_PLAN']
ap=json.loads(acceptance.read_bytes());assert ap['Pin']==os.environ['OTP_PIN'] and ap['N1BuildPlanSha256']==os.environ['NATIVE_PLAN'] and ap['OriginalPlanSha256']==os.environ['ORIGINAL_PLAN']
for row in ap['Files']:
    p=(repo/row['Path']).resolve();assert p.is_relative_to(repo) and p.stat().st_size==row['Bytes'] and sha(p)==row['Sha256'],row['Path']
source=repo/'tools/Runtime/Erlang.SchedulerAffinityOriginalObservations'
assert sha(source/'manifest.json')==os.environ['ORIGINAL_PLAN']
original=json.loads((source/'manifest.json').read_bytes());assert original['OriginalCommit']==os.environ['OTP_PIN']
for row in original['Files']+original['Dependencies']+[original['Workflow']]:
    root=source if row in original['Files'] else repo
    assert sha(root/row['Path'])==row['Sha256'],row['Path']
tracked=subprocess.check_output(['git','--no-optional-locks','ls-files','-z']).decode().split('\0')
rows=[]
for name in tracked:
    p=repo/name
    if name and (p.suffix in {'.cs','.csproj','.props','.targets','.sln','.slnx'} or name=='global.json'):
        assert p.is_file() and not p.is_symlink()
        rows.append(dict(Path=name,Sha256=sha(p)))
assert rows
out=repo/'artifacts/nif-version651/ci'
(out/'source-freeze.json').write_text(json.dumps(dict(SubjectHead=os.environ['EXPECTED_HEAD'],Rows=rows),indent=2))
(out/'scope.json').write_text(json.dumps(dict(SubjectHead=os.environ['EXPECTED_HEAD'],OtpPin=os.environ['OTP_PIN'],NativePlan=os.environ['NATIVE_PLAN'],CapturePlan=os.environ['CAPTURE_PLAN'],FocusedN1Only=True,PendingGates=['legacy68 callback comparisons','pthread bindings/create/join','ordinary-loader3 and cleanup'],WholeLoadNifCompatible=False),indent=2))
