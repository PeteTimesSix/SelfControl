using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.SelfControl.UI
{
    public class ListableOption_TooManyMods : ListableOption
    {
        public string uiHighlightTagAccessible;

        public ListableOption_TooManyMods(string label, Action action, string uiHighlightTag = null) : base(label, action, uiHighlightTag)
        {
            uiHighlightTagAccessible = uiHighlightTag;
        }

        public static Color INTERNAL_COLOR = new Color(0.65f, 0f, 0f, 0.65f);
        public static Color OUTLINE_COLOR = new Color(1.0f, 0.25f, 0.25f, 1.0f);
        public static Color TEXT_COLOR = new Color(1.0f, 0.75f, 0.75f, 1.0f);
        public static float DESIRED_HEIGHT = (45f * 3f) + (7f * 2f);

        public override float DrawOption(Vector2 pos, float width)
        {
            var startAnchor = Text.Anchor;
            var startColor = GUI.color;

            float height = DESIRED_HEIGHT;
            Rect rect = new Rect(pos.x, pos.y, width, height);

            rect = rect.ContractedBy(2f);

            Widgets.DrawBoxSolidWithOutline(rect, INTERNAL_COLOR, OUTLINE_COLOR);

            GUI.color = TEXT_COLOR;
            Text.Anchor = TextAnchor.LowerCenter;
            Widgets.Label(rect.TopHalf(), label);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect.BottomHalf(), "SC_TooManyMods_Text".Translate(ModsConfig.ActiveModsInLoadOrder.Count(), SelfControlMod.maxModCount));


            if (Widgets.ButtonInvisible(rect, true))
            {
                action();
            }
            if (uiHighlightTagAccessible != null)
            {
                UIHighlighter.HighlightOpportunity(rect, uiHighlightTagAccessible);
            }

            Text.Anchor = startAnchor;
            GUI.color = startColor;
            return height;
        }
    }
}
