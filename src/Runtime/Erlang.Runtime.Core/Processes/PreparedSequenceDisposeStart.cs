namespace Erlang;

internal readonly record struct PreparedSequenceDisposeStart(Task Source, Task Native, ValueTask Core);
