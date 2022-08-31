using CalamityMod;
using CalamityMod.Events;
using CalamityMod.Items.Armor.OmegaBlue;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Polterghast;
using CalamityMod.Sounds;
using InfernumMode.BehaviorOverrides.BossAIs.Cultist;
using InfernumMode.OverridingSystem;
using InfernumMode.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;
using PolterghastBoss = CalamityMod.NPCs.Polterghast.Polterghast;

namespace InfernumMode.BehaviorOverrides.BossAIs.Polterghast
{
    public class PolterghastBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => ModContent.NPCType<PolterghastBoss>();

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw | NPCOverrideContext.NPCFindFrame;

        public const float Phase2LifeRatio = 0.65f;

        public const float Phase3LifeRatio = 0.35f;

        #region Enumerations
        public enum PolterghastAttackType
        {
            EctoplasmUppercutCharges,
            LegSwipes,
            WispCircleCharges,
            AsgoreRingSoulAttack,
            ArcingSouls,
            VortexCharge,
            SpiritPetal,
            CloneSplit,
            DesperationAttack
        }
        #endregion

        #region AI

        public static CurveSegment Anticipation => new(EasingType.PolyOut, 0f, 0f, 0.2f, 3);

        public static CurveSegment Slash => new(EasingType.SineIn, 0.18f, 0.2f, 0.8f);

        public static CurveSegment Recovery => new(EasingType.PolyIn, 0.5f, 1f, -1f, 100);

        public override bool PreAI(NPC npc)
        {
            // Set the whoAmI index.
            CalamityGlobalNPC.ghostBoss = npc.whoAmI;

            // Ensure the boss always draws. Without this telegraphs are not properly displayed.
            NPCID.Sets.MustAlwaysDraw[npc.type] = true;

            // Initialize by creating legs.
            if (Main.netMode != NetmodeID.MultiplayerClient && npc.localAI[3] == 0f)
            {
                for (int i = 0; i < 4; i++)
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<PolterghastLeg>(), 1, i);
                npc.localAI[3] = 1f;
            }

            // Select a new target if an old one was lost.
            // If no valid one exists, despawn.
            npc.TargetClosestIfTargetIsInvalid();
            if (!Main.player.IndexInRange(npc.target) || !Main.player[npc.target].active || Main.player[npc.target].dead)
            {
                DoDespawnEffects(npc);
                return false;
            }

            Player target = Main.player[npc.target];
            PolterghastAttackType attackState = (PolterghastAttackType)(int)npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float totalReleasedSouls = ref npc.ai[2];
            ref float dyingTimer = ref npc.Infernum().ExtraAI[6];
            ref float initialDeathPositionX = ref npc.Infernum().ExtraAI[7];
            ref float initialDeathPositionY = ref npc.Infernum().ExtraAI[8];
            ref float legToManuallyControlIndex = ref npc.Infernum().ExtraAI[9];
            ref float vignetteInterpolant = ref npc.Infernum().ExtraAI[12];
            ref float vignetteRadiusDecreaseFactor = ref npc.Infernum().ExtraAI[13];
            ref float veryFirstAttack = ref npc.Infernum().ExtraAI[14];
            ref float telegraphOpacity = ref npc.localAI[1];
            ref float telegraphDirection = ref npc.localAI[2];

            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            bool enraged = npc.Bottom.Y < Main.worldSurface * 16f && !BossRushEvent.BossRushActive;
            npc.Calamity().CurrentlyEnraged = enraged;

            // Store the enraged field so that the limbs can check it more easily.
            npc.ai[3] = enraged.ToInt();

            // Use a ghostly hit sound in the third phase.
            if (phase3)
                npc.HitSound = SoundID.NPCHit36;

            if (totalReleasedSouls < 0f)
                totalReleasedSouls = 0f;

            npc.scale = MathHelper.Lerp(1.225f, 0.68f, MathHelper.Clamp(totalReleasedSouls / 60f, 0f, 1f));

            // Perform the death animation as necessary.
            if (dyingTimer > 0f)
            {
                DoBehavior_DeathAnimation(npc, target, ref dyingTimer, ref totalReleasedSouls, ref initialDeathPositionX, ref initialDeathPositionY);
                return false;
            }

            int totalClones = NPC.CountNPCS(ModContent.NPCType<PolterPhantom>());
            if (totalClones > 0)
                npc.scale = MathHelper.Lerp(0.7f, 1.225f, 1f - totalClones / 2f);

            // Reset things every frame.
            telegraphOpacity = 0f;
            npc.hide = false;
            npc.dontTakeDamage = false;
            npc.damage = npc.defDamage;
            npc.Calamity().DR = 0.2f;
            if (veryFirstAttack == 0f)
                npc.Opacity = 0f;

            switch (attackState)
            {
                case PolterghastAttackType.LegSwipes:
                    DoBehavior_LegSwipes(npc, target, ref legToManuallyControlIndex, ref attackTimer);
                    break;
                case PolterghastAttackType.WispCircleCharges:
                    DoBehavior_WispCircleCharges(npc, target, ref attackTimer);
                    break;
                case PolterghastAttackType.AsgoreRingSoulAttack:
                    DoBehavior_AsgoreRingSoulAttack(npc, target, ref totalReleasedSouls, ref attackTimer);
                    break;
                case PolterghastAttackType.EctoplasmUppercutCharges:
                    DoBehavior_EctoplasmUppercutCharges(npc, target, ref attackTimer, ref telegraphDirection, ref telegraphOpacity, ref veryFirstAttack);
                    break;
                case PolterghastAttackType.ArcingSouls:
                    DoBehavior_ArcingSouls(npc, target, ref attackTimer);
                    break;
                case PolterghastAttackType.SpiritPetal:
                    DoBehavior_SpiritPetal(npc, target, ref attackTimer, ref totalReleasedSouls, enraged);
                    break;
                case PolterghastAttackType.VortexCharge:
                    DoBehavior_DoVortexCharge(npc, target, ref attackTimer, enraged);
                    break;                    
                case PolterghastAttackType.CloneSplit:
                    DoBehavior_CloneSplit(npc, target, ref attackTimer, enraged);
                    break;
                case PolterghastAttackType.DesperationAttack:
                    DoBehavior_DesperationAttack(npc, target, ref attackTimer, ref vignetteInterpolant, ref vignetteRadiusDecreaseFactor);
                    break;
            }
            
            // Always disable contact damage if not drawing at all.
            if (npc.hide)
                npc.damage = 0;

            attackTimer++;
            return false;
        }

        public static void DoDespawnEffects(NPC npc)
        {
            npc.velocity.Y += 0.4f;
            npc.velocity *= 1.035f;
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
            npc.Opacity = MathHelper.Clamp(npc.Opacity - 0.01f, 0f, 1f);
            npc.dontTakeDamage = true;

            if (npc.timeLeft > 200)
                npc.timeLeft = 200;
        }

        public static void SelectNextAttack(NPC npc)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            bool phase2 = lifeRatio < Phase2LifeRatio;
            bool phase3 = lifeRatio < Phase3LifeRatio;
            PolterghastAttackType[] phase1Cycle = new PolterghastAttackType[]
            {
                PolterghastAttackType.EctoplasmUppercutCharges,
                PolterghastAttackType.LegSwipes,
                PolterghastAttackType.WispCircleCharges,
                PolterghastAttackType.EctoplasmUppercutCharges,
                PolterghastAttackType.SpiritPetal,
            };
            PolterghastAttackType[] phase2Cycle = new PolterghastAttackType[]
            {
                PolterghastAttackType.AsgoreRingSoulAttack,
                PolterghastAttackType.VortexCharge,
                PolterghastAttackType.ArcingSouls,
                PolterghastAttackType.SpiritPetal,
                PolterghastAttackType.EctoplasmUppercutCharges,
                PolterghastAttackType.LegSwipes,
                PolterghastAttackType.VortexCharge,
                PolterghastAttackType.WispCircleCharges,
                PolterghastAttackType.ArcingSouls,
                PolterghastAttackType.EctoplasmUppercutCharges,
                PolterghastAttackType.LegSwipes,
                PolterghastAttackType.SpiritPetal,
            };
            PolterghastAttackType[] phase3Cycle = new PolterghastAttackType[]
            {
                PolterghastAttackType.CloneSplit,
                PolterghastAttackType.AsgoreRingSoulAttack,
                PolterghastAttackType.VortexCharge,
                PolterghastAttackType.ArcingSouls,
                PolterghastAttackType.SpiritPetal,
                PolterghastAttackType.EctoplasmUppercutCharges,
                PolterghastAttackType.LegSwipes,
                PolterghastAttackType.CloneSplit,
                PolterghastAttackType.VortexCharge,
                PolterghastAttackType.WispCircleCharges,
                PolterghastAttackType.ArcingSouls,
                PolterghastAttackType.EctoplasmUppercutCharges,
                PolterghastAttackType.LegSwipes,
                PolterghastAttackType.SpiritPetal,
            };

            npc.TargetClosest();

            npc.Infernum().ExtraAI[10]++;
            if (phase3)
                npc.ai[0] = (int)phase3Cycle[(int)npc.Infernum().ExtraAI[10] % phase3Cycle.Length];
            else if (phase2)
                npc.ai[0] = (int)phase2Cycle[(int)npc.Infernum().ExtraAI[10] % phase2Cycle.Length];
            else
                npc.ai[0] = (int)phase1Cycle[(int)npc.Infernum().ExtraAI[10] % phase1Cycle.Length];

            // Transition to the desperation phase after dying.
            if (npc.Infernum().ExtraAI[11] == 1f)
                npc.ai[0] = (int)PolterghastAttackType.DesperationAttack;

            npc.ai[1] = 0f;
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            npc.netUpdate = true;
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float dyingTimer, ref float totalReleasedSouls, ref float initialDeathPositionX, ref float initialDeathPositionY)
        {
            npc.dontTakeDamage = true;
            npc.DeathSound = InfernumSoundRegistry.PoltergastDeathEcho;

            // Clear away any clones and legs.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int cloneType = NPC.CountNPCS(ModContent.NPCType<PolterPhantom>());
                int legType = ModContent.NPCType<PolterghastLeg>();
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if ((Main.npc[i].type == cloneType || Main.npc[i].type == legType) && Main.npc[i].active)
                    {
                        Main.npc[i].life = 0;
                        Main.npc[i].active = false;
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, i);
                    }
                }
            }

            // Quickly slow down.
            npc.velocity *= 0.955f;

            dyingTimer++;

            float turnSpeed = Utils.GetLerpValue(240f, 45f, dyingTimer, true);
            if (turnSpeed > 0f)
                npc.rotation = npc.rotation.AngleLerp(npc.AngleTo(target.Center) + MathHelper.PiOver2, turnSpeed);

            // Begin releasing souls.
            if (dyingTimer > 210f && dyingTimer % 2f == 0f && totalReleasedSouls < 60f)
            {
                if (dyingTimer % 8f == 0f)
                    SoundEngine.PlaySound(SoundID.NPCHit36, target.Center);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(7f, 13f);
                    int soul = Utilities.NewProjectileBetter(npc.Center + soulVelocity * 5f, soulVelocity, ModContent.ProjectileType<NonReturningSoul>(), 0, 0f);
                    if (Main.projectile.IndexInRange(soul))
                        Main.projectile[soul].ai[0] = Main.rand.NextBool(2).ToDirectionInt();

                    totalReleasedSouls++;

                    npc.netSpam = 0;
                    npc.netUpdate = true;
                }
            }

            if (totalReleasedSouls >= 60f)
            {
                // Focus on the boss as it jitters and explode.
                if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 2700f))
                {
                    Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                    Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.GetLerpValue(270f, 290f, dyingTimer, true);
                    Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.GetLerpValue(370f, 362f, dyingTimer, true);
                }

                Vector2 jitter = Main.rand.NextVector2Unit() * MathHelper.SmoothStep(1f, 3.25f, Utils.GetLerpValue(270f, 350f, dyingTimer, true));
                Main.LocalPlayer.Infernum().CurrentScreenShakePower = jitter.Length() * Utils.GetLerpValue(1950f, 1100f, Main.LocalPlayer.Distance(npc.Center), true) * 4f;

                if (initialDeathPositionX != 0f && initialDeathPositionY != 0f)
                    npc.Center = new Vector2(initialDeathPositionX, initialDeathPositionY) + jitter;

                // Make a flame-like sound effect right before dying.
                if (dyingTimer == 368f)
                    SoundEngine.PlaySound(CommonCalamitySounds.FlareSound, target.Center);
                else
                {
                    initialDeathPositionX = npc.Center.X;
                    initialDeathPositionY = npc.Center.Y;
                    npc.netUpdate = true;
                }

                // Release a bunch of other souls right before death.
                if (Main.netMode != NetmodeID.MultiplayerClient && dyingTimer > 360f)
                {
                    Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 9f);
                    int soul = Utilities.NewProjectileBetter(npc.Center + soulVelocity * 5f, soulVelocity, ModContent.ProjectileType<NonReturningSoul>(), 0, 0f);
                    if (Main.projectile.IndexInRange(soul))
                    {
                        Main.projectile[soul].ai[0] = Main.rand.NextBool(2).ToDirectionInt();
                        Main.projectile[soul].ai[1] = 1f;
                    }
                }

                // Release a bunch of souls and transition to the final phase.
                if (Main.netMode != NetmodeID.MultiplayerClient && dyingTimer == 370f)
                {
                    for (int i = 0; i < 125; i++)
                    {
                        Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 9f);
                        int soul = Utilities.NewProjectileBetter(npc.Center + soulVelocity * 5f, soulVelocity, ModContent.ProjectileType<NonReturningSoul>(), 0, 0f);
                        if (Main.projectile.IndexInRange(soul))
                        {
                            Main.projectile[soul].ai[0] = Main.rand.NextBool(2).ToDirectionInt();
                            Main.projectile[soul].ai[1] = 1f;
                        }
                    }

                    SoundEngine.PlaySound(npc.DeathSound, target.Center);
                    SelectNextAttack(npc);
                }
                if (dyingTimer >= 370f)
                    dyingTimer = 0f;
            }
            else if (dyingTimer > 270f)
            {
                // Declare the death position for the sake of jittering later.
                if (initialDeathPositionX == 0f || initialDeathPositionY == 0f)
                {
                    initialDeathPositionX = npc.Center.X;
                    initialDeathPositionY = npc.Center.Y;
                    npc.velocity = Vector2.Zero;
                    npc.netUpdate = true;
                }
                dyingTimer = 260f;
            }
        }

        public static void TeleportToPosition(NPC polterghast, Vector2 teleportCenter, bool createTelegraphLine = false)
        {
            if (createTelegraphLine)
            {
                int dustCount = 250;
                for (int i = 0; i < 40; i++)
                {
                    Dust magic = Dust.NewDustPerfect(polterghast.Center + Main.rand.NextVector2Circular(50f, 50f), 264);
                    magic.velocity = -Vector2.UnitY * Main.rand.NextFloat(2f, 4f);
                    magic.color = Color.Blue;
                    magic.scale = 1.3f;
                    magic.fadeIn = 0.5f;
                    magic.noGravity = true;
                    magic.noLight = true;

                    magic = Dust.CloneDust(magic);
                    magic.position = teleportCenter + Main.rand.NextVector2Circular(50f, 50f);
                }

                for (int i = 0; i < dustCount; i++)
                {
                    Vector2 dustDrawPosition = Vector2.Lerp(polterghast.Center, teleportCenter, i / (float)dustCount);

                    Dust magic = Dust.NewDustPerfect(dustDrawPosition, 267);
                    magic.velocity = -Vector2.UnitY * Main.rand.NextFloat(0.2f, 0.235f);
                    magic.color = Color.LightCyan;
                    magic.color.A = 0;
                    magic.scale = 0.8f;
                    magic.fadeIn = 1.4f;
                    magic.noGravity = true;
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    return;

                // Create the telegraph pulses with slightly differing lifetimes.
                for (int i = 0; i < 6; i++)
                {
                    int lifetimeReduction = i * 2;
                    int telegraph = Projectile.NewProjectile(new EntitySource_WorldEvent(), polterghast.Center, Vector2.Zero, ModContent.ProjectileType<TeleportTelegraph>(), 0, 0f);
                    Main.projectile[telegraph].timeLeft -= lifetimeReduction;

                    telegraph = Projectile.NewProjectile(new EntitySource_WorldEvent(), teleportCenter, Vector2.Zero, ModContent.ProjectileType<TeleportTelegraph>(), 0, 0f);
                    Main.projectile[telegraph].timeLeft -= lifetimeReduction;
                }
            }

            // Teleport the Polterghast to the desired location.
            polterghast.Center = teleportCenter;
            polterghast.netUpdate = true;

            // Teleport the legs as well.
            int legID = ModContent.NPCType<PolterghastLeg>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].type == legID && Main.npc[i].active)
                {
                    Main.npc[i].Center = polterghast.Center + Main.rand.NextVector2Circular(20f, 20f);
                    Main.npc[i].netUpdate = true;
                }
            }
        }

        public static void DoBehavior_LegSwipes(NPC npc, Player target, ref float legToManuallyControlIndex, ref float attackTimer)
        {
            int swingDelay = 96;
            int swipeTime = 75;
            int swipeCount = 7;
            int vortexReleaseRate = 4;
            float hoverSpeed = 26f;
            float swipeArc = 0.89f;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float swipeCounter = ref npc.Infernum().ExtraAI[0];

            if (lifeRatio < Phase2LifeRatio)
                vortexReleaseRate--;
            if (lifeRatio < Phase3LifeRatio)
                swipeTime -= 10;

            // Hover near the target.
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 800f, -225f);
            npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 0.8f);
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Decide the leg to control.
            if (attackTimer == swingDelay)
            {
                // Order legs based on their angular difference with Polterghast's direction to the target.
                // Legs behind Polterghast have a large angular difference while ones in front have a smaller angular difference.
                // This is ideal because you don't want Polterghast to try to somehow swipe at you with a leg that's on the opposite side.
                List<NPC> legsOrderedByPlayerAngleOffset = Main.npc.Take(Main.maxNPCs).Where(n => n.type == ModContent.NPCType<PolterghastLeg>() && n.active).
                    OrderByDescending(l => npc.SafeDirectionTo(target.Center).AngleBetween(l.SafeDirectionTo(npc.Center))).ToList();

                legToManuallyControlIndex = legsOrderedByPlayerAngleOffset[Main.rand.Next(2)].whoAmI;
                return;
            }

            // Make the leg swing.
            NPC legToControl = Main.npc[(int)legToManuallyControlIndex];
            float swingCompletion = (attackTimer - swingDelay) % swipeTime / swipeTime;
            if (legToManuallyControlIndex != 0f)
            {
                float swingAnimationCompletion = PiecewiseAnimation(swingCompletion, Anticipation, Slash, Recovery);
                float legOffsetAngle = (MathHelper.Lerp(-swipeArc, swipeArc, swingAnimationCompletion) + 0.24f) *legToControl.ModNPC<PolterghastLeg>().Direction;
                Vector2 legDirection = npc.SafeDirectionTo(target.Center).RotatedBy(legOffsetAngle);
                Vector2 legDestination = npc.Center + legDirection * (Convert01To010(swingCompletion) * 100f + 350f);

                legToControl.velocity = Vector2.Zero.MoveTowards(legDestination - legToControl.Center, 34f);
            }

            // Release vortices from the leg.
            if (attackTimer >= swingDelay && attackTimer % vortexReleaseRate == vortexReleaseRate - 1f && swingCompletion > 0.2f && swingCompletion < 0.6f)
            {
                SoundEngine.PlaySound(SoundID.Item104, legToControl.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 vortexVelocity = npc.SafeDirectionTo(legToControl.Center) * 3.2f;
                    Utilities.NewProjectileBetter(legToControl.Center + vortexVelocity * 4f, vortexVelocity, ModContent.ProjectileType<GhostlyVortex>(), 280, 0f);
                }
            }

            // Increment the swipe counter.
            if (attackTimer >= swingDelay && (attackTimer - swingDelay) % swipeTime == swipeTime - 1f)
            {
                swipeCounter++;
                if (swipeCounter >= swipeCount)
                {
                    legToManuallyControlIndex = 0f;
                    SelectNextAttack(npc);
                }
            }
        }

        public static void DoBehavior_WispCircleCharges(NPC npc, Player target, ref float attackTimer)
        {
            int hoverTime = 150;
            int chargeCount = 6;
            int slowdownTime = 12;
            int chargeTime = 45;
            int ectoplasmPerRing = 8;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float offsetPerRing = 200f;
            float maxRingOffset = 2000f;
            float chargeSpeed = 33f;
            float spinAngularVelocity = MathHelper.ToRadians(0.75f);

            if (lifeRatio < Phase2LifeRatio)
            {
                maxRingOffset += 300f;
                chargeSpeed += 2.7f;
                spinAngularVelocity *= 1.2f;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                ectoplasmPerRing += 2;
                chargeSpeed += 3.5f;
            }

            ref float chargeCounter = ref npc.Infernum().ExtraAI[0];

            // Create a circle of ectoplasm wisps around Polter on the first frame.
            if (attackTimer == 1f)
            {
                bool clockwise = true;
                for (float radius = 120f; radius < maxRingOffset; radius += offsetPerRing)
                {
                    for (int i = 0; i < ectoplasmPerRing; i++)
                    {
                        int ectoplasm = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<CirclingEctoplasm>(), 300, 0f);
                        if (Main.projectile.IndexInRange(ectoplasm))
                        {
                            Main.projectile[ectoplasm].ModProjectile<CirclingEctoplasm>().OrbitCenter = target.Center - Vector2.UnitY * 200f;
                            Main.projectile[ectoplasm].ModProjectile<CirclingEctoplasm>().OrbitRadius = radius;
                            Main.projectile[ectoplasm].ModProjectile<CirclingEctoplasm>().OrbitAngularVelocity = spinAngularVelocity * clockwise.ToDirectionInt();
                            Main.projectile[ectoplasm].ModProjectile<CirclingEctoplasm>().OrbitOffsetAngle = MathHelper.TwoPi * i / ectoplasmPerRing;
                        }
                    }
                    clockwise = !clockwise;
                }
            }

            // Hover to the top left/right of the target.
            if (attackTimer < hoverTime)
            {
                float flySpeedFactor = Utils.GetLerpValue(0f, hoverTime * 0.55f, attackTimer, true);
                float hoverSpeed = chargeSpeed * flySpeedFactor * 1.5f;
                Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 475f, -175f);
                npc.SimpleFlyMovement(npc.SafeDirectionTo(hoverDestination) * hoverSpeed, 0.55f);
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
                return;
            }

            float wrappedAttackTimer = (attackTimer - hoverTime) % (slowdownTime + chargeTime);

            // Slow down and look at the target.
            if (wrappedAttackTimer <= slowdownTime)
            {
                npc.velocity *= 0.925f;
                npc.rotation = npc.rotation.AngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.15f);
                if (wrappedAttackTimer == slowdownTime)
                {
                    SoundEngine.PlaySound(OmegaBlueHelmet.ActivationSound with { Pitch = -0.525f, Volume = 1.5f }, npc.Center);
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    npc.netUpdate = true;
                }
            }
            else
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Increment the charge counter at the end of charges.
            if (wrappedAttackTimer == slowdownTime + chargeTime - 1f)
            {
                chargeCounter++;
                if (chargeCounter >= chargeCount)
                    SelectNextAttack(npc);
            }
        }

        public static void DoBehavior_AsgoreRingSoulAttack(NPC npc, Player target, ref float totalReleasedSouls, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int ringCount = 7;
            int soulsPerRing = 24;
            int ringReleaseRate = 75;
            int ringCreationDelay = 60;
            float overallRingSpeedFactor = MathHelper.Lerp(1f, 1.84f, 1f - lifeRatio);
            float ringOpeningAngleSpread = MathHelper.ToRadians(41f);
            int actualSoulsPerRing = (int)(soulsPerRing * (MathHelper.TwoPi - ringOpeningAngleSpread) / MathHelper.TwoPi);
            ref float ringShootCounter = ref npc.Infernum().ExtraAI[0];

            // Teleport near the target. A net-update is already fired in the teleport method.
            if (attackTimer == 1f)
            {
                int tries = 0;
                Vector2 teleportPosition;
                do
                {
                    teleportPosition = target.Center + Main.rand.NextVector2CircularEdge(540f, 540f);
                    tries++;
                }
                while (tries < 500 && Collision.SolidCollision(teleportPosition - Vector2.One * 270f, 540, 540));
                TeleportToPosition(npc, teleportPosition, true);
                npc.velocity = Vector2.Zero;
            }

            // Roar and explode into many souls before creating rings.
            if (attackTimer == ringCreationDelay)
            {
                SoundEngine.PlaySound(OmegaBlueHelmet.ActivationSound with { Pitch = -0.525f, Volume = 1.5f }, target.Center);
                for (int i = 0; i < actualSoulsPerRing * ringCount; i++)
                {
                    Vector2 soulVelocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(26f, 40.5f);
                    int soul = Utilities.NewProjectileBetter(npc.Center, soulVelocity, ModContent.ProjectileType<NonReturningSoul>(), 0, 0f);
                    if (Main.projectile.IndexInRange(soul))
                        Main.projectile[soul].ai[0] = Main.rand.Next(2);
                    totalReleasedSouls++;
                }
                npc.netUpdate = true;
            }

            // Cast rings of souls that converge inward on the Polterghast. The player is expected to weave through the open gap.
            // This attack is very similar to the flame circles in Asgore's fight from Undertale.
            if (attackTimer >= ringCreationDelay + 54f && attackTimer % ringReleaseRate == ringReleaseRate - 1f)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    ringShootCounter++;
                    if (ringShootCounter >= ringCount)
                    {
                        Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<SpinningSoul>());
                        SelectNextAttack(npc);
                        return;
                    }

                    bool counterClockwise = Main.rand.NextBool();
                    float ringOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < soulsPerRing; i++)
                    {
                        // Determine the angle of the current soul. This is done by creating an even spread of N points on a circle across 360 degrees.
                        // Angles that are less than a certain threshold are discarded to create an opening in the ring. Following this a random rotation is
                        // applied to allow the opening to be on any point on the resulting ring.
                        float soulAngle = MathHelper.TwoPi * i / soulsPerRing;
                        if (soulAngle < ringOpeningAngleSpread)
                            continue;

                        soulAngle += ringOffsetAngle;
                        int soul = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<SpinningSoul>(), 290, 0f);
                        if (Main.projectile.IndexInRange(soul))
                        {
                            Main.projectile[soul].ai[0] = Main.rand.Next(2);
                            Main.projectile[soul].ai[1] = soulAngle;
                            Main.projectile[soul].localAI[0] = overallRingSpeedFactor;
                            Main.projectile[soul].ModProjectile<SpinningSoul>().CounterclockwiseSpin = counterClockwise;
                            Main.projectile[soul].netUpdate = true;
                        }
                    }
                }

                // Provide the target infinite flight time.
                target.wingTime = target.wingTimeMax;
            }

            // Look at the target.
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Disable contact damage and have a much higher DR than usual.
            npc.damage = 0;
            npc.Calamity().DR = 0.67f;
        }

        public static void DoBehavior_EctoplasmUppercutCharges(NPC npc, Player target, ref float attackTimer, ref float telegraphDirection, ref float telegraphOpacity, ref float veryFirstAttack)
        {
            int descendTime = 75;
            int telegraphTime = 27;
            int chargeTime = 67;
            int chargeCount = 3;
            int ectoplasmReleaseRate = 6;
            float downwardOffset = 1325f;
            float chargeSpeed = 36.5f;
            float lifeRatio = npc.life / (float)npc.lifeMax;

            if (lifeRatio < Phase2LifeRatio)
            {
                chargeTime -= 5;
                chargeSpeed += 4f;
            }
            if (lifeRatio < Phase3LifeRatio)
            {
                telegraphTime -= 2;
                chargeTime -= 5;
                chargeSpeed += 5f;
            }

            ref float horizontalHoverOffset = ref npc.Infernum().ExtraAI[0];
            ref float hasCreatedLight = ref npc.Infernum().ExtraAI[1];
            ref float chargeCounter = ref npc.Infernum().ExtraAI[2];

            // Start from below if this is the very first attack Polter is performing, for cinematic purposes.
            if (veryFirstAttack == 0f)
            {
                attackTimer = descendTime;
                veryFirstAttack = 1f;
                npc.netUpdate = true;
            }

            // Descend downward.
            if (attackTimer <= descendTime)
            {
                npc.damage = 0;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * 39f, 0.08f) * new Vector2(0.967f, 1f);
                if (npc.velocity.Y < 24f)
                    npc.velocity.Y += 2.5f;

                // Fade out as the descent reaches its end.
                npc.Opacity = Utils.GetLerpValue(descendTime, descendTime - 16f, attackTimer, true);
                npc.hide = npc.dontTakeDamage = npc.Opacity < 0.2f;
                return;
            }

            // Project a telegraph line.
            if (attackTimer <= descendTime + telegraphTime)
            {
                // Initialize the horizontal offset. This gives a bit of variance to the charges.
                if (attackTimer == descendTime + 1f)
                {
                    horizontalHoverOffset = Main.rand.NextBool() ? 0f : Main.rand.NextFloatDirection() * 150f;
                    TeleportToPosition(npc, target.Center + new Vector2(horizontalHoverOffset, downwardOffset));
                    npc.velocity = Vector2.Zero;
                    telegraphDirection = npc.AngleTo(target.Center);
                    npc.netUpdate = true;
                }

                // Stay below the target, invisible.
                npc.Opacity = 0f;
                npc.damage = 0;
                npc.dontTakeDamage = true;

                // Aim the telegraph.
                float telegraphCompletion = Utils.GetLerpValue(descendTime, descendTime + telegraphTime, attackTimer, true);
                telegraphOpacity = CalamityUtils.Convert01To010(telegraphCompletion) * 0.67f;
                return;
            }

            // Charge and release ectoplasm.
            if (attackTimer <= descendTime + telegraphTime + chargeTime)
            {
                // Roar and initiate the charge.
                if (attackTimer == descendTime + telegraphTime + 1f)
                {
                    SoundEngine.PlaySound(OmegaBlueHelmet.ActivationSound with { Pitch = -0.525f, Volume = 1.5f }, target.Center);
                    npc.velocity = telegraphDirection.ToRotationVector2() * chargeSpeed;
                    npc.netUpdate = true;
                }

                // Create light if sufficiently close to the target.
                if (Main.netMode != NetmodeID.MultiplayerClient && hasCreatedLight == 0f && npc.WithinRange(target.Center, 1200f))
                {
                    for (int i = 0; i < 7; i++)
                        Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<Light>(), 0, 0f);
                    hasCreatedLight = 1;
                }

                // Release perpendicular ectoplasm.
                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % ectoplasmReleaseRate == ectoplasmReleaseRate - 1f)
                {
                    Vector2 perpendicularDirection = npc.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY);
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 ectoplasmVelocity = perpendicularDirection * i * Main.rand.NextFloat(7.5f, 23f) + Main.rand.NextVector2Circular(1.8f, 1.8f);
                        Utilities.NewProjectileBetter(npc.Center, ectoplasmVelocity, ModContent.ProjectileType<EctoplasmShot>(), 290, 0f);

                        Vector2 fallingEctoplasmVelocity = perpendicularDirection * i * 13f;
                        int fallingEctoplasm = Utilities.NewProjectileBetter(npc.Center, fallingEctoplasmVelocity, ModContent.ProjectileType<EctoplasmShot>(), 290, 0f);
                        if (Main.projectile.IndexInRange(fallingEctoplasm))
                            Main.projectile[fallingEctoplasm].ai[0] = 1f;
                    }
                }
                
                // Rotate and fade back in immediately.
                npc.Opacity = 1f;
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                return;
            }

            attackTimer = 0f;
            hasCreatedLight = 0f;
            chargeCounter++;
            if (chargeCounter >= chargeCount)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_ArcingSouls(NPC npc, Player target, ref float attackTimer)
        {
            float lifeRatio = npc.life / (float)npc.lifeMax;
            int shootDelay = 35;
            int shootRate = 42;
            int shootTime = 240;
            int attackTransitionDelay = 90;
            int soulCount = (int)MathHelper.Lerp(5f, 9f, 1f - lifeRatio);
            float shootSpeed = MathHelper.Lerp(13f, 16f, 1f - lifeRatio);

            if (lifeRatio < Phase2LifeRatio)
                shootRate -= 5;
            if (lifeRatio < Phase3LifeRatio)
                shootRate -= 8;

            // Slow down and look at the target at the beginning.
            if (attackTimer < shootDelay)
                npc.velocity *= 0.95f;

            // Otherwise crawl into a corner and shoot things.
            else
            {
                Vector2 destination = target.Center - Vector2.UnitY * 175f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 450f;
                npc.velocity = (npc.velocity * 9f + npc.SafeDirectionTo(destination) * 20f) / 10f;

                if (attackTimer % shootRate == shootRate - 1f && attackTimer < shootTime)
                {
                    SoundEngine.PlaySound(OmegaBlueHelmet.ActivationSound with { Pitch = -0.525f, Volume = 1.5f }, target.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int direction = -1; direction <= 1; direction += 2)
                        {
                            for (int i = 0; i < soulCount / 2; i++)
                            {
                                float shootOffsetAngle = MathHelper.Lerp(0.35f, 1.47f, i / (float)(soulCount / 2f - 1f)) * direction;
                                float soulAngularVelocity = -shootOffsetAngle * 0.03f;
                                Vector2 soulShootVelocity = npc.SafeDirectionTo(target.Center).RotatedBy(shootOffsetAngle) * shootSpeed;
                                int soul = Utilities.NewProjectileBetter(npc.Center, soulShootVelocity, ModContent.ProjectileType<ArcingSoul>(), 290, 0f);
                                if (Main.projectile.IndexInRange(soul))
                                    Main.projectile[soul].ai[0] = soulAngularVelocity;
                            }
                        }
                    }
                }
            }

            // Look at the target.
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            if (attackTimer >= shootDelay + shootTime + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_SpiritPetal(NPC npc, Player target, ref float attackTimer, ref float totalReleasedSouls, bool enraged)
        {
            int slowdownTime = 60;
            int shootTime = 240;
            int attackDuration = slowdownTime + shootTime;

            // Slow down and look at the target.
            npc.velocity *= 0.97f;
            npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

            // Hover above the player prior to attacking.
            if (attackTimer < slowdownTime - 10f)
            {
                Vector2 destination = target.Center - Vector2.UnitY * 250f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 170f;
                npc.velocity = (npc.velocity * 10f + npc.SafeDirectionTo(destination) * 21.5f) / 11f;
            }

            // Create a light effect at the bottom of the screen.
            if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer == 45f)
                Utilities.NewProjectileBetter(target.Center, Vector2.Zero, ModContent.ProjectileType<Light>(), 0, 0f);

            // Create a petal of released souls.
            int shootRate = enraged ? 4 : 6;
            if (BossRushEvent.BossRushActive)
                shootRate = 3;

            // Release a petal-like dance of souls. They spawn randomized, to make the pattern semi-inconsistent.
            bool attacking = attackTimer >= slowdownTime && attackTimer < attackDuration;
            if (Main.netMode != NetmodeID.MultiplayerClient && attacking && attackTimer % shootRate == shootRate - 1f)
            {
                float offsetAngle = (float)Math.Sin(MathHelper.TwoPi * (attackTimer - 60f) / 128f) * MathHelper.Pi / 3f + Main.rand.NextFloatDirection() * 0.16f;
                Vector2 baseSpawnPosition = npc.Center + npc.SafeDirectionTo(target.Center) * 44f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 leftVelocity = (MathHelper.TwoPi * i / 3f - offsetAngle).ToRotationVector2() * 23.5f;
                    Vector2 rightVelocity = (MathHelper.TwoPi * i / 3f + offsetAngle).ToRotationVector2() * 23.5f;

                    int soul = Utilities.NewProjectileBetter(baseSpawnPosition + leftVelocity * 2f, leftVelocity, ModContent.ProjectileType<NotSpecialSoul>(), 290, 0f);
                    if (Main.projectile.IndexInRange(soul))
                        Main.projectile[soul].ai[0] = 1f;

                    soul = Utilities.NewProjectileBetter(baseSpawnPosition + rightVelocity * 2f, rightVelocity, ModContent.ProjectileType<NotSpecialSoul>(), 290, 0f);
                    if (Main.projectile.IndexInRange(soul))
                        Main.projectile[soul].ai[0] = 1f;
                    totalReleasedSouls += 2f;
                }
            }

            if (totalReleasedSouls > 90f)
                totalReleasedSouls = 90f;

            // Do fade effect.
            if (attackTimer < attackDuration + 60f)
                npc.Opacity = Utils.GetLerpValue(slowdownTime + 45f, slowdownTime, attackTimer, true);
            else
                npc.Opacity = Utils.GetLerpValue(attackDuration + 60f, attackDuration + 100f, attackTimer, true);
            npc.hide = npc.Opacity < 0.25f;
            npc.dontTakeDamage = npc.hide;

            for (int i = 0; i < 15; i++)
            {
                Vector2 spawnOffsetDirection = Main.rand.NextVector2Unit();

                Dust ectoplasm = Dust.NewDustPerfect(npc.Center + spawnOffsetDirection * Main.rand.NextFloat(120f) * npc.scale, 264);
                ectoplasm.velocity = -Vector2.UnitY * MathHelper.Lerp(1f, 2.4f, Utils.GetLerpValue(0f, 100f, npc.Distance(ectoplasm.position), true));
                ectoplasm.color = Color.Lerp(Color.Cyan, Color.Red, Main.rand.NextFloat(0.6f));
                ectoplasm.scale = 1.45f;
                ectoplasm.noLight = true;
                ectoplasm.noGravity = true;
            }

            if (attackTimer % 14f == 13f && attacking)
                SoundEngine.PlaySound(SoundID.NPCHit36, target.Center);

            if (attackTimer >= attackDuration + 135f && totalReleasedSouls <= 15f)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DoVortexCharge(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            int aimTime = 20;
            int slowdownTime = 12;
            int chargeTime = 40;
            float lifeRatio = npc.life / (float)npc.lifeMax;
            float chargeSpeed = MathHelper.Lerp(35f, 43f, 1f - lifeRatio);
            if (BossRushEvent.BossRushActive)
                chargeSpeed *= 1.45f;

            // Aim.
            if (attackTimer < aimTime)
            {
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                Vector2 destination = target.Center - Vector2.UnitY * 300f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 240f;
                npc.velocity = (npc.velocity * 10f + npc.SafeDirectionTo(destination) * chargeSpeed) / 11f;
            }

            // Slow down.
            if (attackTimer > aimTime && attackTimer < aimTime + slowdownTime)
            {
                npc.velocity *= 0.94f;
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;
            }

            // Charge.
            if (attackTimer == aimTime + slowdownTime)
            {
                npc.rotation = npc.AngleTo(target.Center) + MathHelper.PiOver2;

                SoundEngine.PlaySound(OmegaBlueHelmet.ActivationSound with { Pitch = -0.525f, Volume = 1.5f }, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.velocity = npc.SafeDirectionTo(target.Center) * chargeSpeed;
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 vortexVelocity = npc.velocity.RotatedBy(MathHelper.Lerp(-0.21f, 0.21f, i / 2f)).SafeNormalize(Vector2.UnitY) * 11f;
                        Utilities.NewProjectileBetter(npc.Center + vortexVelocity * 4f, vortexVelocity, ModContent.ProjectileType<GhostlyVortex>(), 280, 0f);
                    }
                    npc.netUpdate = true;
                }
            }

            // And release accelerating vortices.
            if (attackTimer >= aimTime + slowdownTime && attackTimer < aimTime + slowdownTime + chargeTime)
            {
                // Accelerate.
                npc.velocity *= 1.005f;

                int shootRate = enraged ? 2 : 3;
                if (lifeRatio > Phase2LifeRatio)
                    shootRate++;

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % shootRate == shootRate - 1f)
                {
                    Vector2 vortexVelocity = npc.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY) * 2.3f;
                    Utilities.NewProjectileBetter(npc.Center + vortexVelocity * 20f, vortexVelocity, ModContent.ProjectileType<GhostlyVortex>(), 280, 0f);
                    vortexVelocity *= -1f;
                    Utilities.NewProjectileBetter(npc.Center + vortexVelocity * 20f, vortexVelocity, ModContent.ProjectileType<GhostlyVortex>(), 280, 0f);
                }
            }

            // Slow down.
            if (attackTimer >= aimTime + slowdownTime + chargeTime)
            {
                npc.rotation = npc.rotation.SimpleAngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.275f);
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.1f) * 0.9f;
            }

            if (attackTimer >= aimTime + slowdownTime + chargeTime + slowdownTime * 2)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_CloneSplit(NPC npc, Player target, ref float attackTimer, bool enraged)
        {
            int totalCharges = 4;
            int cloneCount = 5;
            int splitDelay = 15;
            int hoverTime = 25;
            int chargeTime = 34;
            int postChargeDelay = 20;
            if (attackTimer < 90f + hoverTime + chargeTime + postChargeDelay)
                splitDelay = 90;

            int attackCycleLength = splitDelay + hoverTime + chargeTime + postChargeDelay;
            float chargeSpeed = enraged || BossRushEvent.BossRushActive ? 38f : 32f;
            float adjustedTimer = attackTimer % attackCycleLength;

            int cloneID = ModContent.NPCType<PolterPhantom>();
            IEnumerable<int> polterghasts = Main.npc.Take(Main.maxNPCs).
                Where(n => (n.type == npc.type || n.type == cloneID) && n.active).
                Select(n => n.whoAmI);

            if (adjustedTimer < splitDelay + hoverTime && !npc.WithinRange(target.Center, 300f))
            {
                Vector2 destination = target.Center - Vector2.UnitY * 300f;
                destination.X += (target.Center.X < npc.Center.X).ToDirectionInt() * 240f;

                npc.velocity = (npc.velocity * 15f + npc.SafeDirectionTo(destination) * 18f) / 16f;
            }

            if (adjustedTimer == splitDelay)
            {
                // Summon three new clones.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < cloneCount; i++)
                    {
                        int clone = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X - 1, (int)npc.Center.Y, cloneID);

                        // An NPC must update once for it to recieve a whoAmI variable.
                        // Without this, the below IEnumerable collection would not incorporate this NPC.
                        // Yes, this is dumb.
                        Main.npc[clone].UpdateNPC(clone);
                    }
                }

                polterghasts = Main.npc.Take(Main.maxNPCs).
                    Where(n => n.type == cloneID && n.active).
                    Select(n => n.whoAmI);

                // Teleport around the player.
                Vector2 originalPosition = npc.Center;
                for (int i = 0; i < polterghasts.Count(); i++)
                {
                    Vector2 newPosition = originalPosition - Vector2.UnitY.RotatedBy(MathHelper.TwoPi * i / polterghasts.Count()) * 540f;
                    while (target.WithinRange(newPosition, 380f))
                        newPosition.Y += 10f;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Main.npc[polterghasts.ElementAt(i)].Center = newPosition;
                        Main.npc[polterghasts.ElementAt(i)].netUpdate = true;
                    }
                }
                SoundEngine.PlaySound(OmegaBlueHelmet.ActivationSound with { Pitch = -0.525f, Volume = 1.5f }, target.Center);
            }

            if (adjustedTimer > splitDelay + hoverTime && adjustedTimer < splitDelay + hoverTime + chargeTime)
            {
                npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;
                npc.velocity *= 1.0145f;
            }
            else
            {
                npc.rotation = npc.rotation.SimpleAngleTowards(npc.AngleTo(target.Center) + MathHelper.PiOver2, 0.325f);
                if (adjustedTimer > splitDelay + hoverTime)
                    npc.velocity *= 0.97f;
            }

            // Charge.
            if (Main.netMode != NetmodeID.MultiplayerClient && adjustedTimer == splitDelay + hoverTime)
            {
                for (int i = 0; i < polterghasts.Count(); i++)
                {
                    Main.npc[polterghasts.ElementAt(i)].velocity = Main.npc[polterghasts.ElementAt(i)].SafeDirectionTo(target.Center) * chargeSpeed;
                    Main.npc[polterghasts.ElementAt(i)].netUpdate = true;
                }
            }

            if (attackTimer >= totalCharges * attackCycleLength)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_DesperationAttack(NPC npc, Player target, ref float attackTimer, ref float vignetteInterpolant, ref float radiusDecreaseFactor)
        {
            int vignetteFadeinTime = 96;
            int vortexSpiralCount = 3;
            int vortexSpiralSpawnRate = 24;
            int vortexSpiralTime = 420;

            int spiritFlameTime = 420;
            int soulsPerRing = 24;
            int ringReleaseRate = 60;
            float overallRingSpeedFactor = 1.75f;
            float ringOpeningAngleSpread = MathHelper.ToRadians(48f);

            int soulBurstCount = 33;

            ref float soulBurstDelay = ref npc.Infernum().ExtraAI[0];
            ref float soulBurstCounter = ref npc.Infernum().ExtraAI[1];

            // Initialize the soul burst delay.
            if (soulBurstDelay == 0f)
                soulBurstDelay = 27f;

            // Remain invisible and invincible.
            npc.dontTakeDamage = true;
            npc.Opacity = 0f;
            npc.damage = 0;

            // Provide the target infinite flight time.
            target.wingTime = target.wingTimeMax;

            // Drift towards the target.
            npc.Center = npc.Center.MoveTowards(target.Center, 4f);

            vignetteInterpolant = Utils.GetLerpValue(0f, vignetteFadeinTime, attackTimer, true);

            if (attackTimer < vignetteFadeinTime)
                return;

            // Release spirals of vortices from outside inward towards the player.
            if (attackTimer < vignetteFadeinTime + vortexSpiralTime)
            {
                if (attackTimer % vortexSpiralSpawnRate == 0f)
                {
                    float spiralAngle = (attackTimer - vignetteFadeinTime) / vortexSpiralTime * MathHelper.Pi * 4f;
                    for (int i = 0; i < vortexSpiralCount; i++)
                    {
                        Vector2 spiralSpawnOffset = (MathHelper.TwoPi * i / vortexSpiralCount + spiralAngle).ToRotationVector2() * 560f;
                        Vector2 spiralVelocity = -spiralSpawnOffset.SafeNormalize(Vector2.UnitY) * 3f;
                        Utilities.NewProjectileBetter(target.Center + spiralSpawnOffset, spiralVelocity, ModContent.ProjectileType<GhostlyVortex>(), 300, 0f);
                    }
                }
                return;
            }

            // Perform a super-fast version of the Asgore flame attack.
            if (attackTimer < vignetteFadeinTime + vortexSpiralTime + spiritFlameTime)
            {
                if (attackTimer == vignetteFadeinTime + vortexSpiralTime + 1f)
                    Utilities.DeleteAllProjectiles(false, ModContent.ProjectileType<GhostlyVortex>());

                if (Main.netMode != NetmodeID.MultiplayerClient && attackTimer % ringReleaseRate == ringReleaseRate - 1f)
                {
                    bool counterClockwise = Main.rand.NextBool();
                    float ringOffsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    for (int i = 0; i < soulsPerRing; i++)
                    {
                        // Determine the angle of the current soul. This is done by creating an even spread of N points on a circle across 360 degrees.
                        // Angles that are less than a certain threshold are discarded to create an opening in the ring. Following this a random rotation is
                        // applied to allow the opening to be on any point on the resulting ring.
                        float soulAngle = MathHelper.TwoPi * i / soulsPerRing;
                        if (soulAngle < ringOpeningAngleSpread)
                            continue;

                        soulAngle += ringOffsetAngle;
                        int soul = Utilities.NewProjectileBetter(npc.Center, Vector2.Zero, ModContent.ProjectileType<SpinningSoul>(), 300, 0f);
                        if (Main.projectile.IndexInRange(soul))
                        {
                            Main.projectile[soul].ai[0] = Main.rand.Next(2);
                            Main.projectile[soul].ai[1] = soulAngle;
                            Main.projectile[soul].localAI[0] = overallRingSpeedFactor;
                            Main.projectile[soul].ModProjectile<SpinningSoul>().CounterclockwiseSpin = counterClockwise;
                            Main.projectile[soul].netUpdate = true;
                        }
                    }
                }
                return;
            }

            // Delete leftover flames.
            Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<SpinningSoul>());

            // Release barrages of souls at an accelerating pace that are telegraphed by large lines.
            int startingTime = vignetteFadeinTime + vortexSpiralTime + spiritFlameTime;
            if (attackTimer >= startingTime + soulBurstDelay && soulBurstCounter < soulBurstCount)
            {
                soulBurstDelay = MathHelper.Clamp(soulBurstDelay - 1f, 10f, 36f);
                attackTimer = startingTime;
                soulBurstCounter++;

                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 soulTelegraphSpawnPosition = target.Center + Main.rand.NextVector2CircularEdge(1180f, 1180f);
                    Vector2 soulTelegraphDirection = (target.Center - soulTelegraphSpawnPosition).SafeNormalize(Vector2.UnitY);
                    Utilities.NewProjectileBetter(soulTelegraphSpawnPosition, soulTelegraphDirection, ModContent.ProjectileType<SoulTelegraphLine>(), 0, 0f);
                }

                npc.netUpdate = true;
            }

            // Make the radius fade inward.
            if (attackTimer >= startingTime + soulBurstDelay + 120f)
            {
                radiusDecreaseFactor = MathHelper.Lerp(radiusDecreaseFactor, 0f, 0.2f);
                vignetteInterpolant = Utils.GetLerpValue(startingTime + soulBurstDelay + 240f, startingTime + soulBurstDelay + 120f, attackTimer, true);
                if (attackTimer == startingTime + soulBurstDelay + 120f)
                {
                    SoundEngine.PlaySound(ScorchedEarth.ShootSound with { Pitch = -0.27f, Volume = 1.5f }, target.Center);
                    npc.NPCLoot();
                }

                if (attackTimer >= startingTime + soulBurstDelay + 240f)
                    npc.active = false;
            }
            else
                radiusDecreaseFactor = MathHelper.Lerp(radiusDecreaseFactor, soulBurstCounter / soulBurstCount * 0.85f, 0.1f);
        }

        #endregion AI

        #region Frames and Drawcode

        public static float TelegraphWidthFunction(NPC _, float _2) => 80f;

        public static Color TelegraphColorFunction(NPC npc, float completionRatio)
        {
            float endFadeOpacity = Utils.GetLerpValue(0f, 0.15f, completionRatio, true) * Utils.GetLerpValue(1f, 0.8f, completionRatio, true);
            return Color.LightCyan * endFadeOpacity * npc.localAI[1] * 0.4f;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            // Initialize the telegraph primitive drawer.
            npc.Infernum().OptionalPrimitiveDrawer ??= new(c => TelegraphWidthFunction(npc, c), c => TelegraphColorFunction(npc, c), null, true, GameShaders.Misc["Infernum:SideStreak"]);

            bool inPhase3 = npc.life < npc.lifeMax * Phase3LifeRatio;
            bool enraged = npc.ai[3] == 1f;
            Vector2 baseDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;
            Texture2D polterTexture = TextureAssets.Npc[npc.type].Value;
            Texture2D polterGlowmaskEctoplasm = ModContent.Request<Texture2D>("CalamityMod/NPCs/Polterghast/PolterghastGlow").Value;
            Texture2D polterGlowmaskHeart = ModContent.Request<Texture2D>("CalamityMod/NPCs/Polterghast/PolterghastGlow2").Value;

            void drawInstance(Vector2 position, Color color)
            {
                Main.spriteBatch.Draw(polterTexture, position, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(polterGlowmaskHeart, position, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(polterGlowmaskEctoplasm, position, npc.frame, color, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);
            }

            // Draw the telegraph line as needed.
            Vector2 telegraphDirection = npc.localAI[2].ToRotationVector2();
            Vector2 telegraphStart = npc.Center;
            Vector2 telegraphEnd = npc.Center + telegraphDirection * 5000f;
            Vector2[] telegraphPoints = new Vector2[]
            {
                telegraphStart,
                (telegraphStart + telegraphEnd) * 0.5f,
                telegraphEnd
            };
            npc.Infernum().OptionalPrimitiveDrawer.Draw(telegraphPoints, -Main.screenPosition, 72);

            if (inPhase3 || enraged)
            {
                Main.spriteBatch.SetBlendState(BlendState.Additive);

                Color baseColor = Color.White;
                float drawOffsetFactor = MathHelper.Lerp(6.5f, 8.5f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 2.7f) * 0.5f + 0.5f) * npc.scale * npc.Opacity;
                float fadeFactor = 0.225f;
                if (enraged)
                {
                    drawOffsetFactor = MathHelper.Lerp(7f, 9.75f, (float)Math.Cos(Main.GlobalTimeWrappedHourly * 4.3f) * 0.5f + 0.5f) * npc.scale * npc.Opacity;
                    baseColor = Color.Red;
                    fadeFactor = 0.3f;
                }

                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 1.9f).ToRotationVector2() * drawOffsetFactor;
                    drawInstance(baseDrawPosition + drawOffset, npc.GetAlpha(baseColor) * fadeFactor);
                }
                Main.spriteBatch.ResetBlendState();
            }

            drawInstance(baseDrawPosition, npc.GetAlpha(Color.White));

            Texture2D blackCircle = TextureAssets.MagicPixel.Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;

            // Draw the circle.
            float circleRadius = MathHelper.Lerp(6000f, 600f, npc.Infernum().ExtraAI[12]) * (1f - npc.Infernum().ExtraAI[13]);
            Vector2 circleScale = new Vector2(MathHelper.Max(Main.screenWidth, Main.screenHeight)) * 5f;

            if (npc.Infernum().ExtraAI[12] > 0.1f)
            {
                Main.spriteBatch.EnterShaderRegion();

                GameShaders.Misc["Infernum:CircleCutout2"].Shader.Parameters["uImageSize0"].SetValue(circleScale);
                GameShaders.Misc["Infernum:CircleCutout2"].Shader.Parameters["uCircleRadius"].SetValue(circleRadius * 1.414f);
                GameShaders.Misc["Infernum:CircleCutout2"].SetShaderTexture(ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PolterghastLayer"));
                GameShaders.Misc["Infernum:CircleCutout2"].Apply();
                Main.spriteBatch.Draw(blackCircle, drawPosition, null, Color.Black, 0f, blackCircle.Size() * 0.5f, circleScale / blackCircle.Size(), 0, 0f);
                Main.spriteBatch.ExitShaderRegion();
            }
            return false;
        }

        public override void FindFrame(NPC npc, int frameHeight)
        {
            npc.frameCounter++;
            if (npc.frameCounter % 7f == 6f)
                npc.frame.Y += frameHeight;

            int minFrame = 0;
            int maxFrame = 3;

            if (npc.life / (float)npc.lifeMax < Phase2LifeRatio)
            {
                minFrame = 4;
                maxFrame = 7;
            }
            if (npc.life / (float)npc.lifeMax < Phase3LifeRatio)
            {
                minFrame = 8;
                maxFrame = 11;
            }

            if (npc.frame.Y < frameHeight * minFrame)
                npc.frame.Y = frameHeight * minFrame;
            if (npc.frame.Y > frameHeight * maxFrame)
                npc.frame.Y = frameHeight * minFrame;
        }
        #endregion Frames and Drawcode
    }
}
