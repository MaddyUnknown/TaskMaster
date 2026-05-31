namespace TaskMaster.API.Entities.Abstractions
{
    public class BaseEntity
    {
        public long Id { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public DateTime? ModifyDateTime { get; set; }
    }
}
