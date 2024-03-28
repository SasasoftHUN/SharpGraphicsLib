using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGraphics
{
    public enum VertexInputRate : uint { Vertex = 0u, Instance = 1u }
    public readonly struct VertexInputBinding : IEquatable<VertexInputBinding>
    {

        public readonly uint stride;
        public readonly VertexInputRate rate;

        public VertexInputBinding(uint stride, VertexInputRate rate)
        {
            this.stride = stride;
            this.rate = rate;
        }
        public VertexInputBinding(uint stride) : this(stride, VertexInputRate.Vertex) { }
        public VertexInputBinding(VertexInputRate rate) : this(0u, rate) { }

        public static VertexInputBinding Create<T>(VertexInputRate rate = VertexInputRate.Vertex) where T : unmanaged
            => new VertexInputBinding((uint)Marshal.SizeOf<T>(), rate);

        public override bool Equals(object? obj) => obj is VertexInputBinding binding && Equals(in binding);
        public bool Equals(VertexInputBinding other) => stride == other.stride && rate == other.rate;
        public bool Equals(in VertexInputBinding other) => stride == other.stride && rate == other.rate;
        public override int GetHashCode() => HashCode.Combine(stride, rate);

        public static bool operator ==(VertexInputBinding left, VertexInputBinding right) => left.Equals(right);
        public static bool operator ==(in VertexInputBinding left, in VertexInputBinding right) => left.Equals(right);
        public static bool operator !=(VertexInputBinding left, VertexInputBinding right) => !(left == right);
        public static bool operator !=(in VertexInputBinding left, in VertexInputBinding right) => !(left == right);

    }

    public readonly struct VertexInputAttribute : IEquatable<VertexInputAttribute>
    {

        public readonly uint binding;
        public readonly uint location;
        public readonly uint offset;
        public readonly DataFormat format;

        public VertexInputAttribute(uint binding, uint location, uint offset, DataFormat format)
        {
            this.binding = binding;
            this.location = location;
            this.offset = offset;
            this.format = format;
        }
        public VertexInputAttribute(uint location, uint offset, DataFormat format) : this(0u, location, offset, format) { }

        public override bool Equals(object? obj) => obj is VertexInputAttribute attribute && Equals(in attribute);
        public bool Equals(VertexInputAttribute other)
            => binding == other.binding &&
                location == other.location &&
                offset == other.offset &&
                format == other.format;
        public bool Equals(in VertexInputAttribute other)
            => binding == other.binding &&
                location == other.location &&
                offset == other.offset &&
                format == other.format;

        public override int GetHashCode() => HashCode.Combine(binding, location, offset, format);

        public static bool operator ==(VertexInputAttribute left, VertexInputAttribute right) => left.Equals(right);
        public static bool operator ==(in VertexInputAttribute left, in VertexInputAttribute right) => left.Equals(in right);
        public static bool operator !=(VertexInputAttribute left, VertexInputAttribute right) => !(left == right);
        public static bool operator !=(in VertexInputAttribute left, in VertexInputAttribute right) => !(left == right);
    }

    public readonly struct VertexInputs : IEquatable<VertexInputs>
    {

        private readonly VertexInputBinding[]? _bindings; //Null can be valid when initialized with default constructor or when using no Vertex Inputs
        private readonly VertexInputAttribute[]? _attributes;

        public ReadOnlySpan<VertexInputBinding> Bindings => _bindings;
        public ReadOnlySpan<VertexInputAttribute> Attributes => _attributes;

        public VertexInputs(in ReadOnlySpan<VertexInputBinding> bindings, in ReadOnlySpan<VertexInputAttribute> attributes)
        {
            _bindings = bindings.IsEmpty ? null : bindings.ToArray(); //Copy for Safety
            _attributes = attributes.IsEmpty ? null : attributes.ToArray(); //Copy for Safety
        }

        public override bool Equals(object? obj) => obj is VertexInputs inputs && Equals(in inputs);

        public bool Equals(VertexInputs other)
        {
            if (_bindings == null)
                return other._bindings == null;
            else
            {
                if (other._bindings == null)
                    return false;

                if (_bindings.Length != other._bindings.Length)
                    return false;

                for (int i = 0; i < _bindings.Length; i++)
                    if (!_bindings[i].Equals(other._bindings[i]))
                        return false;
            }

            if (_attributes == null)
                return other._attributes == null;
            else
            {
                if (other._attributes == null)
                    return false;

                if (_attributes.Length != other._attributes.Length)
                    return false;

                for (int i = 0; i < _attributes.Length; i++)
                    if (!_attributes[i].Equals(other._attributes[i]))
                        return false;
            }

            return true;
        }
        public bool Equals(in VertexInputs other)
        {
            if (_bindings == null)
                return other._bindings == null;
            else
            {
                if (other._bindings == null)
                    return false;

                if (_bindings.Length != other._bindings.Length)
                    return false;

                for (int i = 0; i < _bindings.Length; i++)
                    if (!_bindings[i].Equals(other._bindings[i]))
                        return false;
            }

            if (_attributes == null)
                return other._attributes == null;
            else
            {
                if (other._attributes == null)
                    return false;

                if (_attributes.Length != other._attributes.Length)
                    return false;

                for (int i = 0; i < _attributes.Length; i++)
                    if (!_attributes[i].Equals(other._attributes[i]))
                        return false;
            }

            return true;
        }

        public override int GetHashCode() => HashCode.Combine(
            _bindings != null ? StructuralComparisons.StructuralEqualityComparer.GetHashCode(_bindings) : 0,
            _attributes != null ? StructuralComparisons.StructuralEqualityComparer.GetHashCode(_attributes) : 0);

        public static bool operator ==(VertexInputs left, VertexInputs right) => left.Equals(right);
        public static bool operator ==(in VertexInputs left, in VertexInputs right) => left.Equals(right);
        public static bool operator !=(VertexInputs left, VertexInputs right) => !(left == right);
        public static bool operator !=(in VertexInputs left, in VertexInputs right) => !(left == right);

    }
}
