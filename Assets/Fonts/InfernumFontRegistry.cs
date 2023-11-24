﻿using CalamityMod.UI;
using InfernumMode.Common.DataStructures;
using ReLogic.Content;
using ReLogic.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace InfernumMode.Assets.Fonts
{
    public class InfernumFontRegistry : ModSystem
    {
        public static LocalizedSpriteFont BossIntroScreensFont
        { 
            get;
            private set;
        }

        public static LocalizedSpriteFont HPBarFont
        {
            get;
            private set;
        }

        public static LocalizedSpriteFont ProfanedTextFont
        {
            get;
            private set;
        }


        public override void Load()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            BossIntroScreensFont = new LocalizedSpriteFont(BossHealthBarManager.HPBarFont)
                .WithLanguage(GameCulture.CultureName.Chinese, ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/BossIntroScreensFont", AssetRequestMode.ImmediateLoad).Value);

            HPBarFont = new LocalizedSpriteFont(ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/HPBarFont", AssetRequestMode.ImmediateLoad).Value);

            ProfanedTextFont = new LocalizedSpriteFont(ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/ProfanedText", AssetRequestMode.ImmediateLoad).Value)
                .WithLanguage(GameCulture.CultureName.Chinese, ModContent.Request<DynamicSpriteFont>("InfernumMode/Assets/Fonts/ProfanedTextChinese", AssetRequestMode.ImmediateLoad).Value);
        }

        public override void Unload()
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            BossIntroScreensFont = null;
            HPBarFont = null;
            ProfanedTextFont = null;
        }
    }
}
