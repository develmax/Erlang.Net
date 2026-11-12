# Part645 native fixed byte matching

The general C# BEAM emitter lowers fixed unsigned byte patterns with a binary match context and bs_match. It tests the exact bit length, skips wildcard segments, compares byte literals and captures other values directly into saved registers. Normal pattern checks apply to captures, including repeated bindings. The managed regression checks successful capture, repeated-value mismatch, excess length and a non-byte-aligned input.

The generated LDAP image no longer imports binary:at/2. Its import set matches the release. ImpT still differs in entry order and atom indices. The image contains 10,677 instructions against 10,689 in Part644 and 7,028 in OTP. The generated file is 110,132 bytes against 99,008. The abstract forms and debug options continue to match after removing coordinates. [Imports](import-oracle.txt), [chunks](chunks-oracle.txt).

Both builds and three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type reasons. The pinned reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter and existing binary opcodes. No dependency or license changes.

The corpus tool was built with dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part645-tool. It ran as dotnet artifacts/beam-backend/part645-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part645.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The complete harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The full corpus returned 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused tests](focused.json).
