# Part 1208 — generate images for the three previously rejected sources

- test_server_node uses the legacy atom table and zlib-compressed literal table selected by r26. The generated AtU8 has 190 entries and the LitT payload decompresses to 1,404 bytes. This covers the table formats; full r26 code generation parity remains open.
- error_handler accepts no_module_opt. The current backend does not run the interprocedural type analysis or receive optimization disabled by the OTP option.
- merl_transform uses a preprocessor-only MERL_NO_TRANSFORM definition in its Windows release profile to bootstrap without running itself. The release imports merl:quote/2 and does not record a corresponding definition in CInf.

The full Windows OTP 29.1.1 run generated all 1,366 images. It recorded 357 Equal and 1,009 Different, with no rejected sources. All three newly generated images differ from the release. The OTP loader accepted all differing image pairs. ELDAPv3 remained Equal.

- [Full corpus](otp-corpus.json)
- [Three source admission results and ELDAPv3](admission.json)
- [OTP loader](loader.txt)
- [Focused tests](focused.json)
- [Opcode ranking](ranking.txt)
