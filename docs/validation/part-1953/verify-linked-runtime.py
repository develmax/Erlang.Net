from pathlib import Path
import hashlib,json,os,re,shutil,subprocess,time

inputs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118')
parent=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/tuple-conversions-and-entry-frames-1952')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/checked-entry-and-general-event-plans-1953-exact-retake')
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

deadline=time.monotonic()+1600
while not (stage/'pilot-receipt.json').is_file():
    if time.monotonic()>deadline:
        raise TimeoutError('Pilot receipt unavailable')
    time.sleep(3)

pilot=read(stage/'pilot.json')
identity=read(stage/'focused-identity.json')
assert pilot['Total']==read(stage/'scope.json')['Modules']
assert all(sha(payload/f'Erlang.{name}.dll')==identity[name] for name in ('Compiler','Runtime'))
x400='PKIX-X400Address-2009'
targets=['ex_cursor','ex_aui','ex_button',x400]
rows={Path(row['Beam'].replace(chr(92),'/')).stem:row for row in pilot['Modules']}
paths={name:rows[name]['Beam'].replace(chr(92),'/') for name in targets}
exact={name:paths[name] for name in targets if rows[name]['Status']=='Equal'}
assert 'ex_cursor' in exact
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
wx=Path(specs['GeneralNestedRecordEntryPlan']['Root'])
stubs={'ex_cursor':inputs/'cursor-checked-record-and-call-1978/stub',
       'ex_aui':wx/'ex_aui-stub','ex_button':wx/'ex_button-stub'}
scenario_counts={'ex_cursor':10,'ex_aui':9,'ex_button':5}
verify_count=root/'verify-terms-count.eval'
verify_count.write_text('[Path,Count]=init:get_plain_arguments(),{ok,[Terms]}=file:consult(Path),true=(length(Terms)=:=list_to_integer(Count)),halt().\n',encoding='utf-8')

def image_for(name):
    rel=paths[name]
    if name in exact:
        generated=root/'capture.json.generated'/rel
        assert sha(generated)==sha(refs/rel),name
        return generated
    return stage/'pilot.json.generated'/rel

for name,stub in stubs.items():
    rel=paths[name]
    image=image_for(name)
    observations={}
    for role,beam in [('pinned',refs/rel),('generated',image)]:
        label=f'{name}-{role}'
        output=root/(label+'.terms')
        if name=='ex_cursor':
            evaluate(stage/'native-ex-cursor-events.eval',[beam,stub,output],root,label)
        else:
            evaluate(wx/'native-events.eval',[name,beam,stub,output],root,label)
        evaluate(verify_count,[output,str(scenario_counts[name])],root,label+'-coverage')
        observations[role]=sha(output)
    assert observations['pinned']==observations['generated'],name
    witnesses[name]=dict(Beam=sha(image),Observations=scenario_counts[name],Output=observations['generated'])

asn=Path(specs['X400ExtensionDispatch']['Root'])
name=x400
rel=paths[name]
image=image_for(name)
previous=list((parent/'full-uncached').glob('shard-*/otp-corpus.json.generated/'+rel))
assert len(previous)==1
parent_root=next(path for path in previous[0].parents if path.name=='otp-corpus.json.generated')
test_root=root/'extensions'
target=test_root/rel
target.parent.mkdir(parents=True)
shutil.copyfile(image,target)
evaluate(asn/'native-x400-extension-selectors.eval',[refs,parent_root,test_root],root,'x400-extensions')
out=root/'x400-extensions.stdout.log'
observations=re.findall(r'(\d+) cases, baseline \d+, candidate (\d+)',out.read_text(encoding='utf-8-sig'))
assert observations==[('62','62'),('54','54')],observations
witnesses[name]=dict(Beam=sha(image),Observations=116,BehaviorCases=62,FunctionIndexChecks=54,Output=sha(out))
receipt=dict(Compiler=identity['Compiler'],Runtime=identity['Runtime'],Witnesses=witnesses,
             ActualObservations=sum(row['Observations'] for row in witnesses.values()))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt,indent=2))

