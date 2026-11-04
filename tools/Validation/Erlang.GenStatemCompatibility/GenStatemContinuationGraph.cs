using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Erlang;

internal static class GenStatemContinuationGraph
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    internal static int CountMachines(ProcessContext context)
    {
        var signal = (TaskCompletionSource)typeof(Mailbox).GetField(GenStatemRetentionNames.MailboxSignal, Fields)!.GetValue(context.Mailbox)!;
        var pending = new Stack<object>();
        pending.Push(signal.Task);
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        int machines = 0;
        while (pending.TryPop(out object? value))
        {
            if (!visited.Add(value))
            {
                continue;
            }
            if (visited.Count > GenStatemRetentionNames.MaximumContinuationObjects)
            {
                throw new InvalidOperationException(GenStatemRetentionNames.GraphFailure);
            }
            Type type = value.GetType();
            if (type.FullName == GenStatemRetentionNames.MachineType)
            {
                machines++;
                continue;
            }
            if (value is Term or ProcessContext or ProcessRuntime or ExecutionContext || type.IsPrimitive || type.IsEnum || value is string)
            {
                continue;
            }
            if (value is Delegate callback)
            {
                if (callback.Target is { } target)
                {
                    pending.Push(target);
                }
                continue;
            }
            if (value is Array array)
            {
                foreach (object? item in array)
                {
                    if (item is not null)
                    {
                        pending.Push(item);
                    }
                }
                continue;
            }
            if (value is not Task && value is not IList && !type.IsValueType && !typeof(IAsyncStateMachine).IsAssignableFrom(type)
                && !type.Name.Contains(GenStatemRetentionNames.ContinuationTypePart, StringComparison.Ordinal)
                && !type.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                continue;
            }
            for (Type? current = type; current is not null; current = current.BaseType)
            {
                foreach (FieldInfo field in current.GetFields(Fields))
                {
                    if (!field.FieldType.IsPointer && !field.FieldType.IsByRefLike && field.GetValue(value) is { } child)
                    {
                        pending.Push(child);
                    }
                }
            }
        }

        return machines;
    }
}
