"""Root only, after actual full. Invocation: python this.py CONFIG.json"""
import importlib.util,sys,json
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common',Path(__file__).with_name('part306-common.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1])
p=m.pinned(c['ReadinessScopeBefore']); scope=m.read(p)
runtime=next(x for x in scope['Components'] if x['Id']=='Runtime')
assert not any('Part306:' in x for x in runtime.get('Implemented',[]))
for name in c['AddedNames']:
    if name not in runtime['TestPrefixes']: runtime['TestPrefixes'].append(name)
note=m.pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip()
assert note.startswith('Part306:') and 'whole' in note.lower() and 'incomplete' in note.lower()
runtime.setdefault('Implemented',[]).append(note)
scope['ComparisonTestReport']=c['ComparisonTestReport']
p.write_text(json.dumps(scope,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Runtime scope prepared; root must regenerate readiness and pin its actual results before finalization.')
