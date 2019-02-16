using System;

namespace DynamicHash
{
    public interface IRecord<T> : IConverter, IEquatable<T> where T : new()
    {
        
    }
}
