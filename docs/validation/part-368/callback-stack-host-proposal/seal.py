import hashlib
import json
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def write(name,value):
    (OUT/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

fixture=OUT/'source/tests/Erlang.Tests/Fixtures/queue_callback368_regression.erl'
lines=fixture.read_text().splitlines()
for line,text in [(7,'[badarity(filter)'),(13,'expansion()]'),(17,'try erlang:apply(queue'),(23,'invoke(Function,Arguments)'),(27,'try queue:filter')]:
    assert text in lines[line-1],(line,lines[line-1])
files=[]
for path in sorted((OUT/'source').rglob('*')):
    if path.is_file():
        dest=path.relative_to(OUT/'source').as_posix()
        assert not (ROOT/dest).exists(),dest
        files.append({'Source':path.relative_to(ROOT).as_posix(),'Destination':dest,'BaseSha256':None,'Bytes':path.stat().st_size,'Sha256':sha(path)})
native=ROOT/'artifacts/queue-original368-original1/download/run-39078144460/artifact-12117323576/capture/queue_callbacks/stack-observations.etf'
discovery=ROOT/'artifacts/queue-higher368-comparison-peer1/initial-discrepancies.json'
write('copy-plan.json',{'Status':'SOURCE_ONLY_PERMANENT_REGRESSION_NOT_BUILT','Files':files,
 'Registration':'Test(Erlang.Tests.QueueCallbackRegressionNames.Test, Erlang.Tests.QueueCallbackRegressionHostChecks.Run);',
 'Resource':'<EmbeddedResource Include="Fixtures/queue_callback368_regression.erl" />',
 'Dependencies':[{'Path':p,'Sha256':sha(ROOT/p)} for p in ['src/Erlang.Runtime/ErlangBasicRegistrations.cs','src/Erlang.Runtime/QueueHigherFilters.cs','src/Erlang.Runtime/QueueHigherTrace.cs','src/Erlang.Runtime/QueueCoreTrace.cs','src/Erlang.Runtime/QueueHigherSource.cs','tools/Erlang.Differential/CompiledModuleExecution.cs','tools/Erlang.Differential/GeneratedModuleCompiler.cs']],
 'NewHostNames':1,'MainRuntimeCiChanges':0})
write('source-proof.json',{
 'NativeRaw':{'Path':native.relative_to(ROOT).as_posix(),'Sha256':sha(native)},
 'Discovery':{'Path':discovery.relative_to(ROOT).as_posix(),'Sha256':sha(discovery)},
 'Cases':7,'BadArityCallbackMfas':['filter/2','filtermap/2','any/2','all/2','delete_with/2','delete_with_r/2'],
 'Assertions':['Real compiled callbackbad/1 tail-invokes realinvoke/2 using actualwrong/0 localFUN; no direct invalidFUNarity queueguardshortcut.',
 'Ownreason checks badarity+actualLocalIdentity module/name/arity/environment and exact argumentvalues1 or3. No forced crossVMFUNindex/unique.',
 'Entire stack term equality requires compiledinvoke/2 integerarity2 source23, exact pinned queue/list helper+public sourceframes, exact compiledcapture/run frames. Extra erlangapply/error_info or lost frame cannotpass.',
 'Improperrear expansion requires full listsreverse effectiveargs[[1|callback_tail],[]], exact erl_stdlib_errors metadata, filter_r765 then filter_r754 then filter721 and exact compiled expansion/run callerframes.',
 'Fixture sets backtrace8 and restores oldvalue in after. All seven independent record assertions aggregate; unexpected success or malformed record fails.',
 'CompiledModuleExecution returns outcome wrapper; HOST explicitly validates ok and unwraps once.'],
 'NoActualNewFixtureSdkOrOriginalExecution':True})
(OUT/'README.md').write_text('''# Permanent compiled queue callback error regressions\n\nFour new payloads add one HOST and one embedded Erlang resource. Six callback APIs must preserve the actual compiled invoke/2 caller for apply2 badarity; a seventh record requires the complete improper rear-expansion reverse/filter_r765/recursive754/public721 stack. Every frame, effective argument, metadata entry and compiled caller source location is checked by exact Term equality. The actual own wrong/0 FUN descriptor is checked relationally without inventing native FUN table numbers.\n\nThe known native discovery is retained and hash-bound. New fixture caller locations derive from the exact new source, not a rewritten native capture. Successor2 repairs test-only access to internal list/outcome constants and reads actual LocalFunctionIdentity.Name as Atom.Name; frozen proposal1 is unchanged. This proposal is source-only and unbuilt/unrun; Root owns validation. All previous payloads/receipts and Main remain unchanged.\n\nRegister the HOST once and embed the exact fixture path in copy-plan.json; the helper compiles with its declared basename. No public diagnostic API or internal reflection is added.\n''',encoding='utf-8')
write('source-seal.json',{'Files':[{'Path':p.relative_to(OUT).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(OUT.rglob('*')) if p.is_file() and p.name!='source-seal.json']})
print(json.dumps({'CopyPlanSha256':sha(OUT/'copy-plan.json'),'SealSha256':sha(OUT/'source-seal.json')}))
