using System;

namespace ImageGeneratorApp
{
    public class HistoryItemCopiedEventArgs : EventArgs
    {
        public string Prompt { get; }
        public string ModelName { get; }

        public HistoryItemCopiedEventArgs(string prompt, string modelName)
        {
            Prompt = prompt;
            ModelName = modelName;
        }
    }
}
