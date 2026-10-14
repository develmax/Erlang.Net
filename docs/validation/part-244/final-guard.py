from pathlib import Path
import hashlib, json, zipfile
root=Path(__file__).resolve().parents[3]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

freeze_path=root/'docs/validation/part-244/source-manifest.json'
freeze=json.loads(freeze_path.read_text(encoding='utf-8-sig'))
assert len(freeze['Files']) == 18
for row in freeze['Files']:
    if row['Path']=='docs/beam-receive-host-prototype.md':
        editorial=json.loads((root/'docs/validation/part-244/editorial-promotion.json').read_text(encoding='utf-8-sig'))
        assert editorial['FrozenSha256']==row['Sha256']
        assert sha(root/row['Path'])==editorial['PublishedSha256']
    else:
        assert sha(root/row['Path'])==row['Sha256'],row['Path']
old=json.loads((root/'docs/validation/part-242/tests.json').read_text(encoding='utf-8-sig'))
new=json.loads((root/'artifacts/tests.json').read_text(encoding='utf-8-sig'))
oldnames=[row['Name'] for row in old['Tests']]
newnames=[row['Name'] for row in new['Tests']]
assert len(oldnames)==6439 and len(newnames)==6440
assert new['Passed']==6440 and new['Failed']==0
assert newnames[:-1]==oldnames and len(set(newnames))==6440
assert all(row['Status']=='Passed' for row in new['Tests'])
resources=root/'tests/Erlang.Tests/Resources/Beam'
proof=json.loads((resources/'source-proof.json').read_text(encoding='utf-8-sig'))
binaries=[]
sources=[]
for row in proof['Fixtures']:
    for filekey, hashkey in [('BeamFile','BeamSha256'),('SourceFile','SourceSha256'),('NativeActualEtfFile','NativeActualEtfSha256'),('NativeExpectedEtfFile','NativeExpectedEtfSha256')]:
        name=row[filekey]
        if name is None:
            continue
        data=(resources/name).read_bytes()
        actual=hashlib.sha256(data).hexdigest().upper()
        if filekey=='SourceFile':
            canonical=hashlib.sha256(data.replace(b'\r\n',b'\n')).hexdigest().upper()
            assert canonical==row[hashkey], name
            sources.append({'Path':name,'RawSha256':actual,'NativeLfSha256':row[hashkey],'RawByteExact':actual==row[hashkey],'LfCanonicalExact':True})
        else:
            assert actual==row[hashkey], name
            binaries.append(name)
assert len(binaries)==13 and len(sources)==5 and not proof['OnLoadExecuted']
packages=[]
for path in sorted((root/'artifacts/packages').glob('*.nupkg')):
    with zipfile.ZipFile(path) as archive:
        names=archive.namelist()
        assert not any('LICENSE.local' in name for name in names)
        assert not any(name.endswith(('.beam','.actual.etf','.expected.etf')) for name in names)
        entries=[name for name in names if name.endswith('OTP-PORTS.md')]
        assert len(entries)==1
        notice=archive.read(entries[0]).decode('utf-8-sig')
        assert 'Bounded BEAM receive adaptation2026-10-14' in notice
        assert 'Ericsson AB2015-2026' in notice
        assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in notice
        packages.append({'Path':str(path.relative_to(root)),'Sha256':sha(path),'NoticeEntry':entries[0],'PrivateLicenseAbsent':True,'NativeFixtureResourcesAbsent':True})
assert len(packages)==2
result={'Status':'PASS','SourceFreezeSha256':sha(freeze_path),'OwnedFilesUnchanged':17,'EditorialStatusPromotions':1,'All6439OldNamesOrderPreserved':True,'NewHostFamilies':1,'FullPassed':6440,'FullFailed':0,'GenuineEmbeddedFixtures':5,'NativeBinaryResourceFilesByteExact':13,'CanonicalSourceTextsVerified':5,'SourceTextComparisons':sources,'Packages':packages,'OnLoadExecuted':False,'NativeNewProofClaimed':False}
(root/'docs/validation/part-244/final-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
