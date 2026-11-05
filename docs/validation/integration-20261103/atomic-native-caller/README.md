# Atomic native-caller selection evidence

[Owner handoff notes](owner-README.md), [source/lifetime audit](SOURCE-AUDIT.md), [original manifest](manifest.json) and [patch](atomic-native-caller.patch) preserve the exact reviewed proposal and the source/base hashes. All original source bytes are in the gzip members indexed by [source archive](source-archive.json); decompress a member to recover the recorded raw SHA256. They are proof data, not extra build projects.

The deterministic [baseline](race-before2.log) invokes replacement_reverse with a stale native-only caller; the [preinstalled control](control-before.log) has no such frame. The [admitted candidate](race-after-admitted.log) retains its selected native badarg body and correct helper. [Owner run](owner-final-run.log) and [scope controls](scope-results.json) preserve success/throw/yield/hibernate/factory-cancel and scope-disposal observations. No sleep/race loop or extended corpus deadline is used.

[Matching-ABI broader replay](broad-replay-summary.json) retains24 complete raw partitions,36100 pairs and ten guest probes per backend, plus54 native/equality controls. Mixed-ABI acquisition attempts are rejected and retained by the owner. Current exact-source full9680 and final-head CI/review are separate mandatory delivery gates; no whole-module classification is promoted here.
