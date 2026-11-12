value(X) when is_integer(X) ->
    {integer, X};
value(X) ->
    {other, X}.
