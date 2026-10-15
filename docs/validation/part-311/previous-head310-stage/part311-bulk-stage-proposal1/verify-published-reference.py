"""Artifact-only benchmark against a published head and previously accepted stage records."""
from pathlib import Path
import importlib.util,sys,json,hashlib,time
s=importlib.util.spec_from_file_location('bulk',Path(__file__).with_name('bulk-git.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
reference=Path(sys.argv[1]);head=sys.argv[2];out=Path(sys.argv[3]);assert not out.exists() and len(head)==40 and all(x in '0123456789abcdef' for x in head)
v=json.loads(reference.read_bytes());rows=v['Records'];paths=[x['Path'] for x in rows];start=time.monotonic();files,attrs=m.capture(paths,Path.cwd(),head);sha=lambda b:hashlib.sha256(b).hexdigest().upper()
for row in rows:
 data=files[row['Path']];assert len(data)==row['StagedBytes'] and sha(data)==row['StagedSha256'] and attrs[row['Path']]==row['GitAttributes'],row['Path']
report={'Status':'PASS_BULK_PUBLISHED_BLOBS_AND_ATTRIBUTES_EQUAL_PRIOR_STAGE_GUARD','Head':head,'ReferenceGuardSha256':sha(reference.read_bytes()),'VerifiedRecords':len(rows),'GitSubprocessesForAllBytesAndAttrs':2,'ElapsedSeconds':time.monotonic()-start,'ReferenceDefaultWhitespacePassed':v.get('DefaultGitWhitespaceCheckPassed'),'WhitespacePolicyChangedByThisBenchmark':False,'NewStageApplied':False,'MainEdited':False,'IndexEdited':False,'NativeExecuted':False}
out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
