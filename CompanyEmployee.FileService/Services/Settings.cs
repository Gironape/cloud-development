namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Настройки SNS/SQS.
/// </summary>
public class SnsSettings
{
    public string ServiceURL { get; set; } = "http://localhost:4566";
    public string AccessKeyId { get; set; } = "test";
    public string SecretAccessKey { get; set; } = "test";
    public string Region { get; set; } = "us-east-1";
    public string TopicArn { get; set; } = "arn:aws:sns:us-east-1:000000000000:employee-events";
}

/// <summary>
/// Настройки объектного хранилища.
/// </summary>
public class StorageSettings
{
    public string Endpoint { get; set; } = "http://localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string AccessSecret { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "employee-data";
}