﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TerRoguelike.Managers;
using TerRoguelike.Systems;
using TerRoguelike.TerPlayer;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics.Renderers;
using System.Reflection;
using static TerRoguelike.Utilities.TerRoguelikeUtils;

namespace TerRoguelike.Projectiles
{
    public class AdaptiveBladeHoldout : ModProjectile, ILocalizedModType
    {
        //This manages whatever happens when you hold down with adaptive blade
        public override string Texture => "TerRoguelike/Projectiles/InvisibleProj";

        public ref float Charge => ref Projectile.ai[0];

        public bool autoRelease = false;
        public Player Owner => Main.player[Projectile.owner];
        public TerRoguelikePlayer modPlayer;

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            if (modPlayer == null)
                modPlayer = Owner.GetModPlayer<TerRoguelikePlayer>();

            if (Charge >= 60f && !autoRelease) //cap chargetime if no autorelease
            {
                Charge = 60f;
                
            }

            float pointingRotation = (AimWorld() - Owner.MountedCenter).ToRotation();
            Projectile.Center = Owner.MountedCenter + pointingRotation.ToRotationVector2() * 16f;

            if (Owner.channel) //Keep the player's hands full relative to attack speed
            {
                Projectile.timeLeft = 2;
                Owner.itemTime = (int)(20 / Owner.GetAttackSpeed(DamageClass.Generic)) + 2;
                if (Owner.itemTime < 2)
                    Owner.itemTime = 2;
                Owner.itemAnimation = (int)(20 / Owner.GetAttackSpeed(DamageClass.Generic)) + 2;
                if (Owner.itemAnimation < 2)
                    Owner.itemAnimation = 2;
                Owner.heldProj = Projectile.whoAmI;
            }
            else // player released m1. swing it.
            {
                ReleaseSword();
                return;
            }
            if (autoRelease && Charge >= 60f) // autorelease swing sword
            {
                ReleaseSword();
            }
                
            
            if (Charge < 60 || autoRelease) // charge. autorelease allows overflowing of charge amount, leading to more than 1 swing a frame
            {
                float chargeAmt = 1f * Owner.GetAttackSpeed(DamageClass.Generic);
                if (chargeAmt >= 3f) // autorelease at 3x attack speed
                    autoRelease = true;
                else
                    autoRelease = false;

                Charge += chargeAmt;
                if (Charge >= 60f)
                {
                    SoundEngine.PlaySound(new SoundStyle("TerRoguelike/Sounds/Ding") with { Volume = 0.085f }, Owner.Center);
                    modPlayer.bladeFlashTime = 15;
                }
            }
        }

        public void ReleaseSword()
        {
            if ((Charge <= 60f || (Owner.channel && autoRelease)) && modPlayer.swingAnimCompletion == 0)
                modPlayer.swingAnimCompletion += 0.00001f; // start the swing anim

            int shotsToFire = Owner.GetModPlayer<TerRoguelikePlayer>().shotsToFire; //multishot support
            int damage = Charge >= 60f ? (int)(Projectile.damage * 4f) : (int)(Projectile.damage * (1 + (Charge / 60f * 2f)));
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = SoundID.Item41.Volume * 1f });
            for (int i = 0; i < shotsToFire; i++)
            {
                float mainAngle;
                float spread = 20f;
                if (shotsToFire == 1)
                {
                    mainAngle = (Projectile.Center - Owner.MountedCenter).ToRotation();
                }
                else if (shotsToFire % 2 == 0)
                {
                    mainAngle = (Projectile.Center - Owner.MountedCenter).ToRotation() - ((float)((shotsToFire - 1) * 2) * MathHelper.Pi/(spread * 4f)) + ((float)i * MathHelper.Pi/spread);
                }
                else
                {
                    mainAngle = (Projectile.Center - Owner.MountedCenter).ToRotation() - ((float)((shotsToFire - 1) / 2) * MathHelper.Pi/spread) + ((float)i * MathHelper.Pi / spread);
                }

                
                Vector2 direction = (mainAngle).ToRotationVector2();
                int spawnedProjectile = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Owner.MountedCenter + (direction * 16f), Vector2.Zero, ModContent.ProjectileType<AdaptiveBladeSlash>(), damage, 1f, Owner.whoAmI);
                Main.projectile[spawnedProjectile].rotation = direction.ToRotation();
                Main.projectile[spawnedProjectile].scale = modPlayer.scaleMultiplier;
                Main.projectile[spawnedProjectile].GetGlobalProjectile<TerRoguelikeGlobalProjectile>().swingDirection = Owner.direction;
            }
            Charge -= 60f;
            if (Charge > 60f) // support for swinging more than once a frame if one has that much attack speed
            {
                ReleaseSword();
            }
        }
    }
}
