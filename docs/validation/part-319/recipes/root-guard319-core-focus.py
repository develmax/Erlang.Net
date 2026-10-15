from pathlib import Path
import hashlib,json
r=Path.cwd();g=r/'artifacts/root-part319-composed2';red=r/'artifacts/root-part319-historical-red1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
gf=read(g/'focus-source-freeze.json');rf=read(red/'source-freeze.json');assert (gf['SourcePaths'],gf['SourceClosurePaths'],rf['SourceClosurePaths'])==(17,2282,2274)
for base,freeze,name in [(g,gf,'focus-source-resource-closure.json'),(red,rf,'source-resource-closure.json')]:
 cl=base/name;assert h(cl.read_bytes())==freeze['SourceClosureSha256']
 for e in read(cl)['Files']:
  raw=(base/'candidate'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
green=read(g/'actual-green-focus.json');assert green['Status']=='PASS' and len(green['Cases'])==9 and all(e['Status']=='PASS' for e in green['Cases'])
failed=read(red/'actual-red-focus.json');assert failed['Status']=='FAIL' and len(failed['Cases'])==1 and read(red/'actual-red-exit.json')['ExitCode']==1
case=failed['Cases'][0];error=case.get('Error',case.get('Exception',''));assert case['Status']=='FAIL' and 'Connected guest PID send assertion failed' in error and 'g__Take' in error and 'TypeLoadException' not in error
for e in rf['SameGreenHostFiles']:assert (red/'candidate'/e['Path']).read_bytes()==(g/'candidate'/e['Path']).read_bytes()
for report in [green,failed]:
 for e in report['Identities']:assert h(Path(e['Path']).read_bytes())==e['Sha256']
parent=read(r/'artifacts/root-part318-main1/source-resource-closure.json')['Files']
for e in parent:assert h((red/'candidate'/e['Path']).read_bytes())==e['Sha256'],e['Path']
guard={'Status':'PASS_ACTUAL319_NINE_GREEN_HOST_GROUPS_AND_PUBLIC_GUEST_SEND_HISTORICAL_RED','GreenSourceFreezeSha256':h((g/'focus-source-freeze.json').read_bytes()),'GreenSourceClosureSha256':gf['SourceClosureSha256'],'GreenSourcePaths':17,'GreenSourceClosurePaths':2282,'HistoricalSourceFreezeSha256':h((red/'source-freeze.json').read_bytes()),'HistoricalSourceClosurePaths':2274,'HistoricalProductionExactPublished318':True,'ExactSameTwoHostSourceFilesAcrossRedGreen':True,'HistoricalFailureAtRealEmptyOutputAfterPublicReturns':True,'GreenHostGroups':9,'NewHostContainsEightSubgroups':True,'ActualOriginalVmExecuted':False,'NativeOrSdkLivePeerComparisonClaimed':False,'AssembliesAndBothSourceClosuresRehashed':True,'InitialWrongTypeSelectionRetainedSeparately':True,'GreenReportSha256':h((g/'actual-green-focus.json').read_bytes()),'HistoricalReportSha256':h((red/'actual-red-focus.json').read_bytes()),'GuardSourceSha256':h(Path(__file__).read_bytes())}
out=g/'actual-core-focus-guard.json';assert not out.exists();out.write_bytes((json.dumps(guard,indent=2)+'\n').encode());print(h(out.read_bytes()))
