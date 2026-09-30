# Weekly workbook date validation

Weekly games are grouped beneath date headers, positioned before or in the Favorite column. Blank rows between headers and games are allowed. Headers use English calendar dates, for example `Friday, September 18, 2026`.

A weekday must agree with its calendar date. `Friday, September 19, 2026` is invalid because September 19 is Saturday. Correct the intended date in Excel; the importer does not guess whether the weekday or the numeric date is wrong.

Non-empty invalid date headers now reject the entire upload with HTTP 400. Blank padding is allowed; placeholders such as `TBD` are not valid date headers. The admin screen shows the offending cell and header text. A game without a preceding valid date also rejects the upload instead of using today's date. Parsing completes before storage is invoked, so an invalid upload does not replace either the existing workbook or its parsed JSON.

Valid re-uploads still replace the selected week's data. Existing stored data is not automatically repaired by this code change; a corrected workbook must be uploaded when old data is already wrong.

Regression coverage includes the mismatched Week 3 headers, date headers in the Favorite column after blank rows, missing dates, HTTP status/error propagation, admin error rendering, and Azurite byte-for-byte preservation of an existing week after a rejected upload. Browser coverage mocks the 400 response; the .NET Azurite test exercises the real parser, upload function, client, and storage.
