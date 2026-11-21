from pathlib import Path
import hashlib,json,os,shutil,subprocess,time
p=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/list-entry-and-construction-staging-1941')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/root-slots-closures-and-required-records-1942')
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
release_paths={'mod_head': 'lib/inets-9.8/ebin/mod_head.beam', 'httpd_instance_sup': 'lib/inets-9.8/ebin/httpd_instance_sup.beam', 'rand': 'lib/stdlib-8.1/ebin/rand.beam', 'erl_pp': 'lib/stdlib-8.1/ebin/erl_pp.beam', 'inet_parse': 'lib/kernel-11.0.4/ebin/inet_parse.beam', 'ssh_tcpip_forward_client': 'lib/ssh-6.0.6/ebin/ssh_tcpip_forward_client.beam', 'ssh_tcpip_forward_srv': 'lib/ssh-6.0.6/ebin/ssh_tcpip_forward_srv.beam'}
asn_paths={'PKIX1Implicit-2009': 'lib/public_key-1.21.7/ebin/PKIX1Implicit-2009.beam', 'PKIXAttributeCertificate-2009': 'lib/public_key-1.21.7/ebin/PKIXAttributeCertificate-2009.beam', 'PKIXCMP-2023': 'lib/public_key-1.21.7/ebin/PKIXCMP-2023.beam', 'PKIXCRMF-2009': 'lib/public_key-1.21.7/ebin/PKIXCRMF-2009.beam', 'OTP-PKIX': 'lib/public_key-1.21.7/ebin/OTP-PKIX.beam'}
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
recipes={'head': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-call-and-failures-1943/head-native.eval', 'sample': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/owned-record-call-and-failures-1943/witness-body.txt', 'supervisor': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/fixed-list-delayed-call-copies-1952/supervisor-native.eval', 'rand_pp': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/proved-outgoing-stack-roots-1942/release-native/observe-rand-pp.eval', 'inet': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binary-match-audit-1931/parser-residual-1941/native-parser-scope.eval', 'ssh': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/nested-ssh-record-dispatch-1953/native.eval', 'required': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/leading-required-record-residuals/native-leading-required.eval', 'literal': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/literal-table-optional-residuals/native-optional-literal-table.eval', 'alias': 'D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/asn-named-bits-replay-1931/multi-tag-optional-residuals/native-optional-alias-join.eval'}

archived = {key:Path(path) for key,path in recipes.items()}
(root/'head-sample.txt').write_bytes(archived['sample'].read_bytes())
direct = [
    ('mod_head','head',12,[root/'head-sample.txt']),
    ('httpd_instance_sup','supervisor',10,[]),
    ('rand','rand_pp',7,[]),
    ('erl_pp','rand_pp',5,[]),
    ('inet_parse','inet',3,[]),
]
for module,key,count,extra in direct:
    relative=release_paths[module]
    outcomes={}
    for label,image in [('Reference',refs/relative),('Candidate',generated/relative)]:
        out=root/f'{module}-{label}.terms'
        args=[image] if key=='inet' else [image,*extra,out]
        evaluate(archived[key],args,root,f'{module}-{label}')
        if key=='inet':
            out=root/f'{module}-{label}.stdout.log'
        outcomes[label]=sha(out)
    assert len(set(outcomes.values()))==1,module
    witnesses[module]=dict(Beam=sha(generated/relative),Observations=count,Outcome=outcomes['Candidate'])
ssh=['ssh_tcpip_forward_client','ssh_tcpip_forward_srv']
for label,directory in [('Reference',refs),('Candidate',generated)]:
    evaluate(archived['ssh'],[*(directory/release_paths[module] for module in ssh),root/f'ssh-{label}.terms'],root,f'ssh-{label}')
assert sha(root/'ssh-Reference.terms')==sha(root/'ssh-Candidate.terms')
for module in ssh:
    witnesses[module]=dict(Beam=sha(generated/release_paths[module]),Observations=9,Outcome=sha(root/'ssh-Candidate.terms'))
parent=root/'asn-parent-images'
parent.mkdir()
prior_rows={row['Beam'].replace(chr(92),'/'):row['Status'] for row in read(p/'full-uncached/otp-corpus.json')['Modules']}
prior_exact=sorted(relative for relative in asn_paths.values() if prior_rows[relative]=='Equal')
if prior_exact:
    previous_capture=root/'prior-raw-exact-capture'
    previous_freeze=read(p/'source-freeze.json')
    for artifact in ('Compiler','Runtime'):
        assert sha(p/f'corpus-bin/Erlang.{artifact}.dll').lower()==previous_freeze[artifact].lower()
    capture_exact(previous_capture,prior_exact,p/'corpus-bin')
for relative in asn_paths.values():
    destination=parent/relative
    destination.parent.mkdir(parents=True,exist_ok=True)
    previous=previous_capture/'otp-corpus.json.generated'/relative if relative in prior_exact else prior_image(relative)
    shutil.copyfile(previous,destination)
for key,expected in [('required',50),('literal',66),('alias',36)]:
    label=f'asn-{key}'
    evaluate(archived[key],[refs,parent,generated],root,label)
    log=root/f'{label}.stdout.log'
    rows=re.findall(r"^'([^']+)'/.*? (\d+) cases, baseline (\d+), candidate (\d+)\s*$",
                    log.read_text(encoding='utf-8'),re.MULTILINE)
    assert sum(int(row[1]) for row in rows)==expected,(key,rows)
    assert all(row[1]==row[3] for row in rows),rows
    for stem,count,_,_ in rows:
        value=witnesses.setdefault(stem,dict(Beam=sha(generated/asn_paths[stem]),Observations=0,Outcomes={}))
        value['Observations']+=int(count)
        value['Outcomes'][key]=sha(log)
freeze=read(until('source-freeze.json'))
assert sha(stage/'corpus-bin/Erlang.Compiler.dll').lower()==freeze['Compiler'].lower()
receipt=dict(Compiler=freeze['Compiler'],Runtime=freeze['Runtime'],Witnesses=witnesses,
             Observations=sum(record['Observations'] for record in witnesses.values()),
             Groups=sorted(groups))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')


