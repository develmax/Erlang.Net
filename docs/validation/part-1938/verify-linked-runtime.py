from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/cons-inputs-and-measured-choice-1937')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/value-stack-and-construction-destinations-1938')
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
asn_stems=('EnrollmentMessageSyntax-2009','PKIX1Implicit-2009','PKIXCMP-2023')
asn_paths={name:f'lib/public_key-1.21.7/ebin/{name}.beam' for name in asn_stems}
release_paths={'mod_head':'lib/inets-9.8/ebin/mod_head.beam',
               'xmerl_xpath_parse':'lib/xmerl-2.2/ebin/xmerl_xpath_parse.beam',
               'binary':'lib/stdlib-8.1/ebin/binary.beam',
               'uri_string':'lib/stdlib-8.1/ebin/uri_string.beam'}
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
sample.write_bytes(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-response-heap-1941/witness-body.txt').read_bytes())
for label,beam in [('Reference',refs/release_paths['mod_head']),('Candidate',head)]:
    evaluate(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-response-heap-1941/head-native.eval'),[beam,sample,root/f'mod-head-{label}.terms'],root,f'mod-head-{label}')
outcome=sha(root/'mod-head-Candidate.terms')
assert outcome==sha(root/'mod-head-Reference.terms')
witnesses['mod_head']=dict(Beam=sha(head),Observations=12,Outcome=outcome)
xpath=release_paths['xmerl_xpath_parse']
evaluate(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931/private-cons-callers/native-parser-scope.eval'),
          [refs/xpath,Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/cons-inputs-and-measured-choice-1937/pilot.json.generated')/xpath,generated/xpath,root/'xpath.terms'],root,'xpath')
assert len((root/'xpath.terms').read_text().splitlines())==5
witnesses['xmerl_xpath_parse']=dict(Beam=sha(generated/xpath),Observations=5,Outcome=sha(root/'xpath.terms'))
accepted=p/'pilot.json.generated'
evaluate(Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/post-choice-residuals/native-encoder-custody.eval'),[refs,accepted,generated],root,'asn-encoder')
log=root/'asn-encoder.stdout.log'
matches=re.findall(r"^'([^']+)'/.*? (\d+) cases, encoder exact (\d+), strict round-trip (\d+), stable prior decode stack differences (\d+)\s*$",
                   log.read_text(encoding='utf-8'),re.MULTILINE)
assert len(matches)==6,matches
asn={}
for stem,count,enc,roundtrip,stable in matches:
    count,enc,roundtrip,stable=map(int,(count,enc,roundtrip,stable))
    assert enc==count==roundtrip+stable
    values=asn.setdefault(stem,[0,0,0])
    values[0]+=count
    values[1]+=roundtrip
    values[2]+=stable
assert [sum(row[index] for row in asn.values()) for index in range(3)]==[81,77,4]
for stem,(count,roundtrip,stable) in asn.items():
    witnesses[stem]=dict(Beam=sha(generated/asn_paths[stem]),Observations=count,Outcome=sha(log),
                         StrictRoundTrips=roundtrip,StablePriorStackDifferences=stable)
for module,count in [('binary',8),('uri_string',8)]:
    relative=release_paths[module]
    for label,beam in [('Reference',refs/relative),('Candidate',generated/relative)]:
        evaluate(stage/'observe-construction-membership.eval',[beam,root/f'{module}-{label}.terms'],root,f'{module}-{label}')
    outcome=sha(root/f'{module}-Candidate.terms')
    assert outcome==sha(root/f'{module}-Reference.terms')
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcome=outcome)
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
             Observations=sum(record['Observations'] for record in witnesses.values()),
             Groups=sorted(groups))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')


