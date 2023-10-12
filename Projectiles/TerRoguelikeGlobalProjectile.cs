﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent.ItemDropRules;
using TerRoguelike.World;
using Microsoft.Xna.Framework;
using TerRoguelike.Items.Rare;
using TerRoguelike.TerPlayer;
using Terraria.Audio;
using Terraria.ID;
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
        public override bool PreAI(Projectile projectile)
        {
            extraBounces = 0; // set bounces in projectile ai.
            return true;
        }
        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[projectile.owner];
            TerRoguelikePlayer modPlayer = player.GetModPlayer<TerRoguelikePlayer>();

            if (TerRoguelikeWorld.IsTerRoguelikeWorld)
            {
                modifiers.DamageVariationScale *= 0;
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
            TerRoguelikePlayer modPlayer = Main.player[projectile.owner].GetModPlayer<TerRoguelikePlayer>();
            if (modPlayer.volatileRocket > 0 && procChainBools.originalHit && projectile.type != ModContent.ProjectileType<Explosion>())
            {
                SpawnExplosion(projectile, modPlayer, target, crit: hit.Crit);
            }
        }
        public override void OnKill(Projectile projectile, int timeLeft)
        {
            TerRoguelikePlayer modPlayer = Main.player[projectile.owner].GetModPlayer<TerRoguelikePlayer>();
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
                position = target.getRect().ClosestPointInRect(projectile.Center);
            }
            int spawnedProjectile = Projectile.NewProjectile(projectile.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<Explosion>(), (int)(projectile.damage * 0.6f), 0f, projectile.owner);
            Main.projectile[spawnedProjectile].scale = 1f * modPlayer.volatileRocket;
            TerRoguelikeGlobalProjectile modProj = Main.projectile[spawnedProjectile].GetGlobalProjectile<TerRoguelikeGlobalProjectile>();
            modProj.procChainBools = new ProcChainBools(projectile.GetGlobalProjectile<TerRoguelikeGlobalProjectile>().procChainBools);
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

            int projIndex = projectile.whoAmI;
            
            if (homingTarget != -1)
            {
                if (!Main.npc[homingTarget].active || Main.npc[homingTarget].life <= 0) //reset homing target if it's gone
                    homingTarget = -1;
            }

            if (homingTarget == -1 && idleSpin)
            {
                projectile.velocity = projectile.velocity.RotatedBy(homingStrength * MathHelper.TwoPi);
            }

            if (homingCheckCooldown > 0) //cooldown on homing checks as an attempt to stave off lag
            {
                homingCheckCooldown--;
                return;
            }

            if (homingTarget == -1)
            {
                //create a list of each npc's homing rating relative to the projectile's position and velocity direction to try and choose the best target.
                float prefferedDistance = 480f;
                List<float> npcHomingRating = new List<float>(new float[Main.maxNPCs]);
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (!npc.CanBeChasedBy(null, false))
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
                        npcHomingRating[i] += 1f - (distance / 1000f);
                    }
                }
                homingCheckCooldown = 10;

                if (npcHomingRating.All(x => x == -10f))
                    return;

                homingTarget = npcHomingRating.FindIndex(x => x == npcHomingRating.Max());

                if (!Main.npc[homingTarget].CanBeChasedBy(null, false))
                {
                    homingTarget = -1;
                    return;
                }
            }

            Vector2 realDistanceVect = Main.npc[homingTarget].Center - projectile.Center;
            float targetAngle = Math.Abs(projectile.velocity.ToRotation() - realDistanceVect.ToRotation());
            float setAngle = homingStrength * MathHelper.TwoPi;

            if (setAngle > targetAngle)
                setAngle = targetAngle;


            if (Vector2.Dot(Vector2.Normalize(projectile.velocity).RotatedBy(MathHelper.PiOver2), Vector2.Normalize(realDistanceVect)) < 0)
                setAngle *= -1;

            setAngle += projectile.velocity.ToRotation();

            Vector2 setVelocity = setAngle.ToRotationVector2() * projectile.velocity.Length(); // rotate projectile somwhat in the direction of the target


            Main.projectile[projIndex].velocity = setVelocity;
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
