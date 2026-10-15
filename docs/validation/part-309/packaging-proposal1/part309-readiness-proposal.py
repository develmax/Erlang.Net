import importlib.util,sys,json
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common',Path(__file__).with_name('part309-common.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c,f,index=m.gates(sys.argv[1]);p=m.pinned(c['ReadinessScopeBefore']);scope=m.read(p);component=next(x for x in scope['Components'] if x['Id']=='BEAM fixture evidence')
assert not any('Part309:' in x for x in component.get('Implemented',[]));note=m.render(m.pinned(c['MetadataNote']).read_text(encoding='utf-8-sig').strip(),c);component.setdefault('Implemented',[]).append(note);scope['ComparisonTestReport']='validation/part-308/tests.json';p.write_text(json.dumps(scope,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Standalone tooling scope prepared; previous308 report is the MFA-delta baseline, reused current tests remain6637.')
