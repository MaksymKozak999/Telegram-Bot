using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace secondtgbot
{
    public class PasswordConfig
    {
        public int _length { get; set; } = 12;
        public bool _includeUppercase { get; set; } = true;
        public bool _includeNumbers {  get; set; } = true;
        public bool _includeCharacters { get; set; } = true;
        public bool _includeSymbols { get; set; } = true;


        public void Reset()
        {
            _length = 12;
            _includeCharacters = true;
            _includeNumbers = true;
            _includeSymbols = true;
            _includeUppercase = true;
        }
        // Gets the existing config for a chat ID or initializes a default one.
        public static PasswordConfig GetForChat(long chatId)
        {
            return _userConfigs.GetOrAdd(chatId, _ => new PasswordConfig());
        }
        // Internal thread-safe storage for all chats
        private static readonly ConcurrentDictionary<long, PasswordConfig> _userConfigs
            = new ConcurrentDictionary<long, PasswordConfig>();


    }
}
