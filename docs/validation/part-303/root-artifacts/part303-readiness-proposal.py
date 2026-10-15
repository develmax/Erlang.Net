"""Root-only after actual terminal full; preserve raw exporter bytes before derived metadata."""
from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();parent='ce3636a9d17a4d1e1c979c57b4df3d0800b72134'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==parent
assert j(a/'root-part303-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part303-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-301/tests.json');names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
assert (t['Passed'],t['Failed'])==(6622,0) and len(names)==len(set(names))==6622 and len(oldnames)==6621 and [n for n in names if n in set(oldnames)]==oldnames and set(names)-set(oldnames)=={'runtime/direct-source-load-nif-exact-caller-capability'} and all(x['Status']=='Passed' for x in t['Tests'])
registry=r/'docs/supported-mfas.json';raw=registry.read_bytes();rows=j(registry);previous=json.loads(subprocess.check_output(['git','cat-file','blob',parent+':docs/supported-mfas.json']));key=lambda q:(q['Module'],q['Function'],q['Arity']);new=('erlang','process_info',1)
assert len(rows)==len(previous)==503 and [key(x) for x in rows]==[key(x) for x in previous]
for before,after in zip(previous,rows):assert (before==after if key(before)!=new else {k:v for k,v in before.items() if k!='Limits'}=={k:v for k,v in after.items() if k!='Limits'})
path=a/'root-part303-supported-mfas-raw-export.json';assert not path.exists();path.write_bytes(raw)
row=next(x for x in rows if key(x)==new);row['Limits']=next(x['Limits'] for x in previous if key(x)==new);assert rows==previous
registry.write_text(json.dumps(rows,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
receipt={'Status':'ACTUAL_TERMINAL_FULL503_EXPORT_PRESERVED_BEFORE_EXACT_PUBLISHED_SUBSET_LIMITS_RESTORATION','RawSha256':h(raw),'DerivedSha256':h(registry.read_bytes()),'FullValidationLogSha256':h((a/'root-part303-full-validation.log').read_bytes()),'ActualFullTestReportSha256':h((a/'tests.json').read_bytes()),'RegisteredMfas':503,'All503RowsEqualPublishedAfterRestoration':True,'OnlyProcessInfo1LimitsRestored':True,'SourceProgramExporterModified':False}
(a/'root-part303-supported-mfas-derived-metadata-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
scope=r/'docs/module-readiness-scope.json';s=j(scope);runtime=next(c for c in s['Components'] if c['Id']=='Runtime');assert not any('Part303:' in x for x in runtime.get('Implemented',[]))
runtime.setdefault('Implemented',[]).append('Part303: actual6622/full and thirteen focused groups cover an explicitly installed internal direct-source load_nif caller capability with exact image/owner lease, both argument effects and failures, actual generated managed entry and cleanup. No public load_nif MFA/default handler/native loader or ABI activation. Registry503/erlang345 of351 unchanged. Native source closure1007; actual post-full trusted-profile checks remain separate. Source-only pin audit, controlled staged-image evaluation and historical source entry do not establish BEAM i_load_nif or native on_load execution.')
s['ComparisonTestReport']='validation/part-301/tests.json';scope.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8');print('Prepared partial Runtime note and exact existing subset disclosure; root regenerates readiness with immutable301 baseline')
