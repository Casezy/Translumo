
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using Translumo.Infrastructure.Language;
using Translumo.Translation.Configuration;
using Translumo.Translation.Exceptions;
using Translumo.Utils.Http;

namespace Translumo.Translation.Google
{
    public class GoogleTranslator : BaseTranslator<GoogleContainer>
    {
        private const string TRANSLATE_URL = "https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}";

        public GoogleTranslator(TranslationConfiguration translationConfiguration, LanguageService languageService, ILogger logger) 
            : base(translationConfiguration, languageService, logger)
        {
        }

        public override Task<string> TranslateTextAsync(string sourceText)
        {
            //TODO: Temp implementation for specific lang
            if (TargetLangDescriptor.Language == Languages.PortugueseBrazil)
            {
                throw new TransactionException("Google translate is unavailable for this language");
            }

            return base.TranslateTextAsync(sourceText);
        }

        protected override async Task<string> TranslateTextInternal(GoogleContainer container, string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText))
            {
                return string.Empty;
            }

            string sourceLanguage = SourceLangDescriptor.Code;
            string targetLanguage = TargetLangDescriptor.Code;

            var url = string.Format(TRANSLATE_URL, sourceLanguage, targetLanguage, HttpUtility.UrlEncode(sourceText));

            var requestResult = await container.Reader.RequestWebDataAsync(url, HttpMethods.GET, null);

            if (!requestResult.IsSuccessful)
            {
                container.Block(); // clean cookies for the next attempt
                throw new TranslationException($"Google Translate request failed or timed out.");
            }

            try
            {
                // Parsing the returned JSON array from ://googleapis.com
                using (JsonDocument doc = JsonDocument.Parse(requestResult.Body))
                {
                    var rootArray = doc.RootElement;

                    // Verify the structure of the Google JSON response: [[[ "translation", "original", ... ]]]
                    if (rootArray.ValueKind == JsonValueKind.Array && rootArray.GetArrayLength() > 0)
                    {
                        var firstElement = rootArray[0];
                        if (firstElement.ValueKind == JsonValueKind.Array)
                        {
                            StringBuilder translatedText = new StringBuilder();

                            // Iterate through individual segments/sentences of the text
                            foreach (JsonElement segment in firstElement.EnumerateArray())
                            {
                                if (segment.ValueKind == JsonValueKind.Array && segment.GetArrayLength() > 0)
                                {
                                    var textPart = segment[0];
                                    if (textPart.ValueKind == JsonValueKind.String)
                                    {
                                        translatedText.Append(textPart.GetString());
                                    }
                                }
                            }

                            if (translatedText.Length > 0)
                            {
                                return translatedText.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                container.Block();
                throw new TranslationException("Failed to parse Google Translate JSON response.", ex);
            }

            throw new TranslationException("Google Translate returned an empty or unexpected response structure.");
        }

        protected override IList<GoogleContainer> CreateContainers(TranslationConfiguration configuration)
        {
            var result = configuration.ProxySettings.Select(proxy => new GoogleContainer(proxy)).ToList();
            result.Add(new GoogleContainer(isPrimary: true));

            return result;
        }
    }
}
