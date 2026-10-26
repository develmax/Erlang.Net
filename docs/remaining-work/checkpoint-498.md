# Part498 — post-merge review repair

All14 affected PR discussions have been read. Four concrete defects from PR47,54,55 reproduce on current main and are repaired: relocated native-tool paths/imports, saved host error-handler stack arguments, and flat-map copying across VM atom orders. Failed/running/unattributable automatic review states remain explicitly visible. [Evidence and per-finding dispositions](../validation/part-498/merged-review-followups/README.md).

Final local affected regression196/196 retains all132 earlier BIF groups and adds48 host-handler combinations and imported maps from host/other VM. Both copy flags, key/value sharing, explicit source tuples, depth limits and parser source metadata are covered. Three production files change;1723 guards remain; native plan rows/seals advance without ABI promotion. Actual import/path checks run locally and are scheduled on Ubuntu CI.

No API or full-contract promotion:708 registered,177 verified,506 pending,25 known. erlang80/351, binary31/31, proplists22/22, queue38/38, ordsets6/21. The original559 cohort remains59 closed/500 pending (10.55%); six reclassified pending contracts are additional. Module counts and the broader obligations remain as [checkpoint497](checkpoint-497.md).

Merged main CI40295953011 and its independently verified artifact pass9035/9035. This repair's local9037 readiness is a parent/final196 composite, not a new uninterrupted full run. Exact repair-head continuous CI, packages, original captures, consumers, cleanup, Ubuntu tooling checks and fresh review retrieval remain delivery gates recorded in the PR. Historical ordsets validation worktree/branch and LICENSE.local.txt stay intact. Next API work remains is_process_alive/1 and is_alive/0 after this repair is verified.
