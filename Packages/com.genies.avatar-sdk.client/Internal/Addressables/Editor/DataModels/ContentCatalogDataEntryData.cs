using System;
using System.Collections.Generic;

namespace Genies.Addressables.Editor.DataModels
{
    /// <summary>
    /// Model for capturing and combining addressable ContentCatalogDataEntries
    /// </summary>
    public class ContentCatalogDataEntryData
    {
        public int index;
        public List<DataType> types;
        public string internalId;
        public string provider;
        public List<object> keys;
        public List<string> dependencies;

        public struct DataType
        {
            public Type type;
            public object data;
        }
    }
}
