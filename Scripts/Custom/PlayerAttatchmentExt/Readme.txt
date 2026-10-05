PlayerAttatchmentExtensions
===========================

This is the standalone attachment subsystem used by Level System Extreme.

It was originally adapted from ArteGordon's XMLSpawner attachment work, but it
has been separated from XMLSpawner naming so it can live beside XMLSpawner2 on
the same shard without namespace, class, command, or save-folder conflicts.

Primary namespace:

/* --- BEGIN CODE BLOCK: USING STATEMENT --- */
using Server.Engines.PlayerAttatchmentExtensions;
/* --- END CODE BLOCK --- */

Primary API:

/* --- BEGIN CODE BLOCK: COMMON CALLS --- */
PlayerAttatchmentExt.AttachTo(target, new MyAttachment());
PlayerAttatchmentExt.FindAttachment(target, typeof(MyAttachment));
PlayerAttatchmentExt.FindAttachments(target);
/* --- END CODE BLOCK --- */

Attachment classes should inherit from `PlayerAttatchmentExtension` and use the
`[PlayerAttatchable]` constructor attribute when they should be creatable from
the attachment admin commands.

Available commands:

All commands are Game Master access or higher.

- GetAttExt
  Shows attachments on a targeted object. You can also pass an attachment
  serial number to open the properties gump for that specific attachment.

- GetPlayerAttExt
  Same handler as GetAttExt. This is the new unique command name that avoids
  XMLSpawner command naming conflicts.

- PlayerGetAttExt
  Opens the attachment browser gump for a targeted object. This is easier for
  reviewing, filtering, selecting, and deleting attachments from a gump.

- AddAttExt
  Adds an attachment to the targeted item or mobile.

  Usage:

  /* --- BEGIN CODE BLOCK: ADD ATTACHMENT COMMAND --- */
  [AddAttExt AttachmentType optional arguments
  /* --- END CODE BLOCK --- */

- AddPlayerAttExt
  Same handler as AddAttExt. This is the new unique command alias.

- DelAttExt
  Deletes an attachment from the targeted item or mobile by attachment type.

  Usage:

  /* --- BEGIN CODE BLOCK: DELETE ATTACHMENT COMMAND --- */
  [DelAttExt AttachmentType
  /* --- END CODE BLOCK --- */

- DelPlayerAttExt
  Same handler as DelAttExt. This is the new unique command alias.

- AvailAttExt
  Lists attachment classes that can be created by command. A constructor must
  use `[PlayerAttatchable]` before it appears here.

- AvailPlayerAttExt
  Same handler as AvailAttExt. This is the new unique command alias.

- ItemAttExt
  Lists all items that currently have attachments and prints each attachment
  summary to the caller.

- PlayerItemAttExt
  Same handler as ItemAttExt. This is the new unique command alias.

Compatibility command notes:

- The old Ext command names are intentionally still registered:
  GetAttExt, AddAttExt, DelAttExt, AvailAttExt, and ItemAttExt.
- The Player-named aliases are preferred for new scripts and documentation.
- The old XMLSpawner-style command names AddAtt, DelAtt, TrigAtt, ItemAtt, and
  MobAtt are not registered by this standalone subsystem.

Save data writes to:

Saves/PlayerAttatchmentExtensions

Legacy migration:

If an old save exists in `Saves/ExtXmlAttachments`, this subsystem can read it
and remap old `Server.Engines.XmlSpawnerExtMod` attachment type names to the
new `Server.Engines.PlayerAttatchmentExtensions` namespace. The next world save
writes the data to the new standalone folder.

Supported hooks retained from the old attachment system include OnAttach,
OnReattach, OnDelete, OnUse, OnUser, BlockDefaultOnUse, OnDragLift, OnSpeech,
OnMovement, OnKill, OnBeforeKill, OnKilled, OnBeforeKilled, OnWeaponHit,
OnArmorHit, CanEquip, OnEquip, OnRemoved, AddProperties, DisplayedProperties,
serialization, deserialization, and expiration timers.

RunUO 2.0 compatibility note:

The subsystem avoids modern C# syntax and does not hard-code the ServUO
`Core.TickCount` object-use timer path. It checks the core's `NextActionTime`
shape at runtime so the same code can compile against ServUO-style tick timers
or RunUO 2.0-style DateTime timers.

Safety notes:

- New saves are written to `Saves/PlayerAttatchmentExtensions`.
- Old `Saves/ExtXmlAttachments` data is only used as a migration source when a
  new attachment save does not already exist.
- Attachment admin gumps include extra null and bounds checks so stale search
  results or empty selections do not break the command.
- All original attachment hooks are still expected to work the same way.
