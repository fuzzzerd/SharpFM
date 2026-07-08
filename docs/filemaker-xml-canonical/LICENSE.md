# License — Vendored canonical FileMaker XML references

The reference material in this directory is **not** original to SharpFM. It is
vendored, unmodified except where noted, from two open-source projects by
**Andrew Kear** of **Clockwork Creative Technology** (https://www.clockworkct.co.uk),
and is redistributed here under its original licence.

## License: CC BY 4.0

Both sources are licensed under the
[Creative Commons Attribution 4.0 International License](https://creativecommons.org/licenses/by/4.0/).

You are free to share and adapt the material for any purpose, provided you give
appropriate credit. SharpFM gives that credit here and in the `README.md`
alongside this file.

## Attribution

- **FileMaker XML Snippet Claude Skill** — reverse-engineered specification of
  FileMaker's `fmxmlsnippet` clipboard format.
  Source: https://github.com/andykear/FileMaker-XMLsnippet-Claude-Skill
  Vendored version: **v1.12** (FileMaker Pro 2026 / FM 26 support).
  Vendored under `skill/`.

- **Clockwork Inspector** — open-source FileMaker DDR / database-design XML
  analyzer. SharpFM vendors only two extracts: the `FM_STEP_IDS` step catalog
  and the `extractStepText` display renderer, used as a cross-check catalog and
  a reference for SharpFM's own step-display convention.
  Source: https://github.com/andykear/FileMaker-XML-inspector-open-source
  Vendored version: **v2.0**.
  Vendored under `inspector/`.

## What SharpFM uses this for

SharpFM is an XML-faithful FileMaker clipboard editor. These references are the
authoritative description of the exact native XML FileMaker's Script Workspace
paste handler accepts cleanly, including the documented silent-failure modes.
SharpFM's step model is validated against them. No SharpFM source code is
derived from Clockwork's code; the vendored files are reference data and are
parsed at test time only.
