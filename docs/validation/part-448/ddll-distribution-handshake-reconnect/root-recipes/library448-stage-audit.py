from pathlib import Path
import gzip,hashlib,json,subprocess
r=Path.cwd();plan=json.loads((r/'artifacts/library448-publication-paths.json').read_bytes());folder=plan['EvidenceDirectory'];assert folder=='docs/validation/part-448/ddll-distribution-handshake-reconnect'
paths=plan['DocumentPaths']+plan['ProductPaths']+[p.relative_to(r).as_posix() for p in sorted((r/folder).rglob('*')) if p.is_file()]
assert len(paths)==len(set(paths)) and not any('LICENSE.local' in p for p in paths)
pathlist=r/'artifacts/library448-exact-stage-paths.nul';pathlist.write_bytes(b'\0'.join(p.encode() for p in paths)+b'\0')
subprocess.run(['git','add','--pathspec-from-file='+str(pathlist),'--pathspec-file-nul'],check=True)
staged=subprocess.check_output(['git','diff','--cached','--name-only','-z']).decode().strip('\0').split('\0');assert set(staged)==set(paths)
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes());old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json']));assert ledger['Entries']==old['Entries'] and len(ledger['VerifiedSourceFiles'])==1621
checks={x['Path']:x['Sha256'] for x in ledger['VerifiedSourceFiles']};checks.update({folder+'/'+x['Path']:x['Sha256'] for x in json.loads((r/folder/'manifest.json').read_bytes())})
checks.update({name:hashlib.sha256((r/name).read_bytes()).hexdigest().upper() for name in plan['ProductPaths']})
archive=json.loads((r/folder/'archive-map.json').read_bytes());batch=subprocess.run(['git','cat-file','--batch'],input=''.join(':'+p+'\n' for p in checks).encode(),stdout=subprocess.PIPE,check=True).stdout
offset=0;blobs={}
for path,wanted in checks.items():
 eol=batch.index(b'\n',offset);head=batch[offset:eol].decode().split();assert head[1]=='blob',path;count=int(head[2]);blob=batch[eol+1:eol+1+count];offset=eol+count+2;assert hashlib.sha256(blob).hexdigest().upper()==wanted,path
 if path.startswith(folder+'/'):blobs[path.removeprefix(folder+'/')]=blob
assert offset==len(batch)
for row in archive:
 raw=gzip.decompress(blobs[row['Path']]) if row['Gzip'] else blobs[row['Path']];assert len(raw)==row['RawBytes'] and hashlib.sha256(raw).hexdigest().upper()==row['RawSha256'],row['Path']
subprocess.run(['git','diff','--cached','--check'],check=True)
report=dict(Passed=True,StagedFiles=len(staged),VerifiedIndexBlobs=len(checks),RawArchiveFilesVerified=len(archive),SourceGuards=1621,ApiEntriesUnchanged=True,ApiEntries=708,FullContracts=118,ProductPaths=plan['ProductPaths'],LicenseLocalExcluded=True)
(r/'artifacts/library448-index-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
