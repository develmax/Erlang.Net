from pathlib import Path
import json
p=Path('artifacts/nif-pending-varargs-original-proposal1/published290-windows-provenance.json');d=json.loads(p.read_bytes())
print(json.dumps({k:v for k,v in d.items() if not isinstance(v,(dict,list))},indent=2))
print({k:(len(v) if isinstance(v,(dict,list)) else type(v).__name__) for k,v in d.items()})
rows=json.loads(Path('artifacts/root-nif-native299-isolated/tokens31-after.json').read_bytes())
row=next(x for x in rows if x['Path'].endswith('/NifNativeCompiledRouteNames.cs'));ts=row['Tokens'];i=next(i for i,x in enumerate(ts) if x['Text']=='Source')
source=json.loads(ts[i+2]['Text']);w=Path('tools/Erlang.NifNativeBackend/compiled_source_fixture.erl').read_text()
assert source.replace('\r\n','\n')==w.replace('\r\n','\n')
print('PASS exact embedded source vs normalized Erlang witness')
