# Assessment data security boundary

Future endpoints that read or write private career-assessment answers, scores, or results must enforce the Student role and a confirmed email address on the server. Hiding controls in the browser is not sufficient.

Password recovery for an unverified account does not confirm its email and must not grant access to existing private assessment data. Google accounts must continue to be associated by their external provider key or through an authenticated linking flow, never by email matching alone.
