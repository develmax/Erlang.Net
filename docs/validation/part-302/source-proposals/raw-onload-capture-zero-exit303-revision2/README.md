# Inherited-pipe parent exit requirement

Source-only two-file revision atop immutable raw-onload-visibility-original303-proposal1. The previous proposal is unchanged; exact parent hashes are recorded. Apply check_process_capture.py and the resealed source-plan.json to the same original source closure. The workflow's approved-plan placeholder and independent typed guard must use the revised plan SHA from this manifest.

The inherited-pipe control now requires parent ExitCode==0 in addition to timeout, pipe closure, parent reap and exact raw stdout. Thus a parent killed before exiting cannot be labeled as an already-exited parent whose owned child retained stdout/stderr. The launched program and raw process capture implementation are unchanged. The stronger typed guard should independently check this same condition.

No original emulator, native fixture, SDK or process-control execution occurred during this revision. No Main, Git or published original source was changed. The predeclared original semantic policy is unchanged.
