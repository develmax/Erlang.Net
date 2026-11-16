# Part704 LDAP decoder result forwarding

The compiler forwards the result of a local decoder call directly in x0 to a unary local tail call or binary_to_list/1. Both forms use a zero-slot stack frame. The planner recognizes the same forwarding shape as the emitter, including a plain assignment followed by a local call. The regression exercises a local consumer and the external conversion.

The reference is OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The change reuses existing call resolution, location emission and tail-call instructions. Dependencies and licenses are unchanged.

Twenty-one decoder wrappers drop from 20 to 8 instructions each. All match OTP after renumbering labels. The managed image drops from 100,204 bytes and 7,376 instructions to 99,456 bytes and 7,124 instructions. OTP has 99,008 bytes and 7,028 instructions. The Line section still matches byte for byte. Code, ExpT, LocT, Attr and Type differ. [Instruction comparison](decoder-forwarding-delta.txt), [sections](chunks-oracle.txt), [remaining instructions](instruction-gaps-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part704-final-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part704-final.json. Full runtime validation on this version and canonical readiness regeneration remain pending. The immutable Part561 comparison report remains selected.

The three focused checks passed after the planner fix. The corpus checked 1,366 files and reported 18 Equal, 19 Different, 488 SourceRejected and 841 Unsupported. The LDAP oracles matched 320 direct calls, 83 round trips and 88 boundary calls. [Focused checks](focused.json), [corpus](otp-corpus.json).
