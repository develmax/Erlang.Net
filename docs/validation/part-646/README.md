# Part646 LDAP import order and ordered selection

The general C# BEAM emitter replaces element/2 with a direct tuple projection when a local call is proven to return a tuple of sufficient arity. The proof checks every normal return path through local tail calls. Uncertain calls retain element/2 and its badarg behavior. The managed regression exercises both paths.

Literal and tagged-tuple select_val tables now use exact Erlang term order and emit branch bodies in reverse key order, as observed in the pinned OTP assembler input. The semantic order of all 19 imports matches the release. The atom table shares a prefix of 314 entries, up from 11. The remaining atom differences start in decode_boolean and decode_integer, where the generated binary instructions introduce =:= and skip and the release uses no_fail. ImpT still differs at the byte level because atom indices differ. [Order comparison](order-oracle.txt), [chunk comparison](chunks-oracle.txt).

The generated module has 10,673 instructions against 7,028 in OTP. Its file is 110,120 bytes against 99,008. Instructions, registers and metadata remain different. The 300 forms and debug options still match after removing source coordinates. OTP matched 320 direct calls, 83 round trips, 88 boundary cases, info/0 and two unknown-type reasons.

Reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation reuses the C# emitter and Erlang term ordering. No dependency or license changes.

Both builds used dotnet build -m:1 --no-restore -v:q. The final corpus command was dotnet artifacts/beam-backend/part646-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part646-final.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The complete harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

Both builds and all three focused checks passed. The full corpus returned 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused tests](focused.json).
