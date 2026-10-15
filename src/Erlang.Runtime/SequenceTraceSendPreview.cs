namespace Erlang;

internal readonly record struct SequenceTraceSendPreview(
    SequenceTraceToken? Original,
    ulong Clock,
    ulong LastCount,
    bool SelfTracer,
    SequenceTraceToken? Next
);
