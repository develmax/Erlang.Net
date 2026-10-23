from pathlib import Path
source=Path('artifacts/library444-style-readiness.py').read_text()
assert source.count('artifacts/library444-style-readiness1')==1
assert source.count('dict(os.environ)')==2
source=source.replace('artifacts/library444-style-readiness1','artifacts/library446-style-readiness1')
source=source.replace('dict(os.environ)',"dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1')")
exec(compile(source,str(Path(__file__).resolve()),'exec'))
