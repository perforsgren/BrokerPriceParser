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
    /// Runs a simple end-to-end smoke test when the window is loaded.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var rawMessage = new RawBrokerMessage
        {
            MessageId = "MSG-001",
            ConversationId = "CONV-001",
            Source = "ManualTest",
            Broker = "TestBroker",
            RawText = "NOK/SEK 1Y 25 delta rr pls",
            ReceivedUtc = DateTime.UtcNow
        };

        var normalizedMessage = _normalizer.Normalize(rawMessage);
        var state = _stateStore.GetOrCreate(rawMessage.ConversationId);

        var context = new ParseContext
        {
            RawMessage = rawMessage,
            NormalizedMessage = normalizedMessage,
            ConversationState = state
        };

        var result = await _parseService.ParseAsync(context);

        MessageBox.Show(
            $"Raw: {result.RawMessage}\n" +
            $"Normalized: {result.NormalizedMessage}\n" +
            $"Detected Pair: {normalizedMessage.DetectedCurrencyPair}\n" +
            $"Detected Tenor: {normalizedMessage.DetectedTenor}\n" +
            $"Detected Structure: {normalizedMessage.DetectedStructure}\n" +
            $"Detected Delta: {normalizedMessage.DetectedDelta}\n" +
            $"MessageType: {result.MessageType}\n" +
            $"EventType: {result.EventType}\n" +
            $"Confidence: {result.Quality.Confidence:F2}",
            "Broker Parser Smoke Test",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}