# Addtional Power Scrolls

```c#
        
    // =========================================================================
    // ДОБАВЛЕНО ДЛЯ ТЕСТА: 100% Выпадение мифических свитков магии в труп зомби
    // =========================================================================
    public override void OnDeath(Container c)
    {
        base.OnDeath(c);

        if (c == null || c.Deleted)
            return;

        // -------------------------------------------------------------------------
        // НАСТРОЙКА ШАНСА: 0.05 означает ровно 5% вероятность дропа.
        // Если нужно сделать 10%, замените на 0.10. Если 1% — на 0.01.
        // -------------------------------------------------------------------------
        if (Utility.RandomDouble() > 1.00) 
        {
            return; // Вероятность не сработала — выходим из метода, свиток не падает!
        }
        // -------------------------------------------------------------------------

        // Если 5% шанс сработал, запускается стандартная логика выбора и спавна:
        string[] targetScrolls = new string[] 
        { 
            "MageryMythicalScroll", 
            "SwordsMythicalScrolls" 
        };

        string randomScrollName = targetScrolls[Utility.Random(targetScrolls.Length)];
        Type scrollType = ScriptCompiler.FindTypeByName(randomScrollName);

        if (scrollType != null)
        {
            int randomPlus = Utility.RandomList(10, 20, 30, 40, 50);

            try
            {
                Item mythicalScroll = Activator.CreateInstance(scrollType, new object[] { randomPlus }) as Item;

                if (mythicalScroll != null)
                {
                    c.DropItem(mythicalScroll);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PowerScrollsTest] Ошибка создания свитка: {ex.Message}");
            }
        }
    }
    // =========================================================================
```