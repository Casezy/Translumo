using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Translumo.Translation.Onnx
{
    public class GenerationConfig
    {
        [JsonPropertyName("bad_words_ids")]
        public List<List<int>> BadWordsIds { get; set; }
        [JsonPropertyName("pad_token_id")]
        public int? PadTokenId { get; set; }

        [JsonPropertyName("eos_token_id")]
        public int? EosTokenId { get; set; }

        [JsonPropertyName("decoder_start_token_id")]
        public int? DecoderStartTokenId { get; set; }

        [JsonPropertyName("forced_bos_token_id")]
        public int? ForcedBosTokenId { get; set; }

        [JsonPropertyName("bos_token_id")]
        public int? BosTokenId { get; set; }
        [JsonPropertyName("max_length")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("num_beams")]
        public int? NumBeams { get; set; }        
    }
}
