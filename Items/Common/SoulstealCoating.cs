﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerRoguelike.Managers;
using TerRoguelike.Systems;
using TerRoguelike.Player;
using Microsoft.Xna.Framework.Graphics;

namespace TerRoguelike.Items.Common
{
    public class SoulstealCoating : BaseRoguelikeItem, ILocalizedModType
    {
        public override bool HealingItem => true;
        public override int itemTier => 0;
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 58;
            Item.rare = ItemRarityID.Blue;
            Item.maxStack = Item.CommonMaxStack;
        }
        public override void ItemEffects(Terraria.Player player)
        {
            player.GetModPlayer<TerRoguelikePlayer>().soulstealCoating += Item.stack;
        }
    }
}
