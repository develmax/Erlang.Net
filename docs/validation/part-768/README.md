# Part768 asn1ct_table release image parity

The compiler emits process-dictionary reads as BEAM BIFs. A literal assertion preserves the looked-up value on the failure path, and the frame for the following calls is allocated only on success. An exact comparison can consume the read directly, and a remote tail call can use it as an argument without spilling registers.

The negated local predicate optimization requires a callee with a provably boolean comparison result. The list comprehension used for effects emits a private recursive helper. The helper keeps the remaining list across the item call, discards the result and reuses the generator's nil and invalid-input lowering.

The reference is Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The pinned v3_core.erl and the release disassembly establish how the unused comprehension result is omitted. The existing C# call, operand, metadata and generator failure writers are reused. The release test embeds the original source and image already in the repository. Dependencies and licenses are unchanged.

The 2,976-byte release image matches byte for byte. [Oracle script](oracle.eval) and [output](oracle.txt).

Build the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part768-tool`. Run its DLL against the pinned Windows OTP installation and write `artifacts/beam-backend/otp-corpus-part768.json`.

The four targeted compiler checks passed, including the installed-release image regression. [Focused report](focused.json). The complete runtime suite and canonical readiness regeneration were not run.

The next candidates are diameter_config_sup and diameter_watchdog_sup. The pinned code folds the constant bindings into the supervisor start call and the init return literal. Release metadata also includes the sys_pre_attributes transform and app_vsn insertion.

The full corpus run checked 1,366 release files and reported 42 Equal, 209 SourceRejected and 1,115 Unsupported. asn1ct_table was the only status change from Part767. [Corpus report](otp-corpus.json).
