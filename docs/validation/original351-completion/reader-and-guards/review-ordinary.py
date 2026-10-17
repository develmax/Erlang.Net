"""Passive actual original351 three-priority-fixture audit; no guest or SDK execution."""
from pathlib import Path
import importlib.util
import json
import re
import sys
sys.dont_write_bytecode=True
F=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('common351',F/'common351.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
mp,d,run,job,items,download=m.load('ordinary')
H='547a2dd266586589a90c7883dac88d09a5a1de20';OTP='ad05823719d77c8faee87348ea39513d4e2f99c5'
assert mp['Head']==H and run['id']==38989291376
assert mp['Job']==job['id'] and items[0]['workflow_run']['id']==run['id']
assert run['path']=='.github/workflows/differential.yml' and run['event']=='workflow_dispatch'
assert run['conclusion']==job['conclusion']=='success' and all(x['conclusion'] in ['success','skipped'] for x in job['steps'])
assert mp['MappedDispatchAttemptTime']<=mp['MappedDispatchAcceptedTime']<=mp['CreatedAt']
ex=d/f"artifact-{items[0]['id']}";oldroot=m.A/'part350-remote-new-review/ordinary/artifact-12083085671'
v=m.read(ex/'differential.json');old=m.read(oldroot/'differential.json');fd=m.read(ex/'fd-differential.json');oldfd=m.read(oldroot/'fd-differential.json')
prior=m.A/'part350-ordinary-independent/semantic-guard.json'
assert m.sha(prior.read_bytes())=='9A289E0430F92276B7DD691A6269F6E086FC52CBA1BC6EBC2546F1B662A71EC2'
assert m.sha((oldroot/'differential.json').read_bytes())=='83582FE46F692A4F0F450DBEEE7A4D6E20C4C9C7F372891F364456AEABA9272A'
for report,n in [(v,6316),(old,6313)]:
 assert report['Planned']==report['Executed']==report['Passed']==n and report['Failed']==0 and report['Complete'] and report['VersionVerified'] and report['Baseline']=='OTP-29.1.1'
 assert not report['InvestigationOnly'] and not report['ReferenceCoverageComplete'] and all(report[k] is None for k in ['AbortedSource','InfrastructureError','AbortedNativeEvidence'])
changes=[]
for key,n,added in [('Results',778,0),('ModuleResults',5366,3),('DiagnosticResults',169,0)]:
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
families=[('SpawnPriority',3)]
paths=[prefix+x+suffix+'.cs' for x,_ in families for suffix in ['CaseNames','CaseSources','Cases']]+[prefix+x for x in ['CompiledModuleCases.cs','OracleOutcomeTags.cs','TermDisplayCases.cs','CompiledStandardOutputContract.cs']]+['reference-baseline.json','.github/workflows/differential.yml']
bs=m.blobs(H,paths)
assert json.loads(bs['reference-baseline.json'])['commit']==OTP and json.loads(bs['reference-baseline.json'])['tag']=='OTP-29.1.1'
workflow=bs['.github/workflows/differential.yml'];assert ('test "$(git -C artifacts/otp-source rev-parse HEAD)" = '+OTP).encode() in workflow and b'./configure --prefix="$GITHUB_WORKSPACE/artifacts/otp-oracle"' in workflow and b'artifacts/otp-oracle/bin/erl' in workflow
log=(d/f"job-{job['id']}.log").read_text();assert OTP in log and "Note: switching to '"+OTP+"'." in log and 'checking OTP version... 29.1.1' in log
# Closed C# constant syntax only: no code execution or SDK-derived expected values.
def constants(text):
 values={name:json.loads(value) for name,value in re.findall(r'public const string (\w+)\s*=\s*("(?:[^"\\]|\\.)*")\s*;',text)}
 for match in re.finditer(r'public const string (\w+)\s*=\s*"""\r?\n(.*?)\r?\n([ \t]*)"""\s*;',text,re.S):
  name,body,indent=match.groups();assert name not in values
  if indent:
   assert all(not line or line.startswith(indent) for line in body.split('\n'));body='\n'.join(line[len(indent):] if line else '' for line in body.split('\n'))
  values[name]=body
 return values
contracts=[]
for family,count in families:
 names=constants(bs[prefix+family+'CaseNames.cs'].decode());sources=constants(bs[prefix+family+'CaseSources.cs'].decode());cat=bs[prefix+family+'Cases.cs'].decode()
 pairs=re.findall(r'new\(\s*'+family+r'CaseNames\.(\w+),\s*'+family+r'CaseSources\.(\w+),',cat)
 assert len(pairs)==count and names['Done']=='done'
 assert len(re.findall(r'Term.Tuple\(Term.A\(OracleOutcomeTags.Success\), Term.A\('+family+r'CaseNames.Done\)\)',cat))==count
 assert ('.. '+family+'Cases.Modules').encode() in bs[prefix+'CompiledModuleCases.cs']
 contracts.extend(dict(Name=names[n],Source=sources[k],Family=family) for n,k in pairs)
assert constants(bs[prefix+'OracleOutcomeTags.cs'].decode())['Success']=='ok'
assert len(contracts)==3 and len({x['Name'] for x in contracts})==3
for t,c in zip(v['ModuleResults'][5366:],contracts):
 assert t['Name']==c['Name'] and t['Source']==c['Source'] and t['SourceSha256']==m.sha(c['Source'].encode())
 assert t['Expected']==t['FixtureExpected']==t['Actual']=='{ok,done}' and t['CallerContext']=='UntrappedFresh' and t['CompilerPolicy']=='Default' and t['Passed'] and t['StandardStreamsMatch']
 assert all(t.get(k) in [None,''] for k in ['NativeStandardOutputHex','NativeStandardErrorHex','SdkStandardOutputHex','SdkStandardErrorHex'])
# All previous C# oracle inputs remain raw-identical; catalog has one additive spread.
import subprocess
prior_head='c1f219f31726fc93e537080f400651d300c47d72'
proc=subprocess.run(['git','ls-tree','-r','--name-only',prior_head,'--','tools/Erlang.Differential'],capture_output=True);assert proc.returncode==0
old_paths=[p for p in proc.stdout.decode().splitlines() if p.endswith('.cs')]
old_blobs=m.blobs(prior_head,old_paths);current_blobs=m.blobs(H,old_paths)
for p in old_paths:
 before,after=old_blobs[p],current_blobs[p]
 if p==prefix+'CompiledModuleCases.cs':
  expected=before.replace(b'.. NumericBoundaryCases.Modules',b'.. NumericBoundaryCases.Modules,\n        .. SpawnPriorityCases.Modules')
  assert after==expected,p
 else:assert after==before,p
for t in v['ModuleResults']+v['DiagnosticResults']:assert m.sha(t['Source'].encode())==t['SourceSha256']
for key in ['OriginalOtpDefectExclusions','SpawnContextDiagnostics','NativeCompilationInvestigation','ExpressionTrack','CompilerDiagnosticTrack']:assert v[key]==old[key]
assert v['CompiledModuleTrack']['Planned']==v['CompiledModuleTrack']['Executed']==v['CompiledModuleTrack']['Passed']==5369 and v['CompiledModuleTrack']['Failed']==0
assert {k:x for k,x in v['CompiledModuleTrack'].items() if k not in ['Planned','Executed','Passed']}=={k:x for k,x in old['CompiledModuleTrack'].items() if k not in ['Planned','Executed','Passed']}
assert len(v['OriginalOtpDefectExclusions'])==6 and all(not x['NativeExecuted'] for x in v['OriginalOtpDefectExclusions'])
assert fd['Passed']==8 and fd['Failed']==0 and len(fd['Results'])==8 and fd['Results']==oldfd['Results']

# FD subprocess results preserve raw marker bytes and exact output streams; no text normalization.
import base64
for row in fd['Results']:
 assert row['Passed'] and row['ExitCode']==0 and row['StandardError']=='' and row['Native']==row['Sdk']=='{ok,done}' and row['ActualOutputHex']==row['OutputHex']
 matches=re.findall(r'ERLANG_NET_ETF_BEGIN:([A-Za-z0-9+/=]+):ERLANG_NET_ETF_END',row['StandardOutput'])
 assert len(matches)==1 and base64.b64decode(matches[0],validate=True)==bytes.fromhex('83680277026F6B7704646F6E65')
assert {k:x for k,x in fd.items() if k!='Results'}=={k:x for k,x in oldfd.items() if k!='Results'}

assert b'ExpectedStandardOutputAlternatives = Array.AsReadOnly' in bs[prefix+'TermDisplayCases.cs'] and b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal) == true' in bs[prefix+'CompiledStandardOutputContract.cs']
native_variants=[dict(Index=i,Name=t['Name']) for i,t in enumerate(v['ModuleResults']) if not t['NativeAndSdkStandardOutputExactlyMatch']]
assert all(x['Index']==3605 and x['Name']=='erlang-term-display-map' for x in native_variants)
expected_pass=[t['Source'] for t in v['Results']]+['module '+t['Name'] for t in v['ModuleResults']]+['diagnostic '+t['Name'] for t in v['DiagnosticResults']]
actual_pass=re.findall(r'^\S+ PASS (.*)$',log,re.M)
assert actual_pass==expected_pass,(len(actual_pass),len(expected_pass))
result=dict(Status='PASS_PUBLISHED351_ORIGINAL6316_FD8_SEMANTIC_CONTRACTS',Head=H,Run=run['id'],Job=job['id'],Artifact=items[0]['id'],Passed=6316,Failed=0,FdPassed=8,NewModules=3,AllPrevious6313OrderedContractsPreserved=True,ExactJobPassOrder6316=True,OldPrefixRawChanges=changes,PredeclaredMapStdoutHistoricalChanges=len(changes),CurrentNativeSdkRawMapVariants=native_variants,StrictRawEqualityPassed=not changes and not native_variants,RawReportSha256=m.sha((ex/'differential.json').read_bytes()),Prior6313ReportSha256=m.sha((oldroot/'differential.json').read_bytes()),PinnedOtpCommit=OTP,OriginalGuestCodeExecutedByReviewer=False,SdkExecutedByReviewer=False,MainModified=False,NetworkUsed=False,WholePriorityOrSchedulerCompatibilityClaimed=False,Disclosure='Passive reviewer did not execute native or managed guest code. Native observations came from the published351 workflow. Selected cases do not establish complete priority/scheduler compatibility.',SourceSha256=m.sha(Path(__file__).read_bytes()),CommonSourceSha256=m.sha((F/'common351.py').read_bytes()),ExistingTrustedSourcePins=m.PINS,PriorOracleSourceFilesRawPreserved=len(old_paths),PublishedSourceBindings=[dict(Path=p,Bytes=len(b),Sha256=m.sha(b)) for p,b in bs.items()],SourceDerivedNewModuleContracts=contracts,SelectedNewRawRows=v['ModuleResults'][5366:])
out=F/'semantic-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',newline='\n')
strict=dict(Status='PASS_PUBLISHED351_STRICT_RAW_COMPARISON' if result['StrictRawEqualityPassed'] else 'REFUSED_STRICT_RAW_EQUALITY_PREDECLARED_MAP_OUTPUT_VARIANCE',SemanticGuardSha256=m.sha(out.read_bytes()),HistoricalChanges=changes,CurrentNativeSdkRawMapVariants=native_variants,RawNormalization=False)
out=F/'strict-guard.json';assert not out.exists();out.write_text(json.dumps(strict,indent=2)+'\n',newline='\n')
print(json.dumps(dict(Semantic=result['Status'],Strict=strict['Status'],SemanticSha256=m.sha((F/'semantic-guard.json').read_bytes()),StrictSha256=m.sha(out.read_bytes()))))
