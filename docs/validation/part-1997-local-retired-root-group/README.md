# Part 1997 local outgoing root cohort

The pinned baseline is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. This group used the unchanged P156 compiler. The [launch receipt](comparison-launch.json) binds its executable to the exact [204-file selection](scope.json). The group covers all 484 audited saved-image corridors with a final X0 setup and a directly observed outgoing root.

The uncached compiled comparison took 383.041 s. It recorded 0 Equal, 204 Different, 0 Unsupported and 0 SourceRejected. Normalization and OTP preparation took 115.062 s. The historical comparison finds 31 normalized gains and eight reported losses. All 188 images that differ from their last observation prepared in OTP. The saved prior image can predate the current compiler's direct parent, so these counts describe the observed comparison.

A fresh P154 recompile covers every module implicated in those eight apparent losses: diameter_service, mod_auth, application_controller, yecc, ssl_config and erl_tar. The exact [six-file parent comparison](immediate-parent-comparison.terms) lists ten changed functions. It reports zero normalized losses against the parent; all eight original bodies have the same normalized equality status on both fresh compilers. This cohort makes no whole-file promotions. Current aggregate evidence is the unchanged [observed ledger](../part-1990-local-parity/observed-status.json) of 539 Equal and 827 Different.

The preceding [Part 1996 checks](../part-1996-local-root-schedule/README.md) already verify paired native execution. This larger run extends compile and load-preparation coverage. The next task is stable source frame ownership before label emission. Complete corpus acceptance, full regression, readiness and local commit are pending.
