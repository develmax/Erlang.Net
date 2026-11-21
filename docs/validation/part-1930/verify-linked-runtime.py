from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-entry-and-short-returns-1930')
refs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed')
out=stage/'linked-runtime-witnesses'
out.mkdir()
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def until(filename):
    deadline=time.monotonic()+2500
    path=stage/filename
    while not (path.is_file() and path.stat().st_size):
        if time.monotonic()>deadline:
            raise TimeoutError(path)
        time.sleep(3)
    return path
def execute(recipe,args,label):
    env={**os.environ,'ERL_CRASH_DUMP':str(out/(label+'-crash.dump'))}
    command=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',
             recipe.read_text(encoding='utf-8-sig'),'-extra',*[arg.as_posix() for arg in args]]
    with (out/(label+'.stdout.log')).open('wb') as stdout, (out/(label+'.stderr.log')).open('wb') as stderr:
        subprocess.run(command,check=True,cwd=out,env=env,stdout=stdout,stderr=stderr,timeout=60)
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
pilot=read(until('pilot-receipt.json'))
assert pilot['Count']==55 and pilot['ExitCode'] in (0,1)
gen=stage/'pilot.json.generated'
errors=[]
for name in ('BrokenMini.asn1','SyntaxMini.asn1'):
    path=out/name
    shutil.copyfile(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-generator-label-planning-1928-retake')/name,path)
    errors.append(path)
roles=[('Reference',refs),('Candidate',gen)]
witnesses={}
for name,relative,recipe,count in (
    ('test_server_io','lib/common_test-1.31.2/ebin/test_server_io.beam',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-short-return-continuations-1930/observe-transaction-prints.eval'),3),
    ('counters','lib/erts-17.1/ebin/counters.beam',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/observe-counters.eval'),6),
    ('asn1ct','lib/asn1-5.5.2/ebin/asn1ct.beam',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-generator-label-planning-1928-retake/observe-invalid-asn.eval'),2),
):
    outcomes={}
    for role,root in roles:
        path=out/f'{name}-{role}-observation.terms'
        extra=[]
        if name=='test_server_io':
            extra=[out/f'{role}-transaction-printed.log']
        if name=='asn1ct':
            failed=out/f'asn1ct-{role}-failed'
            failed.mkdir()
            extra=[*errors,failed]
        execute(recipe,[root/relative,*extra,path],name+'-'+role)
        outcomes[role]=sha(path)
    assert outcomes['Reference']==outcomes['Candidate'],name
    witnesses[name]=dict(Beam=sha(gen/relative),Observations=count,Outcome=outcomes['Candidate'])
fixture=out/'protected-projection'
fixture.mkdir()
base=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/Before')
for file in base.iterdir():
    if file.is_file() and file.suffix in ('.dll','.json'):
        shutil.copyfile(file,fixture/file.name)
compiler=stage/'corpus-bin/Erlang.Compiler.dll'
shutil.copyfile(compiler,fixture/compiler.name)
source=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/protected_projection_probe.erl')
image=fixture/'protected_projection_probe.beam'
with (fixture/'compile.stdout.log').open('wb') as stdout, (fixture/'compile.stderr.log').open('wb') as stderr:
    subprocess.run(['D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe',str(fixture/'probe.dll'),str(source),str(image)],
                   check=True,stdout=stdout,stderr=stderr,cwd=fixture,timeout=60)
output=fixture/'candidate.terms'
execute(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/observe-protected-projection.eval'),[image,output],'synthetic-protected')
assert sha(output)==sha(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/Reference/native.terms'))
freeze=read(until('source-freeze.json'))
assert sha(compiler).lower()==freeze['Compiler'].lower()
result=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
            Synthetic=dict(Beam=sha(image),Observations=8,Outcome=sha(output)))
(out/'receipt.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')

