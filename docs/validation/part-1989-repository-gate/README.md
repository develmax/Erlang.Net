# Part 1989 repository gate

The linked worktree passed `tools/validate.ps1 -Part All -TestRuntimeVersion 10.0.11` in 4819.69 seconds. The source archive is 7e9378fd62eabb209eb3cce1f6701afc526f4ebbefd29ec0aa5b73b0594a36a5. The transfer verifies 1,330 source paths. [Source transfer](source-transfer.json)

- All 10,410 managed tests passed. [Managed report](tests.json)
- Original native catalog, package consumers and command-line validation completed. [Raw validator output](validation.stdout.log)
- Windows OTP byte parity records 513 Equal, 853 Different, 0 Unsupported and 0 SourceRejected. Compiler validation added 48 exact normalized bodies and recovered five historical frame widths. [Compiler outcomes](../part-1989/README.md)
