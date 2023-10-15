﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerRoguelike.Managers;
using TerRoguelike.Systems;
using TerRoguelike.TerPlayer;
using Microsoft.Xna.Framework.Graphics;

namespace TerRoguelike.Items.Rare
{
    public class DroneBuddy : BaseRoguelikeItem, ILocalizedModType
    {
        public override int modItemID => ModContent.ItemType<DroneBuddy>();
        public override bool CombatItem => true;
        public override bool HealingItem => true;
        public override int itemTier => 2;
        public override float ItemDropWeight => 0.5f;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Item.rare = ItemRarityID.Red;
            Item.maxStack = Item.CommonMaxStack;
        }
        public override void ItemEffects(Player player)
        {
            player.GetModPlayer<TerRoguelikePlayer>().droneBuddy += Item.stack;
        }
    }
}
