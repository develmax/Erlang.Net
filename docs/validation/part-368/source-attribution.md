# Source and adaptation record

Authority remains OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. The sibling reference checkout is unchanged. Read source through its pinned Git objects.

The C# queue implementation follows `lib/stdlib/src/queue.erl` (SHA256 `61B322302ECF74578D72B442D2317EA200A5DEE4E0994B58C5E1E961F252B101`), including immutable rear/front representation, shallow guards, aliases, private balancing/filter/delete helpers, callback evaluation and source error locations. Iterative C# continuations replace physical Erlang recursion while retaining observable source frames and cooperative cancellation.

The member traversal follows `erts/emulator/beam/erl_bif_lists.c`: actual sixteen-cell reduction entries, exact equality, early return and last-entry error arguments. Reverse retains the accepted part367 allocation-path protocol. Saved-entry relations are checked against each VM's own state and every unchanged error/frame field; another VM's physical entry offset is not injected.

The conservative literal apply lowering follows `lib/compiler/src/sys_core_fold.erl` simplify_apply/get_fixed_args. Wrong-arity function application follows `erts/emulator/beam/beam_common.c` and uses the existing C# source-frame capture. These are general source semantics; no fixture or test-name dispatch is added to production code.

OTP source carries Apache-2.0 notices. Existing third-party notices and attribution remain in place. No external package or production BEAM runtime is introduced. The owner's separate `LICENSE.local.txt` remains uncommitted, as requested. Test-only Erlang/Python tools and the native OTP build are development validation infrastructure.

These source records are provenance and adaptation evidence. Complete-contract classification also requires the independent original comparisons, per-MFA clause/domain arguments, actual source/DLL bindings and final regression receipts.
