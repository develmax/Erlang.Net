from pathlib import Path
r=Path.cwd()
script=(r/'artifacts/root-priority351-diagnostic-prepare.py').read_text(encoding='utf-8')
script=script.replace("'artifacts/spawn-priority351-diagnostic'", "'artifacts/spawn-priority351-diagnostic2'").replace('CompiledModuleCases.Modules.Single','CompiledModuleCases.All.Single')
exec(compile(script,str(r/'artifacts/root-priority351-diagnostic-prepare2.py'),'exec'))
