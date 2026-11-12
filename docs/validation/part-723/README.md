# Part723 LDAP final encoder schedules

The compiler projects both inputs before encoding either of two optional fields. For an enumerated first field followed by a required field it preserves the fallback operand in x1, retains the second field across the enumeration and trims the frame before the second call. The fixture covers both optional defaults, valid enumeration and its tagged error.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the two-field scheduler, deferred enumeration and tagged exit instructions. Dependencies and licenses are unchanged.

enc_IntermediateResponse/2 and enc_ModifyRequest_changes_SEQOF/2 match OTP disassembly including labels. Three functions retain differences in the length operands. Both images have 7,028 instructions. The managed image is 98,996 bytes and the OTP image is 99,008 bytes. Code and the Attr vsn differ. [Instruction comparison](encoder-delta.txt), [sections](chunks-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part723-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part723.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
