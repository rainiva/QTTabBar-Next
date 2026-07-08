# Archived ExplorerManager (full implementation)

`ExplorerManager.full.cs` is the historical ~10k-line ExplorerManager implementation
from the indiff fork. It is **not compiled** into QTTabBar Rebirth.

The active build uses the stub at [`QTTabBar/ExplorerManager.cs`](../../QTTabBar/ExplorerManager.cs),
which provides the subset of APIs still referenced by the rebirth codebase (for example
`GetWatermarkImage`, `WindowDpi`, `ToolbarManager`).

This archive is kept for reference when porting features from the full implementation.
