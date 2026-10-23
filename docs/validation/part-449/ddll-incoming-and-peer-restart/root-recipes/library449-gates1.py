from pathlib import Path
import importlib.util,json,os
r=Path.cwd();out=r/'artifacts/library449-gates1';assert not out.exists();out.mkdir()
p=r/'artifacts/spawn-priority351-preparation/execute-root.py';s=importlib.util.spec_from_file_location('owned449gates',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
py='C:/Users/Maksim/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe';exe='C:/Program Files/dotnet/dotnet.exe';env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1');rows=[]
def run(name,args,seconds):
 try:m.owned(args,out,name,seconds,env,str(r));passed=True;rows.append({'Name':name,'Passed':True})
 except Exception as error:passed=False;rows.append({'Name':name,'Passed':False,'Failure':repr(error)})
 (out/'stages.json').write_text(json.dumps(rows,indent=2)+'\n');print(name,passed,flush=True);return passed
if run('select',[py,'-B','artifacts/library449-select-affected.py'],120):run('affected',[py,'-B','artifacts/library413-final-regression-proposal1/run-shards.py','--repository','.','--selection','artifacts/library449-affected-selection1/selection.json','--output','artifacts/library449-affected-run1','--shard-seconds','180'],1200)
run('native8-retained36',[py,'-B','artifacts/library438-ddll-visibility-acceptance-proposal2/run-managed-root.py','--repository','.','--fresh-parent-input','artifacts/library449-driver-package1/parent-input.json','--runtime-directory','tests/Erlang.Tests/bin/Debug/net10.0','--dotnet',exe,'--dotnet-sha256','AB1B71FD3DD71062E074C9FAB8312081A81B7F2B3E0327C48C4D249C8D1A3135','--original','artifacts/library441-visibility-original2','--native','artifacts/library441-visibility-native2','--output','artifacts/library449-native-retained-managed1'],420)
run('boot79',[py,'-B','artifacts/library449-run-standard79.py'],300)
run('style',[exe,'tools/Erlang.Style/bin/Debug/net10.0/Erlang.Style.dll','--check','.'],120)
passed=len(rows)==5 and all(x['Passed'] for x in rows);(out/'completion.json').write_text(json.dumps({'Passed':passed,'Stages':rows},indent=2)+'\n');raise SystemExit(0 if passed else 1)
