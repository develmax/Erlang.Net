"""Root only. --apply explicitly stages; default verifies and writes pathspec only."""
import importlib.util,sys,json,subprocess
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common',Path(__file__).with_name('common312.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c=m.read(sys.argv[1]);assert c.get('RootExecutionApproved') is True and c.get('BoundToActualComposite') is True; f=m.read(m.pinned(c['SourceFreeze'])); v=m.read(m.OUT/'evidence-receipt.json')
assert m.git('rev-parse','HEAD').decode().strip()==c['ParentHead'] and v['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
assert v['FinalActualConfigSha256']==m.sha(Path(sys.argv[1]).read_bytes()) and v['CompositeAuthoritySha256']==c['CompositeAuthority']['Sha256']
authority=m.read(m.pinned(c['CompositeAuthority']));assert authority['Status']=='ROOT_REVIEWED_ACTUAL_FINAL312_AUTHORITY' and authority['RootExecutionApproved'] is True
assert (v['Passed'],v['Failed'],v['Registry'],v['NativeInputs'])==(c['ExpectedPassed'],0,c['ExpectedRegistry'],c['ExpectedNativeInputs'])
assert v['FreshMainFullValidationExecuted'] and not v['ReusedFullValidation'] and v['IntegrationAndPackagesPassed'] and v['NativeTrustedWindowsProfilePassed'] and v['ActualGeneratedCSharpRoutePassed']
assert m.pinned(c['DerivedRegistry']).read_bytes()==(m.OUT/'supported-mfas.json').read_bytes()
assert m.pinned(c['ReadinessResult']).read_bytes()==(m.OUT/'module-readiness.json').read_bytes()
for row in f['Files']:m.pinned(row)
proofs={p.relative_to(m.ROOT).as_posix() for p in m.OUT.rglob('*') if p.is_file()}
assert proofs=={f'docs/validation/part-312/{x["Path"]}' for x in v['Files']}|{'docs/validation/part-312/evidence-receipt.json'}
for row in v['Files']:
    data=(m.OUT/row['Path']).read_bytes(); assert len(data)==row['Bytes'] and m.sha(data)==row['Sha256']
metadata=set(c['StageMetadataPaths']); sources={x['Path'] for x in f['Files']}
changed=set(m.git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''}
assert changed<=sources|metadata|proofs, sorted(changed-sources-metadata-proofs)
assert c['RotationTarget']=='docs/changelog/0047.md' and m.safe(c['RotationTarget']).exists() and c['RotationTarget'] in metadata
expected=sorted(sources|(metadata&changed)|proofs|{c['RotationTarget']})
for name in expected:m.safe(name)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,47))
assert m.sha((m.ROOT/'docs/changelog/0045.md').read_bytes())==m.CLOSED45
assert m.sha((m.ROOT/'docs/changelog/0046.md').read_bytes())==m.CLOSED46
active=m.safe(c['RotationTarget']).read_bytes();assert len(active)<=32768 and len(active.splitlines())<=400
pathspec=m.ROOT/'artifacts/root-part312-stage-pathspec';pathspec.write_bytes(b'\0'.join(p.encode() for p in expected)+b'\0')
if '--apply' in sys.argv:
    subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=m.ROOT,check=True)
    actual=set(m.git('diff','--cached','--name-only','-z').decode().split('\0'))-{''};assert actual==set(expected),(actual-set(expected),set(expected)-actual)
    bulk_spec=importlib.util.spec_from_file_location('bulk_git',Path(__file__).with_name('bulk-git.py'));bulk=importlib.util.module_from_spec(bulk_spec);bulk_spec.loader.exec_module(bulk)
    staged_blobs,staged_attrs=bulk.capture(expected,m.ROOT)
    records=[]
    for name in expected:
        data=m.safe(name).read_bytes();staged=staged_blobs[name];attrs=staged_attrs[name]
        exact=name in proofs or name.endswith(('.cs','.beam','.etf')) or attrs['text']=='unset' or attrs['binary']=='set'
        if name in sources and not exact:assert name in c['AllowedTextLfSources'],name
        mode='BinaryExact' if exact else 'TextLF'
        assert (staged==data if exact else staged.replace(b'\r\n',b'\n')==data.replace(b'\r\n',b'\n')),name
        records.append({'Path':name,'GitAttributes':attrs,'ComparisonMode':mode,'WorkingBytes':len(data),'WorkingSha256':m.sha(data),'StagedBytes':len(staged),'StagedSha256':m.sha(staged),'EqualAfterDeclaredMode':True})
    assert all(x['ComparisonMode']=='BinaryExact' for x in records if x['Path'] in proofs or x['Path'].endswith(('.cs','.beam','.etf')))
    source_records=[x for x in records if x['Path'] in sources]
    assert len(source_records)==c['SourcePaths'] and sum(x['Path'].endswith('.cs') for x in source_records)==c['CSharpPaths']
    subprocess.run(['git','diff','--cached','--check'],cwd=m.ROOT,check=True)
    guard={'Status':'PASS_EXACT_STAGE_PATHS_EXPLICIT_GIT_ATTRIBUTE_HASH_MODES','SourceFreezeSha256':c['SourceFreeze']['Sha256'],'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'ProofsBinaryExact':True,'WorkingSourceFreezePreserved':True,'Closed0045Sha256':m.CLOSED45,'Closed0046RawSha256':m.CLOSED46,'Rotated0047':True,'DefaultGitWhitespaceCheckPassed':True,'BlankAtEofExceptionUsed':False,'BulkGitByteAndAttributeSubprocesses':2,'Records':records}
    (m.ROOT/'artifacts/root-part312-stage-index-guard.json').write_text(json.dumps(guard,indent=2)+'\n')
print(json.dumps({'Passed':c['ExpectedPassed'],'SourcePaths':len(sources),'ProofFiles':len(proofs),'ExpectedStagePaths':len(expected),'StageApplied':'--apply' in sys.argv}))
