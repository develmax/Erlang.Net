from pathlib import Path
import hashlib,json
r=Path.cwd();d=r/'artifacts/non-tail-stack329-root-focus5';a=r/'artifacts';h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper();write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
fp=d/'formatted-source-freeze.json';assert h(fp)=='E17BE62528B6FEEE6AFF86A519EED48169C94DEBF5CD22C3EA64F392FCEC9D76';f=j(fp);cl=d/'formatted-source-resource-closure.json';assert h(cl)==f['SourceClosureSha256'];rows=j(cl)['Files'];assert (len(rows),f['SourcePaths'],f['CSharpPaths'])==(2397,7,7)
for x in rows:
 q=d/'candidate'/x['Path'];assert q.stat().st_size==x['Bytes'] and h(q)==x['Sha256']
assert (d/'tokens-before.json').read_bytes()==(d/'tokens-after.json').read_bytes();assert len(j(d/'tokens-before.json'))==7
assert j(d/'target-build/process.json')['ExitCode']==0 and j(d/'diagnostic-summary.json')['Passed'] is True
log=(d/'target-build/stdout.log').read_text(encoding='utf-8-sig');assert 'Предупреждений: 0' in log and 'Ошибок: 0' in log
common=[fp,cl,d/'tokens-before.json',d/'tokens-after.json',d/'token-inputs.json',d/'diagnostic-summary.json']+[d/'target-build'/n for n in ['attempt.json','process.json','stdout.log','stderr.log']]
def binds(paths):
 out=[]
 for p in dict.fromkeys(paths):assert p.resolve().is_relative_to(a);out.append(dict(Path=p.relative_to(r).as_posix(),Bytes=p.stat().st_size,Sha256=h(p)))
 return out
hosts=[];hostpaths=[]
for i,selector in enumerate(f['Selectors']):
 q=d/f'selector-{i:02d}';v=j(q/'actual.json');process=j(q/'process.json');assert v['Status']=='PASS' and len(v['Cases'])==1 and v['Cases'][0]['Name']==selector and v['Cases'][0]['Status']=='PASS';assert process['ExitCode']==0 and process['OwnedProcessSettled'] and not process['TimedOut'] and process['ValidationError'] is None
 for x in v['Identities']:assert h(Path(x['Path']))==x['Sha256']
 hosts.append(dict(Path=(q/'actual.json').relative_to(r).as_posix(),Sha256=h(q/'actual.json')));hostpaths += [q/n for n in ['attempt.json','process.json','actual.json','stdout.log','stderr.log']]
assert len(hosts)==10
selectors=['named-finite-checksum','mutual-finite-checksum','anonymous-self-argument-finite-checksum','tail-accumulator-control','shallow-decrement-base-and-checksums','argument-effects-before-and-after-recursive-call','try-after-normal-return-cleanup-order','try-after-error-and-throw-cleanup-once']
cases=[];managedpaths=[];extra={}
for selector in selectors+['original-retained','interpreted-anonymous']:
 q=d/('recursion-'+selector);v=j(q/'actual.json');process=j(q/'process.json');assert process['Passed'] and process['ExitCode']==0 and process['OwnedProcessSettled'] and not process['TimedOut'];assert v['RecipePassed'] and v['FixturePassed']==v['Executed']==v['ExpectedCases']==1 and v['FixtureFailed']==0 and v['Exception'] is None and v['Actual']==v['Expected'];assert v['StandardOutputHex']==v['StandardErrorHex']=='' and v['ApprovedSourceFreezeSha256']==h(fp)
 for x in v['ActualLoadedAssemblies']:assert h(Path(x['Path']))==x['Sha256']
 paths=[q/n for n in ['attempt.json','process.json','actual.json','stdout.log','stderr.log']]
 if selector in selectors:
  assert v['Case']=='non-tail-recursion-'+selector
  if selector in selectors[:4]:assert v['Actual']=='{ok,{8390656,4096}}'
  case={k:v[k] for k in ['Case','Source','SourceSha256','GeneratedCSharpSha256','CallerContext','CompilerPolicy','Expected','Actual','StandardOutputHex','StandardErrorHex','Exception']};assert case['SourceSha256']==sha(case['Source'].encode());case['FixturePassed']=True;cases.append(case);managedpaths+=paths
 else:
  if selector=='original-retained':assert v['SourceSha256']=='87C6025BECE5C525D430257D10EF1308EA4B623AA53EF89355E83CFB7EB01BF5' and v['Actual']=='{ok,done}'
  else:assert v['Actual']=='{ok,{8390656,4096}}' and v['GeneratedCSharpSha256'] is None
  p=a/('non-tail-stack329-'+selector+'-actual-guard1.json');assert not p.exists();write(p,dict(Status='PASS_ACTUAL329_'+selector.upper().replace('-','_'),Passed=True,FormattedSourceFreezeSha256=h(fp),SourceClosureSha256=h(cl),ActualTerm=v['Actual'],SourceSha256=v['SourceSha256'],Bindings=binds(common+paths)));extra[selector]=p
assert len(cases)==8
managed=a/'non-tail-stack329-managed-eight-actual1.json';assert not managed.exists();write(managed,dict(RecipePassed=True,ExpectedCases=8,Executed=8,FixturePassed=8,FixtureFailed=0,Unexecuted=0,ApprovedSourceFreezeSha256=h(fp),SourceClosureSha256=h(cl),SeparateOwnedProcessPerCase=True,Cases=cases,Bindings=binds(managedpaths)))
mg=a/'non-tail-stack329-managed-eight-actual-guard1.json';assert not mg.exists();write(mg,dict(Status='PASS_ACTUAL329_EIGHT_ORDINARY_FINITE4096',Passed=True,FormattedSourceFreezeSha256=h(fp),SourceClosureSha256=h(cl),GeneratedCasesPassed=8,Bindings=binds(common+managedpaths+[managed])))
green=a/'non-tail-stack329-green-actual-guard1.json';assert not green.exists();write(green,dict(Status='PASS_ACTUAL329_GREEN_TEN_HOST_EIGHT_ORDINARY',Passed=True,TargetBuildPassed=True,GreenSourceFreezeSha256=h(fp),SourceClosureSha256=h(cl),SourceClosureFiles=2397,HostGroupsPassed=10,GeneratedCasesPassed=8,LifecycleNamedAnonymousSubcontrols=6,ActualActorUnwoundAwaitedBeforeLeaseZero=True,GuestAfterEffectsZeroOnCancellation=True,NoClaimAfterAstNeverEntered=True,Bindings=binds(common+hostpaths+[managed,mg]+list(extra.values()))))
ledger=a/'non-tail-stack329-actual-case-ledger1.json';assert not ledger.exists();write(ledger,dict(Passed=True,ManagedActual=dict(Path=managed.relative_to(r).as_posix(),Sha256=h(managed)),FormattedSourceFreeze=dict(Path=fp.relative_to(r).as_posix(),Sha256=h(fp)),HostActuals=hosts,NewHostRows=[dict(Name='host_non_tail_stack_async_leaf_old_image_and_lease_cleanup',NamesPath='tests/Erlang.Tests/NonTailStackLifecycleHostNames.cs',Constant='Test',ProgramLine='Test(Erlang.Tests.NonTailStackLifecycleHostNames.Test, Erlang.Tests.NonTailStackLifecycleHostChecks.Run);')]))
b=a/'non-tail-stack329-historical328-focus1';bf=b/'source-freeze.json';bc=b/'source-resource-closure.json';assert h(bf)=='03A3933C2D4AE3D91A7C6783E0EDDA488A7EF6E4B6D293914690E61FEC0F3204';assert len(j(bc)['Files'])==2395;bp=j(b/'recursion-build-non-tail-recursion329-root-managed-repair1/process.json');assert bp['Passed'] and bp['ExitCode']==0
for x in j(bc)['Files']:assert h(b/'candidate'/x['Path'])==x['Sha256']
old=r/'src/Erlang.Compiler/Execution.cs';assert h(old)=='696C484A18F4052DD1AC2C18E6B309E9B1D9B50C0A8E1921E84F0E68D0BF2627';assert (b/'candidate/src/Erlang.Compiler/Execution.cs').read_bytes()==old.read_bytes()
histpaths=[bf,bc,b/'recursion-build-non-tail-recursion329-root-managed-repair1/process.json'];controls=[]
for selector in ['original-retained','named-finite-checksum','tail-accumulator-control']:
 q=b/('recursion-'+selector);v=j(q/'process.json');assert v['OwnedProcessSettled'] and not v['TimedOut'] and v['ValidationError'] is None
 if selector!='tail-accumulator-control':assert v['ExitCode']==3221225725 and not(q/'actual.json').exists() and (q/'stderr.log').read_bytes().startswith(b'Stack overflow.');controls.append(dict(Selector=selector,ExitCode=3221225725,GuestPass=False,OwnedProcessSettled=True))
 else:assert v['Passed'] and v['ExitCode']==0 and j(q/'actual.json')['Actual']=='{ok,{32896,256}}';histpaths.append(q/'actual.json')
 histpaths += [q/n for n in ['attempt.json','process.json','stdout.log','stderr.log']]
hist=a/'non-tail-stack329-historical328-actual-guard1.json';assert not hist.exists();write(hist,dict(Status='PASS_VERIFIED_CONTROLLED_HISTORICAL328_STACK_OVERFLOW_EVIDENCE',Passed=True,ControlledGuestFailuresNotGuestPasses=True,HistoricalFreezeSha256=h(bf),CurrentGreenSourceFreezeSha256=h(fp),OriginalRetainedSourceSha256='87C6025BECE5C525D430257D10EF1308EA4B623AA53EF89355E83CFB7EB01BF5',OldNamedDepth=256,CurrentNamedDepth=4096,SourceProductionExecutionExactPublished328=True,Controls=controls,TailControlPassed=True,Bindings=binds(histpaths)))
for p in [managed,mg,green,ledger,hist]+list(extra.values()):print(p.relative_to(r),h(p))
