using Refit;

namespace BotSharp.Plugin.HuggingFace.Services;

/// <summary>
/// https://huggingface.co/docs/api-inference/quicktour
/// </summary>
public interface IInferenceApi
{
    [Post("/models/{space}/{model}")]
    Task<List<TextGenResponse>> TextGenerate(string space, string model, [Body] InferenceInput input);
}
