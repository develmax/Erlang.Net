# Frozen ordsets handoff

The [owner notes](owner-README.md) describe the completed source window and its limitations. [Original manifest](manifest.json) preserves all19 source hashes. Every original source byte is retained in the gzip files indexed by [source archive](source-archive.json); decompress a member to recover its original SHA256. The archives are proof data, not additional build projects.

[Integration notes](integration-notes.json) identify the two deliberate adaptations: retain the complete per-pair public API routes, and apply the reviewed logical-map exact-equality fallback fix. The ten genuine guest probes and their fixture are added alongside the existing comparison and ordsets coverage. No compatibility status is promoted. Unvalidated watchdog/native-caller proposals remain excluded.
