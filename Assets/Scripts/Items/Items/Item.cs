public class Item
{
    public ItemData ItemData;
    public int Id { get; private set; }
    public int Count { get; set; }  
    
    public Item(ItemData data, int id, int count)
    {
        ItemData = data;
        Id = id;
        Count = count;
    }
}