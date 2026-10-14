from pathlib import Path
import hashlib,json,zipfile,subprocess
root=Path(__file__).resolve().parent.parent
sha=lambda path:hashlib.sha256(path.read_bytes()).hexdigest().upper()
plan_path=root/'artifacts/beam-closure-publication-prototype/artifacts/closure-publication-import-plan.json'
plan=json.loads(plan_path.read_text(encoding='utf-8-sig'))
assert len(plan['Files'])==29
for row in plan['Files']:
    assert sha(root/row['Path'])==row['Sha256'],row['Path']
old=json.loads((root/'docs/validation/part-245/tests.json').read_text(encoding='utf-8-sig'))
new=json.loads((root/'artifacts/tests.json').read_text(encoding='utf-8-sig'))
old_names=[row['Name'] for row in old['Tests']];new_names=[row['Name'] for row in new['Tests']]
assert len(old_names)==6460 and len(new_names)==6461 and new['Passed']==6461 and new['Failed']==0
assert new_names[:-1]==old_names and len(set(new_names))==6461
assert all(row['Status']=='Passed' for row in new['Tests'])
corpus=json.loads((root/'artifacts/based-integer-corpus-guard.json').read_text(encoding='utf-8-sig'))
ledger=json.loads((root/'artifacts/based-integer-source-ledger.json').read_text(encoding='utf-8-sig'))
assert ledger==json.loads((root/'docs/validation/part-245/source-ledger.json').read_text(encoding='utf-8-sig'))
assert corpus['TotalModules']==4843 and corpus['TotalDiagnostics']==159 and corpus['ActualMfas']==484
assert corpus['InheritedCompleteSourcesGeneratedHashesOutcomesPoliciesContextsFiniteBytesAndOrderEqual']
resources=root/'tests/Erlang.Tests/Resources/Beam'
proof=json.loads((resources/'source-proof.json').read_text(encoding='utf-8-sig'))
historical=json.loads((root/'artifacts/part246-historical-five-proof.json').read_text(encoding='utf-8-sig'))
assert proof['HistoricalOriginalFiveProof']==historical
for before,after in zip(historical['Fixtures'],proof['Fixtures'][:5]):
    assert all(after[key]==value for key,value in before.items())
    assert set(after)-set(before)=={'OriginalNativeRun','ProvenanceGuard'}
checks=[]
for row in proof['Fixtures']:
    for filekey,hashkey in [('BeamFile','BeamSha256'),('SourceFile','SourceSha256'),('NativeActualEtfFile','NativeActualEtfSha256'),('NativeExpectedEtfFile','NativeExpectedEtfSha256')]:
        name=row[filekey]
        if name is None:continue
        assert sha(resources/name)==row[hashkey],name
        checks.append(name)
assert len(proof['Fixtures'])==6 and len(checks)==22 and not proof['OnLoadExecuted']
attrs=(root/'.gitattributes').read_text()
assert 'tests/Erlang.Tests/Resources/Beam/*.erl text eol=lf' in attrs
assert '*.beam binary' in attrs and '*.etf binary' in attrs
packages=[]
for package in sorted((root/'artifacts/packages').glob('*.nupkg')):
    with zipfile.ZipFile(package) as z:
        names=z.namelist();notices=[name for name in names if name.startswith('licenses/')]
        assert len(notices)==5
        for notice in notices:
            assert z.read(notice)==(root/'docs'/Path(notice).name).read_bytes(),notice
        assert not any('LICENSE.local' in name for name in names)
        assert not any(name.endswith(('.beam','.actual.etf','.expected.etf')) for name in names)
        packages.append({'Path':str(package.relative_to(root)),'Sha256':sha(package),'FiveNoticesByteExact':True,'PrivateLicenseAbsent':True,'NativeFixturesAbsent':True})
assert len(packages)==2
changed=subprocess.check_output(['git','diff','HEAD','--name-only','--','src/Erlang.Compiler','tools/Erlang.Differential']).decode().strip()
assert not changed,changed
result={'Status':'PASS','Baseline':plan['Seed'],'OwnedFrozenFilesExact':29,'MainPassed':6461,'MainFailed':0,'Old6460NamesOrderExact':True,'NewHostFamilies':1,'SdkModules':4843,'Comparable':4837,'Diagnostics':159,'Exclusions':6,'Mfas':484,'Old4831CompletePrefixAndNew12LedgerExact':True,'CompilerAndDifferentialSourceUnchanged':True,'HistoricalFiveProofEntireJsonExact':True,'OldFiveRowValuesExact':True,'OnlyTwoAddedRowProvenanceFields':True,'MixedCompiledFixtures':6,'MixedNativeExecutedFixtures':5,'All22ResourceBytesNativeHashExact':True,'SourceLfCheckoutRule':True,'OnLoadExecuted':False,'NativeNewProofClaimed':False,'RuntimePublicationImplemented':False,'GenericFunCallsSupported':False,'Packages':packages}
(root/'artifacts/part246-main-postguard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,indent=2))
