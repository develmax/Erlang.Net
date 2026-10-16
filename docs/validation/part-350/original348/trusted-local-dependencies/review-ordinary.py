"""Passive exact published343 ordinary result, preserving explicit map-output variance."""
from pathlib import Path
import importlib.util,json,re,sys
sys.dont_write_bytecode=True
f=Path(__file__).resolve().parent;s=importlib.util.spec_from_file_location('m',f/'reader-common.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mp,d,run,job,items,rows=m.load('ordinary');H='f26d913cf9bb9e27576f01f55eb9b78ec19fa57c'
assert mp['Head']==H and run['id']==38970910651 and run['path']=='.github/workflows/differential.yml' and run['event']=='workflow_dispatch'
assert run['conclusion']==job['conclusion']=='success' and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
ex=d/f'artifact-{items[0]["id"]}';oldroot=m.A/'part342-remote-new-review/ordinary/artifact-12076906826'
v=m.read(ex/'differential.json');old=m.read(oldroot/'differential.json');fd=m.read(ex/'fd-differential.json');oldfd=m.read(oldroot/'fd-differential.json')
assert m.sha((oldroot/'differential.json').read_bytes())=='F762F5838F32D8611552687A404350DDE95FF3479553D134440039CC40EAEB25'
for report,n in [(v,6270),(old,6261)]:
 assert report['Planned']==report['Executed']==report['Passed']==n and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
changes=[]
for key,n in [('Results',778),('ModuleResults',5318),('DiagnosticResults',165)]:
 assert len(old[key])==n and len(v[key])==n+(9 if key=='ModuleResults' else 0) and all(x['Passed'] for x in v[key])
 assert [x.get('Name',x.get('Source')) for x in old[key]]==[x.get('Name',x.get('Source')) for x in v[key][:n]]
 changes += [{'Track':key,'Index':i,'Old':x,'New':y} for i,(x,y) in enumerate(zip(old[key],v[key])) if x!=y]
assert len(changes)<=1
can='237B613D3E7B307D2C623D3E2241227D0D0A';alt='237B623D3E2241222C613D3E7B307D7D0D0A'
for c in changes:
 assert c['Track']=='ModuleResults' and c['Index']==3605
 x,y=c['Old'],c['New'];assert x['Name']==y['Name']=='erlang-term-display-map'
 assert {k for k in set(x)|set(y) if x.get(k)!=y.get(k)}<={'NativeStandardOutputHex','NativeAndSdkStandardOutputExactlyMatch'}
 for t in [x,y]:
  assert t['ExpectedStandardOutputHex']==t['SdkStandardOutputHex']==can and t['ExpectedStandardOutputAlternatives']==[alt] and t['NativeStandardOutputHex'] in [can,alt]
  assert t['NativeAndSdkStandardOutputExactlyMatch']==(t['NativeStandardOutputHex']==can) and t['StandardStreamsMatch'] and t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}'
pol=m.blobs(H,['tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs'])
assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in pol['tools/Erlang.Differential/TermDisplayCases.cs'] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in pol['tools/Erlang.Differential/CompiledStandardOutputContract.cs']
prefix='tools/Erlang.Differential/';paths=[prefix+x for x in ['ListLengthCaseNames.cs','ListLengthCaseSources.cs','ListLengthCases.cs','CompiledModuleCases.cs']];bs=m.blobs(H,paths)
q=r'"(?:[^"\\]|\\.)*"'
def constants(text):return {k:json.loads(v) for k,v in re.findall(r'public const string (\w+) = ('+q+r');',text)}
names=constants(bs[paths[0]].decode());sources=constants(bs[paths[1]].decode());cat=bs[paths[2]].decode()
pairs=re.findall(r'ListLengthCaseNames\.(\w+),\s*ListLengthCaseSources\.(\w+),',cat)
assert len(pairs)==len(sources)==9 and names['Done']=='done'
assert cat.count('Term.Tuple(Term.A(OracleOutcomeTags.Success), Term.A(ListLengthCaseNames.Done))')==9
assert b'.. ListLengthCases.Modules' in bs[paths[3]]
for t,(nk,sk) in zip(v['ModuleResults'][5318:],pairs):
 assert t['Name']==names[nk] and t['Source']==sources[sk] and t['SourceSha256']==m.sha(sources[sk].encode())
 assert t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}' and t['CallerContext']=='UntrappedFresh' and t['CompilerPolicy']=='Default' and t['Passed'] and t['StandardStreamsMatch']
 assert all(t.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
for t in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(t['Source'].encode())==t['SourceSha256']
for k in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack']:assert v[k]==old[k]
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
assert fd['Passed']==8 and fd['Failed']==0 and fd['Results']==oldfd['Results'] and len(fd['Results'])==8
result=dict(Status='PASS_PUBLISHED343_ORIGINAL6270_FD8_SEMANTIC_CONTRACTS',Head=H,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Passed=6270,Failed=0,FdPassed=8,NewLengthCases=9,AllPrevious6261OrderedContractsPreserved=True,RawReportSha256=m.sha((ex/'differential.json').read_bytes()),PredeclaredMapStdoutVarianceRows=len(changes),StrictRawEqualityPassed=not changes,RawNormalization=False,SourceSha256=m.sha(Path(__file__).read_bytes()),WholeMfaVerificationClaimed=False)
out=f/'ordinary-semantic-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
if changes:
 out=f/'ordinary-strict-refusal.json';assert not out.exists();out.write_text(json.dumps(dict(Status='REFUSED_STRICT_RAW_EQUALITY_PREDECLARED_MAP_OUTPUT_VARIANCE',Changes=changes),indent=2)+'\n')
