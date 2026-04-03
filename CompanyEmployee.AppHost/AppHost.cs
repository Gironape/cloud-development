using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "api", scheme: "http")
    .WithEndpoint(port: 9001, targetPort: 9001, name: "console", scheme: "http");

var localstack = builder.AddContainer("localstack", "localstack/localstack:latest")
    .WithEnvironment("SERVICES", "sns,sqs")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "api", scheme: "http");

var gateway = builder.AddProject<Projects.CompanyEmployee_Gateway>("gateway")
    .WithEndpoint("https", e => e.Port = 7000)
    .WithExternalHttpEndpoints();

const int startApiPort = 6001;
const int replicaCount = 5;

for (var i = 0; i < replicaCount; i++)
{
    var port = startApiPort + i;
    var api = builder.AddProject<Projects.CompanyEmployee_Api>($"api-{i + 1}")
        .WithReference(redis)
        .WithReference(minio)
        .WithReference(localstack)
        .WithEndpoint("https", e => e.Port = port)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
        .WithEnvironment("AWS__AccessKeyId", "test")
        .WithEnvironment("AWS__SecretAccessKey", "test")
        .WithEnvironment("AWS__Region", "us-east-1")
        .WithEnvironment("SNS__TopicArn", "arn:aws:sns:us-east-1:000000000000:employee-events")
        .WithEnvironment("STORAGE__Endpoint", "http://localhost:9000")
        .WithEnvironment("STORAGE__AccessKey", "minioadmin")
        .WithEnvironment("STORAGE__AccessSecret", "minioadmin")
        .WithEnvironment("STORAGE__BucketName", "employee-data")
        .WaitFor(redis)
        .WaitFor(minio)
        .WaitFor(localstack);

    gateway.WaitFor(api);
}

var fileService = builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WithReference(localstack)
    .WithReference(minio)
    .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
    .WithEnvironment("AWS__AccessKeyId", "test")
    .WithEnvironment("AWS__SecretAccessKey", "test")
    .WithEnvironment("AWS__Region", "us-east-1")
    .WithEnvironment("SNS__TopicArn", "arn:aws:sns:us-east-1:000000000000:employee-events")
    .WithEnvironment("STORAGE__Endpoint", "http://localhost:9000")
    .WithEnvironment("STORAGE__AccessKey", "minioadmin")
    .WithEnvironment("STORAGE__AccessSecret", "minioadmin")
    .WithEnvironment("STORAGE__BucketName", "employee-data")
    .WaitFor(localstack)
    .WaitFor(minio);

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();