using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var localstack = builder.AddContainer("localstack", "localstack/localstack:3.0")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "localstack", scheme: "http")
    .WithEnvironment("SERVICES", "sns,sqs")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithLifetime(ContainerLifetime.Persistent);

var minio = builder.AddContainer("minio", "minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "api", scheme: "http")
    .WithEndpoint(port: 9001, targetPort: 9001, name: "console", scheme: "http");

var gateway = builder.AddProject<Projects.CompanyEmployee_Gateway>("gateway")
    .WithEndpoint("https", e => e.Port = 7000)
    .WithExternalHttpEndpoints();

const int startApiPort = 6001;
const int replicaCount = 5;
var apiReplicas = new List<IResourceBuilder<ProjectResource>>();

for (var i = 0; i < replicaCount; i++)
{
    var port = startApiPort + i;
    var api = builder.AddProject<Projects.CompanyEmployee_Api>($"api-{i + 1}")
        .WithReference(redis)
        .WithEndpoint("https", e => e.Port = port)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
        .WithEnvironment("AWS__Region", "us-east-1")
        .WithEnvironment("AWS__AccessKeyId", "test")
        .WithEnvironment("AWS__SecretAccessKey", "test")
        .WaitFor(redis)
        .WaitFor(localstack);

    apiReplicas.Add(api);
    gateway.WaitFor(api);
}

var fileService = builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WaitFor(localstack)
    .WaitFor(minio)
    .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
    .WithEnvironment("AWS__Region", "us-east-1")
    .WithEnvironment("AWS__AccessKeyId", "test")
    .WithEnvironment("AWS__SecretAccessKey", "test")
    .WithEnvironment("MINIO__Endpoint", "localhost:9000")
    .WithEnvironment("MINIO__AccessKey", "minioadmin")
    .WithEnvironment("MINIO__SecretKey", "minioadmin")
    .WithEnvironment("MINIO__BucketName", "employee-data");

foreach (var replica in apiReplicas)
{
    gateway.WithReference(replica);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();