# Conformance tests — qfoldit-engine-adapter-spec-v0.1

This directory verifies `../../Editor/Core/UagModel.cs` and
`../../Editor/Core/UagValidator.cs` against the **real, unmodified**
artifacts from `qfoldit-engine-adapter-spec-v0.1` — not a reimplementation
of the spec's intent, the actual files:

- `test_vectors.json` — copied verbatim from
  `qfoldit-engine-adapter-spec-v0.1/conformance/test_vectors.json`.
- `protein-folding.uag.json` — copied verbatim from
  `qfoldit-engine-adapter-spec-v0.1/examples/protein-folding.uag.json`,
  the spec's own hand-authored example.
- `compiler_output_unity.json` — the **actual output** of running
  `qfoldit-scientific-gameplay-framework-v0.1/reference/compiler.py`'s
  real `compile_pattern()` against the real `protein_folding_construction`
  pattern and this repo's `qfoldit.adapter.json`, captured once and
  committed here so the parse test doesn't depend on having Python or the
  gameplay-framework repo available.

## What's tested

- `conformance_test.cs` — runs every vector in `test_vectors.json` against
  `UagValidator.Validate()`, checking both `valid` and (where given)
  `error_code` match exactly. Adds 4 extended scenarios beyond the 3
  official vectors: self-referential cycles, wrong `schema` value, a
  genuinely-unknown node type (confirmed to be a *gap*, not a validation
  error), and — importantly — confirming `scientific_subject/construction`
  nodes and `construction`-typed interactions are **not** flagged as gaps,
  since those are exactly what `reference/compiler.py` emits for every
  themed gameplay pattern.
- `uag_schema_parse_test.cs` — confirms `UagModel.cs` correctly
  deserializes both the spec's example and the real compiler output,
  field-for-field (schema id, scene metadata, node type/parent/properties,
  interaction target, binding source).

## Running

Requires `mono-mcs` and a `net40`-or-`net45` build of `Newtonsoft.Json.dll`
(see the root README's "Before you build" note — same dependency the
plugin itself has via `com.unity.nuget.newtonsoft-json`).

```bash
cd tests/conformance
cp ../../Editor/Core/UagModel.cs ../../Editor/Core/UagValidator.cs ../../Editor/Core/UAGBridgeMechanics.cs .
mcs -langversion:latest -out:conformance.exe -r:Newtonsoft.Json.dll UagModel.cs UagValidator.cs UAGBridgeMechanics.cs conformance_test.cs
mcs -langversion:latest -out:parsetest.exe -r:Newtonsoft.Json.dll UagModel.cs uag_schema_parse_test.cs
mono conformance.exe
mono parsetest.exe
```

Both exit non-zero if any check fails. This does **not** verify
`UAGBridgeTools.cs`'s orchestration logic (node creation dispatch,
interaction/binding realization) — that depends on `UnityEditor`/
`UnityEngine` and can't run outside the Editor; see the `⚠`-style honesty
notes in `UAGBridgeTools.cs` itself and `docs/UAG_BRIDGE.md` for what's
verified by code review rather than execution.
