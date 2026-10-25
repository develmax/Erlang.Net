# Part493 — complete integer/text contracts

Pinned OTP-29.1.1: `ad05823719d77c8faee87348ea39513d4e2f99c5`. Close integer_to_list/1,2, integer_to_binary/1,2, list_to_integer/1,2 and binary_to_integer/1,2 through complete source partitions and2467 values/12335 full stack outcomes (14802 whole comparisons).

## Complete source arguments

Formatting accepts integers and bases2..36, default10. The decimal invariant provider and repeated exact DivRem produce the same sign/uppercase digits as pinned bif.c/binary.c, including zero and small/big boundaries. Term.String creates fresh character cells; BitString creates fresh ASCII storage. Invalid values/bases produce native badarg with original argument/stack behavior.

The native list parser's short path recognizes exact sign words, skips zero digits and interprets the low byte of native-small character terms. Preserve this unusual behavior; do not substitute a stricter Unicode parser. The base-specific digit threshold decides when to restart from the original list through strict whole-byte conversion. Proper tails, empty/sign-only inputs, invalid characters and short/big representation boundaries follow pinned big.c and erlang.erl. Tests include wide/negative native-small terms and out-of-small values in first/middle/long paths.

The binary parser first validates base and whole-byte binary, skips optional sign/zero prefix, and uses remaining length to select the native short/big path before validating digits. All-zero data returns immediate zero directly. Strict digit arithmetic accepts no whitespace, separator, radix prefix or malformed sign. The whole-byte bitstring extraction is shared with the already-audited binary primitives.

Long parsing groups12 digits into bounded UInt64 chunks. Even36^12 fits UInt64. For adjacent groups, value=left*rightMultiplier+right and multiplier=leftMultiplier*rightMultiplier; balanced regrouping preserves the exact positional value. BigInteger combination and sign application are exact for supported term magnitudes. No callback/input mutation occurs. Identical physical allocation limits/timing, dirty-CPU scheduling and whole compiler completion are not asserted. source-audit.json binds every partition and dependency.

## Actual stack repair and bounded zero path

The first run passes18/35 groups. All2467 ordinary values match; parser errors lack the source helper location. Pinned erlang.erl uses badarg_with_info for the native short failure, and error_with_info after promotion to the long parser. Add an explicit native-path result flag, preserve the original argument list, and select the correct helper source location. The list path sets the flag exactly at its existing digit-threshold transition. The binary path mirrors the native sign/zero/remaining-length decision. Do not infer the location from error text or numeric result.

Share the source formatter literal between integer and existing atom wrappers through ErlangErrorHelperMetadata. The atom wrappers retain the same map identity and error_with_info location. The distinct badarg_with_info line is stored separately; the existing erlang source filename/error_with_info constants retain their actual meaning. No unrelated metadata rewrite occurs. Keep every complete pre-fix actual/expected trace.

The binary all-zero prefix returns zero immediately, matching native chars_to_integer and avoiding unnecessary BigInteger group/multiplier construction. Signs without a digit still fail. This is a source-aligned optimization, not a speed benchmark. Existing12-digit grouped parsing and native short-path character rules are retained.

## Execution and delivery

All35 final original subjects use the existing opaque function barrier and monitored worker/support. Disassembly proves runtime apply;6427 provider files match before/after, and owned children settle. Default/depth0/1/2/8 whole values/stacks compare with no normalized fields. Bases2..36, signs, boundaries, large integers, malformed types/lists/binaries, native-small character quirks and long paths are included.

The final rebuilt assembly passes all100 preceding/new BIF groups:46584 whole comparisons. This reruns every preceding65 group, including shared atom metadata identity. Local8996 readiness remains explicitly composite; exact current-head continuous GitHub CI/full cold consumers and fresh original subjects are required before delivery. Source/plan preflight and index hashes are recorded separately. LICENSE.local.txt remains excluded.

Registered708 unchanged. Full159->167 (+8), pending521->513, known28 unchanged. Original559 cohort49 closed/510 remaining (8.77%):507 pending and3 known, plus six additional reclassified audits. erlang70 verified/265 pending/16 known of351; other module counts unchanged. The broad request is unfinished.
