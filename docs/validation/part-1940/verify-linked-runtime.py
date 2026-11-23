from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-frame-trims-and-guarded-helpers-1939')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/bounded-entry-and-generator-calls-1940')
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
release_paths={
 'mod_head':'lib/inets-9.8/ebin/mod_head.beam',
 'httpd_instance_sup':'lib/inets-9.8/ebin/httpd_instance_sup.beam',
 'xmerl_xpath_parse':'lib/xmerl-2.2/ebin/xmerl_xpath_parse.beam',
 'erl_parse':'lib/stdlib-8.1/ebin/erl_parse.beam',
 'local_udp':'lib/kernel-11.0.4/ebin/local_udp.beam',
 'ssh_fsm_userauth_client':'lib/ssh-6.0.6/ebin/ssh_fsm_userauth_client.beam',
 'PKIX-X400Address-2009':'lib/public_key-1.21.7/ebin/PKIX-X400Address-2009.beam'
}
asn_paths={}
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
ssh_witness=Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/missing-comprehension-helpers-1940/tuple-reuse/release-native/observe_ssh_record_generator.erl')
definition=Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-call-and-failures-1943')
recipe_archive={
 'mod_head':(definition/'head-native.eval',12,[root/'head-sample.txt']),
 'httpd_instance_sup':(Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/closed-catch-frame-ownership-1948/supervisor-native.eval'),10,[]),
 'local_udp':(stage/'observe-udp-and-parser.eval',6,[]),
 'erl_parse':(stage/'observe-udp-and-parser.eval',8,[]),
 'ssh_fsm_userauth_client':(stage/'observe-ssh-probe.eval',5,[ssh_witness]),
}
(root/'head-sample.txt').write_bytes((definition/'witness-body.txt').read_bytes())
for module,(recipe,count,extra) in recipe_archive.items():
    relative=release_paths[module]
    for label,image in [('Reference',refs/relative),('Candidate',generated/relative)]:
        args=[image,*extra,root/f'{module}-{label}.terms'] if module=='mod_head' else [*extra,image,root/f'{module}-{label}.terms']
        evaluate(recipe,args,root,f'{module}-{label}')
    outcome=sha(root/f'{module}-Candidate.terms')
    assert outcome==sha(root/f'{module}-Reference.terms'),module
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcome=outcome)

accepted=p/'pilot.json.generated'
xpath=release_paths['xmerl_xpath_parse']
evaluate(Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931/private-cons-callers/native-parser-scope.eval'),
          [refs/xpath,accepted/xpath,generated/xpath,root/'xpath.terms'],root,'xpath')
assert len((root/'xpath.terms').read_text().splitlines())==5
witnesses['xmerl_xpath_parse']=dict(Beam=sha(generated/xpath),Observations=5,Outcome=sha(root/'xpath.terms'))

stem='PKIX-X400Address-2009'
actual=release_paths[stem]
asn=witnesses[stem]=dict(Beam=sha(generated/actual),Observations=0,Outcome={})
commands=[
 (Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/post-signed-range-residuals/native-binary-measurement.eval'),84,'binary'),
 (Path(r'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/implicit-record-residuals/native-prepared-measurement.eval'),48,'prepared'),
]
for recipe,expected,label in commands:
    evaluate(recipe,[refs,accepted,generated],root,'asn-'+label)
    log=root/f'asn-{label}.stdout.log'
    rows=re.findall(r"^'PKIX-X400Address-2009'/.*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                    log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[0]) for row in rows)==expected and all(row[0]==row[2] for row in rows),log
    asn['Observations']+=expected
    asn['Outcome'][label]=sha(log)
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
             Observations=sum(record['Observations'] for record in witnesses.values()),
             Groups=sorted(groups))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')


