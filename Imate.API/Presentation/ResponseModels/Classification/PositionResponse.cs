namespace Imate.API.Presentation.ResponseModels.Classification
{
    public class PositionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int QuestionCount { get; set; }
        public List<SkillResponse> Skills { get; set; }
        public class SkillResponse
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}
