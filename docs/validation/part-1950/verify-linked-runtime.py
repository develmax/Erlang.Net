from pathlib import Path
import hashlib,json,os,shutil,subprocess,time

inputs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118')
stage=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/arithmetic-constructors-and-xml-wrappers-1950')
refs=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/library376-official-compiler/installed')
root=stage/'linked-runtime-witnesses'
payload=stage/'corpus-bin'
sdk=inputs/'sdk/dotnet.exe'
terminal=Path('D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/parity-inputs-20261118/binding-free-composite-case-selectors-1949')
root.mkdir()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def run(cmd,folder,label,**options):
    env={**os.environ,'ERL_CRASH_DUMP':str(folder/(label+'-crash.dump')),
         'ERL_FLAGS':'+S 1:1 +A 1','DOTNET_PROCESSOR_COUNT':'1', **options}
    with (folder/(label+'.stdout.log')).open('wb') as stdout, (folder/(label+'.stderr.log')).open('wb') as stderr:
        subprocess.run([str(arg) for arg in cmd],cwd=folder,env=env,stdout=stdout,stderr=stderr,check=True,timeout=150)

def evaluate(recipe,args,folder,label):
    run([refs/'bin/erl.exe','+S','1:1','+A','1','-noshell','-eval',recipe.read_text(encoding='utf-8-sig'),'-extra',*args],
        folder,label)

deadline=time.monotonic()+1200
while not (stage/'pilot-receipt.json').is_file():
    if time.monotonic()>deadline:
        raise TimeoutError('Pilot receipt missing')
    time.sleep(3)

pilot=read(stage/'pilot.json')
identity=read(stage/'focused-identity.json')
assert pilot['Total']==read(stage/'scope.json')['Modules']
assert all(sha(payload/f'Erlang.{name}.dll')==identity[name] for name in ('Compiler','Runtime'))
targets={'megaco_binary_term_id':8,'sasl_report_file_h':7,'wxXmlResource':14}
rows={Path(row['Beam'].replace(chr(92),'/')).stem:row for row in pilot['Modules']}
paths={name:rows[name]['Beam'].replace(chr(92),'/') for name in targets}
assert all(rows[name]['Status']=='Equal' for name in targets)
tool=root/'capture'
shutil.copytree(inputs/'boolean-dispatch-and-wx-remainder-1927/early-raw-linked/tool',tool)
for name in ('Compiler','Runtime'):
    library=f'Erlang.{name}.dll'
    shutil.copyfile(payload/library,tool/library)
manifest=root/'modules.txt'
manifest.write_text('\n'.join(paths.values())+'\n',encoding='utf-8',newline='\n')
report=root/'capture.json'
temp=root/'temp'
temp.mkdir()
run([sdk,tool/'Erlang.BeamCorpus.dll',refs,report,inputs/'public-key-generated',inputs/'pinned-inputs-full',
     '--modules-file',manifest,'--jobs','1'],root,'capture',
     TEMP=str(temp),TMP=str(temp),ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES='1',ERLANG_BEAM_CORPUS_TIMINGS='1')
assert read(report)['Counts']['Equal']==len(targets)

witnesses={}
wx=inputs/'late-outgoing-root-trims-1974'
stub=inputs/'xml-resource-wx-wrappers-1973/stub'
for name,count in targets.items():
    rel=paths[name]
    image=root/'capture.json.generated'/rel
    oracle=refs/rel
    assert sha(image)==sha(oracle),name
    outputs={}
    for role,beam in [('pinned',oracle),('generated',image)]:
        output=root/(name+'-'+role+'.terms')
        if name=='wxXmlResource':
            evaluate(wx/'native-resource-stack.eval',[beam,stub,'all',output],root,name+'-'+role)
        else:
            evaluate(stage/'native-release-1950.eval',[name,beam,output,root/'sasl-runtime.log'],root,name+'-'+role)
        outputs[role]=sha(output)
    assert outputs['pinned']==outputs['generated'],name
    witnesses[name]=dict(Beam=sha(image),Observations=count,Output=outputs['generated'])

fixture=root/'terminal-reconstruction'
shutil.copytree(terminal/'regression-candidate',fixture)
for name in ('Compiler','Runtime'):
    library=f'Erlang.{name}.dll'
    shutil.copyfile(payload/library,fixture/library)
image=fixture/'terminal-reconstruction.beam'
run([sdk,fixture/'Erlang.Tests.dll',image],fixture,'compile')
execute=root/'terminal-reconstruction.eval'
execute.write_text('[Path]=init:get_plain_arguments(),{ok,B}=file:read_file(Path),M=beam_terminal_reconstruction,{module,M}=code:load_binary(M,Path,B),io:format("~tp.~n",[M:observe()]),halt().\n',encoding='utf-8')
evaluate(execute,[image],fixture,'native')
assert sha(fixture/'native.stdout.log')==sha(terminal/'pinned-witness.terms')
receipt=dict(Compiler=identity['Compiler'],Runtime=identity['Runtime'],Witnesses=witnesses,
             SyntheticFixture=dict(Beam=sha(image),NativeOutput=sha(fixture/'native.stdout.log')),
             ActualObservations=sum(targets.values()))
(root/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps(receipt,indent=2))

