"""Read-only published345 original6290/fd8 comparison with source-derived new contracts."""
from pathlib import Path
import importlib.util,json,re,sys
sys.dont_write_bytecode=True
F=Path(__file__).resolve().parent;s=importlib.util.spec_from_file_location('common345',F/'common345.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mp,d,run,job,items,download=m.load('ordinary');H='fe3dc9582997ee1365289e8f08ff20da14e74cb0';OTP='ad05823719d77c8faee87348ea39513d4e2f99c5'
assert mp['Head']==H and run['id']==38975762912 and job['id']==117224702296 and items[0]['id']==12079873039
assert run['path']=='.github/workflows/differential.yml' and run['event']=='workflow_dispatch'
assert run['conclusion']==job['conclusion']=='success' and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/'artifact-12079873039';oldroot=m.A/'part343-remote-new-review/ordinary/artifact-12077509369'
v=m.read(ex/'differential.json');old=m.read(oldroot/'differential.json');fd=m.read(ex/'fd-differential.json');oldfd=m.read(oldroot/'fd-differential.json')
assert m.sha((oldroot/'differential.json').read_bytes())=='F911394556BA930A7C2C728797D45545B8CFD94657040874DBBC212CD4B5ECB9'
for report,n in [(v,6290),(old,6270)]:
 assert report['Planned']==report['Executed']==report['Passed']==n and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
changes=[]
for key,n,added in [('Results',778,0),('ModuleResults',5327,16),('DiagnosticResults',165,4)]:
 assert len(old[key])==n and len(v[key])==n+added and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in old[key]]==[x.get('Name',x.get('Source')) for x in v[key][:n]]
 changes += [dict(Track=key,Index=i,Old=x,New=y) for i,(x,y) in enumerate(zip(old[key],v[key])) if x!=y]
assert len(changes)<=1
can='237B613D3E7B307D2C623D3E2241227D0D0A';alt='237B623D3E2241222C613D3E7B307D7D0D0A'
for c in changes:
 assert c['Track']=='ModuleResults' and c['Index']==3605
 x,y=c['Old'],c['New'];assert x['Name']==y['Name']=='erlang-term-display-map'
 assert {k for k in set(x)|set(y) if x.get(k)!=y.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for t in [x,y]:
  assert t['ExpectedStandardOutputHex']==t['SdkStandardOutputHex']==can and t['ExpectedStandardOutputAlternatives']==[alt] and t['NativeStandardOutputHex'] in [can,alt]
  assert t['NativeAndSdkStandardOutputExactlyMatch']==(t['NativeStandardOutputHex']==can) and t['StandardStreamsMatch'] and t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}'
prefix='tools/Erlang.Differential/'
paths=[prefix+x for x in ['SourceCallBindingCaseNames.cs','SourceCallBindingCaseSources.cs','SourceCallBindingCases.cs','TypePredicateCaseNames.cs','TypePredicateCaseSources.cs','TypePredicateCases.cs','SourceCallBindingDiagnosticCases.cs','CompiledModuleCases.cs','CompiledDiagnosticCases.cs','CompiledDiagnosticExpectations.cs','OracleOutcomeTags.cs','TermDisplayCases.cs','CompiledStandardOutputContract.cs']]+['reference-baseline.json','.github/workflows/differential.yml']
bs=m.blobs(H,paths)
assert json.loads(bs['reference-baseline.json'])['commit']==OTP and json.loads(bs['reference-baseline.json'])['tag']=='OTP-29.1.1'
workflow=bs['.github/workflows/differential.yml'];assert ('test "$(git -C artifacts/otp-source rev-parse HEAD)" = '+OTP).encode() in workflow and b'./configure --prefix="$GITHUB_WORKSPACE/artifacts/otp-oracle"' in workflow and b'artifacts/otp-oracle/bin/erl' in workflow
log=(d/'job-117224702296.log').read_text();assert OTP in log and "Note: switching to '"+OTP+"'." in log and 'checking OTP version... 29.1.1' in log
q=r'"(?:[^"\\]|\\.)*"'
def constants(text):
 values={name:json.loads(value) for name,value in re.findall(r'public const string (\w+)\s*=\s*('+q+r')\s*;',text)}
 for match in re.finditer(r'public const string (\w+)\s*=\s*"""\r?\n(.*?)\r?\n([ \t]*)"""\s*;',text,re.S):
  name,body,indent=match.groups();assert not indent;assert name not in values;values[name]=body
 return values
contracts=[]
for family,count in [('SourceCallBinding',11),('TypePredicate',5)]:
 names=constants(bs[prefix+family+'CaseNames.cs'].decode());sources=constants(bs[prefix+family+'CaseSources.cs'].decode());cat=bs[prefix+family+'Cases.cs'].decode()
 pairs=re.findall(r'new\(\s*'+family+r'CaseNames\.(\w+),\s*'+family+r'CaseSources\.(\w+),',cat);assert len(pairs)==count and names['Done']=='done'
 assert len(re.findall(r'Term\.Tuple\(\s*Term\.A\(OracleOutcomeTags.Success\),\s*Term\.A\('+family+r'CaseNames.Done\)\)',cat))==count
 assert ('.. '+family+'Cases.Modules').encode() in bs[prefix+'CompiledModuleCases.cs']
 contracts += [dict(Name=names[n],Source=sources[src]) for n,src in pairs]
for t,c in zip(v['ModuleResults'][5327:],contracts):
 assert t['Name']==c['Name'] and t['Source']==c['Source'] and t['SourceSha256']==m.sha(c['Source'].encode())
 assert t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}' and t['CallerContext']=='UntrappedFresh' and t['CompilerPolicy']=='Default' and t['Passed'] and t['StandardStreamsMatch']
 assert all(t.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
names=constants(bs[prefix+'SourceCallBindingCaseNames.cs'].decode());sources=constants(bs[prefix+'SourceCallBindingCaseSources.cs'].decode());cat=bs[prefix+'SourceCallBindingDiagnosticCases.cs'].decode();gold=constants(bs[prefix+'CompiledDiagnosticExpectations.cs'].decode())
pairs=re.findall(r'new\(\s*SourceCallBindingCaseNames\.(\w+),\s*SourceCallBindingCaseSources\.(\w+),\s*"",\s*Kind:\s*DiagnosticCaseKind\.(\w+)\s*\)',cat);assert len(pairs)==4 and b'.. SourceCallBindingDiagnosticCases.All' in bs[prefix+'CompiledDiagnosticCases.cs']
for t,(nk,sk,kind) in zip(v['DiagnosticResults'][165:],pairs):
 tag=gold[kind+'Tag'];message=gold[kind+'Message'];expected='{error,['+tag+']}'
 assert t['Name']==names[nk] and t['Source']==sources[sk] and t['SourceSha256']==m.sha(sources[sk].encode())
 assert t['DiagnosticKind']==kind and t['Expected']==t['Reference']==expected and t['ImplementationAccepted']==False and t['ImplementationCode']==gold['ModuleDefinitionCode'] and t['ImplementationMessage']==message and t['Passed']
 assert re.search(r'DiagnosticCaseKind\.'+kind+r'\s*=>\s*Term.Tuple\(Term.A\(OracleOutcomeTags.Failure\), Term.List\(Term.A\('+kind+r'Tag\)\)\)',bs[prefix+'CompiledDiagnosticExpectations.cs'].decode())
assert constants(bs[prefix+'OracleOutcomeTags.cs'].decode())['Failure']=='error'
for t in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(t['Source'].encode())==t['SourceSha256']
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack']:assert v[key]==old[key]
for key,n in [('CompiledModuleTrack',5343),('CompilerDiagnosticTrack',169)]:
 assert v[key]['Planned']==v[key]['Executed']==v[key]['Passed']==n and v[key]['Failed']==0
 assert {k:x for k,x in v[key].items() if k not in ['Planned','Executed','Passed']}=={k:x for k,x in old[key].items() if k not in ['Planned','Executed','Passed']}
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']
assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in bs[prefix+'TermDisplayCases.cs'] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in bs[prefix+'CompiledStandardOutputContract.cs']
native_variants=[dict(Index=i,Name=t['Name']) for i,t in enumerate(v['ModuleResults']) if not t['NativeAndSdkStandardOutputExactlyMatch']]
assert all(x['Index']==3605 and x['Name']=='erlang-term-display-map' for x in native_variants)
result=dict(Status='PASS_PUBLISHED345_ORIGINAL6290_FD8_SEMANTIC_CONTRACTS',Head=H,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Passed=6290,Failed=0,FdPassed=8,NewModules=16,NewNativeDiagnostics=4,AllPrevious6270OrderedContractsPreserved=True,OldPrefixRawChanges=changes,PredeclaredMapStdoutHistoricalChanges=len(changes),CurrentNativeSdkRawMapVariants=native_variants,StrictRawEqualityPassed=not changes and not native_variants,RawReportSha256=m.sha((ex/'differential.json').read_bytes()),Prior6270ReportSha256=m.sha((oldroot/'differential.json').read_bytes()),PinnedOtpCommit=OTP,OriginalGuestCodeExecutedByReviewer=False,SdkExecutedByReviewer=False,MainModified=False,NetworkUsed=False,WholeMfaVerificationClaimed=False,SourceSha256=m.sha(Path(__file__).read_bytes()),CommonSourceSha256=m.sha((F/'common345.py').read_bytes()),ExistingTrustedSourcePins=m.PINS,PublishedSourceBindings=[dict(Path=p,Bytes=len(b),Sha256=m.sha(b)) for p,b in bs.items()],SourceDerivedNewModuleContracts=contracts,NewDiagnosticRows=v['DiagnosticResults'][165:])
out=F/'semantic-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n')
strict=dict(Status='PASS_PUBLISHED345_STRICT_RAW_COMPARISON' if result['StrictRawEqualityPassed'] else 'REFUSED_STRICT_RAW_EQUALITY_PREDECLARED_MAP_OUTPUT_VARIANCE',SemanticGuardSha256=m.sha(out.read_bytes()),HistoricalChanges=changes,CurrentNativeSdkRawMapVariants=native_variants,RawNormalization=False)
out=F/'strict-guard.json';assert not out.exists();out.write_text(json.dumps(strict,indent=2)+'\n');print(json.dumps(dict(Semantic=result['Status'],Strict=strict['Status'],SemanticSha256=m.sha((F/'semantic-guard.json').read_bytes()),StrictSha256=m.sha(out.read_bytes()))))
