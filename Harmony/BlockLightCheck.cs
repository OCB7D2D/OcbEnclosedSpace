using HarmonyLib;

public class BlockLightCheck : Block
{

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return new BlockActivationCommand[] {
            new BlockActivationCommand("take", "hand", false)
        };
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        ChunkCluster chunkCluster = _world.ChunkClusters[_clrIdx];
        if (chunkCluster == null) return "No Chunk Cluster found";
        IChunk chunkSync = chunkCluster.GetChunkSync(World.toChunkXZ(_blockPos.x), _blockPos.y, World.toChunkXZ(_blockPos.z));
        if (chunkSync == null) return "No chunk sync found";
        int blockXz1 = World.toBlockXZ(_blockPos.x);
        int blockY = World.toBlockY(_blockPos.y);
        int blockXz2 = World.toBlockXZ(_blockPos.z);
        return string.Format("Light Block {0}, Sun {1}",
            chunkSync.GetLight(blockXz1, blockY, blockXz2, Chunk.LIGHT_TYPE.BLOCK),
            chunkSync.GetLight(blockXz1, blockY, blockXz2, Chunk.LIGHT_TYPE.SUN));
    }

}
