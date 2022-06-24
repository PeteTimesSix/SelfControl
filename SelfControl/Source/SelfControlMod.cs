using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using RimWorld;
using PeteTimesSix.SelfControl.Utilities;
using System.Collections.ObjectModel;

namespace PeteTimesSix.SelfControl
{
    public class SelfControlMod : Mod
    {
        public static SelfControlMod ModSingleton { get; private set; }
        public static SelfControl_Settings Settings { get; private set; }

        private int? _modCountCached;
        public int ModCount { 
            get 
            {
                if (!_modCountCached.HasValue)
                {
                    _modCountCached = ModsConfig.ActiveModsInLoadOrder.Where(m => !m.IsIgnoredBySelfControl()).Count();
                }
                return _modCountCached.Value;
            } 
        }

        private ReadOnlyDictionary<ModMetaData, long> _modFileSizesCached;
        public ReadOnlyDictionary<ModMetaData, long> ModFileSizes { 
            get 
            {
                if(_modFileSizesCached == null) 
                {
                    Dictionary<ModMetaData, long> modFileSizes = new Dictionary<ModMetaData, long>();
                    foreach(var mod in ModsConfig.ActiveModsInLoadOrder)
                    {
                        modFileSizes[mod] = mod.GetTotalFileSize();
                    }
                    _modFileSizesCached = new ReadOnlyDictionary<ModMetaData, long>(modFileSizes);
                }
                return _modFileSizesCached;
            } 
        }

        public SelfControlMod(ModContentPack content) : base(content)
        {
            ModSingleton = this;
            Settings = GetSettings<SelfControl_Settings>();

            var harmony = new Harmony("PeteTimesSix.SelfControl");
            harmony.PatchAll();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Settings.DoSettingsWindowContents(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }

    [StaticConstructorOnStartup]
    public static class SelfControl_PostInit
    {
        static SelfControl_PostInit()
        {
            //just for you, Bradson
        }
    }
}
