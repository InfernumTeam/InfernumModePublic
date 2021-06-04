﻿using CalamityMod.NPCs;
using CalamityMod.Projectiles.Boss;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

using CrimulanSGBig = CalamityMod.NPCs.SlimeGod.SlimeGodRun;
using CrimulanSGSmall = CalamityMod.NPCs.SlimeGod.SlimeGodRunSplit;
using EbonianSGBig = CalamityMod.NPCs.SlimeGod.SlimeGod;
using EbonianSGSmall = CalamityMod.NPCs.SlimeGod.SlimeGodSplit;

namespace InfernumMode.FuckYouModeAIs.SlimeGod
{
    public class SlimeGodCoreAIClass
    {
        #region Enumerations
        public enum SlimeGodCoreAttackType
        {
            HoverAndFireAbyssBalls,
            SlowHorizontalCharge,
            SlowVerticalCharge,
            FastHover,
            FastHorizontalCharge,
            FastVerticalCharge,
            Spin
        }
        #endregion

        #region AI

        [OverrideAppliesTo("SlimeGodCore", typeof(SlimeGodCoreAIClass), "SlimeGodCoreAI", EntityOverrideContext.NPCAI)]
        public static bool SlimeGodCoreAI(NPC npc)
        {
            bool anyBigBois = NPC.AnyNPCs(ModContent.NPCType<CrimulanSGBig>()) || NPC.AnyNPCs(ModContent.NPCType<CrimulanSGSmall>()) || NPC.AnyNPCs(ModContent.NPCType<EbonianSGBig>()) || NPC.AnyNPCs(ModContent.NPCType<EbonianSGSmall>());
            npc.dontTakeDamage = anyBigBois;

            // Do targeting.
            npc.TargetClosest();
            Player target = Main.player[npc.target];

            // This debuff is not fun.
            if (target.HasBuff(BuffID.VortexDebuff))
                target.ClearBuff(BuffID.VortexDebuff);

            // This will affect the other gods as well in terms of behavior.
            ref float universalState = ref npc.ai[0];
            ref float universalTimer = ref npc.ai[1];
            ref float localState = ref npc.ai[2];
            ref float localTimer = ref npc.ai[3];

            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[3] == 0f)
            {
                NPC.SpawnOnPlayer(npc.target, ModContent.NPCType<EbonianSGBig>());
                NPC.SpawnOnPlayer(npc.target, ModContent.NPCType<CrimulanSGBig>());
                npc.localAI[3] = 1f;
			}

            // Set the universal whoAmI variable.
            CalamityGlobalNPC.slimeGod = npc.whoAmI;

            universalTimer++;
            int attackLength = 900;
            if (universalState == (int)CrimulanSlimeGodAIClass.CrimulanSlimeGodAttackType.IchorSlam)
                attackLength = 270;
            if (universalState == (int)CrimulanSlimeGodAIClass.CrimulanSlimeGodAttackType.BigSlam)
                attackLength = 270;
            if (universalState == (int)CrimulanSlimeGodAIClass.CrimulanSlimeGodAttackType.GelCloudSlam)
                attackLength = 440;
            if (universalTimer > attackLength)
            {
                universalTimer = 0f;

                int attackCycleCount = 3;
                if (Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed) && Main.npc[CalamityGlobalNPC.slimeGodRed].life < Main.npc[CalamityGlobalNPC.slimeGodRed].lifeMax * 0.5f)
                    attackCycleCount = 4;

                if (Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
				{
                    Main.npc[CalamityGlobalNPC.slimeGodRed].noTileCollide = false;
                    Main.npc[CalamityGlobalNPC.slimeGodPurple].noTileCollide = false;
                }
                universalState = (universalState + 1f) % attackCycleCount;
                UpdateOtherSlimeAIValues();
                npc.netUpdate = true;
            }

            switch ((SlimeGodCoreAttackType)(int)localState)
            {
                case SlimeGodCoreAttackType.HoverAndFireAbyssBalls:
                    DoAttack_HoverAndFireAbyssBalls(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.SlowHorizontalCharge:
                    DoAttack_SlowHorizontalCharge(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.SlowVerticalCharge:
                    DoAttack_SlowVerticalCharge(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.FastHover:
                    DoAttack_FastHover(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.FastHorizontalCharge:
                    DoAttack_FastHorizontalCharge(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.FastVerticalCharge:
                    DoAttack_FastVerticalCharge(npc, target, ref localTimer);
                    break;
                case SlimeGodCoreAttackType.Spin:
                    DoAttack_Spin(npc, target, ref localTimer);
                    break;
            }

            localTimer++;
            return false;
        }

        public static void UpdateOtherSlimeAIValues()
        {
            IEnumerable<NPC> slimes = Main.npc.Where(n => (n.type == ModContent.NPCType<CrimulanSGBig>() || n.type == ModContent.NPCType<EbonianSGBig>()) && n.active && n.whoAmI != 255);
            foreach (NPC slime in slimes)
            {
                for (int i = 0; i < 5; i++)
                    slime.Infernum().ExtraAI[i] = 0f;
                slime.netUpdate = true;
            }
        }

        public static void DoAttack_HoverAndFireAbyssBalls(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 300f;
            if (!npc.WithinRange(destination, 200f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 10f, 0.4f);
                npc.rotation = npc.velocity.X * 0.15f;
            }
            else
                npc.rotation += npc.velocity.X * 0.04f;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 180f == 179f)
            {
                Vector2 mineShootVelocity = npc.SafeDirectionTo(target.Center) * 12f;
                Utilities.NewProjectileBetter(npc.Center + mineShootVelocity * 3f, mineShootVelocity, ModContent.ProjectileType<ExplosiveAbyssMine>(), 90, 0f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 520f)
            {
                attackTimer = 0f;
                SelectAttackState(npc);
            }
        }

        public static void DoAttack_SlowHorizontalCharge(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float verticalOffsetLeniance = 65f;
                float flySpeed = 7f;
                float flyInertia = 4f;
                float horizontalOffset = 720f;
                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset - 50f && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 1f)
            {
                int chargeDelay = 30;
                float flySpeed = 9f;
                float flyInertia = 8f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 2f)
            {
                npc.rotation += (npc.velocity.X > 0f).ToDirectionInt() * 0.15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 75f)
                {
                    attackTimer = 0f;
                    SelectAttackState(npc);
                }
            }
        }

        public static void DoAttack_SlowVerticalCharge(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float flySpeed = 7f;
                float flyInertia = 4f;
                float verticalOffset = 400f;
                Vector2 destination = target.Center - Vector2.UnitY * Math.Sign(target.Center.Y - npc.Center.Y) * verticalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.Y - target.Center.Y) > verticalOffset - 50f)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 1f)
            {
                int chargeDelay = 30;
                float flySpeed = 9f;
                float flyInertia = 8f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 2f)
            {
                npc.rotation += (npc.velocity.Y > 0f).ToDirectionInt() * 0.15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 75f)
                {
                    attackTimer = 0f;
                    SelectAttackState(npc);
                }
            }
        }

        public static void DoAttack_Spin(NPC npc, Player target, ref float attackTimer)
		{
            ref float spinAngleOffset = ref npc.Infernum().ExtraAI[0];
            if (attackTimer < 180f)
            {
                Vector2 destination = target.Center + spinAngleOffset.ToRotationVector2() * 360f;
                npc.Center = npc.Center.MoveTowards(destination, 46f);

                spinAngleOffset += MathHelper.TwoPi * Utils.InverseLerp(170f, 150f, attackTimer, true) / 90f;
                npc.rotation += spinAngleOffset * 0.3f;

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 22f == 21f)
                {
                    Vector2 abyssBallVelocity = npc.SafeDirectionTo(target.Center) * -7f;
                    Utilities.NewProjectileBetter(npc.Center, abyssBallVelocity, ModContent.ProjectileType<RedirectingAbyssBall>(), 95, 0f);
                }
            }

            if (attackTimer == 180f)
			{
                npc.velocity = npc.SafeDirectionTo(target.Center + target.velocity * 10f) * 16.5f;
                npc.netUpdate = true;
			}

            if (attackTimer > 180f)
                npc.rotation += npc.velocity.X * 0.05f;

            if (attackTimer > 240f)
                npc.velocity *= 0.98f;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 270f)
            {
                attackTimer = 0f;
                SelectAttackState(npc);
            }
        }

        public static void DoAttack_FastHover(NPC npc, Player target, ref float attackTimer)
        {
            Vector2 destination = target.Center - Vector2.UnitY * 350f;
            if (!npc.WithinRange(destination, 250f))
            {
                npc.velocity = npc.velocity.MoveTowards(npc.SafeDirectionTo(destination) * 14f, 0.6f);
                npc.rotation = npc.velocity.X * 0.15f;
            }
            else
                npc.rotation += npc.velocity.X * 0.04f;

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 50f == 49f)
            {
                Vector2 mineShootVelocity = npc.SafeDirectionTo(target.Center) * 12f;
                Utilities.NewProjectileBetter(npc.Center + mineShootVelocity * 3f, mineShootVelocity, ModContent.ProjectileType<ExplosiveAbyssMine>(), 95, 0f);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 80f == 79f)
            {
                Vector2 mineShootVelocity = npc.SafeDirectionTo(target.Center) * 8f;
                Utilities.NewProjectileBetter(npc.Center + mineShootVelocity * 3f, mineShootVelocity, ModContent.ProjectileType<RedirectingAbyssBall>(), 95, 0f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 520f)
            {
                attackTimer = 0f;
                SelectAttackState(npc);
                npc.netUpdate = true;
            }
        }

        public static void DoAttack_FastHorizontalCharge(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float verticalOffsetLeniance = 65f;
                float flySpeed = 10f;
                float flyInertia = 4f;
                float horizontalOffset = 720f;
                Vector2 destination = target.Center - Vector2.UnitX * Math.Sign(target.Center.X - npc.Center.X) * horizontalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.X - target.Center.X) > horizontalOffset - 50f && Math.Abs(npc.Center.Y - target.Center.Y) < verticalOffsetLeniance)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 1f)
            {
                int chargeDelay = 30;
                float flySpeed = 14.5f;
                float flyInertia = 8f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 2f)
            {
                // Release abyss balls upward.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 20f == 19f)
				{
                    Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitY * 6f, ModContent.ProjectileType<AbyssBallVolley>(), 95, 0f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitY * 6f, ModContent.ProjectileType<AbyssBallVolley>(), 95, 0f);
                }

                npc.rotation += (npc.velocity.X > 0f).ToDirectionInt() * 0.15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 90f)
                {
                    attackTimer = 0f;
                    SelectAttackState(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void DoAttack_FastVerticalCharge(NPC npc, Player target, ref float attackTimer)
        {
            ref float attackSubstate = ref npc.Infernum().ExtraAI[0];

            // Line up for the charge.
            if (attackSubstate == 0f)
            {
                float flySpeed = 9.5f;
                float flyInertia = 4f;
                float verticalOffset = 400f;
                Vector2 destination = target.Center - Vector2.UnitY * Math.Sign(target.Center.Y - npc.Center.Y) * verticalOffset;

                // Fly towards the destination beside the player.
                npc.velocity = (npc.velocity * (flyInertia - 1f) + npc.SafeDirectionTo(destination) * flySpeed) / flyInertia;

                // If within a good approximation of the player's position, prepare charging.
                if (Math.Abs(npc.Center.Y - target.Center.Y) > verticalOffset - 50f)
                {
                    attackSubstate = 1f;
                    attackTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Prepare for the charge.
            if (attackSubstate == 1f)
            {
                int chargeDelay = 30;
                float flySpeed = 9f;
                float flyInertia = 13.5f;
                Vector2 chargeVelocity = npc.SafeDirectionTo(target.Center) * flySpeed;
                npc.velocity = (npc.velocity * (flyInertia - 1f) + chargeVelocity) / flyInertia;

                if (attackTimer >= chargeDelay)
                {
                    attackTimer = 0f;
                    attackSubstate = 2f;
                    npc.velocity = chargeVelocity;
                    npc.netUpdate = true;
                }
            }

            // Do the actual charge.
            if (attackSubstate == 2f)
            {
                // Release abyss balls upward.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % 20f == 19f)
                {
                    Utilities.NewProjectileBetter(npc.Center, -Vector2.UnitX * 6f, ModContent.ProjectileType<AbyssBallVolley>(), 95, 0f);
                    Utilities.NewProjectileBetter(npc.Center, Vector2.UnitX * 6f, ModContent.ProjectileType<AbyssBallVolley>(), 95, 0f);
                }

                npc.rotation += (npc.velocity.Y > 0f).ToDirectionInt() * 0.15f;
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer >= 75f)
                {
                    attackTimer = 0f;
                    SelectAttackState(npc);
                    npc.netUpdate = true;
                }
            }
        }

        public static void SelectAttackState(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            npc.netUpdate = true;
            for (int i = 0; i < 4; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            ref float localState = ref npc.ai[2];
            if (!Main.npc.IndexInRange(CalamityGlobalNPC.slimeGodRed))
			{
                if (localState < 3f)
                    localState = 3f;
                localState++;

                int highestAttackIndex = npc.life < npc.lifeMax * 0.5f ? 7 : 6;
                if (localState >= highestAttackIndex)
                    localState = 3f;
                return;
			}

            float universalState = npc.ai[0];
            float oldLocalState = npc.ai[2];

            WeightedRandom<float> newStatePicker = new WeightedRandom<float>(Main.rand);
            newStatePicker.Add(0, universalState == 1f || universalState == 3f ? 3f : 1f);
            newStatePicker.Add(1, universalState == 2f ? 3f : 1f);
            newStatePicker.Add(2, universalState == 4f ? 3f : 1f);
            
            localState = newStatePicker.Get();
            while (localState == oldLocalState)
                localState = (localState + 1f) % 3f;
        }
        #endregion AI
    }
}
