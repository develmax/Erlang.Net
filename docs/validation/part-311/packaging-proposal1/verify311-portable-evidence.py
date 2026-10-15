"""Independent archive-only replay. Requires no checkout candidate directories."""
from pathlib import Path,PurePosixPath
import hashlib,json,sys,zipfile,io
root=Path(sys.argv[1]);h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());v=j(root/'portable-index.json');authority=j(root/'input-authority.json');assert h((root/'input-authority.json').read_bytes())==v['InputAuthoritySha256'];zpath=root/v['Archive'];assert zpath.stat().st_size==v['ArchiveBytes'] and h(zpath.read_bytes())==v['ArchiveSha256'];required=set();count=0
with zipfile.ZipFile(zpath) as z:
 names=z.namelist();assert len(names)==len(set(names))==v['UniquePhysicalMembers'];assert z.testzip() is None
 for name in names:
  p=PurePosixPath(name);assert not p.is_absolute() and '..' not in p.parts and name=='sha256/'+h(z.read(name))
 inner={}
 def contents(row):
  if 'NestedMember' not in row:return z.read(row['ArchiveMember'])
  if row['ArchiveMember'] not in inner:
   data=z.read(row['ArchiveMember'])
   child=zipfile.ZipFile(io.BytesIO(data));children=child.namelist();assert len(children)==len(set(children)) and child.testzip() is None
   for name in children:assert not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts
   inner[row['ArchiveMember']]=child
  return inner[row['ArchiveMember']].read(row['NestedMember'])
 for declared in authority['Sets']:
  group=v['Sets'][declared['Name']];assert group['FreezeSha256']==declared['FreezeSha256'] and group['FrozenFiles']==declared['Count'];records=group['Records'];assert len(records)==len({x['Path'] for x in records})==declared['Count']+1
  frozen=[x for x in records if x['Sha256']==declared['FreezeSha256']];assert len(frozen)==1;f=json.loads(contents(frozen[0]));rows=f[declared.get('RowsField','Files')];assert len(rows)==declared['Count'];indexed={x['Path']:x for x in records}
  for row in rows:
   found=indexed[row['Path'].replace('\\','/')];assert (found['Bytes'],found['Sha256'])==(row['Bytes'],row['Sha256'])
  for row in records:
   p=PurePosixPath(row['Path']);assert not p.is_absolute() and '..' not in p.parts;data=contents(row);assert len(data)==row['Bytes'] and h(data)==row['Sha256'];required.add(row['ArchiveMember']);count+=1
  if declared.get('MembersZipSha256'):assert 'sha256/'+declared['MembersZipSha256'] in required
 assert required==set(names) and count==v['LogicalRecords']
report={'Status':'PASS_ARCHIVE_ONLY_EVERY_FROZEN_LOGICAL_MEMBER_REPLAY','ArchiveSha256':v['ArchiveSha256'],'LogicalRecords':count,'UniquePhysicalMembers':len(required),'SourceCandidateDirectoriesRead':False,'BuildExecuted':False,'NativeExecuted':False,'MainEdited':False,'GitMutated':False}
out=root/'independent-archive-replay-guard.json';assert not out.exists();out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
