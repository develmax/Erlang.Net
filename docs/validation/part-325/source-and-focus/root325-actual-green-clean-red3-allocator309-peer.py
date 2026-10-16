from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());G=a/'connected-link-ownership325-focus3';D=a/'connected-link-ownership325-historical-red3'
GG=a/'root325-actual-green3-guard.json';RG=a/'root325-actual-clean-red3-guard.json';assert h(GG.read_bytes())=='5FC1A288DA59BED4879AEC6F9E9542452861D5FC03CFA4B2185D60CFCEAB9B32';assert h(RG.read_bytes())=='123AB3DE1EC1A5AC6B44710035FD389C9BCDCCF6690810821D40B7B96F2B0C12'
gg=read(GG);rg=read(RG);assert gg['Passed'] is True and rg['Passed'] is True
for x in gg['Bindings']:
 assert h((r/x['Path']).read_bytes())==x['Sha256']
assert gg['Bindings']==rg['Bindings']
for folder,count,oldfolder in [(G,2352,a/'connected-link-ownership325-focus2'),(D,2351,a/'connected-link-ownership325-historical-red2')]:
 f=read(folder/'source-freeze.json');cl=folder/'source-resource-closure.json';assert h(cl.read_bytes())==f['SourceClosureSha256'];rows=read(cl)['Files'];assert len(rows)==count
 for e in rows:
  b=(folder/'candidate'/e['Path']).read_bytes();assert len(b)==e['Bytes'] and h(b)==e['Sha256']
 old={x['Path']:x for x in read(oldfolder/'source-resource-closure.json')['Files']};assert {x['Path'] for x in rows if x!=old[x['Path']]}=={'tests/Erlang.Tests/DistributionRetainedLinkHostChecks.cs'}
 build=read(folder/'root-target1/process.json');assert build['ExitCode']==0 and build['TimedOut'] is False and build['OwnedRootSettled'] is True and build['SourceFreezeSha256']==h((folder/'source-freeze.json').read_bytes())
 assert not(folder/'root-target1/stderr.log').read_bytes()
 log=(folder/'root-target1/stdout.log').read_text(encoding='utf-8-sig');assert 'Предупреждений: 0' in log and 'Ошибок: 0' in log
host='tests/Erlang.Tests/DistributionRetainedLinkHostChecks.cs';assert (G/'candidate'/host).read_bytes()==(D/'candidate'/host).read_bytes();assert h((G/'candidate'/host).read_bytes())==gg['HostSourceSha256']=='EF3DB41873F572E2C738C4441D2CDA770A2613E0AEB7A05BB6AC7A87A2DF0232'
old={x['Path']:x for x in read(a/'root-part323-composed2/source-resource-closure.json')['Files']};assert len(old)==2349
assert all(x==old[x['Path']] for x in read(D/'source-resource-closure.json')['Files'] if x['Path'] in old)
reports=[(G/'root-green-focus1',0,'PASS')]+[(D/('root-red-'+s),1,'FAIL') for s in ['NormalCapacity','AsyncCapacity']]
for folder,exitcode,status in reports:
 v=read(folder/'actual.json');p=read(folder/'process.json');assert p['ExitCode']==exitcode and p['TimedOut'] is False and p['OwnedRootSettled'] is True;assert v['Status']==status
 for identity in v['Identities']:assert h(Path(identity['Path']).read_bytes())==identity['Sha256']
 if exitcode==0:
  assert len(v['Cases'])==18 and all(x['Status']=='PASS' for x in v['Cases']);previous=read(a/'root-part323-composed1/actual-final-focus.json')['Cases'];assert [x['Name'] for x in v['Cases']]==['DistributionRetainedLinkHostChecks',*[x['Name'] for x in previous]]
 else:
  assert len(v['Cases'])==1;case=v['Cases'][0];error=case['Exception'];line=82 if folder.name.endswith('NormalCapacity') else 63
  assert case['Name']=='DistributionRetainedLinkHostChecks:'+folder.name.removeprefix('root-red-');assert error.startswith('System.InvalidOperationException: Retained connected guest LINK assertion failed');assert f'Checks.cs:line {line}' in error and 'Checks.cs:line 281' in error
  assert not any(s in error for s in ['AggregateException','TimeoutException','NullReferenceException','MissingMethodException','TypeLoadException','TargetInvocationException'])
report=dict(Status='PASS_INDEPENDENT_ACTUAL325_CLEANUP3_GREEN18_AND_CLEAN_HISTORICAL_RED2_PASSIVE_REVIEW',Passed=True,GreenGuardSha256=h(GG.read_bytes()),RedGuardSha256=h(RG.read_bytes()),GreenSourceFreezeSha256=gg['GreenSourceFreezeSha256'],RedSourceFreezeSha256=rg['RedSourceFreezeSha256'],GreenRowsIndependentlyRehashed=2352,RedRowsIndependentlyRehashed=2351,SameEf3DHostRaw=True,OnlyHostCleanupChangedFromRevision2=True,All2349HistoricalProductionAndProgramRowsExact=True,TargetBuildsZeroWarningsErrors=True,ActualLoadedAssemblyHashesRehashed=True,GreenGroupsPassed=18,All17PreviousFocusOrderedNamesPreserved=True,NormalCapacityActualMissingLinkAssertionLine=82,AsyncCapacityActualMissingEarlyMarkerAssertionLine=63,BothSoleInvalidOperationException=True,NoTimeoutAggregateOrMissingMembers=True,UnchangedFinishRethrowLine=281,OwnedRootSettledExitOne=True,TwoEarlierMixedCleanupRevisionsNotRelabeled=True,Original325Executed=False,Final325NativePlansNotValidated=True,ReviewerExecutedBuildSdkVmNativeNetworkMainGitMutation=False,Contribution='Reviewer authored future sparse composer; did not author production325, HOSTcleanup3, Rootactualguards or execute target/focus.')
p=a/'root325-actual-green-clean-red3-allocator309-peer.json';assert not p.exists();b=(json.dumps(report,indent=2)+'\n').encode();p.write_bytes(b);print(h(b))
