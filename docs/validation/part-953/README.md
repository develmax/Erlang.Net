# Part 953 map updates and catch debug forms

Emit the OTP map update form with its base expression and the annotation of the update operator. Catch handlers now distinguish implicit throw from explicit classes and attach an omitted stack variable to the last annotation of the reason pattern.

The fixture compares decoded Dbgi function forms against OTP 29.1.1 for chained map updates, implicit throw, explicit class without stack and explicit stack. All three focused checks passed. [Reference forms](reference.txt) and [focused checks](focused.json).

The full Windows corpus has 223 Equal, 1099 Different, 37 Unsupported and 7 SourceRejected. Dialyzer_cl_parse and typer generate BEAM. The typer Dbgi and CInf chunks match the release bytes. Three eunit modules move to later debug form failures. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
