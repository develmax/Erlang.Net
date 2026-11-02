using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class BeamLocation146Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Assembly Core = typeof(ProcessRuntime).Assembly;

    internal static async Task Run()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(runtime);
        Term a = Location(BeamLocation146Names.FileA, BeamLocation146Names.LineA), b = Location(BeamLocation146Names.FileB, BeamLocation146Names.LineB);
        Term? foreign = null;
        IDisposable? foreignFrame = null;
        try
        {
            await other.InvokeAsync(context =>
 {
     object heap = Get(context, BeamLocation146Names.HeapProperty);
     foreignFrame = (IDisposable)Call(heap, BeamLocation146Names.EnterMethod)!;
     int slot = (int)Call(foreignFrame, BeamLocation146Names.ReserveMethod)!;
     foreign = (Term)Call(
         foreignFrame,
         BeamLocation146Names.AdoptMethod,
         slot,
         a
     )!;

     return Task.FromResult<Term>(Nil.Value);
 });
            await actor.InvokeAsync(context =>
 {
     object heap = Get(context, BeamLocation146Names.HeapProperty);
     object target = Activator.CreateInstance(
         Core.GetType(BeamLocation146Names.LinkedFunctionType)!,
         Members,
         null,
         new object[] { BeamLocation146Names.Module, BeamLocation146Names.Function, BeamLocation146Names.TargetArity, BeamLocation146Names.InfoLabel, BeamLocation146Names.EntryLabel, BeamLocation146Names.FirstInstruction, BeamLocation146Names.EndInstruction },
         null
     )!;
     var scope = (IDisposable)Activator.CreateInstance(
         Core.GetType(BeamLocation146Names.FunctionScopeType)!,
         Members,
         null,
         new object[] { context, target, Array.Empty<Term>() },
         null
     )!;
     Term? actualA = null;
     Term? detachedTrace = null;
     try
     {
         Call(scope, BeamLocation146Names.SetLocationMethod, a);
         object frame = Field(scope, BeamLocation146Names.FrameField)!;
         var slots = (IDictionary)Field(scope, BeamLocation146Names.SlotsField)!;
         Require(slots.Count == BeamLocation146Names.OneEntry);
         actualA = (Term)Call(frame, BeamLocation146Names.ReadMethod, slots[a]!)!;
         Require(!ReferenceEquals(actualA, a) && actualA.Equals(a));
         Require(ReferenceEquals(Get(Call(context, BeamLocation146Names.ExecutionChainMethod)!, BeamLocation146Names.SourceLocationProperty), actualA));
         using (var inner = (IDisposable)Activator.CreateInstance(
             Core.GetType(BeamLocation146Names.FunctionScopeType)!,
             Members,
             null,
             new object[] { context, target, Array.Empty<Term>() },
             null
         )!)
         {
             Call(inner, BeamLocation146Names.SetLocationMethod, a);
             Require(ReferenceEquals(Field(inner, BeamLocation146Names.FrameField), frame));
             Require(ReferenceEquals(Field(inner, BeamLocation146Names.SlotsField), slots));
             Require(ReferenceEquals(Get(Call(context, BeamLocation146Names.ExecutionChainMethod)!, BeamLocation146Names.SourceLocationProperty), actualA));
             Call(
                 heap,
                 BeamLocation146Names.CollectMethod,
                 true,
                 BeamLocation146Names.MajorGeneration
             );
             Require(actualA.Equals(a) && slots.Count == BeamLocation146Names.OneEntry);
         }
         Call(
             heap,
             BeamLocation146Names.CollectMethod,
             true,
             BeamLocation146Names.MajorGeneration
         );
         Require(actualA.Equals(a));
         Require(ReferenceEquals(Get(Call(context, BeamLocation146Names.ExecutionChainMethod)!, BeamLocation146Names.SourceLocationProperty), actualA));
         Call(scope, BeamLocation146Names.SetLocationMethod, b);
         Require(slots.Count == BeamLocation146Names.TwoEntries);
         Call(
             heap,
             BeamLocation146Names.CollectMethod,
             true,
             BeamLocation146Names.MajorGeneration
         );
         Require(actualA.Equals(a));
         Call(scope, BeamLocation146Names.SetLocationMethod, a);
         Require(slots.Count == BeamLocation146Names.TwoEntries && ReferenceEquals(Call(frame, BeamLocation146Names.ReadMethod, slots[a]!), actualA));
         Require(ReferenceEquals(Get(Call(context, BeamLocation146Names.ExecutionChainMethod)!, BeamLocation146Names.SourceLocationProperty), actualA));
         // The original incoming-reference short circuit must not create another entry.
         Call(scope, BeamLocation146Names.SetLocationMethod, a);
         Require(slots.Count == BeamLocation146Names.TwoEntries);
         bool rejected = false;
         try
         {
             Call(scope, BeamLocation146Names.SetLocationMethod, foreign!);
         }
         catch (InvalidOperationException foreignError) when (foreignError.Message == ModuleLiteral801Names.Foreign)
         {
             rejected = true;
         }
         Require(rejected && slots.Count == BeamLocation146Names.TwoEntries);
         Call(scope, BeamLocation146Names.SetLocationMethod, a);
         Require(ReferenceEquals(Get(Call(context, BeamLocation146Names.ExecutionChainMethod)!, BeamLocation146Names.SourceLocationProperty), actualA));
         var error = new ErlangException(Term.A(BeamLocation146Names.Reason));
         object chain = Call(context, BeamLocation146Names.ExecutionChainMethod)!;
         var captured = (ErlangException)CallStatic(
             Core.GetType(BeamLocation146Names.ExceptionCaptureType)!,
             BeamLocation146Names.CaptureSourceMethod,
             context,
             error,
             chain,
             null,
             false
         )!;
         Require(ReferenceEquals(captured.Reason, error.Reason));
         detachedTrace = (Term)Call(
             heap,
             BeamLocation146Names.ExportMethod,
             frame,
             captured.StackTraceTerm
         )!;
         Term expected = Term.List(Term.Tuple(
             Term.A(BeamLocation146Names.Module),
             Term.A(BeamLocation146Names.Function),
             Term.I(BeamLocation146Names.TraceArity),
             a
         ));
         Require(detachedTrace.Equals(expected));
     }
     finally
     {
         scope.Dispose();
     }
     Call(
         heap,
         BeamLocation146Names.CollectMethod,
         true,
         BeamLocation146Names.MajorGeneration
     );
     Require(Field(context, BeamLocation146Names.ContextCacheField) is null);
     bool stale = false;
     try
     {
         _ = Cons.Items(actualA!).ToArray();
     }
     catch (ObjectDisposedException)
     {
         stale = true;
     }
     Require(stale);
     Require(detachedTrace!.Equals(Term.List(Term.Tuple(
         Term.A(BeamLocation146Names.Module),
         Term.A(BeamLocation146Names.Function),
         Term.I(BeamLocation146Names.TraceArity),
         a
     ))));

     return Task.FromResult<Term>(Nil.Value);
 });
        }
        finally
        {
            foreignFrame?.Dispose();
        }
        await using var plainRuntime = OwnedGuest506RuntimeFactory.Create(false);
        await using var plainActor = new RuntimeNodeFoundationHostActor(plainRuntime);
        await plainActor.InvokeAsync(context =>
 {
     object target = Activator.CreateInstance(
         Core.GetType(BeamLocation146Names.LinkedFunctionType)!,
         Members,
         null,
         new object[] { BeamLocation146Names.Module, BeamLocation146Names.Function, BeamLocation146Names.TargetArity, BeamLocation146Names.InfoLabel, BeamLocation146Names.EntryLabel, BeamLocation146Names.FirstInstruction, BeamLocation146Names.EndInstruction },
         null
     )!;
     using var scope = (IDisposable)Activator.CreateInstance(
         Core.GetType(BeamLocation146Names.FunctionScopeType)!,
         Members,
         null,
         new object[] { context, target, Array.Empty<Term>() },
         null
     )!;
     Call(scope, BeamLocation146Names.SetLocationMethod, a);
     Require(Field(scope, BeamLocation146Names.FrameField) is null && Field(scope, BeamLocation146Names.SlotsField) is null);
     Require(ReferenceEquals(Get(Call(context, BeamLocation146Names.ExecutionChainMethod)!, BeamLocation146Names.SourceLocationProperty), a));

     return Task.FromResult<Term>(Nil.Value);
 });
    }

    private static Term Location(string file, int line) => Term.List(
        Term.Tuple(Term.A(BeamLocation146Names.FileAtom), Cons.From(file.Select(character => Term.I(character)))),
        Term.Tuple(Term.A(BeamLocation146Names.LineAtom), Term.I(line))
    );

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Field(object value, string name) => value.GetType().GetField(name, Members)!.GetValue(value);

    private static object? Call(object value, string name, params object?[] arguments) => Invoke(
        value.GetType(),
        value,
        name,
        arguments
    );

    private static object? CallStatic(Type type, string name, params object?[] arguments) => Invoke(
        type,
        null,
        name,
        arguments
    );

    private static object? Invoke(
        Type type,
        object? value,
        string name,
        object?[] arguments
    )
    {
        try
        {
            return type.GetMethods(Members).Single(method => method.Name == name && method.GetParameters().Length == arguments.Length).Invoke(value, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(BeamLocation146Names.Failure);
    }
}
