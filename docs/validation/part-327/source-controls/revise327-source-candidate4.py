from pathlib import Path
import json,hashlib,shutil
root=Path(__file__).resolve().parents[1]
old=root/'artifacts/process-error-handler327-candidate3'
out=root/'artifacts/process-error-handler327-candidate4'
assert not out.exists();out.mkdir()
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def dump(p,v):p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8',newline='\n')
assert sha((old/'source-freeze.json').read_bytes())=='5517D79476C99DF4C021648A87D1EFADAB19D3B30C3DDC2DF103A8EA2E0D002E'
shutil.copytree(old/'candidate',out/'candidate')
c=out/'candidate';changed=set()
def edit(path,before,after):
 p=c/path;s=p.read_text();assert s.count(before)==1,(path,s.count(before));p.write_text(s.replace(before,after),encoding='utf-8',newline='\n');changed.add(path)
def write(path,s):
 p=c/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_text(s,encoding='utf-8',newline='\n');changed.add(path)
path='src/Erlang.Terms/ErlangException.cs'
s=(c/path).read_text();start=s.index('    public ErlangException WithStackFrame(');end=s.index('    public ErlangException(string reason)',start)
edit(path,s[start:end],'''    private ErlangExceptionTraceOrigin traceOrigin;

    public ErlangException WithStackFrame(string module, string function, IReadOnlyList<Term> arguments)
        => WithFrame(module, function, arguments, false);

    internal ErlangException WithSelectedHandlerFrame(string module, string function, IReadOnlyList<Term> arguments)
        => WithFrame(module, function, arguments, true);

    private ErlangException WithFrame(string module, string function, IReadOnlyList<Term> arguments, bool selectedHandler)
    {
        if (ExplicitStackTrace)
            return this;
        Term frame = Term.Tuple(Term.A(module), Term.A(function), Cons.From(arguments), Nil.Value);

        return new(Reason, ExceptionClass)
        {
            StackTraceTerm = traceOrigin == ErlangExceptionTraceOrigin.SelectedHandlerCaptured
                ? AppendFrame(StackTraceTerm, frame)
                : new Cons(frame, StackTraceTerm),
            traceOrigin = selectedHandler ? ErlangExceptionTraceOrigin.SelectedHandlerCaptured : traceOrigin
        };
    }

    private static Term AppendFrame(Term trace, Term frame)
    {
        var frames = new List<Term>();
        Term tail = trace;
        while (tail is Cons current)
        {
            frames.Add(current.Head);
            tail = current.Tail;
        }
        Term result = new Cons(frame, tail);
        for (int index = frames.Count - 1; index >= 0; index--)
            result = new Cons(frames[index], result);

        return result;
    }

''')
write('src/Erlang.Terms/ErlangExceptionTraceOrigin.cs','''namespace Erlang;

internal enum ErlangExceptionTraceOrigin
{
    Uncaptured,
    SelectedHandlerCaptured
}
''')
path='src/Erlang.Runtime/ModuleRegistry.ErrorHandler.cs'
p=c/path;s=p.read_text();assert s.count('exception.WithStackFrame(')==2
p.write_text(s.replace('exception.WithStackFrame(','exception.WithSelectedHandlerFrame('),encoding='utf-8',newline='\n');changed.add(path)
path='tools/Erlang.Differential/ProcessErrorHandlerCaseNames.cs'
edit(path,
 '        frame(F,C)->try F() of _->error(accepted) catch C:boom:[{oracle_error_handler,undefined_function,[missing_module,missing_fun,[payload]],_}|_]->ok end.',
 '        frame(F,C)->try F() of _->error(accepted) catch C:boom:Trace->[{oracle_error_handler,undefined_function,A,_}|_]=Trace,case A of 3->ok;[missing_module,missing_fun,[payload]]->ok end end.')
edit(path,
 'catch error:undef:[{missing_module,missing_fun,[payload],_}|_]->caught end',
 'catch error:undef:Trace->[{missing_module,missing_fun,[payload],_}|_]=Trace,caught end')
edit(path,
 'catch error:boom:[{oracle_error_handler,undefined_function,[projected],_}|_]->caught end";',
 'catch error:boom:Trace->[{oracle_error_handler,undefined_function,[projected],_}|_]=Trace,caught end";') if False else None
p=c/path;s=p.read_text();before='catch error:boom:[{oracle_error_handler,undefined_function,[projected],_}|_]->caught end';assert s.count(before)==2
s=s.replace(before,'catch error:boom:Trace->[{oracle_error_handler,undefined_function,[projected],_}|_]=Trace,caught end');p.write_text(s,encoding='utf-8',newline='\n');changed.add(path)
edit(path,'catch error:boom:[{oracle_error_handler,undefined_function,[nested_missing,inner_missing,[]],_}|T]->true=',
 'catch error:boom:Trace->[{oracle_error_handler,undefined_function,[nested_missing,inner_missing,[]],_}|T]=Trace,true=')
edit(path,'catch error:boom:[]->caught end','catch error:boom:Trace->[]=Trace,caught end')
edit(path,'catch error:boom:[{trace_module,trace_function,[],[]}]->caught end',
 'catch error:boom:Trace->[{trace_module,trace_function,[],[]}]=Trace,caught end')
# The separately source-audited same-handler error/1 relation becomes one additional ordinary case.
edit(path,'                nested_error_mode->case F of outer_missing->R=nested_missing:inner_missing(),{kept,R};inner_missing->error(boom,[M,F,A]) end;',
'''                nested_error_mode->case F of outer_missing->R=nested_missing:inner_missing(),{kept,R};inner_missing->error(boom,[M,F,A]) end;
                nested_error_one_mode->case F of outer_missing->R=nested_missing:inner_missing(),put(outer_returned,R),R;inner_missing->error(boom) end;''')
edit(path,'    public const string ExplicitEmpty =',
'''    public const string NestedErrorOne = "nested-error-one-retains-repeated-handler-order";
    public const string NestedErrorOneBody = "install(),erase(outer_returned),put(mode,nested_error_one_mode),caught=try nested_missing:outer_missing() of _->wrong catch error:boom:Trace->Frames=[A||{oracle_error_handler,undefined_function,A,_}<-Trace],case Frames of [3,_|_]->ok;[[nested_missing,inner_missing,[]],_|_]->ok;_->error(wrong_handler_frame_order) end,undefined=get(outer_returned),caught end";
    public const string ExplicitEmpty =''')
edit('tools/Erlang.Differential/ProcessErrorHandlerCases.cs',
 '        Case(ProcessErrorHandlerCaseNames.NestedError, ProcessErrorHandlerCaseNames.NestedErrorBody),',
 '        Case(ProcessErrorHandlerCaseNames.NestedError, ProcessErrorHandlerCaseNames.NestedErrorBody),\n        Case(ProcessErrorHandlerCaseNames.NestedErrorOne, ProcessErrorHandlerCaseNames.NestedErrorOneBody),')
path='tests/Erlang.Tests/ProcessErrorHandlerHostNames.cs'
edit(path,'    internal const string InnerHandler = "host_inner_error_handler";',
'''    internal const string InnerHandler = "host_inner_error_handler";
    internal const string EqualHandler = "host_equal_error_handler";
    internal const string Intermediate = "handler_intermediate";
    internal const string Relay = "relay";
    internal const string TraceModule = "handler_explicit_trace_module";
    internal const string TraceFunction = "handler_explicit_trace_function";''')
edit(path,'    internal const BindingFlags Fields =',
'''    internal const string OuterSource = """
        -module(outer_source_error_handler).
        -export([undefined_function/3]).
        undefined_function(M,F,A)->process_flag(error_handler,host_inner_error_handler),
            R=handler_intermediate:relay(),{forwarded,M,F,A,R}.
        """;
    internal const string IntermediateSource = """
        -module(handler_intermediate).
        -export([relay/0]).
        relay()->R=missing_handler_target:missing_handler_function(handler_payload),{forwarded,R}.
        """;
    internal const BindingFlags Fields =''')
path='tests/Erlang.Tests/ProcessErrorHandlerHostChecks.cs'
edit(path,'        bool explicitTrace = false;',
'''        bool explicitTrace = false;
        ErlangException explicitFailure = new(Term.A(ProcessErrorHandlerHostNames.Fault))
        {
            ExplicitStackTrace = true
        };''')
p=c/path;s=p.read_text();before='            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault)) { ExplicitStackTrace = explicitTrace };';assert s.count(before)==2
s=s.replace(before,'''            if (explicitTrace)
                throw explicitFailure;
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));''');p.write_text(s,encoding='utf-8',newline='\n');changed.add(path)
edit(path,'            Require(explicitEmpty.ExplicitStackTrace && explicitEmpty.StackTraceTerm is Nil);',
'''            Require(ReferenceEquals(explicitEmpty, explicitFailure) && explicitEmpty.ExplicitStackTrace && explicitEmpty.StackTraceTerm is Nil);
            Term customTrace = Term.List(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.TraceModule),
                Term.A(ProcessErrorHandlerHostNames.TraceFunction), Nil.Value, Nil.Value));
            explicitFailure = new(Term.A(ProcessErrorHandlerHostNames.Fault)) { ExplicitStackTrace = true, StackTraceTerm = customTrace };
            ErlangException explicitCustom = await Failure(() => runtime.Modules.Call(context, caller.Definition.Name, ProcessErrorHandlerHostNames.Apply));
            Require(ReferenceEquals(explicitCustom, explicitFailure) && ReferenceEquals(explicitCustom.StackTraceTerm, customTrace));''')
edit(path,'    private static async Task SourceLease(bool fail, bool kill)',
'''    private static async Task RepeatedFrames()
    {
        await using var runtime = new ProcessRuntime();
        int calls = 0;
        runtime.Modules.Register(ProcessErrorHandlerHostNames.EqualHandler, ProcessErrorHandlerHostNames.Undefined, 3,
            async (context, arguments) =>
        {
            CheckArguments(arguments);
            if (++calls == 1)
            {
                Term value = await Call(context);

                return Term.Tuple(Term.A(ProcessErrorHandlerHostNames.Forwarded), value);
            }
            await Task.Yield();
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.EqualHandler);
            ErlangException error = await Failure(() => Call(context));
            CheckFrame(error, ProcessErrorHandlerHostNames.EqualHandler);
            Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
            Require(calls == 2 && frames.Length == 2 && frames[0].Equals(frames[1]));

            return Nil.Value;
        });
    }

    private static async Task IntermediateFrames()
    {
        using var outer = GeneratedModuleCompiler.Compile(ProcessErrorHandlerHostNames.OuterSource);
        using var intermediate = GeneratedModuleCompiler.Compile(ProcessErrorHandlerHostNames.IntermediateSource);
        await using var runtime = new ProcessRuntime();
        outer.Definition.Register(runtime.Modules);
        intermediate.Definition.Register(runtime.Modules);
        runtime.Modules.Register(ProcessErrorHandlerHostNames.InnerHandler, ProcessErrorHandlerHostNames.Undefined, 3,
            (context, arguments) =>
        {
            CheckArguments(arguments);
            throw new ErlangException(Term.A(ProcessErrorHandlerHostNames.Fault));
        });
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            await Change(context, outer.Definition.Name);
            ErlangException error = await Failure(() => Call(context));
            CheckFrame(error, ProcessErrorHandlerHostNames.InnerHandler);
            Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
            Require(frames.Length == 3 && frames[1] is TupleTerm relay && frames[2] is TupleTerm callback &&
                relay.Items[0].Equals(Term.A(ProcessErrorHandlerHostNames.Intermediate)) &&
                relay.Items[1].Equals(Term.A(ProcessErrorHandlerHostNames.Relay)) &&
                callback.Items[0].Equals(Term.A(outer.Definition.Name)) &&
                callback.Items[1].Equals(Term.A(ProcessErrorHandlerHostNames.Undefined)));

            return Nil.Value;
        });
    }

    private static async Task SourceLease(bool fail, bool kill)''')
# Insert these after the original strict nested check, without changing its assertion.
p=c/path;s=p.read_text();start=s.index('    internal static async Task RunNestedFrames()');end=s.index('    private static async Task RepeatedFrames()',start)
segment=s[start:end];needle='''            return Nil.Value;
        });
    }

''';assert segment.count(needle)==1
new=segment.replace(needle,'''            return Nil.Value;
        });
        await RepeatedFrames();
        await IntermediateFrames();
    }

''');edit(path,segment,new)
edit(path,'    private static ValueTask<Term> Change(',
'''    private static async Task SharedFailures()
    {
        await using var runtime = new ProcessRuntime();
        ErlangException shared = new(Term.A(ProcessErrorHandlerHostNames.Fault));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int count = 0;
        foreach (string handler in new[] { ProcessErrorHandlerHostNames.Handler, ProcessErrorHandlerHostNames.OtherHandler })
            runtime.Modules.Register(handler, ProcessErrorHandlerHostNames.Undefined, 3, async (context, arguments) =>
            {
                CheckArguments(arguments);
                if (Interlocked.Increment(ref count) == 2)
                    arrived.TrySetResult();
                await release.Task.WaitAsync(context.Cancellation);
                throw shared;
            });
        await using var first = new RuntimeNodeFoundationHostActor(runtime);
        await using var second = new RuntimeNodeFoundationHostActor(runtime);
        Task<ErlangException> a = first.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.Handler);
            return await Failure(() => Call(context));
        });
        Task<ErlangException> b = second.InvokeAsync(async context =>
        {
            await Change(context, ProcessErrorHandlerHostNames.OtherHandler);
            return await Failure(() => Call(context));
        });
        try { await arrived.Task.WaitAsync(ProcessErrorHandlerHostNames.Deadline); }
        finally { release.TrySetResult(); }
        await Task.WhenAll(a, b).WaitAsync(ProcessErrorHandlerHostNames.Deadline);
        CheckFrame(a.Result, ProcessErrorHandlerHostNames.Handler);
        CheckFrame(b.Result, ProcessErrorHandlerHostNames.OtherHandler);
        Require(!ReferenceEquals(a.Result, b.Result) && !ReferenceEquals(a.Result, shared) &&
            shared.StackTraceTerm is Nil && !shared.ExplicitStackTrace);
        ErlangException markedClone = a.Result.WithStackFrame(ProcessErrorHandlerHostNames.Bridge, ProcessErrorHandlerHostNames.Relay, []);
        CheckFrame(markedClone, ProcessErrorHandlerHostNames.Handler);
        Require(Cons.Items(markedClone.StackTraceTerm).Count() == 2);
        ErlangException fresh = new(shared.Reason);
        ErlangException unmarked = fresh.WithStackFrame(ProcessErrorHandlerHostNames.Handler, ProcessErrorHandlerHostNames.Undefined, [])
            .WithStackFrame(ProcessErrorHandlerHostNames.OtherHandler, ProcessErrorHandlerHostNames.Undefined, []);
        Require(((TupleTerm)((Cons)unmarked.StackTraceTerm).Head).Items[0].Equals(Term.A(ProcessErrorHandlerHostNames.OtherHandler)));
    }

    private static ValueTask<Term> Change(''')
edit(path,'        Require(b.Result.Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.OtherHandler), second.Process.Pid)));',
'''        Require(b.Result.Equals(Term.Tuple(Term.A(ProcessErrorHandlerHostNames.OtherHandler), second.Process.Pid)));
        await SharedFailures();''')
baseline=json.loads((root/'artifacts/root-part326-composed1/source-resource-closure.json').read_bytes())['Files'];base={r['Path']:r for r in baseline}
prior=json.loads((old/'source-freeze.json').read_bytes());owned=set(r['Path'] for r in prior['Files'])|changed
rows=[]
for p in sorted(set(base)|owned):
 data=(c/p).read_bytes();rows.append({'Path':p,'Bytes':len(data),'Sha256':sha(data)})
assert len(rows)==2379 and len(owned)==17
for row in baseline:
 if row['Path'] not in owned:assert (c/row['Path']).read_bytes()==(root/row['Path']).read_bytes()
dump(out/'source-resource-closure.json',{'Files':rows})
files=[dict(next(r for r in rows if r['Path']==p),Added=p not in base,ParentSha256=base.get(p,{}).get('Sha256')) for p in sorted(owned)]
note='''SOURCE ONLY UNBUILT327 revision4, 2026-10-16. Parent published326 b51f/371B/098D2370. Root approved concrete immutable exception provenance extension CCB86A: 11 production paths, two additional Terms paths, no new friend/public API/global stack rewrite. Origin is private enum value only, no Runtime/process/image/activation references. First selected capture prepends and marks; selected and ordinary outward WithStackFrame on marked clones append. Unmarked generic prepend remains unchanged, ExplicitStackTrace returns same exception/trace. Iterative append preserves all frame objects and final proper/improper terminal tail. Repeated equal activations are never deduplicated. Shared original exceptions are never mutated.
Normal sourceOwners lease/admission remains as candidate3 and SourceNote3: deferred actual lease inside Execution.InvokeAsync; non-source EnterSourceCode succeeds before Handler policy, no earlier/doublelease.
Actual diagnostic1 07E89 target0/0 and observed nested CheckFrame RED (settled receipt replay), separate invalid new catch syntax and old pure-HOST loadedCompiler receipt defect remain historical. This revision fixes catch third components to variables with same body pattern expectations. Error/1 matcher accepts pinned arity3 OR exact original callback arguments; error2/3 projections stay exact. One extra source-audited repeated-handler error1 ordinary case appended: twenty total. Original327 execution pending. No managed/SDK/native/VM/network execution by author.
Strict distinct nested HOST retained; new repeated equal-frame, intermediate compiled source continuation and shared-raw-exception clone/isolation controls added. Before-FIRST-handler generic source-callee ordering remains the preexisting broad compiler profile limitation: this bounded marker does not prove/rewrite the whole preexisting stack. No activation is removed by tuple equality, no ambient marker/map/Exception.Data/global origin, no blanket explicit flag. Native plans remain unchanged326 until final composition reseal. No Main/Git mutations.
'''
(out/'SOURCE_NOTE.txt').write_text(note,encoding='utf-8',newline='\n')
prior.update(Status='SOURCE_ONLY_UNBUILT_PROVENANCE_REVISION_REQUIRES_INDEPENDENT_SOURCE_PEER',CandidateDirectory=str(c),SourcePaths=17,CSharpPaths=17,ProductionPaths=11,SourceClosurePaths=2379,FinalSourceClosureSha256=sha((out/'source-resource-closure.json').read_bytes()),Files=files,NewOrdinaryCases=20,ParentCandidateFreezeSha256=sha((old/'source-freeze.json').read_bytes()),ApprovedProvenanceExtensionSha256='CCB86A304CE5610224BB6AE2883767D8C1423B9794EE8AA3E9C074B975BD3E1A',KnownSourceRisk='Before-first-selected-handler generic source-callee ordering remains partial; previous central nested reversal has a proposed immutable provenance repair, unexecuted.',Revision='Two approved Terms paths plus callbackcapture use, valid catch syntax/nativearitymatcher and highvalue HOST/source controls. Actual success not yet claimed.',NoteSha256=sha((out/'SOURCE_NOTE.txt').read_bytes()))
dump(out/'source-freeze.json',prior)
print(json.dumps({'FreezeSha256':sha((out/'source-freeze.json').read_bytes()),'ClosureSha256':prior['FinalSourceClosureSha256'],'Owned':17,'Production':11,'Cases':20,'Closure':2379}))
