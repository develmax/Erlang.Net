namespace Erlang.Tests;

internal static class QueueBeamFullSmokeNames
{
    internal const string BeamPrefix = "runtime/queue/original-beam/smoke/";
    internal const string CSharpPrefix = "runtime/queue/maintained-csharp/smoke/";
    internal const string EffectsPrefix = "runtime/queue/backend-effects/";
    internal const string Failure = "Queue selected backend failed its retained shared smoke contract.";
    internal const string LongSuffix = "long-reverse-split";
    internal const string CallbackSuffix = "callback-errors";
    internal const string CancellationSuffix = "cancel-callback-owner-cleanup";
    internal const string PredicateInvalidTest = "runtime/lists/predicate-invalid-list-source-frame";
    internal const string InvalidList = "queue_beam_invalid_list";
    internal const string LongTest = "runtime/queue/original-beam/long-reverse-split";
    internal const string Tail = "queue_beam_invalid_tail";
    internal const string CallbackError = "queue_beam_callback_error";
    internal const string CallbackThrow = "queue_beam_callback_throw";
    internal const string CallbackExit = "queue_beam_callback_exit";
    internal const int Exports = 38;
    internal const int LongCells = 8193;
    internal const int SplitIndex = 4096;
}
