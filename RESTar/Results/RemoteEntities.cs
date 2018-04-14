using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RESTar.Results
{
    /// <inheritdoc />
    /// <summary>
    /// An entity collection received from a remote RESTar service
    /// </summary>
    internal class RemoteEntities : Entities<JObject>
    {
        public override IEnumerable<T> ToEntities<T>() => this.Select(jobj => jobj.ToObject<T>());
        internal RemoteEntities(IRequest request, IEnumerable<JObject> enumerable) : base(request, enumerable) { }
    }

    internal class JObjectEnumerable : IEnumerable<JObject>
    {
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<JObject> GetEnumerator() => Enumerator;
        private JObjectEnumerator Enumerator { get; }
        public JObjectEnumerable(Stream dataStream) => Enumerator = new JObjectEnumerator(dataStream);
    }

    internal class JObjectEnumerator : IEnumerator<JObject>
    {
        private readonly Stream DataStream;
        private readonly JsonReader JsonReader;

        public JObjectEnumerator(Stream dataStream)
        {
            DataStream = dataStream;
            JsonReader = new JsonTextReader(new StreamReader(DataStream, RESTarConfig.DefaultEncoding)) {CloseInput = true};
            JsonReader.Read();
        }

        public void Dispose() => JsonReader.Close();

        public bool MoveNext()
        {
            try
            {
                Current = JObject.LoadAsync(JsonReader).Result;
                JsonReader.Read();
                JsonReader.TokenType
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Reset()
        {
            DataStream.Seek(0, SeekOrigin.Begin);
            Current = null;
        }

        object IEnumerator.Current => Current;

        public JObject Current { get; private set; }
    }
}