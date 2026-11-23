# Part 1964 mixed terminal case component

The production patch updates the existing register-case planner. A terminal atom and tagged-tuple decision may test the disjoint tuple domain first when the tuple arm returns exactly the selected X0 value. The tagged check records tuple type information. Original instruction order is retained for guarded arms and for an arm that inspects, changes or forwards the payload through an effect. The decision uses source-derived tests and matches pinned OTP Core aliasing and constructor partitioning.

| Check | Result |
| --- | --- |
| Frozen compiler parent | 061352dd54adf01c548a224825c3930a6a8d73b09d0212324d1e2c9b576d6518 |
| Isolated compiled candidate | aefdffb3fc236c63ef9a9140edbfb4419b4ccfb750073b0d69a9782609aac804 |
| Source closure | All 928 compiler inputs verified. Only GeneralCatch.cs differs. |
| Permanent regression | Red on the parent for the typed case layout. Green on the candidate. |
| Scoped candidate | 5 Equal and 36 Different. Zero Unsupported or SourceRejected. All previous exact matches retained. No new full raw matches. |
| Changed release functions | megaco_digit_map:report/2, mnesia_log:prepare_prev/4 and mnesia_schema:remote_read_schema/0 receive pinned mixed-case dispatch order. Full normalized functions have zero new or lost matches. |
| Candidate scope time | 218.720 seconds comparing all 41 files at jobs=1. Parent compared the same set in 205.399 seconds. |
| Native execution | 13 synthetic and five actual loaded megaco_digit_map observations agree with pinned ERTS. The changed fixture preserves exact error reasons, source lines and the recorded side effect. |

The first experiment reached a later emitter. Its generated fixture had the same layout as the baseline. The production V2 change uses the earlier register-case planner that owns the fixture and three release witnesses. The mnesia_backup functions used to diagnose this family still need changes in their other emitter routes.

The first isolated build missed dependency reference assemblies. The first two test build attempts lacked a linked source file and generated apphosts. Subsequent attempts supplied the exact existing production inputs, reusing the unchanged dependency assemblies. The original build logs and test outcomes are retained with the component.
