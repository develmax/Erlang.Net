from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/proof-elision-and-destinations-1935')
refs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed')
root=stage/'linked-runtime-witnesses'
root.mkdir()
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def until(filename):
    stop=time.monotonic()+2700
    file=stage/filename
    while not (file.is_file() and file.stat().st_size):
        if time.monotonic()>stop:
            raise TimeoutError(file)
        time.sleep(3)
    return file
def run(cmd,folder,label):
    env={**os.environ,'ERL_CRASH_DUMP':str(folder/(label+'-crash.dump')),'ERL_FLAGS':'+S 1:1 +A 1'}
    with (folder/(label+'.stdout.log')).open('wb') as stdout, (folder/(label+'.stderr.log')).open('wb') as stderr:
        subprocess.run(cmd,cwd=folder,env=env,stdout=stdout,stderr=stderr,check=True,timeout=90)
def evaluate(recipe,args,folder,label):
    cmd=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',recipe.read_text(encoding='utf-8-sig'),
         '-extra',*[Path(arg).as_posix() for arg in args]]
    run(cmd,folder,label)
def escript(recipe,beam,folder,expected,candidate):
    folder.mkdir()
    run([str(refs/'bin/escript.exe'),str(recipe),str(beam)],folder,'execution')
    assert sha(folder/expected)==sha(folder/candidate)
    return sha(folder/candidate)

pilot=read(until('pilot-receipt.json'))
assert pilot['Count']==read(stage/'scope.json')['Modules'] and pilot['ExitCode'] in (0,1)
groups={entry['Agent'] for entry in read(stage/'agent-intake.json')}
source=stage/'pilot.json.generated'
generated=root/'input-images'
generated.mkdir()
import re
asn_stems=('OTP-PKIX','OTP-PKIX-Relaxed','PKIX1Explicit-2009','PKIX1Implicit-2009')
asn_paths={name:f'lib/public_key-1.21.7/ebin/{name}.beam' for name in asn_stems}
release_paths={'mod_head':'lib/inets-9.8/ebin/mod_head.beam',
               'erl_syntax':'lib/syntax_tools-4.1.1/ebin/erl_syntax.beam'}
needed=[*release_paths.values(),*asn_paths.values()]
absent=[rel for rel in needed if not (source/rel).is_file()]
if absent:
    capture=root/'raw-exact-capture'
    capture.mkdir()
    tool=capture/'tool'
    shutil.copytree(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/boolean-dispatch-and-wx-remainder-1927')/'early-raw-linked/tool',tool)
    for name in ('Compiler','Runtime'):
        match=f'Erlang.{name}.dll'
        shutil.copyfile(stage/'corpus-bin'/match,tool/match)
    phase=capture/'otp-corpus.json'
    selection=capture/'modules.txt'
    selection.write_text('\n'.join(absent)+'\n',encoding='utf-8')
    temp=capture/'temp'
    temp.mkdir()
    cmd=['D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe',str(tool/'Erlang.BeamCorpus.dll'),refs.as_posix(),
         phase.as_posix(),'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/public-key-generated','D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pinned-inputs-full',
         '--modules-file',selection.as_posix(),'--jobs','1']
    env={**os.environ,'TEMP':str(temp),'TMP':str(temp),'DOTNET_PROCESSOR_COUNT':'1',
         'ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES':'1','ERLANG_BEAM_CORPUS_TIMINGS':'1'}
    with (capture/'stdout.log').open('wb') as stdout,(capture/'stderr.log').open('wb') as stderr:
        subprocess.run(cmd,env=env,stdout=stdout,stderr=stderr,check=True,cwd=capture,timeout=200)
    raw=read(phase)
    assert raw['Total']==raw['Counts']['Equal']==len(absent)
    for rel in absent:
        img=capture/'otp-corpus.json.generated'/rel
        assert sha(img)==sha(refs/rel),rel
for rel in needed:
    origin=capture/'otp-corpus.json.generated'/rel if rel in absent else source/rel
    destination=generated/rel
    destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(origin,destination)
assert all((generated/rel).is_file() for rel in needed)
witnesses={}
head=generated/release_paths['mod_head']
sample=root/'head-sample.txt'
sample.write_bytes(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/mod-head-field-heap-1935/witness-body.txt').read_bytes())
for label,beam in [('Reference',refs/release_paths['mod_head']),('Candidate',head)]:
    evaluate(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/mod-head-field-heap-1935/head-native.eval'),[beam,sample,root/f'mod-head-{label}.terms'],root,f'mod-head-{label}')
outcome=sha(root/'mod-head-Candidate.terms')
assert outcome==sha(root/'mod-head-Reference.terms')
witnesses['mod_head']=dict(Beam=sha(head),Observations=12,Outcome=outcome)
syntax=generated/release_paths['erl_syntax']
for label,beam in [('Reference',refs/release_paths['erl_syntax']),('Candidate',syntax)]:
    evaluate(stage/'observe-erl-syntax.eval',[beam,root/f'erl-syntax-{label}.etf'],root,f'erl-syntax-{label}')
    assert (root/f'erl-syntax-{label}.stdout.log').read_text().strip()=='13 observations'
outcome=sha(root/'erl-syntax-Candidate.etf')
assert outcome==sha(root/'erl-syntax-Reference.etf')
witnesses['erl_syntax']=dict(Beam=sha(syntax),Observations=13,Outcome=outcome)
recipe=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/generated-owner-residuals/native.eval')
accepted=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/matched-contexts-and-call-liveness-1933/rank-input')
evaluate(recipe,[refs,accepted,generated],root,'tuple-head-encoding')
log=root/'tuple-head-encoding.stdout.log'
matches=re.findall(r"^'([^']+)'/[^\r\n]*? (\d+) cases, baseline \d+, candidate (\d+)\s*$",
                   log.read_text(encoding='utf-8'),re.MULTILINE)
assert sum(int(count) for _,count,_ in matches)==80,matches
assert all(expected==actual for _,expected,actual in matches),matches
for stem,count,_ in matches:
    witnesses[stem]=dict(Beam=sha(generated/asn_paths[stem]),Observations=int(count),Outcome=sha(log))
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
             Observations=sum(record['Observations'] for record in witnesses.values()),
             Groups=sorted(groups))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')


