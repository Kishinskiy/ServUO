# KarmaVirtue

## TODO:
`Scripts/Mobiles/Core/BaseCreature.cs`

```c#
// Вставьте эту строчку в конец метода OnDeath в файле BaseCreature.cs:
Server.Custom.KarmaVirtueSystem.KarmaVirtueManager.HandleMonsterLoot(this, c);
```