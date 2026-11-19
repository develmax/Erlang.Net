# Terminal tagged tuple projection and selection

The independently generated 3,108-byte ssh_channel_sup.beam is byte-identical to the Windows OTP 29.1.1 release image. Both complete files have SHA-256 730924FBF4A6D95E9318756115EC575F032B31D6E041FDAE8735913E07A851C6. The repository BeamCorpus tool independently reports Equal.

This patch extends the validated map-field continuation with a terminal tuple case plan. It admits tag-preserving return pairs, one wider direct projection, and a guarded nested projection with a later unconstrained same-tag identity branch. It emits the arity and tag selections in block order, leaves the original selector intact for identity and nested failure paths, and roots the selector and nested tuple through their respective heap checks. Other patterns, such as a fixed literal field in the nested pattern, use the regular case lowering. The implementation is independent of release module and atom names.

The reference is pinned OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. The saved beam_ssa_codegen listing shows the control-flow tests and live prefixes. The beam_clean listing for a separate wider-arity fixture shows the shared identity return and physical block labels. The raw baseline, pinned sources, and comparison after normalization of physical labels are saved separately. The candidate's post-call body matches both raw and normalized comparisons.

| Scoped BeamCorpus result | Map-field baseline | Candidate |
|---|---|---|
| Equal | 5 | 6 |
| Different | 3 | 2 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Comparison time, one worker | 7.63 s | 8.38 s |

One full match gained and none lost. All five prior-equal controls retained exact file equality. The permanent fixture passed with the candidate and failed the intended raw-code assertion on the map-field baseline. ERTS prepared and executed both SSH roles with four supervisor reply forms. Native OTP also ran sixteen independent direct, nested, identity and failure observations across the baseline, candidate and native-compiled fixture. All corresponding observations were identical. The constrained nested literal case retained its different matching semantics.

The verified compiler SHA-256 is CFB91602E79AC1C4E4EE539D90A675EA56439BA8559940E8913C85C36EAEA3B6. The build ran with one BelowNormal worker and took 75.52 seconds after qualifying the conflicting Pattern type name. The original compiler error log is retained. Source layout and patch application checks passed. The candidate test ran against the isolated compiler. The complete repo tests and release corpus gate remain with the integration lane.
