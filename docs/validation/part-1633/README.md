# Part 1633

The compiler lowers append with a proper literal prefix of up to four members to cons construction, following OTP 29.1.1 sys_core_fold. The fixture checks prefix lengths one, four and five and agrees with OTP byte for byte. The OTP VM confirmed proper and improper tails. Three focused tests passed. [Disassembly](disassembly.txt) · [Tests](focused.txt) · [VM outcome](outcomes.txt).

All 1,366 release sources were accepted and compiled. 394 images matched byte for byte, including ELDAPv3 and Observer. The remaining 972 images loaded in OTP. The change affected 199 images. The Code size gap decreased in 143 and increased in 51. Five had the same gap. The aggregate absolute gap decreased by 2,452 bytes. edoc_layout:local_label/1 matched after label normalization with no losses. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

In httpd_file, the literal table is now byte-identical and the Code size gap narrowed from 48 to 42 bytes. Frame allocation and the remaining append sequence still differ. The full tests and readiness regeneration were outside this run. [httpd_file disassembly](httpd-file-disassembly.txt).
