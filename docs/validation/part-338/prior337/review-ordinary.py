"""Passive published337 ordinary contract audit; strict raw variance remains a separate refusal."""
from pathlib import Path
import base64,importlib.util,json,re,sys
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('m',Path(__file__).with_name('passive-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mp,d,run,job,items,rows=m.load('ordinary');H=mp['Head'];assert H=='add206c03c801cbd68c51d23dbbdb8e36f2d581f' and run['id']==mp['Run']
assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{items[0]["id"]}';oldroot=m.A/'part335-remote-new-review/ordinary/artifact-12063256485';v=m.read(ex/'differential.json');old=m.read(oldroot/'differential.json');fd=m.read(ex/'fd-differential.json');oldfd=m.read(oldroot/'fd-differential.json')
prior=m.read(m.A/'part335-remote-new-review/ordinary-semantic-independent-guard2.json');assert prior['Head']=='0f357f55d016936fe7df7cf38408c3009887d382' and prior['Passed']==6114
for report,n in [(v,6171),(old,6114)]:
 assert report['Planned']==report['Executed']==report['Passed']==n and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
changes=[]
for key,n in [('Results',778),('ModuleResults',5171),('DiagnosticResults',165)]:
 assert len(old[key])==n and len(v[key])==n+(57 if key=='ModuleResults' else 0) and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in old[key]]==[x.get('Name',x.get('Source')) for x in v[key][:n]]
 changes += [{'Track':key,'Index':i,'Previous335':x,'Current337':y} for i,(x,y) in enumerate(zip(old[key],v[key])) if x!=y]
assert len(changes)<=1
can='237B613D3E7B307D2C623D3E2241227D0D0A';alt='237B623D3E2241222C613D3E7B307D7D0D0A'
policies=m.blobs(H,['tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs'])
for c in changes:
 assert c['Track']=='ModuleResults' and c['Index']==3605
 x,y=c['Previous335'],c['Current337'];assert x['Name']==y['Name']=='erlang-term-display-map'
 assert {k for k in set(x)|set(y) if x.get(k)!=y.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for t in [x,y]:
  assert t['ExpectedStandardOutputHex']==t['SdkStandardOutputHex']==can and t['ExpectedStandardOutputAlternatives']==[alt] and t['NativeStandardOutputHex'] in [can,alt]
  assert t['NativeAndSdkStandardOutputExactlyMatch']==(t['NativeStandardOutputHex']==can) and t['StandardStreamsMatch'] and t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in policies['tools/Erlang.Differential/TermDisplayCases.cs'] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in policies['tools/Erlang.Differential/CompiledStandardOutputContract.cs']
paths=lambda n:[x['Path'] for x in m.read(m.ROOT/f'docs/validation/part-{n}'/('final-candidate' if n>=333 else 'final-main')/'source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
pold,pnew=paths(335),paths(337);assert len(pold)==257 and len(pnew)==260 and set(pold)<set(pnew)
bold,bnew=m.blobs(prior['Head'],pold),m.blobs(H,pnew);changed=[p for p in pold if bold[p]!=bnew[p]];assert changed==['tools/Erlang.Differential/CompiledModuleCases.cs']
assert set(pnew)-set(pold)=={'tools/Erlang.Differential/ProcLibReport'+n for n in ['Cases.cs','CaseNames.cs','CaseData.cs']}
p=changed[0];oldtext=bold[p].decode().replace('\r\n','\n');newtext=bnew[p].decode().replace('\r\n','\n');assert newtext.replace(',\n        .. ProcLibReportCases.Modules','')==oldtext
localtests=m.read(m.ROOT/'docs/validation/part-337/final-candidate/tests.json');assert localtests['Passed']==7017 and localtests['Failed']==0
localnames={x['Name'] for x in localtests['Tests'] if x['Status']=='Passed'}
quoted=r'"(?:[^"\\]|\\.)*"'
pattern=r'\(('+quoted+r'),\s*('+quoted+r'),\s*('+quoted+r')\)'
data=bnew['tools/Erlang.Differential/ProcLibReportCaseData.cs'].decode()
declared=[tuple(json.loads(value) for value in match) for match in re.findall(pattern,data)]
assert len(declared)==57 and len({x[0] for x in declared})==57
focuspath=m.A/'proc-lib337-all-capture4/report-observations.json';focus=m.read(focuspath)
assert len(focus)==57 and len({x['Name'] for x in focus})==57
local57={x['Name']:x for x in focus}
comparison=m.read(m.A/'root-formatter-original-full-comparison4.json')
assert comparison['Status']=='MATCH' and comparison['Compared']==comparison['Passed']==76
compared={x['Name']:x for x in comparison['Rows'] if x['Track']=='report'}
originalroot=m.A/'part336-remote-new-review/format/artifact-12063070500/capture'
for native,contract in zip(v['ModuleResults'][-57:],declared):
 name,source,expected_etf=contract;local=local57[name]
 original=(originalroot/('report--'+name)/'returned.etf').read_bytes()
 assert base64.b64decode(expected_etf,validate=True)==original
 assert compared[name]['OriginalEtfSha256']==m.sha(original) and compared[name]['Passed']
 assert compared[name]['SourceSha256']==m.sha(source.encode())
 assert native['Name']=='proc-lib-report-'+name and native['Source']==source
 assert local['Actual']==native['Expected']==native['FixtureExpected']==native['Actual']
 assert 'oracle/compiled-module/'+native['Name'] in localnames
 assert native['SourceSha256']==m.sha(source.encode()) and native['CallerContext']=='UntrappedFresh'
 assert native['CompilerPolicy']=='Default' and native['Passed'] and native['StandardStreamsMatch']
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
report={'Status':'PASS_PUBLISHED337_ORIGINAL6171_FD8_PREVIOUS6114_ORDERED_AND_FIFTY_SEVEN_FORMATTER_CASES','Head':H,'Run':run['id'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':6171,'Failed':0,'FdPassed':8,'Expressions':778,'CompiledModules':5228,'Diagnostics':165,'AllPrevious6114OrderedContractsVerified':True,'AllOld257PublishedSourcesExactExceptAdditiveCatalog':True,'NewSourceFiles':3,'NewOriginalCases':57,'Managed57FocusedReceiptSha256':m.sha(focuspath.read_bytes()),'SixExclusionsAndCallerPoliciesPreserved':True,'StrictRawEqualityPassed':not changes,'PredeclaredMapStdoutVarianceRows':len(changes),'RawNormalization':False,'RawReportSha256':m.sha((ex/'differential.json').read_bytes()),'Prior335RawReportSha256':m.sha((oldroot/'differential.json').read_bytes()),'RawZipAndAllMembersApiDigestVerified':True,'GuardSourceSha256':m.sha(Path(__file__).read_bytes()),'ReviewerExecutedVmSdkNativeNetworkMainOrGitMutation':False}
if mode=='strict' and changes:report.update(Status='REFUSED_STRICT_RAW_EQUALITY_EXISTING_PREDECLARED_MAP_STDOUT_FLIP',SemanticComparisonRequiredSeparately=True)
for path,value in [(Path(__file__).parent/f'ordinary-{mode}-historical-differences.json',changes),(Path(__file__).parent/f'ordinary-{mode}-independent-guard.json',report)]:
 assert not path.exists();path.write_bytes((json.dumps(value,indent=2)+'\n').encode())
print(json.dumps(report))
if mode=='strict' and changes:raise SystemExit(1)
