from pathlib import Path
source=Path('artifacts/library436-run-standard79.py').read_text()
assert source.count('artifacts/library436-standard79-final1')==1 and source.count('dict(os.environ)')==1
source=source.replace('artifacts/library436-standard79-final1','artifacts/library449-standard79-final1')
source=source.replace('dict(os.environ)',"dict(os.environ,DOTNET_PROCESSOR_COUNT='1',DOTNET_GCConserveMemory='9',MSBUILDDISABLENODEREUSE='1')")
exec(compile(source,str(Path(__file__).resolve()),'exec'))
