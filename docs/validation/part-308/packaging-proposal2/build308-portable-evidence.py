"""Artifact-only deterministic archival; no build, native execution or source edits."""
from pathlib import Path,PurePosixPath
import hashlib,json,zipfile,shutil
r=Path.cwd();a=r/'artifacts';source=a/'root-part308-combined-validated1';out=a/'part308-composite-portable1';h=lambda data:hashlib.sha256(data).hexdigest().upper();j=lambda p:json.loads(p.read_bytes())
assert not out.exists();out.mkdir();(out/'blobs').mkdir()
freeze=source/'validation-freeze.json';f=j(freeze);assert f['FileCount']==len(f['Files'])==2189
authority={'CompositeFreezePath':freeze.relative_to(r).as_posix(),'CompositeFreezeSha256':h(freeze.read_bytes()),'PromotedSourceFreezePath':'artifacts/root-part308-final-source-freeze.json','PromotedSourceFreezeSha256':'C1A2377E970ECDE5E2AF5C298D4EBC32BD0E45E59FD6F9EFDC2EB57BE6F5492D'}
assert h((r/authority['PromotedSourceFreezePath']).read_bytes())==authority['PromotedSourceFreezeSha256']
members={};archive=out/'composite2189.zip'
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED,compresslevel=1,allowZip64=True) as z:
 for row in sorted(f['Files'],key=lambda x:x['Path']):
  name=row['Path'];p=PurePosixPath(name);assert not p.is_absolute() and '..' not in p.parts and not {'bin','obj','__pycache__','.git'}.intersection(p.parts)
  data=(source/name).read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256']
  info=zipfile.ZipInfo(name,(1980,1,1,0,0,0));info.compress_type=zipfile.ZIP_DEFLATED;info.create_system=3;info.external_attr=0o100644<<16
  z.writestr(info,data,compress_type=zipfile.ZIP_DEFLATED,compresslevel=1);members.setdefault((len(data),h(data)),name)
shutil.copyfile(freeze,out/'composite-validation-freeze.json');shutil.copyfile(source/'compiled-source-closure.json',out/'compiled-source-closure.json')
sets={};blobs={}
def reference(sourcefile,logical):
 data=sourcefile.read_bytes();digest=h(data);key=(len(data),digest)
 row={'Path':logical,'Bytes':len(data),'Sha256':digest}
 if key in members:row['CompositeMember']=members[key]
 else:
  name='blobs/'+digest;row['BlobPath']=name
  if digest not in blobs:(out/name).write_bytes(data);blobs[digest]=len(data)
 return row
for directory in ['beam-closure-pending-entry311-validated1','indirect-load-nif312-validated2']:
 root=a/directory;fp=root/'validation-freeze.json';v=j(fp);records=[]
 for row in v['Files']:
  p=root/row['Path'];data=p.read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256'];records.append(reference(p,row['Path']))
 records.append(reference(fp,'validation-freeze.json'));sets[directory]={'FreezeSha256':h(fp.read_bytes()),'Records':records}
portable=a/'nif-allocation309-ledger-portable-proof1/portable-validation-manifest.json';v=j(portable);assert len(v['Files'])==838 and v['CompiledSourceInputCount']==822
records=[];prefix='docs/validation/part-308/nif-allocation-ledger/'
for row in v['Files']:
 assert row['Destination'].startswith(prefix);p=r/row['SourcePath'];data=p.read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256'];records.append(reference(p,row['Destination'][len(prefix):]))
for p in sorted(portable.parent.iterdir()):
 if p.is_file():records.append(reference(p,'portable-index/'+p.name))
sets['ledger-executed-portable']={'FreezeSha256':h(portable.read_bytes()),'Records':records,'ActualCompiledInputs':822,'BaselineTreeCopies':0}
root=a/'nif-allocation309-ledger-candidate3';fp=root/'source-freeze-sourceonly.json';v=j(fp);records=[]
for row in v['CandidateFiles']:
 p=root/'overlay'/row['Path'];data=p.read_bytes();assert len(data)==row['Bytes'] and h(data)==row['Sha256'];records.append(reference(p,'overlay/'+row['Path']))
for p in sorted(root.iterdir()):
 if p.is_file():records.append(reference(p,p.name))
sets['ledger-final-constant-relocation']={'FreezeSha256':h(fp.read_bytes()),'Records':records,'Candidate3IsolatedExecutionClaimed':False}
index={'Status':'COMPLETE_IMMUTABLE_CANDIDATES_BY_COMPOSITE_MEMBER_OR_SINGLE_BLOB','CompositeZip':'composite2189.zip','CompositeZipSha256':h(archive.read_bytes()),'CompositeMemberCount':2189,'CompositeFreezeSha256':h(freeze.read_bytes()),'Sets':sets}
(out/'candidate-reference-index.json').write_text(json.dumps(index,indent=2)+'\n');(out/'source-authority.json').write_text(json.dumps(authority,indent=2)+'\n')
total=sum(len(v['Records']) for v in sets.values());reused=sum('CompositeMember' in x for v in sets.values() for x in v['Records'])
report={'Status':'DETERMINISTIC_ARCHIVE_CREATED_SOURCE_BYTES_VERIFIED','ArchiveSha256':h(archive.read_bytes()),'ArchiveBytes':archive.stat().st_size,'CompositeMembers':2189,'UncompressedCompositeBytes':sum(x['Bytes'] for x in f['Files']),'CandidateLogicalFiles':total,'ReusedCompositeMembers':reused,'UniqueBlobFiles':len(blobs),'UniqueBlobBytes':sum(blobs.values()),'BuildExecuted':False,'NativeExecuted':False,'MainEdited':False,'GitMutated':False}
(out/'archive-build-receipt.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
