# Part 2004 compiler fixture publication check

The checked entry projection fixture passed a collection expression directly to a ReadOnlyMemory<byte> parameter. The fixture now accepts the array and converts it to the same memory value for instruction decoding. The six affected compiler fixtures pass on the .NET 10.0.400 SDK using the Part 2003 compiler.

Whitespace-only lines in the Erlang fixture literals were trimmed. Roslyn confirmed identical token values before and after trimming, including the Erlang source strings. The [initial build diagnostic](initial-fixture-build.stdout), [isolated probe dependency diagnostic](isolated-dependency-check.stdout) and [six passing checks](focused.stdout) are retained. The complete test-project build was started separately. Full execution, uncached corpus acceptance and readiness are still open. The compiler source and release BEAM count are unchanged by this fixture-only part.
