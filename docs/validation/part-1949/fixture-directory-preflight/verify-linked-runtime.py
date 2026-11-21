from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/ber-final-record-schedules-and-frame-roots-1948')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/tag-continuations-and-directory-exits-1949')
refs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed')
root=stage/'linked-runtime-witnesses'
root.mkdir(exist_ok=True)
assert not any(root.iterdir())
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
pilot=read(until('pilot-receipt.json'))
assert pilot['Count']==read(stage/'scope.json')['Modules'] and pilot['ExitCode'] in (0,1)
groups={entry['Agent'] for entry in read(stage/'agent-intake.json')}
source=stage/'pilot.json.generated'
generated=root/'input-images'
generated.mkdir()
import re
def capture_exact(capture, absent, payload):
    capture.mkdir()
    tool=capture/'tool'
    shutil.copytree(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/boolean-dispatch-and-wx-remainder-1927')/'early-raw-linked/tool',tool)
    for name in ('Compiler','Runtime'):
        match=f'Erlang.{name}.dll'
        shutil.copyfile(payload/match,tool/match)
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

def until_parent():
    deadline=time.monotonic()+2700
    report=p/'full-uncached/otp-corpus.json'
    while not report.is_file():
        if time.monotonic()>deadline: raise TimeoutError(report)
        time.sleep(3)
    return report

def capture_exact(capture, absent, payload):
    capture.mkdir()
    tool=capture/'tool'
    shutil.copytree(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/boolean-dispatch-and-wx-remainder-1927')/'early-raw-linked/tool',tool)
    for name in ('Compiler','Runtime'):
        match=f'Erlang.{name}.dll'
        shutil.copyfile(payload/match,tool/match)
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


def until_parent():
    deadline=time.monotonic()+2700
    report=p/'full-uncached/otp-corpus.json'
    while not report.is_file():
        if time.monotonic()>deadline: raise TimeoutError(report)
        time.sleep(3)
    return report




pilot_data=read(stage/'pilot.json')
rels={Path(row['Beam'].replace(chr(92),'/')).stem:row['Beam'].replace(chr(92),'/') for row in pilot_data['Modules']}
gui=['dbg_wx_settings','wxGridBagSizer']
asn=['EnrollmentMessageSyntax-2009','PKIX1Explicit-2009','PKIXCRMF-2009','PKIX1Implicit-2009','CryptographicMessageSyntax-2009']
targets=sorted({rels[module] for module in [*gui,*asn,'megaco_text_mini_decoder','sudoku']})
needed=[path for path in targets if not (source/path).is_file()]
if needed:
    extracted=root/'raw-exact-capture'
    capture_exact(extracted,needed,stage/'corpus-bin')
for relative in targets:
    original=extracted/'otp-corpus.json.generated'/relative if relative in needed else source/relative
    dest=generated/relative
    dest.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(original,dest)
witnesses={}
def add(module,key,count,outcome):
    entry=witnesses.setdefault(module,dict(Beam=sha(generated/rels[module]),Observations=0,Outcomes={}))
    assert key not in entry['Outcomes']
    entry['Observations']+=count
    entry['Outcomes'][key]=outcome
paths=root/'directory-fixtures'
paths.mkdir()
root1970=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/canonical-frame-choice-exits-1970')
root1967=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-selector-projection-1967')
root1971=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/pair-guarded-wx-wrappers-1971')
for module,count,script,kind in [
    ('dbg_wx_settings',7,root1970/'native-directories.eval','directory'),
    ('wxGridBagSizer',15,root1971/'native-wrappers.eval','wx-class')
]:
    actual={}
    for role,base in [('Reference',refs),('Candidate',generated)]:
        label=role+'-'+kind
        result=root/(label+'.terms')
        if kind=='directory':
            tasks=paths/role
            tasks.mkdir()
            args=[base/rels[module],root1967/'stub',tasks,result]
            evaluate(script,args,root,label)
        else:
            command=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell',
                     '-eval',script.read_text(encoding='utf-8-sig'),'-extra',
                     (base/rels[module]).as_posix(),(root1971/'stub').as_posix(),'all',result.as_posix()]
            run(command,root,label)
        actual[role]=sha(result)
    assert actual['Reference']==actual['Candidate']
    add(module,kind,count,actual['Candidate'])
m='megaco_text_mini_decoder'
megaco_out=root/'megaco.terms'
evaluate(Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931/megaco-call-tails-1948/native-megaco.eval'),[refs/rels[m],generated/rels[m],megaco_out],root,'megaco')
add(m,'decoder',5,sha(megaco_out))

previous=read(until_parent())
statuses={row['Beam'].replace(chr(92),'/'):row['Status'] for row in previous['Modules']}
exact=sorted(rels[module] for module in asn if statuses[rels[module]]=='Equal')
if exact:
    exact_before=root/'parent-exact-capture'
    frozen_before=read(p/'source-freeze.json')
    for name in ('Compiler','Runtime'):
        assert sha(p/f'corpus-bin/Erlang.{name}.dll').lower()==frozen_before[name].lower()
    capture_exact(exact_before,exact,p/'corpus-bin')
before=root/'parent-asn-images'
for module in asn:
    path=rels[module]
    if path in exact:
        origin=exact_before/'otp-corpus.json.generated'/path
    else:
        matches=list((p/'full-uncached').glob(f'shard-*/otp-corpus.json.generated/{path}'))
        assert len(matches)==1,path
        origin=matches[0]
    dest=before/path
    dest.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(origin,dest)
asn_root=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931')
for label,script,count in [
    ('enrollment-selected-decoders','native-enrollment-selected-decoders.eval',43),
    ('enrollment-status-encoders','native-enrollment-status-encoders.eval',36),
    ('enrollment-required-records','native-enrollment-required-records.eval',36)
]:
    evaluate(asn_root/label/script,[refs,before,generated],root,label)
    log=root/(label+'.stdout.log')
    rows=re.findall(r"^'([^']+)'[^\n]*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
        log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[1]) for row in rows)==count and all(row[1]==row[3] for row in rows),rows
    for module in {row[0] for row in rows}:
        add(module,label,sum(int(row[1]) for row in rows if row[0]==module),sha(log))
for module in asn:
    if module not in witnesses:
        witnesses[module]=dict(Beam=sha(generated/rels[module]),Observations=0,Outcomes={},Dependency=True)

sudoku=root/'sudoku-load'
sudoku.mkdir()
load_eval='[Ref,C]=init:get_plain_arguments(),[begin {ok,B}=file:read_file(P),{ok,_}=code:prepare_loading([{sudoku,P,B}]) end||P<-[Ref,C]],ok=io:format("Both Sudoku images accepted for loader preparation.~n"),halt().'
run([str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',load_eval,'-extra',
    (refs/rels['sudoku']).as_posix(),(generated/rels['sudoku']).as_posix()],sudoku,'native-loader')
witnesses['sudoku']=dict(Beam=sha(generated/rels['sudoku']),Observations=0,Outcomes={},LoaderPreparation=True)
frozen=read(until('source-freeze.json'))
sdk='D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe'
def stage_fixture(origin,root):
    shutil.copytree(origin,root)
    for key in ['Compiler','Runtime']:
        original=stage/f'corpus-bin/Erlang.{key}.dll'
        assert sha(original).lower()==frozen[key]
        shutil.copyfile(original,root/original.name)
    return root/'Erlang.Tests.dll'
sudoku_packet=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931/sudoku-call-receive-1949')
sudoku_test=stage_fixture(sudoku_packet/'test-build/bin/Debug/net10.0',sudoku)
sudoku_out=sudoku/'output'
sudoku_out.mkdir()
run([sdk,str(sudoku_test),str(sudoku_out)],sudoku,'focused')
probe='beam_private_payload_continuations_probe'
evaluate(sudoku_packet/'native-regression.eval',[sudoku_out/(probe+'.erl'),sudoku_out/(probe+'.beam'),sudoku_out/'native-reference.beam'],sudoku,'native-fixture')
assert 'four values' in (sudoku/'native-fixture.stdout.log').read_text(encoding='utf-8')
entry_packet=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/late-entry-root-definitions-1949')
entry=root/'entry-definitions-fixture'
entry_test=stage_fixture(entry_packet/'regression-candidate',entry)
entry_image=entry/'candidate-fixture.beam'
run([sdk,str(entry_test),str(entry_image)],entry,'focused')
entry_eval='[Path]=init:get_plain_arguments(),{ok,B}=file:read_file(Path),M=beam_entry_root_defs,{module,M}=code:load_binary(M,Path,B),io:format("~tp.~n",[M:observe()]),halt().'
run([str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',entry_eval,'-extra',str(entry_image)],entry,'native-fixture')
assert sha(entry/'native-fixture.stdout.log')==sha(entry_packet/'pinned-witness.terms')
result=dict(Compiler=frozen['Compiler'],Runtime=frozen['Runtime'],Witnesses=witnesses,
            Observations=sum(entry['Observations'] for entry in witnesses.values()),Groups=sorted(groups),
            LinkedFixtures={
                'Sudoku':dict(Observations=4,CompiledImage=sha(sudoku_out/(probe+'.beam')),Results=sha(sudoku/'native-fixture.stdout.log')),
                'EntryDefinitions':dict(Observations=4,CompiledImage=sha(entry_image),Results=sha(entry/'native-fixture.stdout.log'))
            })
assert result['Observations']==142
(root/'receipt.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(result['Observations'])

