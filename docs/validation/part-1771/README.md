# Part 1771

Test the shared tagged message and its literal fields in registers. Use separate stack frames for the lookup and deletion paths. The unconditional branch returns without allocating a frame. The shape recognizer uses tags, fields, keys and call targets from the source.

The C# compiler accepted all 1,366 Windows OTP 29.1.1 sources and emitted 1,366 files. 399 images match byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepts both images in all 967 differing pairs.

Only logger_handler_watcher changed. Its handle_info/2 now matches the release instructions and the Code chunk is 19 bytes closer.

967 differing images, 0 with the same Code payload and 0 with all chunk payloads equal. 0 differing images have identical instruction bytes after removing the Code header.

The tagged lookup fixture passed 15 assertions on the previous compiler, the new compiler and OTP. The new image and OTP test the tagged entry before allocating stack frames of three and one slots for the two branches. The previous image allocates four slots at entry. The release fixture for logger_handler_watcher verifies the whole installed image. All 11 targeted tests and seven BEAM cases passed. The previous compiler failed the new release regression.

The compiler block passed 3722 of 3830 tests. The same 108 failed with part 1770. The full runtime suite and readiness regeneration were outside this run.

~~~powershell
$env:ERLANG_BEAM_CORPUS_SAVE_DIFFERENCES = '1'
dotnet artifacts/beam-backend/part1771-tagged-lookup-final-tool/Erlang.BeamCorpus.dll $otp artifacts/beam-backend/otp-corpus-part1771-tagged-lookup-final-pinned.json artifacts/beam-backend/part929-ordered-headers artifacts/beam-backend/part1058-source-overlay
~~~

Extend register-only entry tests to functions with a single tagged clause and an unconditional default. That is the largest classified group with a default, with 361 functions across 157 modules.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Helper comparison](helper-delta.txt) · [Normalized instructions](normalized-functions.txt) · [Differences by BEAM chunk](chunk-rank.txt) · [First instruction differences](first.txt) · [Tagged-tuple entry probe](tagged-tuple-entry-probe.txt) · [Filter rank](filter-rank.txt) · [Missing function identities](function-identities.txt) · [Source comprehension shapes](lc-source-shapes.txt) · [Independent comprehensions](multiple-list-comprehensions.txt) · [Tagged entry groups](tagged-entry.txt) · [Helper callers](helper-callers.txt) · [Release comparison](logger-release.json) · [Fixture hashes](release-fixture.sha256) · [Fixture](beam_tagged_record_lookup.erl) · [Fixture instructions](fixture-functions.txt) · [Fixture execution](fixture-behavior.txt) · [Entry opcodes and frames](entry.txt) · [Focused tests](focused.txt) · [Previous compiler](negative.txt) · [Compiler block](compiler-block-summary.txt)
