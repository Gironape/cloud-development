var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var redisCommander = builder.AddContainer("redis-commander", "rediscommander/redis-commander")
    .WithEnvironment("REDIS_HOSTS", "local:redis:6379")
    .WithReference(redis)
    .WaitFor(redis)
    .WithEndpoint(port: 8081, targetPort: 8081); 

var api = builder.AddProject<Projects.CompanyEmployee_Api>("companyemployee-api")
    .WithReference(redis)
    .WaitFor(redis); 

builder.AddProject<Projects.Client_Wasm>("client")
     .WithReference(api)
     .WaitFor(api);

builder.Build().Run();