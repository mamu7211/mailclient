var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("feirb-postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("mailclientdb");

var ollama = builder.AddOllama("feirb-ollama")
    .WithDataVolume()
    .AddModel("qwen3:4b");

var greenmail = builder.AddContainer("feirb-greenmail", "docker.io/greenmail/standalone")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "api")
    .WithEndpoint("smtp", endpoint =>
    {
        endpoint.Port = 3025;
        endpoint.TargetPort = 3025;
        endpoint.Protocol = System.Net.Sockets.ProtocolType.Tcp;
        endpoint.Transport = "tcp";
        endpoint.IsProxied = false;
    })
    .WithEndpoint("imap", endpoint =>
    {
        endpoint.Port = 3143;
        endpoint.TargetPort = 3143;
        endpoint.Protocol = System.Net.Sockets.ProtocolType.Tcp;
        endpoint.Transport = "tcp";
        endpoint.IsProxied = false;
    })
    .WithBindMount("../../seeding/mails", "/preload", isReadOnly: true)
    .WithEnvironment("GREENMAIL_OPTS",
        "-Dgreenmail.preload.dir=/preload " +
        "-Dgreenmail.setup.test.all " +
        "-Dgreenmail.hostname=0.0.0.0 " +
        "-Dgreenmail.api.port=8080 " +
        "-Dgreenmail.api.hostname=0.0.0.0");

builder.AddProject<Projects.Feirb_Api>("api")
    .WithReference(postgres)
    .WithReference(ollama)
    .WaitFor(postgres)
    .WaitFor(greenmail);

builder.Build().Run();
