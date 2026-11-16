# Part778 Observer persistent-term callback release image parity

The compiler preserves the first call result across a second call, checks the tagged tuple and builds the nested return tuple in one heap reservation. The failure path raises badmatch with the unmatched value. Release metadata now keeps private type definitions and typed legacy record fields from included headers, including their original coordinates. The documentation chunk continues to contain the module and exported function entry.

The Windows OTP 29.1.1 cdv_persistent_cb image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches in all 10,024 bytes. The original source, image and wx.hrl match the installed fixtures and are used in the permanent regression. The implementation reuses the C# type parser, BEAM call and operand writers and release metadata pipeline. Dependencies and upstream notices are unchanged. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part778-tool`. Run the resulting DLL with the pinned installation and `artifacts/beam-backend/otp-corpus-part778.json`.

All four targeted checks passed, including the permanent release comparison with the pinned header. [Focused report](focused.json). The full runtime suite and canonical readiness regeneration were not run.

The full corpus run checked 1,366 release files and reported 55 Equal, 1 Different, 209 SourceRejected and 1,101 Unsupported. cdv_persistent_cb moved from Unsupported to Equal. cdv_atom_cb moved from Unsupported to Different. Its emitted code has 19, 30 and 60 instructions in col_to_elem/1, get_info/1 and format/1 respectively. OTP has 11, 16 and 34 instructions for those functions. The module also needs its release profile. [Corpus report](otp-corpus.json).
