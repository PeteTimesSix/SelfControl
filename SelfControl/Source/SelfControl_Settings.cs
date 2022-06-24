using PeteTimesSix.SelfControl.Rimworld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.SelfControl
{
    public class SelfControl_Settings : ModSettings
    {
        public const int DEFAULT_MAX_MODS = 100;

        public int maxModCount = DEFAULT_MAX_MODS;
        public bool keyholderMode = false;
        public int maxModCountKeyholder = DEFAULT_MAX_MODS;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref maxModCount, "maxModCount", DEFAULT_MAX_MODS);
            Scribe_Values.Look(ref keyholderMode, "keyholderMode", false);
            Scribe_Values.Look(ref maxModCountKeyholder, "maxModCountKeyholder", DEFAULT_MAX_MODS);
        }
        internal void DoSettingsWindowContents(Rect inRect)
        {
            var currentMax = keyholderMode ? Math.Min(maxModCountKeyholder, DEFAULT_MAX_MODS) : DEFAULT_MAX_MODS;
            maxModCount = (int)Widgets.HorizontalSlider(inRect, maxModCount, 5, currentMax, label: "SC_settings_maxModCount_label".Translate());

            Widgets.CheckboxLabeled(inRect, "SC_settings_keyholderMode_label", ref keyholderMode, disabled: keyholderMode == true, Textures.KEYHOLDER_ON, Textures.KEYHOLDER_OFF);
        }
    }
}
