from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/mapped-records-and-prefixed-field-lifetimes-1946')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/byte-prefix-and-continuation-cursors-1947')
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
direct=[
('xmerl_xsd_re','scanner',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-byte-prefix-scanner-1964/native-scanner.eval'),11,[],False),
('snmp_verbosity','format',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/snmp-verbosity-code-residual-1944/physical-label-proof/snmp-native.eval'),4,[],True),
('dbg_wx_settings','dialog',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/release-near-part1945-1965/native-dialog.eval'),6,[Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/release-near-part1945-1965/stub')],False)
]
asn=['PKIXCMP-2023','PKIXCRMF-2009','PKIX1Explicit-2009','PKIX1Implicit-2009','CryptographicMessageSyntax-2009','EnrollmentMessageSyntax-2009']
targets=sorted({rels[key] for key in asn+[entry[0] for entry in direct]})
absent=[relative for relative in targets if not (source/relative).is_file()]
if absent:
    cap=root/'raw-exact-capture'
    capture_exact(cap,absent,stage/'corpus-bin')
for relative in targets:
    original=cap/'otp-corpus.json.generated'/relative if relative in absent else source/relative
    dest=generated/relative
    dest.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(original,dest)
witnesses={}
def add(module,count,key,outcome):
    entry=witnesses.setdefault(module,dict(Beam=sha(generated/rels[module]),Observations=0,Outcomes={}))
    assert key not in entry['Outcomes']
    entry['Outcomes'][key]=outcome
    entry['Observations']+=count
for module,key,recipe,count,within,with_stdout in direct:
    outputs={}
    for role,images in [('Reference',refs),('Candidate',generated)]:
        label=f'{module}-{key}-{role}'
        result=root/(label+'.terms')
        evaluate(recipe,[images/rels[module],*within,result],root,label)
        outputs[role]=[sha(result)]
        if with_stdout:
            outputs[role].append(sha(root/(label+'.stdout.log')))
    assert outputs['Reference']==outputs['Candidate'],module
    add(module,count,key,outputs['Candidate'])
prior=read(until_parent())
old={row['Beam'].replace(chr(92),'/'):row['Status'] for row in prior['Modules']}
prior_exact=sorted(rels[module] for module in asn if old[rels[module]]=='Equal')
if prior_exact:
    cap_prior=root/'prior-raw-exact-capture'
    frozen=read(p/'source-freeze.json')
    for name in ('Compiler','Runtime'):
        assert sha(p/f'corpus-bin/Erlang.{name}.dll').lower()==frozen[name].lower()
    capture_exact(cap_prior,prior_exact,p/'corpus-bin')
before=root/'parent-asn-images'
for module in asn:
    relative=rels[module]
    if relative in prior_exact:
        origin=cap_prior/'otp-corpus.json.generated'/relative
    else:
        originals=list((p/'full-uncached').glob(f'shard-*/otp-corpus.json.generated/{relative}'))
        assert len(originals)==1,relative
        origin=originals[0]
    destination=before/relative
    destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(origin,destination)
asn_root=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931')
for name,expected in [('cmp-foreign-required-optional',28),('cmp-record-witnesses',59)]:
    eval_name='native-cmp-required-optional.eval' if expected==28 else 'native-cmp-record-witnesses.eval'
    evaluate(asn_root/name/eval_name,[refs,before,generated],root,name)
    log=root/(name+'.stdout.log')
    rows=re.findall(r"^'([^']+)'[^\n]*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
        log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[1]) for row in rows)==expected and all(row[1]==row[3] for row in rows),rows
    for module in {row[0] for row in rows}:
        add(module,sum(int(row[1]) for row in rows if row[0]==module),name,sha(log))
for module in asn:
    if module not in witnesses:
        witnesses[module]=dict(Beam=sha(generated/rels[module]),Observations=0,Outcomes={},Dependency=True)
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
    Observations=sum(entry['Observations'] for entry in witnesses.values()),Groups=sorted(groups))
assert receipt['Observations']==108
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(receipt['Observations'])

