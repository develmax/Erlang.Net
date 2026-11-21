from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/construction-and-list-source-lowering-1931')
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
needed=[]
if 'BinaryString' in groups:
    needed.append('lib/inets-9.8/ebin/http_chunk.beam')
if 'SharedCons' in groups:
    needed.extend(['lib/stdlib-8.1/ebin/ordsets.beam','lib/stdlib-8.1/ebin/gb_sets.beam'])
if 'AsnNamedBits' in groups:
    needed.extend(f'lib/public_key-1.21.7/ebin/{stem}.beam' for stem in (
        'PKIXAttributeCertificate-2009','PKIX1Implicit-2009','PKIXCMP-2023','EnrollmentMessageSyntax-2009'))
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
if 'BinaryString' in groups:
    name='http_chunk'
    rel='lib/inets-9.8/ebin/http_chunk.beam'
    for role,source in [('Reference',refs),('Candidate',generated)]:
        evaluate(stage/'observe-http-chunks.eval',[source/rel,root/f'{name}-{role}.terms'],root,f'{name}-{role}')
    candidate=sha(root/f'{name}-Candidate.terms')
    assert candidate==sha(root/f'{name}-Reference.terms')
    witnesses[name]=dict(Beam=sha(generated/rel),Observations=6,Outcome=candidate)
if 'SharedCons' in groups:
    src=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/plain-list-guard-sharing-1931')
    ordsets=generated/'lib/stdlib-8.1/ebin/ordsets.beam'
    exact=escript(src/'verify-native.escript',ordsets,root/'ordsets',
                  'native-reference.etf','native-candidate.etf')
    witnesses['ordsets']=dict(Beam=sha(ordsets),Observations=500,Outcome=exact)
    gb_sets=generated/'lib/stdlib-8.1/ebin/gb_sets.beam'
    gb=escript(src/'one-column/verify-native-gb-sets.escript',gb_sets,root/'gb_sets',
               'gb-sets-native-reference.etf','gb-sets-native-candidate.etf')
    witnesses['gb_sets']=dict(Beam=sha(gb_sets),Observations=100,Outcome=gb)
    if 'ConsInsertion' in groups:
        added=escript(src/'one-column/verify-native-add-final.escript',ordsets,root/'ordsets-insertion',
                      'native-reference-final.etf','native-candidate-final.etf')
        witnesses['ordsets'].update(Observations=600,Insertion=added)
if 'AsnNamedBits' in groups:
    source=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931')
    evaluate(source/'native-expanded.eval',[refs,Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/frame-and-generator-planning-1929-retake')/'rank-input',generated],root,'asn-named-bits')
    exact=sha(root/'asn-named-bits.stdout.log')
    for stem,cases in [
        ('PKIXAttributeCertificate-2009',19),('PKIX1Implicit-2009',38),
        ('PKIXCMP-2023',19),('EnrollmentMessageSyntax-2009',11)
    ]:
        image=generated/f'lib/public_key-1.21.7/ebin/{stem}.beam'
        witnesses[stem]=dict(Beam=sha(image),Observations=cases,Outcome=exact)
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
             Observations=sum(record['Observations'] for record in witnesses.values()),
             Groups=sorted(groups))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')


