using CalamityMod.Buffs.DamageOverTime;
using InfernumMode;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Boss
{
    public class HealingCrystalShot : ModProjectile
    {
        public static readonly Color[] ColorSet = new Color[]
        {
            // Pale pink crystal.
            new Color(181, 136, 177),

            // Profaned fire.
            new Color(255, 191, 73),

            // Yellow-orange crystal.
            new Color(255, 194, 161),
        };

		public NPC Target => Main.npc[(int)projectile.ai[0]];
        public Color StreakBaseColor => CalamityUtils.MulticolorLerp(projectile.ai[1] % 0.999f, ColorSet);
        public ref float HealAmount => ref projectile.localAI[0];
        public override string Texture => "CalamityMod/Projectiles/StarProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystalline Light");
            ProjectileID.Sets.TrailingMode[projectile.type] = 2;
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 24;
        }

        public override void SetDefaults()
        {
            projectile.width = projectile.height = 30;
            projectile.alpha = 255;
            projectile.penetrate = -1;
            projectile.timeLeft = 240;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            cooldownSlot = 1;
        }

        public override void AI()
        {
            if (projectile.timeLeft > 140f)
			{
				float offsetScale = (float)Math.Cos(projectile.identity % 6f / 6f + projectile.position.X / 320f + projectile.position.Y / 160f);

				if (projectile.velocity.Length() < 43f)
					projectile.velocity *= 1.008f;
				projectile.velocity = projectile.velocity.RotatedBy(offsetScale * MathHelper.TwoPi / 240f);
			}

			if (projectile.timeLeft > 30f)
			{
				Vector2 idealVelocity = projectile.velocity;
				if (Main.npc.IndexInRange((int)projectile.ai[0]))
					idealVelocity = projectile.SafeDirectionTo(Target.Center) * (30f + Target.velocity.Length());

                if (projectile.Hitbox.Intersects(Target.Hitbox))
                {
                    if (!Main.dedServ)
                    {
                        for (int i = 0; i < 16; i++)
                        {
                            Dust light = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(16f, 16f), 261);
                            light.velocity = Main.rand.NextVector2Circular(8f, 8f);
                            light.scale = Main.rand.NextFloat(0.9f, 1.4f);
                            light.color = CalamityUtils.MulticolorLerp((projectile.ai[1] + Main.rand.NextFloat(-0.15f, 0.15f)) % 0.999f, ColorSet);
                            light.noGravity = true;
                        }
                    }

                    Target.HealEffect((int)HealAmount);
                    Target.life = Utils.Clamp(Target.life + (int)HealAmount, 0, Target.lifeMax);

                    projectile.Kill();
                }

				projectile.velocity = Vector2.SmoothStep(projectile.velocity, idealVelocity, MathHelper.Lerp(0.07f, 0.15f, Utils.InverseLerp(140f, 30f, projectile.timeLeft, true)));
			}

            if (projectile.timeLeft < 15)
                projectile.damage = 0;

			projectile.Opacity = Utils.InverseLerp(240f, 220f, projectile.timeLeft, true) * Utils.InverseLerp(0f, 20f, projectile.timeLeft, true);
			projectile.rotation = projectile.velocity.ToRotation() + MathHelper.PiOver2;
		}

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D streakTexture = Main.projectileTexture[projectile.type];
            for (int i = 1; i < projectile.oldPos.Length; i++)
            {
                if (projectile.oldPos[i - 1] == Vector2.Zero || projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completionRatio = i / (float)projectile.oldPos.Length;
                float fade = (float)Math.Pow(completionRatio, 2D);
                float scale = projectile.scale * MathHelper.Lerp(1.3f, 0.9f, Utils.InverseLerp(0f, 0.24f, completionRatio, true)) * MathHelper.Lerp(0.9f, 0.56f, Utils.InverseLerp(0.5f, 0.78f, completionRatio, true));
                Color drawColor = Color.Lerp(StreakBaseColor, new Color(229, 255, 255), fade) * (1f - fade) * projectile.Opacity;
                drawColor.A = 0;

                Vector2 drawPosition = projectile.oldPos[i - 1] + projectile.Size * 0.5f - Main.screenPosition;
                Vector2 drawPosition2 = Vector2.Lerp(drawPosition, projectile.oldPos[i] + projectile.Size * 0.5f - Main.screenPosition, 0.5f);
                spriteBatch.Draw(streakTexture, drawPosition, null, drawColor, projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(streakTexture, drawPosition2, null, drawColor, projectile.oldRot[i], streakTexture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < projectile.oldPos.Length / 2; i++)
            {
                if (targetHitbox.Intersects(Utils.CenteredRectangle(projectile.oldPos[i] + projectile.Size * 0.5f, projectile.Size)))
                    return true;
            }
            return false;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
		}

		public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)	
        {
			target.Calamity().lastProjectileHit = projectile;
		}
    }
}
