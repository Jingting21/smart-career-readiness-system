# SmartCareer approved 175-career manifest validation

## Result

- Validation status: **PASS**
- Validation issues: **0**
- Approved occupation rows: **175**
- Unique occupation codes: **175**
- SmartCareer categories: **10**
- Approved replacements: **8**
- Display-title adaptations: **21**
- Rows retaining a specific review flag: **11**
- Missing source descriptions: **0**
- Rows without 10 usable essential IM ratings: **0**
- Rows without 25 usable transferable IM ratings: **0**
- MASCO mappings independently verified: **0**
- Previous manifest modified: **No**

## Validation checks

Every selected occupation was matched by OccupationCode against the pinned O*NET 31.0 occupation data. SourceTitle was compared exactly with the O*NET title. Descriptions were checked for non-empty values. Skill coverage used IM rows not marked recommend_suppress=Y or not_relevant=Y.

No data validation issues were found.

## Category counts

| SmartCareer category | Actual | Expected | Result |
|---|---:|---:|---|
| Digital, Data and Computing | 20 | 20 | Pass |
| Engineering and Manufacturing | 25 | 25 | Pass |
| Business, Finance and Management | 25 | 25 | Pass |
| Health and Life Sciences | 20 | 20 | Pass |
| Science, Research and Environment | 15 | 15 | Pass |
| Education and Social Services | 15 | 15 | Pass |
| Arts, Media and Design | 15 | 15 | Pass |
| Law, Government and Public Services | 15 | 15 | Pass |
| Built Environment, Transport and Logistics | 15 | 15 | Pass |
| Agriculture, Food, Hospitality and Tourism | 10 | 10 | Pass |
| **Total** | **175** | **175** | **Pass** |

## Approved replacements

| Removed code | Removed source title | Replacement code | Replacement source title |
|---|---|---|---|
| 29-1161.00 | Nurse Midwives | 29-9099.01 | Midwives |
| 25-1032.00 | Engineering Teachers, Postsecondary | 25-2011.00 | Preschool Teachers, Except Special Education |
| 25-1072.00 | Nursing Instructors and Teachers, Postsecondary | 25-4022.00 | Librarians and Media Collections Specialists |
| 11-9199.01 | Regulatory Affairs Managers | 13-1199.04 | Business Continuity Planners |
| 25-1042.00 | Biological Science Teachers, Postsecondary | 25-2056.00 | Special Education Teachers, Elementary School |
| 11-9199.02 | Compliance Managers | 19-5011.00 | Occupational Health and Safety Specialists |
| 11-3061.00 | Purchasing Managers | 13-2053.00 | Insurance Underwriters |
| 11-3131.00 | Training and Development Managers | 13-2041.00 | Credit Analysts |

All replacement occupations have a source description, 10 usable Essential Skill importance ratings, and 25 usable Transferable Skill importance ratings.

## Title provenance

- SourceTitle is the exact O*NET 31.0 occupation title.
- Title is the approved SmartCareer display title.
- For 21 rows, Title differs from SourceTitle.
- OccupationCode remains the O*NET-SOC identifier.
- MalaysianMappingStatus remains unverified for every row.

## Review flags

Specific review flags remain on specialised, regulated, jurisdiction-sensitive, or graduate-suitability cases identified during Steps 2.8 and 2.9. A display-title adaptation does not remove the underlying review requirement.

## Checksums

- Pinned O*NET archive SHA-256: `E88360B2D2375D15107B4F1C3DD7FEC780D792D051B37A6A81FE8F1328AF47AA`
- Previous manifest SHA-256: `D1DCAFF3D0A9305BCEF1E4CD123784629D41FB67C43E48266647A3022366C1D4`
- Approved manifest SHA-256: `609579E78F6CA96C9AC95F70A53817F830244AA5010319F5468624B7E454200A`
