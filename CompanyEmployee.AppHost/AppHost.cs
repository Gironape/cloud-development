using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var authToken = builder.Configuration["LocalStack:AuthToken"];

var localstack = builder.AddContainer("localstack", "localstack/localstack")
    .WithEndpoint(port: 4566, targetPort: 4566, name: "localstack", scheme: "http")
    .WithEnvironment("SERVICES", "sns,sqs,s3")
    .WithEnvironment("DEFAULT_REGION", "us-east-1")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1");

if (!string.IsNullOrEmpty(authToken))
{
    localstack = localstack.WithEnvironment("LOCALSTACK_AUTH_TOKEN", authToken);
}

var fileService = builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WaitFor(localstack)
    .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
    .WithEnvironment("AWS__Region", "us-east-1")
    .WithEnvironment("AWS__AccessKeyId", "test")
    .WithEnvironment("AWS__SecretAccessKey", "test")
    .WithEnvironment("S3__BucketName", "employee-data");

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
        .WaitFor(redis)
        .WaitFor(localstack)
        .WaitFor(fileService)
        .WithEndpoint("https", e => e.Port = port)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("AWS__ServiceURL", "http://localhost:4566")
        .WithEnvironment("AWS__Region", "us-east-1")
        .WithEnvironment("AWS__AccessKeyId", "test")
        .WithEnvironment("AWS__SecretAccessKey", "test")
        .WithEnvironment("SNS__TopicArn", "arn:aws:sns:us-east-1:000000000000:employee-events");

    apiReplicas.Add(api);
    gateway.WaitFor(api);
}

foreach (var replica in apiReplicas)
{
    gateway.WithReference(replica);
}

var client = builder.AddProject<Projects.Client_Wasm>("client")
    .WithReference(gateway)
    .WaitFor(gateway);

builder.Build().Run();