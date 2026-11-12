# Part769 Diameter supervisor release image parity

The BEAM lowering reuses the constant binding reader for a trailing static remote call. Each prefix binding must be fresh and resolve to a constant. Each call argument must also resolve from those bindings. The normal argument move scheduler then emits the tail call. The existing constant return lowering still handles init/1. Debug metadata continues to serialize the original Erlang forms.

The four supervisor images match the Windows OTP 29.1.1 release at ad05823719d77c8faee87348ea39513d4e2f99c5. All four have identical instruction counts for the five functions, including 9 for start_link/0 and 7 for init/1. The profile reuses the Diameter bootstrap version, includes, warnings and app_vsn insertion. All four source/image pairs already exist in the repository and match the installed OTP fixtures. No dependency or license changed.

[Oracle script](oracle.eval) and [byte comparison](oracle.txt).

Build with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part769-release-tool`. Run the resulting DLL with the Windows OTP installation path and `artifacts/beam-backend/otp-corpus-part769-release.json`.

The next candidate is mnesia_checkpoint_sup:init/1. It keeps the flags tuple in Y0 across timer:seconds/1 and the supervisor timeout call, then builds the nested worker result with one heap check. Its release metadata records the Mnesia vsn insertion.

The four targeted compiler checks passed on the final implementation, including all four release regressions. [Focused report](focused.json). The complete runtime suite and canonical readiness regeneration were not run.

The intermediate corpus run classified diameter_peer_fsm_sup and diameter_service_sup as Different after the compiler learned to emit them. Applying the same base Diameter release profile made both images match.

The final corpus run checked 1,366 release files and reported 46 Equal, 209 SourceRejected and 1,111 Unsupported. The four Diameter supervisors were the only status changes from Part768. [Corpus report](otp-corpus.json).
