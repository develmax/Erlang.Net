# Validation source increment361

This temporary validation branch adds original OTP get_map_size/make_ref observations and a Linux native namespace experiment. It does not publish a completed runtime API or claim100% compatibility. Main production source and its CI workflow remain unchanged.

Existing manual workflow filenames are branch-local execution aliases because GitHub dispatch requires a workflow present on the default branch. The map-size and make-reference alias contents equal their separately hash-bound original workflow files. The namespace alias additionally accepts a source closure digest. Every dispatch pins this exact branch head and reviewed source manifest.

The branch skips automatic full Windows regression on source-only pushes; the actual original/native workflows run explicitly. This branch-local CI trigger change must not be merged into Main. Production integration still requires full regression and same-head Main CI.

Earlier local checkout attempts hit Windows path limits, then an initially empty sparse index and CRLF-only shared Python checkout differences. The final sparse checkout preserves the complete Git tree; shared dependencies are checked from canonical parent Git blobs. No failed attempt or API result is relabelled successful.
