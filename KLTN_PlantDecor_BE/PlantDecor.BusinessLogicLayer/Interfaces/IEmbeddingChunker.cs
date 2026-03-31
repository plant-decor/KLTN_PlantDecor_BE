namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IEmbeddingChunker
    {
        IReadOnlyList<string> Chunk(string text);
    }
}
