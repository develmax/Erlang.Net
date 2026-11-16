# Part708 LDAP mixed optional and required decoder fields

The compiler extends optional field scheduling to a required penultimate field. It forwards the remaining list to the required match in x0, keeps the final optional tail in y1 and preserves the badmatch payload and source location. The regression covers all fields present, absent optional fields and a missing required field.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reuses the optional field decoder, frame emission and existing call and match instructions. Dependencies and licenses are unchanged.

dec_MatchingRuleAssertion/2 drops from 88 to 75 instructions and matches OTP after renumbering labels. ExpT and LocT now match byte for byte. Line and Type remain identical. The managed image is 99,156 bytes with 7,050 instructions. OTP has 99,008 bytes and 7,028 instructions. Only Code and Attr differ, and Attr differs only in vsn. [Instruction comparison](mixed-decoder-delta.txt), [sections](chunks-oracle.txt), [attributes](attr-oracle.txt), [remaining instruction sites](code-sites-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part708-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part708.json. The LDAP behavior oracles matched 320 direct calls, 83 round trips and 88 boundary calls. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The next repeated encoder difference is in two-field register allocation and payload projection. [Focused checks](focused.json), [corpus](otp-corpus.json), [two-field encoder](two-field-encoder-oracle.txt).
