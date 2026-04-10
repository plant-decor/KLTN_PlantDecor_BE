namespace PlantDecor.BusinessLogicLayer.Constants
{
    public static class EmbeddingEntityTypes
    {
        public const string CommonPlant = "CommonPlant";
        public const string PlantInstance = "PlantInstance";
        public const string NurseryPlantCombo = "NurseryPlantCombo";
        public const string NurseryMaterial = "NurseryMaterial";

        public static readonly string[] AllTypes = new[]
        {
            CommonPlant,
            PlantInstance,
            NurseryPlantCombo,
            NurseryMaterial
        };

        public static bool IsValidType(string entityType)
            => AllTypes.Contains(entityType);
    }
}
