﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Rasa
{
    public class RasaStatusModel
    {
        [JsonProperty("available_projects")]
        public JObject AvailableProjects { get; set; }
    }

    public class RasaProjectModel
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("available_models")]
        public List<string> AvailableModels { get; set; }

        [JsonProperty("loaded_models")]
        public List<string> LoadedModels { get; set; }
    }
}
