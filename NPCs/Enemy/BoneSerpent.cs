﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TerRoguelike;
using TerRoguelike.TerPlayer;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.GameContent;
using TerRoguelike.NPCs;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using TerRoguelike.Projectiles;
using Terraria.Graphics.Shaders;
using static TerRoguelike.Schematics.SchematicManager;
using static TerRoguelike.Managers.TextureManager;
using TerRoguelike.Utilities;
using Terraria.Audio;
using Terraria.GameContent.Animations;

namespace TerRoguelike.NPCs.Enemy
{
    public class BoneSerpent : BaseRoguelikeNPC
    {
        public override int modNPCID => ModContent.NPCType<BoneSerpent>();
        public override List<int> associatedFloors => new List<int>() { FloorDict["Hell"] };
        public override int CombatStyle => 0;
        public Texture2D headTex;
        public Texture2D bodyTex;
        public Texture2D tailTex;
        public bool CollisionPass = false;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.MustAlwaysDraw[Type] = true;
            NPCID.Sets.NoMultiplayerSmoothingByType[Type] = true;
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            NPC.width = 38;
            NPC.height = 38;
            NPC.aiStyle = -1;
            NPC.damage = 28;
            NPC.lifeMax = 1000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.lavaImmune = true;
            modNPC.drawCenter = new Vector2(0, -6);
            modNPC.IgnoreRoomWallCollision = true;
            modNPC.OverrideIgniteVisual = true;
            modNPC.SpecialProjectileCollisionRules = true;
            headTex = TexDict["BoneSerpentHead"];
            bodyTex = TexDict["BoneSerpentBody"];
            tailTex = TexDict["BoneSerpentTail"];
        }
        public override void OnSpawn(IEntitySource source)
        {
            NPC.position.Y -= -NPC.height + 540 * 0.5f;
            NPC.velocity = Vector2.UnitY * -1f;
            NPC.rotation = -MathHelper.PiOver2;
            AddSegments();
        }
        public void AddSegments()
        {
            int segCount = 25;
            int segmentHeight = 20;
            for (int i = 0; i < segCount; i++)
            {
                modNPC.Segments.Add(new WormSegment(NPC.Center + (Vector2.UnitY * segmentHeight * i), MathHelper.PiOver2 * 3f, i == 0 ? NPC.height : segmentHeight));
            }
        }
        public override void AI()
        {
            if (modNPC.Segments.Count == 0)
            {
                AddSegments();
            }
            if (NPC.ai[3] > 0)
                NPC.ai[3]--;
            modNPC.RogueWormAI(NPC, 12f, MathHelper.Pi / 60f, 480);
            NPC.rotation = NPC.velocity.ToRotation();
            modNPC.UpdateWormSegments(NPC);
            Point tile = new Point((int)(NPC.Center.X / 16f), (int)(NPC.Center.Y / 16f));
            if (!Main.dedServ && TerRoguelikeUtils.ParanoidTileRetrieval(tile.X, tile.Y).IsTileSolidGround(true))
            {
                if (NPC.ai[3] <= 0)
                {
                    SoundEngine.PlaySound(SoundID.WormDig with { Volume = 1f }, NPC.Center);
                    NPC.ai[3] += 60 / NPC.velocity.Length();
                    Color lightColor = Lighting.GetColor(tile);
                    if (lightColor.R <= 30 && lightColor.G <= 30 && lightColor.B <= 30)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.SpelunkerGlowstickSparkle);
                            d.velocity = NPC.velocity * 0.25f;
                        }
                    }
                }
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (modNPC.Segments == null || modNPC.Segments.Count == 0)
                return;
            if (NPC.life <= 0)
            {
                SoundEngine.PlaySound(SoundID.NPCHit2, modNPC.Segments[modNPC.hitSegment].Position);
                SoundEngine.PlaySound(SoundID.NPCDeath5, modNPC.Segments[modNPC.hitSegment].Position);
                for (int i = 0; i < modNPC.Segments.Count; i++)
                {
                    WormSegment segment = modNPC.Segments[i];
                    if (i == 0)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, 67, NPC.scale);
                        continue;
                    }
                    if (i == modNPC.Segments.Count - 1)
                    {
                        Gore.NewGore(NPC.GetSource_Death(), segment.Position, segment.Position - segment.OldPosition, 69, NPC.scale);
                        continue;
                    }
                    Gore.NewGore(NPC.GetSource_Death(), segment.Position, segment.Position - segment.OldPosition, 68, NPC.scale);
                }
            }
            else
                SoundEngine.PlaySound(SoundID.NPCHit2, modNPC.Segments[modNPC.hitSegment].Position);
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (modNPC.OverrideIgniteVisual && modNPC.ignitedStacks.Count > 0)
            {
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = modNPC.Segments.Count - 1; i >= 0; i--)
                {
                    Texture2D texture;
                    WormSegment segment = modNPC.Segments[i];
                    if (i == 0)
                        texture = headTex;
                    else if (i == modNPC.Segments.Count - 1)
                        texture = tailTex;
                    else
                        texture = bodyTex;

                    Color color = Color.Lerp(Color.Yellow, Color.OrangeRed, Main.rand.NextFloat(0.4f, 0.6f + float.Epsilon) + 0.2f + (0.2f * (float)Math.Cos((Main.GlobalTimeWrappedHourly * 20f)))) * 0.8f;
                    Vector3 colorHSL = Main.rgbToHsl(color);
                    float outlineThickness = 1f;
                    SpriteEffects spriteEffects = NPC.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                    GameShaders.Misc["TerRoguelike:BasicTint"].UseOpacity(1f);
                    GameShaders.Misc["TerRoguelike:BasicTint"].UseColor(Main.hslToRgb(1 - colorHSL.X, colorHSL.Y, colorHSL.Z));
                    GameShaders.Misc["TerRoguelike:BasicTint"].Apply();

                    Vector2 position = segment.Position + (Vector2.UnitY * NPC.gfxOffY);
                    for (float j = 0; j < 1; j += 0.125f)
                    {
                        spriteBatch.Draw(texture, position + (j * MathHelper.TwoPi + segment.Rotation + MathHelper.PiOver2).ToRotationVector2() * outlineThickness - Main.screenPosition, null, color, segment.Rotation + MathHelper.PiOver2, texture.Size() * 0.5f, NPC.scale, spriteEffects, 0f);
                    }
                }
            }
            for (int i = modNPC.Segments.Count - 1; i >= 0; i--)
            {
                Texture2D texture;
                WormSegment segment = modNPC.Segments[i];
                if (i == 0)
                    texture = headTex;
                else if (i == modNPC.Segments.Count - 1)
                    texture = tailTex;
                else
                    texture = bodyTex;

                modNPC.EliteEffectSpritebatch(NPC, new(-1, 1, texture.Size(), texture.Frame()));
                Color color = modNPC.ignitedStacks.Count > 0 ? Color.Lerp(Color.White, Color.OrangeRed, 0.4f) : Lighting.GetColor(new Point((int)(segment.Position.X / 16), (int)(segment.Position.Y / 16)));
                spriteBatch.Draw(texture, segment.Position - screenPos, null, color, segment.Rotation + MathHelper.PiOver2, texture.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            return false;
        }
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            return true;
        }
        public override bool CanHitNPC(NPC target)
        {
            return true;
        }
        public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
        {
            return true;
        }
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if ((projectile.hostile && !NPC.friendly) || (projectile.friendly && NPC.friendly))
                return false;

            for (int i = 0; i < modNPC.Segments.Count; i++)
            {
                WormSegment segment = modNPC.Segments[i];
                bool pass = projectile.Colliding(projectile.getRect(), new Rectangle((int)(segment.Position.X - ((i == 0 ? NPC.width : segment.Height) / 2)), (int)(segment.Position.Y - ((i == 0 ? NPC.height : segment.Height) / 2)), NPC.width, NPC.height));
                if (pass)
                {
                    projectile.ModProj().ultimateCollideOverride = true;
                    modNPC.hitSegment = i;
                    return null;
                }
            }
            return false;
        }
    }
}
