using CalamityMod;
using CalamityMod.Particles.Metaballs;
using InfernumMode.Assets.Sounds;
using InfernumMode.Common.Graphics;
using InfernumMode.Common.Graphics.Metaballs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Content.BehaviorOverrides.BossAIs.Providence
{
    public class HolySpear : ModProjectile
    {
        public Vector2 CurrentDirectionEdge
        {
            get
            {
                if (InLava)
                    return Vector2.UnitY;

                float bestOrthogonality = -100000f;
                Vector2 aimDirection = (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
                Vector2 edge = Vector2.Zero;
                Vector2[] edges = new Vector2[]
                {
                    Vector2.UnitX,
                    -Vector2.UnitX,
                    Vector2.UnitY,
                    -Vector2.UnitY
                };

                // Determine which edge the current direction aligns with most based on dot products.
                for (int i = 0; i < edges.Length; i++)
                {
                    float orthogonality = Vector2.Dot(aimDirection, edges[i]);
                    if (orthogonality > bestOrthogonality)
                    {
                        edge = edges[i];
                        bestOrthogonality = orthogonality;
                    }
                }

                return edge;
            }
        }

        public bool SpawnedInBlocks
        {
            get;
            set;
        }

        public bool BeenInBlockSinceStart
        {
            get;
            set;
        }

        public bool InLava
        {
            get
            {
                IEnumerable<Projectile> lavaProjectiles = Utilities.AllProjectilesByID(ModContent.ProjectileType<ProfanedLava>());
                if (!lavaProjectiles.Any())
                    return false;

                Rectangle tipHitbox = Utils.CenteredRectangle(Projectile.Center + (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2() * 60f, Vector2.One);
                return lavaProjectiles.Any(l => l.Colliding(l.Hitbox, tipHitbox));
            }
        }

        public ref float Time => ref Projectile.ai[0];

        public ref float DeathCountdown => ref Projectile.ai[1];

        public static int DeathDelay => 50;

        public override void SetStaticDefaults() => DisplayName.SetDefault("Holy Spear");

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 124;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
        }

        public override void AI()
        {
            bool tileCollision = Collision.SolidCollision(Projectile.Top, Projectile.width, Projectile.height);
            if (tileCollision && Time <= 1f)
            {
                SpawnedInBlocks = true;
                BeenInBlockSinceStart = false;
            }
            if (SpawnedInBlocks && !tileCollision && Time >= 35f)
                BeenInBlockSinceStart = false;

            // Decide the rotation of the spear based on velocity, if there is any.
            if (Projectile.velocity != Vector2.Zero)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // Handle death effects.
            if (DeathCountdown >= 1f)
            {
                // Prevent a natural death disrupting the fire wall directions by locking the timeLeft variable in place.
                Projectile.timeLeft = 60;

                // Release fire pillars.
                if (DeathCountdown % 5f == 3f)
                {
                    float perpendicularOffset = Utils.Remap(DeathCountdown, DeathDelay, 0f, 0f, 2000f);
                    Vector2 pillarDirection = -(Projectile.rotation - MathHelper.PiOver4).ToRotationVector2();
                    if (InLava)
                        pillarDirection = -Vector2.UnitY;

                    SoundEngine.PlaySound(SoundID.Item73, Projectile.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 pillarSpawnPosition = Projectile.Center + CurrentDirectionEdge.RotatedBy(MathHelper.PiOver2) * perpendicularOffset - pillarDirection * 800f;
                        Utilities.NewProjectileBetter(pillarSpawnPosition, pillarDirection, ModContent.ProjectileType<HolySpearFirePillar>(), 500, 0f);

                        pillarSpawnPosition = Projectile.Center - CurrentDirectionEdge.RotatedBy(MathHelper.PiOver2) * perpendicularOffset - pillarDirection * 800f;
                        Utilities.NewProjectileBetter(pillarSpawnPosition, pillarDirection, ModContent.ProjectileType<HolySpearFirePillar>(), 500, 0f);
                    }
                }

                DeathCountdown--;
                if (DeathCountdown <= 0f)
                    Projectile.Kill();
            }

            // Stick to lava.
            else if (InLava)
                PrepareForDeath(Projectile.velocity);

            // Wait a little bit before interacting with tiles.
            int collideDelay = SpawnedInBlocks ? 65 : 24;
            Projectile.tileCollide = Time >= collideDelay && !BeenInBlockSinceStart;
            Time++;
        }

        public void PrepareForDeath(Vector2 oldVelocity)
        {
            if (DeathCountdown > 0f)
                return;

            if (Main.netMode != NetmodeID.MultiplayerClient)
                Utilities.NewProjectileBetter(Projectile.Center + oldVelocity.SafeNormalize(Vector2.UnitY) * 60f, oldVelocity, ModContent.ProjectileType<StrongProfanedCrack>(), 0, 0f);

            if (CalamityConfig.Instance.Screenshake)
            {
                Main.LocalPlayer.Infernum_Camera().CurrentScreenShakePower = 9f;
                ScreenEffectSystem.SetBlurEffect(Projectile.Center, 0.2f, 18);
            }

            SoundEngine.PlaySound(InfernumSoundRegistry.ProvidenceSpearHitSound with { Volume = 2f }, Projectile.Center);
            Projectile.velocity = Vector2.Zero;
            Projectile.Center += oldVelocity.SafeNormalize(Vector2.Zero) * 50f;
            DeathCountdown = DeathDelay;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            PrepareForDeath(oldVelocity);
            return false;
        }

        public override void Kill(int timeLeft)
        {
            // Burst into lava metaballs on death.
            if (Main.netMode != NetmodeID.MultiplayerClient)
                ModContent.Request<Texture2D>(Texture).Value.CreateMetaballsFromTexture(ref FusableParticleManager.GetParticleSetByType<ProfanedLavaParticleSet>().Particles, Projectile.Center, Projectile.rotation, Projectile.scale, 20f, 30);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float burnInterpolant = Utils.GetLerpValue(45f, 0f, Time, true);
            float drawOffsetRadius = burnInterpolant * 16f;
            Color color = Projectile.GetAlpha(Color.Lerp(Color.White, Color.Yellow with { A = 0 } * 0.6f, burnInterpolant));
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Draw the spear as a white hot flame with additive blending before it converge inward to create the actual spear.
            for (int i = 0; i < 10; i++)
            {
                float rotation = Projectile.rotation + MathHelper.Lerp(-0.16f, 0.16f, i / 9f) * burnInterpolant;
                Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * drawOffsetRadius;
                Vector2 drawPosition = Projectile.Center - Main.screenPosition + drawOffset;
                Main.EntitySpriteDraw(texture, drawPosition, null, color, rotation, texture.Size() * 0.5f, Projectile.scale, 0, 0);
            }

            return false;
        }
    }
}