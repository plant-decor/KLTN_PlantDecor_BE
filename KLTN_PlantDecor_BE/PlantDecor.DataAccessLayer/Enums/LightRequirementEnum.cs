namespace PlantDecor.DataAccessLayer.Enums
{
    public enum LightRequirementEnum
    {
        LowLight = 1, // ánh sáng yếu (góc phòng, ít cửa sổ)
        IndirectLight = 2, // ánh sáng gián tiếp (gần cửa sổ nhưng không nắng trực tiếp)
        PartialSun = 3, // nắng một phần (3–6 giờ nắng/ngày)
        FullSun = 4 // nắng trực tiếp (6+ giờ nắng/ngày)
    }
}
