from pathlib import Path
import hashlib,json
r=Path.cwd();g=r/'artifacts/root-part320-composed2';red=r/'artifacts/root-part320-controlled-red1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
gf=read(g/'focus-source-freeze.json');rf=read(red/'source-freeze.json');assert gf['SourcePaths']==19 and gf['SourceClosurePaths']==rf['SourceClosurePaths']==2302
closures=[]
for base,freeze,name in [(g,gf,'focus-source-resource-closure.json'),(red,rf,'source-resource-closure.json')]:
 cl=base/name;assert h(cl.read_bytes())==freeze['SourceClosureSha256'];rows=read(cl)['Files'];assert len(rows)==2302
 for e in rows:
  raw=(base/'candidate'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path']
 closures.append({e['Path']:e for e in rows})
changed={name for name in closures[0] if closures[0][name]['Sha256']!=closures[1][name]['Sha256']};assert changed=={'src/Erlang.Runtime/RuntimeDistributionPending.GuestSend.cs'}
green=read(g/'actual-green-focus.json');assert green['Status']=='PASS' and len(green['Cases'])==8 and all(e['Status']=='PASS' for e in green['Cases'])
failed=read(red/'actual-red-focus.json');assert failed['Status']=='FAIL' and len(failed['Cases'])==2 and failed['Cases'][0]['Status']=='PASS' and read(red/'actual-red-exit.json')['ExitCode']==1
case=failed['Cases'][1];error=case.get('Error',case.get('Exception',''));assert case['Status']=='FAIL' and 'TimeoutException' in error and 'AcceptedOrderSurvivesProducerDeath' in error and all(s not in error for s in ['TypeLoadException','MissingMethodException','BadArgument'])
for e in rf['SameFourGreenHostSources']:assert (red/'candidate'/e['Path']).read_bytes()==(g/'candidate'/e['Path']).read_bytes()
for report in [green,failed]:
 for e in report['Identities']:assert h(Path(e['Path']).read_bytes())==e['Sha256']
managed=read(g/'managed-actual-results.json');assert managed['Passed'] is True and managed['Executed']==managed['ExpectedCases']==5 and managed['Unexecuted']==0 and all(e['Passed'] for e in managed['Cases'])
for e in managed['ActualLoadedAssemblies']:assert h(Path(e['Path']).read_bytes())==e['Sha256']
for name in ['actual-green-focus.json','actual-remaining-subgroups-focus.json']:
 assert read(r/'artifacts/root-part320-composed1'/name)['Status']=='FAIL'
guard={'Status':'PASS_ACTUAL320_EIGHT_GREEN_HOST_GROUPS_FIVE_GENERATED_CASES_AND_CONTROLLED_OLD_ROUTE_RED','GreenSourceFreezeSha256':h((g/'focus-source-freeze.json').read_bytes()),'GreenSourceClosureSha256':gf['SourceClosureSha256'],'GreenSourcePaths':19,'GreenSourceClosurePaths':2302,'ControlledRedSourceFreezeSha256':h((red/'source-freeze.json').read_bytes()),'ControlledRedClosureSha256':rf['SourceClosureSha256'],'OnlyOld319GuestRouteReplaced':True,'Other2301RowsRawExactGreen':True,'SameFourHostSources':True,'NewFlagContractPassedInRed':True,'ActualRedReturnBeforeDrainTimeout':True,'HistoricalWhole319ProductionClaimed':False,'GreenHostGroups':8,'NewHostSubgroups':5,'ActualGeneratedCases':5,'InitialTwoHostOnlyFailuresRetained':True,'AssembliesAndBothSourceClosuresRehashed':True,'OriginalOrNativeExecutionClaimed':False,'GreenReportSha256':h((g/'actual-green-focus.json').read_bytes()),'ManagedReportSha256':h((g/'managed-actual-results.json').read_bytes()),'RedReportSha256':h((red/'actual-red-focus.json').read_bytes()),'GuardSourceSha256':h(Path(__file__).read_bytes())}
out=g/'actual-core-focus-guard.json';assert not out.exists();out.write_bytes((json.dumps(guard,indent=2)+'\n').encode());print(h(out.read_bytes()))
