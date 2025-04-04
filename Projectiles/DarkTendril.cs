﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static TerRoguelike.Utilities.TerRoguelikeUtils;
using static TerRoguelike.Managers.TextureManager;
using System.Collections.Generic;
using TerRoguelike.Managers;
using TerRoguelike.Particles;
using ReLogic.Utilities;
using static TerRoguelike.Systems.RoomSystem;
using System.Diagnostics;
using System.IO;

namespace TerRoguelike.Projectiles
{
    public class DarkTendril : ModProjectile, ILocalizedModType
    {
        public float turnMultiplier = 4f;
        public Entity target = null;
        public List<Vector2> specialOldPos = new List<Vector2>();
        public int maxTendrilLength = 360;
        public int maxTimeLeft;
        public float startRot;
        public SlotId rumbleSlot;
        public Texture2D circleTex;
        public bool initialized = false;
        public override string Texture => "TerRoguelike/Projectiles/InvisibleProj";
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
        }
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.timeLeft = maxTimeLeft = 480;
            Projectile.penetrate = -1;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            circleTex = TexDict["Circle"];
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }
        public override void OnSpawn(IEntitySource source)
        {
            rumbleSlot = SoundEngine.PlaySound(SoundID.DD2_EtherianPortalIdleLoop with { Volume = 0.012f, PitchVariance = 0.4f, Pitch = 0.6f, MaxInstances = 10 }, Projectile.Center);
            turnMultiplier = 4f;
            Projectile.ai[1] = Main.rand.NextFloat(MathHelper.TwoPi);
            startRot = Projectile.velocity.ToRotation();
            initialized = true;
        }
        public override void AI()
        {
            if (!initialized)
            {
                rumbleSlot = SoundEngine.PlaySound(SoundID.DD2_EtherianPortalIdleLoop with { Volume = 0.012f, PitchVariance = 0.4f, Pitch = 0.6f, MaxInstances = 10 }, Projectile.Center);
                initialized = true;
            }
            Projectile.netSpam = 0;
            var modProj = Projectile.ModProj();
            if (modProj.npcOwner >= 0)
            {
                NPC npc = Main.npc[modProj.npcOwner];
                var modNPC = npc.ModNPC();
                if (modNPC.isRoomNPC)
                {
                    if (RoomList[modNPC.sourceRoomListID].bossDead)
                        Projectile.ai[0] = 1;
                }
            }

            if (specialOldPos.Count > 0 && (int)Projectile.localAI[0] % 11 == 0 && (int)Projectile.localAI[0] != Projectile.localAI[1])
            {
                Projectile.localAI[1] = (int)Projectile.localAI[0];
                Color outlineColor = Color.Purple;
                Color fillColor = Color.Lerp(outlineColor, Color.Black, 0.6f);
                ParticleManager.AddParticle(new BallOutlined(
                    specialOldPos[0] - startRot.ToRotationVector2() * 4, startRot.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-0.7f, 0.7f) * 2),
                    30, outlineColor, fillColor, new Vector2(0.25f), 4, 0, 0.96f, 30));
            }

            Projectile.localAI[0] += Main.rand.NextFloat(0.5f, 1f);

            if (Projectile.ai[0] == 0 && specialOldPos.Count < maxTendrilLength)
                specialOldPos.Add(Projectile.Center);
            else if (Projectile.ai[0] == 1)
            {
                int removeCount = 3;
                if (specialOldPos.Count < removeCount)
                    removeCount = specialOldPos.Count;
                specialOldPos.RemoveRange(specialOldPos.Count - removeCount, removeCount);
            }
            if (specialOldPos.Count >= maxTendrilLength)
            {
                Projectile.ai[0] = 1;
            }

            if (SoundEngine.TryGetActiveSound(rumbleSlot, out var sound) && sound.IsPlaying)
            {
                if (Projectile.ai[0] == 1)
                    sound.Volume = specialOldPos.Count / (float)maxTendrilLength * 100;
                else
                {
                    if (sound.Volume < 100)
                        sound.Volume += 8;
                    if (sound.Volume > 100)
                        sound.Volume = 100;
                }
                sound.Position = Projectile.Center;
            }

            target = modProj.GetTarget(Projectile);

            if (Projectile.ai[0] == 0)
            {
                if (specialOldPos.Count > maxTendrilLength - 60)
                {
                    Projectile.velocity *= 0.97f;
                }
                if (target != null && (maxTimeLeft - Projectile.timeLeft) >= 60)
                {
                    float direction = (target.Center - Projectile.Center).ToRotation() + (float)(Math.Cos(Projectile.localAI[0] / 10 + Projectile.ai[1]) * 0.3f);

                    float newRot = Projectile.velocity.ToRotation().AngleTowards(direction, 0.006f * turnMultiplier);
                    Projectile.velocity = (Vector2.UnitX * Projectile.velocity.Length()).RotatedBy(newRot);
                }
                else
                {
                    Projectile.velocity = Projectile.velocity.RotatedBy((float)(Math.Cos(Projectile.localAI[0] / 4 + Projectile.ai[1]) * 0.08f));
                }
            }
            else if (Projectile.ai[0] == 1)
            {
                Projectile.velocity *= 0;
                if (specialOldPos.Count <= 0)
                {
                    Projectile.Kill();
                    return;
                }
                Projectile.Center = specialOldPos[specialOldPos.Count - 1];
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(rumbleSlot, out var sound) && sound.IsPlaying)
            {
                sound.Stop();
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (maxTimeLeft - Projectile.timeLeft < 45)
                return false;

            int countOffset = (maxTendrilLength - specialOldPos.Count);
            for (int i = 0; i < specialOldPos.Count; i += 4)
            {
                float completion = MathHelper.Clamp((float)(i + countOffset) / (maxTendrilLength - 1), 0, 1f);
                float scale = MathHelper.Lerp(0.7f, 0.2f, completion);
                Vector2 checkPos = specialOldPos[i];
                if ((checkPos - targetHitbox.ClosestPointInRect(checkPos)).Length() < 16 * scale)
                    return true;
            }
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Color outlineColor = Color.Purple;
            Color fillColor = Color.Lerp(outlineColor, Color.Black, 0.6f);
            Vector2 origin = circleTex.Size() * 0.5f;
            int countOffset = (maxTendrilLength - specialOldPos.Count);

            for (int i = 0; i < specialOldPos.Count; i++)
            {
                float completion = MathHelper.Clamp((float)(i + countOffset) / (maxTendrilLength - 1), 0, 1f);
                Vector2 basePos = specialOldPos[i] - Main.screenPosition;
                float scale = MathHelper.Lerp(0.0875f, 0.026f, completion);
                Main.EntitySpriteDraw(circleTex, basePos, null, outlineColor, 0, origin, scale, SpriteEffects.None);
            }
            for (int i = 0; i < specialOldPos.Count; i++)
            {
                float completion = MathHelper.Clamp((float)(i + countOffset) / (maxTendrilLength - 1), 0, 1f);
                Vector2 basePos = specialOldPos[i] - Main.screenPosition;
                float scale = MathHelper.Lerp(0.07f, 0.02f, completion);

                Main.EntitySpriteDraw(circleTex, basePos, null, fillColor, 0, origin, scale, SpriteEffects.None);
            }
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(startRot);
            writer.Write(Projectile.localAI[0]);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            startRot = reader.ReadSingle();
            Projectile.localAI[0] = reader.ReadSingle();
        }
    }
}
