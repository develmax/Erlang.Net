from pathlib import Path
import hashlib,json,shutil
r=Path.cwd();a=r/'artifacts';b=r/'docs/validation/part-337'
h=lambda data:hashlib.sha256(data).hexdigest().upper()
j=lambda path:json.loads(path.read_bytes())
ready=j(a/'root-part337-readiness/module-readiness.json')
assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'])==(7017,540,536,4)
proc=next(row for row in ready['Modules'] if row['Name']=='proc_lib')
assert (proc['RegisteredMfas'],proc['ReferenceDeclarations'])==(29,39)
for name in ['MODULE_READINESS.md','module-readiness.json']:
    shutil.copyfile(a/'root-part337-readiness'/name,r/'docs'/name)
    shutil.copyfile(r/'docs'/name,b/name)

def archive(directory,label,extensions=None):
    for source in sorted(directory.rglob('*')):
        if source.is_file() and source.suffix not in ['.zip','.dll','.pdb','.exe'] and '__pycache__' not in source.parts and (extensions is None or source.suffix in extensions):
            target=b/label/source.relative_to(directory);target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)

archive(a/'part335-remote-new-review','prior335',{'.json','.py'})
archive(a/'part336-remote-new-review','prior336')
archive(a/'formatter337-independent-peer','independent-formatter-peer')
for number in range(1,5):archive(a/f'proc-lib337-actual{number}',f'development/actual{number}',{'.cs','.json','.log'})
archive(a/'proc-lib337-full-source','development/first-restricted-full',{'.json','.log'})
for directory in ['proc-lib337-permanent-actual','proc-lib337-permanent-actual2','proc-lib337-permanent-actual3']:
    source=a/directory
    if source.exists():archive(source,'development/'+directory,{'.cs','.json','.log'})
for directory in ['proc-lib-unicode-source-independent','proc-lib-format-raw-fix','proc-lib-format-raw-fix-final','proc-lib337-permanent-tests','proc-lib337-plan-source']:
    archive(a/directory,'source-history/'+directory,{'.cs','.json','.py','.md'})
for source in sorted(a.glob('root-formatter-original-*.json')):
    target=b/'development'/source.name;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,target)
for name in ['root-checkpoint337.py','root-finalize337.py','root-part337-promotion.json','root-formatter-compare.py','root-correct335-reader-label.py','root-part336-format-original-dispatch-attempt.json','root-part336-format-original-dispatch-result.json']:
    shutil.copyfile(a/name,b/name)
for directory in ['proc-lib337-all-capture4','proc-lib337-primitive-capture']:
    archive(a/directory,'development/'+directory,{'.json'})
rows=[dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p.read_bytes())) for p in sorted(b.rglob('*')) if p.is_file() and p.name not in ['evidence-index.json','evidence-receipt.json']]
index=b/'evidence-index.json';index.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
receipt=dict(Status='LOCAL337_FULL_AND_INDEPENDENT_ORIGINAL76_COMPLETE',Passed=7017,Registry=540,ProcLib=29,OriginalReportCases=57,OriginalPrimitiveCases=19,RawReportCases=12,RawTextNormalization=False,Prior335Original=6114,Prior335Fd=8,Prior336Ci=6952,Current337CiVerified=False,CopiedFiles=len(rows),CopiedBytes=sum(row['Bytes'] for row in rows),EvidenceIndexSha256=h(index.read_bytes()))
(b/'evidence-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
source=[row['Path'] for row in j(a/'proc-lib337-full-source2/source-freeze.json')['Files']]
metadata=['AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/PROC_LIB_COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/semantic-differences.md','docs/DECISIONS.md','docs/dependency-decisions.md','docs/DEPENDENCY_PRIORITIES.md','docs/NEXT_STEPS.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md','docs/changelog/0052.md','docs/supported-mfas.json']
proof=[p.relative_to(r).as_posix() for p in sorted(b.rglob('*')) if p.is_file()]
paths=sorted(source+metadata+proof);assert len(paths)==len(set(paths)) and len(source)==54
(a/'root-part337-stage-plan.json').write_text(json.dumps(dict(Paths=paths,Metadata=metadata,Source=source,Proof=proof),indent=2)+'\n',encoding='utf-8')
(a/'root-part337-stage-paths.txt').write_text('\n'.join(paths)+'\n',encoding='utf-8')
print(json.dumps(dict(**receipt,StagePaths=len(paths))))
