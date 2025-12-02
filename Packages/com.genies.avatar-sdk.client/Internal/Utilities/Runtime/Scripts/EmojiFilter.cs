using System;
using TMPro;

namespace Genies.Utilities
{
    public class EmojiFilter
    {
        private DateTime _lastInputChange;
        private bool _emojiErrorsFixed;

        public void Reset()
        {
            _lastInputChange = DateTime.UtcNow;
            _emojiErrorsFixed = false;
        }

        public void FixEmoji(TMP_InputField input)
        {
            if (DateTime.UtcNow - _lastInputChange > TimeSpan.FromMilliseconds(100) &&
                !_emojiErrorsFixed)
            {
                FixEmojiErrors(input);
                _emojiErrorsFixed = true;
            }
        }

        private void FixEmojiErrors(TMP_InputField input)
        {
            var processedStr = EmojiUtils.FilterEmojiErrors(input.text);
            input.text = processedStr;
        }
    }
}
