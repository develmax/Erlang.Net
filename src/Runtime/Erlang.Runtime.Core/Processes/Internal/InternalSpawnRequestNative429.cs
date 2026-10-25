namespace Erlang;

internal static class InternalSpawnRequestNative429
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || module != InternalSpawnRequestNames429.Module || function != InternalSpawnRequestNames429.Function || arity != 4)
            return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != 4)
                throw new InvalidOperationException(InternalSpawnRequestNames429.Foreign);
            owner.RequireInternalPortActor416(context);
            // The internal protocol returns malformed MFA/options statuses as values.
            // Public erlang wrappers own their distinct exception/error-info boundary.
            if (arguments[0] is not Atom targetModule || arguments[1] is not Atom targetFunction)
                return ValueTask.FromResult<Term>(Term.A(InternalSpawnRequestNames429.BadArgument));
            var values = new List<Term>();
            Term tail = arguments[2];
            while (tail is Cons item)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                values.Add(item.Head);
                tail = item.Tail;
            }
            if (tail is not Nil)
                return ValueTask.FromResult<Term>(Term.A(InternalSpawnRequestNames429.BadArgument));
            var options = new List<Term>();
            tail = arguments[3];
            while (tail is Cons optionCell)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                options.Add(optionCell.Head);
                tail = optionCell.Tail;
            }
            if (tail is not Nil)
                return ValueTask.FromResult<Term>(Term.A(InternalSpawnRequestNames429.BadOptions));
            // This backend has no process heap allocation policy. Do not silently turn
            // a valid unimplemented source option into an asynchronous badopt.
            // All heap-option value/interaction partitions remain explicit HOST scope.
            foreach (Term option in options)
                if (option is TupleTerm { Items: [Atom { Name: InternalSpawnRequestNames429.MinimumHeap or InternalSpawnRequestNames429.MinimumBinaryHeap or InternalSpawnRequestNames429.MaximumHeap }, _] })
                    throw new NotSupportedException(InternalSpawnRequestNames429.HeapPartition);
            ProcessSpawnRequestOptions parsed = ProcessSpawnRequestOptions.Parse(options, false, owner.ActorDispatchCapacity);
            Term? failure = parsed.BadOptions ? Term.A(InternalSpawnRequestNames429.BadOptions) : null;
            Term[] copiedArguments = values.ToArray();
            SequenceSpawnDescription description = SequenceSpawnDescription.Module(
                targetModule,
                targetFunction,
                arguments[2],
                arguments[3]
            )
                with
            { ReplyTag = parsed.Tag };

            return ValueTask.FromResult<Term>(owner.SpawnRequest(
                context,
                child => child.Runtime.Modules.Call(
                    child,
                    targetModule.Name,
                    targetFunction.Name,
                    copiedArguments
                ),
                parsed,
                failure,
                description
            ));
        };

        return true;
    }
}
