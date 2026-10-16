namespace Erlang.Differential;

public static class NonTailRecursionCaseNames
{
    public const string Prefix = "non-tail-recursion-";
    public const int FiniteDepth = 4096;
    public const int ShallowDepth = 12;
    public const long FiniteChecksum = FiniteDepth * (FiniteDepth + 1L) / 2;
    public const long ShallowChecksum = ShallowDepth * (ShallowDepth + 1L) / 2;
    public const string Entered = "entered";
    public const string Returned = "returned";
    public const string ErrorCaught = "caught_error";
    public const string ThrowCaught = "caught_throw";
    public const string Named = "named-finite-checksum";
    public const string Mutual = "mutual-finite-checksum";
    public const string Anonymous = "anonymous-self-argument-finite-checksum";
    public const string Tail = "tail-accumulator-control";
    public const string Shallow = "shallow-decrement-base-and-checksums";
    public const string Order = "argument-effects-before-and-after-recursive-call";
    public const string Cleanup = "try-after-normal-return-cleanup-order";
    public const string Failure = "try-after-error-and-throw-cleanup-once";
    public const string NamedBody = "walk(0)->0;walk(N)->walk(N-1)+N.";
    public const string MutualBody = "left(0)->0;left(N)->right(N-1)+N. right(0)->0;right(N)->left(N-1)+N.";
    public const string AnonymousBody = "F=fun(Self,0)->0;(Self,N)->Self(Self,N-1)+N end,";
    public const string TailBody = "walk(0,Acc)->Acc;walk(N,Acc)->walk(N-1,Acc+N).";
    public const string OrderBody = "walk(0)->0;walk(N)->combine(mark({entered,N}),walk(N-1),mark({returned,N})). mark(Event)->put(events,[Event|get(events)]),0. combine(_,Result,_)->Result+1.";
    public const string CleanupBody = "walk(0)->0;walk(N)->try walk(N-1)+N after put(cleanups,[N|get(cleanups)]) end.";
    public const string FailureBody = "walk(0,error)->erlang:error(boom);walk(0,throw)->throw(boom);walk(N,Mode)->try walk(N-1,Mode)+N after put(cleanups,[N|get(cleanups)]) end.";
}
