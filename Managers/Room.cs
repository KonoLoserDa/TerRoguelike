﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TerRoguelike.Managers;
using TerRoguelike.TerPlayer;
using TerRoguelike.NPCs;
using Terraria.Chat;
using TerRoguelike.Systems;
using TerRoguelike.Projectiles;
using Microsoft.Xna.Framework.Graphics;

namespace TerRoguelike.Managers
{
    public class Room
    {
        // base room class used by all rooms
        public virtual string Key => null; //schematic key
        public virtual string Filename => null; //schematic filename
        public virtual int ID => -1; //ID in RoomID list
        public virtual int AssociatedFloor => -1; // what floor this room is associated with
        public virtual bool CanExitRight => false; // if room is capable of exiting right
        public virtual bool CanExitDown => false; // if room is capable of exiting down
        public virtual bool CanExitUp => false; // if room is capable of exiting up
        public virtual bool IsBossRoom => false; //if room is the end to a floor
        public virtual bool IsStartRoom => false; // if room is the start of a floor
        public virtual bool IsRoomVariant => false; // if room uses the schematic of another room
        public int myRoom; // index in RoomList
        public bool initialized = false; // whether initialize has run yet
        public bool awake = false; // whether a player has ever stepped into this room
        public bool active = true; // whether the room has been completed or not
        public Vector2 RoomDimensions; // dimensions of the room
        public int roomTime; // time the room has been active
        public int closedTime; // time the room has been completed
        public const int RoomSpawnCap = 200;
        public Vector2 RoomPosition; //position of the room

        //potential NPC variables
        public Vector2[] NPCSpawnPosition = new Vector2[RoomSpawnCap];
        public int[] NPCToSpawn = new int[RoomSpawnCap];
        public int[] TimeUntilSpawn = new int[RoomSpawnCap];
        public int[] TelegraphDuration = new int[RoomSpawnCap];
        public float[] TelegraphSize = new float[RoomSpawnCap];
        public bool[] NotSpawned = new bool[RoomSpawnCap];

        public bool anyAlive = true; // whether any associated npcs are alive
        public int roomClearGraceTime = -1; // time gap of 1 seconds after the last enemy has spawned where a room cannot be considered cleared to prevent any accidents happening
        public int lastTelegraphDuration; // used for roomClearGraceTime
        public bool wallActive = false; // whether the barriers of the room are active
        public virtual void AddRoomNPC(int arrayLocation, Vector2 npcSpawnPosition, int npcToSpawn, int timeUntilSpawn, int telegraphDuration, float telegraphSize = 0)
        {
            NPCSpawnPosition[arrayLocation] = npcSpawnPosition + (RoomPosition * 16f);
            NPCToSpawn[arrayLocation] = npcToSpawn;
            TimeUntilSpawn[arrayLocation] = timeUntilSpawn;
            TelegraphDuration[arrayLocation] = telegraphDuration;
            if (telegraphSize == 0)
                telegraphSize = 1f;
            TelegraphSize[arrayLocation] = telegraphSize;
            NotSpawned[arrayLocation] = true;
        }
        public virtual void Update()
        {
            if (!awake) // not been touced yet? return
                return;

            if (!initialized) // initialize the room
                InitializeRoom();

            if (closedTime <= 60) //wall is visually active up to 1 second after room clear
                wallActive = true;

            if (!active) // room done, closed time increments
            {
                closedTime++;
                return;
            }
                
            WallUpdate(); // update wall logic
            PlayerItemsUpdate(); // update items from all players

            roomTime++; //time room is active

            for (int i = 0; i < RoomSpawnCap; i++)
            {
                if (TimeUntilSpawn[i] - roomTime == 0) //spawn pending enemy that has reached it's time
                {
                    SpawnManager.SpawnEnemy(NPCToSpawn[i], NPCSpawnPosition[i], myRoom, TelegraphDuration[i], TelegraphSize[i]);
                    lastTelegraphDuration = TelegraphDuration[i];
                    NotSpawned[i] = false;
                } 
            }

            // if there is still an enemy yet to be spawned, do not continue with room clear logic
            bool cancontinue = true;
            for (int i = 0; i < RoomSpawnCap; i++)
            {
                if (NotSpawned[i] == true)
                {
                    cancontinue = false;
                    break;
                }
            }

            if (cancontinue)
            {
                // start checking if any npcs in the world are active and associated with this room
                if (roomClearGraceTime == -1)
                {
                    roomClearGraceTime += lastTelegraphDuration + 60;
                }
                if (roomClearGraceTime > 0)
                    roomClearGraceTime--;

                anyAlive = false;
                for (int npc = 0; npc < Main.maxNPCs; npc++)
                {
                    if (Main.npc[npc] == null)
                        continue;
                    if (!Main.npc[npc].active)
                        continue;

                    if (!Main.npc[npc].GetGlobalNPC<TerRoguelikeGlobalNPC>().isRoomNPC)
                        continue;

                    if (Main.npc[npc].GetGlobalNPC<TerRoguelikeGlobalNPC>().sourceRoomListID == myRoom)
                    {
                        anyAlive = true;
                        break;
                    }
                }
            }
            if (!anyAlive && roomClearGraceTime == 0) // all associated enemies are gone. room cleared.
            {
                active = false;
                RoomClearReward();
            }
        }
        public virtual void InitializeRoom()
        {
            initialized = true;
            //sanity check for all npc slots
            for (int i = 0; i < NotSpawned.Length; i++)
            {
                NotSpawned[i] = false;
            }
        }
        public void WallUpdate()
        {
            for (int playerID = 0; playerID < Main.maxPlayers; playerID++) // keep players in the fucking room
            {
                var player = Main.player[playerID];
                bool boundLeft = (player.position.X + player.velocity.X) < (RoomPosition.X + 1f) * 16f;
                bool boundRight = (player.position.X + (float)player.width + player.velocity.X) > (RoomPosition.X - 1f + RoomDimensions.X) * 16f;
                bool boundTop = (player.position.Y + player.velocity.Y) < (RoomPosition.Y + 1f) * 16f;
                bool boundBottom = (player.position.Y + (float)player.height + player.velocity.Y) > (RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f;
                if (boundLeft)
                {
                    player.position.X = (RoomPosition.X + 1f) * 16f;
                    player.velocity.X = 0;
                }
                if (boundRight)
                {
                    player.position.X = ((RoomPosition.X - 1f + RoomDimensions.X) * 16f) - (float)player.width;
                    player.velocity.X = 0;
                }
                if (boundTop)
                {
                    player.position.Y = (RoomPosition.Y + 1f) * 16f;
                    player.velocity.Y = 0;
                }
                if (boundBottom)
                {
                    player.position.Y = ((RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f) - (float)player.height;
                    player.velocity.Y = 0;
                }

            }
            for (int npcID = 0; npcID < Main.maxNPCs; npcID++) // keep npcs in the fucking room
            {
                var npc = Main.npc[npcID];
                if (npc == null)
                    continue;
                if (!npc.active)
                    continue;
                if (!npc.GetGlobalNPC<TerRoguelikeGlobalNPC>().isRoomNPC)
                    continue;
                if (npc.GetGlobalNPC<TerRoguelikeGlobalNPC>().sourceRoomListID != myRoom)
                    continue;

                bool boundLeft = (npc.position.X + npc.velocity.X) < (RoomPosition.X + 1f) * 16f;
                bool boundRight = (npc.position.X + (float)npc.width + npc.velocity.X) > (RoomPosition.X - 1f + RoomDimensions.X) * 16f;
                bool boundTop = (npc.position.Y + npc.velocity.Y) < (RoomPosition.Y + 1f) * 16f;
                bool boundBottom = (npc.position.Y + (float)npc.height + npc.velocity.Y) > (RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f;
                if (boundLeft)
                {
                    npc.position.X = (RoomPosition.X + 1f) * 16f;
                    npc.velocity.X = 0;
                }
                if (boundRight)
                {
                    npc.position.X = ((RoomPosition.X - 1f + RoomDimensions.X) * 16f) - (float)npc.width;
                    npc.velocity.X = 0;
                }
                if (boundTop)
                {
                    npc.position.Y = (RoomPosition.Y + 1f) * 16f;
                    npc.velocity.Y = 0;
                }
                if (boundBottom)
                {
                    npc.position.Y = ((RoomPosition.Y - (1f) + RoomDimensions.Y) * 16f) - (float)npc.height;
                    npc.velocity.Y = 0;
                }
            }
        }
        public void RoomClearReward()
        {
            ClearPlanRockets();

            // reward. boss rooms give higher tiers.
            int chance = Main.rand.Next(1, 101);
            int itemType;
            int itemTier;

            if (IsBossRoom)
            {
                if (chance <= 80)
                {
                    itemType = ItemManager.GiveUncommon(false);
                    itemTier = 1;
                }
                else
                {
                    itemType = ItemManager.GiveRare(false);
                    itemTier = 2;
                }
                SpawnManager.SpawnItem(itemType, (RoomPosition + (RoomDimensions / 2f)) * 16f, itemTier, 75, 0.5f);
                return;
            }

            if (ItemManager.RoomRewardCooldown > 0)
            {
                ItemManager.RoomRewardCooldown--;
                return;
            }
                
            if (chance <= 80)
            {
                itemType = ItemManager.GiveCommon();
                itemTier = 0;
            }
            else if (chance <= 98)
            {
                itemType = ItemManager.GiveUncommon();
                itemTier = 1;
            }
            else
            {
                itemType = ItemManager.GiveRare();
                itemTier = 2;
            }
            SpawnManager.SpawnItem(itemType, (RoomPosition + (RoomDimensions / 2f)) * 16f, itemTier, 75, 0.5f);
        }
        public void PlayerItemsUpdate()
        {
            int totalAutomaticDefibrillator = 0;
            Vector2 roomCenter = new Vector2(RoomPosition.X + (RoomDimensions.X * 0.5f), RoomPosition.Y + (RoomDimensions.Y * 0.5f)) * 16f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player == null)
                    continue;
                if (!player.active)
                    continue;

                TerRoguelikePlayer modPlayer = player.GetModPlayer<TerRoguelikePlayer>();
                totalAutomaticDefibrillator += modPlayer.automaticDefibrillator;
            }

            if (totalAutomaticDefibrillator > 0)
            {
                int healTime = (int)(1500 * (4 / (float)(totalAutomaticDefibrillator + 4)));
                if (healTime <= 0)
                    healTime = 1;
                if (roomTime % healTime == 0 && roomTime > 0)
                {
                    RoomSystem.healingPulses.Add(new HealingPulse(roomCenter));
                }
            }

            if (roomTime == 30)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player == null)
                        continue;
                    if (!player.active)
                        continue;

                    TerRoguelikePlayer modPlayer = player.GetModPlayer<TerRoguelikePlayer>();
                    if (modPlayer.attackPlan <= 0)
                        continue;

                    int rocketCount = 4 + (4 * modPlayer.attackPlan);
                    RoomSystem.attackPlanRocketBundles.Add(new AttackPlanRocketBundle(roomCenter, rocketCount, player.whoAmI, myRoom));
                }
            }
        }
        public void ClearPlanRockets()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.type == ModContent.ProjectileType<PlanRocket>())
                {
                    proj.timeLeft = 60;
                }
            }
        }
    }
}
