namespace Erlang.NifNativeBackend;

public static class MutableSequenceToken550CaseNames
{
    internal const int ImmediateValue = 157;
    internal const int BoxedShift = 80;

    public const string Test = "native/sequence550-private-mutable-token-never-certifies-pure-closure";
    internal const string Failure = "A mutable sequence token acquired an immutable closure certificate, lost actual GC roots or leaked its allocation.";
    internal const string RuntimeGateField = "gate";
    internal const string SenderAtom = "mutable_sequence_sender550";
}
