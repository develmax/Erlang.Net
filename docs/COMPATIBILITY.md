# Compatibility checkpoint

Target: OTP-29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation is incomplete. [Matrix](compatibility-matrix.md), [semantic boundaries](semantic-differences.md), [source inventory](otp-inventory.md) and [direct MFA tests](supported-mfas.json) define the current evidence.

Local regression results cover a language/runtime subset, not every OTP export. The independent production execution path is C#/.NET; BEAM is used only by the optional development differential runner. No reference runtime is installed on this host. OTP reference suites and real-node interoperability have not run.

Each implemented function remains Partially compatible until full applicable inputs/errors/side effects and exact-version oracle evidence are available. An implemented process primitive must not be confused with complete process signal/scheduler compatibility. A generated AST emitter must not be described as a complete Erlang compiler.

Machine-readable source defaults are Not started. `supported-mfas.json` is the overlay marking registered MFAs Partially compatible and naming their tests. Source duplicates/conditional exports remain separate from effective release API coverage.

Part 004 adds source map construction, associative/exact updates and subset patterns with guard-expression keys, duplicate/repeated bindings and badmap/badkey checks. These remain Partially compatible; permanent local and generated-source tests establish only their tested boundaries. Map comprehensions and the complete map BIF/guard set are pending.

Part 005 adds erlang:is_map/1, map_size/1, map_get/2 and is_map_key/2 to runtime dispatch and the guard whitelist. Local tests cover exact keys, badmap/badkey reasons, guard failure and alternatives, qualified erlang calls and map-pattern keys. Registered MFAs total 46; the functions remain Partially compatible pending oracle and complete stacktrace evidence.

Part 006 adds bitstring source construction with integer/binary segments, sizes/units and endianness; patterns, float/UTF and string modifiers remain absent. The first remote OTP build failed on a wx-dependent GUI application before comparison. Headless configuration has been repaired; no successful differential result is claimed yet.

Part 007 adds erlang:is_bitstring/1, bit_size/1 and byte_size/1. byte_size accepts partial bitstrings and rounds bits up to a whole byte; invalid size operands raise badarg. Local predicate/size/error/guard/pattern-key tests and generated module checks pass. All 49 registered MFAs remain Partially compatible. The repaired remote oracle workflow is in progress at dc91e8c with 57 cases; no reference result is claimed yet for these new BIFs.
