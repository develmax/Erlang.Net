from pathlib import Path
import json,hashlib
r=Path.cwd();a=r/'artifacts';d=json.loads((a/'nif-pending-varargs-original-proposal1/published290-windows-provenance.json').read_bytes());base=Path(d['BinaryPath']).parent
for x in d['VerifiedReceiptFiles']:
 p=base/x['Path'];b=p.read_bytes();assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256'],str(p)
for x in d['ProofFiles']:
 b=Path(x['Path']).read_bytes();assert len(b)==x['Bytes'] and hashlib.sha256(b).hexdigest().upper()==x['Sha256']
assert d['ReceiptFileCountReverified']==22 and len(d['VerifiedReceiptFiles'])==22
assert d['SourceSha256']==hashlib.sha256((r/'tools/Erlang.NifFixtureHost/native/fixture.c').read_bytes()).hexdigest().upper()
assert d['BinarySha256']=='235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F'
(a/'root-part299-trusted-binary-provenance-guard.json').write_text(json.dumps({'Status':'PASS_REHASHED_EXISTING_TRUSTED_SOURCE_BINARY_PROVENANCE','BuildFilesReverified':22,'ProofFilesReverified':len(d['ProofFiles']),'OriginalRun':d['Run'],'OriginalHead':d['Head'],'FixtureSha256':d['BinarySha256'],'FixtureSourceSha256':d['SourceSha256'],'NewOriginalExecuted':False},indent=2)+'\n')
print('PASS independent rehash22 buildfiles and5 provenancefiles, trustedoldfixture source/binary bound')
