namespace Project.Base.Models.Entities
{
    public class BaseEntity<TId> : IModel<TId>, IAuditableEntity
    {
        public virtual TId Id { get; set; }

        public DateTime? DateCreated { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? DateModified { get; set; }

        public string ModifiedBy { get; set; }

        public bool IsActive { get; set; }
    }

    public class BaseAuditableEntity : IAuditableEntity
    {
        public DateTime? DateCreated { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? DateModified { get; set; }

        public string ModifiedBy { get; set; }
    }

    public interface IAuditableEntity
    {
        DateTime? DateCreated { get; set; }

        DateTime? DateModified { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }

    public interface IModel { }

    public interface IModel<TId> : IModel
    {
        TId Id { get; set; }
    }
}
