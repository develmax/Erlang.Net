# Shared-frame interior argument copies

The copy pass forwards a retained source X register through a
straight argument setup after the first body call. It requires all
semantic references to a single-frame Y slot to belong to proven
save/reload pairs. It leaves frame shrinkage and indexing to the
existing checked passes. The existing permanent fixture includes
branch-local forwarders, zero-slot call frames, nontrailing live
roots, GC, late Y reads and source writes.

All 40 selected cases of 9,836 pass. The uncached 59-file scope
runs in 146.15 seconds and reports 5 Equal, 54 Different,
0 Unsupported and 0 SourceRejected. All 45 changed images prepare.
The exact 32 positive frame witnesses across 26 modules each reach
the predicted width. Sixteen reach pinned width. The larger outer
36-module selection and live-slot controls are retained. Against Part
1875, no normalized body or raw chunk loses equality in this scope.

The earlier mnesia_ext_sup regression is outside this selected
part. It is still being corrected on a descendant and the accepted
canonical local source remains separate. No full gate is claimed
for this isolated source.
