from collections import Counter
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from time import perf_counter
import hashlib,json,os,statistics,subprocess,sys

stage,prior,dotnet,reference,headers,sources=map(Path,sys.argv[1:])
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def save(path,data):
    path.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8',newline='\n')
def rel(path):
    return path.replace(chr(92),'/')
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

manifest=stage/'pilot-modules.txt'
modules=manifest.read_text(encoding='utf-8-sig').splitlines()
assert len(set(modules))==len(modules)==read(stage/'scope.json')['Modules']
times={}
for timing in sorted((prior/'full-uncached').glob('shard-*/otp-corpus.json.timings.json')):
    for item in read(timing)['Modules']:
        name=rel(item['Beam'])
        assert name not in times,name
        times[name]=item['Milliseconds']
assert set(modules)<=times.keys()
lanes=[[],[]]
loads=[0.,0.]
for name in sorted(modules,key=lambda name:(-times[name],name)):
    lane=min(range(len(lanes)),key=loads.__getitem__)
    lanes[lane].append(name)
    loads[lane]+=times[name]
regular=[sum(times[name] for name in modules[start:end]) for start,end in [(0,len(modules)//2),(len(modules)//2,len(modules))]]
plan={'Modules':len(modules),'Model':prior.as_posix(),'OriginalSelection':sha(manifest),
      'PredictedSeconds':[value/1000 for value in loads],
      'EstimatedContiguousHalfSeconds':[value/1000 for value in regular],
      'Groups':lanes,'CompilerCache':False}
save(stage/'pilot-partition-plan.json',plan)
timer=perf_counter()
tool=stage/'corpus-bin/Erlang.BeamCorpus.dll'
def run(index):
    outer=stage/f'pilot-lane-{index}'
    outer.mkdir()
    temp=outer/'temp'
    temp.mkdir()
    report=outer/'otp-corpus.json'
    subset=outer/'modules.txt'
    subset.write_text('\n'.join(sorted(lanes[index]))+'\n',encoding='utf-8',newline='\n')
    env={**os.environ,'TEMP':str(temp),'TMP':str(temp),'DOTNET_PROCESSOR_COUNT':'1',
         'ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES':'1','ERLANG_BEAM_CORPUS_TIMINGS':'1'}
    command=[str(dotnet),str(tool),str(reference),str(report),str(headers),str(sources),'--modules-file',str(subset),'--jobs','1']
    start=perf_counter()
    with (outer/'stdout.log').open('xb') as out,(outer/'stderr.log').open('xb') as error:
        status=subprocess.run(command,cwd=outer,env=env,stdout=out,stderr=error).returncode
    elapsed=perf_counter()-start
    assert status in (0,1),f'Scoped lane {index} returned {status}'
    result=read(report)
    actual=[rel(row['Beam']) for row in result['Modules']]
    if result['Total']!=len(lanes[index]) or set(actual)!=set(lanes[index]) or len(set(actual))!=len(actual):
        raise RuntimeError(f'Scoped lane {index} covered a different set of modules')
    return outer,result,elapsed

with ThreadPoolExecutor(max_workers=2) as workers:
    outcomes=list(workers.map(run,range(len(lanes))))
total=perf_counter()-timer
all_rows=sorted([item for _,report,_ in outcomes for item in report['Modules']],key=lambda row:row['Beam'])
assert len(all_rows)==len(modules)
assert {rel(row['Beam']) for row in all_rows}==set(modules)
states=Counter(row['Status'] for row in all_rows)
assert not (set(states)-{'Equal','Different','Unsupported','SourceRejected'})
for suffix in ('generated','sources'):
    destination=stage/f'pilot.json.{suffix}'
    destination.mkdir()
    for outer,_,_ in outcomes:
        origin=outer/f'otp-corpus.json.{suffix}'
        if not origin.is_dir():
            continue
        for file in origin.rglob('*'):
            if not file.is_file():
                continue
            path=destination/file.relative_to(origin)
            assert path.resolve().is_relative_to(destination.resolve())
            path.parent.mkdir(parents=True,exist_ok=True)
            os.link(file,path)
timings=[read(outer/'otp-corpus.json.timings.json') for outer,_,_ in outcomes]
save(stage/'pilot.json.timings.json',{'Jobs':2,'SetupMilliseconds':sum(row['SetupMilliseconds'] for row in timings),
     'ComparisonMilliseconds':1000*total,'Modules':[item for row in timings for item in row['Modules']]})
save(stage/'pilot.json',{'OtpVersion':'29.1.1','Total':len(all_rows),'Counts':dict(sorted(states.items())),'Modules':all_rows})
result=int(set(states)!={'Equal'})
save(stage/'pilot-receipt.json',{'ExitCode':result,'Seconds':total,'Count':len(all_rows),
    'Workers':2,'Lanes':[elapsed for _,_,elapsed in outcomes],'PlanningHash':sha(stage/'pilot-partition-plan.json')})
print(dict(Scope=len(all_rows), Counts=dict(states), Seconds=total))

