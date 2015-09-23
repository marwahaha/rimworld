﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace AutoEquip
{
    public class Dialog_PawnApparelComparer : Window
    {
        private Pawn pawn;
        private Apparel apparel;

        public Dialog_PawnApparelComparer(Pawn pawn, Apparel apparel)
        {
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.doCloseButton = true;
            
            this.pawn = pawn;
            this.apparel = apparel;
        }

        public override Vector2 InitialWindowSize
        {
            get
            {
                return new Vector2(500f, 700f);
            }
        }

        private Vector2 scrollPosition;

        public override void DoWindowContents(Rect windowRect)
        {
            MapComponent_AutoEquip mapComponent = MapComponent_AutoEquip.Get;
            Saveable_PawnNextApparelConfiguration conf = mapComponent.GetCache(this.pawn);
            List<Apparel> allApparels = new List<Apparel>(Find.ListerThings.ThingsInGroup(ThingRequestGroup.Apparel).OfType<Apparel>());
            foreach (Pawn pawn in Find.ListerPawns.FreeColonists)
            {
                foreach (Apparel pawnApparel in pawn.apparel.WornApparel)
                    if (pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(pawnApparel))
                        allApparels.Add(pawnApparel);
            }

            allApparels = allApparels.Where(i => !ApparelUtility.CanWearTogether(this.apparel.def, i.def)).ToList();

            Rect groupRect = windowRect.ContractedBy(10f);
            groupRect.height -= 100;
            GUI.BeginGroup(groupRect);

            float apparelScoreWidth = 100f;
            float apparelGainWidth = 100f;
            float apparelLabelWidth = (groupRect.width - apparelScoreWidth - apparelGainWidth) / 3 - 8f - 8f;
            float apparelEquipedWidth = apparelLabelWidth;
            float apparelOwnerWidth = apparelLabelWidth;            

            Rect itemRect = new Rect(groupRect.xMin + 4f, groupRect.yMin, groupRect.width - 8f, 28f);

            this.DrawLine(ref itemRect,
                null, "Apparel", apparelLabelWidth,
                null, "Equiped", apparelEquipedWidth,
                null, "Target", apparelOwnerWidth,
                "Score", apparelScoreWidth,
                "Gain", apparelGainWidth);

            groupRect.yMin += itemRect.height;
            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMin, groupRect.width);
            groupRect.yMin += 4f;
            groupRect.height -= 4f;
            groupRect.height -= Text.LineHeight * 1.2f * 3f;

            Rect viewRect = new Rect(groupRect.xMin, groupRect.yMin, groupRect.width - 16f, allApparels.Count() * 28f + 16f);
            if (viewRect.height < groupRect.height)
                groupRect.height = viewRect.height;

            Rect listRect = viewRect.ContractedBy(4f);

            Widgets.BeginScrollView(groupRect, ref scrollPosition, viewRect);

            allApparels = allApparels.OrderByDescending(i => { float g; if (conf.ApparelScoreGain(i, out g)) return g; return 0f; }).ToList();

            foreach (Apparel currentAppel in allApparels)
            {
                itemRect = new Rect(listRect.xMin, listRect.yMin, listRect.width, 28f);
                if (Mouse.IsOver(itemRect))
                {
                    GUI.color = ITab_Pawn_AutoEquip.HighlightColor;
                    GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                    GUI.color = Color.white;
                }

                Pawn equiped = null;
                Pawn target = null;

                foreach (Pawn pawn in Find.ListerPawns.FreeColonists)
                {
                    foreach (Apparel a in pawn.apparel.WornApparel)
                        if (a == currentAppel)
                        {
                            equiped = pawn;
                            break;
                        }

                    foreach (Apparel a in mapComponent.GetCache(pawn).calculedApparel)
                        if (a == currentAppel)
                        {
                            target = pawn;
                            break;
                        }

                    if ((equiped != null) &&
                        (target != null))
                        break;
                }

                float gain;
                if (conf.ApparelScoreGain(currentAppel, out gain))
                    this.DrawLine(ref itemRect,
                        currentAppel, currentAppel.LabelCap, apparelLabelWidth,
                        equiped, equiped == null ? null : equiped.LabelCap, apparelEquipedWidth,
                        target, target == null ? null : target.LabelCap, apparelOwnerWidth,
                        conf.ApparelScoreRaw(currentAppel).ToString("N5"), apparelScoreWidth,
                        gain.ToString("N5"), apparelGainWidth);
                else
                    this.DrawLine(ref itemRect,
                        currentAppel, currentAppel.LabelCap, apparelLabelWidth,
                        equiped, equiped == null ? null : equiped.LabelCap, apparelEquipedWidth,
                        target, target == null ? null : target.LabelCap, apparelOwnerWidth,
                        conf.ApparelScoreRaw(currentAppel).ToString("N5"), apparelScoreWidth,
                        "No Allow", apparelGainWidth);

                listRect.yMin = itemRect.yMax;
            }

            Widgets.EndScrollView();

            Widgets.DrawLineHorizontal(groupRect.xMin, groupRect.yMax, groupRect.width);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
        }

        private void DrawLine(ref Rect itemRect,
            Apparel apparelThing, string apparelText, float textureWidth,
            Pawn apparelEquipedThing, string apparelEquipedText, float apparelEquipedWidth,
            Pawn apparelOwnerThing, string apparelOwnerText, float apparelOwnerWidth,
            string apparelScoreText, float apparelScoreWidth,
            string apparelGainText, float apparelGainWidth)
        {
            Rect fieldRect;
            if (apparelThing != null) {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelText))
                    TooltipHandler.TipRegion(fieldRect, apparelText);
                if ((apparelThing.def.DrawMatSingle != null) && 
                    (apparelThing.def.DrawMatSingle.mainTexture != null))
                    Widgets.ThingIcon(fieldRect, apparelThing);
                if (Widgets.InvisibleButton(fieldRect))
                {
                    this.Close(true);
                    Find.MainTabsRoot.EscapeCurrentTab(true);
                    if (apparelEquipedThing != null)
                    {
                        Find.CameraMap.JumpTo(apparelEquipedThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelEquipedThing.SpawnedInWorld)
                            Find.Selector.Select(apparelEquipedThing, true, true);
                    }
                    else
                    {
                        Find.CameraMap.JumpTo(apparelThing.PositionHeld);
                        Find.Selector.ClearSelection();
                        if (apparelThing.SpawnedInWorld)
                            Find.Selector.Select(apparelThing, true, true);
                    }
                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, textureWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }
            itemRect.xMin += textureWidth;
            
            if (apparelEquipedThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelEquipedText))
                    TooltipHandler.TipRegion(fieldRect, apparelEquipedText);
                if ((apparelEquipedThing.def.DrawMatSingle != null) &&
                    (apparelEquipedThing.def.DrawMatSingle.mainTexture != null))
                    Widgets.ThingIcon(fieldRect, apparelEquipedThing);
                if (Widgets.InvisibleButton(fieldRect))
                {
                    this.Close(true);
                    Find.MainTabsRoot.EscapeCurrentTab(true);
                    Find.CameraMap.JumpTo(apparelEquipedThing.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (apparelEquipedThing.SpawnedInWorld)
                        Find.Selector.Select(apparelEquipedThing, true, true);
                    return;
                }
            } 
            else
            {
                if (!string.IsNullOrEmpty(apparelEquipedText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelEquipedWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelText);
                }
            }
            itemRect.xMin += apparelEquipedWidth;
            
            if (apparelOwnerThing != null)
            {
                fieldRect = new Rect(itemRect.xMin, itemRect.yMin, itemRect.height, itemRect.height);
                if (!string.IsNullOrEmpty(apparelOwnerText))
                    TooltipHandler.TipRegion(fieldRect, apparelOwnerText);
                if ((apparelOwnerThing.def.DrawMatSingle != null) &&
                    (apparelOwnerThing.def.DrawMatSingle.mainTexture != null))
                    Widgets.ThingIcon(fieldRect, apparelOwnerThing);
                if (Widgets.InvisibleButton(fieldRect))
                {
                    this.Close(true);
                    Find.MainTabsRoot.EscapeCurrentTab(true);
                    Find.CameraMap.JumpTo(apparelOwnerThing.PositionHeld);
                    Find.Selector.ClearSelection();
                    if (apparelOwnerThing.SpawnedInWorld)
                        Find.Selector.Select(apparelOwnerThing, true, true);
                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(apparelOwnerText))
                {
                    fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelOwnerWidth, itemRect.height);
                    Text.Anchor = TextAnchor.UpperLeft;
                    Widgets.Label(fieldRect, apparelOwnerText);
                }
            }
            itemRect.xMin += apparelOwnerWidth;

            fieldRect = new Rect(itemRect.xMin, itemRect.yMin, apparelScoreWidth, itemRect.height);
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(fieldRect, apparelScoreText);
            if (apparelThing != null)
            {
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.InvisibleButton(fieldRect))
                {
                    this.Close(true);
                    Find.MainTabsRoot.EscapeCurrentTab(true);
                    Find.WindowStack.Add(new Dialog_PawnApparelDetail(this.pawn, apparelThing));
                    return;
                }
            }


            itemRect.xMin += apparelScoreWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, apparelGainWidth, itemRect.height), apparelGainText);
            itemRect.xMin += apparelGainWidth;
        }
    }
}
