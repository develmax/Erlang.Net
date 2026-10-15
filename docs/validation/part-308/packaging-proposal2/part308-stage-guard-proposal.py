"""Root only. --apply explicitly stages; default verifies and writes pathspec only."""
import importlib.util,sys,json,subprocess
sys.dont_write_bytecode=True
from pathlib import Path
spec=importlib.util.spec_from_file_location('common',Path(__file__).with_name('part308-common.py'));m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
c=m.read(sys.argv[1]); f=m.read(m.pinned(c['SourceFreeze'])); v=m.read(m.OUT/'evidence-receipt.json')
assert m.git('rev-parse','HEAD').decode().strip()==c['ParentHead'] and v['SourceFreezeSha256']==c['SourceFreeze']['Sha256']
assert (v['Passed'],v['Failed'],v['Registry'],v['NativeInputs'])==(c['ExpectedPassed'],0,c['ExpectedRegistry'],c['ExpectedNativeInputs'])
for row in f['Files']:m.pinned(row)
proofs={p.relative_to(m.ROOT).as_posix() for p in m.OUT.rglob('*') if p.is_file()}
assert proofs=={f'docs/validation/part-308/{x["Path"]}' for x in v['Files']}|{'docs/validation/part-308/evidence-receipt.json'}
for row in v['Files']:
    data=(m.OUT/row['Path']).read_bytes(); assert len(data)==row['Bytes'] and m.sha(data)==row['Sha256']
metadata=set(c['StageMetadataPaths']); sources={x['Path'] for x in f['Files']}
changed=set(m.git('diff','HEAD','--name-only','-z').decode().split('\0'))-{''}
assert changed<=sources|metadata|proofs, sorted(changed-sources-metadata-proofs)
expected=sorted(sources|(metadata&changed)|proofs)
for name in expected:m.safe(name)
assert all('docs/changelog/'+str(i).zfill(4)+'.md' not in expected for i in range(1,46))
assert m.sha((m.ROOT/'docs/changelog/0045.md').read_bytes())==m.CLOSED45
active=(m.ROOT/'docs/changelog/0046.md').read_bytes();assert len(active)<=32768 and len(active.splitlines())<=400
pathspec=m.ROOT/'artifacts/root-part308-stage-pathspec';pathspec.write_bytes(b'\0'.join(p.encode() for p in expected)+b'\0')
if '--apply' in sys.argv:
    subprocess.run(['git','add','--force','--pathspec-from-file='+str(pathspec),'--pathspec-file-nul'],cwd=m.ROOT,check=True)
    actual=set(m.git('diff','--cached','--name-only','-z').decode().split('\0'))-{''};assert actual==set(expected),(actual-set(expected),set(expected)-actual)
    records=[]
    for name in expected:
        data=m.safe(name).read_bytes(); staged=m.git('show',':'+name)
        rawattrs=m.git('check-attr','-z','text','binary','--',name).decode().split('\0')
        assert rawattrs[-1]=='' and len(rawattrs)==7 and rawattrs[0]==rawattrs[3]==name
        attrs={rawattrs[1]:rawattrs[2],rawattrs[4]:rawattrs[5]}
        exact=name in proofs or name.endswith('.cs') or attrs['text']=='unset' or attrs['binary']=='set'
        if name in sources and not exact:assert name in c['AllowedTextLfSources'],name
        mode='BinaryExact' if exact else 'TextLF'
        assert (staged==data if exact else staged.replace(b'\r\n',b'\n')==data.replace(b'\r\n',b'\n')),name
        records.append({'Path':name,'GitAttributes':attrs,'ComparisonMode':mode,'WorkingBytes':len(data),'WorkingSha256':m.sha(data),'StagedBytes':len(staged),'StagedSha256':m.sha(staged),'EqualAfterDeclaredMode':True})
    assert all(x['ComparisonMode']=='BinaryExact' for x in records if x['Path'] in proofs or x['Path'].endswith('.cs'))
    source_records=[x for x in records if x['Path'] in sources]
    assert len(source_records)==c['SourcePaths'] and sum(x['Path'].endswith('.cs') for x in source_records)==c['CSharpPaths']
    subprocess.run(['git','diff','--cached','--check'],cwd=m.ROOT,check=True)
    guard={'Status':'PASS_EXACT_STAGE_PATHS_EXPLICIT_GIT_ATTRIBUTE_HASH_MODES','SourceFreezeSha256':c['SourceFreeze']['Sha256'],'SourcePaths':c['SourcePaths'],'CSharpPaths':c['CSharpPaths'],'ProofsBinaryExact':True,'WorkingSourceFreezePreserved':True,'Closed0045Sha256':m.CLOSED45,'Records':records}
    (m.ROOT/'artifacts/root-part308-stage-index-guard.json').write_text(json.dumps(guard,indent=2)+'\n')
print(json.dumps({'Passed':c['ExpectedPassed'],'SourcePaths':len(sources),'ProofFiles':len(proofs),'ExpectedStagePaths':len(expected),'StageApplied':'--apply' in sys.argv}))
