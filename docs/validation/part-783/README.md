# Part783 Observer port callback release image

The C# compiler rebuilds the complete 15,456-byte `cdv_port_cb.beam` image from the Erlang source and headers using the pinned Windows OTP 29.1.1 release profile. The installed release file serves as the expected output. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The tuple formatter emits both integer conversions in source order, saves the first result in a frame slot, and prepends the single separator character with `put_list`. The tagged reply case shares the existing tuple arity dispatch and emits a lifted callback that captures one value. The integer table reuses the input register when every key targeting a return block equals its result.

The pinned OTP `beam_ssa_pre_codegen.erl` and release disassembly served as references for frame placement and register use. The implementation reuses the existing operand writers, closure table, and source binding resolution without introducing dependencies.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part783-tool`. It ran against the pinned installation and wrote `artifacts/beam-backend/otp-corpus-part783.json`. Across 1,366 files it reported 60 Equal, 195 SourceRejected and 1,111 Unsupported. The tool returned 1 because unmatched cases remain. Only `cdv_port_cb` changed status from Part782. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

All four focused tests passed, including the permanent release comparison. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
