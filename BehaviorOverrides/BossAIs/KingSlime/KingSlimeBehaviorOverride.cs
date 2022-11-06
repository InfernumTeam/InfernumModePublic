using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Events;
using CalamityMod.NPCs.NormalNPCs;
using InfernumMode.Miscellaneous;
using InfernumMode.OverridingSystem;
using InfernumMode.Projectiles;
using InfernumMode.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace InfernumMode.BehaviorOverrides.BossAIs.KingSlime
{
    public class KingSlimeBehaviorOverride : NPCBehaviorOverride
    {
        public override int NPCOverrideType => NPCID.KingSlime;

        public override NPCOverrideContext ContentToOverride => NPCOverrideContext.NPCAI | NPCOverrideContext.NPCPreDraw;

        #region Enumerations
        public enum KingSlimeAttackType
        {
            SmallJump,
            LargeJump,
            SlamJump,
            Teleport,
        }
        #endregion

        #region AI

        public static readonly KingSlimeAttackType[] AttackPattern = new KingSlimeAttackType[]
        {
            KingSlimeAttackType.SmallJump,
            KingSlimeAttackType.SmallJump,
            KingSlimeAttackType.LargeJump,
            KingSlimeAttackType.Teleport,
            KingSlimeAttackType.LargeJump,
        };

        public const float Phase2LifeRatio = 0.75f;

        public const float Phase3LifeRatio = 0.3f;

        public const float DespawnDistance = 4700f;

        public const float MaxScale = 3f;

        public const float MinScale = 1.85f;

        public static readonly Vector2 HitboxScaleFactor = new(108f, 88f);

        public override float[] PhaseLifeRatioThresholds => new float[]
        {
            Phase2LifeRatio,
            Phase3LifeRatio
        };

        public override bool PreAI(NPC npc)
        {
            // Select a new target if an old one was lost.
            npc.TargetClosestIfTargetIsInvalid();
            Player target = Main.player[npc.target];
            npc.direction = (target.Center.X > npc.Center.X).ToDirectionInt();
            npc.damage = npc.defDamage;
            npc.dontTakeDamage = false;

            ref float attackTimer = ref npc.ai[2];
            ref float hasSummonedNinjaFlag = ref npc.localAI[0];
            ref float jewelSummonTimer = ref npc.localAI[1];
            ref float teleportDirection = ref npc.Infernum().ExtraAI[5];
            ref float deathTimer = ref npc.Infernum().ExtraAI[6];

            float lifeRatio = npc.life / (float)npc.lifeMax;

            // Constantly give the target Weak Pertrification in boss rush.
            if (Main.netMode != NetmodeID.Server && BossRushEvent.BossRushActive)
            {
                if (!target.dead && target.active)
                    target.AddBuff(ModContent.BuffType<WeakPetrification>(), 15);
            }

            // Despawn if the target is gone or too far away.
            if (!Main.player[npc.target].active || Main.player[npc.target].dead || !npc.WithinRange(Main.player[npc.target].Center, DespawnDistance))
            {
                npc.TargetClosest();
                if (!Main.player[npc.target].active || Main.player[npc.target].dead)
                {
                    DoBehavior_Despawn(npc);
                    return false;
                }
            }
            else
                npc.timeLeft = 3600;

            float oldScale = npc.scale;
            float idealScale = MathHelper.Lerp(MaxScale, MinScale, 1f - lifeRatio);
            npc.scale = idealScale;

            if (npc.localAI[2] == 0f)
            {
                npc.timeLeft = 3600;
                npc.localAI[2] = 1f;
            }

            if (npc.life < npc.lifeMax * Phase3LifeRatio && hasSummonedNinjaFlag == 0f)
            {
                HatGirl.SayThingWhileOwnerIsAlive(target, "The ninja shoots more shurikens the farther you are, so don't go too far!");
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Ninja>());
                    hasSummonedNinjaFlag = 1f;
                    npc.netUpdate = true;
                }
            }

            // Summon the jewel for the first time when King Slime enters the first phase. This waits until King Slime isn't teleporting to happen.
            if (npc.life < npc.lifeMax * Phase2LifeRatio && jewelSummonTimer == 0f && npc.scale >= 0.8f)
            {
                Vector2 jewelSpawnPosition = target.Center - Vector2.UnitY * 350f;
                SoundEngine.PlaySound(SoundID.Item67, target.Center);
                Dust.QuickDustLine(npc.Top + Vector2.UnitY * 60f, jewelSpawnPosition, 150f, Color.Red);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)jewelSpawnPosition.X, (int)jewelSpawnPosition.Y, ModContent.NPCType<KingSlimeJewel>());
                jewelSummonTimer = 1f;
                npc.netUpdate = true;
            }

            // Resummon the jewel if it's gone and enough time has passed.
            if (!NPC.AnyNPCs(ModContent.NPCType<KingSlimeJewel>()) && jewelSummonTimer >= 1f)
            {
                jewelSummonTimer++;
                if (jewelSummonTimer >= 2100f)
                {
                    jewelSummonTimer = 0f;
                    npc.netUpdate = true;
                }
            }

            // Enforce slightly stronger gravity.
            if (npc.velocity.Y > 0f)
            {
                npc.velocity.Y += MathHelper.Lerp(0.05f, 0.25f, 1f - lifeRatio);
                if (BossRushEvent.BossRushActive && npc.velocity.Y > 4f)
                    npc.position.Y += 4f;
            }

            if(deathTimer > 0)
            {
                DoBehavior_DeathAnimation(npc, target, ref deathTimer);
                deathTimer++;
                return false;
            }

            switch ((KingSlimeAttackType)(int)npc.ai[1])
            {
                case KingSlimeAttackType.SmallJump:
                    DoBehavior_SmallJump(npc, ref target, ref attackTimer);
                    break;
                case KingSlimeAttackType.LargeJump:
                    DoBehavior_LargeJump(npc, ref target, ref attackTimer);
                    break;
                case KingSlimeAttackType.Teleport:
                    DoBehavior_Teleport(npc, target, idealScale, ref attackTimer, ref teleportDirection);
                    break;
            }

            // Update the hitbox based on the current scale if it changed.
            if (oldScale != npc.scale)
            {
                npc.position = npc.Center;
                npc.Size = HitboxScaleFactor * npc.scale;
                npc.Center = npc.position;
            }

            if (npc.Opacity > 0.7f)
                npc.Opacity = 0.7f;

            npc.gfxOffY = (int)(npc.scale * -14f);

            attackTimer++;
            return false;
        }

        public static bool HandleDeathEffects(NPC npc)
        {
            npc.Infernum().ExtraAI[6] = 1;
            npc.life = 1;
            npc.dontTakeDamage = true;
            npc.active = true;
            npc.netUpdate = true;
            return false;
        }

        public static void DoBehavior_DeathAnimation(NPC npc, Player target, ref float deathTimer)
        {
            int deathAnimationLength = 150;

            // Constantly get the ninja.
            NPC ninjaNPC = null;
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i].type == ModContent.NPCType<Ninja>())
                {
                    ninjaNPC = Main.npc[i];
                    goto BreakLoop;
                }
            }
            BreakLoop:

            if (deathTimer == 1)
            {
                // Despawn the jewel
                for(int i = 0; i < Main.npc.Length; i++)
                {
                    if (Main.npc[i].type == ModContent.NPCType<KingSlimeJewel>())
                    {
                        Main.npc[i].active = false;
                        goto BreakNPCLoop;
                    }
                }
                BreakNPCLoop:
                // If the ninja doesnt exist, spawn it!
                if (ninjaNPC is null)
                {
                    NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<Ninja>());
                    npc.netUpdate = true;
                    return;
                }

                // Set the ninjas synced death timer, allowing them to sync with us when needed.
                 ninjaNPC.Infernum().ExtraAI[7] = 1;
            }

            // Don't do or take damage.
            npc.damage = 0;
            npc.dontTakeDamage = true;

            // Make the camera focus on King Slime.
            if (Main.LocalPlayer.WithinRange(Main.LocalPlayer.Center, 3700f))
            {
                Main.LocalPlayer.Infernum().ScreenFocusPosition = npc.Center;
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant = Utils.GetLerpValue(0f, 15f, deathTimer, true);
                Main.LocalPlayer.Infernum().ScreenFocusInterpolant *= Utils.GetLerpValue(210f, 202f, deathTimer, true);
            }

            #region sim
            // Simulate a large jump.
            //if(deathTimer == 30)
            //{
            //    // Simulate the jump, which will also set the landing position, if gotten.
            //    bool hitTile = SimulateJump(npc, ref npc.Infernum().ExtraAI[7], ref target, false, ref npc.Infernum().ExtraAI[10]);
            //    if (hitTile)
            //    {
            //        float landingPositionX = npc.Infernum().ExtraAI[8];
            //        float landingPositionY = npc.Infernum().ExtraAI[9];
            //        NPC ninjaNPC = null;
            //        for (int i = 0; i < Main.npc.Length; i++)
            //        {
            //            if (Main.npc[i].type == ModContent.NPCType<Ninja>())
            //            {
            //                ninjaNPC = Main.npc[i];
            //                goto BreakLoop;
            //            }
            //        }

            //        BreakLoop:

            //        if (ninjaNPC is not null)
            //        {
            //            ninjaNPC.Infernum().ExtraAI[8] = landingPositionX;
            //            ninjaNPC.Infernum().ExtraAI[9] = landingPositionY;
            //        }

            //    }
            //    else
            //    {
            //        // No valid tile was found, kill ourselves.
            //        npc.NPCLoot();
            //        npc.active = false;
            //        return;
            //    }
            //}

            //if(deathTimer>30)
            //{
            //    SimulateJump(npc, ref npc.Infernum().ExtraAI[7], ref target, true, ref npc.Infernum().ExtraAI[10]);
            //}
            #endregion

            // Perform a large jump
            DoBehavior_LargeJump(npc, ref target, ref deathTimer, true);

            // Check if the ninja has initialized their local timer, which happens after they create the projectile.
            if (ninjaNPC.Infernum().ExtraAI[10] > 0)
            {

            }
            if (deathTimer >= deathAnimationLength)
            {
                // Die
                KillKingSlime(npc);
            }
        }

        public static void KillKingSlime(NPC npc)
        {
            npc.NPCLoot();
            npc.active = false;
        }

        public static void DoBehavior_Despawn(NPC npc)
        {
            // Rapidly cease any horizontal movement, to prevent weird sliding behaviors
            npc.velocity.X *= 0.8f;
            if (Math.Abs(npc.velocity.X) < 0.1f)
                npc.velocity.X = 0f;

            // Disable damage.
            npc.dontTakeDamage = true;
            npc.damage = 0;

            // Release slime dust to accompany the despawn behavior.
            for (int i = 0; i < 30; i++)
            {
                Dust slime = Dust.NewDustDirect(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, 4, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                slime.noGravity = true;
                slime.velocity *= 0.5f;
            }

            // Shrink over time.
            npc.scale *= 0.97f;
            if (npc.timeLeft > 30)
                npc.timeLeft = 30;

            // Update the hitbox based on the current scale.
            npc.position = npc.Center;
            npc.Size = HitboxScaleFactor * npc.scale;
            npc.Center = npc.position;

            // Despawn if sufficiently small. This is bypassed if the target is sufficiently far away, in which case the despawn happens immediately.
            if (npc.scale < 0.7f || !npc.WithinRange(Main.player[npc.target].Center, DespawnDistance))
            {
                npc.active = false;
                npc.netUpdate = true;
            }
        }

        public static void DoBehavior_SmallJump(NPC npc, ref Player target, ref float attackTimer)
        {
            if (npc.velocity.Y == 0f)
            {
                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;

                if (attackTimer == 25f && npc.collideY)
                {
                    target = Main.player[npc.target];
                    float jumpSpeed = MathHelper.Lerp(8.25f, 11.6f, Utils.GetLerpValue(40f, 700f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                    jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                    npc.velocity = new Vector2(npc.direction * 8.5f, -jumpSpeed);
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 2.4f;

                    npc.netUpdate = true;
                }

                if (attackTimer > 25f && (npc.collideY || attackTimer >= 180f))
                    SelectNextAttack(npc);
            }
            else
                attackTimer--;
        }

        public static void DoBehavior_LargeJump(NPC npc, ref Player target, ref float attackTimer, bool performingDeathAnimation = false)
        {
            if (npc.velocity.Y == 0f)
            {
                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;

                if (attackTimer == 35f)
                {
                    target = Main.player[npc.target];
                    float jumpSpeed = MathHelper.Lerp(10f, 23f, Utils.GetLerpValue(40f, 360f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                    jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                    npc.velocity = new Vector2(npc.direction * 10.25f, -jumpSpeed);
                    if (BossRushEvent.BossRushActive)
                        npc.velocity *= 1.5f;
                    npc.netUpdate = true;
                }

                if (attackTimer > 35f && (npc.collideY || attackTimer >= 180f))
                {
                    if (!performingDeathAnimation)
                    {
                        SelectNextAttack(npc);
                        return;
                    }
                    else
                    {
                        NPC ninjaNPC = null;
                        for (int i = 0; i < Main.npc.Length; i++)
                        {
                            if (Main.npc[i].type == ModContent.NPCType<Ninja>())
                            {
                                ninjaNPC = Main.npc[i];
                                goto BreakLoop;
                            }
                        }

                        BreakLoop:

                        if (ninjaNPC is not null)
                        {
                            ninjaNPC.Infernum().ExtraAI[8] = npc.Center.X;
                            ninjaNPC.Infernum().ExtraAI[9] = npc.Center.Y;
                        }
                    }
                }
            }
            else
                attackTimer--;
        }

        public static void DoBehavior_Teleport(NPC npc, Player target, float idealScale, ref float attackTimer, ref float teleportDirection)
        {
            int digTime = 60;
            int reappearTime = 30;

            ref float digXPosition = ref npc.Infernum().ExtraAI[0];
            ref float digYPosition = ref npc.Infernum().ExtraAI[1];

            if (attackTimer < digTime)
            {
                // Rapidly cease any horizontal movement, to prevent weird sliding behaviors
                npc.velocity.X *= 0.8f;
                if (Math.Abs(npc.velocity.X) < 0.1f)
                    npc.velocity.X = 0f;

                npc.scale = MathHelper.Lerp(idealScale, 0.2f, MathHelper.Clamp((float)Math.Pow(attackTimer / digTime, 3D), 0f, 1f));
                npc.Opacity = Utils.GetLerpValue(0.7f, 1f, npc.scale, true) * 0.7f;
                npc.dontTakeDamage = true;
                npc.damage = 0;

                // Release slime dust to accompany the teleport
                for (int i = 0; i < 30; i++)
                {
                    Dust slime = Dust.NewDustDirect(npc.position + Vector2.UnitX * -20f, npc.width + 40, npc.height, 4, npc.velocity.X, npc.velocity.Y, 150, new Color(78, 136, 255, 80), 2f);
                    slime.noGravity = true;
                    slime.velocity *= 0.5f;
                }
            }

            // Perform the teleport. 
            if (attackTimer == digTime)
            {
                // Initialize the teleport direction as on the right if it has not been defined yet.
                if (teleportDirection == 0f)
                    teleportDirection = 1f;

                digXPosition = target.Center.X + 600f * teleportDirection;
                digYPosition = target.Top.Y - 800f;
                if (digYPosition < 100f)
                    digYPosition = 100f;

                if (Main.netMode != NetmodeID.Server)
                    Gore.NewGore(npc.GetSource_FromAI(), npc.Center + new Vector2(-40f, npc.height * -0.5f), npc.velocity, 734, 1f);

                WorldUtils.Find(new Vector2(digXPosition, digYPosition).ToTileCoordinates(), Searches.Chain(new Searches.Down(200), new GenCondition[]
                {
                    new CustomTileConditions.IsSolidOrSolidTop(),
                    new CustomTileConditions.ActiveAndNotActuated()
                }), out Point newBottom);

                // Decide the teleport position and prepare the teleport direction for next time by making it go to the other side.
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    npc.Bottom = newBottom.ToWorldCoordinates(8, -16);
                    teleportDirection *= -1f;
                    npc.netUpdate = true;
                }
                npc.Opacity = 0.7f;
            }

            if (attackTimer > digTime && attackTimer <= digTime + reappearTime)
            {
                npc.scale = MathHelper.Lerp(0.2f, idealScale, Utils.GetLerpValue(digTime, digTime + reappearTime, attackTimer, true));
                npc.Opacity = 0.7f;
                npc.dontTakeDamage = true;
                npc.damage = 0;
            }

            if (attackTimer > digTime + reappearTime + 25)
                SelectNextAttack(npc);
        }

        public static void SelectNextAttack(NPC npc)
        {
            npc.ai[3]++;

            KingSlimeAttackType[] patternToUse = AttackPattern;
            KingSlimeAttackType nextAttackType = patternToUse[(int)(npc.ai[3] % patternToUse.Length)];

            // Go to the next AI state.
            npc.ai[1] = (int)nextAttackType;

            // Reset the attack timer.
            npc.ai[2] = 0f;

            // And reset the misc ai slots.
            for (int i = 0; i < 5; i++)
                npc.Infernum().ExtraAI[i] = 0f;

            if (npc.velocity.Y < 0f)
                npc.velocity.Y = 0f;
            npc.netUpdate = true;
        }

        public static bool SimulateJump(NPC npc, ref float simTimer, ref Player target, bool actuallyPerformJump, ref float actualTimer)
        {
            if (!actuallyPerformJump)
            {
                Vector2 simulatedVelocity = npc.velocity;
                Vector2 simulatedPosition = npc.Center;
                bool collided = false;
                while (!collided && simTimer < 180)
                {
                    if (simulatedVelocity.Y == 0f)
                    {
                        simulatedVelocity.X *= 0.8f;
                        if (Math.Abs(simulatedVelocity.X) < 0.1f)
                            simulatedVelocity.X = 0f;

                        if (simTimer == 35f)
                        {
                            target = Main.player[npc.target];
                            float jumpSpeed = MathHelper.Lerp(10f, 23f, Utils.GetLerpValue(40f, 360f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                            jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                            simulatedVelocity = new Vector2(npc.direction * 10.25f, -jumpSpeed);
                            if (BossRushEvent.BossRushActive)
                                simulatedVelocity *= 1.5f;
                            //npc.netUpdate = true;
                        }

                    }
                    bool onSolidGround = WorldGen.SolidTile(Framing.GetTileSafely(simulatedPosition + Vector2.UnitY * 16f));
                    // If colliding with something
                    if (onSolidGround && simTimer > 30)
                    {
                        npc.Infernum().ExtraAI[8] = simulatedPosition.X;
                        npc.Infernum().ExtraAI[9] = simulatedPosition.Y;
                        return true;
                    }
                    simulatedPosition += simulatedVelocity;
                    simulatedPosition.Y = 0.4f;
                    simTimer++;
                }
                return false;
            }
            else
            {
                if (npc.velocity.Y == 0f)
                {
                    npc.velocity.X *= 0.8f;
                    if (Math.Abs(npc.velocity.X) < 0.1f)
                        npc.velocity.X = 0f;

                    if (actualTimer == 35f)
                    {
                        target = Main.player[npc.target];
                        float jumpSpeed = MathHelper.Lerp(10f, 23f, Utils.GetLerpValue(40f, 360f, Math.Abs(target.Center.Y - npc.Center.Y), true));
                        jumpSpeed *= Main.rand.NextFloat(1f, 1.15f);

                        npc.velocity = new Vector2(npc.direction * 10.25f, -jumpSpeed);
                        if (BossRushEvent.BossRushActive)
                            npc.velocity *= 1.5f;
                        npc.netUpdate = true;
                    }
                }
            }
            return false;
        }
        #endregion AI

        #region Draw Code

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D kingSlimeTexture = TextureAssets.Npc[npc.type].Value;
            Vector2 kingSlimeDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * npc.gfxOffY;

            // Draw the ninja, if it's still stuck.
            if (npc.life > npc.lifeMax * Phase3LifeRatio)
            {
                Vector2 drawOffset = Vector2.Zero;
                float ninjaRotation = npc.velocity.X * 0.05f;
                drawOffset.Y -= npc.velocity.Y;
                drawOffset.X -= npc.velocity.X * 2f;
                if (npc.frame.Y == 120)
                    drawOffset.Y += 2f;
                if (npc.frame.Y == 360)
                    drawOffset.Y -= 2f;
                if (npc.frame.Y == 480)
                    drawOffset.Y -= 6f;

                Texture2D ninjaTexture = TextureAssets.Ninja.Value;
                Vector2 ninjaDrawPosition = npc.Center - Main.screenPosition + drawOffset;
                Main.spriteBatch.Draw(ninjaTexture, ninjaDrawPosition, null, lightColor, ninjaRotation, ninjaTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.Draw(kingSlimeTexture, kingSlimeDrawPosition, npc.frame, npc.GetAlpha(lightColor), npc.rotation, npc.frame.Size() * 0.5f, npc.scale, SpriteEffects.None, 0f);

            float verticalCrownOffset = 0f;
            switch (npc.frame.Y / (TextureAssets.Npc[npc.type].Value.Height / Main.npcFrameCount[npc.type]))
            {
                case 0:
                    verticalCrownOffset = 2f;
                    break;
                case 1:
                    verticalCrownOffset = -6f;
                    break;
                case 2:
                    verticalCrownOffset = 2f;
                    break;
                case 3:
                    verticalCrownOffset = 10f;
                    break;
                case 4:
                    verticalCrownOffset = 2f;
                    break;
                case 5:
                    verticalCrownOffset = 0f;
                    break;
            }
            Texture2D crownTexture = TextureAssets.Extra[39].Value;
            Vector2 crownDrawPosition = npc.Center - Main.screenPosition + Vector2.UnitY * (npc.gfxOffY - (56f - verticalCrownOffset) * npc.scale);
            Main.spriteBatch.Draw(crownTexture, crownDrawPosition, null, lightColor, 0f, crownTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            return false;
        }
        #endregion Drawcode

        #region Tips
        public override IEnumerable<Func<NPC, string>> GetTips()
        {
            yield return n => "Try to learn King Slime's jump pattern! It could help you plan your next move better.";
            yield return n => "With a jump that high, I wonder if you could duck beneath him?";
            yield return n =>
            {
                if (HatGirlTipsManager.ShouldUseJokeText)
                    return "Quite a sticky situation you had to deal with...";
                return string.Empty;
            };
        }
        #endregion
    }
}