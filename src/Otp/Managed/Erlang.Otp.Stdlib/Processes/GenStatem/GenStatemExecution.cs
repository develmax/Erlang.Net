namespace Erlang;

internal static class GenStatemExecution
{
    private static readonly AsyncLocal<ProcessContext?> Active = new();

    internal static bool IsActive(ProcessContext context) => ReferenceEquals(Active.Value, context);

    internal static async ValueTask<Term> Run(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        ProcessContext? previous = Active.Value;
        Active.Value = context;
        bool entering = true;
        try
        {
            while (true)
            {
                try
                {
                    ValueTask<Term> operation = entering
                        ? GenStatemModule.InvokeCore(context, function, arguments)
                        : GenStatemModule.Call(
                            context,
                            GenStatemDependencyNames.Sys,
                            GenStatemFunctionNames.HandleSystemMsg,
                            arguments.ToArray()
                        );
                    arguments = Array.Empty<Term>();

                    return await operation;
                }
                catch (GenStatemSystemTransfer transfer) when (ReferenceEquals(transfer.Context, context))
                {
                    // Preserve the proc_lib exception handler outside this driver.
                    arguments = transfer.Arguments;
                    entering = false;
                }
            }
        }
        finally
        {
            Active.Value = previous;
        }
    }
}
