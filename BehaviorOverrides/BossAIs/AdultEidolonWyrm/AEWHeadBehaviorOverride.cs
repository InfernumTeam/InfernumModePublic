using CalamityMod;
using CalamityMod.NPCs.AdultEidolonWyrm;
using InfernumMode.BehaviorOverrides.AbyssAIs;
using InfernumMode.OverridingSystem;
using InfernumMode.Projectiles;
using InfernumMode.Sounds;
using InfernumMode.Systems;
using InfernumMode.WorldGeneration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.AdultEidolonWyrm
{
    public class AEWHeadBehaviorOverride : NPCBehaviorOverride
    {
        public enum AEWAttackType
        {
            // Spawn animation states.
            SnatchTerminus,
            ThreateninglyHoverNearPlayer,

            // Light attacks.
            BurningGaze,

            // Neutral attacks.
            SplitFormCharges,
        }

        public override int NPCOverrideType => ModContent.NPCType<AdultEidolonWyrmHead>();

        #region AI
        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio,
            Phase4LifeRatio,
            Phase5LifeRatio
        };

        // Projectile damage values.
        public const int NormalShotDamage = 540;

        public const int StrongerNormalShotDamage = 560;

        public const int PowerfulShotDamage = 850;

        public const float Phase2LifeRatio = 0.8f;

        public const float Phase3LifeRatio = 0.6f;

        public const float Phase4LifeRatio = 0.35f;

        public const float Phase5LifeRatio = 0.1f;

        public const int EyeGlowOpacityIndex = 5;

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();

            float lifeRatio = npc.life / (float)npc.lifeMax;
            ref float attackType = ref npc.ai[0];
            ref float attackTimer = ref npc.ai[1];
            ref float initializedFlag = ref npc.ai[2];
            ref float eyeGlowOpacity = ref npc.Infernum().ExtraAI[EyeGlowOpacityIndex];

            if (Main.netMode != NetmodeID.MultiplayerClient && initializedFlag == 0f)
            {
                CreateSegments(npc, 125, ModContent.NPCType<AdultEidolonWyrmBody>(), ModContent.NPCType<AdultEidolonWyrmBodyAlt>(), ModContent.NPCType<AdultEidolonWyrmTail>());
                initializedFlag = 1f;
                npc.netUpdate = true;
            }
            
            // If there still was no valid target, swim away.
            if (npc.target < 0 || npc.target >= 255 || Main.player[npc.target].dead || !Main.player[npc.target].active)
            {
                DoBehavior_Despawn(npc);
                return false;
            }

            Player target = Main.player[npc.target];

            // Disable obnoxious water mechanics so that the player can fight the boss without interruption.
            target.breath = target.breathMax;
            target.ignoreWater = true;
            target.wingTime = target.wingTimeMax;
            AbyssWaterColorSystem.WaterBlacknessInterpolant = 0f;

            // This is necessary to allow the boss effects buff to be shown.
            npc.Calamity().KillTime = 1;

            // Why are you despawning?
            npc.boss = true;
            npc.timeLeft = 7200;

            switch ((AEWAttackType)attackType)
            {
                case AEWAttackType.SnatchTerminus:
                    DoBehavior_SnatchTerminus(npc);
                    break;
                case AEWAttackType.ThreateninglyHoverNearPlayer:
                    DoBehavior_ThreateninglyHoverNearPlayer(npc, target, ref eyeGlowOpacity, ref attackTimer);
                    break;
                case AEWAttackType.BurningGaze:
                    DoBehavior_BurningGaze(npc, target, ref attackTimer);
                    break;
                case AEWAttackType.SplitFormCharges:
                    DoBehavior_SplitFormCharges(npc, target, ref attackTimer);
                    break;
            }

            // Determine rotation based on the current velocity.
            npc.rotation = npc.velocity.ToRotation() + MathHelper.PiOver2;

            // Increment the attack timer.
            attackTimer++;

            return false;
        }
        #endregion AI

        #region Specific Behaviors

        public static void DoBehavior_Despawn(NPC npc)
        {
            npc.velocity.X *= 0.985f;
            if (npc.velocity.Y < 33f)
                npc.velocity.Y += 0.6f;

            if (npc.timeLeft > 210)
                npc.timeLeft = 210;
        }

        public static void DoBehavior_SnatchTerminus(NPC npc)
        {
            float chargeSpeed = 41f;
            List<Projectile> terminusInstances = Utilities.AllProjectilesByID(ModContent.ProjectileType<TerminusAnimationProj>()).ToList();

            // Fade in.
            npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.08f, 0f, 1f);

            // Transition to the next attack if there are no more Terminus instances.
            if (terminusInstances.Count <= 0)
            {
                SelectNextAttack(npc);
                return;
            }

            Projectile target = terminusInstances.First();

            // Fly very, very quickly towards the Terminus.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * chargeSpeed, 0.16f);

            // Delete the Terminus instance if it's being touched.
            // On the next frame the AEW will transition to the next attack, assuming there isn't another Terminus instance for some weird reason.
            if (npc.WithinRange(target.Center, 90f))
            {
                SoundEngine.PlaySound(SoundID.DD2_SkyDragonsFuryShot with { Volume = 1.3f }, target.Center);
                target.Kill();
            }
        }

        public static void DoBehavior_ThreateninglyHoverNearPlayer(NPC npc, Player target, ref float eyeGlowOpacity, ref float attackTimer)
        {
            int roarDelay = 60;
            int eyeGlowFadeinTime = 105;
            int attackTransitionDelay = 210;
            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < npc.Center.X).ToDirectionInt() * 450f, -360f);
            ref float hasReachedDestination = ref npc.Infernum().ExtraAI[0];

            // Attempt to hover to the top left/right of the target at first.
            if (hasReachedDestination == 0f)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(hoverDestination) * 32f, 0.084f);
                if (npc.WithinRange(hoverDestination, 96f))
                {
                    hasReachedDestination = 1f;
                    npc.netUpdate = true;
                }

                // Don't let the attack timer increment.
                attackTimer = -1f;

                return;
            }

            // Roar after a short delay.
            if (attackTimer == roarDelay)
                SoundEngine.PlaySound(InfernumSoundRegistry.AEWThreatenRoar);

            // Slow down and look at the target threateningly before attacking.
            npc.velocity = Vector2.Lerp(npc.velocity, npc.SafeDirectionTo(target.Center) * 3f, 0.071f);

            // Make the eye glowmask gradually fade in.
            eyeGlowOpacity = Utils.GetLerpValue(0f, eyeGlowFadeinTime, attackTimer, true);

            if (attackTimer >= roarDelay + attackTransitionDelay)
                SelectNextAttack(npc);
        }

        public static void DoBehavior_BurningGaze(NPC npc, Player target, ref float attackTimer)
        {
            
        }

        public static void DoBehavior_SplitFormCharges(NPC npc, Player target, ref float attackTimer)
        {
            int swimTime = 105;
            int telegraphTime = 42;
            int chargeTime = 45;
            int chargeCount = 5;
            int attackCycleTime = telegraphTime + chargeTime;
            int chargeCounter = (int)(attackTimer - swimTime) / attackCycleTime;
            float attackCycleTimer = (attackTimer - swimTime) % attackCycleTime;
            bool shouldStopAttacking = chargeCounter >= chargeCount && !Utilities.AnyProjectiles(ModContent.ProjectileType<AEWSplitForm>());
            ref float verticalSwimDirection = ref npc.Infernum().ExtraAI[0];

            // Don't let the attack cycle timer increment if still swimming.
            if (attackTimer < swimTime)
                attackCycleTimer = 0f;

            // Swim away from the target. If they're close to the bottom of the abyss, swim up. Otherwise, swim down.
            if (verticalSwimDirection == 0f)
            {
                verticalSwimDirection = 1f;
                if (target.Center.Y >= CustomAbyss.AbyssBottom * 16f - 2400f)
                    verticalSwimDirection = -1f;
                
                npc.netUpdate = true;
            }
            else if (!shouldStopAttacking)
            {
                npc.velocity = Vector2.Lerp(npc.velocity, Vector2.UnitY * verticalSwimDirection * 105f, 0.1f);

                // Fade out after enough time has passed, in anticipation of the attack.
                npc.Opacity = Utils.GetLerpValue(swimTime - 1f, swimTime - 35f, attackTimer, true);
            }

            // Stay below the target once completely invisible.
            if (npc.Opacity <= 0f)
            {
                npc.Center = target.Center + Vector2.UnitY * verticalSwimDirection * 1600f;
                npc.velocity = -Vector2.UnitY * verticalSwimDirection * 23f;
            }

            // Fade back in if ready to transition to the next attack.
            if (shouldStopAttacking)
            {
                npc.Opacity = MathHelper.Clamp(npc.Opacity + 0.05f, 0f, 1f);
                if (npc.Opacity >= 1f)
                {
                    Utilities.DeleteAllProjectiles(true, ModContent.ProjectileType<EidolistIce>());
                    SelectNextAttack(npc);
                }
            }

            // Cast telegraph direction lines. Once they dissipate the split forms will appear and charge.
            if (attackCycleTimer == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item158, target.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int crossLineID = ModContent.ProjectileType<AEWTelegraphLine>();
                    bool firstCrossIsDark = Main.rand.NextBool();
                    float crossSpawnOffset = 2080f;
                    bool flipHorizontalDirection = target.Center.X < Main.maxTilesX * 16f - 3000f;
                    float directionX = flipHorizontalDirection.ToDirectionInt();
                    switch (chargeCounter % 2)
                    {
                        // Plus-shaped cross.
                        case 0:
                            Utilities.NewProjectileBetter(target.Center + Vector2.UnitX * directionX * crossSpawnOffset, -Vector2.UnitX * directionX, crossLineID, 0, 0f, -1, firstCrossIsDark.ToInt(), telegraphTime);
                            Utilities.NewProjectileBetter(target.Center + Vector2.UnitY * crossSpawnOffset, -Vector2.UnitY, crossLineID, 0, 0f, -1, 1f - firstCrossIsDark.ToInt(), telegraphTime);
                            break;

                        // X-shaped cross.
                        case 1:
                            Utilities.NewProjectileBetter(target.Center + new Vector2(directionX, -1f) * crossSpawnOffset * 0.707f, new(-directionX, 1f), crossLineID, 0, 0f, -1, firstCrossIsDark.ToInt(), telegraphTime);
                            Utilities.NewProjectileBetter(target.Center + new Vector2(directionX, 1f) * crossSpawnOffset * 0.707f, new(-directionX, -1f), crossLineID, 0, 0f, -1, 1f - firstCrossIsDark.ToInt(), telegraphTime);
                            break;
                    }
                }
            }
        }

        #endregion Specific Behaviors

        #region AI Utility Methods
        public static void CreateSegments(NPC npc, int wormLength, int bodyType1, int bodyType2, int tailType)
        {
            int previousIndex = npc.whoAmI;
            for (int i = 0; i < wormLength; i++)
            {
                int nextIndex;
                if (i < wormLength - 1)
                {
                    int bodyID = i % 2 == 0 ? bodyType1 : bodyType2;
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, bodyID, npc.whoAmI + 1);
                }
                else
                    nextIndex = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, tailType, npc.whoAmI + 1);

                Main.npc[nextIndex].realLife = npc.whoAmI;
                Main.npc[nextIndex].ai[2] = npc.whoAmI;
                Main.npc[nextIndex].ai[1] = previousIndex;

                if (i >= 1)
                    Main.npc[previousIndex].ai[0] = nextIndex;

                // Force sync the new segment into existence.
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, nextIndex, 0f, 0f, 0f, 0);

                previousIndex = nextIndex;
            }
        }

        internal static void SelectNextAttack(NPC npc)
        {
            AEWAttackType currentAttack = (AEWAttackType)npc.ai[0];
            AEWAttackType nextAttack = currentAttack;

            if (currentAttack == AEWAttackType.SnatchTerminus)
                nextAttack = AEWAttackType.ThreateninglyHoverNearPlayer;
            else if (currentAttack == AEWAttackType.ThreateninglyHoverNearPlayer)
                nextAttack = AEWAttackType.SplitFormCharges;
            else if (currentAttack == AEWAttackType.SplitFormCharges)
                nextAttack = AEWAttackType.BurningGaze;

            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;
            npc.ai[0] = (int)nextAttack;
            npc.ai[1] = 0f;
            npc.netUpdate = true;
        }

        #endregion AI Utility Methods

        #region Draw Effects
        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            npc.frame = new(0, 0, 254, 138);

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Texture2D glowmaskTexture = ModContent.Request<Texture2D>("CalamityMod/NPCs/AdultEidolonWyrm/AdultEidolonWyrmHeadGlow").Value;
            Texture2D eyeTexture = ModContent.Request<Texture2D>("InfernumMode/BehaviorOverrides/BossAIs/AdultEidolonWyrm/AEWEyes").Value;
            Vector2 drawPosition = npc.Center - Main.screenPosition;
            Color eyeColor = Color.Cyan * npc.Opacity * npc.Infernum().ExtraAI[EyeGlowOpacityIndex];

            Main.EntitySpriteDraw(texture, drawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0);
            Main.EntitySpriteDraw(glowmaskTexture, drawPosition, npc.frame, npc.GetAlpha(Color.White), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0);
            ScreenSaturationBlurSystem.ThingsToDrawOnTopOfBlur.Add(new(eyeTexture, drawPosition, npc.frame, eyeColor, npc.rotation, npc.frame.Size() * 0.5f, npc.scale, 0, 0));

            // Hacky way of ensuring that PostDraw doesn't do anything.
            npc.frame = Rectangle.Empty;

            return false;
        }
        #endregion Draw Effects

        #region Tips

        #endregion Tips
    }
}
