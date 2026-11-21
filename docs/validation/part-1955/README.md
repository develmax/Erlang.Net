# Part 1955 validation stopped after uncached shard output

| Equal | Different | Unsupported | SourceRejected | CompilerError |
| ---: | ---: | ---: | ---: | ---: |
| 505 | 859 | 0 | 0 | 2 |

Five uncached shards covered the same 1,366 distinct paths as the release corpus. The aggregator stopped on two CompilerError rows. Their actual classification and exception text are preserved. The merged release acceptance gate did not run.

The raw candidate gains otp_internal and PKIX-X400Address-2009 relative to provisional Part 1954. It loses erl_bifs. The accepted source remains Part 1953. The 122-module pilot retained all previous controls and gained both target images after restoring the ASN.1 forwarding and ct_logs capture order. All 450 selected tests passed. Actual linked ERTS matches comprised 623 release observations and 36 synthetic observations. The ct_logs callback instruction sequence matched separately after physical label comparison.

ex_canvas_paint and sudoku_game expose the same unchecked last-field access in a zero-field record update. A two-module rerun on the exact frozen compiler captured full stacks. The preliminary raw erl_bifs regression is in a literal-dispatch return arm. Both defects require another linked acceptance gate. Readiness was regenerated from the latest complete passed managed report and accepted compiler scope. The original Jaro deadline remains open.

[Raw inventory](failure-audit/first-failure-inventory.json) · [Exception stacks](failure-audit/stacks.json) · [Prior accepted scope](../part-1953/README.md).
