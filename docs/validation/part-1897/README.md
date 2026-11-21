# Part 1897 scoped typed record update correctness

The direct record-to-call rewrite preserves the typed source register and its ordinal before replacing the plain destination. The permanent instruction fixture handles an input Y and an X source alias. All 86 selected cases pass in the 9,881-case catalog.

The 85-module uncached scope completed in 333.5999 seconds with 7 Equal, 78 Different, 0 Unsupported and 0 SourceRejected. prim_eval is newly exact compared with Part 1894 and all earlier exact controls stay exact. Pinned ERTS prepares all 58 changed images. Compared with Part 1894, 16 normalized functions gain equality and none lose it. Four frame distances grow and 14 shrink.

The strong assembler validator runs against all 20 modules from the earlier 43 uninitialized update operand observations. None of those errors recur, and both the installed and generated megaco_tcp_accept pass the isolated strong test. Actual loopback acceptance reaches the expected supervisor interaction and preserves the badrecord reason. The stack line remains 83 on the accepted Part 1875, Part 1892 and this candidate, versus 87 on OTP. A separately scoped source annotation proof addresses that remaining difference. Raw and normalized behavioral outcomes are retained with the launch identities.
