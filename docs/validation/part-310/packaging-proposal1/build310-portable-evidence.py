"""Artifact-only deterministic content archive; input authority must be explicitly pinned."""
from pathlib import Path,PurePosixPath
import hashlib,json,sys,zipfile,io
r=Path.cwd().resolve();h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
authority=Path(sys.argv[1]);out=Path(sys.argv[2]);assert not out.exists();v=j(authority);sets={};blobs={};containers=set()
for declared in v['Sets']:
 name=declared['Name'];assert name not in sets;root=(r/declared['Root']).resolve();assert root.is_relative_to(r)
 freeze=(r/declared['FreezePath']).resolve();fb=freeze.read_bytes();assert h(fb)==declared['FreezeSha256'];f=j(freeze);rows=f[declared.get('RowsField','Files')];assert len(rows)==declared['Count'];records=[];seen=set()
 sourcezip=None
 if declared.get('MembersZipPath'):
  zipbytes=(r/declared['MembersZipPath']).read_bytes();assert h(zipbytes)==declared['MembersZipSha256'];sourcezip=zipfile.ZipFile(io.BytesIO(zipbytes));blobs['sha256/'+h(zipbytes)]=zipbytes;containers.add('sha256/'+h(zipbytes))
 for row in [*rows,{'Path':freeze.relative_to(root).as_posix(),'Bytes':len(fb),'Sha256':h(fb)}]:
  logical=row['Path'].replace('\\','/');p=PurePosixPath(logical);assert not p.is_absolute() and '..' not in p.parts and logical not in seen;seen.add(logical)
  assert not {'.git','bin','obj','__pycache__','cache','.cache'}.intersection(p.parts) and not any(x.lower().startswith('license') for x in p.parts)
  source=(root/logical).resolve();assert source.is_relative_to(root)
  data=sourcezip.read(logical) if sourcezip and logical!=freeze.relative_to(root).as_posix() else source.read_bytes();digest=h(data);assert len(data)==row['Bytes'] and digest==row['Sha256'],str(source)
  member='sha256/'+digest;blobs.setdefault(member,data);records.append({'Path':logical,'Bytes':len(data),'Sha256':digest,'ArchiveMember':member})
  if logical.endswith('.zip'):containers.add(member)
 if sourcezip:sourcezip.close()
 sets[name]={'FreezeSha256':h(fb),'FrozenFiles':len(rows),'Records':records}
nested={}
for container in sorted(containers):
 with zipfile.ZipFile(io.BytesIO(blobs[container])) as z:
  names=z.namelist();assert len(names)==len(set(names)) and z.testzip() is None
  for name in names:
   p=PurePosixPath(name);assert not p.is_absolute() and '..' not in p.parts
   if name.endswith('/'):continue
   data=z.read(name);member='sha256/'+h(data)
   if member in blobs and member not in containers:nested.setdefault(member,(container,name))
for group in sets.values():
 for row in group['Records']:
  if row['ArchiveMember'] in nested:row['ArchiveMember'],row['NestedMember']=nested[row['ArchiveMember']]
used={row['ArchiveMember'] for group in sets.values() for row in group['Records']};blobs={name:data for name,data in blobs.items() if name in used}
out.mkdir(parents=True);archive=out/'immutable-evidence.zip'
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=1,allowZip64=True) as z:
 for name,data in sorted(blobs.items()):
  info=zipfile.ZipInfo(name,(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;info.create_system=3;info.external_attr=0o100644<<16;z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=1)
index={'Status':'IMMUTABLE_CANDIDATES_EVERY_LOGICAL_MEMBER_BOUND_TO_ONE_CONTENT_ARCHIVE','Archive':'immutable-evidence.zip','ArchiveSha256':h(archive.read_bytes()),'ArchiveBytes':archive.stat().st_size,'UniquePhysicalMembers':len(blobs),'LogicalRecords':sum(len(x['Records']) for x in sets.values()),'NestedOriginalArchiveReferences':sum('NestedMember' in row for group in sets.values() for row in group['Records']),'InputAuthoritySha256':h(authority.read_bytes()),'Sets':sets}
(out/'input-authority.json').write_bytes(authority.read_bytes());(out/'portable-index.json').write_text(json.dumps(index,indent=2)+'\n');print(json.dumps({key:index[key] for key in ['ArchiveSha256','ArchiveBytes','UniquePhysicalMembers','LogicalRecords']}))
