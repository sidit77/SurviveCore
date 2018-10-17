namespace SurviveCore.World.Generating {
    public interface IWorldGenerator {
        void FillChunk(Chunk chunk);
        void DecorateChunk(Chunk c);
    }
}