from pathlib import Path
import hashlib,json,zipfile,subprocess
r=Path.cwd();a=r/'artifacts';j=lambda p:json.loads(p.read_text(encoding='utf-8-sig'));sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
report=j(a/'part-266-differential/differential.json');old=j(r/'docs/validation/part-243/differential.json');ledger=j(r/'docs/validation/part-245/source-ledger.json');m=j(a/'part-266-differential-metadata.json');p=j(a/'part-266-original-latest-provenance.json')
assert m['Run']['head_sha']==p['Run']['head_sha']=='5b3bd2e0b7f12bfcf4fe717e72579df454f8aa4d'
assert m['Run']['id']==p['Run']['id']==38564019036 and p['Job']['id']==115852013139 and p['Job']['labels']==['ubuntu-latest']
assert m['Run']['conclusion']==p['Job']['conclusion']=='success'
assert report['Complete'] and report['VersionVerified'] and report['Planned']==report['Executed']==report['Passed']==5774 and report['Failed']==0 and report['InfrastructureError'] is None
assert not report['ReferenceCoverageComplete'] and len(report['OriginalOtpDefectExclusions'])==6
assert report['OriginalOtpDefectExclusions']==old['OriginalOtpDefectExclusions']
assert report['Results']==old['Results'] and len(report['Results'])==778
modules=report['ModuleResults'];diagnostics=report['DiagnosticResults'];assert len(modules)==4837 and len(diagnostics)==159
assert diagnostics[:151]==old['DiagnosticResults']
differences=[]
for index,(actual,historical) in enumerate(zip(modules[:4825],old['ModuleResults'])):
 if actual==historical:continue
 delta={key:{'Actual':actual.get(key),'Historical':historical.get(key)} for key in actual.keys()|historical.keys() if actual.get(key)!=historical.get(key)}
 assert index==3605 and actual['Name']==historical['Name']=='erlang-term-display-map'
 assert set(delta)<= {'NativeAndSdkStandardOutputExactlyMatch','NativeStandardOutputHex'}
 assert actual['Passed'] and historical['Passed'] and actual['StandardStreamsMatch'] and historical['StandardStreamsMatch']
 assert actual['ExpectedStandardOutputHex']==historical['ExpectedStandardOutputHex']=='237B613D3E7B307D2C623D3E2241227D0D0A'
 assert actual['ExpectedStandardOutputAlternatives']==historical['ExpectedStandardOutputAlternatives']==['237B623D3E2241222C613D3E7B307D7D0D0A']
 allowed=[actual['ExpectedStandardOutputHex']]+actual['ExpectedStandardOutputAlternatives']
 assert actual['NativeStandardOutputHex'] in allowed and historical['NativeStandardOutputHex'] in allowed
 assert actual['NativeAndSdkStandardOutputExactlyMatch']==(actual['NativeStandardOutputHex']==actual['SdkStandardOutputHex'])
 assert historical['NativeAndSdkStandardOutputExactlyMatch']==(historical['NativeStandardOutputHex']==historical['SdkStandardOutputHex'])
 differences.append({'Index':index,'Name':actual['Name'],'Differences':delta,'FullCurrentRow':actual,'FullHistoricalRow':historical})
assert len(differences)<=1
(a/'part266-original-historical-row-differences.json').write_text(json.dumps(differences,indent=2)+'\n')
assert m['Artifact']['workflow_run']['id']==38564019036 and m['Artifact']['workflow_run']['head_sha']==m['Run']['head_sha'] and not m['Artifact']['expired']
policy=subprocess.check_output(['git','show','5b3bd2e0b7f12bfcf4fe717e72579df454f8aa4d:tools/Erlang.Differential/TermDisplayCases.cs'])
matcher=subprocess.check_output(['git','show','5b3bd2e0b7f12bfcf4fe717e72579df454f8aa4d:tools/Erlang.Differential/CompiledStandardOutputContract.cs'])
assert b'ExpectedStandardOutputAlternatives' in policy and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal)' in matcher
assert len(ledger['Modules'])==12 and len(ledger['Diagnostics'])==8
for expected,actual in zip(ledger['Modules'],modules[4825:]):
 assert all(actual[k]==v for k,v in expected.items()),expected['Name']
 assert actual['Passed'] and actual['Actual']==actual['Expected']==actual['FixtureExpected']
for expected,actual in zip(ledger['Diagnostics'],diagnostics[151:]):
 for k,v in expected.items():
  key={'ExpectedCode':'ImplementationCode','ExpectedMessage':'ImplementationMessage'}.get(k,k)
  assert actual[key]==v,(expected['Name'],key)
 assert actual['Passed'] and actual['Reference']==actual['Expected'] and not actual['ImplementationAccepted']
for row in modules+diagnostics:assert hashlib.sha256(row['Source'].encode()).hexdigest().upper()==row['SourceSha256'] and row['Passed']
fd=j(a/'part-266-differential/fd-differential.json');assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8
assert fd['Results']==j(r/'docs/validation/part-243/fd-differential.json')['Results']
assert sha(a/'part-266-differential.zip')==m['ZipSHA256']==m['Artifact']['digest'].removeprefix('sha256:').upper()
with zipfile.ZipFile(a/'part-266-differential.zip') as z:
 for name in z.namelist():
  if not name.endswith('/'):assert z.read(name)==(a/'part-266-differential'/name).read_bytes()
digest=j(a/'part-266-original-job.log.digest.json');assert sha(a/'part-266-original-job.log')==digest['SHA256']
log=(a/'part-266-original-job.log').read_text(encoding='utf-8-sig');assert 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'PASS module erlang-alias-owner-exit' in log
for step in p['Job']['steps']:assert step['status']=='completed' and step['conclusion']=='success',step['name']
ci=j(a/'part266-ci-final-guard.json');assert ci['Status']=='PASS' and ci['CiPassed']==6483 and not ci['RawJobLogAuditPending']
result={'Status':'PASS','Head':m['Run']['head_sha'],'Run':m['Run']['id'],'Job':p['Job']['id'],'Artifact':m['Artifact']['id'],'ZipSha256':m['ZipSHA256'],'JobLogSha256':digest['SHA256'],'OriginalPassed':5774,'OriginalFailed':0,'Complete':True,'Expressions':778,'ComparableModules':4837,'Diagnostics':159,'All4825HistoricalModuleRowsExact':not bool(differences),'All4825HistoricalSemanticContractsExact':True,'RawObservationDifferences':differences,'PriorDeclaredMapAlternativesOnly':True,'NoExpectedOrSourceChanges':True,'All151HistoricalDiagnosticRowsExact':True,'All12Plus8IndependentNewContractsExact':True,'AllSixExclusionsUnchanged':True,'ReferenceCoverageComplete':False,'FdPassed':8,'CiPassed':6483,'SeparateHaltOriginalStatus':'Historical251 PASSED26of26 plus2physical; format comparison false'}
(a/'part266-original-observation-guard.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('PASS5774 semantic/source contracts+fd8+CI6483; actual raw row equality and any predeclared map observation recorded')
