# Part381: scoped warm build repair

Source and application tasks now reuse a request path derived from the exact serialized request bytes. Existing wrong contents, directory or reparse destinations are refused. Owned input staging occurs after a genuine cache miss; an unused source stage no longer gets a completion file. Content-aware AtomicJson itself is unchanged. Provider, driver, tool/dependency, native evidence, live-input and cold owned-input checks remain. The previous source-route behavior of rebuilding an edited owned BEAM is retained; application refusal behavior is unchanged.

Final Tool4617D2A8EF7AF501A4D58FA87A6039ABE5E07EF6A4897F2E353523C59863AE58 passes:

- Five source/include/cache steps and sixteen application CLI cases.
- Seven MSBuild groups, including ordinary projects, source/warm, retained/data-only alternatives, placement/overlap refusals, resource edits and publish/clean.
- Two actual altered-request refusals plus recovery: no new native child, prior owned files unchanged.
- Two real original-child resource mutation controls, preserving accepted state and owned-process cleanup. Exact parser instruction at mutation is not claimed.
- Six isolated PackageReference restore/build/no-op/away-run/publish/published-away-run steps.
- Final source style check.

The real final warm MSBuild invocation leaves all 69 file/directory entries across source and application owned trees unchanged, including hashes, sizes and modification times. Filesystem observers receive zero mutation notifications and zero errors after successful create/delete canaries. The same observer against frozen380ToolE0FF detects 43 events and 20 differences, with entries increasing from69to87 despite build exit0. Its owned exit1 is an expected negative control, not a failed new build. This proves the named Windows V1 owned-tree no-op case, not universal filesystem syscall tracing or zero writes elsewhere in MSBuild.

Old/new observed wrapper durations are9.578/12.390seconds. They ran under different concurrent validation load and do not establish a speed improvement. The measurable reduction is eighteen extra entries to zero. An early style check required a multiline method declaration; all affected regression groups were replayed on the final identity rather than relabeling the preliminary ToolFEC0 results.

Runtime remains exact Part379 SHA897181D2747427D534BBA6727D3FAAAB6D21B359CBD02753D0B5E96DBA083146. Its8081/8081 general result was not rerun. Only the changed CSharp SDK package was rebuilt (SHA5B67C71E65FD785E97102FF9755ADFCF98C2B2B1AEE7AE849475FAD3CCFFE350); unaffected tool-package/full general gates were not repeated. [Completion](completion.json) and [467 frozen bindings](bindings.json) preserve source before/after, final binaries/package, actual reports and process evidence.

The broader Part381 ordsets corpus remains open. Original112values/560depth profiles pass; managed fixture preparation refuses apply/select_tuple_arity/recv_marker roles before any corpus comparison. This repair neither replaces those fixtures nor promotes API compatibility. Global API/full-contract delta0/0; no external publication.
