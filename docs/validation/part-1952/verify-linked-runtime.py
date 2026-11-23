from pathlib import Path
import hashlib,json,os,re,shutil,subprocess,time

inputs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118')
parent=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/entry-and-selective-frames-1951')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/tuple-conversions-and-entry-frames-1952')
refs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed')
root=stage/'linked-runtime-witnesses'
payload=stage/'corpus-bin'
sdk=inputs/'sdk/dotnet.exe'
root.mkdir()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def run(cmd,folder,label,**options):
    env={**os.environ,'ERL_CRASH_DUMP':str(folder/(label+'-crash.dump')),
         'ERL_FLAGS':'+S 1:1 +A 1','DOTNET_PROCESSOR_COUNT':'1',**options}
    with (folder/(label+'.stdout.log')).open('wb') as stdout, (folder/(label+'.stderr.log')).open('wb') as stderr:
        subprocess.run([str(arg) for arg in cmd],cwd=folder,env=env,stdout=stdout,stderr=stderr,check=True,timeout=150)

def evaluate(recipe,args,folder,label):
    run([refs/'bin/erl.exe','+S','1:1','+A','1','-noshell','-eval',recipe.read_text(encoding='utf-8-sig'),'-extra',*args],
        folder,label)

deadline=time.monotonic()+1800
for expected in [stage/'pilot-receipt.json',parent/'full-uncached/timing.json']:
    while not expected.is_file():
        if time.monotonic()>deadline:
            raise TimeoutError(expected)
        time.sleep(3)

pilot=read(stage/'pilot.json')
previous=read(parent/'full-uncached/otp-corpus.json')
identity=read(stage/'focused-identity.json')
assert pilot['Total']==read(stage/'scope.json')['Modules']
assert all(sha(payload/f'Erlang.{name}.dll')==identity[name] for name in ('Compiler','Runtime'))
enrollment='EnrollmentMessageSyntax-2009'
explicit='PKIX1Explicit-2009'
implicit='PKIX1Implicit-2009'
pubkey='pubkey_translation'
targets=(enrollment,explicit,implicit,pubkey)
rows={Path(row['Beam'].replace(chr(92),'/')).stem:row for row in pilot['Modules']}
prior={Path(row['Beam'].replace(chr(92),'/')).stem:row for row in previous['Modules']}
paths={name:rows[name]['Beam'].replace(chr(92),'/') for name in targets}
exact={name:paths[name] for name in targets if rows[name]['Status']=='Equal'}
assert {enrollment,pubkey}<=exact.keys()
tool=root/'capture'
shutil.copytree(inputs/'boolean-dispatch-and-wx-remainder-1927/early-raw-linked/tool',tool)
for name in ('Compiler','Runtime'):
    library=f'Erlang.{name}.dll'
    shutil.copyfile(payload/library,tool/library)
manifest=root/'modules.txt'
manifest.write_text('\n'.join(exact.values())+'\n',encoding='utf-8',newline='\n')
report=root/'capture.json'
temp=root/'temp'
temp.mkdir()
run([sdk,tool/'Erlang.BeamCorpus.dll',refs,report,inputs/'public-key-generated',inputs/'pinned-inputs-full',
     '--modules-file',manifest,'--jobs','1'],root,'capture',
     TEMP=str(temp),TMP=str(temp),ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES='1',ERLANG_BEAM_CORPUS_TIMINGS='1')
assert read(report)['Counts']['Equal']==len(exact)

owners=[enrollment,explicit,implicit]
before=root/'before'
after=root/'after'
for name in targets:
    rel=paths[name]
    if name in exact:
        candidate=root/'capture.json.generated'/rel
        assert sha(candidate)==sha(refs/rel),name
    else:
        candidate=stage/'pilot.json.generated'/rel
    out=after/rel
    out.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(candidate,out)
    if name not in owners:
        continue
    pinned=refs/rel
    if prior[name]['Status']=='Different':
        images=list((parent/'full-uncached').glob('shard-*/otp-corpus.json.generated/'+rel))
        assert len(images)==1,name
        pinned=images[0]
    out=before/rel
    out.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(pinned,out)

witnesses={}
specs=read(stage/'component-specs.json')
asn=Path(specs['FinalAsnCheckedListDecoders']['Root'])
evaluator=asn/'native-enrollment-final-decoders.eval'
evaluate(evaluator,[refs,before,after],root,'enrollment-decoders')
out=root/'enrollment-decoders.stdout.log'
observations=re.findall(r'cases, baseline \d+, candidate (\d+)',out.read_text(encoding='utf-8-sig'))
assert sum(map(int,observations))==37 and len(observations)==2,observations
witnesses[enrollment]=dict(Beam=sha(after/paths[enrollment]),Observations=37,Output=sha(out))

evaluator=stage/'native-pubkey-1952.eval'
for role,path in [('pinned',refs/paths[pubkey]),('generated',after/paths[pubkey])]:
    evaluate(evaluator,[path,root/f'pubkey-{role}.terms'],root,'pubkey-'+role)
assert sha(root/'pubkey-pinned.terms')==sha(root/'pubkey-generated.terms')
witnesses[pubkey]=dict(Beam=sha(after/paths[pubkey]),Observations=9,Output=sha(root/'pubkey-generated.terms'))

receipt=dict(Compiler=identity['Compiler'],Runtime=identity['Runtime'],Witnesses=witnesses,
             ActualObservations=sum(row['Observations'] for row in witnesses.values()))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt,indent=2))

