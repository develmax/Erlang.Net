from pathlib import Path
import json,hashlib,re
O=Path(__file__).resolve().parent;R=O.parents[1];D=O/'source'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
p=R/'src/Erlang.Runtime/OrdsetFullFrame.cs';s=p.read_text();assert s.endswith(');\n')
s=s[:-3]+''')
{
    internal OrdsetFullContinuationKind ContinuationKind { get; init; }
}
'''
(D/'src/Erlang.Runtime/OrdsetFullFrame.cs').write_text(s,encoding='utf-8')
p=R/'src/Erlang.Runtime/ListCallbackSourceExecution.cs';s=p.read_text()
start=s.index('    private static async ValueTask<Term> Filter(');end=s.index('    private static async ValueTask<Term> TransformList(',start)
part=s[start:end];old='OrdsetFullTrace.Push(pending, frame);';assert part.count(old)==1
part=part.replace(old,'OrdsetFullTrace.Push(pending, frame with { ContinuationKind = OrdsetFullContinuationKind.FilterConsReturn });')
s=s[:start]+part+s[end:];(D/'src/Erlang.Runtime/ListCallbackSourceExecution.cs').write_text(s,encoding='utf-8')
p=R/'tests/Erlang.Tests/OrdsetFoldFilterHostChecks.cs';s=p.read_text()
pattern=r'(SourceFrame\(\s*error,\s*OrdsetFoldFilterHostNames.Comprehension,\s*2,\s*OrdsetFoldFilterHostNames.FilterCallLine)(\s*\);)'
s,count=re.subn(pattern,r'\1, 2\2',s);assert count==3
pattern=r'(private static void SourceFrame\([\s\S]*?int line)(\s*\))'
s,count=re.subn(pattern,r'\1, int expectedFrames = 1\2',s);assert count==1
old='Require(Cons.Items(error.StackTraceTerm).Any(frame => frame.Equals(expected)));';assert old in s
s=s.replace(old,'Require(Cons.Items(error.StackTraceTerm).Count(frame => frame.Equals(expected)) == expectedFrames);')
dest=D/'tests/Erlang.Tests/OrdsetFoldFilterHostChecks.cs';dest.parent.mkdir(parents=True,exist_ok=True);dest.write_text(s,encoding='utf-8')
files=[]
for p in sorted(D.rglob('*.cs')):
 dest=p.relative_to(D).as_posix();base=R/dest
 files.append({'Source':p.relative_to(R).as_posix(),'Destination':dest,'BaseSha256':sha(base) if base.exists() else None,'Sha256':sha(p)})
out('copy-plan.json',{'Status':'NARROW_SOURCE_PC_ROLE_FIX_NOT_EXECUTED','Files':files,'NoProgramRegistrationChange':True,'NoMainBuildRunNetwork':True})
diffs=json.loads((R/'artifacts/part372-after-comparison1/differences.json').read_text());relevant=[d for d in diffs if d['Group']=='fold_filter_edges'];assert len(relevant)==18
out('actual-native-witnesses.json',{'Run':39119539009,'ParentConclusion':'failure','IndependentOrdsetsAdmitted':True,'Comparison':'artifacts/part372-after-comparison1/differences.json','ComparisonSha256':sha(R/'artifacts/part372-after-comparison1/differences.json'),'Differences':relevant})
out('source-proof.json',{'Status':'SOURCE_AND_ACTUAL_NATIVE_FIX_PROPOSAL_NOT_EXECUTED_NOT_PROMOTED','PinnedCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','PinnedLists':'lib/stdlib/src/lists.erl:2526','NativeObservations':18,'NativeCases':7,'Cause':'Comprehension predicate-call/error location and successful-cons recursive return location have the same Erlang source file/line/function/arity, but they are distinct instruction/continuation roles. The old record key merges them, so RuntimeExceptionCapture deduplicates two native frames into one. Repeated cons-return continuations themselves must still deduplicate.','Fix':[
 'Keep original four-argument OrdsetFullFrame constructor. Add an init-only internal named ContinuationKind property whose default Operation preserves every existing caller and comparer key.',
 'Set FilterConsReturn only when a true predicate result records the pending recursive-cons return. Current callback/error/generator frames stay Operation. Record equality and its interned continuation identity now distinguish these two actual source roles without changing emitted Erlang stack fields.',
 'Repeated true-prefix pending frames all share FilterConsReturn and remain coalesced by OrdsetFullTrace.Push and RuntimeExceptionCapture. False branches remain tail calls and add no pending frame. No global duplicate-frame rule changes.',
 'Fold, map, filtermap and all other frame construction paths keep default Operation and their existing semantics unchanged. This intentionally fixes only native-confirmed filter branches; no unrelated API promotion.',
 'Strengthen existing permanent HOST assertions: late callback failure, late bad_filter and improper generator after true-prefix require exactly two matching comprehension frames; deep improper traversal likewise. Fold helper still requires exactly one. Previously Any(frame) assertions could not detect this source-identity bug.'
 ],'RequiredGates':['Root style/build, strengthened four HOST groups and actual same21 source cases/full stacks against after-fix Main capture.','Exact source guard successor and final affected regression.','Only then source-close/promote filter/2; native parent run remains failed due independent numeric abort.'],'NoMainEditsOrRuntimeExecution':True})
out('freeze.json',{'Files':[{'Path':p.relative_to(O).as_posix(),'Sha256':sha(p)} for p in sorted(O.rglob('*')) if p.is_file() and p.name!='freeze.json'],'NoMainBuildRuntimeRunNetwork':True})
print(sha(O/'freeze.json'))
