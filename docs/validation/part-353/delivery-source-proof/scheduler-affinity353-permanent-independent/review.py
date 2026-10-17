import hashlib,json,re,subprocess
from pathlib import Path

root=Path(__file__).resolve().parents[2]
p=root/'artifacts/scheduler-affinity353-permanent-proposal2'
c=root/'artifacts/scheduler-affinity353-candidate'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def load(p): return json.loads(p.read_text(encoding='utf-8-sig'))
def text(p): return p.read_text(encoding='utf-8-sig')
assert sha(p/'source-freeze.json')=='6D719CDE2635B96E3D95C6BC4A2F7039D80A8B6DE422667D91212E40B30EF0C3'
f=load(p/'source-freeze.json'); rows=f['Files']; assert len(rows)==42
assert len({x['Path'] for x in rows})==42
owned={x['Path'] for x in rows}
for row in rows:
    for base in (c,p/'source'):
        q=base/row['Path']; assert len(q.read_bytes())==row['Bytes'] and sha(q)==row['Sha256'],q
cl=p/'source-resource-closure.json'; assert sha(cl)==f['SourceClosureSha256']
closure=load(cl)['Files']; assert len(closure)==2953
for row in closure:
    q=c/row['Path']; assert len(q.read_bytes())==row['Bytes'] and sha(q)==row['Sha256'],q
assert sha(p/'baseline-raw.json')==f['BaselineRawSha256']
baseline=load(p/'baseline-raw.json')['Files']
for row in baseline:
    if row['Path'] not in owned:
        assert sha(c/row['Path'])==row['Sha256'],row['Path']
production=[x for x in rows if x['Path'].startswith('src/')]; assert len(production)==20
focus=root/'artifacts/scheduler-affinity352-proposal6/source'
runtime_changes=[]
for row in production:
    if (focus/row['Path']).read_bytes()!=(c/row['Path']).read_bytes():
        runtime_changes.append(row['Path'])
assert set(runtime_changes)=={
 'src/Erlang.Runtime/ProcessRuntime.Schedulers.cs','src/Erlang.Runtime/RuntimeActorDispatcher.cs',
 'src/Erlang.Runtime/RuntimeActorExecutionTurn.cs','src/Erlang.Runtime/RuntimeProcessTaskScheduler.cs'}
tokenpath=root/'artifacts/scheduler-affinity353-permanent-style2/token-review.json'
assert sha(tokenpath)==f['RoslynTokenReviewSha256']
token=load(tokenpath)['Rows']; assert len(token)==42 and {r['Path'] for r in token}==owned
for row in token: assert row['BeforeStyleTokenSha256']==row['ActualTokenSha256']
reordered={r['Path'] for r in token if r['Source1TokenSha256']!=r['ActualTokenSha256']}
assert reordered=={'tests/Erlang.Tests/Program.cs','tests/Erlang.Tests/SchedulerAffinityTestNames.cs'}
style=load(root/'artifacts/scheduler-affinity353-permanent-style2/check.exit.json')
assert style['ExitCode']==0 and style['Settled'] and not style['SdkBuild']
def published(path):
    return subprocess.check_output(['git','show',f['ParentHead']+':'+path],cwd=root).decode('utf-8-sig').replace('\r\n','\n')
program_path='tests/Erlang.Tests/Program.cs'; old=published(program_path); new=text(c/program_path)
oldloop='foreach (var fixture in Erlang.Differential.CompiledModuleCases.All)'
newloop='foreach (var fixture in Erlang.Differential.CompiledModuleCases.All.Where(fixture => !Erlang.Differential.SchedulerAffinityCases.Modules.Contains(fixture)))'
begin=new.index('foreach (var fixture in Erlang.Differential.SchedulerAffinityCases.Modules)')
end=new.index('int failed = 0;',begin)
assert (new[:begin]+new[end:]).replace(newloop,oldloop)==old
catalog='tools/Erlang.Differential/CompiledModuleCases.cs'
assert text(c/catalog).replace(',\n        .. SchedulerAffinityCases.Modules','')==published(catalog)
src=text(c/'tools/Erlang.Differential/SchedulerAffinityCaseSources.cs')
captures=re.findall(r'public const string (\w+) = """\n(.*?)\n        """;',src,re.S)
assert len(captures)==5
report=load(p/'report.json')
for prop,body in captures:
    decoded='\n'.join(line[8:] if line.startswith('        ') else line for line in body.split('\n'))
    name=next(x['Name'] for x in report['CompiledCases'] if x['Property']==prop)
    original=root/'artifacts/scheduler-affinity352-proposal6/portable'/(name+'.erl')
    assert decoded.encode()==original.read_bytes(),name
assert len(report['HostTargets'])==11
for row in report['HostTargets']:
    assert row['Method'] in new[begin:end] and new[begin:end].count(row['Method'])==1
result={
 'Status':'PASS_SOURCE_ONLY_PERMANENT353_INITIAL_TEST_PROJECT_BUILD_ADMISSION',
 'Passed':True,'SourceFreezeSha256':sha(p/'source-freeze.json'),'SourceClosureSha256':sha(cl),
 'ParentHead':f['ParentHead'],'OwnedPaths':42,'ProductionFiles':20,'SourceClosureRows':2953,
 'UnownedBaselineRowsVerified':len([r for r in baseline if r['Path'] not in owned]),
 'ProductionRawSameFocused6':16,'ProductionLayoutOnlyChanged':runtime_changes,
 'RoslynTokenReviewSha256':sha(tokenpath),'TokenRows':42,
 'DeclaredPreStyleRegistrationReordering':sorted(reordered),
 'OldProgramOutsideNewLoopFilterAndAppendExact':True,'OldCatalogExceptOneTailAppendExact':True,
 'PortableSourcesRawExact':5,'AppendedCompiledTests':5,'AppendedHostTests':11,
 'InitializerMigrationReview':{
   'ThreeDefaultSchedulerAssertionsReplaced':True,
   'NewHelperChecks':'actual initializer ProcessContext; nondefault per-owner concurrency1; synchronous system_info scheduler_id admission; recursive InitializeModulesAsync same scheduler/lane and immediate completion',
   'Preserved':'global capacity/child scheduler independence/normal guest completion/retirement join/post-parent-retirement child initialization and guest/all prior test names',
   'NoNewSourceBlockerFound':True},
 'Scope':'Source admission for first Tests-project build only. Earlier focused6 actual30 evidence remains separate. No permanent353 execution, full, readiness or native-profile success inferred.',
 'RemainingGates':['Actual permanent build and executions','Fresh metadata/current-source guards and complete full input preparation','Explicit +S2:2 original profile workflow/build provenance and actual captures'],
 'SdkExecutedByReviewer':False,'NetworkExecuted':False,'MainModified':False,
 'AuthorshipDisclosure':'Reviewer previously reviewed scheduler source and authored passive focused readers; did not author these scheduler production/permanent integration changes.'}
out=Path(__file__).with_name('report.json'); assert not out.exists()
out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Status':result['Status'],'Report':str(out),'Sha256':sha(out)}))
