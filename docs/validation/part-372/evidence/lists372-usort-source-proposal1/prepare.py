from pathlib import Path
import json,hashlib,re
O=Path(__file__).resolve().parent;R=O.parents[1];D=O/'source/src/Erlang.Runtime'
def sha(p):return hashlib.sha256((R/p).read_bytes()).hexdigest().upper()
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
p=R/'src/Erlang.Runtime/ListSortOperations.cs';s=p.read_text();a=s.index('    public static Term Unique(');b=s.index('    public static Term Sort(',a)
s=s[:a]+'''    public static ValueTask<Term> Unique(ProcessContext context, Term list)
        => ListUniqueMergeMachine.Sort(context, list);

'''+s[b:]
(D/'ListSortOperations.cs').write_text(s,encoding='utf-8')
p=R/'src/Erlang.Runtime/ListModuleRegistrations.cs';s=p.read_text();old='(context, arguments) => ValueTask.FromResult(ListSortOperations.Unique(context, arguments[0]))';new='(context, arguments) => ListSortOperations.Unique(context, arguments[0])'
assert s.count(old)==1;s=s.replace(old,new)
assert 'ListCallbackSourceExecution.LegacyFilterMap' in s
(D/'ListModuleRegistrations.cs').write_text(s,encoding='utf-8')
old=read('artifacts/erlang371-lists-unique-source-proposal1/parsed-source-clauses.json')
new=read('artifacts/lists372-usort-source-proposal1/parsed-source-clauses.json')
merge=[c for c in new['Clauses'] if c['Line']>=3284]
assert merge==old['Clauses'] and len(merge)==84
def tokens(s):return re.findall(r'@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|[A-Za-z_][A-Za-z_0-9]*|[0-9]+|[^\s]',s)
def cases(s):
 pattern=r'case ListUniqueMergeState\.([A-Za-z_0-9]+):'
 starts=list(re.finditer(pattern,s));d={}
 for i,m in enumerate(starts):
  end=starts[i+1].start() if i+1<len(starts) else s.index('default:',m.end())
  d[m.group(1)]=tokens(s[m.start():end])
 return d
oldcases=cases((R/'src/Erlang.Runtime/ListUniqueMergeMachine.cs').read_text())
newcases=cases((D/'ListUniqueMergeMachine.cs').read_text())
assert len(oldcases)==19 and len(newcases)==25
assert all(newcases[k]==v for k,v in oldcases.items())
out('existing-merge-preservation.json',{'Status':'ALL_84_PINNED_CLAUSE_ASTS_AND_19_CURRENT_MAIN_CASE_BODIES_UNCHANGED','ClauseCount':84,'StateCount':19,'AllClauseAstsExact':True,'AllMainCaseBodiesTokenEquivalent':True,'OriginalMainMachineSha256':sha('src/Erlang.Runtime/ListUniqueMergeMachine.cs'),'CurrentMainNamesSha256':sha('src/Erlang.Runtime/ListUniqueMergeNames.cs'),'OldGeneratorPath':'artifacts/erlang371-lists-unique-source-proposal1/generate.py','OldGeneratorSha256':sha('artifacts/erlang371-lists-unique-source-proposal1/generate.py'),'NonuniqueSortBodyByteTextUnchanged':(D/'ListSortOperations.cs').read_text().split('    public static Term Sort(',1)[1]==(R/'src/Erlang.Runtime/ListSortOperations.cs').read_text().split('    public static Term Sort(',1)[1],'RegistryOnlyOneUsortDelegateChangedAndZfPreserved':True})
files=[]
for p in sorted((O/'source').rglob('*')):
 if p.is_file():
  dest=p.relative_to(O/'source').as_posix();base=R/dest
  files.append({'Source':p.relative_to(R).as_posix(),'Destination':dest,'BaseSha256':sha(dest) if base.exists() else None,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()})
anchor='    Test(Erlang.Tests.ListUniqueSourceHostNames.TestName, Erlang.Tests.ListUniqueSourceHostChecks.Run);'
assert (R/'tests/Erlang.Tests/Program.cs').read_text().count(anchor)==1
out('copy-plan.json',{'Status':'SOURCE_ONLY_ROOT_REVIEW_INTEGRATES_AND_EXECUTES','Files':files,'RegistrationEdit':{'Destination':'tests/Erlang.Tests/Program.cs','BaseSha256':sha('tests/Erlang.Tests/Program.cs'),'ExactUniqueAnchor':anchor,'InsertAfter':'\n'.join('    Test(Erlang.Tests.ListUniqueSortSourceHostNames.'+a+', Erlang.Tests.ListUniqueSortSourceHostChecks.'+b+');' for a,b in [('IdentityTest','Identity'),('MalformedTest','Malformed'),('DeepTest','Deep')])},'NoMainEditsBuildOrTestsByHelper':True})
out('source-proof.json',{'Status':'TRUE_SOURCE_PORT_PROPOSAL_NOT_EXECUTED_NOT_PROMOTED','PinnedCommit':'ad05823719d77c8faee87348ea39513d4e2f99c5','PinnedSource':{'Path':'artifacts/erlang371-root/lists.erl.pinned.txt','Sha256':sha('artifacts/erlang371-root/lists.erl.pinned.txt')},'StateCount':25,'ParsedOrdinaryClauseCount':118,'ManualPublicUsortClauseCount':5,'NewParsedHelperClauses':34,'Proof':[
 'The original 84 merge clauses are byte-identical parsed AST objects and their 19 current Main generated C# case bodies are token-equivalent. Shared reverse and nested merge continuation mechanics are reused without changes. State enum numeric ordinals are internal, unpersisted implementation details; named transitions and source function/arity/line mappings retain their original meanings.',
 'Public usort/1 five clauses source2075-2113 are ported literally as an ordinary C# state, including ascending/descending two-element fast paths, each five-clause three-element case tree and final wildcard helper tail transfer. Nil and singleton return original object. Ascending2 and strictly ascending3 return entire original list. All numeric equal branches retain the exact source-selected X/Y original representative, never cast integers/floats.',
 'usort_1/2 source2116-2123 has four ordinary parsed clauses: equal run tail recursion, ascending or descending split initialization, and final singleton construction. Malformed suffix misses every clause and raises source function_clause with actual residual arguments at2116.',
 'usplit_1/5 (seven), usplit_1_1/6 (eight), usplit_2/5 (seven), usplit_2_1/6 (eight) source3217-3279 are parsed in exact source order. Each >/< guard is TermOrder.Compare sign, each == is NumericEquals as in source. Final split clauses tail-transfer accumulated original representatives into existing rumergel/umergel states; no sort-by-library, fixture recognition or argument reconstruction based on failure type exists.',
 'Private malformed arguments are runtime state argument arrays from the exact transition; source trace uses first-clause function line and real arguments. usplit helpers are all tail calls, so no invented usort/split continuation survives. Existing non-tail merge continuation calls retain their genuine source callers and actual registry lists:reverse/2 invokes the current selected function.',
 'The trampoline does one production ReduceAsync per source-state iteration and uses explicit pending continuation stack; no CLR recursion or runtime interpretation of parsed source AST is introduced. Async reverse remains awaited with scope cleanup through ordinary using scopes on failure/cancellation.',
 'ListSortOperations.Unique now returns the actual asynchronous machine ValueTask, and only registered lists:usort/1 delegate signature changes. The rest of Sort, sort/1, key sorting and comparator usort/2 remain byte-text unchanged; existing zf dynamic filtermap fix is retained.',
 'This closes the ordsets from_list/map/filtermap sorting source-algorithm gap when final native packet and focused regression accept exact final source. Whole-MFA compatibility still needs independent whole-domain ordering/equality dependency closure; source port alone does not promote those API contracts.'
 ],'RequiredGates':['Root build and style on final integrated source.','New native helper/top-case cohort full default/0/1/2/8 stacks plus old original436 values/2180profiles as relevant after source change.','Three permanent HOST groups source residual frames, numeric representative object identity and deep32768 trampoline; existing umerge/ordsets tests retained.','Cancellation/deep current final regression and any shared guard source review.'],'DynamicReverseHostBoundary':'Source proves actual registry call; this proposal does not claim a native lists replacement experiment or add reflection-based access to private runtime state. Existing replacement-module tests concern callback delegates and are not mislabelled as reverse-specific proof.'})
out('freeze.json',{'Files':[{'Path':p.relative_to(O).as_posix(),'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()} for p in sorted(O.rglob('*')) if p.is_file() and p.name!='freeze.json'],'NoMainBuildRuntimeRunNetworkOrPublication':True})
print(json.dumps({'ProductionFiles':sum(x['Destination'].startswith('src/') for x in files),'HostFiles':sum(x['Destination'].startswith('tests/') for x in files),'States':len(newcases),'MergeCasesPreserved':len(oldcases)}))
