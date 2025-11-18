using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using LocalWhisper.Models;

namespace LocalWhisper.Services;

/// <summary>
/// LLM-based post-processing service using llama.cpp.
/// Formats transcripts while preserving meaning.
/// </summary>
/// <remarks>
/// Iteration 7: US-060, US-061, US-062
/// See: ADR-0010 (LLM Post-Processing Architecture)
/// </remarks>
public class LlmPostProcessor
{
    private const string MarkdownTriggerPattern = @"\bmarkdown\s+mode\b";

    /// <summary>
    /// Detect if transcript contains "markdown mode" trigger.
    /// Checks first ~20 words and last ~20 words only.
    /// </summary>
    /// <param name="transcript">Raw transcript text</param>
    /// <returns>Tuple of (isMarkdown, cleanedTranscript)</returns>
    public (bool isMarkdown, string cleaned) DetectMarkdownMode(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return (false, transcript);
        }

        // Extract first and last ~20 words
        var words = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstWords = string.Join(" ", words.Take(20));
        var lastWords = string.Join(" ", words.TakeLast(20));

        // Check for trigger in start or end
        var isMarkdown = Regex.IsMatch(firstWords, MarkdownTriggerPattern, RegexOptions.IgnoreCase) ||
                         Regex.IsMatch(lastWords, MarkdownTriggerPattern, RegexOptions.IgnoreCase);

        // If found, strip the trigger phrase
        var cleaned = isMarkdown ? StripMarkdownTrigger(transcript) : transcript;

        return (isMarkdown, cleaned);
    }

    /// <summary>
    /// Remove "markdown mode" trigger from transcript.
    /// </summary>
    public string StripMarkdownTrigger(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Remove trigger at start (with optional trailing punctuation/whitespace)
        var cleaned = Regex.Replace(text, @"^\s*markdown\s+mode\s*[.,;:]?\s*", "", RegexOptions.IgnoreCase);

        // Remove trigger at end (with optional leading punctuation/whitespace)
        cleaned = Regex.Replace(cleaned, @"\s*[.,;:]?\s*markdown\s+mode\s*$", "", RegexOptions.IgnoreCase);

        return cleaned.Trim();
    }

    /// <summary>
    /// Build system prompt for LLM.
    /// </summary>
    /// <param name="isMarkdownMode">If true, use Markdown prompt; else plain text</param>
    /// <param name="glossary">Glossary entries to append (optional)</param>
    /// <returns>System prompt string</returns>
    public string BuildSystemPrompt(bool isMarkdownMode, Dictionary<string, string> glossary)
    {
        var sb = new StringBuilder();

        if (isMarkdownMode)
        {
            // Markdown mode prompt
            sb.AppendLine("System: You are a careful transcript formatter and light copy editor.");
            sb.AppendLine();
            sb.AppendLine("INPUT: Raw text from speech recognition (Whisper). May contain run-on sentences, missing punctuation.");
            sb.AppendLine();
            sb.AppendLine("YOUR GOAL: Make text easy to read while preserving intent and personality.");
            sb.AppendLine();
            sb.AppendLine("DO:");
            sb.AppendLine("- Fix grammar, punctuation, capitalization.");
            sb.AppendLine("- Split long sentences when it improves clarity.");
            sb.AppendLine("- Insert paragraph breaks between distinct topics.");
            sb.AppendLine("- Use Markdown formatting (## Heading, **bold**, - lists).");
            sb.AppendLine("- Remove filler words (\"uh\", \"um\", \"like\") when safe.");
            sb.AppendLine();
            sb.AppendLine("DON'T:");
            sb.AppendLine("- Don't add new ideas or explanations.");
            sb.AppendLine("- Don't change meaning.");
            sb.AppendLine("- Don't summarize or shorten.");
            sb.AppendLine("- Don't change technical terms or names.");
            sb.AppendLine();
            sb.AppendLine("OUTPUT: Markdown formatted text.");
        }
        else
        {
            // Plain text mode prompt (default)
            sb.AppendLine("System: You are a careful transcript formatter and light copy editor.");
            sb.AppendLine();
            sb.AppendLine("INPUT: Raw text from speech recognition (Whisper). May contain run-on sentences, missing punctuation.");
            sb.AppendLine();
            sb.AppendLine("YOUR GOAL: Make text easy to read while preserving intent and personality.");
            sb.AppendLine();
            sb.AppendLine("DO:");
            sb.AppendLine("- Fix grammar, punctuation, capitalization.");
            sb.AppendLine("- Split long sentences when it improves clarity.");
            sb.AppendLine("- Insert paragraph breaks between distinct topics.");
            sb.AppendLine("- Turn clearly spoken lists into simple bullets (- item) or numbers (1. item).");
            sb.AppendLine("- Remove filler words (\"uh\", \"um\", \"like\") when safe.");
            sb.AppendLine();
            sb.AppendLine("DON'T:");
            sb.AppendLine("- Don't add new ideas or explanations.");
            sb.AppendLine("- Don't change meaning.");
            sb.AppendLine("- Don't summarize or shorten.");
            sb.AppendLine("- Don't change technical terms or names.");
            sb.AppendLine("- Don't use Markdown headings, bold, italics.");
            sb.AppendLine();
            sb.AppendLine("OUTPUT: Plain text only. Blank lines between paragraphs. Simple lists only.");
        }

        // Append glossary if provided
        if (glossary != null && glossary.Count > 0)
        {
            var glossaryLoader = new GlossaryLoader();
            sb.Append(glossaryLoader.FormatGlossaryForPrompt(glossary));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Build command line arguments for llama-cli.exe.
    /// </summary>
    public string BuildCommandLineArgs(PostProcessingConfig config, string systemPrompt, string transcript)
    {
        var args = new List<string>();

        // Model path
        args.Add($"-m \"{config.ModelPath}\"");

        // System prompt + user transcript
        var fullPrompt = $"{systemPrompt}\n\nUser: {transcript}\n\nAssistant:";
        args.Add($"-p \"{fullPrompt}\"");

        // System prompt (alternative approach - using -sys flag)
        args.Add($"-sys \"{systemPrompt}\"");

        // Generation parameters
        args.Add($"--temp {config.Temperature:F1}");
        args.Add($"--top-p {config.TopP:F2}");
        args.Add($"--repeat-penalty {config.RepeatPenalty:F2}");
        args.Add($"-n {config.MaxTokens}");

        // GPU acceleration
        if (config.GpuAcceleration)
        {
            args.Add("-ngl 99"); // Offload all layers to GPU
        }

        // Quiet mode (suppress progress output)
        args.Add("--log-disable");

        return string.Join(" ", args);
    }

    /// <summary>
    /// Process transcript using LLM.
    /// </summary>
    /// <param name="transcript">Raw transcript text</param>
    /// <param name="config">Post-processing configuration</param>
    /// <param name="glossary">Glossary entries (optional)</param>
    /// <returns>Processed transcript</returns>
    /// <exception cref="TimeoutException">If processing exceeds timeout</exception>
    /// <exception cref="InvalidOperationException">If LLM process fails</exception>
    public async Task<string> ProcessAsync(
        string transcript,
        PostProcessingConfig config,
        Dictionary<string, string>? glossary)
    {
        // Detect markdown mode and clean transcript
        var (isMarkdown, cleanedTranscript) = DetectMarkdownMode(transcript);

        // Build system prompt
        var systemPrompt = BuildSystemPrompt(isMarkdown, glossary ?? new Dictionary<string, string>());

        // Build command line arguments
        var args = BuildCommandLineArgs(config, systemPrompt, cleanedTranscript);

        // Execute llama-cli.exe
        var processInfo = new ProcessStartInfo
        {
            FileName = config.LlmCliPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeoutMs = config.TimeoutSeconds * 1000;
            var completed = await Task.Run(() => process.WaitForExit(timeoutMs));

            if (!completed)
            {
                process.Kill();
                throw new TimeoutException($"LLM processing timed out after {config.TimeoutSeconds}s");
            }

            if (process.ExitCode != 0)
            {
                var error = errorBuilder.ToString();
                throw new InvalidOperationException($"LLM process failed with exit code {process.ExitCode}: {error}");
            }

            var output = outputBuilder.ToString().Trim();

            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException("LLM process returned empty output");
            }

            AppLogger.LogInformation("LLM post-processing completed", new
            {
                IsMarkdown = isMarkdown,
                InputLength = transcript.Length,
                OutputLength = output.Length,
                DurationMs = process.TotalProcessorTime.TotalMilliseconds
            });

            return output;
        }
        catch (Exception ex)
        {
            AppLogger.LogError("LLM post-processing failed", ex, new
            {
                LlmCliPath = config.LlmCliPath,
                ModelPath = config.ModelPath
            });
            throw;
        }
    }
}
