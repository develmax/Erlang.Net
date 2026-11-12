# Remaining tuple projection schedules

The new plan groups two or more fields with a single later consumer. It keeps the original tuple in a stack slot and reads the grouped fields into X registers before that consumer. Other fields may be extracted early. The fixture also verifies slot reuse and frame trimming when the intervening call consumes the last use of a saved argument.

asn1ct_imm:combine_imms/2 still needs additional lowering. The pinned OTP source at lib/asn1/src/asn1ct_imm.erl lines 1630–1635 contains two matched call results and three list append expressions. The C# planner handles nested append operands but the checked call sequence expects calls or a constant at its end. The disassembly shows a nine-slot frame from C# and a two-slot frame from OTP. Before extending deferral to fields read at different calls, admit append expressions used as a binding and as a tail result into the same checked plan.

- [Release and generated instructions](asn1-mixed-projection.txt)

The section payload audit also identifies a near-parity module, xmerl_xpath_lib. Its sole normalized instruction difference is primary_expr/2. OTP uses a three-slot frame and emits apply_last in both dynamic remote call branches. The C# image uses apply with a six-slot frame and later deallocation. The selector uses atom dispatch and a tagged tuple alternative. This is a useful whole-module target for the general dynamic tail call and case planner.

- [xmerl function comparison](near-parity-functions.txt)
- [xmerl instruction operands](xmerl-primary-expr.txt)
