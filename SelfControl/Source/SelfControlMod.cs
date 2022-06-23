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

namespace PeteTimesSix.SelfControl
{
    public class SelfControlMod : Mod
    {
        public static int maxModCount = 100;

        public static SelfControlMod ModSingleton { get; private set; }


        public SelfControlMod(ModContentPack content) : base(content)
        {
            ModSingleton = this;

            var harmony = new Harmony("PeteTimesSix.SelfControl");
            harmony.PatchAll();
        }
    }

    [StaticConstructorOnStartup]
    public static class ResearchReinvented_PostInit
    {
        static ResearchReinvented_PostInit()
        {
        }
    }
}
