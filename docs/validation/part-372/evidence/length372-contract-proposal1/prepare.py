from pathlib import Path
import hashlib, json, subprocess

root=Path(__file__).resolve().parents[2]
out=Path(__file__).resolve().parent
files=[]
def emit(path,text):
    before=(root/path).read_bytes()
    target=out/'source'/path
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_bytes(text.encode())
    files.append({'Source':str(target.relative_to(root)).replace('\\','/'),'Destination':path,'BeforeSha256':hashlib.sha256(before).hexdigest().upper(),'AfterSha256':hashlib.sha256(target.read_bytes()).hexdigest().upper()})

p='tests/Erlang.Tests/ListLengthHostChecks.cs'
s=(root/p).read_text()
s=s.replace('    private static async Task Boundary(', '''    public static Task OrdsetSizeProgress() => Boundary(null, false, ordset: true);

    public static Task OrdsetSizeCancellation() => Boundary(null, true, ordset: true);

    public static Task OrdsetSizeSuspendResume() => Boundary(null, false, suspend: true, ordset: true);

    public static async Task LateImproperOriginalArgument()
    {
        foreach (bool ordset in new[] { false, true })
        {
            await using var runtime = new ProcessRuntime();
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await using var observer = new RuntimeNodeFoundationHostActor(runtime);
            var queue = new ListLengthYieldContext();
            var entered = new TaskCompletionSource<Task<Term>>(TaskCreationOptions.RunContinuationsAsynchronously);
            Term original = Term.A(ListLengthHostNames.ImproperTail);
            for (int index = 0; index < ListLengthHostNames.Cells; index++)
                original = new Cons(new MapTerm([]), original);
            Task<Term> invocation = actor.InvokeAsync(async context =>
            {
                var previous = SynchronizationContext.Current;
                Task<Term> operation;
                SynchronizationContext.SetSynchronizationContext(queue);
                try
                {
                    operation = runtime.Modules.Call(
                        context,
                        ordset ? ListLengthHostNames.Ordsets : ErlangModuleNames.Module,
                        ordset ? ListLengthHostNames.Size : ErlangModuleNames.Length,
                        original
                    ).AsTask();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(previous);
                }
                entered.TrySetResult(operation);

                return await operation;
            });
            Task<Term> pending = await entered.Task.WaitAsync(ListLengthHostNames.Deadline);
            try
            {
                if (pending.IsCompleted || !await observer.Invoke(_ => true))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                await queue.ResumeUntil(pending);
                ErlangException? error = null;
                try
                {
                    await invocation;
                }
                catch (ErlangException failure)
                {
                    error = failure;
                }
                if (error is null || error.ExceptionClass != ErlangExceptionClasses.Error || !error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                Term[] frames = Cons.Items(error.StackTraceTerm).ToArray();
                if (frames.Length == 0 || frames[0] is not TupleTerm { Items.Count: 4 } first
                    || !first.Items[0].Equals(Term.A(ErlangModuleNames.Module))
                    || !first.Items[1].Equals(Term.A(ErlangModuleNames.Length))
                    || first.Items[2] is not Cons { Tail: Nil } args
                    || !ReferenceEquals(args.Head, original))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                if (ordset && (frames.Length < 2 || frames[1] is not TupleTerm { Items.Count: 4 } wrapper
                    || !wrapper.Items[0].Equals(Term.A(ListLengthHostNames.Ordsets))
                    || !wrapper.Items[1].Equals(Term.A(ListLengthHostNames.Size))))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
                Term followup = await actor.InvokeAsync(context => runtime.Modules.Call(context, ErlangModuleNames.Module, ErlangModuleNames.Length, Nil.Value).AsTask());
                if (!followup.Equals(Term.I(0)))
                    throw new InvalidOperationException(ListLengthHostNames.Failure);
            }
            finally
            {
                if (!pending.IsCompleted)
                {
                    await observer.Invoke(context =>
                    {
                        runtime.Exit(context, actor.Process.Pid, Term.A(ListLengthHostNames.Kill));

                        return true;
                    });
                    await queue.ResumeUntil(pending);
                }
            }
        }
    }

    private static async Task Boundary(''')
s=s.replace('        bool removal = false\n','        bool removal = false,\n        bool ordset = false\n')
s=s.replace('function is not null ? ListLengthHostNames.Module : ErlangModuleNames.Module,','ordset ? ListLengthHostNames.Ordsets : function is not null ? ListLengthHostNames.Module : ErlangModuleNames.Module,')
s=s.replace('function ?? ErlangModuleNames.Length,','ordset ? ListLengthHostNames.Size : function ?? ErlangModuleNames.Length,')
emit(p,s)
p='tests/Erlang.Tests/ListLengthHostNames.cs'
s=(root/p).read_text().replace('    internal const string Module =', '''    internal const string Ordsets = "ordsets";
    internal const string Size = "size";
    internal const string ImproperTail = "length_late_improper_tail";
    internal const string Module =''')
emit(p,s)
(out/'copy-plan.json').write_text(json.dumps({'Files':files,'ProgramAnchor':'Test("runtime/length-ordinary-progress", Erlang.Tests.ListLengthHostChecks.OrdinaryProgress);','Registrations':[{'Name':'runtime/ordsets-size-progress','Method':'OrdsetSizeProgress'},{'Name':'runtime/ordsets-size-cancellation','Method':'OrdsetSizeCancellation'},{'Name':'runtime/ordsets-size-suspend-resume','Method':'OrdsetSizeSuspendResume'},{'Name':'runtime/length-size-late-improper-original-argument','Method':'LateImproperOriginalArgument'}],'BuildOrTestExecuted':False,'NoMainEdits':True},indent=2)+'\n')
print(json.dumps({'Files':len(files),'BuildOrTestExecuted':False}))
