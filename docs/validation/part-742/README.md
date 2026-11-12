# Part742 Byte-identical ASN.1 runtime template image

The compiler groups unguarded, distinct three-element tuple patterns by two atom keys and an integer. It emits shared return and decision blocks for identical outcomes, placing blocks at their last occurrence to allow fall-through from the final predecessor. The optimization requires constant results and a constant unconditional fallback. Other shapes retain existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The emitted dependencies/1 instruction sequence and literal ordering match the pinned release. The compiler reuses the existing tuple checks, select instructions and literal writer. Dependencies and licenses are unchanged.

The generated asn1ct_rtt.beam matches all 168,012 bytes of the installed release. OTP verified all 104 assertion keys and 179 dependency keys, five invalid inputs and the code/0 result. The existing vendored source and release image now participate in the whole-file regression. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part742-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part742.json.

The next candidate is megaco_config_misc. The release build profile and the guard-heavy functions were inspected for the next comparison.

The corpus checked 1,366 files and reported 31 Equal, 6 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. asn1ct_rtt was the only status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part742-tests`. All five selected compiler and ETF checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
