using System;

namespace Domain.Entity
{
    public class Filter
    // Filter means Filter data or specific processing conditions， for example in "chair" group we can choose filter "wooden"
    {
        public Guid Id { get; set; } //Primary Key
        public string Name { get; set; }
        public int Order { get; set; }

        // [Tag][Tag][Tag]... Excel Format
        // ["Tag","Tag","Tag"] json => Db Format
        public string Tags { get; set; }


        // [Group],[Group],[Group]
        ///["Group","Group","Group"] json => Db Format
        public string Groups { get; set; }
    }
}
