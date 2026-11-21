from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/root-slots-closures-and-required-records-1942')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/terminal-arms-and-comprehension-sources-1943')
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
release_paths={'mod_head': 'lib/inets-9.8/ebin/mod_head.beam', 'httpd_instance_sup': 'lib/inets-9.8/ebin/httpd_instance_sup.beam', 'inet_udp': 'lib/kernel-11.0.4/ebin/inet_udp.beam', 'inet6_udp': 'lib/kernel-11.0.4/ebin/inet6_udp.beam', 'local_udp': 'lib/kernel-11.0.4/ebin/local_udp.beam', 'ssh_tcpip_forward_client': 'lib/ssh-6.0.6/ebin/ssh_tcpip_forward_client.beam', 'ssh_tcpip_forward_srv': 'lib/ssh-6.0.6/ebin/ssh_tcpip_forward_srv.beam', 'xmerl_xsd_re_parse': 'lib/xmerl-2.2/ebin/xmerl_xsd_re_parse.beam', 'reltool_server': 'lib/reltool-1.1/ebin/reltool_server.beam'}
asn_paths={'PKIX1Implicit-2009': 'lib/public_key-1.21.7/ebin/PKIX1Implicit-2009.beam', 'PKIXAttributeCertificate-2009': 'lib/public_key-1.21.7/ebin/PKIXAttributeCertificate-2009.beam'}
needed=[*release_paths.values(),*asn_paths.values()]
absent=[rel for rel in needed if not (source/rel).is_file()]
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
if absent:
    capture=root/'raw-exact-capture'
    capture_exact(capture, absent, stage/'corpus-bin')
for rel in needed:
    origin=capture/'otp-corpus.json.generated'/rel if rel in absent else source/rel
    destination=generated/rel
    destination.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(origin,destination)
assert all((generated/rel).is_file() for rel in needed)
witnesses={}
def prior_image(relative):
    pilot=p/'pilot.json.generated'/relative
    if pilot.is_file():
        return pilot
    full=p/'full-uncached'
    deadline=time.monotonic()+1800
    while not (full/'otp-corpus.json').is_file():
        if time.monotonic()>deadline:
            raise TimeoutError(full)
        time.sleep(3)
    matches=list(full.glob(f'shard-*/otp-corpus.json.generated/{relative}'))
    assert len(matches)==1,relative
    return matches[0]
recipes={'head': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-call-and-failures-1943/head-native.eval', 'head-input': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-call-and-failures-1943/witness-body.txt', 'supervisor': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/fixed-list-delayed-call-copies-1952/supervisor-native.eval', 'udp': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/independent-outgoing-call-moves-1943/single-source-chain/udp-native/native-udp.eval', 'reltool': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/terminal-arms-and-comprehension-sources-1943/reltool-trial/observe-reltool.eval', 'inbound': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-action-arms-1954/native.eval', 'action': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-action-arms-1954/action-native.eval', 'ssh-stub': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-action-arms-1954/stub/ssh_connection.beam', 'parser': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931/accept-return-frames-1942/native-parser-scope.eval', 'asn': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/record-encoder-residuals/native-retained-record.eval'}

recipes={key:Path(path) for key,path in recipes.items()}
(root/'head-sample.txt').write_bytes(recipes['head-input'].read_bytes())
direct=[
    ('mod_head','head',12,[root/'head-sample.txt']),
    ('httpd_instance_sup','supervisor',10,[]),
    ('inet_udp','udp',2,[]),
    ('inet6_udp','udp',2,[]),
    ('local_udp','udp',1,[]),
    ('reltool_server','reltool',3,[]),
]
for module,key,count,extra in direct:
    relative=release_paths[module]
    for label,directory in [('Reference',refs),('Candidate',generated)]:
        out=root/f'{module}-{label}.terms'
        evaluate(recipes[key],[directory/relative,*extra,out],root,f'{module}-{label}')
    outcome=sha(root/f'{module}-Candidate.terms')
    assert outcome==sha(root/f'{module}-Reference.terms'),module
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcome=outcome)
ssh=['ssh_tcpip_forward_client','ssh_tcpip_forward_srv']
for case in ['inbound','action']:
    extra=[] if case=='inbound' else [recipes['ssh-stub']]
    for label,directory in [('Reference',refs),('Candidate',generated)]:
        targets=[directory/release_paths[module] for module in ssh]
        evaluate(recipes[case],[*targets,*extra,root/f'ssh-{case}-{label}.terms'],root,f'ssh-{case}-{label}')
    outcome=sha(root/f'ssh-{case}-Candidate.terms')
    assert outcome==sha(root/f'ssh-{case}-Reference.terms'),case
    for module in ssh:
        entry=witnesses.setdefault(module,dict(Beam=sha(generated/release_paths[module]),Observations=0,Outcomes={}))
        entry['Observations']+=9
        entry['Outcomes'][case]=outcome
regex=release_paths['xmerl_xsd_re_parse']
evaluate(recipes['parser'],[refs/regex,prior_image(regex),generated/regex,root/'regex.terms'],root,'regex')
assert len((root/'regex.terms').read_text().splitlines())==5
witnesses['xmerl_xsd_re_parse']=dict(Beam=sha(generated/regex),Observations=5,Outcome=sha(root/'regex.terms'))
asn_before=root/'asn-parent-images'
asn_before.mkdir()
for relative in asn_paths.values():
    target=asn_before/relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(prior_image(relative),target)
evaluate(recipes['asn'],[refs,asn_before,generated],root,'asn-retained-pairs')
log=root/'asn-retained-pairs.stdout.log'
rows=re.findall(r"^'([^']+)'/.*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                log.read_text(encoding='utf-8'),re.MULTILINE)
assert sum(int(row[1]) for row in rows)==63,rows
assert all(row[1]==row[3] for row in rows),rows
for module,count,_,_ in rows:
    entry=witnesses.setdefault(module,dict(Beam=sha(generated/asn_paths[module]),Observations=0,Outcome=sha(log)))
    entry['Observations']+=int(count)
assert sum(row['Observations'] for row in witnesses.values())==134,witnesses
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
             Observations=sum(record['Observations'] for record in witnesses.values()),
             Groups=sorted(groups))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')


