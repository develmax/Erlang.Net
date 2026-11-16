# Part774 SASL terminal report-handler release image parity

The BEAM compiler emits the repeated binding as an exact test between the tuple field and the second argument. The guard evaluates node/1 with a fallback label and compares its atom result against node/0. The fallback preserves state in Y0, forwards the local formatting call result and returns the state in a tagged tuple. A zero-argument remote call can now also provide the first element of a pair while the second element survives in Y0.

The Windows OTP 29.1.1 sasl_report_tty_h image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 2,300 bytes. The pinned source and release disassembly establish the repeated binding, typed node comparison and call sequence. The original source and image are reused in the permanent regression. The profile records bootstrap version, include path and warnings. Dependencies and notices are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part774-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part774.json`.

The next candidate is mnesia_sp:init_proc/4. The pinned code reserves the catch slot before logging and uses a guarded exception branch and a stacktrace-building fallback.

All four targeted checks passed, including the permanent release comparison. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.

The full corpus run checked 1,366 release files and reported 51 Equal, 209 SourceRejected and 1,106 Unsupported. sasl_report_tty_h was the only status change from Part773. [Corpus report](otp-corpus.json).
