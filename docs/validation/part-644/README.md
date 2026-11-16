# Part644 terminal branches and guard type tests

The C# emitter stops linear case and if emission after an unconditional match or true guard. A prebound pattern variable remains a comparison, and case_clause remains reachable when no clause matches. If branches share temporary stack slots. Guard type predicates use the BEAM type-test opcodes for atoms, binaries, integers, lists and tuples. The managed regression covers true fallback and a failing prebound pattern.

The generated LDAP image contains 10,689 instructions against 10,958 in Part643 and 7,028 in OTP. The generated image is 110,172 bytes against 99,008. The import sets now differ only by binary:at/2 in the generated image. Instruction order, registers, source annotations and Type remain different. The abstract forms and debug options still match after removing source coordinates. [Import comparison](import-oracle.txt), [chunk comparison](chunks-oracle.txt).

Both builds and all three focused tests passed. OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and two unknown-type reasons. The reference remains OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The changes reuse the C# emitter and existing BEAM test opcodes. No dependency or license changes.

The corpus tool was built with dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part644-tool. It ran as dotnet artifacts/beam-backend/part644-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part644.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The full corpus returned 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused tests](focused.json).
