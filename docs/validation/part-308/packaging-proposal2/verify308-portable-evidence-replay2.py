"""Independent passive archive replay; verifies every composite and candidate byte."""
from pathlib import Path,PurePosixPath
import hashlib,json,zipfile,sys
r=Path.cwd();b=Path(sys.argv[1]) if len(sys.argv)>1 else r/'artifacts/part308-composite-portable1';j=lambda p:json.loads(p.read_bytes());h=lambda data:hashlib.sha256(data).hexdigest().upper()
freeze=b/'composite-validation-freeze.json';f=j(freeze);index=j(b/'candidate-reference-index.json');archive=b/'composite2189.zip'
assert len(f['Files'])==f['FileCount']==index['CompositeMemberCount']==2189 and h(freeze.read_bytes())==index['CompositeFreezeSha256'] and h(archive.read_bytes())==index['CompositeZipSha256']
with zipfile.ZipFile(archive) as z:
 assert len(z.namelist())==len(set(z.namelist()))==2189 and set(z.namelist())=={x['Path'] for x in f['Files']}
 for row in f['Files']:
  p=PurePosixPath(row['Path']);assert not p.is_absolute() and '..' not in p.parts
  data=z.read(row['Path']);assert len(data)==row['Bytes'] and h(data)==row['Sha256']
 count=0
 for name,group in index['Sets'].items():
  assert len({x['Path'] for x in group['Records']})==len(group['Records'])
  for row in group['Records']:
   p=PurePosixPath(row['Path']);assert not p.is_absolute() and '..' not in p.parts
   data=z.read(row['CompositeMember']) if 'CompositeMember' in row else (b/row['BlobPath']).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256'];count+=1
source=j(b/'source-authority.json');assert source['CompositeFreezeSha256']==h(freeze.read_bytes())
report={'Status':'PASS_INDEPENDENT_COMPLETE_COMPOSITE_AND_CANDIDATE_ARCHIVE_REPLAY','CompositeMembersVerified':2189,'CandidateLogicalFilesVerified':count,'ArchiveSha256':h(archive.read_bytes()),'CompositeFreezeSha256':h(freeze.read_bytes()),'CandidateReferenceIndexSha256':h((b/'candidate-reference-index.json').read_bytes()),'CompiledClosureSha256':h((b/'compiled-source-closure.json').read_bytes()),'NativeExecuted':False,'BuildExecuted':False,'OriginalExecuted':False,'MainEdited':False,'GitMutated':False}
out=Path(sys.argv[2]) if len(sys.argv)>2 else None
if out is not None:assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report))
