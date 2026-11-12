# Part780 Arithmetic patterns and Observer release parity

The parser folds closed arithmetic patterns with the existing Erlang numeric evaluator. The executable pattern is a literal. Debug metadata retains the original operator tree, including macro expansion coordinates, and writes record indexes in their source form.

BEAM lowering reuses the existing integer selection-table writer after a typed atom redirect. Tagged reply projection can forward one input argument to the remote call before building the result pair.

The Windows OTP 29.1.1 cdv_timer_cb image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 12,376 bytes. The pinned OTP lint source, abstract forms and release disassembly establish the admitted operators, annotations and emitted code. The Observer wx profile and original header fixtures are reused. Dependencies and upstream notices are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part780-release-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part780-release.json`.

The intermediate corpus run admitted cdv_fun_cb and matched its instruction and data chunks. Applying the same Observer profile made the entire release image match. Both Observer modules are embedded in the permanent release-image regression.

The final corpus run checked all 1,366 release files and reported 58 Equal, 195 SourceRejected and 1,113 Unsupported, with zero differing images. Fourteen previously rejected sources progressed. Two reached byte parity and twelve reached the emission stage. [Corpus report](otp-corpus.json).

All four targeted checks passed, including the permanent release comparison for both modules. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.
