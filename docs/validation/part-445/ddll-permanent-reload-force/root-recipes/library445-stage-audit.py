from pathlib import Path
import gzip,hashlib,json,subprocess
r=Path.cwd();folder='docs/validation/part-445/ddll-permanent-reload-force'
paths=['AGENTS.md','docs/API_DELIVERY_PACKAGES.md','docs/NATIVE_API_LIMITATIONS.md','docs/WORK_PLAN.md','docs/changelog/0078.md','docs/remaining-work/INDEX.md','docs/mfa-compatibility.json','docs/module-readiness.json','src/Erlang.Runtime/RuntimeDdllParent422.cs']
paths += [p.relative_to(r).as_posix() for p in sorted((r/folder).rglob('*')) if p.is_file()]
assert len(paths)>9 and len(paths)==len(set(paths))
pathlist=r/'artifacts/library445-exact-stage-paths.nul';pathlist.write_bytes(b'\0'.join(p.encode('utf-8') for p in paths)+b'\0')
subprocess.run(['git','add','--pathspec-from-file='+str(pathlist),'--pathspec-file-nul'],cwd=r,check=True)
staged=subprocess.check_output(['git','diff','--cached','--name-only','-z'],cwd=r).decode().strip('\0').split('\0')
assert set(staged)==set(paths) and not any('LICENSE.local' in p for p in staged)
ledger=json.loads((r/'docs/mfa-compatibility.json').read_bytes());old=json.loads(subprocess.check_output(['git','show','HEAD:docs/mfa-compatibility.json'],cwd=r))
assert ledger['Entries']==old['Entries'] and len(ledger['Entries'])==708 and len(ledger['VerifiedSourceFiles'])==1618
changes=[(a,b) for a,b in zip(old['VerifiedSourceFiles'],ledger['VerifiedSourceFiles']) if a!=b]
assert len(changes)==1 and changes[0][0]['Path']==changes[0][1]['Path']=='src/Erlang.Runtime/RuntimeDdllParent422.cs'
assert changes[0][0]['Sha256']=='E290340C18D91308DDEEFFF9DDC24CE9DF465BBC99D3EEB3151D8D36B4E4060C'
assert changes[0][1]['Sha256']=='CD1BAAF05B59F5D1F2EE20C4186C16D617AC7D5CB326B93EAB1C0E80639256AE'
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
report=dict(Passed=True,StagedFiles=len(staged),VerifiedIndexBlobs=len(checks),RawArchiveFilesVerified=len(archive),SourceGuards=1618,ClassificationsUnchanged=True,ProductionFilesChanged=1,LicenseLocalExcluded=True,WholeModulePromotions=0)
(r/'artifacts/library445-index-audit.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
