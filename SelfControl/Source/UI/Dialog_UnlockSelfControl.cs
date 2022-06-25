using System;
using UnityEngine;
using Verse;
using System.Linq;
using System.Collections.Generic;
using PeteTimesSix.SelfControl.Rimworld;
using PeteTimesSix.SelfControl.Utilities;
using RimWorld;
using Verse.Sound;
using Verse.Steam;

namespace PeteTimesSix.SelfControl.UI
{
    [HotSwappable]
    public class Dialog_UnlockSelfControl : Window
    {
        private const float LANE_WIDTH = 640f - 100f;
        private const float MAX_BLOCK_SIZE = 200f;
        private const float MIN_BLOCK_SIZE = 30f;
        private const float PLAYER_HITBOX_WIDTH = 5f;
        private const float PLAYER_WIDTH = 24f;
        private const float PLAYER_HEIGHT = 24f;

        private const float BIG_GAP_SIZE = 100f;
        private const float SMALL_GAP_SIZE = 30f;
        private const float IMPASSABLE_GAP_SIZE = 3f;

        private const float SMALL_GAPS_EASY = 4;
        private const float SMALL_GAPS_HARD = 3;
        private const float SMALL_GAPS_AVG = (SMALL_GAPS_EASY + SMALL_GAPS_HARD) / 2f;
        private const float SMALL_GAPS_DIFF = (SMALL_GAPS_EASY - SMALL_GAPS_HARD);

        private const float BIG_GAPS_EASY = 2;
        private const float BIG_GAPS_HARD = 0;
        private const float BIG_GAPS_AVG = (BIG_GAPS_EASY + BIG_GAPS_HARD) / 2f;
        private const float BIG_GAPS_DIFF = (BIG_GAPS_EASY - BIG_GAPS_HARD);

        private const float SPEED_HORIZ_EASY = 0.1f;
        private const float SPEED_HORIZ_HARD = 0.2f;
        private const float SPEED_HORIZ_DIFF = (SPEED_HORIZ_EASY - SPEED_HORIZ_HARD);
        private const float SPEED_VERT_EASY = 0.05f;
        private const float SPEED_VERT_HARD = 0.15f;

        private const float LANE_GAP = 2f;
        private const float LANE_ACTUAL_HEIGHT = 30f;
        private const float PLAYER_OFFSET = (LANE_ACTUAL_HEIGHT - PLAYER_HEIGHT) / 2f;

        private const long HOP_COOLDOWN = 120;
        private const float HOP_SPEED = 0.65f;
        private const float STRAFE_SPEED = 0.15f;

        private const long PRE_PHASE_TIME = 300;

        private class FroggerModBlock
        {
            public float sizeInUnits;
            public string name;
            public float offset;
            public float position;
            public float followGap;

            public FroggerModBlock(float size, string name, float offset)
            {
                this.sizeInUnits = size;
                this.name = name;
                this.offset = offset;
                this.position = offset;
            }

            public FroggerModBlock CloneWhenOffscreen(float laneWidth)
            {
                if (position + sizeInUnits >= laneWidth)
                {
                    return new FroggerModBlock(sizeInUnits, name, offset) { position = position - laneWidth };
                }
                else if (position <= 0)
                {
                    return new FroggerModBlock(sizeInUnits, name, offset) { position = position + laneWidth };
                }
                return null;
            }
        }

        private class FroggerLane 
        {
            public List<FroggerModBlock> blocks = new List<FroggerModBlock>();

            public float TotalUnitSize => blocks.Sum(b => b.sizeInUnits);
            public float TotalSpaceUsed => blocks.Sum(b => b.sizeInUnits + b.followGap);
            public float TotalSpaceRemaining => LANE_WIDTH - TotalSpaceUsed;

            public float speed = 0f;
            public bool backwards = false;

            internal void MoveBlocks(long tick, float playAreaWidth)
            {
                var laneWidth = TotalSpaceUsed >= playAreaWidth ? TotalSpaceUsed : playAreaWidth;
                foreach(var block in blocks) 
                {
                    if (!backwards)
                    {
                        block.position = (block.offset + tick * this.speed) % laneWidth;
                        //if (block.block.position > playAreaWidth)
                        //    block.block.position -= playAreaWidth;
                    }
                    else
                    {
                        block.position = (block.offset - tick * this.speed) % laneWidth;
                        //if (block.block.position + block.block.sizeInUnits + block.followGap < 0)
                        //    block.block.position += playAreaWidth;
                    }
                }
            }
        }

        private class FroggerPlayer
        {
            public Vector2 position;
            public int lane;
            public FroggerPlayerState state = FroggerPlayerState.Standing;
            public long hopCooldown = 0;

            internal bool Intersects(FroggerModBlock block)
            {
                var b_x1 = block.position;
                var b_x2 = block.position + block.sizeInUnits;
                var p_x1 = position.x - (PLAYER_HITBOX_WIDTH / 2f);
                var p_x2 = position.x + (PLAYER_HITBOX_WIDTH / 2f);
                return !((p_x1 < b_x1 && p_x1 < b_x2 && p_x2 < b_x1 && p_x1 < b_x2) || (p_x1 > b_x1 && p_x1 > b_x2 && p_x2 > b_x1 && p_x1 > b_x2));
            }
        }
        private enum FroggerPlayerState
        {
            Standing,
            HoppingForward,
            HoppingBackward,
            Inactive,
            Dead,
        }

        private enum FroggerGameState 
        {
            BeforePlay,
            Playing,
            PreVictory,
            Victory,
            VictoryChestOpen,
            PreDefeat,
            Defeat,
        }

        public override Vector2 InitialSize => new Vector2(640f, 460f);

        private long tick = 0;
        private long prePhaseTicks = 0;

        private FroggerGameState gameState = FroggerGameState.BeforePlay;
        private List<FroggerLane> gameLanes;
        private FroggerPlayer gamePlayer;
        private FroggerModBlock lastCrashedInto;

        public Dialog_UnlockSelfControl() : base()
        {
            this.doCloseButton = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            tick++;
            var startAnchor = Text.Anchor;
            var startColor = GUI.color;

            if(gameState == FroggerGameState.PreVictory)
            {
                prePhaseTicks++;
                if(prePhaseTicks > PRE_PHASE_TIME)
                {
                    prePhaseTicks = 0;
                    gameState = FroggerGameState.Victory;
                }
            }
            else if(gameState == FroggerGameState.PreDefeat)
            {
                prePhaseTicks++;
                if (prePhaseTicks > PRE_PHASE_TIME)
                {
                    prePhaseTicks = 0;
                    gameState = FroggerGameState.Defeat;
                }
            }

            switch (gameState) 
            {
                case FroggerGameState.BeforePlay:
                    DoBeforePlayContents(inRect);
                    break;
                case FroggerGameState.PreVictory:
                case FroggerGameState.PreDefeat:
                case FroggerGameState.Playing:
                    DoFrogger(inRect);
                    break;
                case FroggerGameState.Victory:
                case FroggerGameState.VictoryChestOpen:
                    DoVictoryScreen(inRect);
                    break;    
                case FroggerGameState.Defeat:
                    DoDefeatScreen(inRect);
                    break;
            }

            Text.Anchor = startAnchor;
            GUI.color = startColor;
        }


        private void DoBeforePlayContents(Rect inRect)
        {
            Rect buttonRect = new Rect(inRect.center - new Vector2(40f, 12f), new Vector2(80f, 24f));
            var clicked = Widgets.ButtonText(buttonRect, "SC_Start".Translate());
            if(clicked)
            {
                SetupGame();
            }
        }

        private void SetupGame()
        {
            var sizes = SelfControlMod.ModSingleton.ModFileSizes;
            var largestValue = sizes.Values.Max();
            var blocks = new List<FroggerModBlock>();
            var totalSizeInUnits = 0f;

            for (int i = 0; i < 1; i++)
            {
                foreach (var mod in sizes.Keys)
                {
                    var modSize = sizes[mod];
                    var sizeInUnits = (float)Math.Ceiling((modSize / (double)largestValue) * MAX_BLOCK_SIZE);
                    if (sizeInUnits < MIN_BLOCK_SIZE)
                        sizeInUnits = MIN_BLOCK_SIZE;
                    var modBlock = new FroggerModBlock(sizeInUnits, mod.Name, 0);
                    totalSizeInUnits += sizeInUnits;
                    blocks.Add(modBlock);
                }
            }
            blocks.Shuffle();

            var initialCount = blocks.Count;
            var lanes = new List<FroggerLane>();
            lanes.Add(new FroggerLane());

            int index = 1;
            while (blocks.Count > 0)
            {
                var lane = new FroggerLane();
                var difficultyModifier = (1f - (blocks.Count / (float)initialCount));
                lane.speed = SPEED_HORIZ_EASY - (SPEED_HORIZ_DIFF * difficultyModifier);
                lane.backwards = index % 2 == 0;
                var targetSmallGaps = SMALL_GAPS_EASY - (SMALL_GAPS_DIFF * difficultyModifier);
                var targetBigGaps = BIG_GAPS_EASY - (BIG_GAPS_DIFF * difficultyModifier);
                FillLane(blocks, targetSmallGaps, targetBigGaps, lane);
                lanes.Add(lane);
                index++;
            }

            lanes.Add(new FroggerLane());

            gameLanes = lanes;
            gamePlayer = new FroggerPlayer();
            gamePlayer.lane = 0;
            gamePlayer.position = new Vector2(LANE_WIDTH / 2f, 0);

            gameState = FroggerGameState.Playing;
        }

        private static void FillLane(List<FroggerModBlock> candidateBlocks, float targetSmallGaps, float targetBigGaps, FroggerLane lane)
        {
            while (candidateBlocks.Any() && lane.TotalSpaceUsed < LANE_WIDTH)
            {
                var block = candidateBlocks.Pop();
                var gapSize = IMPASSABLE_GAP_SIZE;
                bool gapSet = false;
                if(targetBigGaps > 0)
                {
                    if (targetBigGaps >= 1 || Rand.Chance(targetBigGaps))
                    {
                        gapSet = true;
                        gapSize = BIG_GAP_SIZE;
                    }
                    targetBigGaps -= 1;
                }
                if(!gapSet && targetSmallGaps > 0)
                {
                    if (targetSmallGaps >= 1 || Rand.Chance(targetSmallGaps))
                    {
                        gapSet = true;
                        gapSize = SMALL_GAP_SIZE;
                    }
                    targetSmallGaps -= 1;
                }
                block.followGap = gapSize;
                lane.blocks.Add(block);
            }
            lane.blocks.Shuffle();
            float posAccum = 0;
            for(int i = 0; i < lane.blocks.Count; i++) 
            {
                var block = lane.blocks[i];
                block.offset = posAccum;
                posAccum += block.sizeInUnits + block.followGap;
            }
        }

        private FroggerGameState DoPlayerMovement(float playRectWidth, bool left, bool right, bool up, bool down) 
        {
            var lane = gameLanes[gamePlayer.lane];
            switch (gamePlayer.state) 
            {
                case FroggerPlayerState.HoppingForward:
                    gamePlayer.position.y += HOP_SPEED;
                    if(gamePlayer.position.y >= LANE_ACTUAL_HEIGHT + LANE_GAP) 
                    {
                        gamePlayer.lane++;
                        gamePlayer.position.y = 0;
                        gamePlayer.hopCooldown = HOP_COOLDOWN;
                        gamePlayer.state = FroggerPlayerState.Standing;
                        lane = gameLanes[gamePlayer.lane];
                    }
                    break;
                case FroggerPlayerState.HoppingBackward:
                    gamePlayer.position.y -= HOP_SPEED;
                    if (gamePlayer.position.y <= -(LANE_ACTUAL_HEIGHT + LANE_GAP))
                    {
                        gamePlayer.lane--;
                        gamePlayer.position.y = 0;
                        gamePlayer.hopCooldown = HOP_COOLDOWN;
                        gamePlayer.state = FroggerPlayerState.Standing;
                        lane = gameLanes[gamePlayer.lane];
                    }
                    break;
                case FroggerPlayerState.Standing:
                    if (!lane.backwards)
                        gamePlayer.position.x += lane.speed;
                    else
                        gamePlayer.position.x -= lane.speed;
                    if (right)
                        gamePlayer.position.x += STRAFE_SPEED;
                    if (left)
                        gamePlayer.position.x -= STRAFE_SPEED;
                    if (gamePlayer.hopCooldown <= 0)
                    {
                        if(down && gamePlayer.lane < gameLanes.Count - 1)
                            gamePlayer.state = FroggerPlayerState.HoppingForward;
                        else if(up && gamePlayer.lane > 0)
                            gamePlayer.state = FroggerPlayerState.HoppingBackward;
                    }
                    else
                        gamePlayer.hopCooldown--;
                    break;
                case FroggerPlayerState.Inactive:
                case FroggerPlayerState.Dead:
                    if (!lane.backwards)
                        gamePlayer.position.x += lane.speed;
                    else
                        gamePlayer.position.x -= lane.speed;
                    break;
            }

            if (gamePlayer.lane >= gameLanes.Count - 1)
            {
                gamePlayer.state = FroggerPlayerState.Inactive;
                return FroggerGameState.PreVictory;
            }
            else 
            {
                if(gamePlayer.state == FroggerPlayerState.Standing)
                {
                    var laneWidth = lane.TotalSpaceUsed >= playRectWidth ? lane.TotalSpaceUsed : playRectWidth;
                    var crashedInto = lane.blocks.FirstOrDefault(b => gamePlayer.Intersects(b) || (b.CloneWhenOffscreen(laneWidth) != null && gamePlayer.Intersects(b.CloneWhenOffscreen(laneWidth))));
                    if (crashedInto != null)
                    {
                        gamePlayer.state = FroggerPlayerState.Dead;
                        lastCrashedInto = crashedInto; 
                        return FroggerGameState.PreDefeat;
                    }
                }
            }

            return gameState;
        }

        private void DoFrogger(Rect inRect)
        {
            var controls = inRect.TopPartPixels(50f);
            controls.x = controls.x + (controls.width / 2f) - 50f;
            controls.width = 100f;
            var leftArrowRect = new Rect(controls.x, controls.y, 25f, controls.height);
            var rightArrowRect = new Rect(controls.x + 75f, controls.y, 25f, controls.height);
            var upArrowRect = new Rect(controls.x + 25f, controls.y, 50f, controls.height / 2f);
            var downArrowRect = new Rect(controls.x + 25f, controls.y + controls.height / 2f, 50f, controls.height / 2f);

            var playRect = inRect.ContractedBy(50f);

            Widgets.ButtonImage(leftArrowRect, Textures_Custom.ArrowLeft, false);
            Widgets.ButtonImage(rightArrowRect, Textures_Custom.ArrowRight, false);
            Widgets.ButtonImage(upArrowRect, Textures_Custom.ArrowUp, false);
            Widgets.ButtonImage(downArrowRect, Textures_Custom.ArrowDown, false);

            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) || (Mouse.IsOver(leftArrowRect) && Input.GetMouseButton(0)); //Widgets.ButtonText(testButtonRect.LeftHalf().LeftHalf(), "Left");
            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) || (Mouse.IsOver(rightArrowRect) && Input.GetMouseButton(0)); //Widgets.ButtonText(testButtonRect.LeftHalf().RightHalf(), "Right");
            bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || (Mouse.IsOver(upArrowRect) && Input.GetMouseButton(0)); //Widgets.ButtonText(testButtonRect.RightHalf().LeftHalf(), "Forward");
            bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || (Mouse.IsOver(downArrowRect) && Input.GetMouseButton(0)); // Widgets.ButtonText(testButtonRect.RightHalf().RightHalf(), "Back");
            gameState = DoPlayerMovement(playRect.width, left, right, up, down);

            GUI.BeginClip(playRect);

            var playerPos = (gamePlayer.lane * (float)(LANE_ACTUAL_HEIGHT + LANE_GAP)) + gamePlayer.position.y;
            var laneOffset = 0f;
            var totalHeight = (float)((LANE_ACTUAL_HEIGHT + LANE_GAP) * gameLanes.Count);
            if (playRect.height > totalHeight)
            {
                laneOffset = ((playRect.height - totalHeight) / 2f);
            }
            else
            {
                if (playerPos > playRect.height / 2f)
                {
                    var maxOffset = totalHeight - (playRect.height);
                    laneOffset = -(playerPos - (playRect.height / 2f));
                    if (playerPos - (playRect.height / 2f) > maxOffset)
                        laneOffset = -maxOffset;
                }
            }

            Text.Anchor = TextAnchor.MiddleCenter;

            Rect playerLaneRect = new Rect();

            for(int i = 0; i < gameLanes.Count; i++)
            {
                var lane = gameLanes[i];
                lane.MoveBlocks(tick, playRect.width);

                var laneRect = new Rect(0, laneOffset, playRect.width, LANE_ACTUAL_HEIGHT);

                if (laneOffset + LANE_ACTUAL_HEIGHT < 0 || laneOffset > playRect.height)
                {
                    laneOffset += LANE_ACTUAL_HEIGHT + LANE_GAP;
                    continue;
                }

                Widgets.DrawBoxSolid(laneRect, new Color(0.2f, 0.2f, 0.3f));

                var laneWidth = lane.TotalSpaceUsed >= playRect.width ? lane.TotalSpaceUsed : playRect.width;



                foreach (var block in lane.blocks)
                {
                    var blockRect = new Rect(block.position, laneOffset + 2f, block.sizeInUnits, LANE_ACTUAL_HEIGHT - 4f);
                    GUI.color = Color.white;
                    Widgets.DrawAtlas(blockRect, Widgets.ButtonSubtleAtlas);
                    //GUI.DrawTexture(blockRect, Textures_Custom.TestTexture);
                    GUI.color = Color.white;
                    Widgets.LabelFit(blockRect.ContractedBy(2f), block.name);
                    var cloneBlock = block.CloneWhenOffscreen(laneWidth);
                    if (cloneBlock != null)
                    {
                        var cloneBlockRect = new Rect(cloneBlock.position, laneOffset + 2f, cloneBlock.sizeInUnits, LANE_ACTUAL_HEIGHT - 4f);
                        GUI.color = Color.white;
                        Widgets.DrawAtlas(cloneBlockRect, Widgets.ButtonSubtleAtlas);
                        //GUI.DrawTexture(cloneBlockRect, Textures_Custom.TestTexture);
                        GUI.color = Color.white;
                        Widgets.LabelFit(cloneBlockRect.ContractedBy(2f), block.name);
                    }
                }

                if (i == gamePlayer.lane)
                    playerLaneRect = laneRect;

                laneOffset += LANE_ACTUAL_HEIGHT + LANE_GAP;
            }

            var playerRect = new Rect(playerLaneRect.x + gamePlayer.position.x - (PLAYER_WIDTH / 2f), playerLaneRect.y + gamePlayer.position.y + PLAYER_OFFSET, PLAYER_WIDTH, PLAYER_HEIGHT);
            GUI.color = gamePlayer.state == FroggerPlayerState.Dead ? Color.red : Color.white;
            GUI.DrawTexture(playerRect, Textures_Custom.Frogger);
            GUI.color = Color.yellow;
            //var playerHitboxRect = new Rect(playerLaneRect.x + gamePlayer.position.x - (PLAYER_HITBOX_WIDTH / 2f), playerLaneRect.y + gamePlayer.position.y + PLAYER_OFFSET, PLAYER_HITBOX_WIDTH, PLAYER_HEIGHT);
            //GUI.DrawTexture(playerHitboxRect, Textures_Custom.TestTexture);

            GUI.EndClip();
        }

        private void DoVictoryScreen(Rect inRect)
        {
            Rect chestRect = new Rect(inRect.center.x - 128f, inRect.center.y - 128f, 256f, 256f);
            if (gameState == FroggerGameState.VictoryChestOpen)
            {
                var lidRect = chestRect.TopHalf();
                lidRect.y -= 128f;
                GUI.DrawTexture(lidRect, Textures_Custom.ChestLid, ScaleMode.ScaleToFit);
            }
            GUI.DrawTexture(chestRect, Textures_Custom.ChestBack, ScaleMode.ScaleToFit);
            if(gameState == FroggerGameState.Victory) 
            {
                GUI.DrawTexture(chestRect.TopHalf(), Textures_Custom.ChestLid, ScaleMode.ScaleToFit);
                if (Widgets.ButtonInvisible(chestRect.TopHalf()))
                {
                    gameState = FroggerGameState.VictoryChestOpen;
                    SoundDefOf.ExecuteTrade.PlayOneShotOnCamera(null);
                }
            }
            else if(gameState == FroggerGameState.VictoryChestOpen)
            {
                GUI.DrawTexture(chestRect.TopHalf().ContractedBy(32f), Textures_Custom.ChestKey, ScaleMode.ScaleToFit);
                if (Widgets.ButtonInvisible(chestRect.TopHalf()))
                {
                    SoundDefOf.Tick_High.PlayOneShotOnCamera(null);
                    SelfControlMod.Settings.keyholderMode = false;
                    this.Close();
                }
            }
        }

        private void DoDefeatScreen(Rect inRect)
        {
            Text.Anchor = TextAnchor.UpperCenter;
            var textRect = inRect.ContractedBy(50f);
            Widgets.Label(textRect, "SC_DefeatedBy_Header".Translate());
            textRect.y += 25f;
            Widgets.Label(textRect, lastCrashedInto.name);
            textRect.y += 25f;
            Widgets.Label(textRect, "SC_DefeatedBy_Footer".Translate());

            Rect buttonRect = new Rect(inRect.center - new Vector2(40f, 12f), new Vector2(80f, 24f));
            var clicked = Widgets.ButtonText(buttonRect, "SC_StartAgain".Translate());
            if (clicked)
            {
                SetupGame();
            }
        }
    }
}