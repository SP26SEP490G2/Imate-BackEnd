namespace Imate.API.Presentation.ResponseModels.Classification
{
    public class CompanyPositionsSkillsResponse
    {
        public List<PositionForCompanyResponse> Positions { get; set; } = new List<PositionForCompanyResponse>();
        public List<SkillForCompanyResponse> Skills { get; set; } = new List<SkillForCompanyResponse>();
    }

    public class PositionForCompanyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SkillForCompanyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

