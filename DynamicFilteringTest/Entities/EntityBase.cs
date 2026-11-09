using System.ComponentModel.DataAnnotations;

namespace DynamicFilteringTest.Entities
{
    /// <summary>
    /// Base type for entities and for the types built from views.
    /// Every dynamic-filter view must return an <c>Id</c> column; it is the key the filtered entity is joined on.
    /// </summary>
    public abstract class EntityBase
    {
        [Key]
        public int Id { get; set; }
    }
}
