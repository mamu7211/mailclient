var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("feirb-postgres")
    .WithEndpoint("tcp", endpoint =>
    {
        endpoint.Port = 15432;
        endpoint.TargetPort = 5432;
        endpoint.IsProxied = false;
    })
    .WithDataVolume()
    .WithPgAdmin()
    .AddDatabase("mailclientdb");

var ollama = builder.AddOllama("feirb-ollama", port: 11434)
    .WithBindMount("../../.ollama-data", "/root/.ollama");

var qwen = ollama.AddModel("qwen3:0.6b");

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

var api = builder.AddProject<Projects.Feirb_Api>("api")
    .WithReference(postgres)
    .WithReference(qwen)
    .WaitFor(postgres)
    .WaitFor(greenmail);

if (string.Equals(Environment.GetEnvironmentVariable("AUTO_LOGIN"), "true", StringComparison.OrdinalIgnoreCase))
{
    api.WithEnvironment("AUTO_LOGIN", "true");
}

builder.Build().Run();
