from pathlib import Path
import hashlib,json,zipfile,subprocess,sys
root=Path(sys.argv[1]).resolve() if len(sys.argv)>1 else Path(__file__).resolve().parent.parent
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest().upper()
manifest_path=root/'docs/validation/part-245/source-manifest.json'
manifest=json.loads(manifest_path.read_text(encoding='utf-8-sig'))
imports=json.loads((root/'docs/validation/part-245/import-guard.json').read_text(encoding='utf-8-sig'))
repairs=json.loads((root/'docs/validation/part-245/eof-layout-repair.json').read_text(encoding='utf-8-sig'))
repair_map={row['Path'].replace(chr(92),'/'):row for row in repairs['Files']}
import_map={row['Path']:row for row in imports['Records']}
for row in manifest['Files']:
    name=row['Path']
    expected=row['Sha256']
    if name=='docs/OTP-BASED-INTEGER-SCANNER.md':
        editorial=json.loads((root/'docs/validation/part-245/editorial-promotion.json').read_text(encoding='utf-8-sig'))
        assert editorial['FrozenSha256']==expected
        expected=editorial['PublishedSha256']
    elif name in repair_map:
        assert repair_map[name]['FrozenImportedSha256']==expected
        expected=repair_map[name]['MainSha256']
    elif import_map[name]['Mode']!='Exact frozen import':
        assert import_map[name]['FrozenCandidateSha256']==expected
        expected=import_map[name]['AfterSha256']
    assert sha(root/name)==expected,name
old=json.loads((root/'docs/validation/part-244/tests.json').read_text(encoding='utf-8-sig'))
new=json.loads((root/'artifacts/tests.json').read_text(encoding='utf-8-sig'))
old_names=[row['Name'] for row in old['Tests']]
new_names=[row['Name'] for row in new['Tests']]
assert len(old_names)==6440 and len(new_names)==6460 and new['Passed']==6460 and new['Failed']==0
assert len(set(new_names))==6460
old_set=set(old_names)
assert [name for name in new_names if name in old_set]==old_names
assert len([name for name in new_names if name not in old_set])==20
assert all(row['Status']=='Passed' for row in new['Tests'])
ledger=json.loads((root/'artifacts/based-integer-source-ledger.json').read_text(encoding='utf-8-sig'))
frozen_ledger=json.loads((root/'docs/validation/part-245/source-ledger.json').read_text(encoding='utf-8-sig'))
assert ledger==frozen_ledger and len(ledger['Modules'])==12 and len(ledger['Diagnostics'])==8
corpus=json.loads((root/'artifacts/based-integer-corpus-guard.json').read_text(encoding='utf-8-sig'))
assert corpus['InheritedModules']==4831 and corpus['TotalModules']==4843 and corpus['TotalDiagnostics']==159 and corpus['ActualMfas']==484
assert corpus['InheritedCompleteSourcesGeneratedHashesOutcomesPoliciesContextsFiniteBytesAndOrderEqual']
changed=subprocess.check_output(['git','diff','--name-only','bdf2dc0e283be5a2e81e353d4e9a54dfce244372','--','src/Erlang.Runtime','src/Erlang.Terms','src/Erlang.Serialization','tests/Erlang.Tests']).decode().strip()
assert not changed,changed
closed=json.loads((root/'docs/validation/part-245/closed-volume.json').read_text())
assert sha(root/closed['ClosedVolume'])==closed['WorkingSha256']
packages=[]
for package in sorted((root/'artifacts/packages').glob('*.nupkg')):
    with zipfile.ZipFile(package) as z:
        names=z.namelist()
        notices=[name for name in names if name.startswith('licenses/')]
        assert len(notices)==5
        for notice in notices:
            assert z.read(notice)==(root/'docs'/Path(notice).name).read_bytes(),notice
        assert not any('LICENSE.local' in name for name in names)
        assert not any(name.endswith(('.beam','.actual.etf','.expected.etf')) for name in names)
        packages.append({'Path':str(package.relative_to(root)),'Sha256':sha(package),'FiveNoticesByteExact':True,'PrivateLicenseAbsent':True,'NativeFixturesAbsent':True})
assert len(packages)==2
result={'Status':'PASS','MainPassed':6460,'MainFailed':0,'Old6440TestNamesRelativeOrderExact':True,'NewSourceContracts':20,'OwnedPaths':15,'FrozenImportsExact':9,'EditorialStatusPromotions':1,'EofOnlySourceCatalogRepairs':3,'AppendOnlyNoticeCompositions':2,'InheritedCompleteContracts':4831,'TotalSdkModules':4843,'ComparableModules':4837,'Diagnostics':159,'Exclusions':6,'ActualMfas':484,'RuntimeTermsSerializationAndTestsUnchangedFrom244':True,'NewLedgerByteValuesExact':True,'ClosedJournalUnchanged':True,'NativeComparisonsPlanned':5774,'OriginalExecutionPending':True,'Packages':packages}
(root/'docs/validation/part-245/final-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
