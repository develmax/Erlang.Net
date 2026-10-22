import hashlib
import json
import re
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[1]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def write(name,value):
    (OUT/name).write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')

text=(OUT/'source/tests/Erlang.Tests/StaticApplyRegressionSources.cs').read_text()
execution=re.search(r'const string Execution = """\n(.*?)\n""";',text,re.S).group(1)
assert 'Value = erlang:apply(queue,get,[bad])' in execution.splitlines()[21]
syntax=ROOT/'src/Erlang.Compiler/StaticApplySyntax.cs'
parameters=ROOT/'src/Erlang.Compiler/StaticApplyParameters.cs'
assert 'arguments.Items.Count > StaticApplyParameters.MaximumDirectArguments' in syntax.read_text()
assert 'byte.MaxValue' in parameters.read_text()
files=[]
for path in sorted((OUT/'source').rglob('*.cs')):
    dest=path.relative_to(OUT/'source').as_posix()
    assert not (ROOT/dest).exists(),dest
    files.append({'Source':path.relative_to(ROOT).as_posix(),'Destination':dest,'BaseSha256':None,'Bytes':path.stat().st_size,'Sha256':sha(path)})
write('copy-plan.json',{'Status':'SOURCE_ONLY_PERMANENT_CONTROLS_NOT_BUILT','Files':files,
 'Registration':'Test(Erlang.Tests.StaticApplyRegressionNames.Test, Erlang.Tests.StaticApplyRegressionHostChecks.Run);',
 'Resources':'None: five declared Erlang source/basename constants are in separate StaticApplyRegressionSources class.',
 'Dependencies':[{'Path':p,'Sha256':sha(ROOT/p)} for p in ['src/Erlang.Compiler/StaticApplySyntax.cs','src/Erlang.Compiler/StaticApplyParameters.cs','src/Erlang.Compiler/Execution.cs','src/Erlang.Compiler/SourceCallResolution.cs','src/Erlang.Runtime/ErlangApplyDispatch.cs','tests/Erlang.Tests/RuntimeNodeFoundationHostActor.cs','tools/Erlang.Differential/GeneratedModuleCompiler.cs']],
 'NewHostNames':1,'RuntimeChanges':0})
write('coverage.json',{
 'CompiledModules':5,'IndependentGroups':5,'NoReflectionOrAstAssertions':True,
 'Controls':[
  'Qualified literal apply3 executes target with left-to-right mark(a),mark(b) exactly once; pair and dictionary order must both match.',
  'Autoimport apply3 returns queue:new result; explicit imported erlang apply3 under suppression returns same result.',
  'Compiled local apply3 shadow under suppression returns shadow, never queue:new.',
  'Compiled foreign import invokes public registered sentinel exactly once with exact module/function/listargs; wrong lowering produces wrong result and zero sentinel calls.',
  'Suppressed apply3 without local/import binding remains undef; it cannot silently invoke autoimport apply3.',
  'Tail qualified static apply3 badarg has exact queue:get public frame and no tail/erlangapply frame; non-tail preserves exact compiled non_tail/0 source22 continuation.',
  'Syntactically improper and dynamic-improper argument lists both retain native apply3 validation and complete exact native top frame/effectiveargs/error_info; captures return full raw stacks.',
 ],
 'ConservativeSourceBoundary':'Current Lower rejects argument count above255; actual source hash bound. Runtime apply3 own existing1023 limit remains unchanged. No new256/1024 execution controls are claimed by this bounded packet.',
 'KnownLimits':'New compiled modules and new HOST are not executed by author. Root must retain any parse/build/runtime assertion failure. Existing full binding/guard/call_on_load controls remain separate evidence.',
 'NoMainWritesBuildSdkNativeCiNetwork':True})
(OUT/'README.md').write_text('''# Static apply3 permanent behavior controls\n\nThree new test files add one HOST. Five small Erlang modules are compiled and executed through the existing GeneratedModuleCompiler and public runtime actors. The tests assert behavior for qualified/auto/imported-erlang calls, local shadow, foreign import and suppression, argument effects/order once, actual tail/non-tail source frames, and improper/dynamic native argument validation. All independent groups aggregate failures.\n\nThere are no reflection, exact AST or production implementation-mirroring assertions. The foreign import uses a public registry sentinel to prove the actual selected callee and exact arguments. Captured full error stacks are returned; exact native top frame metadata and relevant compiled continuation frames are checked. No normalizer or frame projection is used to compare a native capture.\n\nCurrent conservative source guard declines direct lowering beyond255 arguments; this source boundary is hash-bound, while large256/1024 runtime controls are not claimed. This is source-only, unbuilt and unrun. Root owns integration/build/focus/full validation; no Main or runtime file was edited.\n\nRegister once using copy-plan.json. No csproj resource addition is required because source/basename constants reside separately from test execution code.\n''',encoding='utf-8')
write('source-seal.json',{'Files':[{'Path':p.relative_to(OUT).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(OUT.rglob('*')) if p.is_file() and p.name!='source-seal.json']})
print(json.dumps({'CopyPlanSha256':sha(OUT/'copy-plan.json'),'SealSha256':sha(OUT/'source-seal.json')}))
