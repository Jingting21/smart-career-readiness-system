# Registration autocomplete datasets

## Universities and education organizations

`universities.json` was generated from Research Organization Registry (ROR)
data release v2.14, published 2026-10-06 on Zenodo under DOI
`10.5281/zenodo.23187250`. ROR data is dedicated to the public domain under
CC0 1.0. Location metadata is derived from GeoNames and licensed under CC BY
4.0. The generated file contains the display name and country fields for the
30,136 active ROR records whose types include `education`.

ROR is worldwide, but it is a registry of research organizations rather than
an authoritative list of every university. Its `education` type also includes
some colleges, academies and education-focused research organizations. Manual
entry remains available for missing institutions.

`malaysian-institutions.json` supplements ROR with 40 verified Malaysian
institutions: the 20 public universities in the Ministry of Higher Education
(MOHE) public-university directory and 20 established private universities or
international branch campuses present in the official Education Malaysia/MQA
institution directories. It stores one canonical institution name plus common
abbreviations, English/Malay equivalents and selected former names. Search
results use the canonical name, preventing aliases from appearing as duplicate
institutions.

Primary Malaysian verification sources:

- MOHE public-university directory: https://myinfoipta.mohe.gov.my/CoverPage/MainUniv.aspx
- MOHE Department of Higher Education directory: https://jpt.mohe.gov.my/index.php/en/hei/public-hei/public-universities
- Malaysian Qualifications Register: https://www2.mqa.gov.my/mqr/english/eakrbyipta.cfm
- Education Malaysia institution directory: https://educationmalaysia.gov.my/

Update procedure:

1. Download the current ROR data dump from https://doi.org/10.5281/zenodo.6347574.
2. Select active records whose semicolon-delimited `types` include `education`.
3. Export the display name, acronym/alias/label names, country name and country code.
4. Sort and deduplicate by display name and country.

## Courses and fields of study

`courses.json` was generated from the official NCES Classification of
Instructional Programs (CIP) 2020 CSV. It contains 2,235 detailed six-digit CIP
program titles and codes. Information on the U.S. Department of Education IES
website is public domain unless otherwise stated.

`malaysian-courses.json` supplements CIP with 31 common programme names and 36
search aliases used for spelling and naming variations (for example,
`Cybersecurity` / `Cyber Security` and `Accounting` / `Accountancy`). These are
search conveniences based on terminology commonly found in Malaysian
higher-education directories, not claims that every institution offers them.

CIP is a United States statistical classification. It provides broad subject
coverage but does not represent every programme title used by universities
worldwide. Manual entry therefore remains available.

Update procedure:

1. Download `CIPCode2020.csv` from
   https://nces.ed.gov/ipeds/cipcode/Files/CIPCode2020.csv.
2. Select detailed six-digit CIP codes.
3. Export the code and title, removing only the title's terminal period.
