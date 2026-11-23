from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-results-and-case-comprehensions-1945')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/mapped-records-and-prefixed-field-lifetimes-1946')
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

release_paths={
 'mod_actions':'lib/inets-9.8/ebin/mod_actions.beam',
 'observer_html_lib':'lib/observer-2.19/ebin/observer_html_lib.beam',
 'xmerl_lib':'lib/xmerl-2.2/ebin/xmerl_lib.beam',
}
pilot_rows={Path(row['Beam'].replace(chr(92),'/')).stem:row['Beam'].replace(chr(92),'/')
            for row in read(stage/'pilot.json')['Modules']}
release_paths={key:pilot_rows[key] for key in release_paths}
asn_modules=['PKIXAttributeCertificate-2009','PKIX1Implicit-2009','PKIXCRMF-2009','PKIXCMP-2023','CMS-2009']
asn_paths={module:pilot_rows[module] for module in asn_modules}
needed=sorted(set(release_paths.values())|set(asn_paths.values()))
absent=[rel for rel in needed if not (source/rel).is_file()]
if absent:
    capture=root/'raw-exact-capture'
    capture_exact(capture,absent,stage/'corpus-bin')
for relative in needed:
    original=capture/'otp-corpus.json.generated'/relative if relative in absent else source/relative
    target=generated/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(original,target)
before=root/'parent-asn-images'
before.mkdir()
raw=read(until_parent())
statuses={row['Beam'].replace(chr(92),'/'):row['Status'] for row in raw['Modules']}
prior_exact=[relative for relative in asn_paths.values() if statuses[relative]=='Equal']
if prior_exact:
    prior_capture=root/'prior-raw-exact-capture'
    frozen=read(p/'source-freeze.json')
    for name in ('Compiler','Runtime'):
        assert sha(p/f'corpus-bin/Erlang.{name}.dll').lower()==frozen[name].lower()
    capture_exact(prior_capture,prior_exact,p/'corpus-bin')
for rel in asn_paths.values():
    if rel in prior_exact:
        source_image=prior_capture/'otp-corpus.json.generated'/rel
    else:
        sources=list((p/'full-uncached').glob(f'shard-*/otp-corpus.json.generated/{rel}'))
        assert len(sources)==1,rel
        source_image=sources[0]
    target=before/rel
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(source_image,target)
witnesses={}
def add(module,relative,count,key,result):
    entry=witnesses.setdefault(module,dict(Beam=sha(generated/relative),Observations=0,Outcomes={}))
    assert key not in entry['Outcomes'],key
    entry['Outcomes'][key]=result
    entry['Observations']+=count
for module,key,recipe,count,path in [
    ('mod_actions','do',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/retained-record-call-chain-1962/native-do.eval'),1,Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/retained-record-call-chain-1962')),
    ('mod_actions','store',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/guarded-tagged-alias-switch-1961/native-store.eval'),9,None),
    ('xmerl_lib','markup',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/release-near-selector-1960/native-markup.eval'),6,None),
    ('observer_html_lib','plain_page',Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/saved-entry-and-in-frame-values-1946/observe-observer-html.eval'),3,None)
]:
    rel=release_paths[module]
    result={}
    for role,images in [('Reference',refs),('Candidate',generated)]:
        label=f'{module}-{key}-{role}'
        result_file=root/(label+'.terms')
        if path is None:
            evaluate(recipe,[images/rel,result_file],root,label)
        else:
            command=[str(refs/'bin/erl.exe'),'+S','1:1','+A','1','-noshell','-pa',str(path),
                       '-eval',recipe.read_text(encoding='utf-8-sig'),'-extra',str(images/rel),str(result_file)]
            run(command,root,label)
        result[role]=sha(result_file)
    assert len(set(result.values()))==1,(module,key)
    add(module,rel,count,key,result['Candidate'])
asn=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931')
for folder,recipe,count in [
    ('prefixed-tagged-field-lifetime','native-prefixed-field',39),
    ('foreign-record-encoder-schedule','native-foreign-first-optional',23),
    ('retained-foreign-triple-encoder','native-retained-foreign-triple',29),
    ('constant-remote-dispatch','native-constant-remote-dispatch',None),
    ('attribute-certificate-final-functions','native-attribute-final',26)
]:
    evaluate(asn/folder/(recipe+'.eval'),[refs,before,generated],root,recipe)
    log=root/(recipe+'.stdout.log')
    rows=re.findall(r"^'([^']+)'[^\n]*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                    log.read_text(encoding='utf-8'),re.MULTILINE)
    assert rows and (count is None or sum(int(row[1]) for row in rows)==count),(recipe,rows)
    assert all(row[1]==row[3] for row in rows),rows
    for module in sorted({row[0] for row in rows}):
        add(module,asn_paths[module],sum(int(row[1]) for row in rows if row[0]==module),recipe,sha(log))
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
for module,relative in asn_paths.items():
    if module not in witnesses:
        witnesses[module]=dict(Beam=sha(generated/relative),Observations=0,Outcomes={},Dependency=True)
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
             Observations=sum(record['Observations'] for record in witnesses.values()),Groups=sorted(groups))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps({key:value['Observations'] for key,value in witnesses.items()}))

