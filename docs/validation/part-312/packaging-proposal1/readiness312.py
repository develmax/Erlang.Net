"""Root only after reviewing actual gates; writes scope only, then Root runs existing CLI."""
import sys,json,importlib.util
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common312',Path(__file__).with_name('common312.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1]);p=m.pinned(c['ReadinessScopeCurrent']);scope=m.read(p);runtime=next(x for x in scope['Components'] if x['Id']=='Runtime')
assert not any('Part312:' in x for x in runtime.get('Implemented',[]))
for name in c['AddedHostNames']:
 if name not in runtime['TestPrefixes']:runtime['TestPrefixes'].append(name)
note=m.render(m.pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip(),c);assert note.startswith('Part312:')
runtime.setdefault('Implemented',[]).append(note);scope['ComparisonTestReport']=c['ComparisonTestReport'];p.write_text(json.dumps(scope,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Root scope updated; run existing readiness CLI, then rebind changed scope/readiness pins before finalizer.')
