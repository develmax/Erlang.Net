namespace Erlang.Differential;

internal static class ReceiveWaitCaseSources
{
    public const string QueuedAtom = "-module(receive_timeout_queued_match_atom).\n-export([run/0]).\nrun()->put(timeout_input,invalid),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end.\n";
    public const string InterpretedQueuedAtom = "begin put(timeout_input,invalid),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end end";
    public const string QueuedNegative = "-module(receive_timeout_queued_match_negative).\n-export([run/0]).\nrun()->put(timeout_input,-1),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end.\n";
    public const string InterpretedQueuedNegative = "begin put(timeout_input,-1),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end end";
    public const string QueuedFloat = "-module(receive_timeout_queued_match_float).\n-export([run/0]).\nrun()->put(timeout_input,0.0),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end.\n";
    public const string InterpretedQueuedFloat = "begin put(timeout_input,0.0),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end end";
    public const string QueuedAboveLimit = "-module(receive_timeout_queued_match_above_limit).\n-export([run/0]).\nrun()->put(timeout_input,4294967296),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end.\n";
    public const string InterpretedQueuedAboveLimit = "begin put(timeout_input,4294967296),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end end";
    public const string UnmatchedInvalid = "-module(receive_timeout_unmatched_retained_invalid).\n-export([run/0]).\nrun()->put(timeout_input,invalid),self()!unrelated,T=get(timeout_input),R=try receive wanted->wrong_match after T->wrong_after end catch error:timeout_value->caught end,{R,receive unrelated->preserved after 0->missing end}.\n";
    public const string InterpretedUnmatchedInvalid = "begin put(timeout_input,invalid),self()!unrelated,T=get(timeout_input),R=try receive wanted->wrong_match after T->wrong_after end catch error:timeout_value->caught end,{R,receive unrelated->preserved after 0->missing end} end";
    public const string ExpressionBeforeMatch = "-module(receive_timeout_timeout_expression_before_match).\n-export([run/0]).\nrun()->put(timeout_input,invalid),self()!wanted,R=receive wanted->{matched,get(marker)} after begin put(marker,evaluated),get(timeout_input) end->wrong_after end,{R,get(marker)}.\n";
    public const string InterpretedExpressionBeforeMatch = "begin put(timeout_input,invalid),self()!wanted,R=receive wanted->{matched,get(marker)} after begin put(marker,evaluated),get(timeout_input) end->wrong_after end,{R,get(marker)} end";
    public const string ExpressionError = "-module(receive_timeout_timeout_expression_error_before_match).\n-export([run/0]).\nrun()->self()!wanted,R=try receive wanted->wrong_match after erlang:error(timeout_expression)->wrong_after end catch error:timeout_expression->caught end,{R,receive wanted->preserved after 0->missing end}.\n";
    public const string InterpretedExpressionError = "begin self()!wanted,R=try receive wanted->wrong_match after erlang:error(timeout_expression)->wrong_after end catch error:timeout_expression->caught end,{R,receive wanted->preserved after 0->missing end} end";
    public const string QueuedZero = "-module(receive_timeout_queued_match_zero).\n-export([run/0]).\nrun()->put(timeout_input,0),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end.\n";
    public const string InterpretedQueuedZero = "begin put(timeout_input,0),self()!wanted,T=get(timeout_input),receive wanted->matched after T->wrong_after end end";
}
