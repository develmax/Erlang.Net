# Part 1988 repository gate

The active local worktree passed `tools/validate.ps1 -Part All -TestRuntimeVersion 10.0.11` in 4166.01 seconds. The compiler source archive is de64a654c0e264ef6b3a907adcc6c8eaefef76090ef87d6cad16388a55a32c5f. The transfer checked 1,304 source paths and advanced two audited source hashes. [Source transfer](source-transfer.json)

- All 10,397 managed tests passed. [Test report](tests.json)
- Required native catalog, fixture, tool, example and package gates ran in the complete repository validator. [Raw output](validation.stdout.log)
- The accepted Windows release has 513 Equal, 853 Different, 0 Unsupported and 0 SourceRejected. All 51 changed images prepared. The raw corpus records one new bytewise match, ct_config_xml. [Compiler acceptance](../part-1988/README.md)
