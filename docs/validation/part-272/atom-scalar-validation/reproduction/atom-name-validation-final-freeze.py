from pathlib import Path
import json,hashlib,shutil,subprocess
r=Path.cwd();p=r/'artifacts/atom-name-validation-prototype';e=r/'artifacts/atom-name-validation-evidence';rebase=json.loads((r/'artifacts/atom-name-validation-source-freeze-rebased271.json').read_text());own={x['Path'] for x in rebase['Files']};unowned=[]
for row in rebase['Seed']:
 if row['Path'] in own:continue
 assert hashlib.sha256((p/row['Path']).read_bytes()).hexdigest().upper()==row['Sha256'],row['Path'];unowned.append(row['Path'])
for row in rebase['Files']:
 assert (p/row['Path']).read_bytes()==(e/'owned-style-root'/row['Path']).read_bytes(),row['Path']
focus=json.loads((e/'revision3-focus.json').read_text(encoding='utf-8-sig'));assert focus['HostGroupPassed'] and focus['Failed']==0 and len(focus['Generated'])==9 and all(x['Pass'] for x in focus['Generated']);initial=json.loads((e/'initial-unsupported-atom-count-focus.json').read_text(encoding='utf-8-sig'));assert initial['HostGroupPassed'] and initial['Failed']==2 and len(initial['Generated'])==8
source=next(x['SourceContent'] for x in rebase['Files'] if x['Path'].endswith('AtomScalarValidationCases.cs'));initialsource=next(x['SourceContent'] for x in json.loads((r/'artifacts/atom-name-validation-source-freeze-initial.json').read_text())['Files'] if x['Path'].endswith('AtomScalarValidationCases.cs'))
import re
pattern=r'"(-module\(atom_scalar_validation_contract\).*?)",'
oldcases=re.findall(pattern,initialsource);newcases=re.findall(pattern,source);assert len(oldcases)==8 and len(newcases)==9 and oldcases==newcases[:8]
for name in ['Atom.cs','AtomCache.cs','AtomTableLimits.cs','CommonAtomNames.cs','ErlangPrimordialAtomNames.cs']:
 assert (p/'src/Erlang.Terms'/name).read_bytes()==(r/'src/Erlang.Terms'/name).read_bytes(),name
originalcounter=(e/'pinned-original/erl_bif_info.c').read_text();assert 'BIF_RET(make_small(atom_table_size()));' in originalcounter
(e/'prebuild-command-path-failure.json').write_text(json.dumps({'Status':'PRESERVED_EXECUTION_PATH_FAILURE','Scope':'A first command usedMaincwd withprototype-relative ../output paths; PowerShell redirection failed before build/focus execution. Correctedworkdir andStop preference used for actual execution.','SourceOrExpectationChanged':False,'ActualBuildReportedFromFailure':False},indent=2))
receipt={'Status':'PASS_SCOPED_PROMOTION','PublishedBase':rebase['PublishedBase'],'OldDllMvid':'8f6ee1fd-41ee-4818-90e8-9400117d720a','OldDllActualMapKeyDefectReproduced':True,'OldDllPdbSourceDocumentsExact':4,'HostGroupPassed':True,'GeneratedPassed':9,'GeneratedFailed':0,'InitialUnsupportedCounterFailurePreserved':'6/8 with two unsupported atom_count results; no SourceExpected changes','InitialEightCaseSourcesAndOrderExact':True,'RealAtomCounter':'Actual process-wide AtomNameRegistry.Count; newinsertiondelta1/existingreuse0 and rejectedconversiondelta0, no absolute crossVM expected count','OtherPublishedSeedFilesExact':len(unowned),'OwnedSource11ByteExactToActualFocusStyleInputsAfterRebase':True,'CachedTenAndPrimordialSourceIndicesPreserved':True,'NewAtomValidation':'Existing-namefastlookup unchanged; validate first new name strictUTF16/scalar255 before pool mutation; invalid<=255badarg/over255system_limit','RegistryDefaultExportsUnchanged':True,'NativeExecuted':False,'MainFullExpected':6507,'MainFullExecuted':False,'BuildErrors':0,'BuildWarnings':0,'OwnedStyleChanges':0,'NewAssemblyMvid':focus['AssemblyMvid'],'CoreRegistrationsAdded':0,'SourceNamesOldAppendPreserved':True}
final={'Stage':'FinalScopedPromotionSourceExactPublished271Rebase','Files':rebase['Files'],'Receipt':receipt,'RebasedPrebuildFreezeSha256':hashlib.sha256((r/'artifacts/atom-name-validation-source-freeze-rebased271.json').read_bytes()).hexdigest().upper(),'NativeExecuted':False};f=r/'artifacts/atom-name-validation-source-freeze-final.json';f.write_text(json.dumps(final,indent=2));(e/'receipt.json').write_text(json.dumps(receipt,indent=2));print('FinalfreezeSHA',hashlib.sha256(f.read_bytes()).hexdigest().upper());print(json.dumps(receipt,indent=2))
closure=e/'actual-final-dll-closure';closure.mkdir(exist_ok=True)
for file in (p/'tools/AtomNameValidationFocus/bin/Release/net10.0').glob('*'):
 if file.is_file():shutil.copyfile(file,closure/file.name)
