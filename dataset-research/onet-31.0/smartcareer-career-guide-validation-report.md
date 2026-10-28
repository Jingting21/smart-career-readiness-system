# SmartCareer O*NET 31.0 Career Guide Dataset Validation

- Validation status: **PASS**
- Validation errors: **0**
- Validation warnings: **2**
- Approved occupations: **175**
- Release: **O*NET Database 31.0 (August 2026)**
- Retrieved: **24 October 2026**

This report validates source availability only. It performs no database import and makes no claim that US education or preparation data represents Malaysian requirements.

## Pinned source hashes

| File | SHA-256 |
|---|---|
| `education.json` | `E995D6DA186E06536D7787FF22080EE392F27EBF0B59C78F14EF2F4E4AE5DFA0` |
| `education_categories.json` | `6533A4DF74750EF6144171B80D42B49E5E258D7CF09F0A4CDDA3F6AFD7791BD4` |
| `job_zone_reference.json` | `F66D665A2E507C825A71AEDB2C13BA22765E8259BC6C7FE5B3CDFD8105475A66` |
| `job_zones.json` | `FE9AE7E93DE8B1AD2404FFF8D7279547C32EB633D107E47E56C868A15B8914BB` |
| `task_ratings.json` | `6D89F4598ADB042B523C898F4ABE805B634D90E6806F1C2144B8B3A23430AF49` |
| `task_statements.json` | `D48D985C2BCF39A8760C9FFAC599CD12309D597D564856E4BB3E9759EE948545` |
| `training_and_experience.json` | `C63D830100E41235C086C14F0105B2C83F6D5B061D5C9959CEA7E3AD2841E3A1` |
| `training_and_experience_categories.json` | `4549DFBDEA97F4D7F98432FB3E896134924A4F0A56E6FC26A47D28D021DC4694` |

Each value matches the extracted file, `source-metadata.json`, the approved checksum manifest, and the corresponding entry in the pinned O*NET 31.0 archive.

## Coverage and row counts

Source rows are the complete O*NET table. Approved-career rows are restricted by exact `onetsoc_code` membership in the approved 175-career manifest. Usable rows exclude `recommend_suppress = Y` and null values where applicable.

| Dataset | Full source rows | Approved-career rows | Usable display rows | Covered careers |
|---|---:|---:|---:|---:|
| Task statements | 18,838 | 3,724 | 3,724 | 175 |
| Task ratings, all scales | 165,780 | 32,913 | 32,770 | 175 |
| Education, including certification IM | 11,495 | 2,219 | 2,177 | 172 for RL/RQ |
| Training and experience, including apprenticeship IM | 26,812 | 5,143 | 5,024 | 172 for RW/PT/OJ |
| Job Zones | 923 | 175 | 175 | 175 |

Reference-table rows:

- Education categories: 24
- Training and experience categories: 29
- Job Zone references: 4

## Typical Tasks

- Core task statements: **2,955**
- Supplemental task statements: **702**
- Missing/null task type: **67**
- Duplicate task IDs within an occupation: **0**
- Task rating joins without a matching `(occupation code, task ID)`: **0**
- Suppressed selected task ratings: **143**
  - Frequency (`FT`): 132
  - Importance (`IM`): 2
  - Relevance (`RT`): 9
- Task statements without a usable Importance rating: **69**
- Every approved occupation has at least one Core task with a usable Importance rating.

Core tasks can be ordered deterministically by usable Importance descending and then Task ID. Missing or suppressed Importance values remain missing and are never replaced.

## Education

- `RL` coverage: **142 occupations**, 1,704 selected rows, 1,678 usable rows
- `RQ` coverage: **30 occupations**, 360 selected rows, 344 usable rows
- Combined RL/RQ coverage: **172 occupations**
- Tied modal education responses: **2 occupations**
- Suppressed selected education rows across all scales: **42**

Category descriptions resolve by `(scale_id, category)`. This is required because RL and RQ category 10/11 descriptions differ. Percentages remain O*NET response values and are not Malaysian qualification requirements.

## Training and related experience

| Scale | Meaning | Selected rows | Usable rows | Covered careers | Modal ties |
|---|---|---:|---:|---:|---:|
| `RW` | Related Work Experience | 1,892 | 1,853 | 172 | 12 |
| `PT` | On-Site or In-Plant Training | 1,548 | 1,511 | 172 | 17 |
| `OJ` | On-the-Job Training | 1,548 | 1,505 | 172 | 16 |

Training descriptions resolve by `(element_id, scale_id, category)`. The additional 155 selected `IM` rows describe Job-related Apprenticeship Importance and are not merged into the three duration distributions.

## Job Zones

Every approved occupation has exactly one assignment and every assignment resolves to a pinned reference row.

| Job Zone | Approved careers |
|---:|---:|
| 2 | 3 |
| 3 | 23 |
| 4 | 113 |
| 5 | 36 |

## Missing-data occupations

Education RL/RQ and training RW/PT/OJ data are unavailable for the same three approved occupations:

- `11-9013.00` — Farmers, Ranchers, and Other Agricultural Managers
- `13-1041.00` — Compliance Officers
- `17-2111.00` — Health and Safety Engineers, Except Mining Safety Engineers and Inspectors

No values may be inferred for these occupations.

## Warnings

- Education data is unavailable for 3 approved occupations; no values may be inferred.
- Training data is unavailable for 3 approved occupations; no values may be inferred.

## Errors

- None.

## Provenance and licence

This application uses information from the O*NET 31.0 Database by the U.S. Department of Labor, Employment and Training Administration (USDOL/ETA), under the Creative Commons Attribution 4.0 International licence. O*NET® is a trademark of USDOL/ETA. SmartCareer selected a subset and performs validation specific to its approved catalogue; USDOL/ETA has not approved, endorsed, or tested these modifications.
