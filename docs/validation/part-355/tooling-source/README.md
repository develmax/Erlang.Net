# Original native-entry observations (source proposal)

Unexecuted Linux x86-64 collector for eleven cases and eight source-built images. It reuses the pinned scheduler-profile runtime builder without relabelling that builder manifest, and requires its exact installed PREFIX/lib/erlang root and complete successful owned-process receipts. The workflow takes an exact subject head and collector manifest SHA; ordinary corpus and launch flags are unchanged.

Each case has a fresh compiler VM and observation VM using +S 2:2. Raw compilation errors, initialization errors, ETF observations, loaded/object BEAM paths and significant code MD5, full-file MD5, runtime root and installed hashes remain separate. Failure preserves completed observations and does not claim eleven successes. Both forced foreground termination and remaining-group kill reject normal completion.

The ancestor fixture source is E0302C174FA50450E54687951B9796892E239CAFD458FA4A60E253D69EBD7813. All C and module bodies are byte-identical. The observer only avoids a second code:load_file when the collector already loaded the exact image for provenance inspection; assertions and case expectations are unchanged.

The Latin1 module branch uses the public pinned erl_nif.h entry macro layout, including ERL_NIF_INIT_PROLOGUE/GLOB/DECL/BODY/EPILOGUE and all current ErlNifEntry initializer fields. Only the stringized name is replaced by explicit Latin1 octal bytes. ERL_NIF_INIT_BODY is retained, including the platform callback-table contract. Header SHA and pin are in manifest.json. This is a source audit, not a compiled ABI observation.

Run check-source-controls.py with Python -B for pure schema/hash/cleanup refusal checks. Proposed execution is solely the workflow or build-native.py followed by collect.py with approved runtime/native receipt SHA arguments. There are no embedded golden observations. Successful source assertions establish these fixtures only, not the complete NIF API, exact unload counts, managed interop, resources, dirty scheduling, reload or upgrade.

Native-build receipt fields shared with the managed capability proposal: Status, SubjectHead, PinnedOtpCommit, SourceManifestSha256, RuntimeBuildReceiptSha256, HeaderInputs, Compiler, Images (Leaf/SourcePath/SourceSha256/CompilerDefinitions/Path/Bytes/Sha256/DefinedSymbols/UndefinedSymbols/BuildProcessReceiptSha256), OwnedStages. Building an ELF image does not authorize its managed execution; exact image and bridge review remain separate.
