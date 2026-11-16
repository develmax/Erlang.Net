# Part637 LDAP match emission and stack slot reuse

The general BEAM emitter omits failure code for matches against fresh variables and wildcards. It omits reloading the result of a match when the surrounding sequence discards that result. Matches against existing bindings still test equality and produce badmatch on failure. Tuple construction uses literal operands directly. Linear case branches reuse temporary stack slots while keeping outer bindings.

The whole generated ELDAPv3 image now has 12,402 instructions against 13,922 in Part636 and 7,028 in OTP. The Code payload is 46,252 bytes against 51,177 previously and 27,815 in OTP. The dec_LDAPMessage_protocolOp/2 frame uses 11 Y slots and 318 instructions against 12 and 375 previously. Both outer dispatchers retain their 162 instructions each. The generated image is 62,672 bytes against 99,008 in OTP. See [stack frames](frame-oracle.txt), [instruction gaps](instruction-gaps.txt) and [chunks](chunks-oracle.txt).

OTP matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and both unknown-type reasons on the final image. Both builds and three focused checks passed. Release comparisons use OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5 as the oracle. The implementation reuses the existing C# emitter and stack slots. No dependency or license changes.

Dbgi still carries a stub. Docs and Type still differ. Line reflects the reduced instruction stream and still differs from the release. Whole-image parity remains open.

Both builds used dotnet build -m:1 --no-restore -v:q. The corpus ran as dotnet artifacts/beam-backend/part637-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part637.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The complete harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The final corpus processed 1,366 modules. 18 matched, 13 differed, 488 were rejected during source admission and 847 reached unsupported lowering. [Per-module report](otp-corpus.json), [focused tests](focused.json).
