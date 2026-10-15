"""Root-only metadata update after actual terminal full; comparison retains published300."""
from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_bytes());parent='c2d2cc304d03201dd52592c77d0389062aea0268'
assert j(a/'root-part301-full-exit.json')['ExitCode']==0 and b'Validation passed' in (a/'root-part301-full-validation.log').read_bytes()
t=j(a/'tests.json');old=j(r/'docs/validation/part-300/tests.json');names=[x['Name'] for x in t['Tests']];oldnames=[x['Name'] for x in old['Tests']]
assert (t['Passed'],t['Failed'])==(6621,0) and len(names)==len(set(names))==6621 and len(oldnames)==6611 and [n for n in names if n in set(oldnames)]==oldnames and all(x['Status']=='Passed' for x in t['Tests'])
registry=r/'docs/supported-mfas.json';rows=j(registry);previous=json.loads(subprocess.check_output(['git','show',parent+':docs/supported-mfas.json']))
key=lambda row:(row['Module'],row['Function'],row['Arity']);new=('erlang','process_info',1)
assert len(rows)==503 and len(previous)==502 and [x for x in rows if key(x)!=new]==previous and len([x for x in rows if key(x)==new])==1
row=next(x for x in rows if key(x)==new);assert row['Status']=='Partially compatible'
raw_export=a/'root-part301-supported-mfas-raw-export.json';assert not raw_export.exists();raw_bytes=registry.read_bytes()
raw_export.write_bytes(raw_bytes)
raw_capture={'Status':'ACTUAL_TERMINAL_FULL503_EXPORT_COPIED_BEFORE_DERIVED_LIMITS_ANNOTATION','Source':'docs/supported-mfas.json','RawArtifact':raw_export.name,'RawSha256':hashlib.sha256(raw_bytes).hexdigest().upper(),'FullValidationLogSha256':hashlib.sha256((a/'root-part301-full-validation.log').read_bytes()).hexdigest().upper(),'ActualFullTestReportSha256':hashlib.sha256((a/'tests.json').read_bytes()).hexdigest().upper(),'RegisteredMfas':503,'AllOld502RowsUnchanged':True,'AnnotationOnlyNewMfaLimits':True,'SourceProgramModifiedByThisScript':False}
(a/'root-part301-supported-mfas-derived-metadata-receipt.json').write_text(json.dumps(raw_capture,indent=2)+'\n',encoding='utf-8')
row['Limits']='Supported logical default subset: twelve fields unregistered, thirteen registered; pinned total_heap_size, heap_size, stack_size and garbage_collection omitted. process_info/2 physical items remain unsupported; eight new original projections pending.'
registry.write_text(json.dumps(rows,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
scope=r/'docs/module-readiness-scope.json';s=j(scope);runtime=next(c for c in s['Components'] if c['Id']=='Runtime');assert not any('Part301:' in x for x in runtime.get('Implemented',[]))
runtime.setdefault('Implemented',[]).append('Part301: actual6621/full adds process_info/1 supported logical default subset through one existing coherent owner snapshot; twelve fields unregistered/thirteen registered, four pinned physical heap/stack/GC fields absent. /2 behavior unchanged. Four focused HOST groups, eight generated SDK cases and the actual registered MFA fixture route pass; new eight original projections pending. Registry503/erlang345 of351 is API presence only; exact seventeen-field, physical and whole-module parity remain incomplete.')
s['ComparisonTestReport']='validation/part-300/tests.json';scope.write_text(json.dumps(s,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
print('Prepared supported-subset MFA limitation and partial Runtime note; retain published300 comparison for measured +1 registry delta; root regenerates readiness')
