# Part806 Observer record callbacks and release compile profile

The C# compiler shares the record-event success block and emits a direct tagged error fallback. Cleanup retains the frame across the notification and calls destroy only when the frame is present. The deterministic 944-byte fixture preserves the upstream Apache-2.0 notice and matches OTP 29.1.1 byte for byte. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The Windows release profile restores the compile options and source path recorded in OTP. The full cdv_detail_wx image now has 13 of 14 identical chunk payloads, including Code, CInf, Docs and Line. Dbgi is the sole differing chunk. The release payload is 14,888 bytes and the current payload is 66 bytes. The compile profile writes compile info while leaving abstract form generation disabled. An isolated probe that enabled abstract form emission reached the first unsupported debug writer path at init/1. [Chunk comparison](chunks.txt), [release metadata](release-metadata.txt) and [full debug probe](full-debug-probe.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part806-tool` and run as `dotnet artifacts/beam-backend/part806-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part806.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part806-tests` and run with the exact cases listed in the focused report.

The 1,366-file corpus reported 68 Equal, 1 Different, 195 SourceRejected and 1,102 Unsupported. No status changed from Part805. ELDAPv3 remains Equal. The tool returned 1 because unmatched cases remain. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
