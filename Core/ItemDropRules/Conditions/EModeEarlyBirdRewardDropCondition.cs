﻿using FargowiltasSouls.Content.Items;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace FargowiltasSouls.Core.ItemDropRules.Conditions
{
    public class EModeEarlyBirdRewardDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info) => !info.IsInSimulation && (!Main.hardMode || FargoSoulsWorld.EternityMode);

        public bool CanShowItemDropInUI() => true;

        public string GetConditionDescription() => $"[i:{ModContent.ItemType<Masochist>()}]Pre-Hardmode Eternity Mode drop rate";
    }
}
