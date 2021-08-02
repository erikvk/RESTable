using System.Collections.Generic;
using System.Text.Json.Serialization;
using RESTable.Results;

namespace RESTable.DefaultProtocol.Serialized
{
    [UseDefaultConverter]
    public class SerializedChange<T> : ISerialized where T : class
    {
        private Change<T> Change { get; }

        public string Status => "success";

        public IReadOnlyCollection<T> Data => Change.Entities;

        public long DataCount => Data.Count;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool TooManyEntitiesToIncludeInBody => Change.TooManyEntities;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? UpdatedCount { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? InsertedCount { get; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? DeletedCount { get; }

        public double TimeElapsedMs => Change.TimeElapsed.GetRESTableElapsedMs();

        public SerializedChange(Change<T> change)
        {
            Change = change;
            switch (change)
            {
                case UpdatedEntities<T>:
                {
                    UpdatedCount = change.Count;
                    break;
                }
                case InsertedEntities<T>:
                {
                    InsertedCount = change.Count;
                    break;
                }
                case DeletedEntities<T>:
                {
                    DeletedCount = change.Count;
                    break;
                }
                case SafePostedEntities<T> spe:
                {
                    UpdatedCount = spe.UpdatedCount;
                    InsertedCount = spe.InsertedCount;
                    break;
                }
            }
        }
    }
}