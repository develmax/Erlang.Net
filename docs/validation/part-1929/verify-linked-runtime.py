from pathlib import Path
import hashlib,json,os,shutil,subprocess,sys,time
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/frame-and-generator-planning-1929-retake')
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
def execute(recipe, args, name):
    env={**os.environ, 'ERL_CRASH_DUMP':str(out/(name+'-crash.dump'))}
    cmd=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',recipe.read_text(encoding='utf-8-sig'),'-extra',*[x.as_posix() for x in args]]
    with (out/(name+'.stdout.log')).open('wb') as stdout, (out/(name+'.stderr.log')).open('wb') as stderr:
        subprocess.run(cmd,check=True,cwd=out,env=env,stdout=stdout,stderr=stderr,timeout=60)
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
pilot=read(until('pilot-receipt.json'))
assert pilot['Count']==54 and pilot['ExitCode'] in (0,1)
gen=stage/'pilot.json.generated'
relative='lib/asn1-5.5.2/ebin/asn1ct.beam'
recipes=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/missing-helpers-by-source-kind-1927/two-generator-ownership/real-asn1ct')
schema=out/'TwinGenMini.asn1'
shutil.copyfile(recipes/schema.name,schema)
errors=[]
for name in ('BrokenMini.asn1','SyntaxMini.asn1'):
    path=out/name
    shutil.copyfile(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-generator-label-planning-1928-retake')/name,path)
    errors.append(path)
reuse=[
 ('wxSizer','lib/wx-2.7/ebin/wxSizer.beam',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/mapped-class-index-dispatch-1948/observe-mapped.eval'),
     [Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/record-proof-domination-1947/stubs')],16),
 ('test_server_io','lib/common_test-1.31.2/ebin/test_server_io.beam',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nearest-code-and-frame-1929/observe-transaction.eval'),[],4),
 ('counters','lib/erts-17.1/ebin/counters.beam',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/protected-case-projection-liveness-1929/observe-counters.eval'),[],6),
]
witnesses={}
for name,rel,recipe,args,count in reuse:
    for role,root in [('Reference',refs),('Candidate',gen)]:
        image=root/rel
        execute(recipe,[image,*args,out/f'{name}-{role}.terms'],f'{name}-{role}')
    actual=sha(out/f'{name}-Candidate.terms')
    assert actual==sha(out/f'{name}-Reference.terms'),name
    witnesses[name]=dict(Beam=sha(gen/rel),Observations=count,Outcome=actual)
generated=out/'asn1ct-modes'
generated.mkdir()
direct=out/'read-asn1ct-disassembly.eval'
direct.write_text('''[P,Out]=init:get_plain_arguments(),{beam_file,asn1ct,_,_,_,Fs}=beam_disasm:file(P),
ok=file:write_file(Out,io_lib:format("~p.~n",[length(Fs)])),halt().''',encoding='utf-8')
for role,root in [('Reference',refs),('Candidate',gen)]:
    mode=generated/role
    mode.mkdir()
    image=root/relative
    execute(direct,[image,out/f'asn1ct-{role}-function-count.terms'],f'asn1ct-disasm-{role}')
    execute(recipes/'observe.eval',[image,schema,mode],f'asn1ct-{role}')
    failed=generated/(role+'Errors')
    failed.mkdir()
    execute(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-generator-label-planning-1928-retake/observe-invalid-asn.eval'),[image,*errors,failed,out/f'asn1ct-{role}-errors.terms'],f'asn1ct-invalid-{role}')
with (out/'generated-comparison.log').open('wb') as log:
    subprocess.run([sys.executable,str(recipes/'compare-generated.py'),str(generated)],check=True,stdout=log,stderr=subprocess.STDOUT,timeout=30)
error=sha(out/'asn1ct-Candidate-errors.terms')
assert error==sha(out/'asn1ct-Reference-errors.terms')
assert int((out/'asn1ct-Candidate-function-count.terms').read_text().strip().rstrip('.'))>0
receipt=generated/'normalized-generate-receipt.json'
witnesses['asn1ct']=dict(Beam=sha(gen/relative),Observations=8,GeneratorOutput=sha(receipt),ErrorOutput=error)
identity=read(until('source-freeze.json'))
(out/'receipt.json').write_text(json.dumps(dict(Compiler=identity['Compiler'],Runtime=identity['Runtime'],Witnesses=witnesses),indent=2)+'\n',encoding='utf-8')

