# Independent pre-execution original13 typed semantic guard

Source-only guard; syntax parsed, not run against observations or synthetic fixtures. No new native/build/Main/Git/network action. The guard is authored against the immutable original proposal plan EAC7F22640E3233268FD81EC22568A08666BAD5A190A81748DC1A49A1D5B20C0 and predeclared typed policy B1CA9E182911DE41ECF74214B889E51FFF6CE7E77F244A77DA49EFCB0DFA2F15. It does not put expected values into raw native observations.

Reuse: imports decode_exact from tools/Erlang.BeamFixtures/verify-metadata-mutations.py only after its exact source hash matches decoder-binding.json. The existing typed bounded decoder keeps atoms distinct from integers, tuple/list shape distinct, rejects invalid/truncated/trailing ETF, and enforces depth/size bounds. No parallel ad-hoc ETF decoder is introduced. Import bytecode writes are disabled.

The guard requires exactly thirteen ordered source-policy cases, completed raw receipt with SemanticAssertionsExecuted=false, no skipped/excluded cases, exact receipt file set and every hash/byte count. It verifies copied proposal sources, fixture.so/source/configured ABI header/public pinned headers, original build tool provenance, every process exit/timeout/pipe/reap state, actual argv, fresh VM PIDs and original runtime metadata. A failure-receipt or source/hash/count drift cannot become success.

Typed case checks validate complete observation envelopes, before-load not_loaded, successful load or the source-derived typed load-veto prefix, integer/list/tuple constructor results, latest reason and ignored normal returns, full pending-detail reason tuple, badarg/raise replacement, and the native exception's first zero-arity NIF frame. Full original stacks and taint lists remain in the original result.etf unchanged. First frame identity/arity is checked without asserting volatile full source line text. Taint delta must append the target once and preserve prior admission order, including load veto. The guard emits raw ETF hashes; it never rewrites native files or turns collection Complete into a semantic/SDK pass.

Success status is ORIGINAL_ONLY_THIRTEEN_TYPED_SEMANTIC_PASS, with SdkCompared=false,SdkPass=false,GenericNifSupport=false and NativeUnloadCounterCompared=false. This is original semantics only. Combined workflow SDK ABI/fixture/source-entry results require separate evidence and comparison. Optional CI envelope fields can be checked against explicit expected head/run/job, but this offline guard does not authenticate GitHub state; root must bind downloaded run/job/artifact manifests independently.

Proposed invocation after actual collection (not executed):

    python verify-original13.py --observations <original-output> --proposal <reviewed-original-proposal> --build-provenance <nif-pending-otp-build-provenance.json> --decoder-source <repo>/tools/Erlang.BeamFixtures/verify-metadata-mutations.py --report <fresh-report-outside-raw-directory.json>

Optional envelope schema: {Head,Run,Job,RunConclusion,JobConclusion,SourcePlanSha256}; add --ci-envelope plus --expected-head/--expected-run/--expected-job. Every supplied conclusion must be success and every identity must match. Reports cannot overwrite an existing report or be placed inside immutable raw evidence. Guard failure writes a distinct failure report and rethrows, retaining all raw files.

Required future guard verification before trusting it: positive original13 archive; atom-vs-integer corruption; missing/truncated/trailing ETF; wrong first NIF frame; tuple/list swap; reordered/dropped/extra cases; missing process/raw file; timeout/raw nonzero exit; changed source/plan/header/native bytes; missing configured header; load-veto incorrectly accepted; taint duplicate/reorder; completed collector with false native result; SDK failure paired with original-only success. None of those runs is claimed here. Python is observation test tooling under work.md2.1, not production runtime implementation.

Promotion must preserve frozen source bytes (use the same -text original/* attribute convention as existing NifFixtureHost). Line-ending normalization needs a new reviewed plan/policy and guard binding, not silent hash relaxation.

Revision 2 corrects only the reviewed metadata arity: the immutable observer emits one tag plus seven key/value pairs (eight tuple elements), not nine. METADATA_ARITY names this protocol size. Original observer, frozen source plan/policy, typed projections and decoder are unchanged. Guard 1 and its freeze remain preserved; neither guard is claimed executed.
