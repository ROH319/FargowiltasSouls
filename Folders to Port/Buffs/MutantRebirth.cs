using Terraria;
using Terraria.ModLoader;
using Terraria.Localization;

namespace FargowiltasSouls.Buffs
{
    public class MutantRebirth : ModBuff
    {
        public override void SetDefaults()
        {
            DisplayName.SetDefault("Mutant Rebirth");
            Description.SetDefault("Deathray revive is recharging");
            DisplayName.AddTranslation((int)GameCulture.CultureName.Chinese, "突变重生");
            Description.AddTranslation((int)GameCulture.CultureName.Chinese, "死光复苏蓄能中");
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            
            Terraria.ID.BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override bool Autoload(ref string name, ref string texture)
        {
            texture = "FargowiltasSouls/Buffs/PlaceholderDebuff";
            return true;
        }
    }
}