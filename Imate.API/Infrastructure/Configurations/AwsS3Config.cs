namespace Imate.API.Infrastructure.Configurations
{
    public class AwsS3Config
    {
        public const string ConfigSectionName = "AwsS3Storage";
        public string BucketName { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string RegionName { get; set; } = "ap-southeast-1";
    }
}

