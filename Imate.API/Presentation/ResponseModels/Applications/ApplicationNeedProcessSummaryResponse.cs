using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Applications
{
    public class ApplicationNeedProcessSummaryResponse
    {
        public ApplicationType Type { get; set; }
        public int TotalNeedProcess { get; set; }
    }
}
