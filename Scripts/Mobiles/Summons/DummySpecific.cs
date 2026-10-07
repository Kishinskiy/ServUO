using Server.Items;
using Server.Spells.First;
using Server.Spells.Third;

namespace Server.Mobiles
{
    /// <summary>
    ///     This is a test creature
    ///     You can set its value in game
    ///     It die after 5 minutes, so your test server stay clean
    ///     Create a macro to help your creation "[add Dummy 1 15 7 -1 0.5 2"
    ///     A iTeam of negative will set a faction at random
    ///     Say Kill if you want them to die
    /// </summary>
    public class DummyMace : Dummy
    {
        [Constructable]
        public DummyMace()
            : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Macer
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(125, 125, 90);
            Skills[SkillName.Macing].Base = 120;
            Skills[SkillName.Anatomy].Base = 120;
            Skills[SkillName.Healing].Base = 120;
            Skills[SkillName.Tactics].Base = 120;

            // Name
            Name = "Macer";

            // Equip
            var war = new WarHammer();
            war.Movable = true;
            war.Crafter = this;
            war.Quality = ItemQuality.Normal;
            AddItem(war);

            var bts = new Boots();
            bts.Hue = iHue;
            AddItem(bts);

            var cht = new ChainChest();
            cht.Movable = false;
            cht.LootType = LootType.Newbied;
            cht.Crafter = this;
            cht.Quality = ItemQuality.Normal;
            AddItem(cht);

            var chl = new ChainLegs();
            chl.Movable = false;
            chl.LootType = LootType.Newbied;
            chl.Crafter = this;
            chl.Quality = ItemQuality.Normal;
            AddItem(chl);

            var pla = new PlateArms();
            pla.Movable = false;
            pla.LootType = LootType.Newbied;
            pla.Crafter = this;
            pla.Quality = ItemQuality.Normal;
            AddItem(pla);

            var band = new Bandage(50);
            AddToBackpack(band);
        }

        public DummyMace(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DummyFence : Dummy
    {
        [Constructable]
        public DummyFence()
            : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Fencer
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(125, 125, 90);
            Skills[SkillName.Fencing].Base = 120;
            Skills[SkillName.Anatomy].Base = 120;
            Skills[SkillName.Healing].Base = 120;
            Skills[SkillName.Tactics].Base = 120;

            // Name
            Name = "Fencer";

            // Equip
            var ssp = new Spear();
            ssp.Movable = true;
            ssp.Crafter = this;
            ssp.Quality = ItemQuality.Normal;
            AddItem(ssp);

            var snd = new Boots();
            snd.Hue = iHue;
            snd.LootType = LootType.Newbied;
            AddItem(snd);

            var cht = new ChainChest();
            cht.Movable = false;
            cht.LootType = LootType.Newbied;
            cht.Crafter = this;
            cht.Quality = ItemQuality.Normal;
            AddItem(cht);

            var chl = new ChainLegs();
            chl.Movable = false;
            chl.LootType = LootType.Newbied;
            chl.Crafter = this;
            chl.Quality = ItemQuality.Normal;
            AddItem(chl);

            var pla = new PlateArms();
            pla.Movable = false;
            pla.LootType = LootType.Newbied;
            pla.Crafter = this;
            pla.Quality = ItemQuality.Normal;
            AddItem(pla);

            var band = new Bandage(50);
            AddToBackpack(band);
        }

        public DummyFence(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DummySword : Dummy
    {
        [Constructable]
        public DummySword()
            : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Swordsman
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(125, 125, 90);
            Skills[SkillName.Swords].Base = 120;
            Skills[SkillName.Anatomy].Base = 120;
            Skills[SkillName.Healing].Base = 120;
            Skills[SkillName.Tactics].Base = 120;
            Skills[SkillName.Parry].Base = 120;

            // Name
            Name = "Swordsman";

            // Equip
            var kat = new Katana();
            kat.Crafter = this;
            kat.Movable = true;
            kat.Quality = ItemQuality.Normal;
            AddItem(kat);

            var bts = new Boots();
            bts.Hue = iHue;
            AddItem(bts);

            var cht = new ChainChest();
            cht.Movable = false;
            cht.LootType = LootType.Newbied;
            cht.Crafter = this;
            cht.Quality = ItemQuality.Normal;
            AddItem(cht);

            var chl = new ChainLegs();
            chl.Movable = false;
            chl.LootType = LootType.Newbied;
            chl.Crafter = this;
            chl.Quality = ItemQuality.Normal;
            AddItem(chl);

            var pla = new PlateArms();
            pla.Movable = false;
            pla.LootType = LootType.Newbied;
            pla.Crafter = this;
            pla.Quality = ItemQuality.Normal;
            AddItem(pla);

            var band = new Bandage(50);
            AddToBackpack(band);
        }

        public DummySword(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DummyNox : Dummy
    {
        [Constructable]
        public DummyNox()
            : base(AIType.AI_Mage, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Nox or Pure Mage
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(90, 90, 125);
            Skills[SkillName.Magery].Base = 120;
            Skills[SkillName.EvalInt].Base = 120;
            Skills[SkillName.Inscribe].Base = 100;
            Skills[SkillName.Wrestling].Base = 120;
            Skills[SkillName.Meditation].Base = 120;
            Skills[SkillName.Poisoning].Base = 100;

            // Name
            Name = "Nox Mage";

            // Equip
            var book = new Spellbook();
            book.Movable = false;
            book.LootType = LootType.Newbied;
            book.Content = 0xFFFFFFFFFFFFFFFF;
            AddItem(book);

            var kilt = new Kilt();
            kilt.Hue = jHue;
            AddItem(kilt);

            var snd = new Sandals();
            snd.Hue = iHue;
            snd.LootType = LootType.Newbied;
            AddItem(snd);

            var skc = new SkullCap();
            skc.Hue = iHue;
            AddItem(skc);

            // Spells
            AddSpellAttack(typeof(MagicArrowSpell));
            AddSpellAttack(typeof(WeakenSpell));
            AddSpellAttack(typeof(FireballSpell));
            AddSpellDefense(typeof(WallOfStoneSpell));
            AddSpellDefense(typeof(HealSpell));
        }

        public DummyNox(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DummyStun : Dummy
    {
        [Constructable]
        public DummyStun()
            : base(AIType.AI_Mage, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Stun Mage
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(90, 90, 125);
            Skills[SkillName.Magery].Base = 100;
            Skills[SkillName.EvalInt].Base = 120;
            Skills[SkillName.Anatomy].Base = 80;
            Skills[SkillName.Wrestling].Base = 80;
            Skills[SkillName.Meditation].Base = 100;
            Skills[SkillName.Poisoning].Base = 100;

            // Name
            Name = "Stun Mage";

            // Equip
            var book = new Spellbook();
            book.Movable = false;
            book.LootType = LootType.Newbied;
            book.Content = 0xFFFFFFFFFFFFFFFF;
            AddItem(book);

            var lea = new LeatherArms();
            lea.Movable = false;
            lea.LootType = LootType.Newbied;
            lea.Crafter = this;
            lea.Quality = ItemQuality.Normal;
            AddItem(lea);

            var lec = new LeatherChest();
            lec.Movable = false;
            lec.LootType = LootType.Newbied;
            lec.Crafter = this;
            lec.Quality = ItemQuality.Normal;
            AddItem(lec);

            var leg = new LeatherGorget();
            leg.Movable = false;
            leg.LootType = LootType.Newbied;
            leg.Crafter = this;
            leg.Quality = ItemQuality.Normal;
            AddItem(leg);

            var lel = new LeatherLegs();
            lel.Movable = false;
            lel.LootType = LootType.Newbied;
            lel.Crafter = this;
            lel.Quality = ItemQuality.Normal;
            AddItem(lel);

            var bts = new Boots();
            bts.Hue = iHue;
            AddItem(bts);

            var cap = new Cap();
            cap.Hue = iHue;
            AddItem(cap);

            // Spells
            AddSpellAttack(typeof(MagicArrowSpell));
            AddSpellAttack(typeof(WeakenSpell));
            AddSpellAttack(typeof(FireballSpell));
            AddSpellDefense(typeof(WallOfStoneSpell));
            AddSpellDefense(typeof(HealSpell));
        }

        public DummyStun(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DummySuper : Dummy
    {
        [Constructable]
        public DummySuper()
            : base(AIType.AI_Mage, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Super Mage
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(125, 125, 125);
            Skills[SkillName.Magery].Base = 120;
            Skills[SkillName.EvalInt].Base = 120;
            Skills[SkillName.Anatomy].Base = 120;
            Skills[SkillName.Wrestling].Base = 120;
            Skills[SkillName.Meditation].Base = 120;
            Skills[SkillName.Poisoning].Base = 100;
            Skills[SkillName.Inscribe].Base = 100;

            // Name
            Name = "Super Mage";

            // Equip
            var book = new Spellbook();
            book.Movable = false;
            book.LootType = LootType.Newbied;
            book.Content = 0xFFFFFFFFFFFFFFFF;
            AddItem(book);

            var lea = new LeatherArms();
            lea.Movable = false;
            lea.LootType = LootType.Newbied;
            lea.Crafter = this;
            lea.Quality = ItemQuality.Normal;
            AddItem(lea);

            var lec = new LeatherChest();
            lec.Movable = false;
            lec.LootType = LootType.Newbied;
            lec.Crafter = this;
            lec.Quality = ItemQuality.Normal;
            AddItem(lec);

            var leg = new LeatherGorget();
            leg.Movable = false;
            leg.LootType = LootType.Newbied;
            leg.Crafter = this;
            leg.Quality = ItemQuality.Normal;
            AddItem(leg);

            var lel = new LeatherLegs();
            lel.Movable = false;
            lel.LootType = LootType.Newbied;
            lel.Crafter = this;
            lel.Quality = ItemQuality.Normal;
            AddItem(lel);

            var snd = new Sandals();
            snd.Hue = iHue;
            snd.LootType = LootType.Newbied;
            AddItem(snd);

            var jhat = new JesterHat();
            jhat.Hue = iHue;
            AddItem(jhat);

            var dblt = new Doublet();
            dblt.Hue = iHue;
            AddItem(dblt);

            // Spells
            AddSpellAttack(typeof(MagicArrowSpell));
            AddSpellAttack(typeof(WeakenSpell));
            AddSpellAttack(typeof(FireballSpell));
            AddSpellDefense(typeof(WallOfStoneSpell));
            AddSpellDefense(typeof(HealSpell));
        }

        public DummySuper(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DummyHealer : Dummy
    {
        [Constructable]
        public DummyHealer()
            : base(AIType.AI_Healer, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Healer Mage
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(125, 125, 125);
            Skills[SkillName.Magery].Base = 120;
            Skills[SkillName.EvalInt].Base = 120;
            Skills[SkillName.Anatomy].Base = 120;
            Skills[SkillName.Wrestling].Base = 120;
            Skills[SkillName.Meditation].Base = 120;
            Skills[SkillName.Healing].Base = 100;

            // Name
            Name = "Healer";

            // Equip
            var book = new Spellbook();
            book.Movable = false;
            book.LootType = LootType.Newbied;
            book.Content = 0xFFFFFFFFFFFFFFFF;
            AddItem(book);

            var lea = new LeatherArms();
            lea.Movable = false;
            lea.LootType = LootType.Newbied;
            lea.Crafter = this;
            lea.Quality = ItemQuality.Normal;
            AddItem(lea);

            var lec = new LeatherChest();
            lec.Movable = false;
            lec.LootType = LootType.Newbied;
            lec.Crafter = this;
            lec.Quality = ItemQuality.Normal;
            AddItem(lec);

            var leg = new LeatherGorget();
            leg.Movable = false;
            leg.LootType = LootType.Newbied;
            leg.Crafter = this;
            leg.Quality = ItemQuality.Normal;
            AddItem(leg);

            var lel = new LeatherLegs();
            lel.Movable = false;
            lel.LootType = LootType.Newbied;
            lel.Crafter = this;
            lel.Quality = ItemQuality.Normal;
            AddItem(lel);

            var snd = new Sandals();
            snd.Hue = iHue;
            snd.LootType = LootType.Newbied;
            AddItem(snd);

            var cap = new Cap();
            cap.Hue = iHue;
            AddItem(cap);

            var robe = new Robe();
            robe.Hue = iHue;
            AddItem(robe);
        }

        public DummyHealer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    public class DummyAssassin : Dummy
    {
        [Constructable]
        public DummyAssassin()
            : base(AIType.AI_Melee, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Hybrid Assassin
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(105, 105, 105);
            Skills[SkillName.Magery].Base = 120;
            Skills[SkillName.EvalInt].Base = 120;
            Skills[SkillName.Swords].Base = 120;
            Skills[SkillName.Tactics].Base = 120;
            Skills[SkillName.Meditation].Base = 120;
            Skills[SkillName.Poisoning].Base = 100;

            // Name
            Name = "Hybrid Assassin";

            // Equip
            var book = new Spellbook();
            book.Movable = false;
            book.LootType = LootType.Newbied;
            book.Content = 0xFFFFFFFFFFFFFFFF;
            AddToBackpack(book);

            var kat = new Katana();
            kat.Movable = false;
            kat.LootType = LootType.Newbied;
            kat.Crafter = this;
            kat.Poison = Poison.Deadly;
            kat.PoisonCharges = 12;
            kat.Quality = ItemQuality.Normal;
            AddToBackpack(kat);

            var lea = new LeatherArms();
            lea.Movable = false;
            lea.LootType = LootType.Newbied;
            lea.Crafter = this;
            lea.Quality = ItemQuality.Normal;
            AddItem(lea);

            var lec = new LeatherChest();
            lec.Movable = false;
            lec.LootType = LootType.Newbied;
            lec.Crafter = this;
            lec.Quality = ItemQuality.Normal;
            AddItem(lec);

            var leg = new LeatherGorget();
            leg.Movable = false;
            leg.LootType = LootType.Newbied;
            leg.Crafter = this;
            leg.Quality = ItemQuality.Normal;
            AddItem(leg);

            var lel = new LeatherLegs();
            lel.Movable = false;
            lel.LootType = LootType.Newbied;
            lel.Crafter = this;
            lel.Quality = ItemQuality.Normal;
            AddItem(lel);

            var snd = new Sandals();
            snd.Hue = iHue;
            snd.LootType = LootType.Newbied;
            AddItem(snd);

            var cap = new Cap();
            cap.Hue = iHue;
            AddItem(cap);

            var robe = new Robe();
            robe.Hue = iHue;
            AddItem(robe);

            var pota = new DeadlyPoisonPotion();
            pota.LootType = LootType.Newbied;
            AddToBackpack(pota);

            var potb = new DeadlyPoisonPotion();
            potb.LootType = LootType.Newbied;
            AddToBackpack(potb);

            var potc = new DeadlyPoisonPotion();
            potc.LootType = LootType.Newbied;
            AddToBackpack(potc);

            var potd = new DeadlyPoisonPotion();
            potd.LootType = LootType.Newbied;
            AddToBackpack(potd);

            var band = new Bandage(50);
            AddToBackpack(band);
        }

        public DummyAssassin(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Mobiles.DummyTheif")]
    public class DummyThief : Dummy
    {
        [Constructable]
        public DummyThief()
            : base(AIType.AI_Thief, FightMode.Closest, 15, 1, 0.2, 0.6)
        {
            // A Dummy Hybrid Thief
            var iHue = 20 + Team * 40;
            var jHue = 25 + Team * 40;

            // Skills and Stats
            InitStats(105, 105, 105);
            Skills[SkillName.Healing].Base = 120;
            Skills[SkillName.Anatomy].Base = 120;
            Skills[SkillName.Stealing].Base = 120;
            Skills[SkillName.ArmsLore].Base = 100;
            Skills[SkillName.Meditation].Base = 120;
            Skills[SkillName.Wrestling].Base = 120;

            // Name
            Name = "Hybrid Thief";

            // Equip
            var book = new Spellbook();
            book.Movable = false;
            book.LootType = LootType.Newbied;
            book.Content = 0xFFFFFFFFFFFFFFFF;
            AddItem(book);

            var lea = new LeatherArms();
            lea.Movable = false;
            lea.LootType = LootType.Newbied;
            lea.Crafter = this;
            lea.Quality = ItemQuality.Normal;
            AddItem(lea);

            var lec = new LeatherChest();
            lec.Movable = false;
            lec.LootType = LootType.Newbied;
            lec.Crafter = this;
            lec.Quality = ItemQuality.Normal;
            AddItem(lec);

            var leg = new LeatherGorget();
            leg.Movable = false;
            leg.LootType = LootType.Newbied;
            leg.Crafter = this;
            leg.Quality = ItemQuality.Normal;
            AddItem(leg);

            var lel = new LeatherLegs();
            lel.Movable = false;
            lel.LootType = LootType.Newbied;
            lel.Crafter = this;
            lel.Quality = ItemQuality.Normal;
            AddItem(lel);

            var snd = new Sandals();
            snd.Hue = iHue;
            snd.LootType = LootType.Newbied;
            AddItem(snd);

            var cap = new Cap();
            cap.Hue = iHue;
            AddItem(cap);

            var robe = new Robe();
            robe.Hue = iHue;
            AddItem(robe);

            var band = new Bandage(50);
            AddToBackpack(band);
        }

        public DummyThief(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            var version = reader.ReadInt();
        }
    }
}
