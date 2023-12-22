﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using TerRoguelike;
using TerRoguelike.World;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria.Graphics;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using TerRoguelike.TerPlayer;
using TerRoguelike.Managers;
using System.IO;
using static TerRoguelike.Utilities.TerRoguelikeUtils;
using Terraria.DataStructures;
using TerRoguelike.Projectiles;

namespace TerRoguelike.NPCs
{
    public class TerRoguelikeGlobalNPC : GlobalNPC
    {
        #region Variables
        public bool isRoomNPC = false;
        public int sourceRoomListID = -1;
        public bool hostileTurnedAlly = false;

        //On kill bools to not let an npc somehow proc it more than once on death.
        public bool activatedHotPepper = false;
        public bool activatedSoulstealCoating = false;
        public bool activatedAmberBead = false;
        public bool activatedThrillOfTheHunt = false;
        public bool activatedClusterBombSatchel = false;
        public bool activatedSteamEngine = false;
        public bool activatedNutritiousSlime = false;
        public bool activatedItemPotentiometer = false;

        //debuffs
        public List<IgnitedStack> ignitedStacks = new List<IgnitedStack>();
        public int ignitedHitCooldown = 0;
        public List<BleedingStack> bleedingStacks = new List<BleedingStack>();
        public int bleedingHitCooldown = 0;
        public int ballAndChainSlow = 0;
        public Vector2 drawCenter = new Vector2(-1000);
        public int whoAmI;
        public int targetPlayer = -1;
        public int targetNPC = -1;
        public int friendlyFireHitCooldown = 0;
        public bool OverrideIgniteVisual = false;
        #endregion

        #region Base AIs
        public void RogueFighterAI(NPC npc, float xCap, float jumpVelocity)
        {
            Entity target = GetTarget(npc, false, false);

            if (target == null && npc.direction == 0)
            {
                npc.direction = 1;
                npc.spriteDirection = 1;
            }
            if (npc.ai[0] == 0 && (target != null ? target.active : false))
            {
                if (npc.Center.X < target.Center.X)
                {
                    npc.direction = 1;
                    npc.spriteDirection = 1;
                }
                else
                {
                    npc.direction = -1;
                    npc.spriteDirection = -1;
                }
            }
            else if (npc.ai[0] > 60)
            {
                npc.ai[0] = -240;
                npc.direction *= -1;
                npc.spriteDirection *= -1;
            }
            if (npc.ai[0] < 0)
                npc.ai[0]++;

            if (npc.velocity.X < -xCap || npc.velocity.X > xCap)
            {
                if (npc.velocity.Y == 0f)
                    npc.velocity *= 0.8f;
            }
            else if (npc.velocity.X < xCap && npc.direction == 1)
            {
                npc.velocity.X += 0.07f;
                if (npc.velocity.X > xCap)
                    npc.velocity.X = xCap;
            }
            else if (npc.velocity.X > -xCap && npc.direction == -1)
            {
                npc.velocity.X -= 0.07f;
                if (npc.velocity.X < -xCap)
                    npc.velocity.X = -xCap;
            }

            if (npc.collideX)
            {
                npc.ai[0]++;
                if (npc.collideY && npc.oldVelocity.Y >= 0)
                    npc.velocity.Y = jumpVelocity;
            }
            else if (npc.ai[0] > 0)
                npc.ai[0] = 0f;

            if (target != null)
            {
                if (npc.velocity.Y == 0f && target.Bottom.Y < npc.Top.Y && Math.Abs(npc.Center.X - target.Center.X) < (float)(target.width * 3) && Collision.CanHit(npc, target))
                {

                    if (npc.velocity.Y == 0f)
                    {
                        int padding = 6;
                        if (target.Bottom.Y > npc.Top.Y - (float)(padding * 16))
                        {
                            npc.velocity.Y = jumpVelocity;
                        }
                        else
                        {
                            int bottomtilepointx = (int)(npc.Center.X / 16f);
                            int bottomtilepointY = (int)(npc.Bottom.Y / 16f) - 1;
                            for (int i = bottomtilepointY; i > bottomtilepointY - padding; i--)
                            {
                                if (Main.tile[bottomtilepointx, i].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[bottomtilepointx, i].TileType])
                                {
                                    npc.velocity.Y = jumpVelocity;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (npc.velocity.Y == 0f && target.Top.Y > npc.Bottom.Y && Math.Abs(npc.Center.X - target.Center.X) < (float)(target.width * 3) && Collision.CanHit(npc, target))
                {
                    int fluff = 6;
                    int bottomtilepointx = (int)(npc.Center.X / 16f);
                    int bottomtilepointY = (int)(npc.Bottom.Y / 16f);
                    for (int i = bottomtilepointY; i > bottomtilepointY - fluff - 1; i--)
                    {
                        if (Main.tile[bottomtilepointx, i].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[bottomtilepointx, i].TileType])
                        {
                            npc.position.Y += 1;
                            npc.stairFall = true;
                            npc.velocity.Y += 0.01f;
                            break;
                        }
                    }
                }
            }

            if (npc.velocity.Y >= 0f)
            {
                int dir = 0;
                if (npc.velocity.X < 0f)
                    dir = -1;
                if (npc.velocity.X > 0f)
                    dir = 1;

                Vector2 futurePos = npc.position;
                futurePos.X += npc.velocity.X;
                int tileX = (int)((futurePos.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * dir)) / 16f);
                int tileY = (int)((futurePos.Y + (float)npc.height - 1f) / 16f);
                if (WorldGen.InWorld(tileX, tileY, 4))
                {
                    if ((float)(tileX * 16) < futurePos.X + (float)npc.width && (float)(tileX * 16 + 16) > futurePos.X && ((Main.tile[tileX, tileY].HasUnactuatedTile && !TopSlope(Main.tile[tileX, tileY]) && !TopSlope(Main.tile[tileX, tileY - 1]) && Main.tileSolid[Main.tile[tileX, tileY].TileType] && !Main.tileSolidTop[Main.tile[tileX, tileY].TileType]) || (Main.tile[tileX, tileY - 1].IsHalfBlock && Main.tile[tileX, tileY - 1].HasUnactuatedTile)) && (!Main.tile[tileX, tileY - 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 1].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 1].TileType] || (Main.tile[tileX, tileY - 1].IsHalfBlock && (!Main.tile[tileX, tileY - 4].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 4].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 4].TileType]))) && (!Main.tile[tileX, tileY - 2].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 2].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 2].TileType]) && (!Main.tile[tileX, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 3].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 3].TileType]) && (!Main.tile[tileX - dir, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX - dir, tileY - 3].TileType]))
                    {
                        float tilePosY = tileY * 16;
                        if (Main.tile[tileX, tileY].IsHalfBlock)
                            tilePosY += 8f;

                        if (Main.tile[tileX, tileY - 1].IsHalfBlock)
                            tilePosY -= 8f;

                        if (tilePosY < futurePos.Y + (float)npc.height)
                        {
                            float difference = futurePos.Y + (float)npc.height - tilePosY;
                            if (difference <= 16.1f)
                            {
                                npc.gfxOffY += npc.position.Y + (float)npc.height - tilePosY;
                                npc.position.Y = tilePosY - (float)npc.height;
                            }

                            if (difference < 9f)
                                npc.stepSpeed = 1f;
                            else
                                npc.stepSpeed = 2f;
                        }

                    }
                }
            }
        }
        public void RogueFighterShooterAI(NPC npc, float xCap, float jumpVelocity, float attackDistance, int attackTelegraph, int attackCooldown, float speedMultiWhenShooting, int projType, float projSpeed, Vector2 projOffset, int projDamage, bool LoSRequired)
        {
            Entity target = GetTarget(npc, false, false);

            float realXCap = npc.ai[1] > 0 ? xCap * speedMultiWhenShooting : xCap;

            if (npc.ai[1] != 0)
                npc.ai[1]++;

            if (target == null)
            {
                if (npc.ai[1] > 0)
                    npc.ai[1] = 0;
            }
            else
            {
                if (npc.ai[1] == 0 && npc.ai[0] >= 0 && (npc.Center - target.Center).Length() <= attackDistance && (!LoSRequired || Collision.CanHit(npc.Center + projOffset, 1, 1, target.Center, 1, 1)))
                {
                    npc.ai[1]++;
                }
                
                if (npc.ai[1] >= attackTelegraph)
                {
                    Vector2 projSpawnPos = npc.Center + projOffset;
                    int proj = Projectile.NewProjectile(npc.GetSource_FromThis(), projSpawnPos, (target.Center - projSpawnPos).SafeNormalize(Vector2.UnitY) * projSpeed, projType, projDamage, 0f);
                    SetUpNPCProj(npc, proj);
                    npc.ai[1] = -attackCooldown;
                }
            }

            if (target == null && npc.direction == 0)
            {
                npc.direction = 1;
                npc.spriteDirection = 1;
            }
            if (npc.ai[0] == 0 && (target != null ? target.active : false))
            {
                if (npc.Center.X < target.Center.X)
                {
                    npc.direction = 1;
                    npc.spriteDirection = 1;
                }
                else
                {
                    npc.direction = -1;
                    npc.spriteDirection = -1;
                }
            }
            else if (npc.ai[0] > 60 && npc.ai[1] <= 0)
            {
                npc.ai[0] = -240;
                npc.direction *= -1;
                npc.spriteDirection *= -1;
            }
            if (npc.ai[0] < 0)
                npc.ai[0]++;

            if (npc.velocity.X < -realXCap || npc.velocity.X > realXCap)
            {
                if (npc.velocity.Y == 0f)
                    npc.velocity *= 0.8f;
            }
            else if (npc.velocity.X < realXCap && npc.direction == 1)
            {
                npc.velocity.X += 0.07f;
                if (npc.velocity.X > realXCap)
                    npc.velocity.X = realXCap;
            }
            else if (npc.velocity.X > -realXCap && npc.direction == -1)
            {
                npc.velocity.X -= 0.07f;
                if (npc.velocity.X < -realXCap)
                    npc.velocity.X = -realXCap;
            }

            if (npc.collideX)
            {
                npc.ai[0]++;
                if (npc.collideY && npc.oldVelocity.Y >= 0)
                    npc.velocity.Y = jumpVelocity;
            }
            else if (npc.ai[0] > 0)
                npc.ai[0] = 0f;

            if (target != null)
            {
                if (npc.velocity.Y == 0f && target.Bottom.Y < npc.Top.Y && Math.Abs(npc.Center.X - target.Center.X) < (float)(target.width * 3) && Collision.CanHit(npc, target))
                {

                    if (npc.velocity.Y == 0f)
                    {
                        int padding = 6;
                        if (target.Bottom.Y > npc.Top.Y - (float)(padding * 16))
                        {
                            npc.velocity.Y = jumpVelocity;
                        }
                        else
                        {
                            int bottomtilepointx = (int)(npc.Center.X / 16f);
                            int bottomtilepointY = (int)(npc.Bottom.Y / 16f) - 1;
                            for (int i = bottomtilepointY; i > bottomtilepointY - padding; i--)
                            {
                                if (Main.tile[bottomtilepointx, i].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[bottomtilepointx, i].TileType])
                                {
                                    npc.velocity.Y = jumpVelocity;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (npc.velocity.Y == 0f && target.Top.Y > npc.Bottom.Y && Math.Abs(npc.Center.X - target.Center.X) < (float)(target.width * 3) && Collision.CanHit(npc, target))
                {
                    int fluff = 6;
                    int bottomtilepointx = (int)(npc.Center.X / 16f);
                    int bottomtilepointY = (int)(npc.Bottom.Y / 16f);
                    for (int i = bottomtilepointY; i > bottomtilepointY - fluff - 1; i--)
                    {
                        if (Main.tile[bottomtilepointx, i].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[bottomtilepointx, i].TileType])
                        {
                            npc.position.Y += 1;
                            npc.stairFall = true;
                            npc.velocity.Y += 0.01f;
                            break;
                        }
                    }
                }
            }

            if (npc.velocity.Y >= 0f)
            {
                int dir = 0;
                if (npc.velocity.X < 0f)
                    dir = -1;
                if (npc.velocity.X > 0f)
                    dir = 1;

                Vector2 futurePos = npc.position;
                futurePos.X += npc.velocity.X;
                int tileX = (int)((futurePos.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * dir)) / 16f);
                int tileY = (int)((futurePos.Y + (float)npc.height - 1f) / 16f);
                if (WorldGen.InWorld(tileX, tileY, 4))
                {
                    if ((float)(tileX * 16) < futurePos.X + (float)npc.width && (float)(tileX * 16 + 16) > futurePos.X && ((Main.tile[tileX, tileY].HasUnactuatedTile && !TopSlope(Main.tile[tileX, tileY]) && !TopSlope(Main.tile[tileX, tileY - 1]) && Main.tileSolid[Main.tile[tileX, tileY].TileType] && !Main.tileSolidTop[Main.tile[tileX, tileY].TileType]) || (Main.tile[tileX, tileY - 1].IsHalfBlock && Main.tile[tileX, tileY - 1].HasUnactuatedTile)) && (!Main.tile[tileX, tileY - 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 1].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 1].TileType] || (Main.tile[tileX, tileY - 1].IsHalfBlock && (!Main.tile[tileX, tileY - 4].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 4].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 4].TileType]))) && (!Main.tile[tileX, tileY - 2].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 2].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 2].TileType]) && (!Main.tile[tileX, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 3].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 3].TileType]) && (!Main.tile[tileX - dir, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX - dir, tileY - 3].TileType]))
                    {
                        float tilePosY = tileY * 16;
                        if (Main.tile[tileX, tileY].IsHalfBlock)
                            tilePosY += 8f;

                        if (Main.tile[tileX, tileY - 1].IsHalfBlock)
                            tilePosY -= 8f;

                        if (tilePosY < futurePos.Y + (float)npc.height)
                        {
                            float difference = futurePos.Y + (float)npc.height - tilePosY;
                            if (difference <= 16.1f)
                            {
                                npc.gfxOffY += npc.position.Y + (float)npc.height - tilePosY;
                                npc.position.Y = tilePosY - (float)npc.height;
                            }

                            if (difference < 9f)
                                npc.stepSpeed = 1f;
                            else
                                npc.stepSpeed = 2f;
                        }

                    }
                }
            }
        }
        public void RogueTortoiseAI(NPC npc, float xCap, float jumpVelocity, int jumpTime, int dashTime, float dashVelocity, int attackCooldown, int attackTelegraph)
        {
            Entity target = GetTarget(npc, false, false);

            if (npc.ai[1] < attackCooldown && npc.ai[1] >= 0)
                npc.ai[1]++;

            if (target == null && npc.direction == 0)
            {
                npc.direction = 1;
                npc.spriteDirection = 1;
            }
            if (npc.ai[0] == 0 && (target != null ? target.active : false) && npc.ai[1] >= 0)
            {
                if (npc.Center.X < target.Center.X)
                {
                    npc.direction = 1;
                    npc.spriteDirection = 1;
                }
                else
                {
                    npc.direction = -1;
                    npc.spriteDirection = -1;
                }
            }
            else if (npc.ai[0] > 60 && npc.ai[1] >= 0)
            {
                npc.ai[0] = -240;
                npc.direction *= -1;
                npc.spriteDirection *= -1;
            }
            if (npc.ai[0] < 0)
                npc.ai[0]++;

            if (npc.ai[0] >= 0 && npc.ai[1] >= attackCooldown && npc.collideY)
            {
                npc.ai[1] = -jumpTime - dashTime;
            }

            if (npc.ai[1] >= 0 && npc.ai[1] < attackCooldown - attackTelegraph)
            {
                if (npc.velocity.X < -xCap || npc.velocity.X > xCap)
                {
                    if (npc.velocity.Y == 0f)
                        npc.velocity *= 0.8f;
                }
                else if (npc.velocity.X < xCap && npc.direction == 1)
                {
                    npc.velocity.X += 0.07f;
                    if (npc.velocity.X > xCap)
                        npc.velocity.X = xCap;
                }
                else if (npc.velocity.X > -xCap && npc.direction == -1)
                {
                    npc.velocity.X -= 0.07f;
                    if (npc.velocity.X < -xCap)
                        npc.velocity.X = -xCap;
                }
            }
            else
            {
                if (npc.ai[1] < -dashTime)
                {
                    npc.ai[1]++;
                    npc.velocity.Y = jumpVelocity;
                }
                else if (npc.collideY && npc.oldVelocity.Y > 0)
                {
                    if (npc.ai[1] == -dashTime)
                    {
                        npc.ai[1]++;
                        npc.velocity.X = dashVelocity * npc.direction;
                        
                    }
                }
                if (npc.ai[1] > -dashTime)
                {
                    npc.stairFall = true;
                    if (npc.collideX)
                    {
                        npc.direction *= -1;
                        npc.spriteDirection *= -1;
                        npc.velocity.X = npc.oldVelocity.X * -0.8f;
                    }
                    if (npc.collideY)
                    {
                        npc.velocity.Y = npc.oldVelocity.Y * -0.75f;
                        npc.velocity.X *= 0.9f;
                    }
                    npc.ai[1]++;
                }
            }
            

            if (npc.collideX)
            {
                npc.ai[0]++;
            }
            else if (npc.ai[0] > 0)
                npc.ai[0] = 0f;

            if (target != null)
            {
                if (npc.velocity.Y == 0f && target.Top.Y > npc.Bottom.Y && Math.Abs(npc.Center.X - target.Center.X) < (float)(target.width * 3) && Collision.CanHit(npc, target))
                {
                    int fluff = 6;
                    int bottomtilepointx = (int)(npc.Center.X / 16f);
                    int bottomtilepointY = (int)(npc.Bottom.Y / 16f);
                    for (int i = bottomtilepointY; i > bottomtilepointY - fluff - 1; i--)
                    {
                        if (Main.tile[bottomtilepointx, i].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[bottomtilepointx, i].TileType])
                        {
                            npc.position.Y += 1;
                            npc.stairFall = true;
                            npc.velocity.Y += 0.01f;
                            break;
                        }
                    }
                }
            }

            if (npc.velocity.Y >= 0f)
            {
                int dir = 0;
                if (npc.velocity.X < 0f)
                    dir = -1;
                if (npc.velocity.X > 0f)
                    dir = 1;

                Vector2 futurePos = npc.position;
                futurePos.X += npc.velocity.X;
                int tileX = (int)((futurePos.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * dir)) / 16f);
                int tileY = (int)((futurePos.Y + (float)npc.height - 1f) / 16f);
                if (WorldGen.InWorld(tileX, tileY, 4))
                {
                    if ((float)(tileX * 16) < futurePos.X + (float)npc.width && (float)(tileX * 16 + 16) > futurePos.X && ((Main.tile[tileX, tileY].HasUnactuatedTile && !TopSlope(Main.tile[tileX, tileY]) && !TopSlope(Main.tile[tileX, tileY - 1]) && Main.tileSolid[Main.tile[tileX, tileY].TileType] && !Main.tileSolidTop[Main.tile[tileX, tileY].TileType]) || (Main.tile[tileX, tileY - 1].IsHalfBlock && Main.tile[tileX, tileY - 1].HasUnactuatedTile)) && (!Main.tile[tileX, tileY - 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 1].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 1].TileType] || (Main.tile[tileX, tileY - 1].IsHalfBlock && (!Main.tile[tileX, tileY - 4].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 4].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 4].TileType]))) && (!Main.tile[tileX, tileY - 2].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 2].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 2].TileType]) && (!Main.tile[tileX, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 3].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 3].TileType]) && (!Main.tile[tileX - dir, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX - dir, tileY - 3].TileType]))
                    {
                        float tilePosY = tileY * 16;
                        if (Main.tile[tileX, tileY].IsHalfBlock)
                            tilePosY += 8f;

                        if (Main.tile[tileX, tileY - 1].IsHalfBlock)
                            tilePosY -= 8f;

                        if (tilePosY < futurePos.Y + (float)npc.height)
                        {
                            float difference = futurePos.Y + (float)npc.height - tilePosY;
                            if (difference <= 16.1f)
                            {
                                npc.gfxOffY += npc.position.Y + (float)npc.height - tilePosY;
                                npc.position.Y = tilePosY - (float)npc.height;
                            }

                            if (difference < 9f)
                                npc.stepSpeed = 1f;
                            else
                                npc.stepSpeed = 2f;
                        }

                    }
                }
            }
            if (npc.ai[1] > attackCooldown - attackTelegraph)
            {
                npc.velocity.X *= 0.8f;
            }
        }
        public void RogueSpookrowAI(NPC npc, float xCap, float jumpVelocity)
        {
            Entity target = GetTarget(npc, false, false);

            int slopeCheck1 = (int)Main.tile[(int)(npc.BottomLeft.X / 16f), (int)(npc.BottomLeft.Y / 16f)].Slope;
            int slopeCheck2 = (int)Main.tile[(int)(npc.BottomRight.X / 16f), (int)(npc.BottomRight.Y / 16f)].Slope;
            if (npc.velocity.Y >= 0 && (slopeCheck1 == 1 || slopeCheck1 == 2 || slopeCheck2 == 1 || slopeCheck2 == 2))
            {
                npc.collideY = true;
            }
                

            if (npc.collideY)
            {
                npc.velocity.X *= 0.6f;
                if (npc.ai[0] > 60 && npc.oldVelocity.Y >= 0)
                    npc.ai[0] = 30;
            }
                

            npc.velocity.X *= 0.98f;
            if (npc.collideX)
                npc.velocity.X = npc.oldVelocity.X * -0.5f;

            if (target != null)
            {
                if (npc.Center.X < target.Center.X)
                    npc.direction = 1;
                else
                    npc.direction = -1;
            }
            else
            {
                if (npc.velocity.X != 0)
                {
                    npc.direction = -(int)(npc.velocity.X / Math.Abs(npc.velocity.X));
                }
            }
            
            
            if (npc.ai[0] == 0)
            {
                npc.spriteDirection = npc.direction;
            }
            if (npc.ai[0] >= 30 && npc.ai[0] < 60)
                npc.spriteDirection = npc.direction;
            if (npc.ai[0] != 60)
            {
                npc.ai[0]++;
                if (!npc.collideY && npc.ai[0] < 60)
                    npc.ai[0]--;
            }
            if (npc.ai[0] == 60 && npc.collideY)
            {
                npc.velocity.X = xCap * npc.direction;
                npc.velocity.Y = jumpVelocity;
                npc.ai[0]++;
            }

            

            if (target != null)
            {
                if (npc.velocity.Y == 0f && target.Top.Y > npc.Bottom.Y && Math.Abs(npc.Center.X - target.Center.X) < (float)(target.width * 4) && Collision.CanHit(npc, target))
                {
                    int fluff = 6;
                    int bottomtilepointx = (int)(npc.Center.X / 16f);
                    int bottomtilepointY = (int)(npc.Bottom.Y / 16f);
                    for (int i = bottomtilepointY; i > bottomtilepointY - fluff - 1; i--)
                    {
                        if (Main.tile[bottomtilepointx, i].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[bottomtilepointx, i].TileType])
                        {
                            npc.position.Y += 1;
                            npc.stairFall = true;
                            npc.velocity.Y += 0.01f;
                            break;
                        }
                    }
                }
            }
            
            if (npc.velocity.Y >= 0f)
            {
                int dir = 0;
                if (npc.velocity.X < 0f)
                    dir = -1;
                if (npc.velocity.X > 0f)
                    dir = 1;

                Vector2 futurePos = npc.position;
                futurePos.X += npc.velocity.X;
                int tileX = (int)((futurePos.X + (float)(npc.width / 2) + (float)((npc.width / 2 + 1) * dir)) / 16f);
                int tileY = (int)((futurePos.Y + (float)npc.height - 1f) / 16f);
                if (WorldGen.InWorld(tileX, tileY, 4))
                {
                    if ((float)(tileX * 16) < futurePos.X + (float)npc.width && (float)(tileX * 16 + 16) > futurePos.X && ((Main.tile[tileX, tileY].HasUnactuatedTile && !TopSlope(Main.tile[tileX, tileY]) && !TopSlope(Main.tile[tileX, tileY - 1]) && Main.tileSolid[Main.tile[tileX, tileY].TileType] && !Main.tileSolidTop[Main.tile[tileX, tileY].TileType]) || (Main.tile[tileX, tileY - 1].IsHalfBlock && Main.tile[tileX, tileY - 1].HasUnactuatedTile)) && (!Main.tile[tileX, tileY - 1].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 1].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 1].TileType] || (Main.tile[tileX, tileY - 1].IsHalfBlock && (!Main.tile[tileX, tileY - 4].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 4].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 4].TileType]))) && (!Main.tile[tileX, tileY - 2].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 2].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 2].TileType]) && (!Main.tile[tileX, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX, tileY - 3].TileType] || Main.tileSolidTop[Main.tile[tileX, tileY - 3].TileType]) && (!Main.tile[tileX - dir, tileY - 3].HasUnactuatedTile || !Main.tileSolid[Main.tile[tileX - dir, tileY - 3].TileType]))
                    {
                        float tilePosY = tileY * 16;

                        if (Main.tile[tileX, tileY].IsHalfBlock)
                            tilePosY += 8f;

                        if (Main.tile[tileX, tileY - 1].IsHalfBlock)
                            tilePosY -= 8f;

                        if (tilePosY < futurePos.Y + (float)npc.height)
                        {
                            float difference = futurePos.Y + (float)npc.height - tilePosY;
                            if (difference <= 16.1f)
                            {
                                npc.gfxOffY += npc.position.Y + (float)npc.height - tilePosY;
                                npc.position.Y = tilePosY - (float)npc.height;
                            }

                            if (difference < 9f)
                                npc.stepSpeed = 1f;
                            else
                                npc.stepSpeed = 2f;
                        }

                    }
                }
            }
        }

        public void RogueWormAI(NPC npc, float maxVelocity, float turnRadians, float slowTurnDist)
        {
            Entity target = GetTarget(npc, false, false);
            Vector2 targetPos = npc.Center + Vector2.UnitX.RotatedBy(-npc.rotation);
            if (target != null)
            {
                targetPos = target.Center;
            }

            float targetAngle = (targetPos - npc.Center).ToRotation();
            bool slowTurn = npc.ai[0] == 0;
            float newAngle = npc.rotation.AngleTowards(targetAngle, slowTurn ? turnRadians : turnRadians * 0.4f);
            float angleChange = (float)Math.Atan2(Math.Sin(newAngle - npc.rotation), Math.Cos(newAngle - npc.rotation));
            if ((targetPos - npc.Center).Length() > slowTurnDist)
            {
                npc.ai[0] = 0;
            }
            if (Math.Abs(angleChange) <= turnRadians * 0.3f || (targetPos - npc.Center).Length() < slowTurnDist * 0.75f)
                npc.ai[0] = 1;

            float velMultiplier = slowTurn ? Math.Abs(Vector2.Dot((npc.rotation + MathHelper.PiOver2).ToRotationVector2(), targetAngle.ToRotationVector2())) : 0;
            npc.rotation = newAngle;
            Vector2 wantedVelocity = slowTurn ? npc.rotation.ToRotationVector2() * (maxVelocity * ((((1f - velMultiplier * 0.85f)) + 0.15f))) : npc.rotation.ToRotationVector2() * maxVelocity;
            npc.velocity = Vector2.Lerp(npc.velocity, wantedVelocity, 0.75f);
        }
        public void RogueGiantBatAI(NPC npc, float distanceAbove, float acceleration, float maxVelocity, int attackTelegraph, int attackCooldown, float attackDistance, int projType, Vector2 projVelocity, int projDamage)
        {
            Entity target = GetTarget(npc, false, false);

            npc.stairFall = true;

            if (npc.collideY)
            {
                int fluff = 6;
                int bottomtilepointx = (int)(npc.Center.X / 16f);
                int bottomtilepointY = (int)(npc.Bottom.Y / 16f);
                for (int i = bottomtilepointY; i > bottomtilepointY - fluff - 1; i--)
                {
                    if (Main.tile[bottomtilepointx, i].HasUnactuatedTile && TileID.Sets.Platforms[Main.tile[bottomtilepointx, i].TileType])
                    {
                        npc.position.Y += 1;
                        npc.stairFall = true;
                        npc.velocity.Y += 0.01f;
                        break;
                    }
                }
            }

            if (target != null)
            {
                Vector2 targetPos = target.Center + new Vector2(0, -distanceAbove);
                if ((npc.Center - targetPos).Length() < attackDistance)
                {
                    npc.velocity *= 0.95f;
                    if (npc.ai[0] == 0)
                    {
                        npc.ai[0]++;
                    }
                }
                else
                {
                    npc.velocity += (targetPos - npc.Center).SafeNormalize(Vector2.UnitY) * acceleration;
                }
                if (npc.velocity.Length() > maxVelocity)
                {
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * maxVelocity;
                }

                if (npc.ai[0] != 0)
                {
                    npc.ai[0]++;
                }

                if (npc.ai[0] >= attackTelegraph)
                {
                    npc.ai[0] = -attackCooldown;
                    int proj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVelocity, projType, projDamage, 0f);
                    SetUpNPCProj(npc, proj);
                }
            }
            else
            {
                if (npc.ai[0] != 0)
                {
                    npc.ai[0]++;
                }

                if (npc.ai[0] >= attackTelegraph)
                {
                    npc.ai[0] = -attackCooldown;
                    int proj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, projVelocity, projType, projDamage, 0f);
                    SetUpNPCProj(npc, proj);
                }

                npc.velocity.X += (npc.velocity.X == 0 ? 1 : npc.velocity.X / Math.Abs(npc.velocity.X)) * acceleration;
                npc.velocity.Y += Main.rand.NextFloat(-acceleration / 2f, acceleration / 2f);
                
                if (npc.velocity.Length() > maxVelocity)
                {
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitX) * maxVelocity;
                }
            }

            if (npc.collideX)
            {
                npc.velocity.X *= -0.75f;
            }
            if (npc.collideY)
            {
                npc.velocity.Y *= -0.25f;
            }
        }
        public void RogueFrostbiterAI(NPC npc, float distanceBeside, int dashTime, float dashVelocity, float passiveAccel, float passiveMaxVelocity, int attackTelegraph, int waitTimeAfterAttack, float attackDistance, int projType, float projVelocity, int projDamage, int projCount)
        {
            //ai0 is for attack timers.
            //ai1 is for attack state.
            //ai2 and ai3 store a vector.

            Entity target = GetTarget(npc, false, false);

            if (npc.ai[0] == 0)
            {
                if (npc.ai[1] == 0)
                {
                    if (target != null)
                    {
                        if (npc.Center.X >= target.Center.X)
                            npc.direction = 1;
                        else
                            npc.direction = -1;
                    }
                    else
                    {
                        if (npc.direction == 0)
                            npc.direction = -1;
                        else
                            npc.direction *= -1;
                    }
                    
                }
                else if (npc.direction != 0)
                {
                    Vector2 nextTarget = Main.rand.NextVector2CircularEdge(attackDistance, attackDistance);
                    nextTarget.X = Math.Abs(nextTarget.X) * -npc.direction;
                    nextTarget += target == null ? npc.Center : target.Center;
                    npc.ai[2] = nextTarget.X;
                    npc.ai[3] = nextTarget.Y;
                    if (target != null)
                        npc.direction = 0;
                }
            }

            if (npc.ai[0] != 0 || target == null)
            {
                npc.ai[0]++;
                if (npc.ai[0] < attackTelegraph)
                    npc.velocity *= 0.93f;
            }

            if (target != null)
            {
                Vector2 targetPos = npc.ai[1] == 0 ? new Vector2(distanceBeside * npc.direction, 0) + target.Center : new Vector2(npc.ai[2], npc.ai[3]);

                float greaterDist = attackDistance > distanceBeside ? attackDistance : distanceBeside;
                if ((npc.Center - targetPos).Length() < 64f || ((npc.collideX || npc.collideY) && (npc.Center - target.Center).Length() < greaterDist))
                {
                    if (npc.ai[0] == 0)
                        npc.ai[0]++;
                }
                else if (npc.ai[0] == 0)
                {
                    npc.velocity += (targetPos - npc.Center).SafeNormalize(Vector2.UnitY) * ((npc.Center - targetPos).Length() < 64f ? passiveAccel : passiveAccel * 2f);
                }
                if (npc.velocity.Length() > passiveMaxVelocity && npc.ai[0] < attackTelegraph)
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.UnitY) * passiveMaxVelocity;
            }

            if (npc.ai[0] >= attackTelegraph)
            {
                if (npc.ai[1] == 0 && npc.ai[0] == attackTelegraph)
                {
                    npc.velocity = target != null ? (target.Center - npc.Center).SafeNormalize(-Vector2.UnitX * npc.direction) * dashVelocity : -Vector2.UnitX * npc.direction * dashVelocity;
                }
                else if (npc.ai[1] == 1)
                {
                    float anglePerProj = MathHelper.TwoPi / projCount;
                    for (int i = 0; i < projCount; i++)
                    {
                        int proj = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, (-Vector2.UnitY * projVelocity).RotatedBy(anglePerProj * i), projType, projDamage, 0f);
                        SetUpNPCProj(npc, proj);
                    }
                }

                if (npc.ai[1] == 1 || npc.ai[0] >= attackTelegraph + dashTime)
                {
                    npc.ai[0] = -waitTimeAfterAttack;
                    if (npc.ai[1] == 0)
                        npc.ai[1] = 1;
                    else
                        npc.ai[1] = 0;
                }
            }
            Main.NewText(npc.direction);
        }

        public void UpdateWormSegments(ref List<WormSegment> segments, NPC npc)
        {
            for (int i = 0; i < segments.Count; i++)
            {
                WormSegment segment = segments[i];
                segment.OldPosition = segment.Position;
                segment.OldRotation = segment.Rotation;
                if (i == 0)
                {
                    segment.Position += npc.velocity;
                    segment.Rotation = npc.rotation;
                    continue;
                }

                WormSegment oldSeg = segments[i - 1];

                segment.Position = oldSeg.Position - (Vector2.UnitX * oldSeg.Height).RotatedBy(oldSeg.Rotation.AngleLerp((oldSeg.Position - segment.Position).ToRotation(), 0.95f));

                Vector2 difference = oldSeg.Position - segment.Position;

                segment.Rotation = (difference).ToRotation();
            }
        }

        #endregion

        public override bool InstancePerEntity => true;
        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            whoAmI = npc.whoAmI;
        }
        public override bool PreAI(NPC npc)
        {
            if (ballAndChainSlow > 0) // grant slowed velocity back as an attempt to make the ai run normall as if it was going full speed
            {
                npc.velocity /= 0.7f;
                ballAndChainSlow--;
            }
            return true;
        }
        public override void PostAI(NPC npc)
        {
            if (ignitedStacks != null && ignitedStacks.Any()) // ignite debuff logic
            {
                if (ignitedHitCooldown <= 0)
                {
                    int hitDamage = 0;
                    int targetDamage = (int)(npc.lifeMax * 0.01f);
                    int damageCap = 50;
                    int owner = -1;
                    if (targetDamage > damageCap)
                        targetDamage = damageCap;
                    else if (targetDamage < 1)
                        targetDamage = 1;

                    for (int i = 0; i < ignitedStacks.Count; i++)
                    {
                        if (ignitedStacks[i].DamageToDeal < targetDamage)
                        {
                            hitDamage += ignitedStacks[i].DamageToDeal;
                            ignitedStacks[i].DamageToDeal = 0;
                        }
                        else
                        {
                            hitDamage += targetDamage;
                            ignitedStacks[i].DamageToDeal -= targetDamage;
                        }
                        if (i == ignitedStacks.Count - 1)
                        {
                            owner = ignitedStacks[i].Owner;
                        }
                    }
                    IgniteHit(hitDamage, npc, owner);
                    ignitedStacks.RemoveAll(x => x.DamageToDeal <= 0);
                }
            }
            if (ignitedHitCooldown > 0)
                ignitedHitCooldown--;

            if (bleedingStacks != null && bleedingStacks.Any()) // bleeding debuff logic
            {
                if (bleedingHitCooldown <= 0)
                {
                    int hitDamage = 0;
                    int targetDamage = 40;
                    int owner = -1;

                    for (int i = 0; i < bleedingStacks.Count; i++)
                    {
                        if (bleedingStacks[i].DamageToDeal < targetDamage)
                        {
                            hitDamage += ignitedStacks[i].DamageToDeal;
                            bleedingStacks[i].DamageToDeal = 0;
                        }
                        else
                        {
                            hitDamage += targetDamage;
                            bleedingStacks[i].DamageToDeal -= targetDamage;
                        }
                        if (i == bleedingStacks.Count - 1)
                        {
                            owner = bleedingStacks[i].Owner;
                        }
                    }
                    BleedingHit(hitDamage, npc, owner);
                    bleedingStacks.RemoveAll(x => x.DamageToDeal <= 0);
                }
            }
            if (bleedingHitCooldown > 0)
                bleedingHitCooldown--;

            if (ballAndChainSlow > 0) // slow down
            {
                npc.velocity *= 0.7f;
                ballAndChainSlow--;
            }

            if (hostileTurnedAlly)
            {
                if (friendlyFireHitCooldown == 0)
                {
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC target = Main.npc[i];
                        if (!target.active)
                            continue;
                        if (!target.GetGlobalNPC<TerRoguelikeGlobalNPC>().CanBeChased(false, false))
                            continue;

                        if (npc.getRect().Intersects(target.getRect()))
                        {
                            NPC.HitInfo info = new NPC.HitInfo();
                            info.HideCombatText = true;
                            info.Damage = npc.damage;
                            info.InstantKill = false;
                            info.HitDirection = 1;
                            info.Knockback = 0f;
                            info.Crit = false;

                            target.StrikeNPC(info);
                            NetMessage.SendStrikeNPC(target, info);
                            CombatText.NewText(target.getRect(), Color.Orange, npc.damage);
                            friendlyFireHitCooldown += 20;
                        }
                    }
                }
                else if (friendlyFireHitCooldown > 0)
                    friendlyFireHitCooldown--;
                
            }
        }
        public void IgniteHit(int hitDamage, NPC npc, int owner)
        {
            TerRoguelikePlayer modPlayer = Main.player[owner].GetModPlayer<TerRoguelikePlayer>();

            hitDamage = (int)(hitDamage * modPlayer.GetBonusDamageMulti(npc, npc.Center));

            if (npc.life - hitDamage <= 0)
            {
                modPlayer.OnKillEffects(npc);
            }
            NPC.HitInfo info = new NPC.HitInfo();
            info.HideCombatText = true;
            info.Damage = hitDamage;
            info.InstantKill = false;
            info.HitDirection = 1;
            info.Knockback = 0f;
            info.Crit = false;

            npc.StrikeNPC(info);
            NetMessage.SendStrikeNPC(npc, info);
            CombatText.NewText(npc.getRect(), Color.DarkKhaki, hitDamage);
            ignitedHitCooldown += 10; // hits 6 times a second
        }
        public void BleedingHit(int hitDamage, NPC npc, int owner)
        {
            TerRoguelikePlayer modPlayer = Main.player[owner].GetModPlayer<TerRoguelikePlayer>();

            hitDamage = (int)(hitDamage * modPlayer.GetBonusDamageMulti(npc, npc.Center));

            if (npc.life - hitDamage <= 0)
            {
                modPlayer.OnKillEffects(npc);
            }
            NPC.HitInfo info = new NPC.HitInfo();
            info.HideCombatText = true;
            info.Damage = hitDamage;
            info.InstantKill = false;
            info.HitDirection = 1;
            info.Knockback = 0f;
            info.Crit = false;

            npc.StrikeNPC(info);
            NetMessage.SendStrikeNPC(npc, info);
            CombatText.NewText(npc.getRect(), Color.MediumVioletRed, hitDamage);
            bleedingHitCooldown = 20; // hits 3 times a second
        }
        public void AddBleedingStackWithRefresh(BleedingStack stack)
        {
            if (bleedingStacks.Any())
            {
                for (int i = 0; i < bleedingStacks.Count; i++)
                {
                    if (bleedingStacks[i].DamageToDeal < stack.DamageToDeal)
                        bleedingStacks[i].DamageToDeal = stack.DamageToDeal;
                }
            }
            else
                bleedingHitCooldown += 20;

            bleedingStacks.Add(stack);
        }
        public override bool PreKill(NPC npc)
        {
            if (!isRoomNPC)
                return true;

            //stop all room npcs from dropping shit at all 

            var AllLoadedItemIDs = new int[ItemLoader.ItemCount];
            for (int i = 0; i < ItemLoader.ItemCount; i++)
            {
                AllLoadedItemIDs[i] = i;
            }
            foreach (int itemID in AllLoadedItemIDs)
            {
                NPCLoader.blockLoot.Add(itemID);
            }
            npc.value = 0;
            
            return true;
        }
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (TerRoguelikeWorld.IsTerRoguelikeWorld)
            {
                modifiers.DefenseEffectiveness *= 0f;
            }
        }
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (ignitedStacks != null && ignitedStacks.Any() && !OverrideIgniteVisual)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);


                Texture2D texture = TextureAssets.Npc[npc.type].Value;
                Color color = Color.Lerp(Color.Yellow, Color.OrangeRed, Main.rand.NextFloat(0.4f, 0.6f + float.Epsilon) + 0.2f + (0.2f * (float)Math.Cos((Main.GlobalTimeWrappedHourly * 20f)))) * 0.8f;
                Vector3 colorHSL = Main.rgbToHsl(color);
                float outlineThickness = 1f;
                Vector2 vector = new Vector2(npc.frame.Width / 2f, npc.frame.Height / 2f);
                SpriteEffects spriteEffects = npc.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                GameShaders.Misc["TerRoguelike:BasicTint"].UseOpacity(1f);
                GameShaders.Misc["TerRoguelike:BasicTint"].UseColor(Main.hslToRgb(1 - colorHSL.X, colorHSL.Y, colorHSL.Z));
                GameShaders.Misc["TerRoguelike:BasicTint"].Apply();

                Vector2 position = GetDrawCenter(npc) + (Vector2.UnitY * npc.gfxOffY);
                for (float i = 0; i < 1; i += 0.125f)
                {
                    spriteBatch.Draw(texture, position + (i * MathHelper.TwoPi + npc.rotation).ToRotationVector2() * outlineThickness - Main.screenPosition, npc.frame, color, npc.rotation, vector, npc.scale, spriteEffects, 0f);
                }

                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            }

            if (bleedingStacks != null && bleedingStacks.Any())
            {
                DrawRotatlingBloodParticles(false, npc);
            }

            return true;
        }
        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (ignitedStacks != null && ignitedStacks.Any())
            {
                drawColor = Color.Lerp(Color.White, Color.OrangeRed, 0.4f);
                for (int i = 0; i < ignitedStacks.Count; i++)
                {
                    if (Main.rand.NextBool(5))
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Torch);
                }
            }
            if (ballAndChainSlow > 0)
            {
                drawColor = drawColor.MultiplyRGB(Color.LightGray);
                Dust.NewDust(npc.BottomLeft + new Vector2(0, -4f), npc.width, 1, DustID.t_Slime, newColor: Color.Gray, Scale: 0.5f);
            }
        }
        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (bleedingStacks != null && bleedingStacks.Any())
            {
                DrawRotatlingBloodParticles(true, npc);
                
            }
        }
        /// <summary>
        /// Drawns top half behind npcs and bottom half in front of npcs.
        /// </summary>
        public void DrawRotatlingBloodParticles(bool inFront, NPC npc)
        {
            Texture2D texture = ModContent.Request<Texture2D>("TerRoguelike/Projectiles/AdaptiveGunBullet").Value;
            Color color = Color.Red * 0.8f;
            Vector2 position = GetDrawCenter(npc) + (Vector2.UnitY * npc.gfxOffY);

            for (int i = 0; i < bleedingStacks.Count; i++)
            {
                Vector2 specificPosition = position;
                float rotation = MathHelper.Lerp(0, MathHelper.TwoPi, Main.GlobalTimeWrappedHourly * 1.5f);
                float rotationCompletionOffset = MathHelper.TwoPi / bleedingStacks.Count * i;
                rotation += rotationCompletionOffset;
                specificPosition += new Vector2(0, 16).RotatedBy(rotation);
                specificPosition += (specificPosition - position) * new Vector2((npc.frame.Width * npc.scale) / 32f, -0.5f) ;
                if (specificPosition.Y >= position.Y && inFront)
                    Main.EntitySpriteDraw(texture, specificPosition - Main.screenPosition, null, color, 0f, texture.Size() * 0.5f, 1f, SpriteEffects.None);
                else if (!inFront)
                    Main.EntitySpriteDraw(texture, specificPosition - Main.screenPosition, null, color, 0f, texture.Size() * 0.5f, 1f, SpriteEffects.None);
            }
        }
        /// <summary>
        /// A sorrowful attempt at getting the sprite center of every npc in the game for proper visuals
        /// </summary>
        public Vector2 GetDrawCenter(NPC npc)
        {
            if (drawCenter != new Vector2(-1000))
                return drawCenter + npc.Center;

            
            for (int i = 0; i < NPCManager.AllNPCs.Count; i++)
            {
                if (npc.type == NPCManager.AllNPCs[i].modNPCID)
                {
                    drawCenter = NPCManager.AllNPCs[i].DrawCenterOffset;
                    return drawCenter + npc.Center;
                }
            }

            Vector2 position = npc.Center;
            switch (npc.aiStyle)
            {
                case NPCAIStyleID.Fighter:
                    position = npc.VisualPosition + new Vector2(npc.width / 2, npc.height) + new Vector2(0f, (-npc.frame.Height / 2f + 4) * npc.scale);
                    break;
                case NPCAIStyleID.Slime:
                    position = npc.VisualPosition + new Vector2(npc.width / 2, npc.height) + new Vector2(0f, (-npc.frame.Height / 2f + 4) * npc.scale);
                    break;
                case NPCAIStyleID.KingSlime:
                    position = npc.VisualPosition + new Vector2(npc.width / 2, npc.height) + new Vector2(0f, (-npc.frame.Height / 2f + 4) * npc.scale);
                    break;
                case NPCAIStyleID.TeslaTurret:
                    position += new Vector2(-2, 2);
                    break;
                case NPCAIStyleID.Flying:
                    position += new Vector2(0, 5);
                    break;
                case NPCAIStyleID.HoveringFighter:
                    position += new Vector2(0, 5);
                    break;
                case NPCAIStyleID.EnchantedSword:
                    if (npc.type == NPCID.CursedHammer)
                        position += new Vector2(0, 2);
                    if (npc.type == NPCID.CrimsonAxe)
                        position += new Vector2(0, -9);
                    break;
            }
            return position;
        }
        /// <summary>
        /// Whether this npc can be chased with the given conditions.
        /// chaseFriendly == null: neutral. both friendly and hostile can pass
        /// chaseFriendly == true: only passes if friendly
        /// chaseFriendly == false: only passes if not friendly
        /// </summary>
        /// <param name="ignoreDontTakeDamage"></param>
        /// <param name="chaseFriendly"></param>
        /// <returns>true if passing, else false</returns>
        public bool CanBeChased(bool ignoreDontTakeDamage = false, bool? chaseFriendly = false)
        {
            NPC npc = Main.npc[whoAmI];
            bool allianceCheck = chaseFriendly == null ? true : (chaseFriendly == true ? npc.friendly : !npc.friendly);
            if (npc.active && npc.chaseable && npc.lifeMax > 5 && (!npc.dontTakeDamage || ignoreDontTakeDamage) && allianceCheck)
            {
                return !npc.immortal;
            }
            return false;
        }
        /// <summary>
        /// Basic finding target behaviour. if friendly, targets hostile npcs. if not friendly, targets players.
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="resetDir"></param>
        /// <param name="resetSpriteDir"></param>
        /// <returns></returns>
        public Entity GetTarget(NPC npc, bool resetDir = false, bool resetSpriteDir = false)
        {
            if (targetPlayer != -1)
            {
                if (!Main.player[targetPlayer].active || Main.player[targetPlayer].dead || npc.friendly)
                {
                    targetPlayer = -1;
                }
            }
            if (targetNPC != -1)
            {
                if (!Main.npc[targetNPC].GetGlobalNPC<TerRoguelikeGlobalNPC>().CanBeChased(false, false) || !npc.friendly)
                {
                    
                    targetNPC = -1;
                }
            }

            if (npc.friendly)
            {
                if (targetNPC == -1 || targetPlayer != -1)
                {
                    targetNPC = ClosestNPC(npc.Center, 3200f, false);
                    targetPlayer = -1;
                    if (resetDir)
                        npc.direction = 1;
                    if (resetSpriteDir)
                        npc.spriteDirection = 1;
                }
            }
            else
            {
                if (targetPlayer == -1 || targetNPC != -1)
                {
                    targetPlayer = npc.FindClosestPlayer();
                    if (Main.player[targetPlayer].dead)
                        targetPlayer = -1;

                    targetNPC = -1;
                    if (resetDir)
                        npc.direction = 1;
                    if (resetSpriteDir)
                        npc.spriteDirection = 1;
                }
            }
            return targetPlayer != -1 ? Main.player[targetPlayer] : (targetNPC != -1 ? Main.npc[targetNPC] : null);
        }

        public void SetUpNPCProj(NPC npc, int proj)
        {
            Main.projectile[proj].GetGlobalProjectile<TerRoguelikeGlobalProjectile>().npcOwner = npc.whoAmI;
            Main.projectile[proj].GetGlobalProjectile<TerRoguelikeGlobalProjectile>().npcOwnerType = npc.type;
            if (hostileTurnedAlly || npc.friendly)
            {
                Main.projectile[proj].friendly = true;
                Main.projectile[proj].hostile = false;
            }
            else
            {
                Main.projectile[proj].friendly = false;
                Main.projectile[proj].hostile = true;
                Main.projectile[proj].damage /= 2;
            }
        }
    }

    public class WormSegment
    {
        public WormSegment(Vector2 position, float rotation = 0f, float height = 1)
        {
            Position = position;
            OldPosition = position;
            Rotation = rotation;
            OldRotation = rotation;
            Height = height;
        }
        public Vector2 Position = Vector2.Zero;
        public Vector2 OldPosition = Vector2.Zero;
        public float Rotation = 0;
        public float OldRotation = 0;
        public float Height = 1f;
    }

    public class IgnitedStack
    {
        public IgnitedStack(int damageToDeal, int owner)
        {
            Owner = owner;
            damageToDeal *= Main.player[owner].GetModPlayer<TerRoguelikePlayer>().forgottenBioWeapon + 1;
            DamageToDeal = damageToDeal;
        }
        public int DamageToDeal = 0;
        public int Owner = -1;
    }
    public class BleedingStack
    {
        public BleedingStack(int damageToDeal, int owner)
        {
            Owner = owner;
            damageToDeal *= Main.player[owner].GetModPlayer<TerRoguelikePlayer>().forgottenBioWeapon + 1;
            DamageToDeal = damageToDeal;
        }
        public int DamageToDeal = 0;
        public int Owner = -1;
    }
}
