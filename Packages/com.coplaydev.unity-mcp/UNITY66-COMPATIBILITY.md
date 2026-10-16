# Local Unity 6000.6 compatibility patch

This package was copied from the project's existing `Library/PackageCache/com.coplaydev.unity-mcp@ee2111d32d4c` on 2026-09-09. Original upstream URL: https://github.com/CoplayDev/unity-mcp.git?path=/UnityMcpBridge . Existing package files and license are retained.

`Packages/manifest.json` now uses `file:com.coplaydev.unity-mcp` so the fixes survive package-cache regeneration. This fork targets Unity 6000.6; do not replace it with an older upstream package without checking API compatibility.

Changes:
- Replace Object.GetInstanceID with GetEntityId and serialize via EntityId.ToULong. Preserve the existing JSON property names, but accept the full ulong identifier in lookup/deserialization. Never cast EntityId to int; Unity 6000.6 marks those operators as errors too.
- Resolve references through EntityId.FromULong and EditorUtility.EntityIdToObject.
- Replace Selection.activeInstanceID with activeEntityId.
- Console reflection accepts entityId as well as the older instanceID field name.

Object IDs are session-local identifiers. Clients must preserve the returned integer exactly rather than narrowing it to 32 bits or rounding it through a floating-point number.
