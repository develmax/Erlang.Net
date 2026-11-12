# Part838 DTLS guarded option instructions

The compiler emits a shared list traversal for emulated_active_option/2 and a guarded validation dispatch for validate_inet_option/2. The latter specialization checks that the private function is used only in direct calls with one of the two known option keys. It records the lower bounds inferred after the comparisons in the typed register operands.

The deterministic fixture matches OTP 29.1.1 byte for byte and is part of the permanent regression. Rebuild it with pinned `erlc.exe +deterministic -o tests/Erlang.Tests/Resources/Beam tests/Erlang.Tests/Resources/Beam/beam_guarded_list_lookup_probe.erl`. The behavior probe covers fallback, term ordering, interval boundaries and thrown errors. [Probe](behavior.eval), [results](behavior.txt) and [hashes](oracle.txt).

The full dtls_socket image loads in pinned OTP and now measures 24,112 bytes. The selected full-module checks include interval edges and invalid mode handling. After label normalization 27 of 30 functions match. The three remaining instruction differences are listen/2, check_active_n/2 and emulated_options/3. AtU8, Code, ImpT, ExpT, LocT, Attr, Line and Type still differ. [Function comparison](module-comparison.txt), [full-module probe](module-behavior.eval) and [results](module-behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part838-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part838-tests`.

All four focused tests passed. The full 1,366-file corpus reported 91 Equal, 1 Different, 195 SourceRejected and 1,079 Unsupported. No module status changed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
