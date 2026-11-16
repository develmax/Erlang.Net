# Part 1318

Tail-call clauses with distinct integer keys use select_val. Local calls share a branch when the target, arity, positional moves and constants agree. The branch is placed according to the smallest selector key that reaches it, matching OTP block order.

The focused probe compares complete instructions with OTP 29.1.1. It tests a negative key, two keys sharing a block, reordered arguments and function_clause for an unmatched integer and a float. In core_parse:yeccgoto_anno_atom/7, the table and call blocks match after normalizing labels and the selector type annotation. The reference parser obtains that selector from its state stack through hd/1.

- [Focused command](check-integer-tail-dispatch.ps1)
- [Instruction and execution results](probe.txt)
- [Release parser function comparison](core-parse.txt)

The full Windows corpus produced all 1,366 images with 363 Equal and 1,003 Different. The Code size gap shrank in ten changed images and grew in seven. OTP accepted every differing image. The instruction audit found two new matches after label normalization and 200 when also ignoring integer selector type annotations.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Function comparison](normalized-functions.txt)
- [Function comparison with selector type normalization](selector-normalized-functions.txt)
- [OTP loader](loader.txt)
