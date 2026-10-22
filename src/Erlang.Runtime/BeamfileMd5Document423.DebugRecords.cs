namespace Erlang;

internal sealed partial class BeamfileMd5Document423
{
    private void ParseDebug()
    {
        if (!Optional(BeamfileMd5Names423.Debug, out var payload)) return;
        var cursor = new BeamCursor(payload);
        if (cursor.ReadInt32() != 1) return;
        int count = cursor.ReadInt32(), terms = cursor.ReadInt32();
        Require(count >= 0 && terms >= 0);
        int last = int.MaxValue;
        while (count > 0 || terms > 0)
        {
            cancellation.ThrowIfCancellationRequested();
            var arguments = ReadMetadataOp(cursor, BeamfileMd5Names423.CallOpcode, 2);
            Require(arguments[0].Tag == BeamWire.UnsignedTag && arguments[0].Value <= int.MaxValue);
            int entry = (int)arguments[0].Value;
            if (entry == BeamfileMd5Names423.DebugFrameSize)
            {
                Require(count > 0);
                count--;
                last = entry;
                var frame = arguments[1];
                Require(frame.Tag == BeamfileMd5Names423.NilArgument
                    || frame.Tag == BeamOperandParameters.AtomTag && frame.Literal!.Equals(Term.A(BeamfileMd5Names423.Entry))
                    || frame.Tag == BeamWire.UnsignedTag && frame.Value <= int.MaxValue);
                continue;
            }
            Require(entry >= last);
            last = entry;
            Require(arguments[1].Tag == BeamWire.UnsignedTag);
            int extra = unchecked((int)(long)arguments[1].Value);
            Require(extra >= 0 && extra <= terms);
            Require(extra <= arguments.Length - 2);
            terms -= extra;
            if (entry == BeamfileMd5Names423.DebugVariables)
            {
                Require(extra % 2 == 0);
                for (int index = 0; index < extra; index += 2)
                {
                    var name = arguments[index + 2];
                    Require(name.Tag == BeamWire.IntegerTag || BinaryLiteral(name));
                    var value = arguments[index + 3];
                    Require(value.Tag is BeamWire.IntegerTag or BeamOperandParameters.AtomTag
                        or BeamfileMd5Names423.NilArgument or BeamOperandParameters.XTag
                        or BeamOperandParameters.YTag or BeamfileMd5Names423.LiteralArgument);
                }
            }
            else if (entry == BeamfileMd5Names423.DebugCalls)
            {
                int expected = 0;
                for (int index = 0; index < extra; index++)
                {
                    var argument = arguments[index + 2];
                    if (argument.Tag == BeamWire.UnsignedTag)
                    {
                        Require(expected == 0);
                        int arity = unchecked((int)(long)argument.Value);
                        if (arity > BeamWire.MaximumArity)
                        {
                            arity -= BeamWire.MaximumArity + 1;
                            expected = 1;
                        }
                        else expected = 2;
                        Require(arity >= 0 && arity <= BeamWire.MaximumArity);
                    }
                    else if (argument.Tag == BeamOperandParameters.AtomTag)
                    {
                        Require(expected > 0);
                        expected--;
                    }
                    else
                    {
                        Require(BinaryLiteral(argument));
                        if (expected > 0) expected--;
                    }
                }
                Require(expected == 0);
            }
            // Unknown entry types deliberately ignore their marshaled terms.
        }
    }

    private static bool BinaryLiteral(BeamfileMd5Argument423 argument)
        => argument.Tag == BeamfileMd5Names423.LiteralArgument && argument.Literal is BitString { IsBinary: true };

    private void ParseRecords()
    {
        if (!Optional(BeamfileMd5Names423.Records, out var payload)) return;
        var cursor = new BeamCursor(payload);
        Require(cursor.ReadInt32() == 0);
        int count = cursor.ReadInt32();
        cursor.ReadInt32(); // Declared field allocation count is not checked by native reader.
        Require(count >= 0);
        for (int index = 0; index < count; index++)
        {
            cancellation.ThrowIfCancellationRequested();
            var arguments = ReadMetadataOp(cursor, BeamfileMd5Names423.CallLastOpcode, 3);
            Require(arguments[0].Tag == BeamOperandParameters.AtomTag);
            Require(arguments[1].Tag == BeamOperandParameters.AtomTag
                && (arguments[1].Literal!.Equals(Term.A(BeamfileMd5Names423.True))
                    || arguments[1].Literal!.Equals(Term.A(BeamfileMd5Names423.False))));
            Require(arguments[2].Tag == BeamWire.UnsignedTag);
            int extra = unchecked((int)(long)arguments[2].Value);
            Require(extra >= 0 && extra % 2 == 0 && extra <= arguments.Length - 3);
            for (int field = 0; field < extra; field += 2)
            {
                Require(arguments[field + 3].Tag == BeamOperandParameters.AtomTag);
                Require(arguments[field + 4].Tag is BeamWire.UnsignedTag or BeamOperandParameters.AtomTag
                    or BeamfileMd5Names423.NilArgument or BeamWire.IntegerTag or BeamfileMd5Names423.LiteralArgument);
            }
        }
    }
}
