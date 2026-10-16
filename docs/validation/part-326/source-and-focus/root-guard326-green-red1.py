from pathlib import Path
import hashlib,json
r=Path.cwd();a=r/'artifacts';h=lambda b:hashlib.sha256(b).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());w=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
g=a/'spawn-async-dist326-focus1';d=a/'spawn-async-dist326-historical-red1';gf=g/'formatted-source-freeze.json';rf=d/'source-freeze.json';gs='727EAE9A2FFEFED90A503F86B97A3417A1A0DBF8AD44790657266BD52096BCC4';rs='132C4D6A927E98BED15C9935D740EB5C66BE6E56AD8E062594F958DC50260375';assert h(gf.read_bytes())==gs and h(rf.read_bytes())==rs
bindings=[]
def bind(p):
 bindings.append({'Path':p.relative_to(r).as_posix(),'Bytes':p.stat().st_size,'Sha256':h(p.read_bytes())})
for folder,fp,cl in [(g,gf,g/'formatted-source-resource-closure.json'),(d,rf,d/'source-resource-closure.json')]:
 assert h(cl.read_bytes())==j(fp)['SourceClosureSha256'];rows=j(cl)['Files'];assert len(rows)==2370
 for row in rows:
  raw=(folder/'candidate'/row['Path']).read_bytes();assert len(raw)==row['Bytes'] and h(raw)==row['Sha256']
 bind(fp);bind(cl)
assert (g/'tokens-before.json').read_bytes()==(g/'tokens-after.json').read_bytes();bind(g/'tokens-before.json');bind(g/'tokens-after.json')
baseline={x['Path']:x for x in j(a/'spawn-async-dist326-candidate2/baseline-source-resource-closure.json')['Files']};applied=set(j(rf)['AppliedSameFormattedPaths']);assert len(baseline)==2366 and len(applied)==5
for name,x in baseline.items():
 raw=(d/'candidate'/name).read_bytes()
 if name not in applied:assert len(raw)==x['Bytes'] and h(raw)==x['Sha256']
for name in applied:assert (g/'candidate'/name).read_bytes()==(d/'candidate'/name).read_bytes()
for root in ['src/Erlang.Runtime/ProcessRuntime.cs','src/Erlang.Runtime/ProcessSpawnOptions.cs','tests/Erlang.Tests/Program.cs']:assert h((d/'candidate'/root).read_bytes())==baseline[root]['Sha256']
reports={}
for folder,phase in [(g,'target-build'),(g,'green-build'),(g,'green-host'),(g,'green-run'),(d,'red-build'),(d,'red-host')]:
 out=folder/('root-'+phase+'-execution');proc=j(out/'process.json');assert proc['TimedOut'] is False and proc['OwnedProcessSettled'] is True and proc['SourcesRawExactBeforeAndAfter'] is True and proc['SourceClosureFiles']==2370 and proc['ProcessEnvelopePassed'] is True
 assert proc['SourceFreezeSha256']==(gs if folder==g else rs) and proc['ExitCode']==(1 if phase=='red-host' else 0)
 for name in ['process.json','stdout.log','stderr.log']:bind(out/name)
 if phase.endswith('build'):
  text=(out/'stdout.log').read_text();assert 'Предупреждений: 0' in text and 'Ошибок: 0' in text
 else:
  raw=out/'actual.json';assert h(raw.read_bytes())==proc['RawReportSha256'];bind(raw);reports[phase]=j(raw)
for phase in ['green-host','red-host']:
 for x in reports[phase]['Identities']:
  p=Path(x['Path']);assert p.resolve().is_relative_to(a) and h(p.read_bytes())==x['Sha256']
host=reports['green-host'];assert host['Status']=='PASS' and [x['Name'] for x in host['Cases']]==['SpawnAsyncDistributionHostChecks:Run','SpawnRequestHostChecks:Run','NodeSpawnHostChecks:Run','ProcessInfoAsyncDistributionHostChecks:Run','AsyncDistributionHostChecks:Run','DistributionRetainedLinkHostChecks:Run'] and all(x['Status']=='PASS' for x in host['Cases'])
managed=reports['green-run'];assert managed['RecipePassed'] and managed['ExpectedCases']==managed['Executed']==managed['FixturePassed']==14 and managed['FixtureFailed']==managed['Unexecuted']==0 and managed['SourceFreezeUnchanged'] and managed['LoadedAssemblyFilesUnchanged'] and managed['ApprovedSourceFreezeSha256']==gs
assert len(managed['Cases'])==14 and len({x['Case'] for x in managed['Cases']})==14
for x in managed['ActualLoadedAssemblies']:
 p=Path(x['Path']);assert p.resolve().is_relative_to(a) and p.stat().st_size==x['Bytes'] and h(p.read_bytes())==x['Sha256']
for x in managed['Cases']:
 assert x['FixturePassed'] and x['Exception'] is None and x['Actual']==x['Expected']=='{ok,done}' and x['StandardOutputHex']==x['StandardErrorHex']=='' and h(x['Source'].encode())==x['SourceSha256'] and len(x['GeneratedCSharpSha256'])==64
assert next(x['Sha256'] for x in managed['ActualLoadedAssemblies'] if x['Name']=='Erlang.Runtime')==next(x['Sha256'] for x in host['Identities'] if x['Name']=='Erlang.Runtime')
red=reports['red-host'];assert red['Status']=='FAIL' and len(red['Cases'])==1 and red['Cases'][0]['Name']=='SpawnAsyncDistributionHostChecks:RunInitialization' and red['Cases'][0]['Status']=='FAIL';error=red['Cases'][0]['Exception'];assert error.startswith('Erlang.ErlangException: badarg') and error.count('Erlang.ErlangException:')==1 and 'ProcessSpawnOptionRegistrations.cs:line 75' in error and 'SpawnAsyncDistributionHostChecks.cs:line 34' in error
assert not any(x in error for x in ['TimeoutException','AggregateException','MissingMethodException','TypeLoadException','TargetInvocationException','NullReferenceException'])
common=dict(Passed=True,GreenSourceFreezeSha256=gs,GreenSourceClosureSha256=j(gf)['SourceClosureSha256'],RedSourceFreezeSha256=rs,RedSourceClosureSha256=j(rf)['SourceClosureSha256'],TargetBuildPassed=True,SourceClosureFiles=2370,SameFormattedHostHelperRaw=True,HistoricalProductionAndProgramExactPublished325=True,EightTokenStreamsExact=True,Bindings=bindings,ScriptSha256=h(Path(__file__).read_bytes()),OriginalExecuted=False,MainModified=False,NativeExecuted=False)
green=dict(common,Status='PASS_ACTUAL326_TARGET_FOURTEEN_GENERATED_AND_SIX_HOST_GROUPS',GeneratedCasesPassed=14,HostGroupsPassed=6,NewHostInternalControls=3,ManagedRuntimeSameAsFocusedTestRuntime=True)
redguard=dict(common,Status='PASS_ACTUAL326_IDENTICAL_HELPER_HISTORICAL325_VALID_OPTION_REJECTION',OwnedProcessSettled=True,ActualFailure='public_spawn_opt_badarg',SinglePrimaryException=True,NoTimeoutOrCleanupAggregate=True)
for path,value in [(a/'root326-actual-green1-guard.json',green),(a/'root326-actual-historical-red1-guard.json',redguard)]:
 assert not path.exists();w(path,value);print(json.dumps({'Path':path.relative_to(r).as_posix(),'Sha256':h(path.read_bytes())}))
