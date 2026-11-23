# Part 2010 PR 143 closure table order

The published platform failure and [local reproduction](prior-failure.json) came from a fixture that expected table ordering to decline valid out-of-order closure references. Pinned OTP 29.1.1 assigns a closure index at first use and reuses it for later references to that label. The [executed OTP probe](otp-lambda-table.txt) verifies the indices and both closure table row orders through beam_dict:lambda/3 and lambda_table/1.

The updated fixture checks that references [1,1,0] become [0,0,1], and that the matching FunT rows follow the same permutation. The missing-reference and repeated-single-root rejection controls still run. This part edits the fixture only and uses no new dependency or license.

The [test project build](build.json), [changed-file layout](style.txt), and [13 source plans](source-admission.json) passed. [Three cases passed](focused.json), including the closure-order fixture and the whole-module and installed-release controls. This recovers one additional published platform failure, for 25 of 60 recovered across the focused retakes and 35 remaining in that list. The hosted head remains afd86525. Full CI, corpus acceptance and readiness regeneration remain pending.
