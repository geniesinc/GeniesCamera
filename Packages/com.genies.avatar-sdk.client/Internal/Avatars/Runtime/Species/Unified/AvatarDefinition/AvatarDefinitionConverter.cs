using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;

namespace Genies.Avatars
{
    public class AvatarDefinitionConverter : SimpleDefinitionConverter
    {
        private DefinitionToken _targetDefinition;
        private const string _versionKey = "JsonVersion";

        public AvatarDefinitionConverter() : base(
            //a list of version that we will support to apply the conversion
            // NOTE: it needs bo added on order from the old one to the new one
            new List<string>()
            {
                "1-0-0",
                "1-1-0",
            },
            targetVersion: "1-1-1"
        )
        {
            var targetDefinitionSerialized = AvatarExtensions.SerializedDefaultDefinition();
            DefinitionToken.TryParse(targetDefinitionSerialized, out DefinitionToken defToken, _versionKey);
            _targetDefinition = defToken;
        }

        protected override UniTask<DefinitionToken> ConvertAsync(DefinitionToken definition)
        {
            var token = definition.Token;

            foreach (var keyValuePair in _targetDefinition.Token)
            {
                if (!token.TryGetValue(keyValuePair.Key, out _))
                {
                    token.Add(keyValuePair.Key,keyValuePair.Value);
                }
            }

            //override the version
            definition.Token[_versionKey] = TargetVersion;

            var newDef = new DefinitionToken(token, _versionKey);
            return UniTask.FromResult(newDef);
        }
    }
}
