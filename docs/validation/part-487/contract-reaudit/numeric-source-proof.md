# Native numeric BIF source proof

The complete native BIF audit covers abs/1, float/1, round/1, trunc/1, floor/1 and ceil/1. Together with the size/function predicates, it closes nine contracts on the supported valid Erlang term model. FloatTerm admits only finite IEEE754 binary64 values. This is not a whole compiler, original erlang module or physical allocation-limit acceptance.

| Operation | Integer partition | Finite float partition | Every other term |
|---|---|---|---|
| abs | Preserve nonnegative input; construct exact magnitude for negative input | Preserve positive input; allocate fabs result for <=0, including both zeros | badarg |
| float | Exact through53 significant bits; otherwise nearest, ties to even; reject exponent/rounded overflow | Preserve original object and all bits | badarg |
| round | Preserve input | Nearest integer, ties away from zero; exact integer result | badarg |
| trunc | Preserve input | Toward zero; exact integer result | badarg |
| floor | Preserve input | Mathematical floor; exact integer result | badarg |
| ceil | Preserve input | Mathematical ceiling; exact integer result | badarg |

Pinned erl_bif_guard.c implements exactly these type branches. Its double_to_integer decomposition by powers of the native digit base denotes the exact integer represented by the rounded finite double. Math.Round with AwayFromZero, Math.Truncate, Math.Floor and Math.Ceiling followed by BigInteger conversion denote the same integers. There is no narrowing to Int32/Int64. Integer inputs bypass floating conversion entirely, including arbitrarily wide integers.

Pinned big.c big_to_double and Integer.TryToDouble use equivalent partitions: at most53 significant bits are exact; widths above1024 fail; otherwise retain53 significant bits and compare the discarded remainder with half a unit, incrementing for greater-than-half or an odd retained mantissa at an exact tie. Scale by the discarded bit count, reject nonfinite carry, then apply the original sign. The conversion introduces no iterative digit-rounding error. Overflow raises badarg rather than leaking a host infinity or an arithmetic exception.

Numeric BIFs read their input values without invoking a guest function or modifying process state. Invalid type/overflow paths use the actual registered native-call error machinery. Complete argument/error_info/caller stacks are compared at default/0/1/2/8 depths, in an identical original compiled worker/support context. Allocation ceilings, GC scheduling costs and physical word layout are excluded from the compatibility definition, rather than replaced with fabricated measurements.

## Identity corrections and controls

The old abs implementation always constructed an Integer/FloatTerm result. Source review and original observations require preserving an already nonnegative integer and a strictly positive float. The fix returns the actual input for these branches. Both positive and negative floating zero still construct the fabs result, matching the source <=0 branch; float/1 also preserves its input float object.

The first word-identity acquisition lacked the native erts_debug:same entry and returned undef. That failed preparation is not an abs counterexample. After admitting the existing original same facet, the controlled old-source run produced two false-versus-true observations. Disassembly then distinguished the actual positive-float BIF defect from an independently constant-folded big-integer observation: OTP had replaced the latter with two reads of the same integer constant, so no abs call remained there.

The interpreter formerly created a new Integer for every compact integer operand read. A thread-safe cache owned by BeamLinkedCode now preserves the code image's immutable integer constants. The main interpreter, receive reader and static failure-argument reader use the same cache. Dynamic results are not interned and no global cache is introduced; the cache's values are bounded by that code's constant operands and its lifetime follows the code object. The explicit integer_constant_same observation retains the original two-move identity control. This fixes the discovered path; it does not assert every BEAM metadata/loader contract complete.

## Runtime entry, rather than constant-folded answers

Earlier literal-only numeric observers allowed OTP to replace successful operations with constant returns. Those captures remain historical observations, not proof that our native BIF bodies executed. The final observers call through an opaque function atom obtained by a private dictionary put/erase barrier. It runs in a fresh worker, removes its private key, and returns the actual argument. The target function is resolved through runtime apply. Original disassembly must contain dynamic apply instructions; dispatch-proof.etf is retained with the acquisition receipt.

The final two families contain335 BIF cases and one separate integer-constant control:336 values and1680 full stack outcomes,2016 complete comparisons. The numeric portion includes signed zero, smallest subnormals, half-way neighbors, ties of both signs, widths around53/63/1024, largest finite float conversion, rounded overflow, arbitrary-width integers and invalid nonnumeric terms. These are execution witnesses for the source/type/algorithm proof, not a claim that sampling proves every bit pattern.

Current-source guards bind all dependencies in bif-source-audit.json. Historical nonmatching native source-plan bindings are explicitly retained and listed; refreshing an exact current binding/seal is not a new NIF ABI/platform acceptance. Complete NIF compatibility remains among the recorded limitations.
