using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace PeteTimesSix.SelfControl.Rimworld
{
    [StaticConstructorOnStartup]
    public static class Textures_Custom
    {
        public static Texture2D KeyOff { get; internal set; } = ContentFinder<Texture2D>.Get("lock_and_key", true);
        public static Texture2D KeyOn { get; internal set; } = ContentFinder<Texture2D>.Get("lock", true);
        public static Texture2D TestTexture { get; internal set; } = ContentFinder<Texture2D>.Get("testTexture", true);
        public static Texture2D ChestBack { get; internal set; } = ContentFinder<Texture2D>.Get("archochest", true);
        public static Texture2D ChestLid { get; internal set; } = ContentFinder<Texture2D>.Get("archolid", true);
        public static Texture2D ChestKey { get; internal set; } = ContentFinder<Texture2D>.Get("archokey", true);
        public static Texture2D Frogger { get; internal set; } = ContentFinder<Texture2D>.Get("frogger", true);
        public static Texture2D ArrowLeft { get; internal set; } = ContentFinder<Texture2D>.Get("ArrowLeft", true);
        public static Texture2D ArrowRight { get; internal set; } = ContentFinder<Texture2D>.Get("ArrowRight", true);
        public static Texture2D ArrowUp { get; internal set; } = ContentFinder<Texture2D>.Get("ArrowUp", true);
        public static Texture2D ArrowDown { get; internal set; } = ContentFinder<Texture2D>.Get("ArrowDown", true);
    }
}
