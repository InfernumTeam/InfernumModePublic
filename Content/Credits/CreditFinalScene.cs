using CalamityMod;
using InfernumMode.Assets.Effects;
using InfernumMode.Assets.ExtraTextures;
using InfernumMode.Common.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace InfernumMode.Content.Credits
{
    public static class CreditFinalScene
    {
        #region Textures
        public static Texture2D ArixTexture { get; private set; }
        public static Texture2D BlastTexture { get; private set; }
        public static Texture2D BronzeTexture { get; private set; }
        public static Texture2D DominicTexture { get; private set; }
        public static Texture2D IbanTexture { get; private set; }
        public static Texture2D JaretoTexture { get; private set; }
        public static Texture2D JoeyTexture { get; private set; }
        public static Texture2D LglTexture { get; private set; }
        public static Texture2D MattikTexture { get; private set; }
        public static Texture2D MyraTexture { get; private set; }
        public static Texture2D PikyTexture { get; private set; }
        public static Texture2D ShadeTexture { get; private set; }
        public static Texture2D SmhTexture { get; private set; }
        public static Texture2D ToastyTexture { get; private set; }

        public static Texture2D MainScene { get; private set; }

        public static ManagedRenderTarget PortraitTarget { get; private set; }
        #endregion

        private static List<CreditDeveloper> CreditDevelopers;

        internal static Vector2 ImagePosition;

        internal static Vector2 PortalPosition;

        public static float PortalAppearDelay => 180f;

        public static void SetupObjects()
        {
            ArixTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Arix", AssetRequestMode.ImmediateLoad).Value;
            BlastTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Blast", AssetRequestMode.ImmediateLoad).Value;
            BronzeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Bronze", AssetRequestMode.ImmediateLoad).Value;
            DominicTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Dom", AssetRequestMode.ImmediateLoad).Value;
            IbanTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Iban", AssetRequestMode.ImmediateLoad).Value;
            JaretoTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Jareto", AssetRequestMode.ImmediateLoad).Value;
            JoeyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/joey", AssetRequestMode.ImmediateLoad).Value;
            LglTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/lgl", AssetRequestMode.ImmediateLoad).Value;
            MattikTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Mattik", AssetRequestMode.ImmediateLoad).Value;
            MyraTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Myra", AssetRequestMode.ImmediateLoad).Value;
            PikyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Piky", AssetRequestMode.ImmediateLoad).Value;
            ShadeTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Shade", AssetRequestMode.ImmediateLoad).Value;
            SmhTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Smh", AssetRequestMode.ImmediateLoad).Value;
            ToastyTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/Toasty", AssetRequestMode.ImmediateLoad).Value;

            MainScene = ModContent.Request<Texture2D>("InfernumMode/Content/Credits/Textures/FinalCreditScene", AssetRequestMode.ImmediateLoad).Value;

            ImagePosition = new(Main.screenWidth * 0.5f, Main.screenHeight * 0.4f);
            PortalPosition = ImagePosition + new Vector2(0f, -150f);

            CreditDevelopers = new();

            var arix = new CreditDeveloper(ArixTexture, ImagePosition + new Vector2(-90f, -5f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(arix);
            var blast = new CreditDeveloper(BlastTexture, ImagePosition + new Vector2(-260f, 20f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(blast);
            var bronze = new CreditDeveloper(BronzeTexture, ImagePosition + new Vector2(260f, 20f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(bronze);
            var dom = new CreditDeveloper(DominicTexture, ImagePosition + new Vector2(-45f, -5f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(dom);
            var iban = new CreditDeveloper(IbanTexture, ImagePosition + new Vector2(90f, -5), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(iban);
            var jareto = new CreditDeveloper(JaretoTexture, ImagePosition + new Vector2(-130f, -5f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(jareto);
            var mattik = new CreditDeveloper(MattikTexture, ImagePosition + new Vector2(130f, 5f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(mattik);
            var myra = new CreditDeveloper(MyraTexture, ImagePosition + new Vector2(-195f, 23f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(myra);
            var piky = new CreditDeveloper(PikyTexture, ImagePosition + new Vector2(175f, 16f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(piky);
            var shade = new CreditDeveloper(ShadeTexture, ImagePosition + new Vector2(220f, 25f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(shade);
            var smh = new CreditDeveloper(SmhTexture, ImagePosition + new Vector2(-240f, 31f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(smh);
            var toasty = new CreditDeveloper(ToastyTexture, ImagePosition + new Vector2(45f, -3f), Vector2.Zero, 0f, SpriteEffects.None);
            CreditDevelopers.Add(toasty);

            // Initialize the portrait render target.
            PortraitTarget ??= new ManagedRenderTarget(true, RenderTargetManager.CreateScreenSizedTarget);
        }

        public static void Update(float time)
        {
            float portalAppearLength = 45f;
            float suckSpeed = 0f;

            // Accelerate while the portal is being created.
            if (time >= PortalAppearDelay)
                suckSpeed = Pow(1 + CalamityUtils.ExpOutEasing(Utils.GetLerpValue(PortalAppearDelay + portalAppearLength, PortalAppearDelay + portalAppearLength + 90f, time, true), 0), 2f);

            foreach (var cd in CreditDevelopers)
            {
                if (suckSpeed > 0)
                {
                    cd.Velocity = cd.Position.DirectionTo(PortalPosition) * suckSpeed * 3f;
                    cd.Rotation = Abs(cd.Rotation.AngleTowards(cd.Velocity.ToRotation(), 0.08f));
                }
                else
                {
                    if (Main.rand.NextBool(30) && cd.Velocity.Y == 0f && time < PortalAppearDelay - 76f)
                    {
                        cd.Velocity = -Vector2.UnitY * 3f;
                        cd.Position.Y--;
                    }
                    cd.Velocity.Y += 0.11f;
                    if (cd.Position.Y >= cd.StartingPosition.Y)
                        cd.Velocity.Y = 0f;
                }
                cd.Update();
            }
        }

        #region Drawing
        public static void PreparePortraitTarget(GameTime _)
        {
            float opacity = CreditManager.FinalSceneOpacity;
            if (PortraitTarget is null || opacity <= 0.01f)
                return;

            Main.instance.GraphicsDevice.SetRenderTarget(PortraitTarget.Target);
            Main.instance.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, Main.Rasterizer);

            // Draw the main image.
            Effect creditEffect = InfernumEffectsRegistry.CreditShader.GetShader().Shader;
            creditEffect.Parameters["overallOpacity"].SetValue(opacity);
            creditEffect.Parameters["justCrop"].SetValue(true);
            creditEffect.CurrentTechnique.Passes["CreditPass"].Apply();

            Main.spriteBatch.Draw(MainScene, ImagePosition, null, Color.White * opacity, 0f, MainScene.Size() * 0.5f, 0.6f, SpriteEffects.None, 0f);

            // Clear the shader by applying a blank one.
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();

            // Draw the infernal chalice
            Texture2D chaliceTexture = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Wishes/InfernalChaliceTileAnimation").Value;
            Texture2D chaliceGlowmask = ModContent.Request<Texture2D>("InfernumMode/Content/Tiles/Wishes/InfernalChaliceTileAnimation").Value;
            int frame = (int)(Main.GlobalTimeWrappedHourly * 11f) % 8;
            Vector2 drawPosition = ImagePosition + new Vector2(0f, -13.5f);
            Rectangle frameRect = chaliceTexture.Frame(1, 8, 0, frame);
            Main.spriteBatch.Draw(chaliceTexture, drawPosition, frameRect, Color.White * opacity, 0f, frameRect.Size() * 0.5f, 0.6f, 0, 0f);
            Main.spriteBatch.Draw(chaliceGlowmask, drawPosition, frameRect, Color.White * opacity, 0f, frameRect.Size() * 0.5f, 0.6f, 0, 0f);

            // Draw each developer.
            foreach (CreditDeveloper cd in CreditDevelopers)
            {
                // Shrink when close to the portal.
                float scale = 1f;
                float distance = cd.Position.Distance(PortalPosition);
                if (distance < 100f)
                    scale = CalamityUtils.SineInOutEasing(Utils.GetLerpValue(0f, 100f, distance, true), 0);

                // Don't bother drawing if either of these are 0.
                if (scale == 0 || opacity == 0)
                {
                    Main.instance.GraphicsDevice.SetRenderTarget(null);
                    Main.spriteBatch.End();
                    return;
                }

                cd.Draw(opacity, scale);
            }

            Main.instance.GraphicsDevice.SetRenderTarget(null);
            Main.spriteBatch.End();
        }

        public static void Draw(float opacity)
        {
            float portalAppearLength = 45f;
            float portalOpacity = CalamityUtils.SineInOutEasing(Utils.GetLerpValue(PortalAppearDelay, PortalAppearDelay + portalAppearLength, CreditManager.CreditsTimer, true), 0);
            float distortionIntensity = Utils.GetLerpValue(PortalAppearDelay + 84f, PortalAppearDelay + 256f, CreditManager.CreditsTimer, true);

            // Draw the portrait with an optional collapse effect.
            if (distortionIntensity > 0f)
            {
                var collapseEffect = InfernumEffectsRegistry.BackgroundDistortionShader;
                collapseEffect.UseImage1("Images/Misc/Perlin");
                collapseEffect.Shader.Parameters["distortionIntensity"].SetValue(Pow(distortionIntensity, 1.05f));
                collapseEffect.Shader.Parameters["center"].SetValue(PortalPosition / new Vector2(Main.screenWidth, Main.screenHeight));
                collapseEffect.Apply();
            }
            Main.spriteBatch.Draw(PortraitTarget.Target, new Vector2(Main.screenWidth, Main.screenHeight - distortionIntensity * 600f) * 0.5f, null, Color.White, 0f, PortraitTarget.Target.Size() * 0.5f, 1f - Pow(distortionIntensity, 0.4f) * 0.9f, 0, 0f);

            if (portalOpacity > 0f)
            {
                Main.spriteBatch.EnterShaderRegion();
                DrawPortal(portalOpacity, opacity);
            }
        }

        private static void DrawPortal(float portalOpacity, float opacity)
        {
            float portalScale = portalOpacity * opacity * 0.5f;

            // Get squishyness variables
            float sineX = (float)((1 + Math.Sin(Main.GlobalTimeWrappedHourly * 2.25f)) / 2);
            float sineY = (float)((1 + Math.Sin(Main.GlobalTimeWrappedHourly * 1.5f)) / 2);
            float scaleScalarX = Lerp(0.95f, 1.15f, sineX);
            float scaleScalarY = Lerp(0.95f, 1.15f, sineY);

            Texture2D texture = InfernumTextureRegistry.WhiteHole.Value;
            Asset<Texture2D> eventHorizonTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/TechyNoise");
            Vector2 scale = new(0.22f * scaleScalarX, 0.22f * scaleScalarY);
            DrawData eventHorizon = new(texture, PortalPosition, new(0, 0, texture.Width, texture.Height), Color.Green, 0f, texture.Size() * 0.5f, scale * portalScale, SpriteEffects.None, 0);
            InfernumEffectsRegistry.RealityTear2Shader.SetShaderTexture(eventHorizonTexture);
            InfernumEffectsRegistry.RealityTear2Shader.UseSaturation(0.3f);
            InfernumEffectsRegistry.RealityTear2Shader.Apply(eventHorizon);
            eventHorizon.Draw(Main.spriteBatch);

            // Draw the portal.
            Texture2D diskTexture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes").Value;
            DrawData accrecionDisk = new(diskTexture, PortalPosition, null, Color.White, 0f, diskTexture.Size() * 0.5f, 0.8f * portalScale, SpriteEffects.None, 0);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Lerp(Color.Teal, Color.Lime, 0.5f));
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(portalOpacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Green);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply(accrecionDisk);
            accrecionDisk.Draw(Main.spriteBatch);

            GameShaders.Misc["CalamityMod:DoGPortal"].UseColor(Color.Green);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseOpacity(portalOpacity);
            GameShaders.Misc["CalamityMod:DoGPortal"].UseSecondaryColor(Color.Khaki);
            GameShaders.Misc["CalamityMod:DoGPortal"].Apply(accrecionDisk);
            accrecionDisk.scale = new Vector2(0.83f) * portalScale;
            accrecionDisk.Draw(Main.spriteBatch);

            Main.pixelShader.CurrentTechnique.Passes[0].Apply();

            scaleScalarX = Lerp(0.97f, 1.13f, sineX);
            scaleScalarY = Lerp(0.97f, 1.13f, sineY);
            Vector2 blackScale = new(0.2f * scaleScalarX, 0.2f * scaleScalarY);
            Main.spriteBatch.Draw(texture, PortalPosition, new(0, 0, texture.Width, texture.Height), Color.Black, 0f, texture.Size() * 0.5f, blackScale * portalScale, SpriteEffects.None, 0f);

            Texture2D fireNoise = InfernumTextureRegistry.BlurryPerlinNoise.Value;
            Texture2D miscNoise = InfernumTextureRegistry.HexagonGrid.Value;

            Effect portal = InfernumEffectsRegistry.ProfanedPortalShader.Shader;
            portal.Parameters["sampleTexture"].SetValue(fireNoise);
            portal.Parameters["sampleTexture2"].SetValue(miscNoise);
            portal.Parameters["mainColor"].SetValue(Color.DarkGreen.ToVector3());
            portal.Parameters["secondaryColor"].SetValue(Color.DarkOliveGreen.ToVector3());
            portal.Parameters["resolution"].SetValue(new Vector2(120f));
            portal.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            portal.Parameters["opacity"].SetValue(opacity * 0.5f);
            portal.Parameters["innerGlowAmount"].SetValue(0.8f);
            portal.Parameters["innerGlowDistance"].SetValue(0.15f);
            portal.CurrentTechnique.Passes[0].Apply();
            Main.spriteBatch.Draw(fireNoise, PortalPosition, null, Color.White, 0f, fireNoise.Size() * 0.5f, new Vector2(scaleScalarX, scaleScalarY) * portalScale, SpriteEffects.None, 0f);
        }
        #endregion
    }
}
