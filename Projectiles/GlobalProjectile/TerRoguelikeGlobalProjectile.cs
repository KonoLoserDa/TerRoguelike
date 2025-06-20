﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TerRoguelike.NPCs;
using TerRoguelike.TerPlayer;
using TerRoguelike.World;
using static TerRoguelike.Managers.NPCManager;
using static TerRoguelike.NPCs.TerRoguelikeGlobalNPC;
using static TerRoguelike.Utilities.TerRoguelikeUtils;

namespace TerRoguelike.Projectiles
{
    public class TerRoguelikeGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public ProcChainBools procChainBools = new ProcChainBools();
        public int homingTarget = -1;
        public int extraBounces = 0;
        public int bounceCount = 0;
        public int homingCheckCooldown = 0;
        public int swingDirection = 0;
        public bool ultimateCollideOverride = false;
        public bool killOnRoomClear = false;
        public int npcOwner = -1;
        public int npcOwnerType = -1;
        public float notedBoostedDamage = 1f;
        public int targetPlayer = -1;
        public int targetNPC = -1;
        public bool sluggedEffect = false;
        public bool hostileTurnedAlly = false;
        public int npcOwnerPuppetOwner = -1;
        public int multiplayerIdentifier = -1;
        public int multiplayerClientIdentifier = -1;
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (hostileTurnedAlly)
                GhostSpritebatch();
            return true;
        }
        public override void PostDraw(Projectile projectile, Color lightColor)
        {
            if (hostileTurnedAlly)
                StartVanillaSpritebatch();
        }
        public override bool PreAI(Projectile projectile)
        {
            extraBounces = 0; // set bounces in projectile ai.
            return true;
        }
        public override bool CanHitPlayer(Projectile projectile, Player target)
        {
            ultimateCollideOverride = false;
            return true;
        }
        public override bool? Colliding(Projectile projectile, Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (ultimateCollideOverride)
            {
                ultimateCollideOverride = false;
                return true;
            }
            return null;
        }
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            if (sluggedEffect)
            {
                target.ModPlayer().sluggedAttempt = true;
            }
        }
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[projectile.owner];
            TerRoguelikePlayer modPlayer = player.ModPlayer();

            if (TerRoguelikeWorld.IsTerRoguelikeWorld)
            {
                modifiers.DamageVariationScale *= 0;
            }
            if (modPlayer.bouncyBall > 0)
            {
                modifiers.SourceDamage *= 1 + (0.15f * Math.Min(bounceCount, modPlayer.bouncyBall));
            }
            if (npcOwner >= 0)
            {
                NPC nOwner = Main.npc[npcOwner];
                var modNOwner = nOwner.ModNPC();
                if (modNOwner != null && !nOwner.friendly && !modNOwner.hostileTurnedAlly)
                {
                    modifiers.SourceDamage *= 2;
                }
            }
            
            //Crit inheritance and custom crit chance supported by proc luck
            if (procChainBools.critPreviously)
                modifiers.SetCrit();
            else if (!procChainBools.originalHit)
            {
                modifiers.DisableCrit();
            }
            else
            {
                float critChance = projectile.CritChance * 0.01f;
                if (ChanceRollWithLuck(critChance, modPlayer.procLuck))
                {
                    modifiers.SetCrit();
                }
                else
                {
                    modifiers.DisableCrit();
                }
            }
        }
        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (projectile.hostile || !projectile.friendly)
                return;

            TerRoguelikePlayer modPlayer = Main.player[projectile.owner].ModPlayer();
            if (modPlayer.volatileRocket > 0 && procChainBools.originalHit && projectile.type != ModContent.ProjectileType<Explosion>())
            {
                SpawnExplosion(projectile, modPlayer, target, crit: hit.Crit);
            }
        }
        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (source is EntitySource_Parent parentSource)
            {
                if (parentSource.Entity is NPC)
                {
                    NPC npc = Main.npc[parentSource.Entity.whoAmI];
                    npcOwner = npc.whoAmI;
                    npcOwnerType = npc.type;

                    TerRoguelikeGlobalNPC modNPC = npc.ModNPC();
                    if (modNPC.hostileTurnedAlly || npc.friendly)
                    {
                        projectile.friendly = true;
                        projectile.hostile = false;
                        hostileTurnedAlly = modNPC.hostileTurnedAlly;
                        if (hostileTurnedAlly)
                        {
                            npcOwnerPuppetOwner = modNPC.puppetOwner;
                        }
                    }
                    else
                    {
                        projectile.friendly = false;
                        projectile.hostile = true;
                        projectile.damage /= 2;
                    }
                    if (modNPC.eliteVars.slugged)
                        sluggedEffect = true;
                }
                else if (parentSource.Entity is Projectile)
                {
                    Projectile parentProj = Main.projectile[parentSource.Entity.whoAmI];
                    TerRoguelikeGlobalProjectile parentModProj = parentProj.ModProj();
                    npcOwner = parentModProj.npcOwner;
                    npcOwnerType = parentModProj.npcOwnerType;
                    hostileTurnedAlly = parentModProj.hostileTurnedAlly;
                    npcOwnerPuppetOwner = parentModProj.npcOwnerPuppetOwner;
                    if (npcOwnerType != -1)
                    {
                        if (AllNPCs.Exists(x => x.modNPCID == npcOwnerType))
                        {
                            projectile.hostile = parentProj.hostile;
                            projectile.friendly = parentProj.friendly;
                        }
                    }
                    sluggedEffect = parentModProj.sluggedEffect;
                }
            }
            multiplayerIdentifier = projectile.whoAmI;
            multiplayerClientIdentifier = Main.myPlayer;
            projectile.netUpdate = true;
        }
        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (projectile.hostile || !projectile.friendly)
                return;

            TerRoguelikePlayer modPlayer = Main.player[projectile.owner].ModPlayer();
            if (modPlayer.volatileRocket > 0 && projectile.penetrate > 1 && procChainBools.originalHit && projectile.type != ModContent.ProjectileType<Explosion>())
            {
                SpawnExplosion(projectile, modPlayer, originalHit: true); //Explosions not spawned from hits are counted as original hits, to calculate crit themselves.
            }
        }
        public void SpawnExplosion(Projectile projectile, TerRoguelikePlayer modPlayer, NPC target = null, bool originalHit = false, bool crit = false)
        {
            Vector2 position = projectile.Center;
            if (target != null)
            {
                position = target.ModNPC().SpecialProjectileCollisionRules ? projectile.Center + (Vector2.UnitX * (projectile.width > projectile.height ? projectile.height * 0.5f : projectile.height * 0.5f)).RotatedBy(projectile.rotation) : target.getRect().ClosestPointInRect(projectile.Center);
            }
            int spawnedProjectile = Projectile.NewProjectile(projectile.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<Explosion>(), (int)(projectile.damage * 0.6f), 0f, projectile.owner);
            Main.projectile[spawnedProjectile].scale = 1f * modPlayer.volatileRocket;
            TerRoguelikeGlobalProjectile modProj = Main.projectile[spawnedProjectile].ModProj();
            modProj.procChainBools = new ProcChainBools(projectile.ModProj().procChainBools);
            if (!originalHit)
                modProj.procChainBools.originalHit = false;
            if (crit)
                modProj.procChainBools.critPreviously = true;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = SoundID.Item41.Volume * 0.5f }, projectile.Center);
        }
        public void HomingAI(Projectile projectile, float homingStrength, bool idleSpin = false)
        {
            if (projectile.velocity == Vector2.Zero)
                return;
            
            if (homingTarget != -1)
            {
                if (!Main.npc[homingTarget].CanBeChasedBy() || Main.npc[homingTarget].life <= 0) //reset homing target if it's gone
                    homingTarget = -1;
            }

            if (homingTarget == -1 && idleSpin)
            {
                projectile.velocity = projectile.velocity.RotatedBy(homingStrength * MathHelper.TwoPi);
            }

            if (homingCheckCooldown > 0) //cooldown on homing checks as an attempt to stave off lag
            {
                homingCheckCooldown--;
                if (homingTarget == -1)
                    return;
            }

            if (homingTarget == -1)
            {
                //create a list of each npc's homing rating relative to the projectile's position and velocity direction to try and choose the best target.
                float prefferedDistance = 1000f;
                List<float> npcHomingRating = new List<float>(new float[Main.maxNPCs]);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.CanBeChasedBy(null, false) || npc.life <= 0)
                    {
                        npcHomingRating[i] = -10;
                        continue;
                    }
                    Vector2 distanceVect = npc.Center - projectile.Center;
                    float distance = distanceVect.Length();
                    npcHomingRating[i] += Vector2.Dot(Vector2.Normalize(projectile.velocity), Vector2.Normalize(distanceVect));
                    if (distance < prefferedDistance)
                    {
                        npcHomingRating[i] += 1f;
                    }
                    else
                    {
                        npcHomingRating[i] += 1f - (distance / 7000f);
                        if (npcHomingRating[i] <= -10)
                            npcHomingRating[i] = -9.9999f;
                    }
                }
                homingCheckCooldown = 10 * projectile.MaxUpdates;

                if (npcHomingRating.All(x => x == -10f))
                    return;

                homingTarget = npcHomingRating.FindIndex(x => x == npcHomingRating.Max());

                if (!Main.npc[homingTarget].CanBeChasedBy(null, false) || Main.npc[homingTarget].life <= 0)
                {
                    homingTarget = -1;
                    return;
                }
            }

            float maxChange = homingStrength * MathHelper.TwoPi;

            projectile.velocity = (Vector2.UnitX * projectile.velocity.Length()).RotatedBy(projectile.velocity.ToRotation().AngleTowards((Main.npc[homingTarget].Center - projectile.Center).ToRotation(), maxChange));
        }
        public Entity GetTarget(Projectile proj)
        {
            if ((TerRoguelike.mpClient || Main.dedServ) && proj.owner != Main.myPlayer)
            {
                if (targetPlayer >= 0)
                    return Main.player[targetPlayer];
                if (targetNPC >= 0)
                    return Main.npc[targetNPC];
                return null;
            }
            if (targetPlayer != -1)
            {
                if (!Main.player[targetPlayer].active || Main.player[targetPlayer].dead || proj.friendly)
                {
                    targetPlayer = -1;
                    proj.netUpdate = true;
                }
            }
            if (targetNPC != -1)
            {
                if (!Main.npc[targetNPC].ModNPC().CanBeChased(false, false) || !proj.friendly)
                {
                    targetNPC = -1;
                    proj.netUpdate = true;
                }
            }

            if (proj.friendly)
            {
                if (targetNPC == -1 || targetPlayer != -1)
                {
                    targetNPC = ClosestNPC(proj.Center, 3200f, false);
                    targetPlayer = -1;
                    if (targetNPC != -1)
                        proj.netUpdate = true;
                }
            }
            else
            {
                if (targetPlayer == -1 || targetNPC != -1)
                {
                    targetPlayer = ClosestPlayer(proj.Center, 3200f);
                    if (targetPlayer >= 0 && Main.player[targetPlayer].dead)
                        targetPlayer = -1;

                    targetNPC = -1;
                    if (targetPlayer != -1)
                        proj.netUpdate = true;
                }
            }
            return targetPlayer != -1 ? Main.player[targetPlayer] : (targetNPC != -1 ? Main.npc[targetNPC] : null);
        }

        public void EliteSpritebatch(bool end = true, BlendState blendState = null)
        {
            if (blendState == null)
                blendState = BlendState.AlphaBlend;

            if (hostileTurnedAlly)
            {
                GhostSpritebatch(end, blendState);
                return;
            }
            else
            {
                if (end)
                    Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, blendState, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
        {
            bool sendZero = projectile.ai[0] == 0 || projectile.ai[1] == 0 || projectile.ai[2] == 0;
            writer.Write(sendZero);
            if (sendZero)
            {
                writer.Write(projectile.ai[0]);
                writer.Write(projectile.ai[1]);
                writer.Write(projectile.ai[2]);
            }

            writer.Write(sluggedEffect);
            writer.Write(projectile.rotation);
            writer.Write(multiplayerIdentifier);
            writer.Write(multiplayerClientIdentifier);
            writer.Write(projectile.friendly);
            writer.Write(projectile.hostile);
            writer.Write(npcOwner);
            writer.Write(npcOwnerType);
            writer.Write(targetPlayer);
            writer.Write(targetNPC);
            writer.Write(projectile.scale);
            writer.Write(hostileTurnedAlly);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
        {
            bool receiveZero = reader.ReadBoolean();
            if (receiveZero)
            {
                projectile.ai[0] = reader.ReadSingle();
                projectile.ai[1] = reader.ReadSingle();
                projectile.ai[2] = reader.ReadSingle();
            }

            sluggedEffect = reader.ReadBoolean();
            projectile.rotation = reader.ReadSingle();
            multiplayerIdentifier = reader.ReadInt32();
            multiplayerClientIdentifier = reader.ReadInt32();
            projectile.friendly = reader.ReadBoolean();
            projectile.hostile = reader.ReadBoolean();
            npcOwner = reader.ReadInt32();
            npcOwnerType = reader.ReadInt32();
            targetPlayer = reader.ReadInt32();
            targetNPC = reader.ReadInt32();
            projectile.scale = reader.ReadSingle();
            hostileTurnedAlly = reader.ReadBoolean();
        }
    }
    public class ProcChainBools
    {
        //proc chain bools, usually used to make spawned projectiles inherit what their parent projectiles have done in the past. prevents infinite proc chains, and allows crit inheritance.
        public ProcChainBools() { }
        public ProcChainBools(ProcChainBools procChainBools)
        {
            originalHit = procChainBools.originalHit;
            critPreviously = procChainBools.critPreviously;
            clinglyGrenadePreviously = procChainBools.clinglyGrenadePreviously;
            lockOnMissilePreviously = procChainBools.lockOnMissilePreviously;
        }
        public bool originalHit = true;
        public bool critPreviously = false;
        public bool clinglyGrenadePreviously = false;
        public bool lockOnMissilePreviously = false;
        
    }
}
