# Part635 LDAP dispatch, pattern registers and source metadata

The C# BEAM emitter recognizes two-argument forwarding functions with distinct atom keys and local one-argument destinations. It emits select_val and frameless tail calls for the successful branches. A recognized exit fallback builds its nested tagged reason in X0 with one heap check. Other fallbacks keep the existing general lowering. The managed regression exercises both table branches and the fallback.

Tuple and list matching read saved operands directly and extract fields into Y registers. This removes intermediate X-register copies in the generated decoders. Frame-wide stack slot allocation still differs from OTP.

Each LDAP dispatcher has 162 instructions, matching the installed release. Selector keys, forwarded branch instructions and relative label positions match. The tested unknown-type error reasons also match. The complete module has 14,370 instructions against 16,194 in Part634 and 7,028 in OTP. The Code payload is 54,839 bytes against 27,815 in OTP. All 274 function identities and order match. See [dispatch evidence](dispatch-oracle.txt) and [remaining instruction gaps](instruction-gaps.txt).

The release profile records the source filename in Line. The ASN.1 source prologue matches the module and module-doc positions in Docs. Function positions and instruction line records still differ. Dbgi still carries a stub and Type still lacks OTP's inferred operand types. CInf, StrT and Meta match byte for byte. The final file is 71,308 bytes against 99,008 and remains Different. See [decoded metadata](metadata-oracle.txt) and [chunk comparison](chunks-oracle.txt).

Reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5 and its pinned loader instruction forms. The implementation reuses the existing C# emitter, release profile and source writer. There are no new dependencies or license changes. OTP is used as a comparison oracle.

The corpus tool was built with dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part635-release-tool. Run dotnet artifacts/beam-backend/part635-release-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part635-release.json, using InstalledOtpRoot from artifacts/library376-official-compiler/official-admission.json. The saved Erlang oracle expressions reproduce the OTP VM comparisons against the generated release-profile image. The full test harness and canonical readiness regeneration were not run. The readiness tool needs a complete report.

Both builds and three focused checks passed. The OTP VM matched 320 direct calls, 83 round trips, 88 boundary calls, info/0 and two unknown-type reasons. The complete corpus returned 18 Equal, 13 Different, 488 SourceRejected and 847 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused report](focused.json).
