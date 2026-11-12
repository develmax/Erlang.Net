# Part807 Complete Observer detail release image

The debug writer now emits original abstract forms for dynamic remote calls, calls through fun values and record construction with explicit fields. It shares token-based annotation, call and record field writers with existing forms. The release profile records the wx include path and generates abstract forms.

All 161 decoded forms and the debug options match the pinned OTP 29.1.1 release. The 14,888-byte compressed Dbgi payload matches, as does the complete 19,908-byte Windows release image. [Decoded comparison](decoded-summary.txt), [comparison script](compare.eval) and [whole image hash](oracle.txt).

The permanent release check uses the existing pinned cdv_detail_wx source and BEAM, plus its observer_defs header. The files and the wx header have the same SHA-256 hashes as the installed release assets.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part807-tool` and run as `dotnet artifacts/beam-backend/part807-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part807.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part807-tests` and run with the exact cases listed in the focused report.

The full 1,366-file corpus reported 69 Equal, 195 SourceRejected and 1,102 Unsupported. Only cdv_detail_wx changed status, from Different to Equal. ELDAPv3 remains Equal. The tool returned 1 because unmatched cases remain. [Corpus report](otp-corpus.json).

All four focused tests passed, including the new whole-image regression. Full runtime suite execution and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The next Observer emission gap is cdv_html_wx:init/1, which tests a two-element list and dispatches on guarded and tagged tuple patterns. [Pinned disassembly](next-init.txt).
