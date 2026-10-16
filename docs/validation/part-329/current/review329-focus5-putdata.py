from pathlib import Path
import hashlib
import json

r=Path(__file__).resolve().parents[1]
a=r/'artifacts'
d=a/'non-tail-stack329-root-focus5'
h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
j=lambda p:json.loads(p.read_bytes())
bindings={}
def bind(p):
    p=Path(p)
    bindings[str(p)]=dict(Path=p.relative_to(r).as_posix() if p.is_relative_to(r) else str(p),Bytes=p.stat().st_size,Sha256=h(p))
def rows(p,base):
    bind(p)
    values=j(p)['Files']
    for x in values:
        q=base/x['Path']
        assert q.stat().st_size==x['Bytes'] and h(q)==x['Sha256']
    return values
fp=d/'formatted-source-freeze.json';cl=d/'formatted-source-resource-closure.json'
assert h(fp)=='E17BE62528B6FEEE6AFF86A519EED48169C94DEBF5CD22C3EA64F392FCEC9D76'
assert h(cl)=='CCFCBE2E99B59708AD0E3101907F445C6A78F29FAF10B6A1BEC09CE40886FDFD'
f=j(fp);bind(fp)
assert len(rows(cl,d/'candidate'))==2397 and len(f['Files'])==7
for x in f['Files']: assert h(d/'candidate'/x['Path'])==x['Sha256']
before=d/'tokens-before.json';after=d/'tokens-after.json'
assert before.read_bytes()==after.read_bytes() and len(j(before))==7
for p in (before,after,d/'token-inputs.json',d/'diagnostic-summary.json'):bind(p)
assert j(d/'diagnostic-summary.json')['Passed'] is True
target=d/'target-build'
assert j(target/'process.json')['ExitCode']==0
assert (target/'stderr.log').stat().st_size==0
log=(target/'stdout.log').read_text(encoding='utf-8-sig')
assert 'Предупреждений: 0' in log and 'Ошибок: 0' in log
for p in target.iterdir():
    if p.is_file():bind(p)
hostids=set()
for index,selector in enumerate(f['Selectors']):
    q=d/f'selector-{index:02d}';v=j(q/'actual.json');p=j(q/'process.json')
    assert v['Status']=='PASS' and v['Cases']==[dict(v['Cases'][0])]
    assert v['Cases'][0]['Name']==selector and v['Cases'][0]['Status']=='PASS'
    assert p['ExitCode']==0 and p['OwnedProcessSettled'] and not p['TimedOut'] and p['ValidationError'] is None
    for x in v['Identities']:
        assert h(Path(x['Path']))==x['Sha256'];bind(Path(x['Path']))
        hostids.add((x['Name'],x['Sha256']))
    for name in ('attempt.json','process.json','actual.json','stdout.log','stderr.log'):bind(q/name)
assert len(f['Selectors'])==10 and len(hostids)==3
managed=j(a/'non-tail-stack329-managed-eight-actual1.json')
assert managed['ExpectedCases']==managed['Executed']==managed['FixturePassed']==len(managed['Cases'])==8
assert managed['FixtureFailed']==managed['Unexecuted']==0
selectors=[x['Case'].removeprefix('non-tail-recursion-') for x in managed['Cases']]
pids=[]
for selector in selectors+['original-retained','interpreted-anonymous']:
    q=d/('recursion-'+selector);v=j(q/'actual.json');p=j(q/'process.json')
    assert p['Passed'] and p['ExitCode']==0 and p['OwnedProcessSettled'] and not p['TimedOut'] and p['ValidationError'] is None
    assert p['ActualReportSha256']==h(q/'actual.json') and p['StandardOutputSha256']==h(q/'stdout.log') and p['StandardErrorSha256']==h(q/'stderr.log')
    assert p['SourceFreezeSha256']==h(fp) and p['SourceClosureSha256']==h(cl)
    assert v['RecipePassed'] and v['Executed']==v['FixturePassed']==v['ExpectedCases']==1 and v['FixtureFailed']==0
    assert v['Actual']==v['Expected'] and v['Exception'] is None and v['SourceFreezeUnchanged']
    assert v['StandardOutputHex']==v['StandardErrorHex']=='' and v['ApprovedSourceFreezeSha256']==h(fp)
    assert hashlib.sha256(v['Source'].encode()).hexdigest().upper()==v['SourceSha256']
    assert len(v['ActualLoadedAssemblies'])==5
    for x in v['ActualLoadedAssemblies']:
        path=Path(x['Path']);assert h(path)==x['Sha256'] and path.stat().st_size==x['Bytes'];bind(path)
        if x['Name'] in ('Erlang.Runtime','Erlang.Compiler'):assert (x['Name'],x['Sha256']) in hostids
    if selector in selectors:
        expected=next(c for c in managed['Cases'] if c['Case']==v['Case'])
        for k in expected:
            if k!='FixturePassed':assert expected[k]==v[k]
    elif selector=='original-retained':
        assert v['SourceSha256']=='87C6025BECE5C525D430257D10EF1308EA4B623AA53EF89355E83CFB7EB01BF5' and v['Actual']=='{ok,done}'
    else:assert v['Actual']=='{ok,{8390656,4096}}' and v['GeneratedCSharpSha256'] is None and v['ExecutionKind']=='interpreted-module-null'
    pids.append(p['OwnedPid'])
    for n in ('attempt.json','process.json','actual.json','stdout.log','stderr.log'):bind(q/n)
assert len(set(pids))==10
guards=['green-actual-guard1','managed-eight-actual-guard1','original-retained-actual-guard1','interpreted-anonymous-actual-guard1','historical328-actual-guard1']
for name in guards:
    p=a/('non-tail-stack329-'+name+'.json');v=j(p);assert v['Passed'] is True and v['Status'].startswith('PASS')
    for x in v['Bindings']:
        q=r/x['Path'];assert q.stat().st_size==x['Bytes'] and h(q)==x['Sha256'];bind(q)
    bind(p)
hist=j(a/'non-tail-stack329-historical328-actual-guard1.json')
assert hist['OldNamedDepth']==256 and hist['CurrentNamedDepth']==4096 and hist['ControlledGuestFailuresNotGuestPasses']
old=a/'non-tail-stack329-historical328-focus1'
assert len(rows(old/'source-resource-closure.json',old/'candidate'))==2395
for selector in ('original-retained','named-finite-checksum'):
    q=old/('recursion-'+selector);p=j(q/'process.json')
    assert p['ExitCode']==3221225725 and p['OwnedProcessSettled'] and not p['TimedOut'] and not(q/'actual.json').exists()
    assert (q/'stderr.log').read_bytes().startswith(b'Stack overflow.')
checks=(d/'candidate/tests/Erlang.Tests/NonTailStackLifecycleHostChecks.cs').read_text()
assert checks.index('await unwind.Task.WaitAsync')<checks.index('!Active(',checks.index('await unwind.Task.WaitAsync'))
ledger=a/'non-tail-stack329-actual-case-ledger1.json';l=j(ledger);bind(ledger)
assert l['Passed'] and len(l['HostActuals'])==10 and len(l['NewHostRows'])==1
assert l['ManagedActual']['Sha256']==h(a/'non-tail-stack329-managed-eight-actual1.json')
out=a/'non-tail-stack329-focus5-putdata-actual-peer1.json'
assert not out.exists()
report=dict(Status='PASS_INDEPENDENT_ACTUAL_FOCUS5_SOURCE_OUTPUT_BINDINGS',Passed=True,
    FormattedSourceFreezeSha256=h(fp),GreenSourceFreezeSha256=h(fp),SourceClosureSha256=h(cl),
    SourceClosureFilesRehashed=2397,HistoricalSourceFilesRehashed=2395,OwnedCSharp=7,LexicalTokenStreamsExact=7,
    HostGroupsPassed=10,GeneratedCasesPassed=8,SeparateExtraCasesPassed=2,DistinctManagedChildProcesses=10,
    ActualActorUnwoundAwaitedBeforeLeaseZero=True,GuestAfterEffectsZeroOnCancellation=True,
    HistoricalOriginalExactSourceNewPassOldStackOverflow=True,HistoricalNamedDepth=256,CurrentNamedDepth=4096,
    AllChildrenJoinedExitZeroExceptDeclaredHistoricalFatalControls=True,
    Qualification='Named historical256 and new4096 inputs differ; unchanged retained87C source is the exact oldfatal/newPASS control. Entry guard retains reduction/source-frame/lease flow; this finite observation does not prove generic stack safety, full native continuation or performance. No claim that cancellation never attempts the after AST.',
    ReviewerExecutedSdkVmBuildNativeNetworkMainGitMutation=False,Bindings=list(bindings.values()))
out.write_bytes((json.dumps(report,indent=2)+'\n').encode());print(out,h(out),len(bindings))
