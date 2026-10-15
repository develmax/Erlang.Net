from pathlib import Path
import hashlib,json
r=Path.cwd();g=r/'artifacts/root-part318-composed1';red=r/'artifacts/root-part318-historical-red1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
gf=read(g/'focus-source-freeze.json');rf=read(red/'source-freeze.json');assert gf['SourcePaths']==6 and gf['SourceClosurePaths']==2264 and rf['SourceClosurePaths']==2262
for base,freeze,clname in [(g,gf,'focus-source-resource-closure.json'),(red,rf,'source-resource-closure.json')]:
 cl=base/clname;assert h(cl.read_bytes())==freeze['SourceClosureSha256']
 for e in read(cl)['Files']:
  raw=(base/'candidate'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
green_reports=[read(g/'actual-green-focus.json'),read(g/'actual-distribution-regression-focus.json')];assert all(e['Status']=='PASS' and all(c['Status']=='PASS' for c in e['Cases']) for e in green_reports)
assert sum(len(e['Cases']) for e in green_reports)==7
failed=read(red/'actual-red-focus.json');assert failed['Status']=='FAIL' and len(failed['Cases'])==1 and read(red/'actual-red-exit.json')['ExitCode']==1
case=failed['Cases'][0];error=case.get('Error',case.get('Exception',''));assert case['Status']=='FAIL' and 'CheckControl' in error and 'RegisteredPublicationAndOrder' in error and 'Connected group leader output assertion failed' in error
for e in rf['TestFiles']:assert (red/'candidate'/e['Path']).read_bytes()==(g/'candidate'/e['Path']).read_bytes()
for report in green_reports+[failed]:
 for e in report['Identities']:assert h(Path(e['Path']).read_bytes())==e['Sha256']
parent=read(r/'artifacts/root-part317-composed2/final-source-resource-closure.json')['Files']
for e in parent:
 if e['Path'].startswith('src/'):assert h((red/'candidate'/e['Path']).read_bytes())==e['Sha256']
guard={'Status':'PASS_ACTUAL318_CORE_SEVEN_GREEN_HOST_GROUPS_AND_CONTROLLED_HISTORICAL_REGISTERED_ROUTE_RED','GreenSourceFreezeSha256':h((g/'focus-source-freeze.json').read_bytes()),'GreenSourceClosureSha256':gf['SourceClosureSha256'],'GreenSourcePaths':6,'GreenSourceClosurePaths':2264,'HistoricalSourceFreezeSha256':h((red/'source-freeze.json').read_bytes()),'HistoricalSourceClosurePaths':2262,'HistoricalProductionExactPublished317':True,'ExactSameThreeHostSourceFilesAcrossRedGreen':True,'HistoricalFailureAtRealDecodedControlAssertion':True,'GreenHostGroups':7,'NewHostContainsSixSubgroups':True,'ActualOriginalVmExecuted':False,'NativeOrSdkLivePeerComparisonClaimed':False,'RuntimeAssembliesRehashed':True,'BothSourceClosuresVerifiedAfterExecution':True,'GreenReports':[{'Path':p.relative_to(r).as_posix(),'Sha256':h(p.read_bytes())} for p in [g/'actual-green-focus.json',g/'actual-distribution-regression-focus.json']],'HistoricalReportSha256':h((red/'actual-red-focus.json').read_bytes()),'GuardSourceSha256':h(Path(__file__).read_bytes())}
out=g/'actual-core-focus-guard.json';assert not out.exists();out.write_bytes((json.dumps(guard,indent=2)+'\n').encode());print(json.dumps({'Status':guard['Status'],'Sha256':h(out.read_bytes())}))
