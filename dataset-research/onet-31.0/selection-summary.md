# SmartCareer 175-Career Selection Manifest Review

## Status

This is a proposed selection manifest for review. It is not seed data and has not been imported into SmartCareer.

- O*NET release: 31.0 (August 2026)
- Retrieval date: 24 October 2026
- Official package: https://www.onetcenter.org/dl_files/database/db_31_0_json.zip
- Package SHA-256: `E88360B2D2375D15107B4F1C3DD7FEC780D792D051B37A6A81FE8F1328AF47AA`
- Source population: 1,016 occupation records
- Eligible pool: 910 occupations with a non-empty description and at least one unsuppressed, relevant IM row in both skill domains
- Selected: exactly 175 unique occupation codes
- Missing descriptions among selected: 0
- Missing usable skill-importance data among selected: 0
- Skill coverage for every selected occupation: 10 essential IM rows and 25 transferable IM rows

## Category counts

| SmartCareer category | Careers |
|---|---:|
| Digital, Data and Computing | 20 |
| Engineering and Manufacturing | 25 |
| Business, Finance and Management | 25 |
| Health and Life Sciences | 20 |
| Science, Research and Environment | 15 |
| Education and Social Services | 15 |
| Arts, Media and Design | 15 |
| Law, Government and Public Services | 15 |
| Built Environment, Transport and Logistics | 15 |
| Agriculture, Food, Hospitality and Tourism | 10 |
| **Total** | **175** |

Each occupation has exactly one primary SmartCareer category. These categories are SmartCareer editorial groupings and are not O*NET classifications.

## Selection method and score

Only occupations passing the source-data checks above were considered. Selection then prioritised:

1. recognisable Malaysian study-to-career pathways;
2. accessibility from diploma, degree, or professional programmes;
3. coverage across sectors and disciplines;
4. avoidance of overlapping O*NET specialisations where a clearer occupation was available; and
5. avoidance of roles primarily defined by United States institutions.

`SelectionScore` is an editorial review score out of 10, not an O*NET or Malaysian labour-market rating. It combines Malaysian pathway relevance, graduate accessibility, diversity/demand value, and source completeness. Scores of 9.0 mark especially strong catalogue candidates, 8.5 marks suitable candidates, and 7.5 marks candidates retained for breadth but requiring explicit local review.

## Malaysian mapping status

No official O*NET-SOC-to-MASCO crosswalk was identified or assumed. Consequently, every manifest row is marked `Unmapped - manual MASCO 2020 review required`. This deliberately avoids inventing MASCO codes or claiming equivalence between US and Malaysian classifications.

Supporting Malaysian references used to shape the sector mix and review criteria:

- MASCO 2020: https://www.dosm.gov.my/uploads/content-downloads/file_20220920110308.pdf
- DOSM MyJobProfile: https://myjobprofile.dosm.gov.my/bda-myjobprofile/
- TalentCorp Malaysia Critical Occupations List: https://www.talentcorp.com.my/mycol

These references support local review; they do not establish row-level mappings in this manifest.

## Selections requiring particular manual review

| O*NET-SOC | Occupation | Review concern |
|---|---|---|
| 17-2011.00 | Aerospace Engineers | Confirm Malaysian graduate demand and avoid disproportionate emphasis on a smaller sector. |
| 17-2151.00 | Mining and Geological Engineers, Including Mining Safety Engineers | Confirm current Malaysian pathway and sector relevance. |
| 17-2171.00 | Petroleum Engineers | Retained for Malaysian energy-sector relevance; review future-facing balance and current demand. |
| 29-1161.00 | Nurse Midwives | O*NET role boundaries and advanced-practice assumptions require Malaysian regulatory review. |
| 29-9092.00 | Genetic Counselors | Confirm Malaysian recognition, availability of training pathways, and role naming. |
| 13-1041.08 | Customs Brokers | Confirm Malaysian licensing, terminology, and graduate-entry expectations. |
| 33-3021.00 | Detectives and Criminal Investigators | Duties and entry routes are jurisdiction-specific. |
| 33-3021.06 | Intelligence Analysts | Public-sector and security context requires careful localisation. |
| 33-3051.00 | Police and Sheriff's Patrol Officers | Replace US terminology and validate Malaysian recruitment requirements before publication. |
| 39-7011.00 | Tour Guides and Escorts | Confirm local licensing terminology and whether it fits the intended graduate focus. |
| 41-3041.00 | Travel Agents | Review continued graduate relevance given sector digitisation. |

Additional localisation is required for all regulated health, legal, education, engineering, and public-service careers before user-facing publication.

## Important review limitations

- The manifest validates O*NET descriptions and skill-importance availability, but it does not yet assess every occupation against current Malaysian vacancies or graduate outcomes.
- MyCOL was used as a supporting priority reference, not as proof that each selected occupation is currently critical.
- O*NET titles are retained verbatim. Malaysian display titles and aliases must be reviewed separately.
- The manifest intentionally contains no fabricated MASCO code, salary, demand claim, qualification, or Malaysian occupation equivalence.
- Near-duplicate control was editorial. A subject-matter reviewer should still check specialist engineering, computing, healthcare, and public-service entries.

## Source and licence references

- O*NET 31.0 release archive: https://www.onetcenter.org/db_releases.html
- O*NET 31.0 occupation data dictionary: https://www.onetcenter.org/dictionary/31.0/json/occupation_data.html
- O*NET 31.0 essential skills dictionary: https://www.onetcenter.org/dictionary/31.0/json/essential_skills.html
- O*NET 31.0 transferable skills dictionary: https://www.onetcenter.org/dictionary/31.0/json/transferable_skills.html
- O*NET 31.0 database licence: https://www.onetcenter.org/license_db.html

O*NET 31.0 database content is used under CC BY 4.0. SmartCareer category assignments, selection scores, inclusion reasons, and Malaysian review statuses are project-authored modifications and are not endorsed by USDOL/ETA.
