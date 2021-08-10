using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RESTable.Requests.Processors;

namespace RESTable.Meta
{
    /// <inheritdoc />
    /// <summary>
    /// A term denotes a node in a static or dynamic member tree. Contains a chain of properties, 
    /// used in queries to refer to properties and properties of properties.
    /// </summary>
    public class Term : IEnumerable<Property>
    {
        internal readonly List<Property> Store;
        private readonly string ComponentSeparator;

        /// <summary>
        /// A string representation of the path to the property, using dot notation
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// The property path using the actual names of properties, as defined in the type
        /// declaration.
        /// </summary>
        public string ActualNamesKey { get; private set; }

        /// <summary>
        /// Is this term static? (Are all of its containing property references denoting declared members?)
        /// </summary>
        public bool IsDeclared { get; private set; }

        /// <summary>
        /// Is this term dynamic? (Are not all of its containing property references denoting declared members?)
        /// </summary>
        public bool IsDynamic => !IsDeclared;

        /// <summary>
        /// Automatically sets the Skip property of conditions matched against this term to true
        /// </summary>
        public bool ConditionSkip { get; private set; }

        /// <summary>
        /// Gets the first property reference of the term, and safe casts it to T
        /// </summary>
        public T? FirstAs<T>() where T : Property => First as T;

        /// <summary>
        /// Gets the first property reference of the term, or null of the term is empty
        /// </summary>
        public Property? First => Store.Count > 0 ? Store[0] : null;

        /// <summary>
        /// Gets the last property reference of the term, and safe casts it to T
        /// </summary>
        public T? LastAs<T>() where T : Property => Store.LastOrDefault() as T;

        /// <summary>
        /// Gets the last property reference of the term, or null of the term is empty
        /// </summary>
        public Property? Last => Store.Count > 0 ? Store[Store.Count - 1] : null;

        /// <summary>
        /// A cache with values for whether some flag (key) is present on all
        /// properties in this term. Flags should not change for properties after
        /// they are created.
        /// </summary>
        private IDictionary<string, bool>? Flags { get; set; }

        /// <summary>
        /// Returns true if and only if all properties in the term has a given flag
        /// </summary>
        public bool HasFlag(string flag)
        {
            Flags ??= new Dictionary<string, bool>();
            if (Flags.TryGetValue(flag, out var value))
                return value;
            return Flags[flag] = Store.All(p => p.Flags.Contains(flag));
        }

        /// <summary>
        /// Counts the properties of the Term
        /// </summary>
        public int Count => Store.Count;

        public Term(string componentSeparator)
        {
            Key = null!;
            ActualNamesKey = null!;

            Store = new List<Property>();
            ComponentSeparator = componentSeparator;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        public IEnumerator<Property> GetEnumerator() => Store.GetEnumerator();

        internal void SetCommonProperties()
        {
            var isDeclared = true;
            var conditionSkip = false;
            foreach (var property in Store)
            {
                isDeclared = isDeclared && property is DeclaredProperty;
                if (property is DeclaredProperty { SkipConditions: true })
                    conditionSkip = true;
            }
            IsDeclared = isDeclared;
            ConditionSkip = conditionSkip;
            Key = GetKey();
            ActualNamesKey = GetActualNameKey();
        }

        /// <summary>
        /// The empty term, used when building terms
        /// </summary>
        internal static Term Empty(string componentSeparator)
        {
            var empty = new Term(componentSeparator);
            empty.SetCommonProperties();
            return empty;
        }

        /// <summary>
        /// Converts all properties in this term to dynamic properties
        /// </summary>
        private static Term MakeDynamic(Term term)
        {
            if (term.IsDynamic) return term;
            var newTerm = new Term(term.ComponentSeparator);
            foreach (var prop in term.Store)
            {
                if (prop.IsDynamic)
                    newTerm.Store.Add(prop);
                else newTerm.Store.Add(DynamicProperty.Parse(prop.Name));
            }
            newTerm.IsDeclared = false;
            newTerm.Key = newTerm.GetKey();
            return newTerm;
        }

        private string GetKey(string componentSeparator) => string.Join(componentSeparator, Store.Select(p => p.Name));
        private string GetKey() => string.Join(ComponentSeparator, Store.Select(p => p.Name));
        private string GetActualNameKey() => string.Join(".", Store.Select(p => p.ActualName));

        /// <summary>
        /// Returns the value that this term denotes for a given target object as well as
        /// the actual key for this property (matching is case insensitive), the parent
        /// of the denoted value, and the property representing the denoted value.
        /// </summary>
        public ValueTask<TermValue> GetValue(object? target)
        {
            async ValueTask<TermValue> getTermValue(Term term)
            {
                object? parent = null;
                Property? property = null;
                string actualKey;
                object? value;

                // If the target is the result of processing using some IProcessor, the type
                // will be JsonElement. In that case, the object may contain the entire term key
                // as member, even if the term has multiple properties (common result of add 
                // and select). This code handles those cases.
                if (target is ProcessedEntity processedEntity)
                {
                    if (processedEntity.TryFindInDictionary(term.Key, out var processedKey, out var processedValue))
                    {
                        actualKey = processedKey!;
                        parent = processedEntity;
                        property = DynamicProperty.Parse(term.Key);
                        value = processedValue;
                        return new TermValue(value, actualKey!, parent, property);
                    }
                    term = MakeDynamic(term);
                }

                // Walk over the properties in the term, and if null is encountered, simply
                // keep the null. Else continue evaluating the next property as a property of the
                // previous property value.
                for (var i = 0; target is not null && i < term.Store.Count; i++)
                {
                    parent = target;
                    property = term.Store[i];
                    target = await property.GetValue(target).ConfigureAwait(false);
                }

                // If the term is dynamic, we do not know the actual key beforehand. We instead
                // set names inside the dynamic properties when getting their values, and concatenate the
                // property names here.
                if (term.IsDynamic)
                    term.Key = term.GetKey();

                actualKey = term.Key;
                value = target;
                return new TermValue(value, actualKey, parent, property);
            }

            return getTermValue(this);
        }

        internal static Term Create(IEnumerable<DeclaredProperty> properties, string componentSeparator)
        {
            var newTerm = new Term(componentSeparator);
            newTerm.Store.AddRange(properties);
            newTerm.SetCommonProperties();
            return newTerm;
        }

        /// <summary>
        /// Creates a new term that is this term appended with the given term, that will evaluate to the
        /// final property in the given term.
        /// </summary>
        public static Term Append(Term term1, Term term2, bool checkTypes = true)
        {
            if (term1.IsDynamic)
                return Join(term1, MakeDynamic(term2));
            if (checkTypes && term2.First is DeclaredProperty next && term1.Last is DeclaredProperty last && last.Type != next.Owner)
                throw new InvalidOperationException($"Could not append term '{term1}' with '{term2}'. The first property " +
                                                    $"of the second term ({next}) is not a declared property of " +
                                                    $"the last property of the first term ({last}). Expected a " +
                                                    $"property declared in type '{last.Type}'");
            return Join(term1, term2);
        }

        /// <summary>
        /// Appends a property to the end of a term
        /// </summary>
        public static Term Append(Term term, Property property, bool checkTypes = true)
        {
            if (term.IsDynamic)
                return Join(term, DynamicProperty.Parse(property.Name, true));
            if (checkTypes && property is DeclaredProperty next && term.Last is DeclaredProperty last && last.Type != next.Owner)
                throw new InvalidOperationException($"Could not append property '{term}' with property '{property}'. " +
                                                    $"The new property is not a declared property of the last property " +
                                                    $"of the first term ({last}). Expected a property declared in type '{last.Type}'");
            return Join(term, property);
        }

        private static Term Join(Term term1, params Property[] properties)
        {
            return Join(term1, (IEnumerable<Property>) properties);
        }

        private static Term Join(Term term1, IEnumerable<Property> properties)
        {
            var joinedTerm = new Term(term1.ComponentSeparator);
            joinedTerm.Store.AddRange(term1);
            joinedTerm.Store.AddRange(properties);
            joinedTerm.SetCommonProperties();
            return joinedTerm;
        }

        /// <summary>
        /// Gets a string representation of the given term
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Key;

        private bool Equals(Term other) => string.Equals(Key, other.Key) && IsDeclared == other.IsDeclared;

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Term) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                return ((Key is not null ? Key.GetHashCode() : 0) * 397) ^ IsDeclared.GetHashCode();
            }
        }

        public readonly struct TermValue
        {
            public object? Value { get; }
            public string ActualKey { get; }
            public object? Parent { get; }
            public Property? Property { get; }

            public TermValue(object? value, string actualKey, object? parent, Property? property)
            {
                Value = value;
                ActualKey = actualKey;
                Parent = parent;
                Property = property;
            }
        }
    }
}