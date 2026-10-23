from pathlib import Path
import importlib.util,json,os
r=Path.cwd();out=r/'artifacts/library447-gates1';assert not out.exists();out.mkdir()
py='C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
dotnet='C:/Program Files/dotnet/dotnet.exe';dotnet_sha='AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135'
path=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned447gates',path);owned=importlib.util.module_from_spec(s);s.loader.exec_module(owned)
env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1');results=[]
def run(name,command,seconds):
 try:owned.owned(command,out,name,seconds,env,str(r));results.append({'Name':name,'Passed':True});good=True
 except Exception as error:results.append({'Name':name,'Passed':False,'Failure':repr(error)});good=False
 (out/'stages.json').write_text(json.dumps(results,indent=2)+'\n');print(name,good,flush=True);return good
selected=run('select',[py,'-B','artifacts/library447-select-affected.py'],180)
if selected:run('affected',[py,'-B','artifacts/library413-final-regression-proposal1/run-shards.py','--repository','.','--selection','artifacts/library447-affected-selection1/selection.json','--output','artifacts/library447-affected-run1','--shard-seconds','180'],900)
common=['--repository','.','--fresh-parent-input','artifacts/library447-driver-package1/parent-input.json','--runtime-directory','tests/Erlang.Tests/bin/Debug/net10.0','--dotnet',dotnet,'--dotnet-sha256',dotnet_sha]
run('native8-retained36',[py,'-B','artifacts/library438-ddll-visibility-acceptance-proposal2/run-managed-root.py',*common,'--original','artifacts/library441-visibility-original2','--native','artifacts/library441-visibility-native2','--output','artifacts/library447-ddll-retained-managed1'],420)
run('retained446',[py,'-B','artifacts/library446-ddll-frontier-proposal3/run-managed-root.py',*common,'--original','artifacts/library446-ddll-frontier-original1','--output','artifacts/library447-retained446-managed1'],420)
run('boot79',[py,'-B','artifacts/library447-run-standard79.py'],300)
run('style-readiness',[py,'-B','artifacts/library447-style-readiness.py'],300)
passed=len(results)==6 and all(x['Passed'] for x in results)
(out/'completion.json').write_text(json.dumps({'Passed':passed,'Stages':results},indent=2)+'\n');raise SystemExit(0 if passed else 1)
