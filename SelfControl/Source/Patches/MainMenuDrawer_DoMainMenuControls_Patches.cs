using HarmonyLib;
using PeteTimesSix.SelfControl.UI;
using PeteTimesSix.SelfControl.Utilities;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PeteTimesSix.SelfControl.Patches
{
    [HarmonyPatch(typeof(MainMenuDrawer), nameof(MainMenuDrawer.DoMainMenuControls))]
    public static class MainMenuDrawer_DoMainMenuControls_Patches
    {

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) 
        {
            var enumerator = instructions.GetEnumerator();

            var options_instructions = new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldstr, "Options")
            };

            var check_instructions = new CodeInstruction[] {
                //value is on stack
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MainMenuDrawer_DoMainMenuControls_Patches), nameof(MainMenuDrawer_DoMainMenuControls_Patches.ClearCheck)))
            };


            var iteratedOver = TranspilerUtils.IterateTo(enumerator, options_instructions, out CodeInstruction[] matchedInstructions, out bool found);

            foreach (var instruction in iteratedOver)
                yield return instruction;

            if (!found)
            {
                Log.Warning("failed to apply patch (instructions not found)");
                goto finalize;
            }
            else
            {
                foreach (var extraInstruction in check_instructions)
                    yield return extraInstruction;

                goto finalize;
            }

        finalize:
            //output remaining instructions
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private static void ClearCheck(List<ListableOption> options)
        {
            if (ModsConfig.ActiveModsInLoadOrder.Count() > SelfControlMod.maxModCount)
            {
                options.Clear();
                options.Add(new ListableOption_TooManyMods("SC_TooManyMods_Header".Translate(), () => { }));
            }
        }
    }
}
