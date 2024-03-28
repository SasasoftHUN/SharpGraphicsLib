using SharpGraphics.Shaders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SharpGraphics
{
    public readonly struct PipelineResourceProperties : IEquatable<PipelineResourceProperties>
    {

        #region Fields

        private readonly PipelineResourceProperty[]? _properties; //Null can be valid only when initialized with default constructor

        #endregion

        #region Properties

        public ReadOnlySpan<PipelineResourceProperty> Properties => _properties;

        #endregion

        #region Constructors

        public PipelineResourceProperties(in ReadOnlySpan<PipelineResourceProperty> properties)
        {
            Debug.Assert(!properties.IsEmpty, "Cannot create PipelineResourceProperties with no properties!");
            for (int i = 0; i < properties.Length; i++)
                for (int j = i + 1; j < properties.Length; j++)
                    Debug.Assert(properties[i].uniqueBinding != properties[j].uniqueBinding, $"PipelineResourceProperties at index {i} and {j} have the same UniqueBinding {properties[i].uniqueBinding}");

            _properties = properties.ToArray(); //Copy for safety
        }

        #endregion

        #region Public Methods

        public override bool Equals(object? obj) => obj is PipelineResourceProperties properties && Equals(properties);
        public bool Equals(PipelineResourceProperties other)
        {
            if (_properties == null)
                return other._properties == null;
            else
            {
                if (other._properties == null)
                    return false;

                if (_properties.Length != other._properties.Length)
                    return false;

                for (int i = 0; i < _properties.Length; i++)
                    if (!_properties[i].Equals(other._properties[i]))
                        return false;

                return true;
            }
        }
        public bool Equals(in PipelineResourceProperties other)
        {
            if (_properties == null)
                return other._properties == null;
            else
            {
                if (other._properties == null)
                    return false;

                if (_properties.Length != other._properties.Length)
                    return false;

                for (int i = 0; i < _properties.Length; i++)
                    if (!_properties[i].Equals(other._properties[i]))
                        return false;

                return true;
            }
        }
        public override int GetHashCode() => _properties != null ? StructuralComparisons.StructuralEqualityComparer.GetHashCode(_properties) : 0;

        #endregion

        #region Operators

        public static bool operator ==(PipelineResourceProperties left, PipelineResourceProperties right) => left.Equals(right);
        public static bool operator ==(in PipelineResourceProperties left, in PipelineResourceProperties right) => left.Equals(right);
        public static bool operator !=(PipelineResourceProperties left, PipelineResourceProperties right) => !(left == right);
        public static bool operator !=(in PipelineResourceProperties left, in PipelineResourceProperties right) => !(left == right);
        
        #endregion

    }
}
