using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var minio = builder.AddContainer("minio", "minio/minio")
    .WithVolume("minio-data", "/data")
    .WithArgs("server", "/data", "--console-address", ":9001")
    .WithEnvironment("MINIO_ROOT_USER", "minioadmin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "minioadmin")
    .WithEndpoint(port: 9000, targetPort: 9000, scheme: "http", name: "minio-api")
    .WithEndpoint(port: 9001, targetPort: 9001, scheme: "http", name: "minio-console");

var localstack = builder.AddContainer("localstack", "localstack/localstack:3.8.0")
    .WithEndpoint(port: 4566, targetPort: 4566, scheme: "http", name: "localstack")
    .WithEnvironment("SERVICES", "sns")
    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
    .WithEnvironment("SKIP_SSL_CERT_DOWNLOAD", "1")
    .WithEnvironment("LOCALSTACK_HOST", "localhost.localstack.cloud")
    .WithLifetime(ContainerLifetime.Persistent);

var fileService = builder.AddProject<Projects.CompanyEmployee_FileService>("fileservice")
    .WithEnvironment("MinIO__Endpoint", "http://localhost:9000")
    .WithEnvironment("MinIO__AccessKey", "minioadmin")
    .WithEnvironment("MinIO__SecretKey", "minioadmin")
    .WithEnvironment("MinIO__BucketName", "employee-data")
    .WaitFor(minio)
    .WaitFor(localstack);

var gateway = builder.AddProject<Projects.CompanyEmployee_Gateway>("gateway")
    .WithExternalHttpEndpoints();

const int startApiHttpsPort = 6001;
const int replicaCount = 5;
var apiReplicas = new List<IResourceBuilder<ProjectResource>>();

for (var i = 0; i < replicaCount; i++)
{
    var httpsPort = startApiHttpsPort + i;

    var api = builder.AddProject<Projects.CompanyEmployee_Api>($"api-{i + 1}")
        .WithReference(redis)
        .WithEndpoint("https", e => e.Port = httpsPort)
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WaitFor(redis)
        .WaitFor(localstack);

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