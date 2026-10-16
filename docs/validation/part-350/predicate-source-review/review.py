from pathlib import Path
import hashlib,json,re,subprocess
R=Path.cwd();P=R/'artifacts/additional-type-predicate-proposal';O=R/'artifacts/additional-type-predicate-source-independent'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
freeze=json.loads((P/'source-freeze.json').read_bytes());rows=[]
for row in freeze['Files']:
 b=(P/'source'/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'];rows.append(row)
src=(P/'source/tools/Erlang.Differential/AdditionalTypePredicateCaseSources.cs').read_text(encoding='utf-8')
sources={name:json.loads(literal) for name,literal in re.findall(r'public const string (\w+) = ("(?:\\.|[^"\\])*");',src)}
assert len(sources)==6
expected={'IsBoolean':['false_atom','true_atom'],'IsFunction':['anonymous_fun','captured_fun','external_fun'],'IsList':['empty_list','improper_list','proper_list'],'IsPid':['external_pid','local_pid'],'IsPort':['external_port'],'IsReference':['external_reference','local_reference']}
contexts=['Expected = erlang:','Expected = apply(erlang,','Named = fun erlang:','Expected = Named(Value)','Expected = guarded(Value)','Expected = case Value of CaseValue','Expected = if ','Expected = case Value of AlternativeValue','Expected = (false orelse','Expected = (true andalso','Accepted = [Label ||','Expected = receive','erlang:match_spec_test']
results=[]
for name,s in sources.items():
 predicate=next(x for x in freeze['Predicates'] if x['Function'].replace('_','').lower()==name.lower())
 assert sha(s.encode())==predicate['SourceSha256']
 body=s.split('    check([\n',1)[1].split('    ]),',1)[0]
 terms=re.findall(r'^\s*\{([a-z_]+),(.+),(true|false)\},?$',body,re.M)
 assert len(terms)==38 and len({x[0] for x in terms})==38
 positives=sorted(label for label,_,value in terms if value=='true');assert positives==expected[name]==predicate['PositiveLabels']
 for marker in contexts:assert marker in s,(name,marker)
 assert "{ok,Expected,[],[]} = erlang:match_spec_test({Value},[{{'$1'},[{"+predicate['Function']+",'$1'}],[true]},{'_',[],[false]}],table)" in s
 assert 'when hd(invalid_guard) =:= never; '+predicate['Function']+'(AlternativeValue)' in s
 results.append({'Function':predicate['Function'],'Arity':1,'SourceSha256':sha(s.encode()),'LiteralRows':38,'PositiveLabels':positives,'Contexts':12,'NamedFunSetupIsNotExtraIndependentContext':True,'Expected':'{ok,done}'})
pinned=[]
for path in ['erts/emulator/beam/erl_bif_op.c','erts/emulator/beam/erl_term.h','erts/emulator/beam/erl_node_container_utils.h','erts/emulator/beam/erl_db_util.c']:
 b=subprocess.run(['git','-C',str(R.parent/'otp'),'show',pin+':'+path],capture_output=True,check=True).stdout;pinned.append({'Path':path,'Bytes':len(b),'Sha256':sha(b)})
managed=[]
for path in ['src/Erlang.Runtime/CoreModules.cs','src/Erlang.Runtime/ErlangBasicRegistrations.cs','src/Erlang.Runtime/ErlangBasicBifNames.cs','src/Erlang.Runtime/MatchSpecOperations.cs','src/Erlang.Runtime/ModuleRegistry.cs','src/Erlang.Runtime/MatchSpecProgram.cs','src/Erlang.Compiler/Execution.AsyncGuards.cs','src/Erlang.Compiler/Semantics.cs','src/Erlang.Terms/NativeRecordTerm.cs']:
 b=(R/path).read_bytes();managed.append({'Path':path,'Sha256':sha(b)})
report={'Status':'PASS_SOURCE_EXPECTATIONS_SIX_ADDITIONAL_PREDICATES_NOT_EXECUTED','SourceFreezeSha256':sha((P/'source-freeze.json').read_bytes()),'ParentHead':freeze['ParentHead'],'PinnedOtpCommit':pin,'Files':rows,'FixtureReview':results,'PinnedSource':pinned,'CurrentManagedSource':managed,'Findings':{'BlockingExpectationDefects':[],'Boolean':'Exactly atom true or false; arbitrary atoms/numbers/nonatoms false. Native predicate has no argument-dependent error.','FunctionArity1':'is_any_fun accepts local/external functions without querying code availability, captured environment or fun arity. This is not is_function/2.','List':'Primary list cell OR nil, without walking tail. The improper_list positive is required; native record is a separate boxed kind.','NodeContainers':'is_pid/is_port/is_ref macros include internal OR external containers. Liveness and connectedness are not consulted, so fabricated external ETF identities remain valid positive type witnesses.','MatchSpec':'All six unary BIFs occur as DBIF_ALL in pinned erl_db_util; managed Pure whitelist delegates to registered synchronous completed ValueTasks. Expected {ok,Expected,[],[]} is appropriate for table match-spec test with pure result bodies.','Contexts':'Direct qualified/apply/namedfun/clauseguard/case/if/failed-guardalternative/orelse/andalso/comprehension/receive/matchspec are twelve contexts. Named binding is setup, not a thirteenth context.'},'Limits':['Source/expectation review only: no Erlang compile, ParseAll, original VM, SDK or final compatibility audit has executed these six fixtures.','38 representative term rows do not independently witness all object representations, native architecture tags or lifecycle states. They are not 38 exhaustive physical categories.','No actual local port or separate dead-local-PID row; external port and non-live fabricated external PID cover source-proven tag recognition only.','Anonymous/captured/external fun witnesses have arity0; is_function/1 is arity-independent by source, but higher-arity fun values are not separately witnessed.','Native-record expectation is distinct from an ordinary record tuple; no unsupported internal-only term or host custom Term subclass is claimed.','Existing compatibility classifications remain unchanged; six source fixtures do not establish six verified APIs.'],'SdkExecuted':False,'OriginalExecuted':False,'Network':False,'MainChanged':False}
target=O/'report.json';assert not target.exists();target.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8');print(sha(target.read_bytes()))
