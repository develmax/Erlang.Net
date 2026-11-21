from pathlib import Path
import hashlib,json,os,re,shutil,subprocess,time

inputs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118')
parent=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/arithmetic-constructors-and-xml-wrappers-1950')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/entry-and-selective-frames-1951')
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
    with (folder/(label+'.stdout.log')).open('wb') as stdout,(folder/(label+'.stderr.log')).open('wb') as stderr:
        subprocess.run([str(arg) for arg in cmd],cwd=folder,env=env,stdout=stdout,stderr=stderr,check=True,timeout=150)

def evaluate(recipe,args,folder,label):
    run([refs/'bin/erl.exe','+S','1:1','+A','1','-noshell','-eval',recipe.read_text(encoding='utf-8-sig'),'-extra',*args],
        folder,label)

deadline=time.monotonic()+1500
while not (stage/'pilot-receipt.json').is_file():
    if time.monotonic()>deadline:
        raise TimeoutError('Pilot receipt missing')
    time.sleep(3)

pilot=read(stage/'pilot.json')
identity=read(stage/'focused-identity.json')
assert pilot['Total']==read(stage/'scope.json')['Modules']
assert all(sha(payload/f'Erlang.{name}.dll')==identity[name] for name in ('Compiler','Runtime'))
targets=('inet_sctp','ex_frame_utils','EnrollmentMessageSyntax-2009')
rows={Path(row['Beam'].replace(chr(92),'/')).stem:row for row in pilot['Modules']}
paths={name:rows[name]['Beam'].replace(chr(92),'/') for name in targets}
exact={name:paths[name] for name in targets if rows[name]['Status']=='Equal'}
assert set(exact)=={'inet_sctp','ex_frame_utils'}
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

witnesses={}
specs=read(stage/'component-specs.json')
def copy_path(name):
    rel=paths[name]
    image=root/'capture.json.generated'/rel if name in exact else stage/'pilot.json.generated'/rel
    if name in exact:
        assert sha(image)==sha(refs/rel),name
    return rel,image
def compare_cases(name,scenarios):
    rel,image=copy_path(name)
    results={}
    total=0
    for group,case_count,recipe,args in scenarios:
        outputs={}
        for role,beam in [('pinned',refs/rel),('generated',image)]:
            label=f'{name}-{group}-{role}'
            output=root/(label+'.terms')
            arguments=[beam,*args,output]
            evaluate(recipe,arguments,root,label)
            outputs[role]=sha(output)
        assert outputs['pinned']==outputs['generated'],(name,group)
        results[group]=outputs['generated']
        total+=case_count
    witnesses[name]=dict(Beam=sha(image),Observations=total,Outputs=results)
compare_cases('inet_sctp',[('invalid-call',4,stage/'native-sctp-1951.eval',[])])
wx=Path(specs['NestedRecordEffectDispatch']['Root'])
compare_cases('ex_frame_utils',[
    ('event',17,wx/'native-frame.eval',[inputs/'wx-demo-near-release-1975/stub']),
    ('initialization',2,inputs/'proven-tuple-option-generators-1976/native-init.eval',[inputs/'proven-tuple-option-generators-1976/stub'])])
asn=Path(specs['ClosedTerminalSelector']['Root'])
name='EnrollmentMessageSyntax-2009'
rel,image=copy_path(name)
previous=list((parent/'full-uncached').glob('shard-*/otp-corpus.json.generated/'+rel))
assert len(previous)==1
parent_root=next(path for path in previous[0].parents if path.name=='otp-corpus.json.generated')
evaluate(asn/'native-enrollment-terminal-selector.eval',[refs,parent_root,stage/'pilot.json.generated'],root,'enrollment-selector')
out=root/'enrollment-selector.stdout.log'
observations=re.findall(r'noSignature (\d+) cases, baseline \d+, candidate (\d+)',out.read_text(encoding='utf-8-sig'))
assert observations==[('10','10'),('10','10')],observations
witnesses[name]=dict(Beam=sha(image),Observations=20,Output=sha(out))
receipt=dict(Compiler=identity['Compiler'],Runtime=identity['Runtime'],Witnesses=witnesses,
             ActualObservations=sum(row['Observations'] for row in witnesses.values()))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt,indent=2))

