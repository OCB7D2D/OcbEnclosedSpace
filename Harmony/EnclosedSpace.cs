using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

public class OcbEnclosedSpace : IModApi
{

    public void InitMod(Mod mod)
    {
        Log.Out("OCB Harmony Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    // Mirror `Vector3i.AllDirections` index
    static int MirrorAllDirections(int dir)
    {
        switch (dir)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 3;
            case 3: return 2;
            case 4: return 5;
            case 5: return 4;
            default: return -1;
        }
    }

    // Called by `RefreshLightAtLocalPos`
    private static byte GetLightAtWorldPos(IChunkAccess world,
        int worldX, int worldY, int worldZ,
        Chunk.LIGHT_TYPE type,
        BlockValue into, int dir)
    {
        var c_x = World.toChunkXZ(worldX);
        var c_z = World.toChunkXZ(worldZ);
        IChunk chunkSync = world.GetChunkSync(c_x, worldY, c_z);
        if (chunkSync == null) return 0;
        var b_x = World.toBlockXZ(worldX);
        var b_y = World.toBlockY(worldY);
        var b_z = World.toBlockXZ(worldZ);
        byte light = chunkSync.GetLight(b_x, b_y, b_z, type);
        if (dir == -1) return light;
        var from = chunkSync.GetBlock(b_x, b_y, b_z);
        from = GetMasterBlock(world, from, chunkSync as Chunk, b_x, b_y, b_z);
        var opacity = GetBlockOpacity(0, from, into, dir);
        return (byte)Mathf.Max(0, light - opacity);
    }

    // calcNextLightStep and getLightAtWorldPos
    private static byte GetBlockOpacity(byte opacity, BlockValue from, BlockValue into, int dir)
    {
        if (from.Block.GetBlockName().Contains("Hatch"))
            opacity = GetHatchOpacityFrom(opacity, from, dir);
        if (into.Block.GetBlockName().Contains("Hatch"))
            opacity = GetHatchOpacityFrom(opacity, into,
                MirrorAllDirections(dir));
        if (from.Block.GetBlockName().Contains("Door"))
            opacity = GetDoorOpacityFrom(opacity, from, dir);
        if (into.Block.GetBlockName().Contains("Door"))
            opacity = GetDoorOpacityFrom(opacity, into,
                MirrorAllDirections(dir));
        return opacity;
    }

    // Helper constants
    static int East = 0;
    static int West = 1;
    static int Up = 2;
    static int Down = 3;
    static int North = 4;
    static int South = 5;

    private static bool IsHatchBlockingDirection(BlockValue bv, int dir)
    {
        if (!(bv.Block is BlockDoor)) return false;
        bool open = BlockDoor.IsDoorOpen(bv.meta);
        switch (bv.rotation)
        {
            case 0: // on bottom, opening south
                if (dir == Down) return !open;
                if (dir == South) return open;
                break;
            case 1: // on bottom, opening west
                if (dir == Down) return !open;
                if (dir == West) return open;
                break;
            case 2: // on bottom, opening north
                if (dir == Down) return !open;
                if (dir == North) return open;
                break;
            case 3: // on bottom, opening east
                if (dir == Down) return !open;
                if (dir == East) return open;
                break;

            case 4: // on ceiling, opening south
                if (dir == Up) return !open;
                if (dir == South) return open;
                break;
            case 5: // on ceiling, opening east
                if (dir == Up) return !open;
                if (dir == East) return open;
                break;
            case 6: // on ceiling, opening north
                if (dir == Up) return !open;
                if (dir == North) return open;
                break;
            case 7: // on ceiling, opening west
                if (dir == Up) return !open;
                if (dir == West) return open;
                break;

            case 8: // on south, opening bottom
                if (dir == South) return !open;
                if (dir == Down) return open;
                break;
            case 9: // on south, opening east
                if (dir == South) return !open;
                if (dir == East) return open;
                break;
            case 10: // on south, opening up
                if (dir == South) return !open;
                if (dir == Up) return open;
                break;
            case 11: // on south, opening west
                if (dir == South) return !open;
                if (dir == West) return open;
                break;


            case 12: // on east, opening south
                if (dir == East) return !open;
                if (dir == South) return open;
                break;
            case 13: // on east, opening bottom
                if (dir == East) return !open;
                if (dir == Down) return open;
                break;
            case 14: // on east, opening north
                if (dir == East) return !open;
                if (dir == North) return open;
                break;
            case 15: // on east, opening up
                if (dir == East) return !open;
                if (dir == Up) return open;
                break;

            case 16: // on north, opening bottom
                if (dir == North) return !open;
                if (dir == Down) return open;
                break;
            case 17: // on north, opening west
                if (dir == North) return !open;
                if (dir == West) return open;
                break;
            case 18: // on north, opening up
                if (dir == North) return !open;
                if (dir == Up) return open;
                break;
            case 19: // on north, opening east
                if (dir == North) return !open;
                if (dir == East) return open;
                break;

            case 20: // on west, opening south
                if (dir == West) return !open;
                if (dir == South) return open;
                break;
            case 21: // on west, opening top
                if (dir == West) return !open;
                if (dir == Up) return open;
                break;
            case 22: // on west, opening north
                if (dir == West) return !open;
                if (dir == North) return open;
                break;
            case 23: // on west, opening bottom
                if (dir == West) return !open;
                if (dir == Down) return open;
                break;

        }

        return false;
    }

    private static bool IsDoorBlockingDirection(BlockValue bv, int dir)
    {
        if (!(bv.Block is BlockDoor)) return false;
        bool open = BlockDoor.IsDoorOpen(bv.meta);
        // Log.Out("Door dir {0} open {1} (child {2})", dir, open, bv.ischild);
        switch (bv.rotation)
        {
            case 0: // on north, opening east
                if (dir == North) return !open;
                if (dir == East) return open;
                break;
            case 1: // on east, opening south
                if (dir == East) return !open;
                if (dir == South) return open;
                break;
            case 2: // on south, opening west
                if (dir == South) return !open;
                if (dir == West) return open;
                break;
            case 3: // on west, opening north
                if (dir == West) return !open;
                if (dir == North) return open;
                break;

            case 4: // on north, opening west
                if (dir == North) return !open;
                if (dir == West) return open;
                break;
            case 5: // on west, opening south
                if (dir == West) return !open;
                if (dir == South) return open;
                break;
            case 6: // on south, opening east
                if (dir == South) return !open;
                if (dir == East) return open;
                break;
            case 7: // on east, opening north
                if (dir == East) return !open;
                if (dir == North) return open;
                break;

            case 8: // on ceiling, opening west
                if (dir == Up) return !open;
                if (dir == West) return open;
                break;
            case 9: // on west, opening down
                if (dir == West) return !open;
                if (dir == Down) return open;
                break;
            case 10: // on bottom, opening east
                if (dir == Down) return !open;
                if (dir == East) return open;
                break;
            case 11: // on east, opening up
                if (dir == East) return !open;
                if (dir == Up) return open;
                break;


            case 12: // on north, opening up
                if (dir == North) return !open;
                if (dir == Up) return open;
                break;
            case 13: // on ceiling, opening south
                if (dir == Up) return !open;
                if (dir == South) return open;
                break;
            case 14: // on south, opening down
                if (dir == South) return !open;
                if (dir == Down) return open;
                break;
            case 15: // on bottom, opening north
                if (dir == Down) return !open;
                if (dir == North) return open;
                break;

            case 16: // on ceiling, opening east
                if (dir == Up) return !open;
                if (dir == East) return open;
                break;
            case 17: // on east, opening down
                if (dir == East) return !open;
                if (dir == Down) return open;
                break;
            case 18: // on bottom, opening west
                if (dir == Down) return !open;
                if (dir == West) return open;
                break;
            case 19: // on west, opening up
                if (dir == West) return !open;
                if (dir == Up) return open;
                break;

            case 20: // on north, opening bottom
                if (dir == North) return !open;
                if (dir == Down) return open;
                break;
            case 21: // on bottom, opening south
                if (dir == Down) return !open;
                if (dir == South) return open;
                break;
            case 22: // on south, opening up
                if (dir == South) return !open;
                if (dir == Up) return open;
                break;
            case 23: // on ceiling, opening north
                if (dir == Up) return !open;
                if (dir == North) return open;
                break;

        }

        return false;
    }

    private static byte GetHatchOpacityFrom(byte opacity, BlockValue from, int dir)
    {
        if (!(from.Block is BlockDoor)) return opacity;
        if (IsHatchBlockingDirection(from, dir))
            return byte.MaxValue;
        return opacity;
    }

    private static byte GetDoorOpacityFrom(byte opacity, BlockValue from, int dir)
    {
        if (!(from.Block is BlockDoor)) return opacity;
        // Log.Out("Check Door Dir {0} at rot {1} (Blocked {2})", dir, from.rotation, IsDoorBlockingDirection(from, dir));
        if (IsDoorBlockingDirection(from, dir))
            return byte.MaxValue;
        return opacity;
    }


    private static byte calcNextLightStepOrg(byte _currentLight, BlockValue bv)
    {
        int lightOpacity = bv.Block.lightOpacity;
        int num = (int)_currentLight - (lightOpacity != 0 ? lightOpacity : 1);
        return num >= 0 ? (byte)num : (byte)0;
    }

    private static byte calcNextLightStep(IChunkAccess world, byte _currentLight, BlockValue from, BlockValue into, int dir)
    {
        byte lightOpacity = (byte)into.Block.lightOpacity;
        lightOpacity = GetBlockOpacity(lightOpacity, from, into, dir);
        int num = _currentLight - (lightOpacity != 0 ? lightOpacity : 1);
        return num >= 0 ? (byte)num : (byte)0;
    }

    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch("SpreadBlockLightFromLightSources")]
    public class SpreadBlockLightFromLightSources
    {
        static bool Prefix(LightProcessor __instance, Chunk c)
        {
            for (int index1 = 0; index1 < 16; ++index1)
            {
                for (int index2 = 0; index2 < 16; ++index2)
                {
                    for (int maxValue = (int)byte.MaxValue; maxValue >= 0; --maxValue)
                    {
                        BlockValue blockNoDamage = c.GetBlockNoDamage(index1, maxValue, index2);
                        Block block = blockNoDamage.Block;
                        if (block.GetLightValue(blockNoDamage) > (byte)0)
                            __instance.SpreadLight(c, index1, maxValue, index2, block.GetLightValue(blockNoDamage), Chunk.LIGHT_TYPE.BLOCK, true);
                    }
                }
            }
            return false;
        }
    }

    /*
    [HarmonyPatch(typeof(XUiC_InGameDebugMenu))]
    [HarmonyPatch("BtnRecalcLight_Controller_OnPress")]
    public class BtnRecalcLight_Controller_OnPress
    {
        static bool Prefix(XUiController _sender, int _mouseButton)
        {
            Log.Warning("Regenerating my own ligh");
            if (GameManager.Instance.World.ChunkClusters[0] == null) return false;
            lock (GameManager.Instance.World.ChunkClusters[0].GetSyncRoot())
            {
                LightProcessor lightProcessor = new LightProcessor((IChunkAccess)GameManager.Instance.World);
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                List<Chunk> chunkArrayCopySync = GameManager.Instance.World.ChunkClusters[0].GetChunkArrayCopySync();
                foreach (Chunk chunk in chunkArrayCopySync)
                {
                    chunk.ResetLights();
                    chunk.RefreshSunlight();
                }
                foreach (Chunk chunk in chunkArrayCopySync)
                    lightProcessor.GenerateSunlight(chunk, false);
                foreach (Chunk chunk in chunkArrayCopySync)
                    lightProcessor.GenerateSunlight(chunk, true);
                foreach (Chunk c in chunkArrayCopySync)
                    lightProcessor.LightChunk(c);
                stopwatch.Stop();
                foreach (Chunk chunk in chunkArrayCopySync)
                    chunk.NeedsRegeneration = true;
                Log.Out("#" + chunkArrayCopySync.Count.ToString() + " chunks needed " + stopwatch.ElapsedMilliseconds.ToString() + "ms");
            }
            return false;
        }
    }
    */


    [HarmonyPatch(typeof(Chunk))]
    [HarmonyPatch("RefreshSunlight")]
    public class ChunksRefreshSunlight
    {
        static bool Prefix(Chunk __instance,
            ChunkBlockChannel ___chnLight)
        {

            ___chnLight.SetHalf(false, (byte)15);
            for (int _x = 0; _x < 16; ++_x)
            {
                for (int _z = 0; _z < 16; ++_z)
                {
                    int num = 15;
                    bool flag1 = true;
                    int maxValue;
                    BlockValue from = BlockValue.Air;
                    for (maxValue = (int)byte.MaxValue; maxValue >= 0; --maxValue)
                    {
                        var bv = __instance.GetBlock(_x, maxValue, _z);
                        int blockId = __instance.GetBlockId(_x, maxValue, _z);
                        if (flag1)
                        {
                            if (blockId != 0)
                                flag1 = false;
                            else
                                continue;
                        }
                        Block block = Block.list[blockId];
                        bool flag2 = block.shape.IsTerrain();
                        if (!flag2)
                        {
                            // num -= block.lightOpacity;
                            num -= (int)GetBlockOpacity((byte)block.lightOpacity, from,
                                GetMasterBlock(GameManager.Instance.World, bv, __instance,
                                    _x, maxValue, _z), Down);
                            //if (bv.Block is BlockDoor)
                            //    if (!BlockDoor.IsDoorOpen(bv.meta))
                            //        num = 0;
                            if (num <= 0)
                                break;
                        }
                        ___chnLight.Set(_x, maxValue, _z, (long)(byte)num);
                        if (flag2)
                        {
                            //num -= block.lightOpacity;
                            num -= (int)GetBlockOpacity((byte)block.lightOpacity, from,
                                GetMasterBlock(GameManager.Instance.World, bv, __instance,
                                    _x, maxValue, _z), Down);
                            //if (bv.Block is BlockDoor)
                            //    if (!BlockDoor.IsDoorOpen(bv.meta))
                            //        num = 0;
                            if (num <= 0)
                                break;
                        }
                        from = bv;
                    }
                    for (int _y = maxValue - 1; _y >= 0; --_y)
                        ___chnLight.Set(_x, _y, _z, 0L);
                }
            }
            __instance.isModified = true;
            return false;
        }
    }



    // Called when BlockValue changes
    // Also called by GenerateSunlight
    //  which is only called from recalc


    private static void RefreshSunlightAtLocalPos(
        LightProcessor __instance,
            Chunk c,
            int x,
            int z,
            bool bSpreadLight,
            bool refreshSunlight,
            IChunkAccess ___m_World,
            List<Vector3i> ___brightSpots,
            HashSet<Tuple<int, int>> seen)
    {
        bool opaque = false;
        byte intensity = 15;

        BlockValue from = BlockValue.Air;

        //Log.Out("RefreshSunlightAtLocalPos at {0},{1}",
        //    c.GetBlockWorldPosX(x),
        //    c.GetBlockWorldPosZ(z));

        // Move from top (ceiling/heaven) to (rock) bottom
        for (int y = byte.MaxValue; y >= 0; --y)
        {
            BlockValue bv = c.GetBlockNoDamage(x, y, z);
            int lightOpacity = bv.Block.lightOpacity;
            if (lightOpacity == byte.MaxValue) opaque = true;
            byte currentLight = c.GetLight(x, y, z, Chunk.LIGHT_TYPE.SUN);
            byte lightValue;

            var into = bv;

            if (into.Block is BlockDoor || from.Block is BlockDoor)
            {
                into = GetMasterBlock(___m_World, bv, c, x, y, z);
                lightOpacity = GetBlockOpacity((byte)lightOpacity, from, into, Down);
                if (lightOpacity == byte.MaxValue) opaque = true;
                //Log.Out("Moving Down attenuated to {0} (opaque {3}) ({1} vs {2})", lightOpacity,
                //    from.ischild, into.ischild, opaque);
                // Find other occopant x/z locations


                if (into.Block.isMultiBlock && into.Block is BlockDoor)
                {
                    // Only do work for main block, as we get request
                    // for all child blocks. But we need to do the whole
                    // light unspreading in one wash in order to work
                    if (into.ischild) return;
                    // Get the special class to help with multi blocks
                    Block.MultiBlockArray mb = into.Block.multiBlockPos;
                    // Iterate over all children and self
                    for (int _idx = mb.Length - 1; _idx >= 0; --_idx)
                    {
                        // Get the relative offset to the child (or self)
                        Vector3i offset = mb.Get(_idx, into.type, into.rotation);
                        // Adjust the block position for offset
                        var b_x = x + offset.x;
                        var b_y = y + offset.y;
                        var b_z = z + offset.z;
                        Chunk chunk = c;
                        // Check if given offset lies outside current chunk
                        if (b_x < 0 || b_x >= 16 || b_z < 0 || b_z >= 16)
                        {
                            // Get back world position via chunk
                            var c_x = c.GetBlockWorldPosX(b_x);
                            var c_z = c.GetBlockWorldPosZ(b_z);
                            // Fetch new chunk from offset world position
                            chunk = (Chunk)___m_World.GetChunkFromWorldPos(c_x, b_y, c_z);
                            // Make position local to new chunk
                            b_x = World.toBlockXZ(b_x);
                            b_z = World.toBlockXZ(b_z);
                        }
                        var loc = new Tuple<int, int>(b_x, b_z);
                        if (!seen.Contains(loc))
                        {
                            // Log.Warning("Found another space to check");
                            seen.Add(loc);
                            RefreshSunlightAtLocalPos(__instance, chunk,
                                b_x, b_z, bSpreadLight, refreshSunlight,
                                ___m_World, ___brightSpots, seen);
                        }
                        // Finally unspread the light from block
                        //unspreadLight(___m_World, c, b_x, b_y, b_z,
                        //    lightValue, 0, type, brightSpots);
                    }
                }

            }

            if (!opaque)
            {
                intensity = (byte)Utils.FastMax(0, intensity - lightOpacity);
                c.SetLight(x, y, z, intensity, Chunk.LIGHT_TYPE.SUN);
                lightValue = intensity;
            }
            else
            {
                c.SetLight(x, y, z, 0, Chunk.LIGHT_TYPE.SUN);
                lightValue = 0;
                if (refreshSunlight)
                {
                    // This will look around and will find old light values
                    lightValue = __instance.RefreshLightAtLocalPos(c, x, y, z, Chunk.LIGHT_TYPE.SUN);
                }
            }

            if (bSpreadLight)
            {
                if (into.Block is BlockDoor || from.Block is BlockDoor)
                {
                    UnspreadLight(c, x, y, z, currentLight, Chunk.LIGHT_TYPE.SUN, ___brightSpots, ___m_World);
                }
                else if (currentLight > lightValue)
                    UnspreadLight(c, x, y, z, currentLight, Chunk.LIGHT_TYPE.SUN, ___brightSpots, ___m_World);
                else if (currentLight < lightValue)
                    __instance.SpreadLight(c, x, y, z, lightValue, Chunk.LIGHT_TYPE.SUN, true);
            }
            from = into;
        }
    }


    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch("RefreshSunlightAtLocalPos")]
    public class RefreshSunlightAtLocalPosPatch
    {
        static bool Prefix(
            LightProcessor __instance,
            Chunk c,
            int x,
            int z,
            bool bSpreadLight,
            bool refreshSunlight,
            IChunkAccess ___m_World,
            List<Vector3i> ___brightSpots)
        {
            HashSet<Tuple<int, int>> seen =
                new HashSet<Tuple<int, int>>();
            seen.Add(new Tuple<int, int>(x, z));
            RefreshSunlightAtLocalPos(__instance, c, x, z,
                bSpreadLight, refreshSunlight, ___m_World,
                ___brightSpots, seen);
            return false;
        }
    }

    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch("RefreshLightAtLocalPos")]
    public class RefreshLightAtLocalPos
    {
        static bool Prefix(Chunk c, int x, int y, int z, Chunk.LIGHT_TYPE type, ref byte __result)
        {
            World world = GameManager.Instance.World;
            int worldX = c.GetBlockWorldPosX(x);
            int worldZ = c.GetBlockWorldPosZ(z);
            BlockValue into = c.GetBlockNoDamage(x, y, z);
            into = GetMasterBlock(world, into, c, x, y, z);
            byte intensity = 0;
            int lightOpacity = into.Block.lightOpacity;
            if (lightOpacity == byte.MaxValue)
            {
                c.SetLight(x, y, z, 0, type);
            }
            else
            {
                byte xyz = GetLightAtWorldPos(world, worldX, y, worldZ, type, into, -1);
                byte xa = GetLightAtWorldPos(world, worldX + 1, y, worldZ, type, into, 1);
                byte xb = GetLightAtWorldPos(world, worldX - 1, y, worldZ, type, into, 0);
                byte za = GetLightAtWorldPos(world, worldX, y, worldZ + 1, type, into, 3);
                byte zb = GetLightAtWorldPos(world, worldX, y, worldZ - 1, type, into, 2);
                byte ya = GetLightAtWorldPos(world, worldX, y + 1, worldZ, type, into, 5);
                byte yb = GetLightAtWorldPos(world, worldX, y - 1, worldZ, type, into, 4);

                if (xyz == byte.MaxValue) xyz = 0;
                if (xa == byte.MaxValue) xa = 0;
                if (xb == byte.MaxValue) xb = 0;
                if (za == byte.MaxValue) za = 0;
                if (zb == byte.MaxValue) zb = 0;
                if (ya == byte.MaxValue) ya = 0;
                if (yb == byte.MaxValue) yb = 0;
                int a4 = (byte)Mathf.Max(Mathf.Max(Mathf.Max(xa, xb), Mathf.Max(za, zb)), Mathf.Max(ya, yb)) - 1 - lightOpacity;
                if (a4 < 0) a4 = 0;
                intensity = (byte)Mathf.Max(a4, xyz);
                c.SetLight(x, y, z, intensity, type);
            }
            __result = intensity;
            return false;
        }
    }

    private static void unspreadLight(
        IChunkAccess world, Chunk _chunk,
        int _blockX, int _blockY, int _blockZ,
        byte lightValue, int depth,
        Chunk.LIGHT_TYPE type, List<Vector3i> brightSpots)
    {
        if (_chunk == null) return;
        _chunk.SetLight(_blockX, _blockY, _blockZ, (byte)0, type);
        // Log.Out("  unset light {0},{1},{2}", _blockX, _blockY, _blockZ);
        for (int index = 0; index < Vector3i.AllDirections.Length; ++index)
        {
            int num1 = _blockX + Vector3i.AllDirections[index].x;
            int num2 = _blockY + Vector3i.AllDirections[index].y;
            int num3 = _blockZ + Vector3i.AllDirections[index].z;
            if (num2 >= 0 && num2 <= (int)byte.MaxValue)
            {
                Chunk _chunk1 = _chunk;
                if (num1 < 0 || num1 >= 16 || num3 < 0 || num3 >= 16)
                {
                    _chunk1 = (Chunk)world.GetChunkFromWorldPos(_chunk.GetBlockWorldPosX(num1), num2, _chunk.GetBlockWorldPosZ(num3));
                    num1 = World.toBlockXZ(num1);
                    num3 = World.toBlockXZ(num3);
                }
                if (_chunk1 != null)
                {
                    byte light = _chunk1.GetLight(num1, num2, num3, type);
                    if (light < byte.MaxValue)
                    {
                        if ((int)light < (int)lightValue && light != (byte)0)
                        {
                            var n_bv = _chunk1.GetBlockNoDamage(num1, num2, num3);
                            // int lightValue1 = (int)calcNextLightStep(lightValue, bv, n_bv, index);
                            int lightValue1 = (int)calcNextLightStepOrg(lightValue, n_bv);
                            // Log.Out("  next value with light would be {0}", lightValue1);
                            if (lightValue1 > 0)
                                unspreadLight(world, _chunk1, num1, num2, num3, (byte)lightValue1, depth + 1, type, brightSpots);
                        }
                        else if ((int)light >= (int)lightValue)
                        {
                            brightSpots.Add(new Vector3i(_chunk1.GetBlockWorldPosX(num1), num2, _chunk1.GetBlockWorldPosZ(num3)));
                            // Log.Out("    + add briught spot {0} ({1},{2},{3})", light, num1, num2, num3);
                        }
                    }
                }
            }
        }
    }

    private static byte getLightAtWorldPosOrg(IChunkAccess world, int worldX, int worldY, int worldZ, Chunk.LIGHT_TYPE type)
    {
        IChunk chunkSync = world.GetChunkSync(World.toChunkXZ(worldX), worldY, World.toChunkXZ(worldZ));
        return chunkSync != null ? chunkSync.GetLight(World.toBlockXZ(worldX), World.toBlockY(worldY), World.toBlockXZ(worldZ), type) : (byte)0;
    }


    public static void UnspreadLight(
        Chunk c, int x, int y, int z,
        byte lightValue, Chunk.LIGHT_TYPE type,
        List<Vector3i> brightSpots, IChunkAccess world)
    {
        // Clear helper list
        brightSpots.Clear();
        // Get block value to unspread from
        var bv = c.GetBlockNoDamage(x, y, z);
        // If block is a multi block, we need to unspread the light
        // at every child block first, as otherwise spreading via
        // bright spots would pickup already lit neighbour blocks.
        if (bv.Block.isMultiBlock && bv.Block is BlockDoor)
        {
            // Only do work for main block, as we get request
            // for all child blocks. But we need to do the whole
            // light unspreading in one wash in order to work
            if (bv.ischild) return;
            // Get the special class to help with multi blocks
            Block.MultiBlockArray mb = bv.Block.multiBlockPos;
            // Iterate over all children and self
            for (int _idx = mb.Length - 1; _idx >= 0; --_idx)
            {
                // Get the relative offset to the child (or self)
                Vector3i offset = mb.Get(_idx, bv.type, bv.rotation);
                // Adjust the block position for offset
                var b_x = x + offset.x;
                var b_y = y + offset.y;
                var b_z = z + offset.z;
                // Check if given offset lies outside current chunk
                if (b_x < 0 || b_x >= 16 || b_z < 0 || b_z >= 16)
                {
                    // Get back world position via chunk
                    var c_x = c.GetBlockWorldPosX(b_x);
                    var c_z = c.GetBlockWorldPosZ(b_z);
                    // Fetch new chunk from offset world position
                    c = (Chunk)world.GetChunkFromWorldPos(c_x, b_y, c_z);
                    // Make position local to new chunk
                    b_x = World.toBlockXZ(b_x);
                    b_z = World.toBlockXZ(b_z);
                }
                // Finally unspread the light from block
                unspreadLight(world, c, b_x, b_y, b_z,
                    lightValue, 0, type, brightSpots);
            }
        }
        else
        {
            // Just unspread the light from the current position
            unspreadLight(world, c, x, y, z, lightValue, 0, type, brightSpots);
            // Log.Out("After Unspread Value is now {0}", c.GetLight(x, y, z, Chunk.LIGHT_TYPE.SUN));
            // Log.Out("After Unspread Value is front {0}", c.GetLight(x, y, z + 1, Chunk.LIGHT_TYPE.SUN));
            // Log.Out("After Unspread Value is behind {0}", c.GetLight(x, y, z - 1, Chunk.LIGHT_TYPE.SUN));
        }
        // Re-distribute light from detected edges
        foreach (Vector3i brightSpot in brightSpots)
        {
            Chunk chunk = (Chunk)world.GetChunkFromWorldPos(
                brightSpot.x, brightSpot.y, brightSpot.z);
            if (chunk != null)
            {
                byte light = getLightAtWorldPosOrg(world,
                    brightSpot.x, brightSpot.y, brightSpot.z, type);
                if (light < byte.MaxValue)
                {
                    //Log.Out("  spread light again from {0},{1},{2} => {3}",
                    //    brightSpot.x, brightSpot.y, brightSpot.z, lightAtWorldPos);
                    var b_x = World.toBlockXZ(brightSpot.x);
                    var b_y = World.toBlockY(brightSpot.y);
                    var b_z = World.toBlockXZ(brightSpot.z);
                    var l_bv = chunk.GetBlockNoDamage(b_x, b_y, b_z);
                    spreadLight(world, chunk, b_x, b_y, b_z, l_bv,
                        light, 0, type);
                }
            }
        }
    }
    /*
    [HarmonyPatch(typeof(ChunkCluster))]
    [HarmonyPatch("SetBlock")]
    public class ChunkClusterSetBlock
    {
        static void Postfix(
            Vector3i _pos,
            bool _isChangeBV,
            BlockValue _bv,
            bool _isChangeDensity,
            sbyte _density,
            bool _isNotify,
            bool _isUpdateLight,
            bool _isForceDensity)
        {
            if (_isChangeBV & _isUpdateLight)
            {
                // For lighttype block
            }
        }
    }
    */

    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch("UnspreadLight")]
    public class UnspreadLightPatch
    {
        static bool Prefix(
            Chunk c,
            int x,
            int y,
            int z,
            byte lightValue,
            Chunk.LIGHT_TYPE type,
            IChunkAccess ___m_World,
            List<Vector3i> ___brightSpots)
        {
            //var old_light = c.GetLight(x, y, z, type);
            UnspreadLight(c, x, y, z, lightValue, type, ___brightSpots, ___m_World);
            //if (old_light != lightValue;
            return false;
        }
    }


    private static void spreadLight(
        IChunkAccess world, Chunk _chunk,
        int _blockX, int _blockY, int _blockZ,
        BlockValue from, byte lightValue,
        int depth, Chunk.LIGHT_TYPE type,
        bool bSetAtStarterPos = true)
    {
        if (bSetAtStarterPos)
            _chunk.SetLight(_blockX, _blockY, _blockZ, lightValue, type);
        if (lightValue == (byte)0)
            return;
        for (int index = Vector3i.AllDirections.Length - 1; index >= 0; --index)
        {
            Vector3i allDirection = Vector3i.AllDirections[index];
            int num1 = _blockX + allDirection.x;
            int num2 = _blockY + allDirection.y;
            int num3 = _blockZ + allDirection.z;
            if (num2 >= 0 && num2 <= (int)byte.MaxValue)
            {
                Chunk _chunk1 = _chunk;
                if (num1 < 0 || num1 >= 16 || num3 < 0 || num3 >= 16)
                {
                    _chunk1 = (Chunk)world.GetChunkFromWorldPos(_chunk.GetBlockWorldPosX(num1), num2, _chunk.GetBlockWorldPosZ(num3));
                    num1 = World.toBlockXZ(num1);
                    num3 = World.toBlockXZ(num3);
                    if (_chunk1 == null)
                        continue;
                }
                byte light = _chunk1.GetLight(num1, num2, num3, type);
                if (light < (byte)15)
                {
                    var into = _chunk1.GetBlockNoDamage(num1, num2, num3);
                    byte lightValue1 = calcNextLightStep(world, lightValue,
                        GetMasterBlock(world, from, _chunk, _blockX, _blockY, _blockZ),
                        GetMasterBlock(world, into, _chunk1, num1, num2, num3),
                        index);
                    //if (lightValue == 15) Log.Out(" === attenuate to {0} ({1} => {2})", lightValue1,
                    //    from.Block.GetBlockName(), into.Block.GetBlockName());
                    if (light < lightValue1)
                        spreadLight(world, _chunk1, num1, num2, num3, into, lightValue1, depth + 1, type);
                }
            }
        }

    }

    private static BlockValue GetMasterBlock(IChunkAccess world, BlockValue bv, Chunk _chunk, int _blockX, int _blockY, int _blockZ)
    {
        // We only support this feature for doors
        if (!(bv.Block is BlockDoor)) return bv;
        if (bv.Block.isMultiBlock && bv.ischild)
        {
            var p_x = _blockX + bv.parentx;
            var p_y = _blockY + bv.parenty;
            var p_z = _blockZ + bv.parentz;
            if (p_x < 0 || p_x >= 16 || p_z < 0 || p_z >= 16)
            {
                int c_x = _chunk.GetBlockWorldPosX(p_x);
                int c_z = _chunk.GetBlockWorldPosZ(p_z);
                _chunk = (Chunk)world.GetChunkFromWorldPos(c_x, p_y, c_z);
                if (_chunk == null) return BlockValue.Air;
                p_x = World.toBlockXZ(p_x);
                p_z = World.toBlockXZ(p_z);
            }
            bv = _chunk.GetBlockNoDamage(p_x, p_y, p_z);
        }
        return bv;
    }

    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch("SpreadLight")]
    public class SpreadLightPatch
    {
        static bool Prefix(
            IChunkAccess ___m_World, Chunk c,
            int blockX, int blockY, int blockZ,
            byte lightValue, Chunk.LIGHT_TYPE type,
            bool bSetAtStarterPos)
        {
            var bv = c.GetBlockNoDamage(blockX, blockY, blockZ);
            spreadLight(___m_World, c, blockX, blockY, blockZ,
                bv, lightValue, 0, type, bSetAtStarterPos);
            return false;
        }
    }


    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch("unspreadLight")]
    public class unspreadLightPatch
    {
        static bool Prefix(
            Chunk _chunk,
            int _blockX,
            int _blockY,
            int _blockZ,
            byte lightValue,
            int depth,
            Chunk.LIGHT_TYPE type,
            List<Vector3i> brightSpots)
        {
            Log.Warning("Should not be called anymore");
            return false;
        }
    }

    [HarmonyPatch(typeof(LightProcessor))]
    [HarmonyPatch("spreadLight")]
    public class spreadLightPatch
    {
        static bool Prefix(
            Chunk _chunk,
            int _blockX,
            int _blockY,
            int _blockZ,
            byte lightValue,
            int depth,
            Chunk.LIGHT_TYPE type,
            bool bSetAtStarterPos)
        {
            Log.Warning("Should not be called anymore");
            return false;
        }
    }
}
