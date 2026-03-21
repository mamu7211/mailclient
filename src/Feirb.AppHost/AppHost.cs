var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("feirb-postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("mailclientdb");

var ollama = builder.AddOllama("feirb-ollama")
    .WithDataVolume()
    .AddModel("qwen3:4b");

var mailpit = builder.AddMailPit("feirb-mailpit", httpPort: 8025, smtpPort: 1025);

builder.AddProject<Projects.Feirb_Api>("api")
    .WithReference(postgres)
    .WithReference(ollama)
    .WithReference(mailpit)
    .WaitFor(postgres);

builder.Build().Run();
