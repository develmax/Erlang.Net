# Early outgoing argument motion

Reference OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The scan uses the full fresh Part 1862 allocation operand rank and generated release images.

| Diagnostic | Functions | Modules |
| --- | ---: | ---: |
| Plain excess frame with equal live count | 7,672 | 826 |
| Excess stack and equal heap/live counts | 815 | 342 |
| Excess stack and equal live count, different heap | 94 | 59 |
| Generated X-to-Y spill before its first call | 6,685 | 798 |
| Candidate Y absent after first call and no reference prefix spill from the same X index | 977 | 391 |
| Same first call with no generated label or trim in prefix | 574 | 304 |
| Straight non-GC prefix and uniquely assigned candidate Y | 114 | 76 |
| Previous row with a candidate in the highest physical stack slot | 34 | 21 |
| Pure linear early-motion window, excluding legacy adjacent literal case | 44 | 33 |
| Previous row with one ordinary frame and no trim, handler, or debug/executable line | 41 | 31 |

This is a structural census. It does not prove physical X identity across differently allocated reference bodies or successful compilation with the proposed patch. The final two rows additionally require a dedicated save and one X reload, a known virtual source, and no mention of the new destination across the motion window. Some may already be handled by another phase.

Pinned SSA `find_yregs` records a value for Y only if it is used after an X clobber on a reachable route. It uses a killed-set union and definitions intersection at joins. The isolated C# patch uses its existing register availability analysis, adds unique-predecessor checks for the motion window, and allows only line metadata, moves, and non-collecting reserved-heap list or tuple constructors. It requires one unambiguous stack lifetime and one use, and excludes all original destination accesses in the crossed range. It defers other spill edits in the same function to the next optimization iteration.

The exact disassembly witnesses are in `pinned-vs-part1862.terms`. `erlsrv:rename_service/3` keeps x1 in x0 before a `put_list` overwrites x1. The pinned image preserves one stack slot where the candidate reserves two. `httpd_acceptor:accept_failed/3` can move x2 into x3 before literal loading and tuple construction overwrite x2. The pinned frame has two slots where the candidate has three.

The isolated tests cover positive single-constructor and multi-instruction cases, plus collection and original destination read/write controls. The patch passed `git apply --check` against the frozen Part 1861 work source. The patch awaits managed build, focused fixture execution, exact corpus diagnostics, and loader verification.
