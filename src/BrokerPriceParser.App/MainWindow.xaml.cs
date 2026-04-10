using System.Text;
using System.Windows;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.State;

namespace BrokerPriceParser.App;

/// <summary>
/// Provides the main application window.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IBrokerMessageNormalizer _normalizer;
    private readonly IBrokerParseService _parseService;
    private readonly IConversationStateStore _stateStore;
    private readonly BrokerLlmSettings _llmSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        var app = (App)Application.Current;
        _normalizer = (IBrokerMessageNormalizer)app.Services.GetService(typeof(IBrokerMessageNormalizer))!;
        _parseService = (IBrokerParseService)app.Services.GetService(typeof(IBrokerParseService))!;
        _stateStore = (IConversationStateStore)app.Services.GetService(typeof(IConversationStateStore))!;
        _llmSettings = (BrokerLlmSettings)app.Services.GetService(typeof(BrokerLlmSettings))!;

        Loaded += MainWindow_Loaded;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Runs a smoke test that shows live provider configuration and parser output.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var conversationId = "CONV-001";
        var inputs = new[]
        {
            "NOK/SEK 1Y 25 delta rr pls",
            "showing 0.10/0.30 good",
            "ok take",
            "mom int"
        };

        var output = new StringBuilder();

        output.AppendLine("LLM Provider Configuration");
        output.AppendLine($"Enabled: {_llmSettings.IsEnabled}");
        output.AppendLine($"Model: {_llmSettings.ModelName}");
        output.AppendLine($"LowConfidenceOnly: {_llmSettings.UseOnlyForLowConfidence}");
        output.AppendLine($"Threshold: {_llmSettings.LowConfidenceThreshold:F2}");
        output.AppendLine($"API URL: {_llmSettings.ApiBaseUrl}");
        output.AppendLine(new string('=', 70));

        for (var index = 0; index < inputs.Length; index++)
        {
            var rawMessage = new RawBrokerMessage
            {
                MessageId = $"MSG-{index + 1:000}",
                ConversationId = conversationId,
                Source = "ManualTest",
                Broker = "TestBroker",
                RawText = inputs[index],
                ReceivedUtc = DateTime.UtcNow
            };

            var normalizedMessage = _normalizer.Normalize(rawMessage);
            var state = _stateStore.GetOrCreate(conversationId);

            var context = new ParseContext
            {
                RawMessage = rawMessage,
                NormalizedMessage = normalizedMessage,
                ConversationState = state
            };

            var result = await _parseService.ParseAsync(context);
            _stateStore.Apply(conversationId, result, rawMessage.ReceivedUtc);

            output.AppendLine($"Input: {rawMessage.RawText}");
            output.AppendLine($"Normalized: {result.NormalizedMessage}");
            output.AppendLine($"MessageType: {result.MessageType}");
            output.AppendLine($"EventType: {result.EventType}");
            output.AppendLine($"Pair: {result.Instrument.Pair}");
            output.AppendLine($"Tenor: {result.Instrument.Tenor}");
            output.AppendLine($"Structure: {result.Instrument.Structure}");
            output.AppendLine($"Delta: {result.Instrument.Delta}");
            output.AppendLine($"Bid: {result.Quote.Bid}");
            output.AppendLine($"Ask: {result.Quote.Ask}");
            output.AppendLine($"Mid: {result.Quote.Mid}");
            output.AppendLine($"QuoteStyle: {result.Quote.QuoteStyle}");
            output.AppendLine($"IsFirm: {result.Quote.IsFirm}");
            output.AppendLine($"ActionVerb: {result.Action.Verb}");
            output.AppendLine($"ActionSide: {result.Action.Side}");
            output.AppendLine($"ActionTarget: {result.Action.Target}");
            output.AppendLine($"LinkedToPriorQuote: {result.Action.LinkedToPriorQuote}");
            output.AppendLine($"Confidence: {result.Quality.Confidence:F2}");
            output.AppendLine($"ValidationErrors: {string.Join(", ", result.Quality.ValidationErrors)}");
            output.AppendLine($"AmbiguityFlags: {string.Join(", ", result.Quality.AmbiguityFlags)}");
            output.AppendLine(new string('-', 70));
        }

        MessageBox.Show(
            output.ToString(),
            "Broker Parser Step 7 Smoke Test",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}