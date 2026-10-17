"""Passive verification of locally authored, already settled focused captures."""
from pathlib import Path
import hashlib,json,re
R=Path(__file__).resolve().parents[2];O=Path(__file__).resolve().parent
P=R/'artifacts/spawn-priority351-preparation';C=R/'artifacts/spawn-priority351-candidate';A=R/'artifacts/spawn-priority351-focus-actual1'
def h(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def hb(b):return hashlib.sha256(b).hexdigest().upper()
def rd(p):return json.loads(p.read_bytes())
def constants(path):
 text=path.read_text();return {k:json.loads(v) for k,v in re.findall(r'public const string (\w+) = ("(?:[^"\\]|\\.)*");',text)}
source=rd(P/'styled-source-freeze.json')
assert h(P/'styled-source-freeze.json')=='AA468252959B4C0229410DC09673C9219277FF9B02E1AD63F06897DEAFCD9C2D'
assert h(P/'execute-root.py')==source['ExecutionHelperSha256']=='C66ACEC6FFE57C57EFAABC96FD1DB00598D7AAD7D05C2EA814A146B759D4171A'
assert h(P/'source-resource-closure.json')==source['SourceClosureSha256']=='E61E509F1279AEA447A9DADD86BA3C1A65B53E7F4C4055666E59A27BB66DF955'
assert len(source['Files'])==38 and len(rd(P/'source-resource-closure.json')['Files'])==2798
for row in source['Files']+rd(P/'source-resource-closure.json')['Files']+source['PackageReadinessInputs']:
 path=C/row['Path'];assert h(path)==row['Sha256'] and path.stat().st_size==row['Bytes'],row['Path']
names=rd(P/'focus-targets.json')['Targets'];assert len(names)==len(set(names))==24
assert h(P/'focus-targets.json')==source['TargetManifestSha256']
inv=rd(A/'invocation.json');assert inv['Targets']==names and inv['ApprovedFreezeSha256']==h(P/'styled-source-freeze.json') and inv['SourceClosureSha256']==source['SourceClosureSha256']
assert Path(inv['Candidate']).resolve()==C.resolve() and inv['ReadinessExcluded'] and inv['LedgerFrozenAndIntentionallyStaleForModifiedSources']
receipts=[]
def receipt(directory,stem,command_tail,limit):
 path=directory/(stem+'.exit.json');v=rd(path);live=rd(directory/(stem+'.live.json'))
 assert v['ExitCode']==0 and v['Settled'] and not v['TimedOut'] and v['Failures']==[]
 assert isinstance(v['Pid'],int) and v['Pid']>0 and v['Pid']==live['Pid']
 assert v['Command']==live['Command'] and v['Command'][1:]==command_tail
 assert 0<=v['ElapsedSeconds']<=limit+30
 assert h(directory/(stem+'.stdout.log'))==v['StdoutSha256'] and h(directory/(stem+'.stderr.log'))==v['StderrSha256']
 assert (directory/(stem+'.stderr.log')).read_bytes()==b''
 assert not list(directory.glob(stem+'.kill.*'))
 receipts.append(dict(Path=path.relative_to(R).as_posix(),Sha256=h(path),Pid=v['Pid'],ExitCode=0,Settled=True,ElapsedSeconds=v['ElapsedSeconds']))
 return v
project=C/'tests/Erlang.Priority351.Focus/Erlang.Priority351.Focus.csproj';dll=project.parent/'bin/Debug/net10.0/Priority351Focus.dll'
receipt(A,'build',['build',str(project),'--disable-build-servers','-m:1','-nr:false'],240)
build=(A/'build.stdout.log').read_text(encoding='utf-8-sig')
warning_summaries=re.findall(r'(?m)^\s*(?:(\d+) Warning\(s\)|Предупреждений:\s*(\d+))\s*$',build)
error_summaries=re.findall(r'(?m)^\s*(?:(\d+) Error\(s\)|Ошибок:\s*(\d+))\s*$',build)
assert warning_summaries and error_summaries
assert all(int(left or right)==0 for left,right in warning_summaries+error_summaries)
receipt(A,'parse',[str(dll),'--parse',str(A/'parse.json')],45)
assert (A/'parse.stdout.log').read_text().strip()=='PARSE_ALL 10'
built=rd(A/'built-dlls.json')['Files'];bm={str(Path(x['Path']).resolve()).casefold():x['Sha256'] for x in built};assert len(bm)==len(built)
assert {str(p.resolve()).casefold() for p in dll.parent.glob('*.dll')}==set(bm)
for x in built:assert h(Path(x['Path']))==x['Sha256']
assembly_names=['Erlang.Runtime.dll','Erlang.Terms.dll','Erlang.Serialization.dll','Erlang.Otp.Behaviours.dll','Erlang.Compiler.dll','Erlang.Differential.dll','Erlang.Tests.dll','Priority351Focus.dll']
expected_dlls={str((dll.parent/name).resolve()).casefold():bm[str((dll.parent/name).resolve()).casefold()] for name in assembly_names}
def loaded(report):
 rows=report['LoadedAssemblies'];actual={str(Path(x['Path']).resolve()).casefold():x['Sha256'] for x in rows}
 assert len(rows)==len(actual)==8 and actual==expected_dlls
 assert report['NativeExecuted'] is False and report['StatusesPromoted'] is False
parsed=rd(A/'parse.json');assert parsed['Passed'] and parsed['Name']=='--parse' and parsed['Failure'] is None and parsed['Observation'] is None;loaded(parsed)
expected=[]
def add(name,text,value):expected.append(dict(Name=name,SourceSha256=hb(text.encode('utf-8')),Expected=value))
for name in ['priority_first_observation','priority_flag_old_and_default','priority_duplicate_invalid']:
 raw=(project.parent/'Portable'/(name+'.erl')).read_bytes();assert not raw.startswith(b'\xef\xbb\xbf');add('priority/portable/'+name,raw.decode(),'{ok,done}')
base=C/'tools/Erlang.Differential';cs=constants(base/'NonTailRecursionCaseNames.cs');text=(base/'NonTailRecursionCaseNames.cs').read_text()
depth=int(re.search(r'public const int FiniteDepth = (\d+);',text)[1]);shallow=int(re.search(r'public const int ShallowDepth = (\d+);',text)[1])
result='{ok,{'+str(depth*(depth+1)//2)+','+str(depth)+'}}';d=str(depth);s=str(shallow)
table=[('Named','oracle_stack_named','{walk('+d+'),'+d+'}',cs['NamedBody'],result),('Mutual','oracle_stack_mutual','{left('+d+'),'+d+'}',cs['MutualBody'],result),('Anonymous','oracle_stack_anonymous',cs['AnonymousBody']+'{F(F,'+d+'),'+d+'}','',result),('Tail','oracle_stack_tail','{walk('+d+',0),'+d+'}',cs['TailBody'],result)]
order='['+','.join(map(str,range(shallow,0,-1)))+']'
table += [('Cleanup','oracle_stack_cleanup','put(cleanups,[]),Result=walk('+s+'),{Result,get(cleanups)}',cs['CleanupBody'],'{ok,{'+str(shallow*(shallow+1)//2)+','+order+'}}'),('Failure','oracle_stack_failure','put(cleanups,[]),E=try walk('+s+',error) catch error:boom->caught_error end,ELog=get(cleanups),put(cleanups,[]),T=try walk('+s+',throw) catch throw:boom->caught_throw end,{E,ELog,T,get(cleanups)}',cs['FailureBody'],'{ok,{caught_error,'+order+',caught_throw,'+order+'}}')]
for key,module,run,functions,value in table:add(cs['Prefix']+cs[key],'-module('+module+'). -export([run/0]). run()->'+run+'. '+functions,value)
q=r'"(?:[^"\\]|\\.)*"';rows=re.findall(r'\("hibernate-real-message-resume",\s*('+q+r')\)',(base/'ProcLibLifecycleCaseData.cs').read_text());assert len(rows)==1
add('proc-lib-lifecycle-hibernate-real-message-resume',json.loads(rows[0]),'{ok,ok}')
assert parsed['Sources']==expected and len(expected)==10
assert {p.name for p in A.glob('target-*')}=={'target-'+str(i).zfill(2) for i in range(24)}
completion=rd(A/'completion.json');assert completion['ManagedPassed']==24 and completion['ParseAll']==10 and completion['ApprovedSourceFreezeSha256']==h(P/'styled-source-freeze.json')
assert all(completion[k] is False for k in ['NativeExecuted','CompatibilityStatusesPromoted','FullValidationExecuted','ReadinessExecuted'])
compiled={x['Name']:x for x in expected};outcomes=[]
for i,name in enumerate(names):
 directory=A/('target-'+str(i).zfill(2));path=directory/'outcome.json'
 receipt(directory,'process',[str(dll),'--target',name,str(path)],90)
 v=rd(path);assert v['Name']==name and v['Passed'] and v['Failure'] is None and v['Sources']==expected;loaded(v)
 assert (directory/'process.stdout.log').read_text().strip()=='PASS '+name
 assert completion['Targets'][i]==dict(Name=name,OutcomeSha256=h(path))
 observation=v['Observation']
 if name in compiled:
  assert observation['Name']==name and observation['Expected']==observation['Actual']==compiled[name]['Expected'] and observation['Passed']
  assert observation['CallerContext']=='UntrappedFresh' and observation['CompilerPolicy']=='Default'
  assert re.fullmatch('[0-9A-F]{64}',observation['CSharpSha256'])
 else:assert observation is None
 outcomes.append(dict(Name=name,OutcomeSha256=h(path),Compiled=name in compiled,LoadedAssembliesMatchInitialBuiltManifest=True))
assert len({x['Pid'] for x in receipts})==26
history=[]
for label,code in [('spawn-priority351-core-build-actual1',1),('spawn-priority351-core-build-actual2',0)]:
 p=R/'artifacts'/label;e=rd(p/'exit.json');assert e['ExitCode']==code and e['Settled'] and not e['TimedOut'] and e['LifecycleFailures']==[] and e['GuestTestsExecuted'] is False
 assert h(p/'stdout.log')==e['StdoutSha256'] and h(p/'stderr.log')==e['StderrSha256']
 if code==1:assert 'CS0136' in (p/'stdout.log').read_text()
 history.append(dict(Path=(p/'exit.json').relative_to(R).as_posix(),Sha256=h(p/'exit.json'),ExitCode=code,GuestTestsExecuted=False))
wrapper=R/'artifacts/root-build-priority351.py'
result=dict(Status='PASS_INDEPENDENT_ACTUAL_PRIORITY351_FOCUSED24_PARSE10_BUILD_AND_DLL_BINDINGS',ManagedPassed=24,Failed=0,ParseAll=10,SourceFreezeSha256=h(P/'styled-source-freeze.json'),SourceClosureSha256=h(P/'source-resource-closure.json'),SourceRows38RawExact=True,ClosureRows2798RawExact=True,PackageReadinessInputsRawExact=True,InitialBuiltDllManifestSha256=h(A/'built-dlls.json'),Exact8LoadedDllSetsMatchInitialBuildForParseAndAll24Targets=True,DistinctActualSettledPids26=True,SourceDerivedCompiledContracts=expected,Outcomes=outcomes,ProcessReceipts=receipts,CompletedControllerManifestSha256=h(A/'completion.json'),InitialCoreBuildHistory=history,PreSdkRawSameRefusal=dict(OriginalWrapperPath=wrapper.relative_to(R).as_posix(),OriginalWrapperSha256=h(wrapper),RawReceiptAvailable=False,Explanation='Root reports original wrapper raw-same assumption refused before output mkdir and before SDK; only conversation terminal trace retained. Do not invent a physical refusal receipt or classify as runtime failure.'),ProductionPatternRenamePeerSha256=h(R/'artifacts/spawn-priority351-styled-independent/rename2-review.json'),SourceRunnerPeerSha256=h(R/'artifacts/spawn-priority351-styled-independent/focused-runner-review.json'),NativeExecutedByReviewer=False,SdkExecutedByReviewer=False,MainModified=False,NetworkUsed=False,CompatibilityStatusesPromoted=False,FullValidationClaimed=False,WholeSpawnOptOrNativeSchedulerParityClaimed=False,ReaderSha256=h(Path(__file__)))
out=O/'report.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',newline='\n');print(h(out))
