namespace TranSimCS.Save2 {
    public delegate void Setter<TSource, TField> (ref TSource source, TField value);
}