namespace Argon;

interface IWrappedCollection : IList
{
    object UnderlyingCollection { get; }
}