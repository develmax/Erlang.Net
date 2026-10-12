namespace Erlang.Compiler;

internal static class LocalCallValidation
{
    public static void Validate(ModuleDefinition module)
    {
        var locals = module.Functions.Select(function => (function.Name, function.Arity)).ToHashSet();
        locals.Add((FunctionResolutionNames.ModuleInfo, 0));
        locals.Add((FunctionResolutionNames.ModuleInfo, 1));
        var pending = new Stack<object>();

        void Push(object? node)
        {
            if (node is not null)
                pending.Push(node);
        }

        void PushAll<T>(IEnumerable<T> nodes) where T : class
        {
            foreach (var node in nodes)
                pending.Push(node);
        }

        foreach (var function in module.Functions)
            PushAll(function.Clauses);
        while (pending.TryPop(out object? node))
        {
            switch (node)
            {
                case Expr.Literal or Expr.Variable or Pattern.Any or Pattern.Variable or Pattern.Literal:
                    break;
                case Expr.Call call:
                    if (call.Module is null && !locals.Contains((call.Function, call.Arguments.Count)) && !AutoImportSignatures.Contains(call.Function, call.Arguments.Count))
                        throw new CompileException(
                            CompilerDiagnosticCodes.FunctionDefinition,
                            FunctionResolutionDiagnostics.Undefined(call.Function, call.Arguments.Count),
                            0
                        );
                    PushAll(call.Arguments);
                    break;
                case Expr.FunctionReference function:
                    if (!locals.Contains((function.Name, function.Arity)) && !AutoImportSignatures.Contains(function.Name, function.Arity))
                        throw new CompileException(
                            CompilerDiagnosticCodes.FunctionDefinition,
                            FunctionResolutionDiagnostics.Undefined(function.Name, function.Arity),
                            0
                        );
                    break;
                case Expr.Tuple tuple:
                    PushAll(tuple.Items);
                    break;
                case Expr.List list:
                    PushAll(list.Items);
                    Push(list.Tail);
                    break;
                case Expr.Map map:
                    Push(map.Base);
                    PushAll(map.Fields);
                    break;
                case MapField field:
                    Push(field.Key);
                    Push(field.Value);
                    break;
                case Expr.Bits bits:
                    PushAll(bits.Segments);
                    break;
                case BitSegment segment:
                    Push(segment.Value);
                    Push(segment.Size);
                    break;
                case Expr.Unary unary:
                    Push(unary.Operand);
                    break;
                case Expr.Binary binary:
                    Push(binary.Left);
                    Push(binary.Right);
                    break;
                case Expr.Apply apply:
                    Push(apply.Function);
                    PushAll(apply.Arguments);
                    break;
                case Expr.Match match:
                    Push(match.Pattern);
                    Push(match.Value);
                    break;
                case Expr.MaybeMatch match:
                    Push(match.Pattern);
                    Push(match.Value);
                    break;
                case Expr.Sequence sequence:
                    PushAll(sequence.Items);
                    break;
                case Expr.GuardAlternatives alternatives:
                    PushAll(alternatives.Items);
                    break;
                case Expr.Block block:
                    Push(block.Body);
                    break;
                case Expr.Catch caught:
                    Push(caught.Operand);
                    break;
                case Expr.Case choice:
                    Push(choice.Value);
                    PushAll(choice.Clauses);
                    break;
                case Expr.If conditional:
                    PushAll(conditional.Clauses);
                    break;
                case Expr.Fun function:
                    PushAll(function.Clauses);
                    break;
                case Expr.Maybe maybe:
                    PushAll(maybe.Items);
                    PushAll(maybe.Clauses);
                    break;
                case Expr.Receive receive:
                    PushAll(receive.Clauses);
                    Push(receive.Timeout);
                    Push(receive.After);
                    break;
                case Expr.Try attempt:
                    Push(attempt.Body);
                    PushAll(attempt.Clauses);
                    PushAll(attempt.Catches);
                    Push(attempt.After);
                    break;
                case Clause clause:
                    PushAll(clause.Patterns);
                    Push(clause.Guard);
                    Push(clause.Body);
                    break;
                case Expr.RecordConstruction record:
                    PushAll(record.Definition.Fields);
                    PushAll(record.Fields);
                    Push(record.Wildcard);
                    break;
                case RecordField field:
                    Push(field.Value);
                    break;
                case Expr.RecordAccess record:
                    Push(record.Value);
                    break;
                case Expr.RecordTest record:
                    Push(record.Value);
                    break;
                case Expr.RecordUpdate record:
                    Push(record.Value);
                    PushAll(record.Fields);
                    break;
                case RecordUpdateField field:
                    Push(field.Value);
                    break;
                case Expr.ListComprehension comprehension:
                    PushAll(comprehension.Items);
                    PushAll(comprehension.Qualifiers);
                    break;
                case Expr.BinaryComprehension comprehension:
                    Push(comprehension.Body);
                    PushAll(comprehension.Qualifiers);
                    break;
                case Expr.MapComprehension comprehension:
                    PushAll(comprehension.Fields);
                    PushAll(comprehension.Qualifiers);
                    break;
                case ComprehensionQualifier.Filter filter:
                    Push(filter.Expression);
                    break;
                case ComprehensionQualifier.Generator generator:
                    Push(generator.Pattern);
                    Push(generator.Source);
                    break;
                case ComprehensionQualifier.BinaryGenerator generator:
                    Push(generator.Pattern);
                    Push(generator.Source);
                    break;
                case ComprehensionQualifier.MapGenerator generator:
                    Push(generator.Pattern);
                    Push(generator.Source);
                    break;
                case ComprehensionQualifier.Zip zip:
                    PushAll(zip.Generators);
                    break;
                case Pattern.Tuple tuple:
                    PushAll(tuple.Items);
                    break;
                case Pattern.List list:
                    PushAll(list.Items);
                    Push(list.Tail);
                    break;
                case AliasPattern alias:
                    Push(alias.Left);
                    Push(alias.Right);
                    break;
                case MapPattern map:
                    PushAll(map.Fields);
                    break;
                case MapPatternField field:
                    Push(field.Key);
                    Push(field.Value);
                    break;
                case BitPattern bits:
                    foreach (var segment in bits.Segments)
                    {
                        Push(segment.Value);
                        Push(segment.Specification.Size);
                    }
                    break;
                default:
                    throw new NotSupportedException(FunctionResolutionDiagnostics.UnsupportedNode(node.GetType().Name));
            }
        }
    }
}
