# Part724 byte-identical LDAP release image

The compiler follows reachable local call contexts from exported functions and local function references. It emits natural integer operands only when all observed contexts prove the value. It preserves the narrower positive range after the zero test in the octet loop and the natural range of the intermediate length sum. The ASN.1 regression test now compares the complete generated BEAM with the vendored OTP reference through the existing release image comparison.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses its pair facts and typed operand emission. Dependencies and licenses are unchanged.

The managed ELDAPv3.beam matches the installed release byte for byte. Both images are 99,008 bytes and have SHA-256 F137904A4B15CE2FC382264B168B606AB9728B7A5A7C8D641C6937D87CB2ED9D. All sections match and all 274 functions have the same disassembly. [Image comparison](image-oracle.txt), [sections](chunks-oracle.txt), [instructions](instruction-gaps-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part724-final-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part724.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

All three focused checks passed. The corpus checked 1,366 images and reported 19 Equal, 18 Different, 488 SourceRejected and 841 Unsupported. ELDAPv3 moved from Different to Equal. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. On hosts without the pinned compressor, the permanent test follows the existing release comparer and compares the decoded Docs and Dbgi payloads. [Focused checks](focused.json), [corpus](otp-corpus.json).
