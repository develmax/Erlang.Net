namespace Erlang;

// Actual live pointer cells in this private backend, not ERTS public heap words.
internal readonly record struct ProcessTermGenerationSnapshot508(
    long NurseryPayloadWords,
    long SurvivorPayloadWords,
    long OldPayloadWords,
    long CompletedCollections,
    long ReclaimedPayloadWords
);
