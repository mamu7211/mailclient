var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("mailclientdb");

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .AddModel("qwen3:4b");

var mailpit = builder.AddMailPit("mailpit");

var api = builder.AddProject<Projects.Feirb_Api>("api")
    .WithReference(postgres)
    .WithReference(ollama)
    .WithReference(mailpit)
    .WaitFor(postgres);

builder.AddProject<Projects.Feirb_Web>("web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
