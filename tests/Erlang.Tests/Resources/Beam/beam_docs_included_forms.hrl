-moduledoc "Header module docs.".
-doc "Header callback docs.".
-callback handle(atom()) -> atom().
-doc "Header function docs.".
-spec marked(atom()) -> atom().
marked(X) -> X.
-spec plain(atom()) -> atom().
plain(X) -> X.
