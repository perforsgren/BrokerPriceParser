using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.Review;
using BrokerPriceParser.Core.State;
using Microsoft.Win32;

namespace BrokerPriceParser.App;

/// <summary>
/// Provides the main replay and review tool window.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IBrokerMessageNormalizer _normalizer;
    private readonly IBrokerParseService _parseService;
    private readonly IConversationStateStore _stateStore;
    private readonly IReplayMessageReader _replayMessageReader;
    private readonly IReviewQueueService _reviewQueueService;
    private readonly BrokerLlmSettings _llmSettings;
    private readonly ObservableCollection<ReviewQueueItem> _reviewQueueItems = new();

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
        _replayMessageReader = (IReplayMessageReader)app.Services.GetService(typeof(IReplayMessageReader))!;
        _reviewQueueService = (IReviewQueueService)app.Services.GetService(typeof(IReviewQueueService))!;
        _llmSettings = (BrokerLlmSettings)app.Services.GetService(typeof(BrokerLlmSettings))!;

        ReplayItemsDataGrid.ItemsSource = _reviewQueueItems;
        EnableLlmDuringReplayCheckBox.IsChecked = false;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Opens a replay text file and parses it into the review queue.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private async void OpenReplayFileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Replay File",
            Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            SyncRuntimeSettingsFromUi();

            var records = await _replayMessageReader.ReadAsync(
                dialog.FileName,
                GetDefaultConversationId());

            await ReplayRecordsAsync(records);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Replay File Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Replays a built-in sample sequence through the parser.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private async void ReplaySampleButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SyncRuntimeSettingsFromUi();
            var records = BuildSampleReplayRecords();
            await ReplayRecordsAsync(records);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Sample Replay Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Clears the queue and resets all detail panels.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void ClearQueueButton_Click(object sender, RoutedEventArgs e)
    {
        _reviewQueueItems.Clear();
        _stateStore.ClearAll();
        ReplayItemsDataGrid.SelectedItem = null;
        ClearSelectedItemDetails();
        UpdateStatusText();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Marks the selected review item as accepted.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void AcceptSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSelectedReviewStatus(ReviewStatus.Accepted);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Marks the selected review item as corrected.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void CorrectSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSelectedReviewStatus(ReviewStatus.Corrected);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Marks the selected review item as ignored.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void IgnoreSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateSelectedReviewStatus(ReviewStatus.Ignored);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Updates the details panel when the selected review queue item changes.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void ReplayItemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReplayItemsDataGrid.SelectedItem is ReviewQueueItem item)
        {
            ShowSelectedItemDetails(item);
            return;
        }

        ClearSelectedItemDetails();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Replays a collection of records sequentially through the parser.
    /// </summary>
    /// <param name="records">The replay records.</param>
    /// <returns>A task that completes when replay finishes.</returns>
    private async Task ReplayRecordsAsync(IReadOnlyList<ReplayMessageRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        _reviewQueueItems.Clear();
        _stateStore.ClearAll();
        ClearSelectedItemDetails();

        var priorMessagesByConversation = new Dictionary<string, List<NormalizedBrokerMessage>>(StringComparer.OrdinalIgnoreCase);
        var lowConfidenceThreshold = ReadReviewThreshold();

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            var rawMessage = CreateRawBrokerMessage(record);
            var normalizedMessage = _normalizer.Normalize(rawMessage);
            var conversationState = _stateStore.GetOrCreate(record.ConversationId);

            var priorMessages = GetPriorMessages(
                priorMessagesByConversation,
                record.ConversationId);

            var context = new ParseContext
            {
                RawMessage = rawMessage,
                NormalizedMessage = normalizedMessage,
                ConversationState = conversationState,
                PriorMessages = priorMessages
            };

            var result = await _parseService.ParseAsync(context);
            _stateStore.Apply(record.ConversationId, result, rawMessage.ReceivedUtc);

            var reviewQueueItem = _reviewQueueService.Create(
                record,
                normalizedMessage,
                result,
                lowConfidenceThreshold);

            _reviewQueueItems.Add(reviewQueueItem);

            AppendPriorMessage(
                priorMessagesByConversation,
                record.ConversationId,
                normalizedMessage);

            if ((index + 1) % 250 == 0)
            {
                UpdateStatusText();
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        UpdateStatusText();

        if (_reviewQueueItems.Count > 0)
        {
            ReplayItemsDataGrid.SelectedIndex = 0;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Creates a raw broker message from a replay record.
    /// </summary>
    /// <param name="record">The replay record.</param>
    /// <returns>A raw broker message.</returns>
    private static RawBrokerMessage CreateRawBrokerMessage(ReplayMessageRecord record)
    {
        return new RawBrokerMessage
        {
            MessageId = $"REPLAY-{record.SequenceNumber:000000}",
            ConversationId = record.ConversationId,
            Source = record.Source,
            Broker = record.Broker,
            RawText = record.RawText,
            ReceivedUtc = record.ReceivedUtc
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Returns a stable prior-message snapshot for the supplied conversation.
    /// </summary>
    /// <param name="priorMessagesByConversation">The prior message store.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <returns>A prior-message snapshot.</returns>
    private static IReadOnlyList<NormalizedBrokerMessage> GetPriorMessages(
        IDictionary<string, List<NormalizedBrokerMessage>> priorMessagesByConversation,
        string conversationId)
    {
        if (!priorMessagesByConversation.TryGetValue(conversationId, out var messages))
        {
            return Array.Empty<NormalizedBrokerMessage>();
        }

        return messages.ToArray();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Appends one normalized message to the prior-message store while respecting the configured history limit.
    /// </summary>
    /// <param name="priorMessagesByConversation">The prior message store.</param>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="normalizedMessage">The normalized message to append.</param>
    private void AppendPriorMessage(
        IDictionary<string, List<NormalizedBrokerMessage>> priorMessagesByConversation,
        string conversationId,
        NormalizedBrokerMessage normalizedMessage)
    {
        if (!priorMessagesByConversation.TryGetValue(conversationId, out var messages))
        {
            messages = new List<NormalizedBrokerMessage>();
            priorMessagesByConversation[conversationId] = messages;
        }

        messages.Add(normalizedMessage);

        while (messages.Count > _llmSettings.MaxPriorMessages)
        {
            messages.RemoveAt(0);
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds a built-in sample replay collection.
    /// </summary>
    /// <returns>A sample replay collection.</returns>
    private IReadOnlyList<ReplayMessageRecord> BuildSampleReplayRecords()
    {
        var timestamp = DateTime.UtcNow;

        return new List<ReplayMessageRecord>
        {
            new()
            {
                SequenceNumber = 1,
                ConversationId = "SAMPLE-001",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "NOK/SEK 1Y 25 delta rr pls",
                ReceivedUtc = timestamp.AddMilliseconds(1)
            },
            new()
            {
                SequenceNumber = 2,
                ConversationId = "SAMPLE-001",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "showing 0.10/0.30 good",
                ReceivedUtc = timestamp.AddMilliseconds(2)
            },
            new()
            {
                SequenceNumber = 3,
                ConversationId = "SAMPLE-001",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "flat bid",
                ReceivedUtc = timestamp.AddMilliseconds(3)
            },
            new()
            {
                SequenceNumber = 4,
                ConversationId = "SAMPLE-001",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "ok take",
                ReceivedUtc = timestamp.AddMilliseconds(4)
            },
            new()
            {
                SequenceNumber = 5,
                ConversationId = "SAMPLE-001",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "same in 2y",
                ReceivedUtc = timestamp.AddMilliseconds(5)
            },
            new()
            {
                SequenceNumber = 6,
                ConversationId = "SAMPLE-001",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "mom int",
                ReceivedUtc = timestamp.AddMilliseconds(6)
            },
            new()
            {
                SequenceNumber = 7,
                ConversationId = "SAMPLE-002",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "EUR/SEK 3m at the money forward",
                ReceivedUtc = timestamp.AddMilliseconds(7)
            },
            new()
            {
                SequenceNumber = 8,
                ConversationId = "SAMPLE-002",
                Source = "Sample",
                Broker = "SampleBroker",
                RawText = "0.12/0.18",
                ReceivedUtc = timestamp.AddMilliseconds(8)
            }
        };
    }

    // ────────────────────────────────────

    /// <summary>
    /// Reads the review threshold from the UI, falling back to 0.55 when invalid.
    /// </summary>
    /// <returns>The review threshold.</returns>
    private double ReadReviewThreshold()
    {
        if (double.TryParse(ReviewThresholdTextBox.Text, out var threshold))
        {
            return Math.Clamp(threshold, 0.0, 1.0);
        }

        ReviewThresholdTextBox.Text = "0.55";
        return 0.55;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Returns the default conversation identifier from the UI.
    /// </summary>
    /// <returns>The default conversation identifier.</returns>
    private string GetDefaultConversationId()
    {
        var conversationId = DefaultConversationIdTextBox.Text?.Trim();

        if (string.IsNullOrWhiteSpace(conversationId))
        {
            DefaultConversationIdTextBox.Text = "REPLAY-001";
            return "REPLAY-001";
        }

        return conversationId;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Synchronizes runtime LLM settings from the review tool UI.
    /// </summary>
    private void SyncRuntimeSettingsFromUi()
    {
        var apiKeyExists = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(_llmSettings.ApiKeyEnvironmentVariableName));
        _llmSettings.IsEnabled = EnableLlmDuringReplayCheckBox.IsChecked == true && apiKeyExists;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Updates the status text using current replay queue statistics.
    /// </summary>
    private void UpdateStatusText()
    {
        var total = _reviewQueueItems.Count;
        var requiresReview = _reviewQueueItems.Count(x => x.RequiresReview);
        var accepted = _reviewQueueItems.Count(x => x.ReviewStatus == ReviewStatus.Accepted);
        var corrected = _reviewQueueItems.Count(x => x.ReviewStatus == ReviewStatus.Corrected);
        var ignored = _reviewQueueItems.Count(x => x.ReviewStatus == ReviewStatus.Ignored);

        StatusTextBlock.Text =
            $"Rows={total} | RequiresReview={requiresReview} | Accepted={accepted} | Corrected={corrected} | Ignored={ignored} | LLMEnabled={_llmSettings.IsEnabled}";
    }

    // ────────────────────────────────────

    /// <summary>
    /// Shows the selected review item in the details panel.
    /// </summary>
    /// <param name="item">The selected review item.</param>
    private void ShowSelectedItemDetails(ReviewQueueItem item)
    {
        SelectedRawMessageTextBox.Text = item.RawText;
        SelectedNormalizedMessageTextBox.Text = item.NormalizedText;
        SelectedContextSummaryTextBox.Text = item.ContextSummary;
        SelectedValidationErrorsTextBox.Text = item.ValidationErrorsText;
        SelectedAmbiguityFlagsTextBox.Text = item.AmbiguityFlagsText;
        SelectedReviewReasonTextBox.Text = item.ReviewReason;
        SelectedReviewStatusTextBlock.Text = $"Status: {item.ReviewStatus}";
        SelectedResultJsonTextBox.Text = item.ResultJson;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Clears all selected-item detail controls.
    /// </summary>
    private void ClearSelectedItemDetails()
    {
        SelectedRawMessageTextBox.Text = string.Empty;
        SelectedNormalizedMessageTextBox.Text = string.Empty;
        SelectedContextSummaryTextBox.Text = string.Empty;
        SelectedValidationErrorsTextBox.Text = string.Empty;
        SelectedAmbiguityFlagsTextBox.Text = string.Empty;
        SelectedReviewReasonTextBox.Text = string.Empty;
        SelectedReviewStatusTextBlock.Text = "Status: -";
        SelectedResultJsonTextBox.Text = string.Empty;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Updates the review status for the currently selected queue item.
    /// </summary>
    /// <param name="reviewStatus">The new review status.</param>
    private void UpdateSelectedReviewStatus(ReviewStatus reviewStatus)
    {
        if (ReplayItemsDataGrid.SelectedItem is not ReviewQueueItem item)
        {
            return;
        }

        item.ReviewStatus = reviewStatus;
        ReplayItemsDataGrid.Items.Refresh();
        ShowSelectedItemDetails(item);
        UpdateStatusText();
    }
}