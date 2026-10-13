# Registration autocomplete datasets

## Universities and education organizations

`universities.json` was generated from Research Organization Registry (ROR)
data release v2.14, published 2026-10-06 on Zenodo under DOI
`10.5281/zenodo.23187250`. ROR data is dedicated to the public domain under
CC0 1.0. Location metadata is derived from GeoNames and licensed under CC BY
4.0. The generated file contains the display name and country fields for the
25,543 active ROR records classified as `education`.

ROR is worldwide, but it is a registry of research organizations rather than
an authoritative list of every university. Its `education` type also includes
some colleges, academies and education-focused research organizations. Manual
entry remains available for missing institutions.

Update procedure:

1. Download the current ROR data dump from https://doi.org/10.5281/zenodo.6347574.
2. Select active records whose `types` include `education`.
3. Export `names.types.ror_display`, country name and country code.
4. Sort and deduplicate by display name and country.

## Courses and fields of study

`courses.json` was generated from the official NCES Classification of
Instructional Programs (CIP) 2020 CSV. It contains 2,235 detailed six-digit CIP
program titles and codes. Information on the U.S. Department of Education IES
website is public domain unless otherwise stated.

The application supplements CIP titles at runtime with the sixteen common
plain-language programme names required by this project (for example,
`Software Engineering` and `Data Science`). These are search conveniences, not
claims that CIP or any university uses those exact programme names.

CIP is a United States statistical classification. It provides broad subject
coverage but does not represent every programme title used by universities
worldwide. Manual entry therefore remains available.

Update procedure:

1. Download `CIPCode2020.csv` from
   https://nces.ed.gov/ipeds/cipcode/Files/CIPCode2020.csv.
2. Select detailed six-digit CIP codes.
3. Export the code and title, removing only the title's terminal period.
