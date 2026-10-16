"""Passive published342 ordinary contract audit; strict raw variance remains a separate refusal."""
from pathlib import Path
import base64,importlib.util,json,re,sys
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('m',Path(__file__).with_name('reader-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mp,d,run,job,items,rows=m.load('ordinary');H=mp['Head'];assert H=='0114bc4ba2c7b8152612e930e3cd36db1caf4180' and run['id']==mp['Run']
assert run['id']==38968433998 and run['path']=='.github/workflows/differential.yml' and run['event']=='workflow_dispatch'
assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{items[0]["id"]}';oldroot=m.A/'part340-remote-new-review/ordinary/artifact-12073481602';v=m.read(ex/'differential.json');old=m.read(oldroot/'differential.json');fd=m.read(ex/'fd-differential.json');oldfd=m.read(oldroot/'fd-differential.json')
prior=m.read(m.A/'part340-remote-new-review/ordinary-semantic-independent-guard.json');assert prior['Head']=='df2b4078bf14d7337ef4b928f1cb88d9df4fbc5d' and prior['Passed']==6260
assert prior['RawReportSha256']==m.sha((oldroot/'differential.json').read_bytes())
for report,n in [(v,6261),(old,6260)]:
 assert report['Planned']==report['Executed']==report['Passed']==n and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
changes=[]
for key,n in [('Results',778),('ModuleResults',5317),('DiagnosticResults',165)]:
 assert len(old[key])==n and len(v[key])==n+(1 if key=='ModuleResults' else 0) and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in old[key]]==[x.get('Name',x.get('Source')) for x in v[key][:n]]
 changes += [{'Track':key,'Index':i,'Previous340':x,'Current342':y} for i,(x,y) in enumerate(zip(old[key],v[key])) if x!=y]
assert len(changes)<=1
can='237B613D3E7B307D2C623D3E2241227D0D0A';alt='237B623D3E2241222C613D3E7B307D7D0D0A'
policies=m.blobs(H,['tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs'])
for c in changes:
 assert c['Track']=='ModuleResults' and c['Index']==3605
 x,y=c['Previous340'],c['Current342'];assert x['Name']==y['Name']=='erlang-term-display-map'
 assert {k for k in set(x)|set(y) if x.get(k)!=y.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for t in [x,y]:
  assert t['ExpectedStandardOutputHex']==t['SdkStandardOutputHex']==can and t['ExpectedStandardOutputAlternatives']==[alt] and t['NativeStandardOutputHex'] in [can,alt]
  assert t['NativeAndSdkStandardOutputExactlyMatch']==(t['NativeStandardOutputHex']==can) and t['StandardStreamsMatch'] and t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in policies['tools/Erlang.Differential/TermDisplayCases.cs'] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in policies['tools/Erlang.Differential/CompiledStandardOutputContract.cs']
closurepaths=['docs/validation/part-340/final-candidate/source-resource-closure.json','docs/validation/part-342/full-actual3/source-resource-closure.json']
cb=m.blobs(H,closurepaths);pold,pnew=[[x['Path'] for x in json.loads(cb[p])['Files'] if x['Path'].startswith('tools/Erlang.Differential/')] for p in closurepaths]
assert len(pold)==268 and len(pnew)==271 and set(pold)<set(pnew)
bold,bnew=m.blobs(prior['Head'],pold),m.blobs(H,pnew);changed=[p for p in pold if bold[p]!=bnew[p]];assert changed==['tools/Erlang.Differential/CompiledModuleCases.cs']
added={'tools/Erlang.Differential/'+name for name in ['RemoteSourceFunCases.cs','RemoteSourceFunCaseNames.cs','RemoteSourceFunCaseSources.cs']};assert set(pnew)-set(pold)==added
p=changed[0];oldtext=bold[p].decode().replace('\r\n','\n');newtext=bnew[p].decode().replace('\r\n','\n');assert newtext.replace(',\n        .. RemoteSourceFunCases.Modules','')==oldtext
localpath='docs/validation/part-342/full-actual3/tests.json';localbytes=m.blobs(H,[localpath])[localpath];localtests=json.loads(localbytes);assert localtests['Passed']==7200 and localtests['Failed']==0
localnames={x['Name'] for x in localtests['Tests'] if x['Status']=='Passed'}
quoted=r'"(?:[^"\\]|\\.)*"'
constants=lambda text:{key:json.loads(value) for key,value in re.findall(r'(?:public|internal) const string (\w+) = ('+quoted+r');',text)}
names=constants(bnew['tools/Erlang.Differential/RemoteSourceFunCaseNames.cs'].decode());sources=constants(bnew['tools/Erlang.Differential/RemoteSourceFunCaseSources.cs'].decode());catalog=bnew['tools/Erlang.Differential/RemoteSourceFunCases.cs'].decode()
pairs=re.findall(r'new\(\s*RemoteSourceFunCaseNames\.(\w+),\s*RemoteSourceFunCaseSources\.(\w+),',catalog)
assert pairs==[('LexicalRoundtrip','LexicalRoundtrip')] and len(sources)==1 and names['SuccessAtom']=='ok'
assert 'Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(RemoteSourceFunCaseNames.SuccessAtom))' in catalog
native=v['ModuleResults'][-1];source=sources['LexicalRoundtrip'];name=names['LexicalRoundtrip']
assert native['Name']==name and native['Source']==source and native['SourceSha256']==m.sha(source.encode())
assert native['Expected']==native['FixtureExpected']==native['Actual']=='{ok,ok}' and 'oracle/compiled-module/'+name in localnames
assert native['CallerContext']=='UntrappedFresh' and native['CompilerPolicy']=='Default' and native['Passed'] and native['StandardStreamsMatch']
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
report={'Status':'PASS_PUBLISHED342_ORIGINAL6261_FD8_PREVIOUS6260_ORDERED_AND_CAPTURED_FUN_CASE','Head':H,'Run':run['id'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':6261,'Failed':0,'FdPassed':8,'Expressions':778,'CompiledModules':5318,'Diagnostics':165,'AllPrevious6260OrderedContractsVerified':True,'AllOld268PublishedSourcesExactExceptAdditiveCatalog':True,'NewSourceFiles':3,'NewOriginalCases':1,'Managed7200ReportSha256':m.sha(localbytes),'SixExclusionsAndCallerPoliciesPreserved':True,'StrictRawEqualityPassed':not changes,'PredeclaredMapStdoutVarianceRows':len(changes),'RawNormalization':False,'RawReportSha256':m.sha((ex/'differential.json').read_bytes()),'Prior340RawReportSha256':m.sha((oldroot/'differential.json').read_bytes()),'RawZipAndAllMembersApiDigestVerified':True,'GuardSourceSha256':m.sha(Path(__file__).read_bytes()),'ReviewerExecutedVmSdkNativeNetworkMainOrGitMutation':False}
if mode=='strict' and changes:report.update(Status='REFUSED_STRICT_RAW_EQUALITY_EXISTING_PREDECLARED_MAP_STDOUT_FLIP',SemanticComparisonRequiredSeparately=True)
for path,value in [(Path(__file__).parent/f'ordinary-{mode}-historical-differences.json',changes),(Path(__file__).parent/f'ordinary-{mode}-independent-guard.json',report)]:
 assert not path.exists();path.write_bytes((json.dumps(value,indent=2)+'\n').encode())
print(json.dumps(report))
if mode=='strict' and changes:raise SystemExit(1)
