# Part826 SSL distribution supervisor

The C# compiler emits the nested boot argument match, the ETS initialization and the configuration reader. The configuration path covers reading and decoding the file, scanning and parsing the expression, checking its bindings, and emitting the OTP error branches with source locations.

The full 5,600-byte ssl_dist_sup image matches the Windows OTP 29.1.1 release. The behavior probe compares startup without a configuration file, startup with a valid file and an ETS entry, and seven error scenarios. It runs both images in pinned OTP and compares error stacks. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

Run the probe from the repository with the pinned `erl.exe -noshell -fixture_case CASE -eval SCRIPT`, reading SCRIPT from behavior.eval. Supply `-ssl_dist_optfile` with the corresponding path for each file case. The valid file contains `[{fixture,true}].`, the parse error file contains `[{fixture,}].`, the scan error file contains a lone double quote and the bindings file contains `Fixture = [{fixture,true}].`. The encoding error and incomplete fixtures contain an ASCII `%% coding: utf-8` line followed by byte 255 or 194 respectively. Supply an absent path for the missing case and two arguments for the malformed boot argument case.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part826-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part826-tests`.

The 1,366-file corpus reported 89 Equal, 195 SourceRejected and 1,082 Unsupported. Only ssl_dist_sup changed status. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The next target is ssl_client_session_cache_db. Its update/3 constructs a tuple in an argument register before the external tail call. [Pinned disassembly](next-session-cache.txt).
