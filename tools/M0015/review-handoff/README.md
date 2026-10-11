# M0015 Review Handoff

This package supports the project owner's independent, policy-blinded assessment of frozen BORINGness candidates. It contains no judgments and no token-to-policy mapping.

Start with `ASSESSOR-INSTRUCTIONS.md`. Verify `SHA256SUMS.txt` before use. Source archives are in `source-archives/`. Fill `JUDGMENT-TEMPLATE.json`, validate the completed copy, and return it to `M0015-review/submissions/M0015-judgments.json`.

`validate-submission.ps1` validates only the submission structure and frozen token coverage; it does not perform semantic review or unblind policy membership.
