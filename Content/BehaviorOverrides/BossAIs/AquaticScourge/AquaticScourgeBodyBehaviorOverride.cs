using CalamityMod;
using CalamityMod.NPCs.AquaticScourge;
using InfernumMode.Core.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.AquaticScourge
{
    public class AquaticScourgeBodyBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<AquaticScourgeBody>();

        public override bool PreAI(NPC npc)
        {
            DoAI(npc);
            return false;
        }

        public static void DoAI(NPC npc)
        {
            // Go away if the ahead segment is not present.
            if (!Main.npc.IndexInRange((int)npc.ai[1]) || !Main.npc[(int)npc.ai[1]].active)
            {
                npc.life = 0;
                npc.HitEffect(0, 10.0);
                npc.active = false;
                npc.netUpdate = true;
                return;
            }

            ref float segmentGrowInterpolant = ref npc.Infernum().ExtraAI[0];
            ref float segmentRegrowRate = ref npc.Infernum().ExtraAI[1];

            // Make segments slowly regrow their spikes.
            segmentGrowInterpolant = MathHelper.Clamp(segmentGrowInterpolant + segmentRegrowRate, 0f, 1f);
            if (segmentRegrowRate <= 0f)
            {
                segmentRegrowRate = Main.rand.NextFloat(0.0014f, 0.0023f);
                segmentGrowInterpolant = 1f;
                npc.netUpdate = true;
            }

            NPC aheadSegment = Main.npc[(int)npc.ai[1]];
            NPC headSegment = Main.npc[npc.realLife];

            // Fade in if the ahead segment has faded in sufficiently, resulting into the entire worm smoothly appearing.
            if (aheadSegment.alpha < 128)
                npc.alpha = Utils.Clamp(npc.alpha - 42, 0, 255);

            // Inherit attributes from the head.
            npc.target = aheadSegment.target;
            npc.defense = aheadSegment.defense;
            npc.damage = headSegment.damage >= 1 ? 60 : 0;
            npc.dontTakeDamage = headSegment.dontTakeDamage;
            npc.chaseable = headSegment.chaseable;
            npc.Calamity().newAI[0] = npc.chaseable.ToInt();
            npc.Calamity().DR = MathHelper.Min(npc.Calamity().DR, 0.4f);

            // Stay behind the previous segment.
            Vector2 directionToNextSegment = aheadSegment.Center - npc.Center;
            if (aheadSegment.rotation != npc.rotation)
                directionToNextSegment = directionToNextSegment.RotatedBy(MathHelper.WrapAngle(aheadSegment.rotation - npc.rotation) * 0.075f);

            npc.rotation = directionToNextSegment.ToRotation() + MathHelper.PiOver2;
            npc.Center = aheadSegment.Center - directionToNextSegment.SafeNormalize(Vector2.Zero) * npc.width * npc.scale;

            // Shudder if the head says to do so.
            if (headSegment.ai[2] == (int)AquaticScourgeHeadBehaviorOverride.AquaticScourgeAttackType.PerpendicularSpikeBarrage)
            {
                if (headSegment.Infernum().ExtraAI[3] >= 1f && npc.ai[3] >= 2f)
                    npc.Center += directionToNextSegment.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2) * (float)Math.Sin(MathHelper.Pi * npc.ai[3] / 35f + headSegment.ai[3] / 15f) * 3.6f;
            }
        }

        public static IEnumerable<Vector2> GetSpikePositions(NPC npc)
        {
            yield return npc.Center + new Vector2(16f, 4f).RotatedBy(npc.rotation) * npc.scale;
            yield return npc.Center + new Vector2(16f, -10f).RotatedBy(npc.rotation) * npc.scale;
            yield return npc.Center + new Vector2(-18f, 4f).RotatedBy(npc.rotation) * npc.scale;
            yield return npc.Center + new Vector2(-18f, -10f).RotatedBy(npc.rotation) * npc.scale;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Texture2D bodyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeBody").Value;
            Texture2D spikeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/BehaviorOverrides/BossAIs/AquaticScourge/AquaticScourgeBodySpike").Value;
            Vector2 origin = bodyTexture.Size() * 0.5f;
            Main.EntitySpriteDraw(bodyTexture, drawPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, npc.scale, 0, 0);

            // Draw spikes.
            int index = 0;
            float spikeScale = (float)Math.Pow(npc.Infernum().ExtraAI[0], 1.64) * npc.scale;
            foreach (Vector2 spikePosition in GetSpikePositions(npc))
            {
                SpriteEffects direction = index < 2 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                origin = spikeTexture.Size() * Vector2.UnitX;
                if (index < 2)
                    origin.X = spikeTexture.Width - origin.X;

                Main.EntitySpriteDraw(spikeTexture, spikePosition - Main.screenPosition, null, npc.GetAlpha(lightColor), npc.rotation, origin, spikeScale, direction, 0);
                index++;
            }

            return false;
        }
    }
}
