from pathlib import Path
import hashlib,json
R=Path(__file__).resolve().parent.parent;H=lambda b:hashlib.sha256(b).hexdigest().upper();J=lambda p:json.loads(p.read_bytes())
P=R/'artifacts/non-tail-stack329-source-prototype1';O=R/'artifacts/non-tail-recursion329-oracle2'
assert H((P/'prototype-freeze.json').read_bytes())=='D455F495463B0A52B952D29F6C20BF11940C1541D0CD2EFAB98204697EBD7231'
assert H((O/'source-freeze.json').read_bytes())=='4F458FD75CCDA09B698B655EE669899D1E8503A297FEE63EC305FCB5B44AF27D'
for d,fn in [(P,'prototype-freeze.json'),(O,'source-freeze.json')]:
 for x in J(d/fn)['Files']:
  b=(d/x['Path']).read_bytes();assert len(b)==x['Bytes'] and H(b)==x['Sha256']
for x in J(P/'source-freeze.json')['SourceBindings']+J(O/'source-freeze.json')['Bindings']:
 b=(R/x['Path']).read_bytes();assert len(b)==x['Bytes'] and H(b)==x['Sha256']
parent=(R/'src/Erlang.Compiler/Execution.cs').read_bytes();candidate=(P/'candidate/src/Erlang.Compiler/Execution.cs').read_bytes()
block='''        if (!System.Runtime.CompilerServices.RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();
            await Task.Yield();
            context.Runtime.ThrowIfHalted();
            context.Cancellation.ThrowIfCancellationRequested();
        }
'''.encode()
if b'\r\n' in candidate:block=block.replace(b'\n',b'\r\n')
assert candidate.count(block)==2 and candidate.replace(block,b'')==parent
for token in ['using var flow = lease?.Attach(module);','lease?.Dispose();','return await InvokeFrame(','return await InvokeAnonymousFrame(']:assert token in parent.decode()
cases=J(O/'case-manifest.json')['Cases'];assert len(cases)==8 and len({x['Name'] for x in cases})==8
for x in cases:assert H((R/x['SourcePath']).read_bytes())==x['SourceSha256'] and x['Executed'] is False
fatal=J(O/'retained-fatal-regression.json');assert H((R/fatal['SourcePath']).read_bytes())==fatal['SourceSha256']
assert sum(range(257))==32896 and sum(range(13))==78
v=dict(Status='PASS_SOURCE_FOR_BOUNDED_DIAGNOSTIC_PROTOTYPE329_AND_ORDINARY_FIXTURES_NOT_PROMOTION',Passed=True,
 PrototypeFreezeSha256=H((P/'prototype-freeze.json').read_bytes()),OrdinaryFreezeSha256=H((O/'source-freeze.json').read_bytes()),
 PrototypeOnlyTwoIdenticalPreFrameStackProbeYieldBlocks=True,ExistingLeaseFlowReductionAndLogicalFrameCodeOtherwiseByteExact=True,
 HaltAndCancellationCheckedBeforeAndAfterYield=True,NoCallbackReplayOrFactoryReexecutionIntroduced=True,
 EightFiniteFixturesSourceArithmeticOrderAndCleanupReviewed=True,FiniteChecksum=32896,ShallowChecksum=78,
 ActualFatal328SourceAndProcessBindingsPreservedUnchanged=True,
 Qualification='Source clearance is only for Root-owned fresh bounded diagnostic children. Task.Yield scheduling/context behavior, synchronous completion and exception/finally unwind stack depth, deep AST traversal, lifecycle/lease isolation and native outcomes are not proven. Existing original stack failure must be replayed unchanged; finite fixture success alone cannot authorize a stack-safety claim.',
 NoSdkCompilerVmNativeGitMutationNetworkOrMainWriteByReviewer=True)
out=R/'artifacts/non-tail-stack329-prototype-oracle2-beam310-source-peer.json';assert not out.exists();out.write_bytes((json.dumps(v,indent=2)+'\n').encode());print(H(out.read_bytes()))
