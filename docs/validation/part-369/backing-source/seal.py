from pathlib import Path
import hashlib,json
root=Path(__file__).resolve().parents[2]
base=Path(__file__).resolve().parent
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(p,v): p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
audit=root/'artifacts/binary-primitives369-source-audit1'
compiler={'beam_core_to_ssa.erl','beam_ssa_alias.erl','beam_ssa_destructive_update.erl','v3_core.erl'}
save(audit/'authority.json',[
    {'Path':('lib/compiler/src/' if p.name in compiler else 'erts/emulator/beam/')+p.name,'Sha256':sha(p),'Bytes':p.stat().st_size}
    for p in sorted((audit/'authority').iterdir())
])
for folder in [audit,base]:
    files=[p for p in sorted(folder.rglob('*')) if p.is_file() and p.name!='source-freeze.json']
    save(folder/'source-freeze.json',{'Status':'source-only unbuilt proposal; no compatibility promotion','Files':[{'Path':p.relative_to(folder).as_posix(),'Sha256':sha(p),'Bytes':p.stat().st_size} for p in files]})
    print(folder.name,sha(folder/'source-freeze.json'))
print('copy-plan',sha(base/'copy-plan.json'))
