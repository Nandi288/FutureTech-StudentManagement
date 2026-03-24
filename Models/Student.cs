using Newtonsoft.Json;

namespace FutureTech_StudentManagement.Models
{
    public class Student
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("mobileNumber")]
        public string MobileNumber { get; set; }

        [JsonProperty("enrolmentStatus")]
        public string EnrolmentStatus { get; set; }

        [JsonProperty("profileImageUrl")]
        public string ProfileImageUrl { get; set; }

        [JsonProperty("isDeleted")]
        public bool IsDeleted { get; set; } = false;

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonProperty("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public string FullName => $"{FirstName} {LastName}";
    }
}