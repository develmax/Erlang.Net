# Part750 SSL documentation example release parity

The compiler recognizes a server-side sequence of tagged calls, nested address matching, singleton call arguments and external callback cleanup. It checks the AST argument relationships before emitting the three-slot frame and retains the original unmatched terms and source lines in seven failure branches. The cleanup trims the frame as each remaining value dies, then emits the external tail call.

The Windows OTP 29.1.1 `client_server.beam` matches byte for byte. The generated file is 4,896 bytes, and `start/0` contains 118 instructions. The pinned reference is commit ad05823719d77c8faee87348ea39513d4e2f99c5. [Oracle script](image-oracle.eval) and [output](image-oracle.txt).

The release regression uses the original OTP example source and image from `lib/ssl-11.7.7/doc/examples`, with the upstream source license retained. The test compiles the source independently of the fixture image. The focused validation performs compilation and image comparison without opening sockets.

Build the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part750-tool`. Run its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part750.json`.

The complete corpus checked 1,366 images. It reported 37 Equal, 488 SourceRejected and 841 Unsupported. The only changed status was `client_server`, from Different to Equal. `ELDAPv3` remains Equal. [Corpus report](otp-corpus.json).

Build the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part750-tests`. Run the three cases listed in the focused report. The full runtime suite and canonical readiness regeneration remain pending.

The next input blocker is adjacent string literals. The pinned sources of `ct_gen_conn`, `ct_groups` and `beam_ssa_bsm` contain adjacent strings at their reported parser failures. The diagnostic group also contains other causes and should be evaluated by a fresh corpus run after the parser change.
