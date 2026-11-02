using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using Erlang.Compiler;
using static Erlang.Tests.ControlRoot826Names;

namespace Erlang.Tests;

public static class ControlRoot826Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    private static readonly Type Guard = RequiredType(GuardType);

    public static async Task Run()
    {
        await NoBackingClosure192Checks.Run();
        await EmptyLiteralProof200Checks.Run();
        await LiteralCapture397Checks.Run();
        await NativeAppend206Checks.Run();
        await LazyFloatRegister230Checks.Run();
        await PublishedFunctionClosure293Checks.Run();
        await OwnedMapClosure302Checks.Run();
        await MetadataBorrowed316Checks.Run();
        foreach (bool privateProfile in new[] { false, true })
        {
            await using ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create(privateProfile);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                object heap = Get(context, Heap);
                using var frame = (IDisposable)Call(heap, Enter)!;
                int binarySlot = (int)Call(frame, Reserve)!;
                int integerSlot = (int)Call(frame, Reserve)!;
                var portableBinary = new BitString(new byte[BinaryBytes]);
                var identity = new LocalFunctionIdentity(
                    Term.A(ControlRoot826Names.Module),
                    null,
                    0,
                    0,
                    new BitString(new byte[16])
                );
                int invoked = 0;
                var portableFunction = new FunctionTerm(
                    identity,
                    0,
                    [Term.Tuple(Term.A(Tag), portableBinary)],
                    (_, _) =>
                    {
                        invoked++;

                        return ValueTask.FromResult<Term>(Nil.Value);
                    }
                );
                Term detached = Term.Tuple(
                    new MapTerm([new(Term.A(Tag), Term.List(portableBinary, portableFunction))]),
                    runtime.MakeReference(),
                    context.Self
                );
                Require(ReferenceEquals(Get(CaptureGraph(detached), Value), detached));
                CheckConstructors(context, detached);
                Require(invoked == 0);
                RejectUnknown(() => CaptureGraph(new ControlRoot826UnknownTerm()));
                RejectUnknown(() => CaptureGraph(new ControlRoot826UnknownReference()));

                var nativeBinary = (BitString)Call(
                    frame,
                    Adopt,
                    binarySlot,
                    portableBinary
                )!;
                var nativeInteger = (Integer)Call(
                    frame,
                    Adopt,
                    integerSlot,
                    new Integer(BigInteger.One << BigIntegerShift)
                )!;
                Require(ReadStorage(nativeBinary) is not null && ReadStorage(nativeInteger) is not null);
                Term nestedBinary = Term.Tuple(new MapTerm([new(Term.A(Tag), Term.List(nativeBinary))]));
                RejectNative(() => CaptureGraph(nestedBinary));
                RejectNative(() => NewCode(context, nestedBinary));
                RejectNative(() => NewTask(context.Self, Term.Tuple(Term.A(TestOperation), nestedBinary)));
                RejectNative(() => NewTask(Term.Tuple(nativeInteger), Term.Tuple(Term.A(TestOperation), Nil.Value)));

                BitString slice = nativeBinary.SliceBits(1, BinaryBytes * 8 - 1);
                Require(ReadStorage(slice) is null && ReferenceEquals(Get(slice, Origin), nativeBinary));
                RejectNative(() => CaptureGraph(Term.List(slice)));
                var identityWithNativeBinary = identity with { Unique = nativeBinary };
                var metadataFunction = new FunctionTerm(
                    identityWithNativeBinary,
                    0,
                    [],
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
                // OTP stores UID bytes by value, independently of process cells.
                // The original physical binary/origin rejections above remain.
                Require(!ReferenceEquals(identityWithNativeBinary.Unique, nativeBinary)
                    && ReadStorage(identityWithNativeBinary.Unique) is null
                    && identityWithNativeBinary.Unique.BitLength == nativeBinary.BitLength
                    && identityWithNativeBinary.Unique.ToArray().SequenceEqual(nativeBinary.ToArray()));
                Term metadataGraph = Term.Tuple(metadataFunction);
                Require(ReferenceEquals(Get(CaptureGraph(metadataGraph), Value), metadataGraph));
                var resourceEnvironmentFunction = new FunctionTerm(
                    identityWithNativeBinary,
                    0,
                    [nativeBinary],
                    (_, _) => ValueTask.FromResult<Term>(Nil.Value)
                );
                RejectNative(() => CaptureGraph(Term.Tuple(resourceEnvironmentFunction)));
                await CheckSuspensionBeforeMutation(context, nestedBinary);
                await context.ExecutionCheckpoint();

                // Public Source evaluation creates an authenticated canonical
                // closure and the genuine dictionary BIF roots its actual cells.
                _ = await Execution.EvaluateAsync(
                    new Expr.Call(
                        ErlangModule,
                        Put,
                        [
                        new Expr.Literal(Term.A(Key)),
                        new Expr.Fun([new Clause([], null, new Expr.Literal(Term.I(FunctionValue)))])
                    ]
                    ),
                    context
                );
                await context.ExecutionCheckpoint();
                FunctionTerm nativeFunction = ReadDictionaryFunction(context);
                Require(Get(nativeFunction, FunctionStorage) is not null);
                RejectNative(() => CaptureGraph(Term.Tuple(nativeFunction)));
                _ = await Execution.EvaluateAsync(new Expr.Call(ErlangModule, Erase, [new Expr.Literal(Term.A(Key))]), context);
                await context.ExecutionCheckpoint();

                Call(frame, Clear, binarySlot);
                Call(frame, Clear, integerSlot);
                CollectNow(heap);
                RequireStale(() => _ = nativeInteger.Value);
                RequireStale(() => _ = nativeBinary.BitLength);
                RequireStale(() => _ = nativeFunction.Environment.Count);
                // Exact guard origin distinguishes ownership refusal from a
                // lower stale-cell getter exception: Storage must win first.
                RejectNative(() => CaptureGraph(Term.Tuple(nativeInteger)));
                RejectNative(() => CaptureGraph(Term.List(slice)));
                RejectNative(() => CaptureGraph(Term.Tuple(nativeFunction)));
                await CheckIndependentResource(context);

                return true;
            });
        }
    }

    private static void CheckConstructors(ProcessContext context, Term detached)
    {
        object request = NewCode(context, detached);
        Require(ReferenceEquals(Get(request, Id), detached));
        var operation = Term.Tuple(Term.A(TestOperation), detached);
        object task = NewTask(context.Self, operation);
        Require(ReferenceEquals(Get(task, Requester), context.Self));
        Require(ReferenceEquals(Get(task, Operation), operation) && ReferenceEquals(Get(task, Id), detached));
        Call(task, Clear);
        Require(GetNullable(task, Requester) is null && GetNullable(task, Operation) is null);
    }

    private static async Task CheckSuspensionBeforeMutation(ProcessContext context, Term unsafeTag)
    {
        object runtimeGate = Field(context.Runtime, RuntimeGate);
        object suspensions = Get(context.Runtime, Suspensions);
        int before;
        bool requested;
        lock (runtimeGate)
        {
            before = ((IDictionary)Field(suspensions, Targets)).Count;
            requested = (bool)Field(context, SuspensionRequested);
        }
        bool refused = false;
        try
        {
            object options = New(
                OptionsType,
                true,
                false,
                unsafeTag
            );
            try
            {
                await (ValueTask<Term>)Call(
                    suspensions,
                    Suspend,
                    context,
                    context.Self,
                    options,
                    false
                )!;
            }
            catch (InvalidOperationException failure) when (failure.TargetSite?.DeclaringType == Guard)
            {
                refused = true;
            }
            lock (runtimeGate)
                Require(refused && ((IDictionary)Field(suspensions, Targets)).Count == before
                    && (bool)Field(context, SuspensionRequested) == requested);
        }
        finally
        {
            // If a regression admitted the self-target, remove it synchronously
            // before any actor checkpoint; never leave the actor suspended.
            lock (runtimeGate)
                if (((IDictionary)Field(suspensions, Targets)).Count != before)
                    Call(
                        suspensions,
                        Resume,
                        context,
                        context.Self
                    );
        }
    }

    private static async Task CheckIndependentResource(ProcessContext context)
    {
        ProcessRuntime runtime = context.Runtime;
        var provider = NativeResource524Access.Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = NativeResource524Access.Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        bool released = false;
        using var host = new NativeTermHostScope524(runtime);
        try
        {
            BitString binary = NativeResource524Access.Binary(runtime, payload);
            Require(ReadStorage(binary) is null);
            _ = CallStatic(
                typeof(NativeTermHostScope524),
                NativeAdmission,
                runtime,
                binary
            );
            Call(payload, ReleaseNative);
            released = true;
            long before = ResourceReferences(payload);
            Require(before > 0 && destroyed == 0);
            Term graph = Term.Tuple(new MapTerm([new(Term.A(Tag), binary)]));
            Require(ReferenceEquals(Get(CaptureGraph(graph), Value), graph));
            CheckConstructors(context, graph);
            Require(ResourceReferences(payload) == before && destroyed == 0);
        }
        finally
        {
            if (!released) Call(payload, ReleaseNative);
            host.Dispose();
            await ((Task)Get(payload, Retired)).WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
            await ((Task)Call(provider.Bindings, Retire)!).WaitAsync(TimeSpan.FromSeconds(DeadlineSeconds));
            GC.KeepAlive(destructor);
        }
        Require(destroyed == 1 && provider.Owner.Disposed);
    }

    private static long ResourceReferences(object payload)
    {
        object row = Call(payload, NativeInfo, (nint)Field(payload, NativeAllocation))!;

        return (long)Get(row, NativeReferences);
    }

    private static FunctionTerm ReadDictionaryFunction(ProcessContext context)
    {
        object dictionary = context.Dictionary;
        lock (Field(dictionary, DictionaryGate))
            return (FunctionTerm)((Dictionary<Term, Term>)Field(dictionary, DictionaryData))[Term.A(Key)];
    }

    private static object NewCode(ProcessContext context, Term id)
        => New(
            CodeType,
            context,
            id,
            ControlRoot826Names.Module,
            null
        );

    private static object NewTask(Term requester, TupleTerm operation)
    {
        Type type = RequiredType(TaskType);
        Type priority = type.GetConstructors(Constructors).Single().GetParameters()[3].ParameterType;

        return New(
            TaskType,
            requester,
            operation,
            TestOperation,
            Enum.Parse(priority, NormalPriority)
        );
    }

    private static object CaptureGraph(Term value) => CallStatic(Guard, Capture, value)!;

    private static object? ReadStorage(Term value) => CallStatic(RequiredType(HeapType), Storage, value);

    private static void CollectNow(object heap)
    {
        lock (Get(heap, HeapGate))
            Call(
                heap,
                Collect,
                true,
                (ushort)0
            );
    }

    private static void RejectNative(Action action)
    {
        bool refused = false;
        try
        {
            action();
        }
        catch (InvalidOperationException failure) when (failure.TargetSite?.DeclaringType == Guard)
        {
            refused = true;
        }
        Require(refused);
    }

    private static void RejectUnknown(Action action)
    {
        bool refused = false;
        try
        {
            action();
        }
        catch (NotSupportedException failure) when (failure.TargetSite?.DeclaringType == Guard)
        {
            refused = true;
        }
        Require(refused);
    }

    private static void RequireStale(Action read)
    {
        bool stale = false;
        try
        {
            read();
        }
        catch (Exception failure) when (failure is InvalidOperationException)
        {
            stale = true;
        }
        Require(stale);
    }

    private static Type RequiredType(string name) => typeof(ProcessRuntime).Assembly.GetType(name, true)!;

    private static object Get(object target, string name) => GetNullable(target, name)!;

    private static object? GetNullable(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target);

    private static object Field(object target, string name) => target.GetType().GetField(name, Members)!.GetValue(target)!;

    private static object New(string name, params object?[] arguments)
    {
        try
        {
            return Activator.CreateInstance(
                RequiredType(name),
                Constructors,
                null,
                arguments,
                null
            )!;
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }

    private static object? Call(object target, string name, params object?[] arguments)
        => Invoke(FindMethod(target.GetType(), name, arguments.Length), target, arguments);

    private static object? CallStatic(Type type, string name, params object?[] arguments)
        => Invoke(FindMethod(type, name, arguments.Length), null, arguments);

    private static MethodInfo FindMethod(Type type, string name, int arity)
        => type.GetMethods(Members).Single(method => method.Name == name && method.GetParameters().Length == arity);

    private static object? Invoke(MethodInfo method, object? target, object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
