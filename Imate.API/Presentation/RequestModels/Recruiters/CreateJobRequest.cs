using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.RequestModels.Recruiters
{
    public class CreateUpdateJobRequest
    {
        public int Id { get; set; }
        [Required(ErrorMessage ="Title Required")]
        public string Title { get; set; }
        [Required(ErrorMessage = "EmploymentType Required")]
        public string EmploymentType { get; set; }
        [Required(ErrorMessage = "Location Required")]
        public string Location { get; set; }
        [Required(ErrorMessage = "MinSalary Required")]
        public long MinSalary { get; set; }
        [Required(ErrorMessage = "MaxSalary Required")]
        public long MaxSalary { get; set; }
        [Required(ErrorMessage = "Description Required")]
        public string Description { get; set; }
        [Required(ErrorMessage = "Status Required")]
        [JsonConverter(typeof(JsonStringEnumConverter))]

        public JobStatus Status { get; set; }
        [Required(ErrorMessage = "ApplicationDeadline Required")]

        public DateTimeOffset ApplicationDeadline { get; set; }

        public ICollection<int> JobPositions { get; set; }

        public ICollection<int> JobSkills { get; set; }
    }

}
