from pathlib import Path
import json,os,runpy,time,hashlib
r=Path.cwd();b=r/'artifacts/statistics-full';o=b/'final550-batch02';o.mkdir();helper=runpy.run_path(str(r/'tools/Native/Erlang.DdllFormatterOriginalObservations/scripts/owned.py'));env=dict(os.environ,DOTNET_PROCESSOR_COUNT='2',DOTNET_GCConserveMemory='9');cases=json.loads((b/'final549-affected02/selection.json').read_text());cases+=['runtime/immutable-nested-closure-real-collector-native-stale-foreign-and-mutable-input'];cases+=['beam/unchanged-register-real-root-typed-cell-handler-clone-and-closed'];assert len(cases)==len(set(cases))==367;(o/'selection.json').write_text(json.dumps(cases,indent=2))
source=[Path(row['Path']) for row in json.loads((b/'final550-layout02/pairs.json').read_text())];packages=['Network','Management','Megaco','TestAnalysis','Platform','Gui'];paths=source+list((r/'tests/Erlang.Tests/bin/Debug/net10.0').glob('*.dll'))
for name in packages:paths+=list((r/f'tests/Erlang.Otp.{name}.Library.Tests/bin/Debug/net10.0').glob('*.dll'))
paths=list(dict.fromkeys(paths))
def snap():return {str(p):hashlib.sha256((r/p if not p.is_absolute() else p).read_bytes()).hexdigest().upper() for p in paths}
before=snap();(o/'inputs-before.json').write_text(json.dumps(before,indent=2));summary=[]
def run(label,cmd,timeout):
 d=o/label;d.mkdir();start=time.monotonic()
 try:rec=helper['owned'](cmd,d,label,timeout,env,str(r));passed=rec['ExitCode']==0
 except Exception:passed=False
 row=dict(Stage=label,Passed=passed,ElapsedSeconds=time.monotonic()-start);summary.append(row);(o/'summary.json').write_text(json.dumps(summary,indent=2));print(json.dumps(row),flush=True)
for i in range(0,len(cases),200):
 label='regression'+str(i//200+1);cmd=['C:/Program Files/dotnet/dotnet.exe',str(r/'tests/Erlang.Tests/bin/Debug/net10.0/Erlang.Tests.dll'),'--report',str(o/label/'partial.json')]
 for case in cases[i:i+200]:cmd+=['--case',case]
 run(label,cmd,180)
for name in packages:
 dll=r/f'tests/Erlang.Otp.{name}.Library.Tests/bin/Debug/net10.0/Erlang.Otp.{name}.Library.Tests.dll'
 if not dll.exists():summary.append(dict(Stage=name,Passed=False,PreparationMissing=True));continue
 run(name,['C:/Program Files/dotnet/dotnet.exe',str(dll),str(o/name/'managed')],180)
after=snap();(o/'inputs-after.json').write_text(json.dumps(after,indent=2));assert before==after;(o/'input-integrity.json').write_text(json.dumps(dict(Unchanged=True,Files=len(before))));assert all(x['Passed'] for x in summary)
