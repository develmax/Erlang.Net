# Part814 HTML detail-window expansion

The compiler proves the state record shape at all local call sites, emits the atom-or-tuple result type for lists:keyfind/3, and matches the window opening and raising paths. The update reuses the typed record operand. All 46 expand/3 instructions match after adjusting label numbers and omitting file names from line operands. ImpT and Type now match byte for byte and are included in the release regression. [Comparison](comparison.txt) and [comparison script](compare.eval).

Both full images ran on the pinned OTP runtime with temporary GUI stubs. Eleven scenarios produced the same values, errors and messages, including new and existing windows, a wrong-sized tuple returned by keyfind/3, and an invalid list. [Behavior script](behavior.eval) and [results](behavior.txt).

The complete image still differs in AtU8, Attr, Code, ExpT, Line, LitT and LocT. The next instruction target is handle_event/2.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part814-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part814-tests`.

The 1,366-file corpus reported 69 Equal, 2 Different, 195 SourceRejected and 1,100 Unsupported. Statuses are unchanged. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
