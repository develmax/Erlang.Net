# Supplementary queue reverse-trap original packet

Private original-only preparation; no Main, SDK, VM, Git, CI or network execution. The immutable160/800 queue packet is retained byte-for-byte and is not part of this run. Root owns parse admission, test-only branch and dispatch. Only four producer tool files change relative to the admitted queue tool: new original/queue_long_trap_contract.erl, profile.json, source-inputs.corrected.json and passive_etf_family.py.

Four groups share the same new fixture and unchanged matched worker, so exactly two unique Erlang sources are parsed. Total11 values/55 default/0/1/2/8 profiles. One pinned OTP setup/build serves all groups:

- reverse_direct: reverse_64017, reverse_256017. Direct lists:reverse/2 controls are explicitly original-only dependency diagnostics, not queue API completeness or lists API promotion.
- queue_to_list: to_list_rear_64017, to_list_rear_256017.
- queue_out: out_rear_64017, out_rear_256017.
- trap_controls: short_reverse_direct (original-only lists:reverse/2), short_reverse_rear (queue:to_list/1), long_append_64017 (queue:to_list/1), long_length_64017 (queue:len/1), long_from_list_64017 (queue:from_list/1).

The long improper list is precisely lists:seq(1,N) ++ tail: every integer1..N occurs exactly once, followed by atom tail. queue:out receives an extra rear head atom head, so its internal reverse input is precisely that same N-element improper tail. The append control puts the improper list in the front; the rear is empty. No native reduction budget or partition index is embedded.

All actual classes, reasons, effective arguments, locations, error_info and caller frames are retained. Source-derived protocol permits a future independently validated saved reverse state only when its List equals [k+1..N|tail] and Tail equals [k..1], for that runtime's own actual observed k. This does not drop fields, replace captured arguments, assume a particular k, or let native-only diagnostics hide a captured Main failure. Native nonzero k and its exact decomposition remain pending actual capture; no runtime protocol-state observation exists in this proposal. The pinned source audit is hash-bound in case-policies.json.

The existing queue-subject collector and workflow helpers are reused unchanged. The updated manifest preserves the existing pinned build manifest and repository inputs and rehashes42 tool inputs. profile.json has four groups, eleven names and exactly two unique source hashes. The source-only API implementation is not an input and must not be transferred.

Only the passive decoder MAX_TERMS changes100000→8000000;16MiB MAX_BYTES and depth128 remain unchanged. Per long-route group, source sizing estimates1600170 integer elements across five profiles and at most8000850 bytes for their five-byte integer payloads, plus metadata. Split groups avoid one oversized aggregate raw ETF. Actual capture still must satisfy all bounds; no field truncation is permitted. The independent synthetic controls accept full64017/256017 unique improper prefixes, verify every integer and tail, and demonstrate the old limit rejects256017. Eight refusal controls enforce byte, depth, container/aggregate term, truncation, version, unsupported tag and trailing-byte checks. The aggregate-budget control temporarily lowers only its in-memory test decoder threshold, then restores8million. No downloaded code was used for these parser controls.

Expected source branch: pinned OTP29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5 erl_bif_lists.c reverse BIF_TRAP2 saved arguments. Working sibling OTP files are not source authority. Original and SDK results, native k, reduction counts and complete queue acceptance are deliberately not claimed here.
