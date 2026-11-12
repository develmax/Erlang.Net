# Part736 HTTP acceptor supervisor register list matching

For a single unguarded sequence clause with distinct variable elements in a fixed proper list, the C# compiler checks the input and extracts its elements in X registers before allocating the frame. Pattern failure preserves the original argument and branches directly to func_info. A valid input stores only the elements in the frame and continues through the existing general expression emitter.

The Windows OTP-29.1.1 reference is pinned to ad05823719d77c8faee87348ea39513d4e2f99c5. Its beam_core_to_ssa performs cons tests and extraction ahead of the body. The existing register instructions, body emitter and release regression were reused. Dependencies and licenses are unchanged.

The generated httpd_acceptor_sup matches the first 15 instructions of child_spec/1, and its frame decreases from 23 to 17 slots. The reference uses nine slots. The later function labels align and ExpT and LocT now match. AtU8, Code and Attr still differ. The OTP VM verified both ListenInfo branches and rejected short, long and improper child argument lists. The permanent comparison checks the pinned Code prefix and nine complete chunks. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part736-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part736.json.

The next step is to align child_spec/1 value lifetimes, call argument order and branch constructors.

The corpus checked 1,366 files and reported 29 Equal, 8 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part736-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
