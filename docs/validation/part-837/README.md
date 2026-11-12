# Part837 Complete DTLS socket image emission

The managed compiler emits the listener child startup, dynamic ownership transfer, registration and socket record construction. It also emits constant option dispatch for the default IP and writes debug forms for receive clauses with and without a timeout.

The deterministic listener fixture matches OTP 29.1.1 byte for byte. It is in the permanent regression and can be rebuilt with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_listener_ownership_probe.erl`. The OTP probe compares argument and event order, rejected child startup, ownership failure and default addresses. [Fixture behavior](behavior.txt) and [probe](behavior.eval).

The full dtls_socket image now builds and loads in pinned OTP. The generated image is 24,284 bytes and the Windows release image is 23,548 bytes. The full module probe compares selected socket construction, peer address and option handling. [Image hashes](oracle.txt), [module probe](module-behavior.eval) and [results](module-behavior.txt).

After normalizing instruction label offsets, 25 of the 30 functions match. The remaining five are listen/2, check_active_n/2, emulated_active_option/2, emulated_options/3 and validate_inet_option/2. Dbgi, Docs, CInf, LitT, Meta and StrT match. AtU8, Code, ImpT, ExpT, LocT, Attr, Line and Type differ. [Function and loader results](module-comparison.txt), [section comparison](chunks.txt) and [remaining instructions](next-code-differences.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part837-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part837-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 1 Different, 195 SourceRejected and 1,079 Unsupported. Only dtls_socket changed status, from Unsupported to Different. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
