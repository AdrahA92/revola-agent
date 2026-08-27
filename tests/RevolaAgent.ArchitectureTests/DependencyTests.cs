using Xunit;

namespace RevolaAgent.ArchitectureTests;

public class DependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceApplicationOrInfrastructure()
    {
        var references = typeof(Domain.AssemblyMarker).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, reference => reference.Name!.StartsWith("RevolaAgent."));
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructureOrHost()
    {
        var references = typeof(Application.AssemblyMarker).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, reference => reference.Name is "RevolaAgent.Infrastructure" or "RevolaAgent.Api" or "RevolaAgent.Worker");
    }
}
