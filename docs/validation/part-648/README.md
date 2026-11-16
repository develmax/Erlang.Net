# Part648 LDAP fixed binary decoders

The general C# emitter recognizes a local decode followed by a case over an empty binary or the pair of single-byte patterns and a tagged exit fallback. It uses the OTP instruction sequence with no saved Y slots, retains the original binary for the fallback, and shares the boolean match context. The managed test exercises zero, a nonzero byte, both excess-length cases and an empty binary.

decode_boolean/2 and decode_null/2 now match OTP instruction for instruction after normalizing source lines and local labels, as does decode_integer/2. The respective instruction counts are 26, 18 and 18. AtU8 and ImpT still match byte for byte. The whole module has 10,613 instructions against 7,028 in the release. The generated file is 109,980 bytes against 99,008. Remaining Code, register choices and metadata differ. [Decoder comparison](decoder-oracle.txt), [chunk comparison](chunks-oracle.txt).

Both builds and all three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary cases, info/0 and both unknown-type reasons. Abstract forms and debug options continue to match after removing coordinates. The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter and OTP binary instruction forms. No dependency or license changes.

The corpus tool was built with dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part648-tool. It ran as dotnet artifacts/beam-backend/part648-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part648.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The complete harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The full corpus returned 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused tests](focused.json).
