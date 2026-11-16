# Part 1821 — Local worktree recovery

The audit used the 25 registered worktrees and GitHub main `2caf839f06f60f3d0c3a51a4556e76c7a44d7dca`. It inspected tracked, staged, and untracked files reported by Git and compared their Git-filtered content with main and the integrated checkpoint. Build products, ignored files, and the owner-local license were excluded. The source worktrees and their indexes were left intact.

| Worktree | Result |
| --- | --- |
| Main local checkout | Of 18,923 Git status entries, 18,468 matched main and 448 matched the checkpoint already reachable from main. Six compiler files and one regression file contained distinct local changes. These changes were recovered. |
| `lc-validation` | Current list-comprehension parsing, evaluation, and guard handling use the updated compiler layout. Main contains all 36 executable fixture identifiers and all five diagnostic fixture identifiers from this checkout. |
| `w504` | Main contains the NIF version 651 loader and observer. The old source uses unbounded native string reads where current code uses the bounded reader. The local observations came from an earlier harness revision. |
| `w517fix` | Main contains ordinary reference storage, its replay, and loader map-key tests. The staged source admissions and hash bindings describe an earlier runtime and tool layout. Reusing them would bind the active workflows to older inputs. |
| `w546` | Recovered the cooperative list-length fixture's fault propagation. The other local edits include older native bridges, alternate GC root handling, and ownership experiments. The GC proposal routes public collection through the CLR helper even when the current target can own guest roots. The earlier literal-key proposal does not cover current module literal ownership and retirement. Those experiments require current-source verification. |
| Three Ordsets and Proplists validation copies | The two dirty Python blobs are already reachable from main in immutable validation evidence. |
| `part361-original-validation2` | The source-preservation artifact already exists in main. The local registry attaches a historical author-only source review with integration still pending. |
| `el` | The sole untracked file is a local build log. |

Other registered worktrees were clean. The audit also counted 94 commits reachable from local branches and worktree heads outside main. These are committed histories. They are not part of the recovered file changes and were not rewritten.

The OTP compiler reference was `OTP-29.1.1` at `ad05823719d77c8faee87348ea39513d4e2f99c5`, including `beam_trim.erl`, `beam_ssa_codegen.erl`, and the liveness pass in `beam_ssa_opt.erl`. The implementation uses the existing C# BEAM emission and projection-flow analysis. No dependency or third-party source was added.

The release corpus regenerated all 1,366 images. There were 400 byte matches and 966 differences. ELDAPv3 matched byte for byte, and no module changed its equality status. The report also records changed difference descriptions, which are not an instruction or performance comparison. [Validation results](results.json).

The [per-module corpus report](otp-corpus.json) uses a stable relative source name for the temporary C#-generated ELDAPv3 input.

The hosted candidate run passed all 9,746 tests. The readiness CLI used its test report and the existing immutable comparison snapshot in module-readiness-scope.json. The unsharded local run observed an interpreter-profile teardown assertion failure. Its isolated retake passed, and the hosted suite passed the same test. [Hosted validation](https://github.com/develmax/Erlang.Net/actions/runs/43810185722).
