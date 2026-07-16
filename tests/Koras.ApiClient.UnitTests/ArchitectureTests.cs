using System.Reflection;
using Koras.ApiClient.Testing;
using Xunit;

namespace Koras.ApiClient.UnitTests;

/// <summary>
/// Enforces the dependency rules from docs/architecture/dependency-rules.md:
/// Testing → Core → Abstractions → BCL only.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Abstractions = typeof(ApiError).Assembly;
    private static readonly Assembly Core = typeof(KorasApiClient).Assembly;
    private static readonly Assembly Testing = typeof(MockApiHandler).Assembly;

    [Fact]
    public void Abstractions_ReferencesBclOnly()
    {
        var references = Abstractions.GetReferencedAssemblies().Select(reference => reference.Name!).ToList();

        Assert.DoesNotContain(references, name => name.StartsWith("Koras.", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.Extensions", StringComparison.Ordinal));
        Assert.DoesNotContain(references, name => name.StartsWith("Polly", StringComparison.Ordinal));
    }

    [Fact]
    public void Core_DependsOnAbstractions_ButNeverOnTesting()
    {
        var references = Core.GetReferencedAssemblies().Select(reference => reference.Name!).ToList();

        Assert.Contains("Koras.ApiClient.Abstractions", references);
        Assert.DoesNotContain("Koras.ApiClient.Testing", references);
    }

    [Fact]
    public void Testing_DependsOnCore()
    {
        var references = Testing.GetReferencedAssemblies().Select(reference => reference.Name!).ToList();

        Assert.Contains("Koras.ApiClient", references);
    }

    [Fact]
    public void PublicTypes_LiveInApprovedNamespaces()
    {
        string[] approved =
        [
            "Koras.ApiClient",
            "Koras.ApiClient.Authentication",
            "Koras.ApiClient.Testing",
            "Microsoft.Extensions.DependencyInjection",
        ];

        foreach (var assembly in new[] { Abstractions, Core, Testing })
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                Assert.Contains(type.Namespace, approved);
            }
        }
    }

    [Fact]
    public void PublicAsyncMethods_AcceptCancellationTokens()
    {
        foreach (var assembly in new[] { Abstractions, Core })
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (!typeof(Task).IsAssignableFrom(method.ReturnType) || method.IsSpecialName)
                    {
                        continue;
                    }

                    Assert.True(
                        method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)),
                        $"{type.FullName}.{method.Name} returns a Task but accepts no CancellationToken.");
                }
            }
        }
    }
}
