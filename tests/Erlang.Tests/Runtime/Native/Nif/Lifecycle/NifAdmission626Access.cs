using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal static class NifAdmission626Access
{
    private static readonly Assembly Runtime = typeof(ProcessRuntime).Assembly;
    private static readonly Type Gate = Runtime.GetType(NifAdmission626Names.GateType, true)!;
    private static readonly Type Permit = Runtime.GetType(NifAdmission626Names.PermitType, true)!;
    private static readonly object Sync = Gate.GetField(NifAdmission626Names.Gate, NifAdmission626Names.Statics)!.GetValue(null)!;
    private static readonly FieldInfo Active = Gate.GetFields(NifAdmission626Names.Statics).Single(field => field.FieldType == Permit);

    internal static ProcessContext? Actor => (ProcessContext?)Runtime.GetType(NifAdmission626Names.TurnType, true)!
        .GetProperty(NifAdmission626Names.CurrentActor, NifAdmission626Names.Statics)!.GetValue(null);

    internal static object? Owner()
    {
        lock (Sync) return Active.GetValue(null);
    }

    internal static string State(object permit)
    {
        lock (Sync) return permit.GetType().GetField(NifAdmission626Names.State, NifAdmission626Names.Members)!.GetValue(permit)!.ToString()!;
    }

    internal static int Waiting => (int)Gate.GetProperty(NifAdmission626Names.Waiting, NifAdmission626Names.Statics)!.GetValue(null)!;

    internal static NifAdmission626Awaiter Acquire(CancellationToken cancellation, Action result)
    {
        MethodInfo method = Gate.GetMethod(NifAdmission626Names.Acquire, NifAdmission626Names.Statics)!;
        var adapter = new NifAdmission626Awaiter(method.ReturnType, result);
        object?[] arguments = [cancellation];
        adapter.Bind(Invoke(method, null, arguments)!);

        return adapter;
    }

    internal static NifAdmission626Awaiter Barrier(ProcessContext context, Action result)
    {
        MethodInfo method = typeof(ProcessRuntime).GetMethod(NifAdmission626Names.AcquireBarrier, NifAdmission626Names.Members)!;
        var adapter = new NifAdmission626Awaiter(method.ReturnType, result);
        object?[] arguments = [context];
        adapter.Bind(Invoke(method, context.Runtime, arguments)!);

        return adapter;
    }

    internal static Task Ready(NifAdmission626Awaiter admission)
        => (Task)admission.Raw.GetType().GetField(NifAdmission626Names.Ready, NifAdmission626Names.Members)!.GetValue(admission.Raw)!;

    // Preallocate the scope box and compiled store BEFORE acquiring any permit.
    // The box receives only the genuine returned Scope, never invented fields.
    internal static (object Box, Action<object, object> Enter) PrepareScope()
    {
        MethodInfo enter = Permit.GetMethod(NifAdmission626Names.EnterScope, NifAdmission626Names.Members)!;
        Type scope = enter.ReturnType;
        object box = Activator.CreateInstance(scope)!;
        var method = new DynamicMethod(
            NifAdmission626Names.ScopeWriter,
            typeof(void),
            [typeof(object), typeof(object)],
            typeof(ProcessRuntime).Module,
            true
        );
        ILGenerator code = method.GetILGenerator();
        code.Emit(OpCodes.Ldarg_1);
        code.Emit(OpCodes.Unbox, scope);
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Castclass, Permit);
        code.Emit(OpCodes.Call, enter);
        code.Emit(OpCodes.Stobj, scope);
        code.Emit(OpCodes.Ret);

        return (box, method.CreateDelegate<Action<object, object>>());
    }

    private static object? Invoke(MethodInfo method, object? target, object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } failure)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }
}
