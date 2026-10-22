# Static apply3 permanent behavior controls

Three new test files add one HOST. Five small Erlang modules are compiled and executed through the existing GeneratedModuleCompiler and public runtime actors. The tests assert behavior for qualified/auto/imported-erlang calls, local shadow, foreign import and suppression, argument effects/order once, actual tail/non-tail source frames, and improper/dynamic native argument validation. All independent groups aggregate failures.

There are no reflection, exact AST or production implementation-mirroring assertions. The foreign import uses a public registry sentinel to prove the actual selected callee and exact arguments. Captured full error stacks are returned; exact native top frame metadata and relevant compiled continuation frames are checked. No normalizer or frame projection is used to compare a native capture.

Current conservative source guard declines direct lowering beyond255 arguments; this source boundary is hash-bound, while large256/1024 runtime controls are not claimed. This is source-only, unbuilt and unrun. Root owns integration/build/focus/full validation; no Main or runtime file was edited.

Register once using copy-plan.json. No csproj resource addition is required because source/basename constants reside separately from test execution code.
