# Part634 LDAP instruction selection and release attributes

The C# BEAM emitter now emits comparison tests that branch directly on guard failure. Tests read constants and saved Y registers without copying them to X registers. Conjunctive guards branch on the first failing term. The managed execution regression exercises ordering, conjunction and exact distinction between an integer and an equal-valued float.

The ASN.1 writer reuses the referenced composite codec for tagged aliases. Six LDAP response encoders and their decoders now delegate to LDAPResult using the requested tag. The release source includes separate Dialyzer attributes and the ASN.1 options. The generated info/0 reads those options through module_info. Documentation generation admits those two attribute names.

The local OTP-29.1.1 reference at ad05823719d77c8faee87348ea39513d4e2f99c5 supplies the BER generation and BEAM instruction reference. The compiler uses its existing C# emitter and literal encoding. No dependency or license changes.

All 274 function names, arities and relative order match. The generated instruction count fell from 23,731 to 16,194. OTP emits 7,028. The Code chunk fell from 92,545 to 60,241 bytes against 27,815 in OTP. The generated file is 76,852 bytes against 99,008 in OTP. The abstract debug stub accounts for much of the file-size gap. CInf, Meta and StrT match byte for byte. Decoded attributes and their order match apart from vsn, which is derived from the emitted code. The remaining debug, documentation, line and type chunks differ.

The compiler and tests built with dotnet build -m:1 --no-restore -v:q. The three focused compiler checks passed. OTP matched 320 direct calls, 83 round trips, 88 additional boundary calls and info/0 on the final image. Comparison covers normalized bytes, values and reasons. See the saved oracle expressions and results, including [per-function instruction gaps](instruction-gaps.txt), [chunk sizes](chunks-oracle.txt) and [attribute comparison](attributes-oracle.txt).

The installed release path comes from artifacts/library376-official-compiler/official-admission.json. The corpus tool was built into artifacts/beam-backend/part634-alias-tool and run as dotnet artifacts/beam-backend/part634-alias-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part634-alias.json.

The complete test harness and canonical readiness regeneration were not run. Readiness requires a complete report. No runtime MFA promotion. Work remains local in the existing checkout.

The final corpus processed all 1,366 images. 18 are Equal, 13 Different, 488 SourceRejected and 847 Unsupported. KEMAlgorithmInformation-2023 now emits a differing image. [Per-image report](otp-corpus.json), [focused tests](focused.json).
