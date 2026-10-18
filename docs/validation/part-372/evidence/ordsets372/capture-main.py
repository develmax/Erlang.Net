from pathlib import Path
import hashlib,json,importlib.util,shutil,os,sys,argparse
r=Path.cwd();ap=argparse.ArgumentParser();ap.add_argument('--output',type=Path,required=True);a=ap.parse_args();o=a.output.resolve();assert not o.exists() and o.is_relative_to(r/'artifacts');o.mkdir(parents=True)
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());save=lambda p,x:p.write_bytes((json.dumps(x,indent=2)+'\n').encode())
helper=r/'artifacts/spawn-priority351-preparation/execute-root.py';assert h(helper)=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A';s=importlib.util.spec_from_file_location('owned372capture',helper);owned=importlib.util.module_from_spec(s);s.loader.exec_module(owned)
env={k:v for k,v in os.environ.items() if k.upper() not in {'GH_TOKEN','GITHUB_TOKEN','GITHUB_API_TOKEN'}};env['DOTNET_PROCESSOR_COUNT']='2';results=[]
for name,folder,cache in [('ordsets','Erlang.Ordsets372OriginalObservations','ordsets371-full-main-fixed2'),('numeric','Erlang.Numeric372OriginalObservations','erlang371-main-final1'),('usort','Erlang.OrdsetsUsort372OriginalObservations','ordsets371-full-main-fixed2')]:
 stage=o/name;stage.mkdir();capture=stage/'capture1';capture.mkdir();tool=r/'artifacts/ordsets372-original-validation/tools'/folder;binpath=stage/'main-capture/bin/Debug/net10.0';source=r/'artifacts/ordsets372/sdk-harness/bin/Debug/net10.0';shutil.copytree(source,binpath)
 rows=[]
 for n,base in [('Compiler','src'),('Runtime','src'),('Terms','src'),('Serialization','src'),('Differential','tools')]:
  p=r/f'{base}/Erlang.{n}/bin/Debug/net10.0/Erlang.{n}.dll';q=binpath/p.name;shutil.copyfile(p,q);assert h(p)==h(q);rows.append(dict(Path=p.relative_to(r).as_posix(),Sha256=h(p),CopiedTo=q.relative_to(r).as_posix()))
 save(capture/'main-dll-closure.json',dict(MainSource=True,PrivateCandidateDllsUsed=False,Files=rows,HarnessSourceCache=cache,HarnessDllSha256=h(binpath/'MainCapture.dll')))
 error=None
 try:owned.owned(['C:/Program Files/dotnet/dotnet.exe',str(binpath/'MainCapture.dll'),str(r),str(tool),str(capture/'sdk')],capture,'erlang57-capture',600,env,r)
 except BaseException as e:error=repr(e)
 for row in rows:assert h(r/row['Path'])==row['Sha256']
 results.append(dict(Cohort=name,Error=error,SubjectModule=j(tool/'profile.json')['SubjectModule'],ToolManifestSha256=h(tool/'source-inputs.corrected.json')));save(o/'settlement.json',dict(OriginalExecuted=False,BuildExecuted=False,Results=results,MainDllsUnchanged=True))
print(json.dumps(results));sys.exit(1 if any(x['Error'] for x in results) else 0)
