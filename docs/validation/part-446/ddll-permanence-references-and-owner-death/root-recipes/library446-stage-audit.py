from pathlib import Path
import gzip,hashlib,json,subprocess
r=Path.cwd();plan=json.loads((r/'artifacts/library446-publication-paths.json').read_bytes());folder=plan['EvidenceDirectory']
assert folder=='docs/validation/part-446/ddll-permanence-references-and-owner-death'
paths=list(plan['DocumentPaths']);paths += [p.relative_to(r).as_posix() for p in sorted((r/folder).rglob('*')) if p.is_file()]
assert len(paths)>6 and len(paths)==len(set(paths)) and not any('LICENSE.local' in p for p in paths)
pathlist=r/'artifacts/library446-exact-stage-paths.nul';pathlist.write_bytes(b'\0'.join(p.encode('utf-8') for p in paths)+b'\0')
subprocess.run(['git','add','--pathspec-from-file='+str(pathlist),'--pathspec-file-nul'],cwd=r,check=True)
staged=subprocess.check_output(['git','diff','--cached','--name-only','-z'],cwd=r).decode().strip('\0').split('\0');assert set(staged)==set(paths)
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes());old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'],cwd=r));assert ledger==old
assert len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
assert not subprocess.check_output(['git','diff','HEAD','--name-only','--','src','tests','tools'],cwd=r)
checks={row['Path']:row['Sha256'] for row in ledger['VerifiedSourceFiles']}
checks.update({folder+'/'+row['Path']:row['Sha256'] for row in json.loads((r/folder/'manifest.json').read_bytes())})
archive=json.loads((r/folder/'archive-map.json').read_bytes())
batch=subprocess.run(['git','cat-file','--batch'],cwd=r,input=''.join(':'+p+'\n' for p in checks).encode(),stdout=subprocess.PIPE,check=True).stdout
offset=0;blobs={}
for path,wanted in checks.items():
 eol=batch.index(b'\n',offset);head=batch[offset:eol].decode().split();assert head[1]=='blob',path
 count=int(head[2]);blob=batch[eol+1:eol+1+count];offset=eol+count+2
 assert hashlib.sha256(blob).hexdigest().upper()==wanted,path
 if path.startswith(folder+'/'):blobs[path.removeprefix(folder+'/')]=blob
assert offset==len(batch)
for row in archive:
 raw=gzip.decompress(blobs[row['Path']]) if row['Gzip'] else blobs[row['Path']]
 assert len(raw)==row['RawBytes'] and hashlib.sha256(raw).hexdigest().upper()==row['RawSha256'],row['Path']
subprocess.run(['git','diff','--cached','--check'],cwd=r,check=True)
report=dict(Passed=True,StagedFiles=len(staged),VerifiedIndexBlobs=len(checks),RawArchiveFilesVerified=len(archive),SourceGuards=1618,LedgerUnchanged=True,ProductionUnchanged=True,LicenseLocalExcluded=True,WholeModulePromotions=0)
(r/'artifacts/library446-index-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
