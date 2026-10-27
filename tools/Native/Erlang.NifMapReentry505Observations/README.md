# Native flat-map reentry observations

This dedicated source-only tool observes a map across two NIF calls, preserving
same-reference duplicate fun keys with different values after safe33->32 shrink.
It does not replace or alter the frozen part504 fixtures or their goldens.

The source-plan here is an initial NOT_COLLECTED template. The parent has since
integrated the tool and adopted an actual original golden in the active source
tree; retain that active plan and provenance rather than overwriting them with
this preparation template.

For a new independently prepared integration, the lifecycle is:

1. Copy the tool and five MapReentry505 backend files. Add --map-reentry505 CLI
   dispatch, preserve raw source line endings, then seal the actual target files.
2. `seal.py --repository ROOT --original-only` requires no golden or managed host.
3. Run collect.py with --repository, --provider-admission, --plan-sha256, --output
   and --original-only. Output must be new and inside ROOT/artifacts/native-map505.
4. `seal.py --repository ROOT --original-run RUN_DIRECTORY` validates the genuine
   original-only completion and captured source plan before copying its bytes as
   original-values.etf. Existing different golden bytes are never overwritten.
5. Build the managed backend, reseal actual sources, and run full collect.py with
   --check-golden. Both ordinary and reviewed loaders run independently after
   preparation. Complete default and deterministic captures compare strictly.

Preflight aggregates independent dependency failures, verifies public headers and
the pinned6427-file original provider, and checks managed preparation only for a
full run. Failed preparation prevents dependent native execution. Both route
cleanup proofs are inspected even if the original or raw comparison fails.
Source/host bytes and the original provider are checked again after the run.
Physical native-module retirement, NIF environment/binary baselines, policy-call
counts, owned process receipts, complete ETF equality, and original golden checks
remain separate evidence. No post-capture normalization or managed-seeded golden
is permitted, and no Linux execution or whole-module100% claim is made.

The fixture uses actual iterator-derived roles, fixed NEW_FUN_EXT metadata, two
separate native invocations and actual garbage collection. It removes a unique
numeric key while converting HAMT33 to flat32; removal of the first duplicate key
occurs only later in flat32. All target occurrence values and numeric values are
observed, including after independent-environment copy/clear/free.
