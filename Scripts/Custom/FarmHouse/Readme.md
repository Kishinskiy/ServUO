# Farming

## hoto add seed to Mobs

Откройте скрипт любого монстра (например, Scripts/Mobiles/Monsters/Humanoid/Melee/Orc.cs или Reaper.cs) и найдите метод GenerateLoot(). Добавьте туда проверку на шанс выпадения:

```c#
public override void GenerateLoot()
{
    base.GenerateLoot();

    // Шанс 10% (0.1), что в трупе монстра появится мешочек с семенами
    if ( Utility.RandomDouble() < 0.10 )
        PackItem( new Server.Custom.Farms.FarmSeedBag() );
}

```

Вариант Б. Через глобальный дистрибьютор лута (Чтобы падал со ВСЕХ монстров в мире)
Если вы хотите, чтобы мешок мог упасть вообще с любого существа определенного уровня сложности:
1. Откройте файл 📂 Scripts/Misc/Loot.cs.
2. Найдите там массивы лута (например, MiddlingLoot или LowLoot).
3. Добавьте в конец списка типов ваш новый класс: typeof( Server.Custom.Farms.FarmSeedBag ).