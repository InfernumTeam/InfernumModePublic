using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.BehaviorOverrides.BossAIs.EmpressOfLight
{
    public class SpinningPrismLaserbeam : ModProjectile
    {
        public PrimitiveTrail RayDrawer = null;

        public ref float AngularVelocity => ref Projectile.ai[0];

        public ref float LaserLength => ref Projectile.ai[1];

        public ref float Time => ref Projectile.localAI[0];

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public const float MaxLaserLength = 4800f;

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Prismatic Ray");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LightCloud.LaserLifetime;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.Calamity().DealsDefenseDamage = true;
        }

        public override void AI()
        {
            // Grow bigger up to a point.
            float maxScale = MathHelper.Lerp(0.051f, 1.5f, Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 16f, Time, true));
            Projectile.scale = MathHelper.Clamp(Projectile.scale + 0.02f, 0.05f, maxScale);

            // Spin the laserbeam.
            Projectile.velocity = Projectile.velocity.RotatedBy(AngularVelocity * Utils.GetLerpValue(0f, 32f, Time, true));

            // Update the laser length.
            LaserLength = MaxLaserLength;

            // Make the beam cast light along its length. The brightness of the light is reliant on the scale of the beam.
            DelegateMethods.v3_1 = Color.White.ToVector3() * Projectile.scale * 0.6f;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.width * Projectile.scale, DelegateMethods.CastLight);
            Time++;
        }

        internal float PrimitiveWidthFunction(float completionRatio) => Projectile.scale * 27f;

        internal Color PrimitiveColorFunction(float completionRatio)
        {
            float opacity = Projectile.Opacity * Utils.GetLerpValue(0.97f, 0.9f, completionRatio, true) * 
                Utils.GetLerpValue(0f, MathHelper.Clamp(15f / LaserLength, 0f, 0.5f), completionRatio, true) *
                (float)Math.Pow(Utils.GetLerpValue(60f, 270f, LaserLength, true), 3D);
            Color c = Main.hslToRgb((completionRatio * 5f + Main.GlobalTimeWrappedHourly * 0.5f + Projectile.identity * 0.3156f) % 1f, 1f, 0.7f) * opacity;
            c.A = 0;

            return c;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (RayDrawer is null)
                RayDrawer = new PrimitiveTrail(PrimitiveWidthFunction, PrimitiveColorFunction, specialShader: GameShaders.Misc["Infernum:PrismaticRay"]);

            GameShaders.Misc["Infernum:PrismaticRay"].UseImage1("Images/Misc/Perlin");
            Main.instance.GraphicsDevice.Textures[2] = ModContent.Request<Texture2D>("InfernumMode/ExtraTextures/PrismaticLaserbeamStreak").Value;

            Vector2[] basePoints = new Vector2[24];
            for (int i = 0; i < basePoints.Length; i++)
                basePoints[i] = Projectile.Center + Projectile.velocity * i / (basePoints.Length - 1f) * LaserLength;

            Vector2 overallOffset = -Main.screenPosition;
            RayDrawer.Draw(basePoints, overallOffset, 92);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Projectile.scale * 25f, ref _);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overWiresUI.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;
    }
}
