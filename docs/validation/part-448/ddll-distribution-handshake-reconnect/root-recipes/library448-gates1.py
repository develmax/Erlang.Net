from pathlib import Path
import importlib.util,json,os
r=Path.cwd();out=r/'artifacts/library448-gates1';assert not out.exists();out.mkdir()
py='C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';dotnet='C:/Program Files/dotnet/dotnet.exe';dotnet_sha='AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135'
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned448gates',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1');rows=[]
def run(name,args,seconds):
 try:m.owned(args,out,name,seconds,env,str(r));rows.append(dict(Name=name,Passed=True));passed=True
 except Exception as error:rows.append(dict(Name=name,Passed=False,Failure=repr(error)));passed=False
 (out/'stages.json').write_text(json.dumps(rows,indent=2)+'\n');print(name,passed,flush=True);return passed
run('build-tool',[dotnet,'build','tools/Erlang.Tool/Erlang.Tool.csproj','--no-restore','-m:1','-nr:false','--nologo','--disable-build-servers','-p:UseSharedCompilation=false','-p:ConcurrentBuild=false','-p:BuildInParallel=false'],180)
if run('select',[py,'-B','artifacts/library448-select-affected.py'],180):run('affected',[py,'-B','artifacts/library413-final-regression-proposal1/run-shards.py','--repository','.','--selection','artifacts/library448-affected-selection1/selection.json','--output','artifacts/library448-affected-run1','--shard-seconds','180'],1200)
common=['--repository','.','--fresh-parent-input','artifacts/library448-driver-package2/parent-input.json','--runtime-directory','tests/Erlang.Tests/bin/Debug/net10.0','--dotnet',dotnet,'--dotnet-sha256',dotnet_sha]
run('native8-retained36',[py,'-B','artifacts/library438-ddll-visibility-acceptance-proposal2/run-managed-root.py',*common,'--original','artifacts/library441-visibility-original2','--native','artifacts/library441-visibility-native2','--output','artifacts/library448-ddll-retained-managed1'],420)
run('retained446',[py,'-B','artifacts/library446-ddll-frontier-proposal3/run-managed-root.py',*common,'--original','artifacts/library446-ddll-frontier-original1','--output','artifacts/library448-retained446-managed1'],420)
run('retained447',[py,'-B','artifacts/library448-replay447.py','--output','artifacts/library448-retained447-run1'],420)
run('boot79',[py,'-B','artifacts/library448-run-standard79.py'],300)
run('style-readiness',[py,'-B','artifacts/library448-style-readiness.py'],300)
passed=len(rows)==8 and all(x['Passed'] for x in rows);(out/'completion.json').write_text(json.dumps(dict(Passed=passed,Stages=rows),indent=2)+'\n');raise SystemExit(0 if passed else 1)
