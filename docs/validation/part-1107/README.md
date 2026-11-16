# Part 1107 — rank first-call continuations in the oversized-frame group

The scan uses the same oversized-frame and entry-copy checks as part 1105. It keeps only functions whose first ordinary call is preceded by register moves and line directives after the initial copies. It distinguishes a direct continuation from an immediate copy of X0 into a Y register. For saved results, the scan skips further moves and line directives to classify the next operation.

| First-call continuation | Files | Functions | Copies read only before the call |
| --- | ---: | ---: | ---: |
| Save X0, then test tuple | 359 | 846 | 1,228 |
| Direct move | 249 | 456 | 610 |
| Direct tuple element extraction | 99 | 305 | 393 |
| Save X0, then test exact equality | 180 | 290 | 369 |
| Save X0, then heap check | 54 | 124 | 210 |

A file can occur in several rows. The counts cover instruction shapes and do not identify how many functions a particular source pattern can fix.

In xmerl_xsd:form_default/3, the generated code preserves two inputs and spills the result, then repeats the same record shape test and field extraction on successive clauses. OTP allocates a zero-slot frame, checks the tagged tuple directly in X0 and extracts the field into X0 once. The first clause guards the field with is_list and tail-calls list_to_atom. The second returns the projected field and the fallback returns a literal. This example motivates first-call result dispatch in registers and sharing adjacent patterns that project the same tuple field.

- [Reproducible scan](first-call-continuations.eval)
- [Full ranking](first-call-continuations.txt)
- [OTP and generated instructions for form_default/3](form-default.txt)
- [Corpus used](../part-1106/otp-corpus.json)
