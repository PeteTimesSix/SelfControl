using PeteTimesSix.SelfControl.Extensions;
using PeteTimesSix.SelfControl.Rimworld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.SelfControl.UI
{
    public class SelfControl_Settings : ModSettings
    {

        public const int ABSOLUTE_MIN_MODS = 5;
        public const int ABSOLUTE_MAX_MODS = 500;
#if NORMAL
        public const int DEFAULT_MAX_MODS = 100;
        public const bool DEFAULT_KEYHELD = true;
#elif MILK
        public const int DEFAULT_MAX_MODS = 250;
        public const bool DEFAULT_KEYHELD = false;
#endif

        public int absoluteMin = ABSOLUTE_MIN_MODS;
        public int absoluteMax = ABSOLUTE_MAX_MODS;
        public int maxModCount = DEFAULT_MAX_MODS;
        public bool keyholderMode = DEFAULT_KEYHELD;

        Vector2 scrollPosition = new Vector2(0, 0);
        float cachedScrollHeight = 0;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref maxModCount, "maxModCount", DEFAULT_MAX_MODS);
            Scribe_Values.Look(ref keyholderMode, "keyholderMode", DEFAULT_KEYHELD);
        }
        internal void DoSettingsWindowContents(Rect outerRect)
        {
            Color colorSave = GUI.color;
            TextAnchor anchorSave = Text.Anchor;

            var headerRect = outerRect.TopPartPixels(50);
            var restOfRect = new Rect(outerRect);
            restOfRect.y += 50;
            restOfRect.height -= 50;

            Listing_Standard prelist = new Listing_Standard();
            prelist.Begin(headerRect);

            prelist.GapLine();

            prelist.End();

            bool needToScroll = cachedScrollHeight > outerRect.height;
            var viewRect = new Rect(restOfRect);
            if (needToScroll)
            {
                viewRect.width -= 20f;
                viewRect.height = cachedScrollHeight;
                Widgets.BeginScrollView(restOfRect, ref scrollPosition, viewRect);
            }

            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.maxOneColumn = true;
            listingStandard.Begin(viewRect);

            float maxWidth = listingStandard.ColumnWidth;

            var subsection = listingStandard.BeginHiddenSection(out float subsectionHeight);
            subsection.ColumnWidth = (maxWidth) / 2;

            float floatval = maxModCount;
            subsection.SliderLabeled("SC_MaxModsSlider_Label".Translate(), ref floatval, absoluteMin, absoluteMax, valueSuffix: " mods", tooltip: "SC_MaxModsSlider_Tooltiip".Translate());
            if (!keyholderMode)
                maxModCount = (int)floatval;

            subsection.CheckboxLabeled(keyholderMode ? "SC_KeyholderMode_on".Translate() : "SC_KeyholderMode_off".Translate(), ref keyholderMode, "SC_KeyholderMode_tooltip".Translate(), disabled: keyholderMode);
            //subsection.CheckboxLabeled("SC_KeyholderMode_dev".Translate(), ref keyholderMode);
            if (keyholderMode)
            { 
                bool clicked = subsection.ButtonTextLabeled("SC_KeyholderModeUndo_Label".Translate(), "SC_KeyholderModeUndo_Button".Translate());
                if (clicked)
                    Find.WindowStack.Add(new Dialog_UnlockSelfControl());
            }
                

            listingStandard.EndHiddenSection(subsection, subsectionHeight);

            var imageRect = viewRect.RightHalf().TopHalf().ContractedBy(50f);
            //imageRect.height = imageRect.width;
            Widgets.DrawTextureFitted(imageRect, keyholderMode ? Textures_Custom.KeyOn : Textures_Custom.KeyOff, 1f);

            cachedScrollHeight = listingStandard.CurHeight;
            listingStandard.End();
            if (needToScroll)
            {
                Widgets.EndScrollView();
            }
            GUI.color = colorSave;
            Text.Anchor = anchorSave;
        }
    }
}
