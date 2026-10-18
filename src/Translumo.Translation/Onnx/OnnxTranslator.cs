using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Tokenizers.DotNet;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Exceptions;

namespace Translumo.Translation.Onnx
{
    public class OnnxContainer : TranslationContainer
    {
        public OnnxContainer() : base(null, true)
        {
        }
    }

    public class OnnxTranslator : BaseTranslator<OnnxContainer>
    {
        private readonly string _modelBasePath;
        private InferenceSession _encoderSession;
        private InferenceSession _decoderSession;
        private Tokenizer _tokenizer;
        private bool _isDownloading;
        private string _loadedModelPair; // Track which language pair is currently loaded
        private string _lastSourceText; // Track the last source text to avoid redundant translations

        // Token IDs (read from model config)
        private int _padTokenId = 0;
        private int _eosTokenId = 0;
        private int _decoderStartTokenId = 0;
        private int _maxLength = 128;
        private int _numBeams = 2;

        // Constants
        private readonly int _beamCountLimit = 2; // Limit the number of beams to a reasonable number for performance
        private const float _repetitionPenalty = 1.1f; // Penalty factor to discourage repetition. Recommended values between 1.1f - 1.3f.

        // Bad word IDs that should never be generated (from model config)
        private HashSet<int> _badWordIds = new HashSet<int>();

        public OnnxTranslator(TranslationConfiguration translationConfiguration, LanguageService languageService, ILogger logger)
            : base(translationConfiguration, languageService, logger)
        {
            _beamCountLimit = translationConfiguration.BeamCount;
            _modelBasePath = string.IsNullOrWhiteSpace(translationConfiguration.OnnxModelPath)
                ? "Models"
                : translationConfiguration.OnnxModelPath;            
        }

        protected override IList<OnnxContainer> CreateContainers(TranslationConfiguration configuration)
        {
            return new List<OnnxContainer> { new OnnxContainer() };
        }

        private async Task<bool> EnsureModelLoadedAsync(string sourceLang, string targetLang)
        {
            string pair = $"{sourceLang}-{targetLang}";

            // If sessions are loaded but for a different language pair, dispose and reload
            if (_loadedModelPair != null && _loadedModelPair != pair)
            {
                Logger.LogInformation($"Language pair changed from {_loadedModelPair} to {pair}, reloading model...");
                DisposeModelSessions();
            }

            if (_encoderSession != null && _decoderSession != null && _tokenizer != null)
                return true;

            string modelDir = Path.Combine(_modelBasePath, pair);

            if (!Directory.Exists(modelDir) ||
                !File.Exists(Path.Combine(modelDir, "encoder_model.onnx")) ||
                !File.Exists(Path.Combine(modelDir, "decoder_model.onnx")) ||
                !File.Exists(Path.Combine(modelDir, "tokenizer.json")) ||
                !File.Exists(Path.Combine(modelDir, "generation_config.json"))
                )
            {
                if (!_isDownloading)
                {
                    _isDownloading = true;
                    Logger.LogInformation($"Downloading ONNX model for {pair}...");
                    _ = Task.Run(() => DownloadModelAsync(pair, modelDir));
                }
                return false;
            }

            try
            {
                var options = new SessionOptions
                {
                    IntraOpNumThreads = 1,
                    InterOpNumThreads = 1
                };

                _encoderSession = new InferenceSession(Path.Combine(modelDir, "encoder_model.onnx"), options);
                _decoderSession = new InferenceSession(Path.Combine(modelDir, "decoder_model.onnx"), options);

                _tokenizer = new Tokenizer(Path.Combine(modelDir, "tokenizer.json"));
                _loadedModelPair = pair;

                // Load configuration to get special token IDs
                var generationConfigPath = Path.Combine(modelDir, "generation_config.json");
                if (File.Exists(generationConfigPath))
                {
                    var json = File.ReadAllText(generationConfigPath);

                    var config = JsonSerializer.Deserialize<GenerationConfig>(json);

                    _padTokenId = config?.PadTokenId ?? _padTokenId;
                    _eosTokenId = config?.EosTokenId ?? _eosTokenId;
                    _maxLength = config?.MaxLength ?? _maxLength;
                    _numBeams = config?.NumBeams ?? 1; // if model config doesn't specify, default to 1 (greedy search)
                    _numBeams = Math.Min(_numBeams, _beamCountLimit);

                    // _decoderStartTokenId is determined by the following priority:
                    if (config?.DecoderStartTokenId.HasValue == true)
                        _decoderStartTokenId = config.DecoderStartTokenId.Value;                    
                    else if (config?.ForcedBosTokenId.HasValue == true)
                        _decoderStartTokenId = config.ForcedBosTokenId.Value;                    
                    else if (config?.BosTokenId.HasValue == true)
                        _decoderStartTokenId = config.BosTokenId.Value;

                    // _badWordIds
                    _badWordIds = new HashSet<int> { _padTokenId };
                    if (config?.BadWordsIds?.Count > 0)
                    {
                        _badWordIds = config.BadWordsIds
                            .SelectMany(x => x)
                            .ToHashSet();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                DisposeModelSessions();
                throw new TranslationException($"Failed to load ONNX model from {modelDir}", ex);
            }
        }

        private void DisposeModelSessions()
        {
            _encoderSession?.Dispose();
            _encoderSession = null;
            _decoderSession?.Dispose();
            _decoderSession = null;
            _tokenizer = null;
            _loadedModelPair = null;
        }

        private async Task DownloadModelAsync(string pair, string dir)
        {
            try
            {
                Directory.CreateDirectory(dir);
                string baseUrl = $"https://huggingface.co/Xenova/opus-mt-{pair}/resolve/main";
                using var client = new HttpClient();
                client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

                var files = new[]
                {
                    "onnx/encoder_model.onnx",
                    "onnx/decoder_model.onnx",
                    "tokenizer.json",
                    "generation_config.json"
                };

                foreach (var file in files)
                {
                    string localFile = Path.Combine(dir, Path.GetFileName(file));
                    string tempFile = localFile + ".tmp";
                    if (!File.Exists(localFile))
                    {
                        Logger.LogInformation($"Downloading {file}...");
                        var response = await client.GetAsync($"{baseUrl}/{file}").ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();

                        // Download to a temp file first, then rename to avoid partial files
                        using (var fs = new FileStream(tempFile, FileMode.Create))
                        {
                            await response.Content.CopyToAsync(fs).ConfigureAwait(false);
                        }
                        File.Move(tempFile, localFile);

                        if (file == "tokenizer.json")
                        {
                            try
                            {
                                var text = await File.ReadAllTextAsync(localFile);
                                int start = text.IndexOf("\"normalizer\":");
                                int end = text.IndexOf("\"pre_tokenizer\":");
                                if (start != -1 && end != -1)
                                {
                                    // Substring(0, start) contains the comma from the previous property (e.g. `],\n  `)
                                    // So we just concatenate them directly without removing commas!
                                    text = text.Substring(0, start) + text.Substring(end);
                                    await File.WriteAllTextAsync(localFile, text);
                                    Logger.LogInformation("Patched tokenizer.json for Rust compatibility (removed null normalizer).");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError(ex, "Failed to patch tokenizer.json");
                            }
                        }
                    }
                }
                Logger.LogInformation($"ONNX model download complete for {pair}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to download ONNX model for {pair}");
                // Clean up partial downloads so next attempt starts fresh
                try
                {
                    foreach (var tmpFile in Directory.GetFiles(dir, "*.tmp"))
                    {
                        File.Delete(tmpFile);
                    }
                }
                catch { /* best effort cleanup */ }
            }
            finally
            {
                _isDownloading = false;
            }
        }

        protected override async Task<string> TranslateTextInternal(OnnxContainer container, string sourceText)
        {
            // Nothing to translate
            if (string.IsNullOrWhiteSpace(sourceText?.Trim()))
                return string.Empty;
            
            // Avoid redundant translation if the source text hasn't changed
            if (sourceText.Trim() == _lastSourceText)
                return string.Empty;

            _lastSourceText = sourceText.Trim();

            string srcLang = SourceLangDescriptor.Code.Substring(0, 2).ToLower();
            string tgtLang = TargetLangDescriptor.Code.Substring(0, 2).ToLower();

            bool isReady = await EnsureModelLoadedAsync(srcLang, tgtLang);
            if (!isReady)
            {
                return $"The ONNX model files for “{srcLang}-{tgtLang}” are missing.\nI'll try to download them.\nPlease wait a few minutes and try the translation again.";
            }

            // Translate using Beam search
            var outputTokens = Translate(sourceText);
            var decodedText = _tokenizer.Decode(outputTokens);

            return decodedText.Trim();
        }

        public uint[] Translate(string sourceText)
        {
            // 1. Tokenize Input
            long[] encoderInputIds = _tokenizer.Encode(sourceText).Select(id => (long)id).ToArray();
            int inputLength = encoderInputIds.Length;
            
            long[] encoderAttentionMask = Enumerable.Repeat(1L, inputLength).ToArray();

            // 2. Run encoder
            var encoderInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(encoderInputIds, new int[] { 1, inputLength })),
                NamedOnnxValue.CreateFromTensor("attention_mask", new DenseTensor<long>(encoderAttentionMask, new int[] { 1, inputLength }))
            };

            using var encoderResults = _encoderSession.Run(encoderInputs);
            var lastHiddenState = encoderResults.First().AsTensor<float>();
            var encoderAttentionMaskTensor = new DenseTensor<long>(encoderAttentionMask, new[] { 1, inputLength });

            // 3. Initialize beam search state
            // Each beam stores the generated token sequence and its cumulative log score (starting at 0.0)
            var beams = new List<(List<int> Tokens, double Score)>
            {
                (new List<int> { _decoderStartTokenId }, 0.0)
            };

            var finalCandidates = new List<(List<int> Tokens, double Score)>();

            // 4. Autoregressive text generation loop
            for (int step = 0; step < _maxLength; step++)
            {
                var candidates = new List<(List<int> Tokens, double Score)>();
                foreach (var beam in beams)
                {
                    // If the beam generated an EOS token, move it to the list of completed candidates
                    if (beam.Tokens.Last() == _eosTokenId && beam.Tokens.Count > 1)
                    {
                        finalCandidates.Add(beam);
                        continue;
                    }

                    // Prepare decoder inputs
                    long[] decoderInputIds = beam.Tokens.Select(x => (long)x).ToArray();

                    // Standard Marian ONNX models require these basic inputs
                    var decoderInputs = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor("input_ids", new DenseTensor<long>(decoderInputIds, new int[] { 1, decoderInputIds.Length })),
                        NamedOnnxValue.CreateFromTensor("encoder_hidden_states", lastHiddenState),
                        NamedOnnxValue.CreateFromTensor("encoder_attention_mask", encoderAttentionMaskTensor)
                    };

                    // Execute decoder inference
                    using var decoderResults = _decoderSession.Run(decoderInputs);
                    var logits = decoderResults.First().AsTensor<float>(); // Shape: [1, sequence_length, vocab_size]

                    int vocabSize = logits.Dimensions[2]; // Get vocabulary size
                    int lastTokenOffset = (decoderInputIds.Length - 1) * vocabSize;

                    // Extract logits for the last generated token only
                    float[] lastLogits = new float[vocabSize];
                    for (int i = 0; i < vocabSize; i++)
                    {
                        lastLogits[i] = logits.GetValue(lastTokenOffset + i);
                    }

                    // Repetition penalty
                    foreach (var usedToken in beam.Tokens.Distinct())
                    {
                        if (usedToken == _decoderStartTokenId)
                            continue;
                        float val = lastLogits[usedToken];
                        lastLogits[usedToken] = val > 0
                            ? val / _repetitionPenalty
                            : val * _repetitionPenalty;
                    }

                    // Exclude forbidden tokens
                    foreach (int badWordId in _badWordIds)
                    {
                        if (badWordId >= 0 && badWordId < lastLogits.Length)
                        {
                            lastLogits[badWordId] = float.NegativeInfinity;
                        }
                    }

                    // Apply softmax to obtain log probabilities
                    var logProbs = ApplySoftmaxAndLog(lastLogits);

                    // Select the Top-N (_numBeams) best token candidates for this beam
                    var topTokens = logProbs
                        .Select((logProb, idx) => (Id: idx, LogProb: logProb))
                        .OrderByDescending(x => x.LogProb)
                        .Take(_numBeams);

                    foreach (var token in topTokens)
                    {
                        var newTokens = new List<int>(beam.Tokens) { token.Id };
                        candidates.Add((newTokens, beam.Score + token.LogProb));
                    }
                }

                // If no new candidates were generated, stop decoding
                if (candidates.Count == 0) break;

                // Keep the globally best beams across all candidate combinations
                beams = candidates
                    .OrderByDescending(x => x.Score)
                    .Take(_numBeams)
                    .ToList();

                // Early stopping
                if (finalCandidates.Count >= _numBeams && finalCandidates.Count != 0)
                    break;

                // If all active beams reached EOS, decoding can be stopped early
                if (beams.All(b => b.Tokens.Last() == _eosTokenId))
                {
                    finalCandidates.AddRange(beams);
                    break;
                }
            }

            // 5. Select the best final result
            var bestResult = finalCandidates.Concat(beams)
                .Where(b => b.Tokens.Count > 1)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            // Fallback: return an empty sequence if no valid result was generated
            if (bestResult.Tokens == null) return Array.Empty<uint>();

            // Remove the decoder start token and EOS token before returning the final sequence
            return bestResult.Tokens
                .Where(id => id != _decoderStartTokenId && id != _eosTokenId)
                .Select(id => (uint)id)
                .ToArray();
        }

        private static double[] ApplySoftmaxAndLog(float[] logits)
        {
            float max = logits.Max();
            double sum = 0;
            double[] exps = new double[logits.Length];

            for (int i = 0; i < logits.Length; i++)
            {
                exps[i] = Math.Exp(logits[i] - max);
                sum += exps[i];
            }

            double[] logProbs = new double[logits.Length];
            for (int i = 0; i < logits.Length; i++)
            {
                // Directly compute log probabilities for numerical stability and efficient score accumulation
                logProbs[i] = Math.Log(exps[i] / sum);
            }

            return logProbs;
        }
    }
}
