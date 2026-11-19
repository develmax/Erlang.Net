# Sibling tuple and list construction

This patch follows constructor-singletons.patch on frozen Part 1847. Pinned Windows reference OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5 and SDK 10.0.400. No external dependency added.

The pinned Core and SSA dumps show that an ordinary nested tuple/cons keeps direct field order. A source append is evaluated as a Core let before the enclosing constructor. sys_core_fold replaces the append of a known proper one-element prefix by a cons in that same Core let. The precg dump confirms the X destinations and initial live count, including reuse of a dying right-hand input when no earlier register is available.

The implementation uses the existing register and heap planner for a flat tuple and a flat list that feed one returned tuple. It keeps direct sibling order and models a known one-element append as a prior scheduled binding. A dynamic prefix still invokes the existing append BIF and returns its ordinary badarg on an invalid prefix.

A 14-module build took 5.83 seconds after a clean bounded compiler build. The baseline had 4 Equal and 10 Different. The candidate has 4 Equal, 10 Different, 0 Unsupported and 0 SourceRejected. It gains three complete normalized bodies and loses zero. Those are megaco_sup:worker_spec/2 and megaco_tcp_sup:sup_spec/2 and worker_spec/3. Only Code and the derived Attr changed in the two affected images. Neither became byte-equal. In megaco_tcp_sup, the remaining mismatches are in init/1 and which_child/2, where call-result placement and stack cleanup are separate from sibling construction.

Pinned ERTS produced identical outcomes for six fixture cases, including an improper tail and the ordinary badarg for a non-list prefix. It loaded the reference and generated images of both changed modules and returned equal supervisor specs from init([]). The permanent regression passes the candidate and fails the preceding constructor-singleton compiler at the targeted assertion. SDK layout passes. The full corpus remains for the parent integration gate.

Transferable patch sibling-constructor-allocation.patch. Raw evidence in cohort.json, normalized.terms, megaco_tcp_sup.core, megaco_tcp_sup.corefold, megaco_tcp_sup.ssaopt, megaco_tcp_sup.precodegen, run.log, release-erts.terms and baseline-test.stderr.log.

