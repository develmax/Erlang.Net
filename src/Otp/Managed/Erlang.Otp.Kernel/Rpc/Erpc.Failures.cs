namespace Erlang.Otp;

internal static partial class Erpc
{
    private static (string Function, int Arity, int Line) FailureLocation(string function, IReadOnlyList<Term> arguments, ErlangException error)
    {
        int arity = arguments.Count;
        int line;
        switch (function)
        {
            case ErpcNames.Call:
                if (arity <= 3 && arguments[1] is not FunctionTerm { Arity: 0 })
                    return (function, 3, ErpcSourceLines.CallFunction);

                return (function, 5, ErpcSourceLines.CallArgument);
            case ErpcNames.Multicall:
                return (function, arity <= 3 && arguments[1] is not FunctionTerm { Arity: 0 } ? 3 : 5,
                    arity <= 3 && arguments[1] is not FunctionTerm { Arity: 0 } ? ErpcSourceLines.MulticallFunction : ErpcSourceLines.Multicall);
            case ErpcNames.CastPublic:
                return (function, 4, ErpcSourceLines.Cast);
            case ErpcNames.Multicast:
                return (function, 4, ErpcSourceLines.Multicast);
            case ErpcNames.SendRequest:
                line = arity == 2 ? ErpcSourceLines.SendFunction : arity == 6 ? ErpcSourceLines.SendCollection : ErpcSourceLines.SendArgument;
                break;
            case ErpcNames.ReceiveResponse:
                line = arity == 1 ? ErpcSourceLines.ReceiveOne : arity == 2 ? ErpcSourceLines.ReceiveTwo : ErpcSourceLines.ReceiveCollection;
                break;
            case ErpcNames.WaitResponse:
                line = arity == 1 ? ErpcSourceLines.WaitOne : arity == 2 ? ErpcSourceLines.WaitTwo : ErpcSourceLines.WaitCollection;
                break;
            case ErpcNames.CheckResponse:
                line = arity == 2 ? ErpcSourceLines.CheckTwo : ErpcSourceLines.CheckCollection;
                break;
            case ErpcNames.RequestIdsSize:
                line = ErpcSourceLines.RequestSize;
                break;
            case ErpcNames.RequestIdsAdd:
                line = arguments[0] is Cons { Tail: ReferenceTerm request } && arguments[2] is MapTerm map && map.TryGet(request, out _) ? ErpcSourceLines.RequestDuplicate : ErpcSourceLines.RequestAdd;
                break;
            case ErpcNames.RequestIdsToList:
                line = arguments[0] is MapTerm ? ErpcSourceLines.RequestListContents : ErpcSourceLines.RequestList;
                break;
            case ErpcNames.Execute:
                return (function, arity, ErpcSourceLines.ExecuteExit);
            case ErpcNames.Cast:
                line = error.Reason is TupleTerm { Items: [Atom { Name: ErpcNames.Module }, _] } ? ErpcSourceLines.CastArgument : ErpcSourceLines.CastError;
                break;
            default:
                return (function, arity, ErpcSourceLines.ResultInvalid);
        }

        return (function, arity, line);
    }

    private static async ValueTask<Term> NormalizeTimeout(
        ProcessContext context,
        Term timeout,
        string function,
        int arity
    )
    {
        try
        {
            return await ErpcTimeout.Normalize(context, timeout);
        }
        catch (ErlangException error)
        {
            int line = timeout is TupleTerm { Items: [Atom { Name: ErpcNames.Abs }, Integer] } ? ErpcSourceLines.AbsoluteTimeout : ErpcSourceLines.Timeout;

            throw ErpcSource.Capture(
                context,
                error,
                function,
                arity,
                line
            );
        }
    }
}
