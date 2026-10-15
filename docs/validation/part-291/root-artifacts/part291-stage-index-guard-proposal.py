"""PROPOSAL ONLY: root executes after archive and metadata/readiness completion.
Stages only explicitly allowlisted current source/metadata and all receipt proof.
"""
from pathlib import Path
import hashlib,json,subprocess

r=Path.cwd().resolve();a=r/'artifacts';o=r/'docs/validation/part-291'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
git=lambda *args:subprocess.check_output(['git',*args],cwd=r)
parent='f2e861afc3f1ff2bdfade705ecb058e7cef433d6'
assert git('rev-parse','HEAD').decode().strip()==parent
f=read(a/'root-part291-final-source-freeze.json');v=read(o/'evidence-receipt.json')
assert f['ParentHead']==v['ParentHead']==parent and len(f['Files'])==v['SourcePaths']==21
assert v['Passed']==6599 and v['Failed']==0 and v['Historical6588OrderedNamesExact'] and v['RegisteredMfas']==502
assert v['HistoricalGeneratedCSharpExact']==4895 and v['PriorOriginalNifCasesPassed']==7 and v['PriorLinuxProcessControlsPassed']==2
proof_paths=[p.relative_to(r).as_posix() for p in sorted(o.rglob('*')) if p.is_file()]
receipt_paths={'docs/validation/part-291/'+x['Path'] for x in v['Files']}
assert set(proof_paths)==receipt_paths|{'docs/validation/part-291/evidence-receipt.json'}, 'Unreceipted proof files'
for row in v['Files']:
    raw=(o/row['Path']).read_bytes(); assert len(raw)==row['Bytes'] and sha(raw)==row['Sha256'],row['Path']
source_paths=[x['Path'] for x in f['Files']]
assert set(source_paths)==set(read(a/'root-part291-source-paths.json'))
for row in f['Files']:
    raw=(r/row['Path']).read_bytes();assert len(raw)==row['Bytes'] and sha(raw)==row['Sha256'],row['Path']
metadata=['.gitattributes','AGENTS.md','CHANGELOG.md','docs/COMPATIBILITY.md','docs/DECISIONS.md','docs/ERLANG_COMPATIBILITY.md','docs/NEXT_STEPS.md','docs/PROGRESS.md','docs/changelog/0044.md','docs/dependency-decisions.md','docs/OTP-PORTS.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md']
changed=set(git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''}
metadata_changed=[x for x in metadata if x in changed]
allowed=set(source_paths)|set(metadata)|set(proof_paths)
assert changed<=allowed, ('Unexpected tracked changes',sorted(changed-allowed))
expected=source_paths+metadata_changed+proof_paths
assert len(expected)==len(set(expected)) and not any('LICENSE.local' in x or '__pycache__' in x for x in expected)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,44))
for n,s in [('0043.md','1246C10CA856935B004FA7ACB7E177E5E6A9B6F1250E32EBB893BE9739B6F0E1'),('0042.md','9E66900F77238648D19EFAE7ECC7D4FA50DB8CAA4EC60CAF63BD49DB4CAC1194'),('0041.md','9B52E6BEEF3F611C029838B2F5F2CAC205F740CEA36C57F5EF1B431FDFCA06C7')]: assert sha((r/'docs/changelog'/n).read_bytes())==s
registry=read(r/'docs/supported-mfas.json');assert len(registry)==502 and registry==json.loads(git('show','HEAD:docs/supported-mfas.json'))
scope=read(r/'docs/module-readiness-scope.json');assert scope['ComparisonTestReport']=='validation/part-291/tests.json'
ready=read(r/'docs/module-readiness.json');assert ready['LocalPassedTests']==6599 and ready['RegisteredMfas']==502 and ready['PreviousRegisteredMfas']==502 and ready['RegisteredMfaDelta']==0
pathspec=a/'root-part291-stage-pathspec';pathspec.write_bytes(b'\0'.join(x.encode() for x in expected)+b'\0')
subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=r,check=True)
actual=set(git('diff','--cached','--name-only','-z').decode().split('\0'))-{''}
assert actual==set(expected),(sorted(actual-set(expected)),sorted(set(expected)-actual))
for path in proof_paths: assert git('show',':'+path)==(r/path).read_bytes(),path
for path in source_paths+metadata_changed:
    staged=git('show',':'+path);working=(r/path).read_bytes()
    assert staged.replace(b'\r\n',b'\n')==working.replace(b'\r\n',b'\n'),path
subprocess.run(['git','diff','--cached','--check'],cwd=r,check=True)
print(json.dumps({'StagedPaths':len(expected),'SourcePaths':21,'MetadataPaths':len(metadata_changed),'RawProofFiles':len(proof_paths),'Passed':6599,'Registry':502,'ClosedVolumesUnstaged':True,'LicenseAndCacheExcluded':True}))
