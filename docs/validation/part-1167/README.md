# Part 1167 — lower a closed tuple and atom case

The compiler uses a zero-slot frame for a selector call and branches in X0. It adds the tuple type after the shape test and preserves case_clause for unexpected values. Three-register argument rotations retain the operand direction used by OTP.

The intermediate full run gained byte-identical ssl_server_session_cache_db. The differential check also found two earlier instruction matches lost in lists:keyfind cases. The first shape test lost type information and the atom branch gained a redundant equality test. Part 1168 restores both properties.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Instruction comparison](function-comparison.txt)
- [Logger regression](get-primary-filter-default.txt)
- [Compiler options regression](old-options.txt)
- [Focused cases](focused.json)
