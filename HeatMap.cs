﻿using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace HeatMap
{
    public class HeatMap : ICellBoolGiver
    {
        public HeatMap()
        {
            var minComfortTemp = (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin) + 3;
            var maxComfortTemp = (int)ThingDefOf.Human.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax) - 3;

            // Narrow down the green range to a quarter scale, to make boundary temps stand out more.

            var comfortDoubleRange = (maxComfortTemp - minComfortTemp) * 2;
            _mappedTemperatureRange = new IntRange(
                minComfortTemp - comfortDoubleRange, maxComfortTemp + comfortDoubleRange);

            var mappedColorCount = _mappedTemperatureRange.max - _mappedTemperatureRange.min;
            _mappedColors = new Color[mappedColorCount];

            var channelDelta = 1f / comfortDoubleRange;
            var channelR = -2f;
            var channelG = 0f;
            var channelB = 2f;
            var greenRising = true;

            var mappingTemperature = _mappedTemperatureRange.min;
            for (var i = 0; i < mappedColorCount; i++, mappingTemperature++)
            {
                var realR = Math.Min(channelR, 1f);
                realR = Math.Max(realR, 0f);

                var realG = Math.Min(channelG, 1f);
                realG = Math.Max(realG, 0f);

                var realB = Math.Min(channelB, 1f);
                realB = Math.Max(realB, 0f);

                _mappedColors[i] = new Color(realR, realG, realB);

                if (channelG >= 2f)
                    greenRising = false;

                var delta = channelDelta;
                if (mappingTemperature >= minComfortTemp - 1 &&
                    mappingTemperature <= maxComfortTemp)
                {
                    delta *= 4;
                }

                channelR += delta;
                channelG += greenRising ? delta : -delta;
                channelB -= delta;
            }
        }

        public CellBoolDrawer Drawer
        {
            get
            {
                if (_drawerInt == null)
                {
                    var map = Find.VisibleMap;
                    _drawerInt = new CellBoolDrawer(this, map.Size.x, map.Size.z, 
                        Main.Instance.GetConfiguredOpacity());
                }
                return _drawerInt;
            }
        }

        public bool GetCellBool(int index)
        {
            var map = Find.VisibleMap;
            if (map.fogGrid.IsFogged(index))
                return false;

            var room = map.cellIndices.IndexToCell(index).GetRoom(
                map, RegionType.Set_All);

            if (room != null && !room.PsychologicallyOutdoors)
            {
                if (room.Temperature <= _mappedTemperatureRange.min)
                {
                    _nextColor = _mappedColors[0];
                }
                else if (room.Temperature >= _mappedTemperatureRange.max)
                {
                    _nextColor = _mappedColors[_mappedColors.Length - 1];
                }
                else
                {
                    var colorMapIndex = (int)room.Temperature - _mappedTemperatureRange.min;
                    if (colorMapIndex <= 0)
                    {
                        colorMapIndex = 0;
                    }
                    else if (colorMapIndex >= _mappedColors.Length)
                    {
                        colorMapIndex = _mappedColors.Length;
                    }
                    _nextColor = _mappedColors[colorMapIndex];
                }
                return true;
            }

            return false;
        }

        public Color GetCellExtraColor(int index)
        {
            return _nextColor;
        }

        public Color Color => Color.white;

        public void Update()
        {
            if (Main.Instance.ShowHeatMap)
            {
                Drawer.MarkForDraw();
                var tick = Find.TickManager.TicksGame;
                if (_nextUpdateTick == 0 || tick >= _nextUpdateTick)
                {
                    Drawer.SetDirty();
                    _nextUpdateTick = tick + Main.Instance.GetUpdateDelay();
                }
            }
            Drawer.CellBoolDrawerUpdate();
        }

        public void Reset()
        {
            _drawerInt = null;
            _nextUpdateTick = 0;
        }

        private CellBoolDrawer _drawerInt;

        private IntRange _mappedTemperatureRange;

        private readonly Color[] _mappedColors;

        private Color _nextColor;

        private int _nextUpdateTick;
    }
}