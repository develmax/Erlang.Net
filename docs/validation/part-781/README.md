# Part781 Observer record details and constant arithmetic

The BEAM compiler folds closed arithmetic inside returned literals by reusing the Erlang numeric evaluator. The record-details flow selects tagged reply tuples, preserves live values across conversion and formatting, and emits separate badrecord and case_clause failures. The page-init flow forwards a local call result inside a tuple to a remote tail call. The debug writer can serialize legacy record field access. Planning diagnostics identify the function that failed emission.

The pinned OTP 29.1.1 disassembly establishes register scheduling, failure labels and heap reservations. The independent deterministic fixture covers the arithmetic literal, record-details case and page-init tail call. The full cdv_dist_cb release image remains unsupported at detail_pages/0, which returns a local function reference nested in a list. No dependency or upstream notice changed. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part781-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part781.json`. The fixture oracle was compiled with `compile:file(Source, [binary,deterministic])`.

Four focused checks passed, including byte comparison of the 1,140-byte deterministic fixture. [Focused report](focused.json). Full runtime and canonical readiness were not run.

The full corpus run checked 1,366 release files and reported 58 Equal, 195 SourceRejected and 1,113 Unsupported. Status counts match Part780. The cdv_dist_cb diagnostic advanced from get_detail_cols/1 to detail_pages/0. [Corpus report](otp-corpus.json).
