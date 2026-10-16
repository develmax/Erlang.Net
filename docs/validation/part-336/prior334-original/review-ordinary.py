"""Passive published334 ordinary contract audit; strict raw variance remains a separate refusal."""
from pathlib import Path
import base64,importlib.util,json,re,sys
sys.dont_write_bytecode=True
s=importlib.util.spec_from_file_location('m',Path(__file__).with_name('passive-common.py'));m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mode=sys.argv[1];assert mode in ['strict','semantic']
mp,d,run,job,items,rows=m.load('ordinary');H=mp['Head'];assert H=='8f27347c3137172276b3292753cc7a4595746e2f' and run['id']==38933888536
assert run['conclusion']==job['conclusion']=='success' and len(items)==1 and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{items[0]["id"]}';oldroot=m.A/'part333-remote-new-review/ordinary/artifact-12055643586';v=m.read(ex/'differential.json');old=m.read(oldroot/'differential.json');fd=m.read(ex/'fd-differential.json');oldfd=m.read(oldroot/'fd-differential.json')
prior=m.read(m.A/'part333-remote-new-review/ordinary-semantic-independent-guard2.json');assert prior['Head']=='e9725c4b2eef68014aeb70afd0abcf9f646065fd' and prior['Passed']==6042
for report,n in [(v,6085),(old,6042)]:
 assert report['Planned']==report['Executed']==report['Passed']==n and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
changes=[]
for key,n in [('Results',778),('ModuleResults',5099),('DiagnosticResults',165)]:
 assert len(old[key])==n and len(v[key])==n+(43 if key=='ModuleResults' else 0) and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in old[key]]==[x.get('Name',x.get('Source')) for x in v[key][:n]]
 changes += [{'Track':key,'Index':i,'Previous333':x,'Current334':y} for i,(x,y) in enumerate(zip(old[key],v[key])) if x!=y]
assert len(changes)<=1
can='237B613D3E7B307D2C623D3E2241227D0D0A';alt='237B623D3E2241222C613D3E7B307D7D0D0A'
policies=m.blobs(H,['tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs'])
for c in changes:
 assert c['Track']=='ModuleResults' and c['Index']==3605
 x,y=c['Previous333'],c['Current334'];assert x['Name']==y['Name']=='erlang-term-display-map'
 assert {k for k in set(x)|set(y) if x.get(k)!=y.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for t in [x,y]:
  assert t['ExpectedStandardOutputHex']==t['SdkStandardOutputHex']==can and t['ExpectedStandardOutputAlternatives']==[alt] and t['NativeStandardOutputHex'] in [can,alt]
  assert t['NativeAndSdkStandardOutputExactlyMatch']==(t['NativeStandardOutputHex']==can) and t['StandardStreamsMatch'] and t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}'
 assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in policies['tools/Erlang.Differential/TermDisplayCases.cs'] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in policies['tools/Erlang.Differential/CompiledStandardOutputContract.cs']
paths=lambda n:[x['Path'] for x in m.read(m.ROOT/f'docs/validation/part-{n}'/('final-candidate' if n>=333 else 'final-main')/'source-resource-closure.json')['Files'] if x['Path'].startswith('tools/Erlang.Differential/')]
pold,pnew=paths(333),paths(334);assert len(pold)==251 and len(pnew)==254 and set(pold)<set(pnew)
bold,bnew=m.blobs(prior['Head'],pold),m.blobs(H,pnew);changed=[p for p in pold if bold[p]!=bnew[p]];assert changed==['tools/Erlang.Differential/CompiledModuleCases.cs']
assert set(pnew)-set(pold)=={'tools/Erlang.Differential/ProcLibMetadata'+n for n in ['Cases.cs','CaseNames.cs','CaseSources.cs']}
p=changed[0];oldtext=bold[p].decode().replace('\r\n','\n');newtext=bnew[p].decode().replace('\r\n','\n');assert newtext.replace(',\n        .. ProcLibMetadataCases.Modules','')==oldtext
localtests=m.read(m.ROOT/'docs/validation/part-334/final-candidate/tests.json');assert localtests['Passed']==6918 and localtests['Failed']==0
localnames={x['Name'] for x in localtests['Tests'] if x['Status']=='Passed'}
focuspath=m.A/'proc-lib334-actual1/metadata-focused.stdout.log';focus={'ActualRows':[json.loads(line) for line in focuspath.read_text().splitlines() if line.startswith('{')]};assert len(focus['ActualRows'])==47 and all(x['Passed'] for x in focus['ActualRows'])
catalogpaths=['tools/Erlang.Differential/'+g+n for g in ['ProcLibMetadata'] for n in ['CaseNames.cs','CaseSources.cs','Cases.cs']]
spec=importlib.util.spec_from_file_location('case_contracts',Path(__file__).with_name('new-case-source-contracts.py'));catalogreader=importlib.util.module_from_spec(spec);spec.loader.exec_module(catalogreader);declared=catalogreader.contracts(m.blobs(H,catalogpaths))
local43={x['Name']:x for x in focus['ActualRows'][:43]};assert len(local43)==43
for native,contract in zip(v['ModuleResults'][-43:],declared):
 local=local43[native['Name']];assert local['Expected']==local['Actual']==native['Expected']==native['Actual'] and local['Passed'] is True
 assert native['Name']==contract['Name'] and native['Source']==contract['Source'] and native['FixtureExpected']==contract['Expected']
 assert native['Name'].startswith('proc-lib-metadata-') and 'oracle/compiled-module/'+native['Name'] in localnames
 assert native['SourceSha256']==m.sha(native['Source'].encode()) and native['Expected']==native['FixtureExpected']==native['Actual'] and native['CallerContext']=='UntrappedFresh'
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
report={'Status':'PASS_PUBLISHED334_ORIGINAL6085_FD8_PREVIOUS6042_ORDERED_AND_FORTY_THREE_NEW_METADATA_LABEL_ORIGINAL_AND_SDK_CASES','Head':H,'Run':run['id'],'Job':job['id'],'Artifact':items[0]['id'],'Attempt':1,'Passed':6085,'Failed':0,'FdPassed':8,'Expressions':778,'CompiledModules':5142,'Diagnostics':165,'AllPrevious6042OrderedContractsVerified':True,'AllOld251PublishedSourcesExactExceptAdditiveCatalog':True,'NewSourceFiles':3,'NewOriginalCases':43,'Managed43FocusedReceiptSha256':m.sha(focuspath.read_bytes()),'SixExclusionsAndCallerPoliciesPreserved':True,'StrictRawEqualityPassed':not changes,'PredeclaredMapStdoutVarianceRows':len(changes),'RawNormalization':False,'RawReportSha256':m.sha((ex/'differential.json').read_bytes()),'Prior333RawReportSha256':m.sha((oldroot/'differential.json').read_bytes()),'RawZipAndAllMembersApiDigestVerified':True,'GuardSourceSha256':m.sha(Path(__file__).read_bytes()),'ReviewerExecutedVmSdkNativeNetworkMainOrGitMutation':False}
if mode=='strict' and changes:report.update(Status='REFUSED_STRICT_RAW_EQUALITY_EXISTING_PREDECLARED_MAP_STDOUT_FLIP',SemanticComparisonRequiredSeparately=True)
for path,value in [(Path(__file__).parent/f'ordinary-{mode}-historical-differences.json',changes),(Path(__file__).parent/f'ordinary-{mode}-independent-guard.json',report)]:
 assert not path.exists();path.write_bytes((json.dumps(value,indent=2)+'\n').encode())
print(json.dumps(report))
if mode=='strict' and changes:raise SystemExit(1)
