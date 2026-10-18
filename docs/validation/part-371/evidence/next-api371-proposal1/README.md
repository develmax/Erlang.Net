# Ordsets core eight-API proposal (part 371)

Artifact-only source-close C# adaptation of pinned OTP-29.1.1 ordsets.erl. Root owns copying, shared-file edits, compilation, CI, statuses and publication. No helper build or original execution has occurred. API presence delta is +8 only after integration; compatibility delta currently zero.

`copy-plan.json` lists ten new Main files and three sequential shared-file edits. Append the HOST registration before the catalog snapshot, preserving every earlier ordered test. The HOST fixture executes twenty success/raw/error branch assertions. All namespace-level types are separate. Semantic strings/source line constants belong to OrdsetCoreNames.

`source-audit.json` records branch-by-branch parity and limitations. Original inputs are `ordsets.erl.pinned.txt`; source authority is commit ad05823719d77c8faee87348ea39513d4e2f99c5, lib/stdlib/src/ordsets.erl. Central attribution required; no source banners added. Use the existing Apache-2.0 central notice process.

`tool/` is a nonpreloaded ordinary source-module collector adapted from the already reviewed queue family collector. It accepts an existing admitted same-job original build receipt; it never builds, clones or invokes a second original installation. It validates source/head/build receipts, installed ordsets.beam, code root/path/full BEAM hash and original module MD5 against beam_lib:md5 of the loaded image. It retains bounded owned-process receipts and raw ETF before verdict. Original run and full-stack collection use matching monitored workers. Profiles are 112 values and five depth profiles (default,0,1,2,8), 560 full-stack observations: 672 total.

Copy tool to a Main tools/<chosen-name> directory before building parse-gate; project references are the existing ../../../src layout. Regenerate manifest only if Root intentionally changes source bytes, then record new hash. Read-only path relocation alone does not change manifests. CLI:

    python3 <tool>/collect-original.py --repository ROOT --expected-head HEAD --source-inputs-sha256 MANIFEST_HASH --parse-gate GATE_JSON --parse-gate-sha256 GATE_HASH --otp-source SOURCE --runtime-root PREFIX/lib/erlang --build-receipt BUILD_JSON --approved-build-receipt-sha256 BUILD_HASH --output FRESH

Use --verify-only for source + parse admission. This adapter must be an independent step after the same successful shared build, so another adapter failure cannot skip it. Dependent compile/observe failure remains retained and must fail final aggregation.

`sdk-harness/` reuses final part370 Main capture, with subject metadata set to ordsets. Root can copy to artifacts/<fresh-parent>/main-capture, matching its existing three-level project references, compile after integration, and call the generated DLL with repository/tool/fresh-output. Profile includes matching worker policy and timeout. Root must hash executed Main DLLs and retain raw full stacks. No arbitrary stack field normalization is authorized.

All eight APIs remain Needs verification until source/domain review plus successful original comparison. The partial module covers 8 of 21 exports. In particular add_element pending recursion frames and compound term/FUN ordering are explicit acceptance obligations, not waived differences.
