using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using BrokerPriceParser.Core.Contracts;
using BrokerPriceParser.Core.Llm;
using BrokerPriceParser.Core.Models;
using BrokerPriceParser.Core.Review;
using BrokerPriceParser.Core.State;
using BrokerPriceParser.Core.Validation;
using Microsoft.Win32;
using System.Text.Json.Serialization;

namespace BrokerPriceParser.App;

/// <summary>
/// Provides the main replay and review tool window.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly IBrokerMessageNormalizer _normalizer;
    private readonly IBrokerParseService _parseService;
    private readonly IConversationStateStore _stateStore;
    private readonly IReplayMessageReader _replayMessageReader;
    private readonly IReviewQueueService _reviewQueueService;
    private readonly IReviewDecisionPersistenceService _reviewDecisionPersistenceService;
    private readonly IGoldLabelExportService _goldLabelExportService;
    private readonly IBrokerValidationService _validationService;
    private readonly IConfidenceScoringService _confidenceScoringService;
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
        _reviewDecisionPersistenceService = (IReviewDecisionPersistenceService)app.Services.GetService(typeof(IReviewDecisionPersistenceService))!;
        _goldLabelExportService = (IGoldLabelExportService)app.Services.GetService(typeof(IGoldLabelExportService))!;
        _validationService = (IBrokerValidationService)app.Services.GetService(typeof(IBrokerValidationService))!;
        _confidenceScoringService = (IConfidenceScoringService)app.Services.GetService(typeof(IConfidenceScoringService))!;
        _llmSettings = (BrokerLlmSettings)app.Services.GetService(typeof(BrokerLlmSettings))!;

        ReplayItemsDataGrid.ItemsSource = _reviewQueueItems;
        EnableLlmDuringReplayCheckBox.IsChecked = false;
        ManualNotesTextBox.Text = string.Empty;
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
    /// Saves the current review queue snapshot to a file.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private async void SaveDecisionsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Review Decisions",
            Filter = "JSON files (*.json)|*.json",
            FileName = "review-decisions.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await _reviewDecisionPersistenceService.SaveAsync(dialog.FileName, _reviewQueueItems.ToArray());
            UpdateStatusText();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Save Decisions Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Loads a persisted review queue snapshot from a file.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private async void LoadDecisionsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Load Review Decisions",
            Filter = "JSON files (*.json)|*.json"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var items = await _reviewDecisionPersistenceService.LoadAsync(dialog.FileName);

            _reviewQueueItems.Clear();

            foreach (var item in items)
            {
                _reviewQueueItems.Add(item);
            }

            UpdateStatusText();

            if (_reviewQueueItems.Count > 0)
            {
                ReplayItemsDataGrid.SelectedIndex = 0;
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Load Decisions Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Exports reviewed queue items to a gold-label JSONL file.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private async void ExportGoldLabelsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Export Gold Labels",
            Filter = "JSONL files (*.jsonl)|*.jsonl",
            FileName = "gold-labels.jsonl"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            await _goldLabelExportService.ExportAsync(dialog.FileName, _reviewQueueItems.ToArray());
            UpdateStatusText();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Export Gold Labels Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
    /// Loads the original parser JSON into the manual editor.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void LoadOriginalJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (ReplayItemsDataGrid.SelectedItem is not ReviewQueueItem item)
        {
            return;
        }

        EditableResultJsonTextBox.Text = item.OriginalResultJson;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Loads the current parser JSON into the manual editor.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void LoadCurrentJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (ReplayItemsDataGrid.SelectedItem is not ReviewQueueItem item)
        {
            return;
        }

        EditableResultJsonTextBox.Text = item.CurrentResultJson;
    }

    // ────────────────────────────────────

    /// <summary>
    /// Applies the edited JSON from the manual editor to the selected queue item.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void ApplyEditedJsonButton_Click(object sender, RoutedEventArgs e)
    {
        if (ReplayItemsDataGrid.SelectedItem is not ReviewQueueItem item)
        {
            return;
        }

        try
        {
            var editedJson = EditableResultJsonTextBox.Text ?? string.Empty;

            if (!TryDeserializeBrokerParseResult(editedJson, out var result))
            {
                MessageBox.Show(
                    "The edited JSON could not be parsed into BrokerParseResult.",
                    "Invalid JSON",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            NormalizeEditedResult(item, result);

            item.CurrentResultJson = JsonSerializer.Serialize(result, SerializerOptions);
            item.HasManualOverride = true;
            item.ManualNotes = ManualNotesTextBox.Text ?? string.Empty;
            item.ReviewStatus = ReviewStatus.Corrected;

            UpdateItemFromResult(item, result);
            ReplayItemsDataGrid.Items.Refresh();
            ShowSelectedItemDetails(item);
            UpdateStatusText();
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Apply Override Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Resets the selected review item back to its original parser result.
    /// </summary>
    /// <param name="sender">The sender instance.</param>
    /// <param name="e">The event arguments.</param>
    private void ResetSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (ReplayItemsDataGrid.SelectedItem is not ReviewQueueItem item)
        {
            return;
        }

        if (!TryDeserializeBrokerParseResult(item.OriginalResultJson, out var result))
        {
            MessageBox.Show(
                "The original parser JSON could not be restored.",
                "Reset Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        item.CurrentResultJson = item.OriginalResultJson;
        item.HasManualOverride = false;
        item.ManualNotes = string.Empty;
        item.ReviewStatus = ReviewStatus.Unreviewed;

        UpdateItemFromResult(item, result);
        EditableResultJsonTextBox.Text = item.CurrentResultJson;
        ManualNotesTextBox.Text = item.ManualNotes;

        ReplayItemsDataGrid.Items.Refresh();
        ShowSelectedItemDetails(item);
        UpdateStatusText();
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

    /// <

    /// <

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
        var overrides = _reviewQueueItems.Count(x => x.HasManualOverride);

        StatusTextBlock.Text =
            $"Rows={total} | RequiresReview={requiresReview} | Accepted={accepted} | Corrected={corrected} | Ignored={ignored} | Overrides={overrides} | LLMEnabled={_llmSettings.IsEnabled}";
    }

    // ────────────────────────────────────

    /// <summary>
    /// Shows the selected review item in the details panel and loads the current JSON into the editor.
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
        ManualNotesTextBox.Text = item.ManualNotes;
        EditableResultJsonTextBox.Text = item.CurrentResultJson;
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
        ManualNotesTextBox.Text = string.Empty;
        EditableResultJsonTextBox.Text = string.Empty;
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
        item.ManualNotes = ManualNotesTextBox.Text ?? string.Empty;

        ReplayItemsDataGrid.Items.Refresh();
        ShowSelectedItemDetails(item);
        UpdateStatusText();
    }

    // ────────────────────────────────────

    /// <summary>
    /// Normalizes an edited parse result before it is saved back to the selected queue item.
    /// </summary>
    /// <param name="item">The selected review item.</param>
    /// <param name="result">The edited parse result.</param>
    private void NormalizeEditedResult(ReviewQueueItem item, BrokerParseResult result)
    {
        result.MessageId = string.IsNullOrWhiteSpace(result.MessageId) ? item.MessageId : result.MessageId;
        result.RawMessage = string.IsNullOrWhiteSpace(result.RawMessage) ? item.RawText : result.RawMessage;
        result.NormalizedMessage = string.IsNullOrWhiteSpace(result.NormalizedMessage) ? item.NormalizedText : result.NormalizedMessage;

        var validationErrors = _validationService.Validate(result);

        result.Quality = new BrokerParseQuality
        {
            ValidationErrors = validationErrors,
            AmbiguityFlags = result.ContextUsage.UnresolvedReferences ?? Array.Empty<string>()
        };

        result.Quality.Confidence = _confidenceScoringService.Calculate(result);
    }

    // ────────────────────────────────────

    /// <summary>
    /// Updates the visible queue item fields from a parse result.
    /// </summary>
    /// <param name="item">The queue item.</param>
    /// <param name="result">The parse result.</param>
    private void UpdateItemFromResult(ReviewQueueItem item, BrokerParseResult result)
    {
        item.MessageType = result.MessageType;
        item.EventType = result.EventType;
        item.Confidence = result.Quality.Confidence;
        item.ContextSummary = BuildContextSummary(result);
        item.ValidationErrorsText = result.Quality.ValidationErrors.Count > 0
            ? string.Join(Environment.NewLine, result.Quality.ValidationErrors)
            : "None";
        item.AmbiguityFlagsText = result.Quality.AmbiguityFlags.Count > 0
            ? string.Join(Environment.NewLine, result.Quality.AmbiguityFlags)
            : "None";
        item.RequiresReview = result.MessageType == Core.Enums.BrokerMessageType.Unknown
            || result.Quality.Confidence < ReadReviewThreshold()
            || result.Quality.ValidationErrors.Count > 0
            || result.Quality.AmbiguityFlags.Count > 0;
        item.ReviewReason = BuildReviewReason(result, ReadReviewThreshold());
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds a review reason string from a parse result.
    /// </summary>
    /// <param name="result">The parse result.</param>
    /// <param name="threshold">The review threshold.</param>
    /// <returns>A review reason string.</returns>
    private static string BuildReviewReason(BrokerParseResult result, double threshold)
    {
        var reasons = new List<string>();

        if (result.MessageType == Core.Enums.BrokerMessageType.Unknown)
        {
            reasons.Add("UnknownMessageType");
        }

        if (result.Quality.Confidence < threshold)
        {
            reasons.Add($"LowConfidence<{threshold:F2}");
        }

        if (result.Quality.ValidationErrors.Count > 0)
        {
            reasons.Add("ValidationErrors");
        }

        if (result.Quality.AmbiguityFlags.Count > 0)
        {
            reasons.Add("AmbiguityFlags");
        }

        return reasons.Count > 0 ? string.Join(" | ", reasons) : "No review required";
    }

    // ────────────────────────────────────

    /// <summary>
    /// Builds a compact human-readable summary of the parse result.
    /// </summary>
    /// <param name="result">The parse result.</param>
    /// <returns>A context summary string.</returns>
    private static string BuildContextSummary(BrokerParseResult result)
    {
        return
            $"Instrument: Pair={ValueOrDash(result.Instrument.Pair)}, Tenor={ValueOrDash(result.Instrument.Tenor)}, Structure={ValueOrDash(result.Instrument.Structure)}, Delta={result.Instrument.Delta?.ToString() ?? "-"}{Environment.NewLine}" +
            $"Quote: Bid={result.Quote.Bid?.ToString() ?? "-"}, Ask={result.Quote.Ask?.ToString() ?? "-"}, Mid={result.Quote.Mid?.ToString() ?? "-"}, Style={result.Quote.QuoteStyle}, Firm={result.Quote.IsFirm?.ToString() ?? "-"}{Environment.NewLine}" +
            $"Action: Verb={ValueOrDash(result.Action.Verb)}, Side={ValueOrDash(result.Action.Side)}, Target={ValueOrDash(result.Action.Target)}, Linked={result.Action.LinkedToPriorQuote?.ToString() ?? "-"}{Environment.NewLine}" +
            $"Interest: Side={ValueOrDash(result.Interest.Side)}, Description={ValueOrDash(result.Interest.Description)}{Environment.NewLine}" +
            $"Context: Used={result.ContextUsage.UsedContext}, Resolved={string.Join(", ", result.ContextUsage.ResolvedFromContext)}, Unresolved={string.Join(", ", result.ContextUsage.UnresolvedReferences)}";
    }

    // ────────────────────────────────────

    /// <summary>
    /// Deserializes a broker parse result from JSON.
    /// </summary>
    /// <param name="json">The JSON payload.</param>
    /// <param name="result">The deserialized parse result.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise <c>false</c>.</returns>
    private static bool TryDeserializeBrokerParseResult(string json, out BrokerParseResult result)
    {
        try
        {
            result = JsonSerializer.Deserialize<BrokerParseResult>(json, SerializerOptions) ?? new BrokerParseResult();
            return true;
        }
        catch
        {
            result = new BrokerParseResult();
            return false;
        }
    }

    // ────────────────────────────────────

    /// <summary>
    /// Returns a dash when the supplied value is blank.
    /// </summary>
    /// <param name="value">The input value.</param>
    /// <returns>The original value or a dash.</returns>
    private static string ValueOrDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }
}