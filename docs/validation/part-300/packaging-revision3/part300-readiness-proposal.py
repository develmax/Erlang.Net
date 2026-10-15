"""Root-only actual metadata update; no builds or tests are run by this script."""
from pathlib import Path
import json
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes())
assert j(a/'root-part300-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part300-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-299/tests.json');names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
assert t['Passed']==6611 and t['Failed']==0 and len(names)==len(set(names))==6611 and all(x['Status']=='Passed' for x in t['Tests']) and [n for n in names if n in set(oldnames)]==oldnames and [n for n in names if n not in set(oldnames)]==['runtime/nif-owned-purge-discard-shutdown-and-real-unwind']
scope=r/'docs/module-readiness-scope.json';s=j(scope);runtime=next(c for c in s['Components'] if c['Id']=='Runtime');tool=next(c for c in s['Components'] if c['Id']=='NifNativeBackend')
assert not any('Part300:' in line for c in [runtime,tool] for line in c.get('Implemented',[]))
runtime.setdefault('Implemented',[]).append('Part300: Main6611/full preserves6610 old ordered names plus one thirteen-scenario owned NIF lifetime HOST. Shutdown admission closure linearizes under the registry gate and may queue after core shutdown starts; winning admissions/commits remain owned and Dispose joins closure/owners before completion. Public purge metadata precedes asynchronous cleanup. Genuine retirement-originated halt red failure is fixed by independent actor ownership isolation. Preserved initial full6610/1 and first focused timeout exposed two Dispose lock-order regressions; guarded dormant inline versus owned asynchronous closure passes both unchanged historical tests and refuses active caller-monitor shutdown before mutation. Default-provider native init/unload fence remains bounded; no public load_nif/general ABI/resource/dirty-work/raw failed-on_load or full shutdown equivalence.')
tool.setdefault('Implemented',[]).append('Part300: separately recorded post-full Windows trusted native/generated-source checks include controlled actual-owner fence cancellation/re-entry and non-default provider refusal. Published299 Linux SDK checks and separate original13 typed cases independently pass; actual generated C# hash matches Windows. No thirteen-case SDK/original differential claim; new same-head native/ordinary checks pending.')
tool['Remaining']=[line.replace('Linux native execution and original13 terminal proof pending;', 'Changed lifetime source needs new exact-head original/native observations;') for line in tool.get('Remaining',[])]
s['ComparisonTestReport']='validation/part-300/tests.json';target=r/'docs/validation/part-300/tests.json';raw=(a/'tests.json').read_bytes()
if target.exists():assert target.read_bytes()==raw
else:assert not target.parent.exists()
target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(raw);scope.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');print('Prepared actual6611 comparison and partial Runtime/native-tool notes; root regenerates readiness')
