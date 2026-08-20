# qFoldIT Platform Contract Alignment

## Role

UNITY-TOOLBELT is the Unity runtime adapter within the qFoldIT platform. Unity provides visualization, interaction, simulation and world-state projection while scientific truth remains owned by scientific validators and solvers.

## Canonical contracts

- `qfoldit.mission/1.0`
- `qfoldit.scientific-state/1.0`
- `qfoldit.uag/1.0`
- `qfoldit.engine-adapter/1.0`
- `qfoldit.event/1.0`

## UAG boundary

UAG is the portable world representation. Unity-specific scene structures should remain behind the adapter boundary so the same mission can target other runtimes.

## Capability declaration

The adapter manifest is the authoritative capability declaration. Documentation should be synchronized with validated adapter metadata and conformance tests.

## Runtime flow

```text
Mission
  -> UAG
  -> Unity adapter
  -> interactive world
  -> scientific evidence references
  -> mission orchestration
  -> scientific validation
```
