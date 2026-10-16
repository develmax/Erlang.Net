from pathlib import Path
import hashlib
import json
import shutil
import subprocess

r=Path.cwd();a=r/'artifacts';b=r/'docs/validation/part-336'
h=lambda data:hashlib.sha256(data).hexdigest().upper()
j=lambda path:json.loads(path.read_bytes())
ready=j(a/'root-part336-readiness/module-readiness.json')
assert (ready['LocalPassedTests'],ready['RegisteredMfas'],ready['PreviousRegisteredMfas'],ready['RegisteredMfaDelta'])==(6952,536,536,0)
for name in ['MODULE_READINESS.md','module-readiness.json']:
    shutil.copyfile(a/'root-part336-readiness'/name,r/'docs'/name)
    shutil.copyfile(r/'docs'/name,b/name)
attributes=r/'.gitattributes'
value=attributes.read_text(encoding='utf-8')
value+='\ntools/Erlang.ProcLibFormatOriginalObservations/** -text whitespace=trailing-space,space-before-tab,cr-at-eol\n.github/workflows/proc-lib-format-original.yml -text whitespace=trailing-space,space-before-tab,cr-at-eol\n'
attributes.write_text(value,encoding='utf-8',newline='\n')
for name in ['root-checkpoint336.py','root-finalize336.py','root-part336-promotion.json']:
    shutil.copyfile(a/name,b/name)
review=dict(Status='ROOT_SOURCE_PEER_AND_POST_PROMOTION16_CONTROLS_PASS',SourcePaths=85,SourceManifestSha256='E1D4EFA41A36EE821379A0F6125926762BD36DF6F820FB7697DB2C522189A5DA',Reviewed=['Exact pinned source and same installed compiler/runtime/module provenance','Manifest closure and workflow-bound literal input','Fresh source/beam directories percase and original VM percase','Raw returned/raised/outcome ETF outside guest exception catch','Owned process-group timeout and joined stdout/stderr','Explicit latin1 printable range and source-defined encoding','Distinct compile/process/capture/guest outcomes','No SDK or SDK-derived expected values in collector'],LocalSourceControls=16,LinuxProcessControlsExecutedLocally=False,OriginalFormatterExecuted=False,RuntimeChanged=False,ReadinessMfaDelta=0)
(b/'root-source-review.json').write_text(json.dumps(review,indent=2)+'\n',encoding='utf-8')
rows=[dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p.read_bytes())) for p in sorted(b.rglob('*')) if p.is_file()]
index=b/'evidence-index.json'
index.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf-8')
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/PROGRESS.md','docs/COMPATIBILITY.md','docs/PROC_LIB_COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/semantic-differences.md','docs/DECISIONS.md','docs/dependency-decisions.md','docs/DEPENDENCY_PRIORITIES.md','docs/NEXT_STEPS.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md','docs/changelog/0052.md']
source=[row['TargetPath'] for row in j(b/'collector-source-freeze.json')['Files']]
proof=[p.relative_to(r).as_posix() for p in sorted(b.rglob('*')) if p.is_file()]
paths=sorted(source+metadata+proof)
assert len(paths)==len(set(paths)) and len(source)==85
(a/'root-part336-stage-plan.json').write_text(json.dumps(dict(Paths=paths,Metadata=metadata,Source=source,Proof=proof),indent=2)+'\n',encoding='utf-8')
(a/'root-part336-stage-paths.txt').write_text('\n'.join(paths)+'\n',encoding='utf-8')
print(json.dumps(dict(Source=len(source),Metadata=len(metadata),Proof=len(proof),Paths=len(paths),EvidenceBytes=sum(row['Bytes'] for row in rows),IndexSha256=h(index.read_bytes()))))
