using RevolaAgent.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.AddFoundation("RevolaAgent.Worker");
// Phase 1 only establishes the host. No schedulers or external actions are registered.
await builder.Build().RunAsync();
