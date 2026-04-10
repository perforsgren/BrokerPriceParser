using System.Windows;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Infrastructure.Classification;
using BrokerPriceParser.Infrastructure.Llm;
using BrokerPriceParser.Infrastructure.Normalization;
using BrokerPriceParser.Infrastructure.Parsing;
using BrokerPriceParser.Infrastructure.Scoring;
using BrokerPriceParser.Infrastructure.State;
using BrokerPriceParser.Infrastructure.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace BrokerPriceParser.App;

/// <summary>
/// Provides application startup and dependency registration.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the application service provider.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Handles application startup.
    /// </summary>
    /// <param name="e">The startup event arguments.</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();

        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Registers application services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new BrokerLlmSettings
        {
            IsEnabled = false,
            UseOnlyForLowConfidence = true,
            LowConfidenceThreshold = 0.55,
            ModelName = "UNCONFIGURED",
            MaxPriorMessages = 5
        });

        services.AddSingleton<IBrokerMessageNormalizer, BrokerMessageNormalizer>();
        services.AddSingleton<IBrokerMessageClassifier, BrokerMessageClassifier>();
        services.AddSingleton<IConversationContextResolver, ConversationContextResolver>();
        services.AddSingleton<IConversationStateStore, InMemoryConversationStateStore>();
        services.AddSingleton<IBrokerPromptBuilder, BrokerPromptBuilder>();
        services.AddSingleton<IBrokerLlmClient, NullBrokerLlmClient>();
        services.AddSingleton<IBrokerLlmEnrichmentService, BrokerLlmEnrichmentService>();
        services.AddSingleton<IBrokerValidationService, BrokerValidationService>();
        services.AddSingleton<IConfidenceScoringService, ConfidenceScoringService>();
        services.AddSingleton<IBrokerParseService, BrokerParseService>();
    }
}