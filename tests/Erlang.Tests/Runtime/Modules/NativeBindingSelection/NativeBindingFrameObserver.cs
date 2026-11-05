using System.Linq.Expressions;
using System.Reflection;

namespace Erlang.Tests;

// Test-only observer: preserves frame values and changes no VM ownership/capability state.
internal sealed class NativeBindingFrameObserver : IDisposable
{
    private readonly ProcessContext context;
    private readonly FieldInfo field;
    private readonly object previous;
    internal int Swaps { get; private set; }
    internal Exception? Failure { get; private set; }

    internal NativeBindingFrameObserver(ProcessContext context)
    {
        this.context = context;
        field = typeof(ProcessContext).GetField(NativeBindingSelectionNames.ExecutionFrameField, BindingFlags.Instance | BindingFlags.NonPublic)!;
        previous = field.GetValue(context)!;
        Type slot = previous.GetType();
        Type args = typeof(AsyncLocalValueChangedArgs<>).MakeGenericType(slot.GenericTypeArguments[0]);
        Type action = typeof(Action<>).MakeGenericType(args);
        var parameter = Expression.Parameter(args);
        var callback = Expression.Lambda(
            action,
            Expression.Call(
                Expression.Constant(this),
                typeof(NativeBindingFrameObserver).GetMethod(nameof(Changed), BindingFlags.Instance | BindingFlags.NonPublic)!,
                Expression.Convert(parameter, typeof(object))
            ),
            parameter
        ).Compile();
        object observed = Activator.CreateInstance(slot, callback)!;
        slot.GetProperty(NativeBindingSelectionNames.ValueProperty)!.SetValue(observed, slot.GetProperty(NativeBindingSelectionNames.ValueProperty)!.GetValue(previous));
        field.SetValue(context, observed);
    }

    private void Changed(object change)
    {
        try
        {
            Type type = change.GetType();
            if ((bool)type.GetProperty(NativeBindingSelectionNames.ThreadContextChanged)!.GetValue(change)! || Swaps != 0)
                return;
            object? frame = type.GetProperty(NativeBindingSelectionNames.CurrentValue)!.GetValue(change);
            if (frame is null || (string?)frame.GetType().GetProperty(NativeBindingSelectionNames.FunctionProperty)!.GetValue(frame) != NativeBindingSelectionNames.Helper)
                return;
            Swaps++;
            context.Runtime.Modules.DeleteModule(NativeBindingSelectionNames.Lists);
            context.Runtime.Modules.Register(
                NativeBindingSelectionNames.Lists,
                NativeBindingSelectionNames.Reverse,
                NativeBindingSelectionNames.ReverseArity,
                (caller, _) => caller.Runtime.Modules.Call(
                    caller,
                    NativeBindingSelectionNames.Erlang,
                    NativeBindingSelectionNames.Error,
                    Term.A(NativeBindingSelectionNames.Replacement)
                )
            );
        }
        catch (Exception error)
        {
            Failure = error;
        }
    }

    public void Dispose() => field.SetValue(context, previous);
}
