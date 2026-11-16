# Part643 direct LDAP arithmetic instructions

The general C# BEAM emitter lowers addition, right shift, bitwise AND and bitwise OR with gc_bif2. Operands use literals or saved registers directly. The instruction branches to the guard failure label in a guard and raises badarith for an invalid ordinary operand. Temporary operand slots become available after the instruction while bound values stay live. The managed regression tests successful addition and shift, ordinary badarith, and arithmetic guard failure.

OTP uses 67 gc_bif arithmetic instructions and no external calls for these four operators in ELDAPv3. The generated image now has the same counts, replacing its previous 67 external calls. The complete instruction count fell from 11,041 to 10,958 against 7,028 in OTP. The image remains 111,060 bytes against 99,008. The abstract forms and debug options still match after removing source coordinates. [Arithmetic comparison](arithmetic-oracle.txt), [chunk comparison](chunks-oracle.txt).

Both builds and all three focused checks passed. OTP matched 320 direct calls, 83 round trips, 88 boundary cases, info/0 and both unknown-type reasons. Reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The work reuses C# register operand emission and the existing GC BIF opcode. No dependency or license changes.

The corpus tool build used dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part643-tool. It ran as dotnet artifacts/beam-backend/part643-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part643.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. Full harness and readiness regeneration were not run. No runtime MFA promotion or publication.

The full corpus recorded 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused tests](focused.json).
