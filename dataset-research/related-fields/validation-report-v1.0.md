# SmartCareer related fields pilot validation report

## Dataset identity

- Dataset version: 1.0
- Review date: 2026-10-29
- Approved career catalogue: O*NET 31.0 SmartCareer 175-career manifest
- Pilot scope: 50 careers and 53 career-field candidate mappings
- Decision status: all 53 mappings are **In Review**; independent human approval has not been claimed

## Validation result

Automated dataset validation passed on 2026-10-29:

- 5 validation tests passed
- 0 validation tests failed
- all pilot occupation codes occur in the approved 175-career manifest
- all 10 SmartCareer categories are represented by exactly 5 pilot careers
- all 53 career-field keys are unique
- all 53 CIP-SOC pairs occur in the pinned NCES crosswalk evidence subset
- NEC and CIP code formats are valid
- evidence URLs use absolute HTTPS URLs
- provenance, review status, confidence, relationship type and review date are present
- all regulated rows include regulator evidence and an explicit registration, accreditation or practice caveat

## Coverage by category

| SmartCareer category | Pilot careers | Candidate mappings |
|---|---:|---:|
| Agriculture, Food, Hospitality and Tourism | 5 | 5 |
| Arts, Media and Design | 5 | 5 |
| Built Environment, Transport and Logistics | 5 | 5 |
| Business, Finance and Management | 5 | 6 |
| Digital, Data and Computing | 5 | 7 |
| Education and Social Services | 5 | 5 |
| Engineering and Manufacturing | 5 | 5 |
| Health and Life Sciences | 5 | 5 |
| Law, Government and Public Services | 5 | 5 |
| Science, Research and Environment | 5 | 5 |
| **Total** | **50** | **53** |

## Review and confidence summary

| Measure | Count |
|---|---:|
| Evidence-reviewed candidate mappings | 53 |
| In Review | 53 |
| Approved | 0 |
| Rejected | 0 |
| Needs Revalidation | 0 |
| High confidence | 47 |
| Medium confidence | 6 |
| Low confidence | 0 |
| Professionally regulated rows | 15 |

Evidence review means that the occupation is in the approved manifest, the CIP-SOC pair was checked against the official crosswalk, the NEC code was checked against NEC-2020, and the recorded URLs and caveats passed validation. It does not mean that a Malaysian academic or professional reviewer has approved the relationship.

## Unresolved mappings requiring human review

The six medium-confidence candidates require particular attention:

1. Agricultural Managers to Agricultural Business and Management: NEC 0811 is broader than the named field.
2. Web Developers to Web Development: the Malaysian programme may be classified under NEC 0612, 0613 or another ICT field.
3. Bioengineers and Biomedical Engineers to Biomedical Engineering: NEC 0719 is a broad engineering fallback and EAC classification must be verified.
4. Dietitians and Nutritionists to Dietetics: NEC 0915 is broader than dietetics and must be verified against the selected MQR programme.
5. Compliance Officers to Regulatory Affairs: the SOC is broad and the proposed NEC 0414 alignment is not industry-specific.
6. Forensic Science Technicians to Forensic Science: NEC 0531 is a broad chemistry alignment and may differ across Malaysian programmes.

Additional human work remains for all 53 mappings:

- confirm the exact MASCO code and scope;
- select and verify a current MQR-accredited Malaysian programme where programme examples will be shown;
- obtain an independent subject-matter review before changing status to Approved;
- recheck current professional requirements for the 15 regulated rows;
- confirm whether broad NEC mappings should be retained, changed or left blank.

The remaining 125 approved SmartCareer careers are outside this pilot and intentionally have no mapping in version 1.0.

## Missing or conflicting evidence

- NEC-2020 does not provide equally specific labels for several CIP specialisms. The CSV records the broader NEC code and explains the limitation.
- O*NET detailed occupations can be more specific than their SOC 2018 parent. This pilot uses the seven-character SOC parent for NCES crosswalk evidence and retains the full O*NET-SOC code in the mapping.
- CIP-SOC relationships are qualitative US educational relationships. They are candidate evidence, not Malaysian accreditation evidence or degree requirements.
- Live MQR records were not bulk-scraped or mirrored. Exact programme identifiers must be added only after a human checks an official record.
- Official university pages were used only for selected corroborating examples. They do not establish national or exclusive pathways.

## Licensing limitations

- O*NET 31.0 content requires CC BY 4.0 attribution and a modification notice.
- NCES/IES public information is recorded with source and version attribution; the crosswalk's published limitations must remain visible.
- DOSM/MASCO reuse is subject to Malaysian Government Open Data Terms of Use 1.0, including attribution and non-endorsement requirements.
- No explicit bulk-redistribution licence was located for NEC, MQR or university programme pages. The pilot therefore stores short factual identifiers, evidence summaries and official URLs rather than copying substantial content or mirroring registers.

## Publication gate

Version 1.0 is suitable for human review and Step 2.27C design work. It is not approved for direct student-facing display. A mapping may move to Approved only after an authorised reviewer confirms its Malaysian programme evidence, NEC/MASCO alignment and any professional-regulation caveat.
