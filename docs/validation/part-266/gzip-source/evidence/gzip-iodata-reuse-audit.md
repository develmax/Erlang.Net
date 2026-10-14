# Gzip / iodata wrapper source prototype

Seed d45b59d; six owned paths, no Main edits, build, native observation or default MFA change. Native16 outcomes pending.

Pinned erlang prepare_loading bypasses gunzip only for raw aligned FOR1 binary. Every other Code0 goes through zlib:gunzip, whose iodata path uses iolist_to_iovec; any Erlang exception falls back to exact Code0. prepare_loading_1 builds the error frame with decompressed Code if gunzip succeeded; outer load_module uses original Code0. Candidate keeps those existing wrapper distinctions.

Reuse ErlangIoData.Flatten(value,false,false), including nested lists and improper binary tails, rather than adding another iodata parser. Extract raw DEFLATE boundary measurement from existing BeamZlibMemberBoundary: old zlib path still checks its two-byte header, shared raw payload measurement and four-byte trailer. No second DEFLATE/Huffman implementation or dependency.

Pinned zlib gunzip initializes gzip reset mode. zlib_nif inflate resets when EOS seen and more queued input exists; inflateEnd rejects missing EOS or remaining input. Candidate loops complete gzip members, measuring exact DEFLATE EOS before an eight-byte trailer; each bounded member is decompressed via BCL GZipStream. This rejects incomplete member/trailer and nongzip trailing bytes independently from BCL stream buffering, and concatenates all complete members.

Header framing covers magic/method/reservedflags/extra/name/comment/header-checksum field presence. CRC32/ISIZE and header CRC validation are currently delegated to BCL; their behavior has not been experimentally verified. Header CRC must be separately tested against the pending original probes before promotion. Do not claim full zlib behavior or physical allocation parity. No newly invented size budget; shared CLR/output limits remain scope caveats.

Wrapper catch covers ErlangException (including iodata badarg/system_limit), InvalidDataException, IOException, OverflowException and ArgumentException, returning the identical input object. It intentionally does not swallow CLR process-fatal exceptions or cancellation; pinned catch _:_ covers Erlang exceptions, not arbitrary CLR fatal faults. Existing prepare/load error frame arguments remain unchanged by this decompression helper.

Declared source checks cover binary/nested/improper gzip, raw FOR1 shortcut, raw list exact fallback, empty/invalid iodata/bitstring fallback, concatenated empty members, truncated EOS/trailer, trailing byte, CRC/ISIZE corruption and reserved flags. Tests are not registered, built or executed. Additional full loader successful genuineBEAM cases, corrupted header CRC, optional-header controls, gzip16 raw projection comparison and inherited zlib/ETF regression are prerequisites. Native outcomes must remain independent, never derived from candidate output.
