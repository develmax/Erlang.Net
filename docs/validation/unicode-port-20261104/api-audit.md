# Pinned Unicode API inventory

| API | Binding | Reviewed contract |
| --- | --- | --- |
| `bin_is_7bit/1` | Native | Shared native primitive; upstream wrapper remains a native binding |
| `bom_to_encoding/1` | C# | Ordered BOM prefixes or exact encoding aliases; original function_clause arguments and source location |
| `category/1` | C# | Inclusive integer codepoint guards, ASCII fast paths and exact pinned Unicode property/category dispatch |
| `characters_to_binary/1` | C# | Wrapper/trap dispatch, encoding aliases, exact partial conversions and ml_map residual structure; fake-stack versus error_with_info distinction |
| `characters_to_binary/2` | Native | Shared native primitive; upstream wrapper remains a native binding |
| `characters_to_binary/3` | C# | Wrapper/trap dispatch, encoding aliases, exact partial conversions and ml_map residual structure; fake-stack versus error_with_info distinction |
| `characters_to_binary_int/2` | C# | Wrapper/trap dispatch, encoding aliases, exact partial conversions and ml_map residual structure; fake-stack versus error_with_info distinction |
| `characters_to_list/1` | C# | Wrapper/trap dispatch, encoding aliases, exact partial conversions and ml_map residual structure; fake-stack versus error_with_info distinction |
| `characters_to_list/2` | Native | Shared native primitive; upstream wrapper remains a native binding |
| `characters_to_list_int/2` | C# | Wrapper/trap dispatch, encoding aliases, exact partial conversions and ml_map residual structure; fake-stack versus error_with_info distinction |
| `characters_to_nfc_binary/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `characters_to_nfc_list/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `characters_to_nfd_binary/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `characters_to_nfd_list/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `characters_to_nfkc_binary/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `characters_to_nfkc_list/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `characters_to_nfkd_binary/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `characters_to_nfkd_list/1` | C# | Streaming normalization; pinned unicode_util, flattened list output or 200-grapheme binary rows; error residual and badarg source metadata |
| `encoding_to_bom/1` | C# | Ordered BOM prefixes or exact encoding aliases; original function_clause arguments and source location |
| `is_id_continue/1` | C# | Inclusive integer codepoint guards, ASCII fast paths and exact pinned Unicode property/category dispatch |
| `is_id_start/1` | C# | Inclusive integer codepoint guards, ASCII fast paths and exact pinned Unicode property/category dispatch |
| `is_whitespace/1` | C# | Inclusive integer codepoint guards, ASCII fast paths and exact pinned Unicode property/category dispatch |
