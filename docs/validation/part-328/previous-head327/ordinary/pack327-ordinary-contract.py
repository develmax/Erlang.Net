"""Passive published326 ordinary contract audit; strict raw variance remains a separate refusal."""
from pathlib import Path
import base64,importlib.util,json,re,sys
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('m',Path(__file__).with_name('pack327-audit-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mp,d,run,job,items,rows=m.load('ordinary');H=mp['Head'];assert H=='4bc4c00b713c1cc0bbf32a1c74add45174bbd10a' and run['id']==38856979991 and job['id']==116830552534 and items[0]['id']==12027904580
assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/'artifact-12027904580';oldroot=m.A/'pack326-watch-38850739246-terminal/artifact-12025227970';v=m.read(ex/'differential.json');old=m.read(oldroot/'differential.json');fd=m.read(ex/'fd-differential.json');oldfd=m.read(oldroot/'fd-differential.json')
prior=m.read(m.A/'pack326-ordinary-semantic-independent-guard.json');assert prior['Head']=='b51f050c287f3b91346e1d3e30e86061708fb035' and prior['Passed']==5905
for report,n in [(v,5925),(old,5905)]:
 assert report['Planned']==report['Executed']==report['Passed']==n and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
changes=[]
for key,n in [('Results',778),('ModuleResults',4962),('DiagnosticResults',165)]:
 assert len(old[key])==n and len(v[key])==n+(20 if key=='ModuleResults' else 0) and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in old[key]]==[x.get('Name',x.get('Source')) for x in v[key][:n]]
 changes += [{'Track':key,'Index':i,'Previous326':x,'Current327':y} for i,(x,y) in enumerate(zip(old[key],v[key])) if x!=y]
assert len(changes)<=1
can='237B613D3E7B307D2C623D3E2241227D0D0A';alt='237B623D3E2241222C613D3E7B307D7D0D0A'
policies=m.blobs(H,['tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs'])
for c in changes:
 assert c['Track']=='ModuleResults' and c['Index']==3605
 x,y=c['Previous326'],c['Current327'];assert x['Name']==y['Name']=='erlang-term-display-map'
 assert {k for k in set(x)|set(y) if x.get(k)!=y.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for t in [x,y]:
  assert t['ExpectedStandardOutputHex']==t['SdkStandardOutputHex']==can and t['ExpectedStandardOutputAlternatives']==[alt] and t['NativeStandardOutputHex'] in [can,alt]
  assert t['NativeAndSdkStandardOutputExactlyMatch']==(t['NativeStandardOutputHex']==can) and t['StandardStreamsMatch'] and t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in policies['tools/Erlang.Differential/TermDisplayCases.cs'] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in policies['tools/Erlang.Differential/CompiledStandardOutputContract.cs']
paths=lambda n:[x['Path'] for x in m.read(m.ROOT/f'docs/validation/part-{n}/final-main/source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
pold,pnew=paths(326),paths(327);assert len(pold)==233 and len(pnew)==235 and set(pold)<set(pnew)
bold,bnew=m.blobs(prior['Head'],pold),m.blobs(H,pnew);changed=[p for p in pold if bold[p]!=bnew[p]];assert changed==['tools/Erlang.Differential/CompiledModuleCases.cs']
assert set(pnew)-set(pold)=={'tools/Erlang.Differential/ProcessErrorHandlerCases.cs','tools/Erlang.Differential/ProcessErrorHandlerCaseNames.cs'}
p=changed[0];oldtext=bold[p].decode().replace('\r\n','\n');newtext=bnew[p].decode().replace('\r\n','\n');assert newtext.replace('        .. SpawnAsyncDistributionCases.Modules,\n        .. ProcessErrorHandlerCases.Modules','        .. SpawnAsyncDistributionCases.Modules')==oldtext
managedpath=m.A/'process-error-handler327-focus4/managed-run/actual.json';assert m.sha(managedpath.read_bytes())=='EB16F5B4D3C4E8B08860055234725B6F8AE56EE6220D46C07B9A5CA3F8DDCAB5';managed=m.read(managedpath)
assert managed['RecipePassed'] and managed['Executed']==managed['FixturePassed']==20 and managed['FixtureFailed']==0 and len(managed['Cases'])==20
for native,sdk in zip(v['ModuleResults'][-20:],managed['Cases']):
 assert native['Name']==sdk['Case'] and native['Source']==sdk['Source'] and native['SourceSha256']==sdk['SourceSha256']==m.sha(native['Source'].encode())
 assert native['Expected']==native['FixtureExpected']==native['Actual']==sdk['Actual']==sdk['Expected']=='{ok,done}' and native['CallerContext']==sdk['CallerContext']=='UntrappedFresh'
 assert native['CompilerPolicy']=='Default' and native['Passed'] and native['StandardStreamsMatch'] and sdk['FixturePassed'] and sdk['Exception'] is None
 assert all(native.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
for k in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack']:assert v[k]==old[k]
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
for t in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(t['Source'].encode())==t['SourceSha256']
assert fd['Passed']==8 and fd['Failed']==0 and fd['Results']==oldfd['Results'] and len(fd['Results'])==8
for t in fd['Results']:
 assert t['Passed'] and t['Native']==t['Sdk']=='{ok,done}' and t['ExitCode']==0 and t['StandardError']=='' and t['ActualOutputHex']==t['OutputHex']
 assert base64.b64decode(re.search(r'ERLANG_NET_ETF_BEGIN:([^:]+):ERLANG_NET_ETF_END',t['StandardOutput']).group(1)).hex()=='83680277026f6b7704646f6e65'
raw=(d/f'job-{job["id"]}.log').read_bytes();log=raw.decode('utf-8-sig');assert H in log and 'ad05823719d77c8faee87348ea39513d4e2f99c5' in log and 'checking OTP version... 29.1.1' in log
passes=re.findall(r'^\S+ PASS ([^\r\n]+)\r?$',log,re.M);assert passes==[x['Source'] for x in v['Results']]+['module '+x['Name'] for x in v['ModuleResults']]+['diagnostic '+x['Name'] for x in v['DiagnosticResults']]
report={'Status':'PASS_PUBLISHED327_ORIGINAL5925_FD8_PREVIOUS5905_ORDERED_AND_TWENTY_NEW_ACTUAL_MANAGED_CASES','Head':H,'Run':run['id'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':5925,'Failed':0,'FdPassed':8,'Expressions':778,'CompiledModules':4982,'Diagnostics':165,'AllPrevious5905OrderedContractsVerified':True,'AllOld233PublishedSourcesExactExceptAdditiveCatalog':True,'NewSourceFiles':2,'NewOriginalCases':20,'Managed20Sha256':m.sha(managedpath.read_bytes()),'SixExclusionsAndCallerPoliciesPreserved':True,'StrictRawEqualityPassed':not changes,'PredeclaredMapStdoutVarianceRows':len(changes),'RawNormalization':False,'RawReportSha256':m.sha((ex/'differential.json').read_bytes()),'Prior326RawReportSha256':m.sha((oldroot/'differential.json').read_bytes()),'RawZipAndAllMembersApiDigestVerified':True,'GuardSourceSha256':m.sha(Path(__file__).read_bytes()),'ReviewerExecutedVmSdkNativeNetworkMainOrGitMutation':False}
if mode=='strict' and changes:report.update(Status='REFUSED_STRICT_RAW_EQUALITY_EXISTING_PREDECLARED_MAP_STDOUT_FLIP',SemanticComparisonRequiredSeparately=True)
for path,value in [(m.A/f'pack327-ordinary-{mode}-historical-differences.json',changes),(m.A/f'pack327-ordinary-{mode}-independent-guard.json',report)]:
 assert not path.exists();path.write_bytes((json.dumps(value,indent=2)+'\n').encode())
print(json.dumps(report))
if mode=='strict' and changes:raise SystemExit(1)
