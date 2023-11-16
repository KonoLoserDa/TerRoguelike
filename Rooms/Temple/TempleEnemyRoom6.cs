﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerRoguelike.Managers;
using TerRoguelike.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace TerRoguelike.Rooms
{
    public class TempleEnemyRoom6 : Room
    {
        public override string Key => "TempleEnemyRoom6";
        public override string Filename => "Schematics/RoomSchematics/TempleEnemyRoom6.csch";
        public override bool CanExitRight => true;
        public override void InitializeRoom()
        {
            base.InitializeRoom();
            AddRoomNPC(new Vector2(RoomDimensions.X * 8f, RoomDimensions.Y * 8f), NPCID.FlyingSnake, 60, 120, 0.45f);
        }
    }
}
