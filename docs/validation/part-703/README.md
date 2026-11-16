# Part703 LDAP encoder pair forwarding

The compiler forwards the pair from a sequence encoder with one stack slot for the tag. A private tuple forwarding function omits its entry checks when every call site proves that the input is a pair. The fixture covers both forwarding forms and retains function_clause when the shape is unproven.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This change reuses the compiler's call inspection, pair facts and tuple projections. Dependencies and licenses are unchanged.

Eleven wrappers drop from 26 instructions to 14. encode_tags/2 drops from 21 to 8. Each matches OTP after renumbering labels. The managed image is 100,204 bytes and contains 7,376 instructions. The Line section still matches byte for byte. [Instruction comparison](pair-forwarding-delta.txt), [sections](chunks-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part703-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part703.json. Three focused checks passed. The corpus covered 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. Full runtime validation and canonical readiness regeneration remain pending. [Focused checks](focused.json), [corpus](otp-corpus.json).
