var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CompanyEmployee_Api>("companyemployee-api");

builder.Build().Run();
