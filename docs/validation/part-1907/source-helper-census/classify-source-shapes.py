from collections import defaultdict
from pathlib import Path
import json,sys
out = Path(sys.argv[1])
rows=json.loads((out/"source-shapes.json").read_bytes())
groups=defaultdict(list)
def classify(c):
    kinds=tuple(c["kinds"])
    count=c["generators"]
    if count>1 and all(kind=="generate" for kind in kinds):
        if (count==2 and c["qualifiers"]==2 and
                c["patterns"]==c["sources"]==["var","var"] and
                c["output"] in {"var","tuple","atom","integer"}):
            return "direct_pair_shape" if c["context"]=="single_body" else "nested_or_sequence_pair_shape"
        return "multiple_relaxed_generators"
    if count>1 or "zip" in kinds or (count and kinds[0]!="generate"):
        return "strict_zip_binary_or_map_generator"
    if count==1:
        if c["sources"] in (["op"],["case"],["match"],["record_field"]):
            return "single_computed_source"
        if c["qualifiers"]>1:
            return "single_with_filters"
        return "single_unfiltered"
    return "only_filters"
for row in rows:
    for parent in row.get("parents",[]):
        classes=sorted(set(map(classify,parent["comprehensions"]))) or ["unknown_parent_or_rewrite"]
        for group in classes:
            groups[group].append({"module":row["beam"],"parent":parent["parent"],"arity":parent["arity"],
                "keys":parent["missing"],"unique":len(classes)==1,"epp_errors":row["errors"]})
summary={}
for key,parents in groups.items():
    exact=[item for item in parents if item["unique"] and not item["epp_errors"]]
    summary[key]={"modules":len({x["module"] for x in parents}),"parents":len(parents),
        "unique_no_epp_error_keys":sum(item["keys"] for item in exact),
        "unique_no_epp_error_parents":len(exact)}
(out/"shape-summary.json").write_text(json.dumps({"modules":len(rows),
    "epp_clean_modules":sum(not row.get("errors",1) for row in rows),"shapes":summary},indent=2)+"\n")
(out/"groups.json").write_text(json.dumps(groups,indent=2)+"\n")
print(json.dumps(summary, indent=2))
