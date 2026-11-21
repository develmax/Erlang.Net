from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/byte-prefix-and-continuation-cursors-1947')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/ber-final-record-schedules-and-frame-roots-1948')
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



rels={Path(row['Beam'].replace(chr(92),'/')).stem:row['Beam'].replace(chr(92),'/') for row in read(stage/'pilot.json')['Modules']}
debugger='dbg_wx_settings'
asn=['PKIXCMP-2023','PKIXCRMF-2009','PKIX1Explicit-2009','PKIX1Implicit-2009','CryptographicMessageSyntax-2009','EnrollmentMessageSyntax-2009']
targets=sorted({rels[key] for key in [debugger,*asn]})
missing=[relative for relative in targets if not (source/relative).is_file()]
if missing:
    c=root/'raw-exact-capture'
    capture_exact(c,missing,stage/'corpus-bin')
for rel in targets:
    src=c/'otp-corpus.json.generated'/rel if rel in missing else source/rel
    dest=generated/rel
    dest.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(src,dest)
witnesses={}
def add(module,key,count,outcome):
    entry=witnesses.setdefault(module,dict(Beam=sha(generated/rels[module]),Observations=0,Outcomes={}))
    assert key not in entry['Outcomes']
    entry['Observations']+=count
    entry['Outcomes'][key]=outcome
scratch=root/'directory-fixtures'
scratch.mkdir()
home=scratch/'empty-home'
home.mkdir()
check_dir=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-selector-projection-1967')
recipe=(check_dir/'native-directories.eval').read_text(encoding='utf-8-sig')
reports={}
for label,images in [('Reference',refs),('Candidate',generated)]:
    tree=scratch/label
    tree.mkdir()
    report=root/(label+'-directories.terms')
    command=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-home',home.as_posix(),'-noshell','-eval',recipe,
        '-extra',(images/rels[debugger]).as_posix(),(check_dir/'stub').as_posix(),tree.as_posix(),report.as_posix()]
    run(command,root,label+'-directories')
    reports[label]=sha(report)
assert reports['Reference']==reports['Candidate']
add(debugger,'directories',7,reports['Candidate'])

before=root/'parent-asn-images'
raw=read(until_parent())
statuses={row['Beam'].replace(chr(92),'/'):row['Status'] for row in raw['Modules']}
exact=sorted(rels[k] for k in asn if statuses[rels[k]]=='Equal')
if exact:
    cap=root/'parent-exact-capture'
    parent=read(p/'source-freeze.json')
    for name in ('Compiler','Runtime'):
        assert sha(p/f'corpus-bin/Erlang.{name}.dll').lower()==parent[name].lower()
    capture_exact(cap,exact,p/'corpus-bin')
for module in asn:
    relative=rels[module]
    if relative in exact:
        original=cap/'otp-corpus.json.generated'/relative
    else:
        originals=list((p/'full-uncached').glob(f'shard-*/otp-corpus.json.generated/{relative}'))
        assert len(originals)==1,relative
        original=originals[0]
    dest=before/relative
    dest.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(original,dest)
asn_root=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931')
for name,filename,expected in [
    ('cmp-final-record-schedules','native-cmp-final-schedules.eval',72),
    ('enrollment-open-types','native-enrollment-selected-record.eval',28)
]:
    evaluate(asn_root/name/filename,[refs,before,generated],root,name)
    log=root/(name+'.stdout.log')
    rows=re.findall(r"^'([^']+)'[^\n]*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
        log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[1]) for row in rows)==expected and all(row[1]==row[3] for row in rows),rows
    for module in {row[0] for row in rows}:
        add(module,name,sum(int(row[1]) for row in rows if row[0]==module),sha(log))
for module in asn:
    if module not in witnesses:
        witnesses[module]=dict(Beam=sha(generated/rels[module]),Observations=0,Outcomes={},Dependency=True)

frozen=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==frozen['Compiler']
fixture_packet=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/last-use-outgoing-call-roots-1948')
separate_fixture=root/'outgoing-root-fixture'
shutil.copytree(fixture_packet/'retake-regression-candidate',separate_fixture)
for name in ['Compiler','Runtime']:
    original=stage/f'corpus-bin/Erlang.{name}.dll'
    assert sha(original).lower()==frozen[name]
    shutil.copyfile(original,separate_fixture/original.name)
linked_fixture=separate_fixture/'candidate-fixture.beam'
run(['D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/sdk/dotnet.exe',str(separate_fixture/'Erlang.Tests.dll'),str(linked_fixture)],separate_fixture,'focused')
eval_body='[Path]=init:get_plain_arguments(),{ok,B}=file:read_file(Path),M=beam_outgoing_last_use,{module,M}=code:load_binary(M,Path,B),io:format("~tp.~n",[M:observe()]),halt().'
run([str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-eval',eval_body,'-extra',str(linked_fixture)],separate_fixture,'native')
fixture_outcome=sha(separate_fixture/'native.stdout.log')
assert fixture_outcome==sha(fixture_packet/'pinned-retake/results.terms')
summary=dict(Compiler=frozen['Compiler'],Runtime=frozen['Runtime'],Witnesses=witnesses,
             Observations=sum(entry['Observations'] for entry in witnesses.values()),Groups=sorted(groups),
             LinkedFixture=dict(Observations=5,CompiledImage=sha(linked_fixture),Outcomes=fixture_outcome,
                 Harness=sha(separate_fixture/'Erlang.Tests.dll')))
assert summary['Observations']==107
(root/'receipt.json').write_text(json.dumps(summary,indent=2)+'\n',encoding='utf-8')
print(summary['Observations'])

