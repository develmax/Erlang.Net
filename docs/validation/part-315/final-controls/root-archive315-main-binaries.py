from pathlib import Path
import hashlib,json,zipfile
r=Path.cwd();d=r/'artifacts/root-part315-main-binaries1';assert not d.exists();d.mkdir()
h=lambda b:hashlib.sha256(b).hexdigest().upper()
rows=[]
for family in ['tests/Erlang.Tests/bin/Debug/net10.0','tools/Erlang.NifNativeBackend/bin/Debug/net10.0']:
 for p in sorted((r/family).rglob('*')):
  if p.is_file():rows.append({'Path':p.relative_to(r).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p.read_bytes())})
zp=d/'actual-main315-binaries.zip'
with zipfile.ZipFile(zp,'x',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 z.writestr('binary-index.json',json.dumps(rows,indent=2)+'\n')
 for row in rows:
  raw=(r/row['Path']).read_bytes();assert len(raw)==row['Bytes'] and h(raw)==row['Sha256'];z.writestr(row['Path'],raw)
assert zp.stat().st_size<100*1024*1024
with zipfile.ZipFile(zp) as z:
 assert len(z.namelist())==len(rows)+1 and json.loads(z.read('binary-index.json'))==rows
 for row in rows:
  raw=z.read(row['Path']);assert len(raw)==row['Bytes'] and h(raw)==row['Sha256']
receipt={'Status':'PASS_PASSIVE_ACTUAL_MAIN315_OUTPUT_ARCHIVE_REHASH','ArchiveSha256':h(zp.read_bytes()),'ArchiveBytes':zp.stat().st_size,'NativeGuardSha256':h((r/'artifacts/root-part315-main-windows-native1/guard.json').read_bytes()),'FullGuardSha256':h((r/'artifacts/root-part315-main1/full-guard.json').read_bytes()),'Files':rows,'SdkNativeOrVmExecuted':False}
(d/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps({k:v for k,v in receipt.items() if k!='Files'}))
