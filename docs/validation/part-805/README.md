# Part805 Observer page list comprehension

The C# compiler emits a singleton path and a separate three-argument recursive function for the list comprehension in create_pages/3. The helper captures the notebook and the callback arguments, skips values that do not match the two-element tuple pattern and raises bad_generator for a non-list input or improper tail. It uses the existing generated function naming convention and follows the pinned OTP register schedule.

The deterministic 1,008-byte fixture keeps the upstream Apache-2.0 notice and uses local callback stubs. The complete image matches OTP 29.1.1 byte for byte. Running both images on pinned OTP with a temporary wxNotebook stub also matched results for singleton, empty, mixed, improper and invalid inputs. [Oracle script](oracle.eval), [byte comparison](oracle.txt), [behavior script](behavior.eval) and [behavior results](behavior.txt).

The complete cdv_detail_wx image is 4,776 bytes, compared with 19,908 bytes in the release. The Line chunk now matches byte for byte. AtU8, Code, ImpT, ExpT, LitT, LocT, Attr, CInf, Dbgi and Docs still differ. The disassembly shows 37 instructions for create_pages/3 and 39 for its helper in both images. Both instruction lists match after adjusting the helper label offset and omitting file names from line operands. Remaining label shifts originate in later functions, including handle_event/2 and terminate/2. [Relative label comparison](relative-labels.txt). [Disassembly summary](disassembly-summary.txt) and [comparison script](compare.eval).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part805-tool` and run as `dotnet artifacts/beam-backend/part805-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part805.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part805-tests` and run with the exact cases listed in the focused report.

The 1,366-file corpus reported 68 Equal, 1 Different, 195 SourceRejected and 1,102 Unsupported. No status changed from Part804. ELDAPv3 remains Equal. The tool returned 1 because unmatched cases remain. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
