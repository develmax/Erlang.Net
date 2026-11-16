# Part642 LDAP abstract form parity after removing coordinates

The ASN.1 generator emits the OTP forms for octet decoding blocks, length addition, default boolean guards, extension handling and result bindings. It uses independent counters for enumeration defaults and bounded integer variables. The generated BER helpers follow the pinned OTP bodies. The C# compiler unwraps a block around a closed comprehension mapping call and drops the discarded extension test when both paths return true.

All 261 function forms match the release after removing coordinates, up from 178. All 300 abstract forms and ordered debug options match under the same comparison. File and record metadata continue to match with coordinates. OTP compilation of the same generated source matches all 297 non-file forms with coordinates. [Full form comparison](all-forms-oracle.txt), [function count](public-api-oracle.txt), [same-source comparison](debug-oracle.txt).

Dbgi is 53,280 bytes against 54,262 in OTP. The generated BEAM is 111,060 bytes against 99,008, with 11,041 instructions against 7,028. Differences remain in source coordinates, instruction selection, registers and inferred types. OTP matched 320 direct calls, 83 round trips, 88 boundary cases, info/0 and two unknown-type reasons. Both builds and three focused tests passed.

Reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation uses the existing C# generator and emitter, with OTP as a comparison oracle. No dependency or license changes. The corpus tool was built with dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part642-final-tool. The command was dotnet artifacts/beam-backend/part642-final-tool/Erlang.BeamCorpus.dll $InstalledOtpRoot artifacts/beam-backend/otp-corpus-part642-final.json. InstalledOtpRoot comes from artifacts/library376-official-compiler/official-admission.json. The full harness and canonical readiness regeneration were not run. No runtime MFA promotion or publication.

The full corpus returned 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported out of 1,366. [Corpus report](otp-corpus.json), [focused tests](focused.json).
