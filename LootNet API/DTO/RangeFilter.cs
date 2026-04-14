namespace LootNet_API.DTO;

public class RangeFilter<T> where T : struct, IComparable<T>
{
    public T? Min { get; set; }
    public T? Max { get; set; }

    public bool IsValid =>
        !(Min.HasValue && Max.HasValue && Min.Value.CompareTo(Max.Value) > 0);
}
