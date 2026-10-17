from pathlib import Path
r=Path.cwd()
script=(r/'artifacts/root-priority351-diagnostic-prepare.py').read_text(encoding='utf-8')
script=script.replace("'artifacts/spawn-priority351-diagnostic'", "'artifacts/spawn-priority351-diagnostic3'").replace('CompiledModuleCases.Modules.Single','CompiledModuleCases.All.Single')
script=script.replace('GeneratedModuleCompiler.Compile(fixture.Source, fixture.CompilerPolicy)', 'GeneratedModuleCompiler.Compile(fixture.Source)')
exec(compile(script,str(r/'artifacts/root-priority351-diagnostic-prepare3.py'),'exec'))
