# Part 1991 local projected frame checkpoint

Pin Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The aggregate observed ledger remains 539 Equal, 827 Different, 0 Unsupported and 0 SourceRejected. The measurements here are scoped uncached compiles of previously differing files.

| Scope | Equal | Different | Unsupported | SourceRejected | Changed BEAMs prepared | Normalized gains / losses | Corpus time |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| [Independent projected frames](p135-independent-projected-branch-frames/status.json) | 0 | 46 | 0 | 0 | 14 | 6 / 0 | 101.135 s |
| [Tuple entry and first-call heap regions](p137-projected-input-and-heap-regions/status.json) | 0 | 72 | 0 | 0 | 30 | 18 / 0 | 142.387 s |

The independent arms close ct_util:remove_space/2 and lcnt:toggle_category/2. The common shape and argument plan closes socket_registry:handle_unexpected_msg/2, disk_log:notify_owners_wrap/1, mnesia_frag:table_info/4 and other bodies. The normalized comparisons use each file's latest earlier preserved image. No whole-file promotions or losses were observed in these scopes.

Three managed regressions and 89 paired native fixture observations passed in the final scope. Released native Common Test table parsing, syntax-tool proper and improper abstractions, and disk log wrap notifications matched. The wrap notification ETF SHA-256 is FF5C881D121E3CDC93123AA866DF6D656C8AC918C4F6DD86B88E9AF2F29BE1EA. The exact final source files are bound by [source transfer](source-transfer-2137.json).

The initial untagged build produced CS0119 from name resolution between GeneralEmitter.Pattern and Erlang.Pattern.Variable. The joint untagged/heap retake qualifies the syntax type. The [initial log](2136-initial-build-failure.log) is preserved. The full acceptance corpus, full repository regression, readiness regeneration and commit are still pending.
