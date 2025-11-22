namespace WarOfCrowns.Core
{
    public enum ResourceType
    {
        // --- БАЗОВЫЕ ВАЛЮТЫ И ПОТРЕБНОСТИ ---
        Wood,       // Дерево (ресурс)
        Stone,      // Камень (ресурс)
        Gold,       // Золото (Валюта)
        Food,       // Сытость (абстрактная)

        // --- ЕДА И СЕЛЬХОЗ (ПРЕДМЕТЫ) ---
        Berries,
        Wheat,
        Flour,
        Bread,
        RawMeat,
        CookedMeat,

        // --- РУДЫ И ИСКОПАЕМЫЕ ---
        IronOre,
        SteelOre,
        Coal,
        GoldOre,
        MithrilOre,
        Obsidian,

        // --- СЛИТКИ И МАТЕРИАЛЫ ---
        IronIngot,
        GoldIngot,
        SteelIngot,
        MithrilIngot,
        ObsidianPlate,

        // --- БРОНЯ (5 Видов) ---
        IronArmor,
        SteelArmor,
        GoldArmor,
        MithrilArmor,
        ObsidianArmor,

        // --- ИНСТРУМЕНТЫ (по 7 Видов) ---
        // Кирки
        WoodenPickaxe, StonePickaxe, IronPickaxe, SteelPickaxe,
        GoldPickaxe, MithrilPickaxe, ObsidianPickaxe,

        // Топоры
        WoodenAxe, StoneAxe, IronAxe, SteelAxe,
        GoldAxe, MithrilAxe, ObsidianAxe,

        // Молоты (для строительства)
        WoodenHammer, StoneHammer, IronHammer, SteelHammer,
        GoldHammer, MithrilHammer, ObsidianHammer,

        // --- ОРУЖИЕ ---

        // Мечи (7 видов)
        WoodenSword, StoneSword, IronSword, SteelSword,
        GoldSword, MithrilSword, ObsidianSword,

        // Копья (7 видов)
        WoodenSpear, StoneSpear, IronSpear, SteelSpear,
        GoldSpear, MithrilSpear, ObsidianSpear,

        // Луки (5 видов - без Камня и Обсидиана)
        WoodenBow,
        IronBow,
        SteelBow,
        GoldBow,
        MithrilBow
    }
}