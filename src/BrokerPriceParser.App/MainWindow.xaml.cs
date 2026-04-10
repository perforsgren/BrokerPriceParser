using System.Text;
using System.Windows;
using BrokerPriceParser.Core.Contracts;
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

        Loaded += MainWindow_Loaded;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Runs a simple multi-message stateful smoke test when the window is loaded.
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
            "flat bid",
            "paid 0.30",
            "0.27 offer"
        };

        var output = new StringBuilder();

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
            output.AppendLine($"Quote Style: {result.Quote.QuoteStyle}");
            output.AppendLine($"Is Firm: {result.Quote.IsFirm}");
            output.AppendLine($"Action Verb: {result.Action.Verb}");
            output.AppendLine($"Action Side: {result.Action.Side}");
            output.AppendLine($"Action Target: {result.Action.Target}");
            output.AppendLine($"Linked To Prior Quote: {result.Action.LinkedToPriorQuote}");
            output.AppendLine($"Used Context: {result.ContextUsage.UsedContext}");
            output.AppendLine($"Resolved From Context: {string.Join(", ", result.ContextUsage.ResolvedFromContext)}");
            output.AppendLine($"Unresolved References: {string.Join(", ", result.ContextUsage.UnresolvedReferences)}");
            output.AppendLine(new string('-', 60));
        }

        MessageBox.Show(
            output.ToString(),
            "Broker Parser Quote/Action v2 Smoke Test",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}